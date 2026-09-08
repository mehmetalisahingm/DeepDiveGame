param([switch]$Solo, [string]$BuildPath, [int]$Port = 17777)
$ErrorActionPreference = 'Stop'
$p1Root = Split-Path -Parent $PSScriptRoot
if (-not $BuildPath) { $BuildPath = Join-Path $p1Root 'Builds/P1-NetworkLab/DeepDiveGame-P1.exe' }
if (-not (Test-Path -LiteralPath $BuildPath)) { throw 'Once Build-P1.ps1 calistirin.' }
$p1RunDir = Join-Path $p1Root ('Logs/P1-network-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $Port)
New-Item -ItemType Directory -Path $p1RunDir | Out-Null
$p1Processes = [System.Collections.Generic.List[object]]::new()
$p1Expected = if ($Solo) { 1 } else { 4 }
function Start-P1Process([string]$Name, [string]$Mode, [string]$Reason = '') {
    $p1Report = Join-Path $p1RunDir ($Name + '.json')
    $p1Log = Join-Path $p1RunDir ($Name + '.log')
    $p1Arguments = @('-batchmode', '-nographics', '-p1-smoke', $Mode, '-p1-port', $Port,
        '-p1-count', $p1Expected, '-p1-duration', '32', '-p1-report', ('"{0}"' -f $p1Report),
        '-logFile', ('"{0}"' -f $p1Log))
    if ($Reason) { $p1Arguments += @('-p1-reason', $Reason) }
    $p1Process = Start-Process -FilePath $BuildPath -ArgumentList $p1Arguments -WindowStyle Hidden -PassThru
    $p1Processes.Add([pscustomobject]@{Name=$Name; Process=$p1Process; Report=$p1Report; Log=$p1Log})
}
try {
    Start-P1Process 'host' $(if ($Solo) { 'solo' } else { 'host' })
    if (-not $Solo) {
        Start-Sleep -Seconds 2
        1..3 | ForEach-Object { Start-P1Process "client$_" 'client' }
        Start-Sleep -Seconds 5
        Start-P1Process 'fifth' 'reject' 'RoomFull'
        $p1DiveDeadline = (Get-Date).AddSeconds(40)
        $p1HostLog = Join-Path $p1RunDir 'host.log'
        do {
            Start-Sleep -Milliseconds 200
            $p1Diving = (Test-Path -LiteralPath $p1HostLog) -and
                (Select-String -LiteralPath $p1HostLog -SimpleMatch 'P1_SCENE name=P1NetworkWaterLab success=True' -Quiet)
        } while (-not $p1Diving -and (Get-Date) -lt $p1DiveDeadline)
        if (-not $p1Diving) { throw 'Host sualti sahnesini yukleyemedi.' }
        Start-P1Process 'late-dive' 'reject' 'WrongPhase'
    }
    $p1Deadline = (Get-Date).AddSeconds(65)
    do {
        Start-Sleep -Milliseconds 500
        $p1Running = @($p1Processes | Where-Object { -not $_.Process.HasExited })
    } while ($p1Running.Count -gt 0 -and (Get-Date) -lt $p1Deadline)
    $p1Failures = @()
    foreach ($p1Entry in $p1Processes) {
        if (-not (Test-Path -LiteralPath $p1Entry.Report)) { $p1Failures += "$($p1Entry.Name): rapor yok"; continue }
        $p1Result = Get-Content -Raw -LiteralPath $p1Entry.Report | ConvertFrom-Json
        [pscustomobject]@{Process=$p1Entry.Name; Passed=$p1Result.passed; Players=$p1Result.maxPlayers;
            Walk=$p1Result.walk; Swim=$p1Result.swim; Returned=$p1Result.returned; Stopped=$p1Result.stopped; Reason=$p1Result.reason}
        if (-not $p1Result.passed -or $p1Entry.Process.ExitCode -ne 0) { $p1Failures += "$($p1Entry.Name): $($p1Result.errors -join ', ')" }
    }
    Write-Output "Yerel ag test raporlari: $p1RunDir"
    if ($p1Failures.Count -gt 0) { throw ($p1Failures -join '; ') }
} finally {
    foreach ($p1Entry in $p1Processes) {
        if (-not $p1Entry.Process.HasExited) { Stop-Process -Id $p1Entry.Process.Id -Force }
    }
}
