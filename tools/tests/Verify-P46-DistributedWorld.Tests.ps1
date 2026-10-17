# Plain PowerShell (5.1+) checks for the P4.6 distributed runner/verifier; no Pester or Unity build needed.
# Verify cases use fake evidence folders; runner cases only exercise parameter validation paths.
$ErrorActionPreference = 'Stop'
$tools = Split-Path -Parent $PSScriptRoot
$verify = Join-Path $tools 'Verify-P46-DistributedWorld.ps1'
$runner = Join-Path $tools 'Test-P46-DistributedWorld.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) ('p46-verify-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$script:passed = 0; $script:failed = 0

function New-Evidence([string]$case, [int]$slot, [string]$machine, [string]$mode, [int]$perMachine,
    [string]$hash = 'AAAA', [int]$players = 4) {
    $dir = Join-Path $work "$case/slot$slot"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    @{passed=$true; worldLoads=1; players=$players; distance=150.5; firstDay=1; morningDay=2; errors=@()} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dir "player$slot.json") -Encoding UTF8
    $manifest = [ordered]@{RunId='fake-run'; Players=$players; Slot=$slot; Role=$(if ($slot -eq 0) {'host'} else {'client'})
        Port=27677; Machine=$machine; ExitCode=0; Passed=$true; SourceReport="player$slot.json"
        Files=@(@{File='DeepDiveGame-P1.exe'; Sha256=$hash})}
    # Empty mode = legacy manifest written before the 2+2 fields existed.
    if ($mode) { $manifest.ProcessesPerMachine = $perMachine; $manifest.Mode = $mode }
    [pscustomobject]$manifest | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $dir 'machine-evidence.json') -Encoding UTF8
    $dir
}

function Check([string]$name, [bool]$expectPass, [scriptblock]$body, [string]$expectText) {
    $outcome = $null; $text = ''
    try { $text = (& $body | Out-String); $outcome = $true } catch { $outcome = $false; $text = $_.Exception.Message }
    $ok = ($outcome -eq $expectPass) -and (-not $expectText -or $text -like "*$expectText*")
    $label = if ($ok) { 'OK  ' } else { 'FAIL' }
    if ($ok) { $script:passed++ } else { $script:failed++ }
    $verdict = if ($outcome) { 'PASS' } else { 'FAIL' }
    Write-Output ("[{0}] {1} -> {2}: {3}" -f $label, $name, $verdict, ($text.Trim() -split "`r?`n" | Select-Object -Last 1))
}

