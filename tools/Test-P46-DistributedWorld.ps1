# One real Unity process per physical Windows computer (default), or a 2+2 technical
# rehearsal: two processes per computer on two computers (-ProcessesPerMachine 2).
# Automated network/geometry evidence, NOT human-controlled gameplay acceptance.
param(
    [ValidateSet(2,4)][int]$Players = 4,
    [ValidateRange(0,3)][int]$Slot,
    [ValidateRange(1024,65535)][int]$Port = 27677,
    [string]$HostIp = '',
    [string]$BuildPath = '',
    [ValidatePattern('^[a-zA-Z0-9._-]{3,60}$')][string]$RunId = 'p46-distributed',
    [ValidateSet(1,2)][int]$ProcessesPerMachine = 1
)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Separate Windows PC acceptance runner only.' }
if ($Slot -ge $Players) { throw 'Slot must be lower than Players.' }
if ($ProcessesPerMachine -eq 2) {
    if ($Players -ne 4) { throw '-ProcessesPerMachine 2 needs -Players 4 (2 computers x 2 processes); -Players 2 with it is meaningless.' }
    if ($Slot % 2 -ne 0) { throw '-ProcessesPerMachine 2 needs an even -Slot: 0 on the host computer, 2 on the other computer.' }
}
if ($Slot -ne 0 -and [string]::IsNullOrWhiteSpace($HostIp)) {
    throw 'Guest requires -HostIp of the actual reachable host computer.'
}
$mode = if ($ProcessesPerMachine -eq 2) { 'two-per-pc' } else { 'separate-pc' }
$root = Split-Path -Parent $PSScriptRoot
if (-not $BuildPath) { $BuildPath = Join-Path $root 'Builds/P1-Integrated/DeepDiveGame-P1.exe' }
if (-not (Test-Path -LiteralPath $BuildPath)) { throw "Unity build missing: $BuildPath" }
$exe = (Resolve-Path -LiteralPath $BuildPath).Path

# Each machine emits the identical-build comparison fingerprint (not just EXE).
$managed = Join-Path (Split-Path $exe -Parent) 'DeepDiveGame-P1_Data/Managed'
$hashInputs = @($exe)
if (Test-Path -LiteralPath $managed) {
    $hashInputs += @(Get-ChildItem -LiteralPath $managed -File -Filter 'DeepDive.*.dll' |
        Sort-Object Name | ForEach-Object { $_.FullName })
}
$hashes = @($hashInputs | ForEach-Object {
    [pscustomobject]@{File = (Split-Path $_ -Leaf); Sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}
})

function Start-Slot([int]$s) {
    $dir = Join-Path $root ("Logs/P46-distributed-" + $RunId + "-slot" + $s)
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $reportPath = Join-Path $dir "player$s.json"
    $logPath = Join-Path $dir "player$s.log"
    $role = if ($s -eq 0) { 'host' } else { 'client' }
    $procArgs = @('-batchmode', '-nographics',
        '-p46-world-role', $role, '-p46-world-count', $Players,
        '-p46-world-slot', $s, '-p46-world-port', $Port,
        '-p1-report', ('"{0}"' -f $reportPath), '-logFile', ('"{0}"' -f $logPath))
    # The host computer's second process joins its own host over loopback.
    if ($s -gt 0) { $procArgs += @('-p46-world-host', $(if ($Slot -eq 0) { '127.0.0.1' } else { $HostIp })) }
    $startedUtc = [DateTime]::UtcNow.ToString('o')
    Write-Output "Starting $role slot $s/$Players on port $Port; run $RunId; mode $mode"
    $p = Start-Process -FilePath $exe -ArgumentList $procArgs -WindowStyle Hidden -PassThru
    $script:entries += [pscustomobject]@{Slot=$s; Role=$role; Dir=$dir; Report=$reportPath; Log=$logPath
        Process=$p; StartedUtc=$startedUtc}
}

# Admission order decides the home spawn slot, so the same-computer pair connects one by one.
function Wait-Connected($entry) {
    $limit = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 250
        $hit = Select-String -LiteralPath $entry.Log -Pattern 'P1_CONNECTED id=(\d+)' -ErrorAction SilentlyContinue |
            Select-Object -First 1
    } while (-not $hit -and -not $entry.Process.HasExited -and (Get-Date) -lt $limit)
    if (-not $hit) { throw "Slot $($entry.Slot) did not connect within 30 s; log: $($entry.Log)" }
    $id = [int]$hit.Matches[0].Groups[1].Value
    if ($id -ne $entry.Slot) {
        throw "Slot $($entry.Slot) was admitted as network id $id (wrong start order); log: $($entry.Log)"
    }
    Write-Output "Slot $($entry.Slot) connected as network id $id."
}

$entries = @()
try {
    if ($ProcessesPerMachine -eq 1) {
        Start-Slot $Slot
        Write-Output "Start host, then guests on their separate computers using the SAME game build."
    } else {
        Write-Output "2+2 technical rehearsal: two processes on this computer; NOT four-human acceptance."
        Start-Slot $Slot
        Wait-Connected $entries[0]
        Start-Slot ($Slot + 1)
        Wait-Connected $entries[1]
        if ($Slot -eq 0) {
            Write-Output "Host computer ready (slots 0+1). Now start the other computer: -Slot 2 -ProcessesPerMachine 2 -HostIp <this computer's IP>."
        }
    }
    $deadline = (Get-Date).AddMilliseconds(300000)
    $failures = @()
    foreach ($e in $entries) {
        $left = [Math]::Max(0, [int]($deadline - (Get-Date)).TotalMilliseconds)
        if (-not $e.Process.WaitForExit($left)) { $failures += "Process timed out; log: $($e.Log)"; continue }
        if (-not (Test-Path -LiteralPath $e.Report)) { $failures += "No machine report. Log: $($e.Log)"; continue }
        $report = Get-Content -Raw -LiteralPath $e.Report | ConvertFrom-Json
        $manifest = [pscustomobject]@{
            RunId=$RunId; Players=$Players; Slot=$e.Slot; Role=$e.Role; Port=$Port
            Machine=[Environment]::MachineName
            ProcessesPerMachine=$ProcessesPerMachine; Mode=$mode
            StartedUtc=$e.StartedUtc; FinishedUtc=[DateTime]::UtcNow.ToString('o')
            ExitCode=$e.Process.ExitCode; Passed=($report.passed -eq $true)
            WorldLoads=$report.worldLoads; Meters=$report.distance
            FirstDay=$report.firstDay; NextDay=$report.morningDay
            SourceReport=(Split-Path $e.Report -Leaf)
            Files=$hashes
        }
        $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $e.Dir 'machine-evidence.json') -Encoding UTF8
        Write-Output "Evidence folder: $($e.Dir)"
        if ($e.Process.ExitCode -ne 0 -or $report.passed -ne $true) {
            $failures += "Unity process failed or route validation failed; see $($e.Report) and $($e.Log)"
            continue
        }
        Write-Output "Local $($e.Role) slot $($e.Slot) automated physical route PASS; this is not human play approval."
    }
    if ($failures.Count -gt 0) { throw ($failures -join "`n") }
} finally {
    foreach ($e in $entries) {
        if (-not $e.Process.HasExited) { Stop-Process -Id $e.Process.Id -Force }
    }
}