try {
    Write-Output '== Verify-P46-DistributedWorld =='
    $d = @(0..3 | ForEach-Object { New-Evidence 'sep4' $_ "PC-$_" 'separate-pc' 1 })
    Check 'separate-pc, 4 distinct machines' $true { & $verify -EvidenceDirs $d } 'separate-computer'

    $d = @(0..3 | ForEach-Object { New-Evidence 'legacy4' $_ "PC-$_" '' 0 })
    Check 'legacy manifests (no Mode), 4 distinct machines' $true { & $verify -EvidenceDirs $d } 'separate-computer'

    $d = @(0..1 | ForEach-Object { New-Evidence 'sep2' $_ "PC-$_" 'separate-pc' 1 -players 2 })
    Check 'separate-pc, Players 2 on 2 machines' $true { & $verify -EvidenceDirs $d } 'separate-computer'

    $d = @(0..3 | ForEach-Object { New-Evidence 'sepdup' $_ $(if ($_ -lt 2) {'PC-A'} else {"PC-$_"}) 'separate-pc' 1 })
    Check 'separate-pc, repeated machine name' $false { & $verify -EvidenceDirs $d } 'distinct machines'

    $d = @(0..3 | ForEach-Object { New-Evidence 'two' $_ $(if ($_ -lt 2) {'PC-A'} else {'PC-B'}) 'two-per-pc' 2 })
    Check 'two-per-pc 2x2 with -MinMachines 2' $true { & $verify -EvidenceDirs $d -MinMachines 2 } 'NOT four-real-human acceptance'
    Check 'two-per-pc 2x2 without -MinMachines' $false { & $verify -EvidenceDirs $d } '-MinMachines 2'

    $d = @(0..3 | ForEach-Object { New-Evidence 'three' $_ $(if ($_ -lt 3) {'PC-A'} else {'PC-B'}) 'two-per-pc' 2 })
    Check 'two-per-pc, 3 slots on one machine' $false { & $verify -EvidenceDirs $d -MinMachines 2 } 'ran 3 slots'

    $d = @(0..3 | ForEach-Object { New-Evidence 'split' $_ $(if ($_ -in 0,2) {'PC-A'} else {'PC-B'}) 'two-per-pc' 2 })
    Check 'two-per-pc, slot 0 and 1 on different machines' $false { & $verify -EvidenceDirs $d -MinMachines 2 } 'same (host) machine'

    $d = @(0..3 | ForEach-Object {
        if ($_ -lt 2) { New-Evidence 'mixed' $_ 'PC-A' 'two-per-pc' 2 } else { New-Evidence 'mixed' $_ "PC-$_" 'separate-pc' 1 } })
    Check 'mixed separate-pc / two-per-pc' $false { & $verify -EvidenceDirs $d -MinMachines 2 } 'Mixed evidence modes'
    Check 'mixed modes without -MinMachines' $false { & $verify -EvidenceDirs $d } 'Mixed evidence modes'

    $d = @(0..3 | ForEach-Object { New-Evidence 'sepmin2' $_ "PC-$_" 'separate-pc' 1 })
    Check 'separate-pc evidence with -MinMachines 2' $false { & $verify -EvidenceDirs $d -MinMachines 2 } 'only two-per-pc'

    $d = @(0..3 | ForEach-Object { New-Evidence 'hash' $_ $(if ($_ -lt 2) {'PC-A'} else {'PC-B'}) 'two-per-pc' 2 $(if ($_ -eq 3) {'BBBB'} else {'AAAA'}) })
    Check 'two-per-pc, build hash mismatch still rejected' $false { & $verify -EvidenceDirs $d -MinMachines 2 } 'fingerprint mismatch'

    Write-Output ''
    Write-Output '== Test-P46-DistributedWorld parameter paths (no Unity process started) =='
    $noBuild = Join-Path $work 'missing/DeepDiveGame-P1.exe'
    Check 'Players 2 + ProcessesPerMachine 2 rejected' $false { & $runner -Players 2 -Slot 0 -ProcessesPerMachine 2 -BuildPath $noBuild } 'meaningless'
    Check 'odd Slot with ProcessesPerMachine 2 rejected' $false { & $runner -Players 4 -Slot 1 -ProcessesPerMachine 2 -HostIp 10.0.0.1 -BuildPath $noBuild } 'even -Slot'
    Check 'guest pair without HostIp rejected' $false { & $runner -Players 4 -Slot 2 -ProcessesPerMachine 2 -BuildPath $noBuild } '-HostIp'
    Check 'ProcessesPerMachine 3 rejected by ValidateSet' $false { & $runner -Players 4 -Slot 0 -ProcessesPerMachine 3 -BuildPath $noBuild } 'ProcessesPerMachine'
    Check 'host pair passes validation (stops at missing build)' $false { & $runner -Players 4 -Slot 0 -ProcessesPerMachine 2 -BuildPath $noBuild } 'Unity build missing'
    Check 'guest pair passes validation (stops at missing build)' $false { & $runner -Players 4 -Slot 2 -ProcessesPerMachine 2 -HostIp 10.0.0.1 -BuildPath $noBuild } 'Unity build missing'
    Check 'default separate-pc unchanged (stops at missing build)' $false { & $runner -Players 4 -Slot 3 -HostIp 10.0.0.1 -BuildPath $noBuild } 'Unity build missing'
    Check 'default separate-pc guest without HostIp unchanged' $false { & $runner -Players 4 -Slot 1 -BuildPath $noBuild } '-HostIp'
} finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Output ''
Write-Output "Result: $script:passed passed, $script:failed failed"
if ($script:failed -gt 0) { exit 1 }
