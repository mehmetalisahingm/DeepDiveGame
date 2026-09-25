param([ValidateSet(1,2,4)][int]$Players = 4, [int]$Port = 18777, [switch]$Capture, [switch]$Hunt, [switch]$Record, [switch]$Event, [switch]$Town, [switch]$Boat, [switch]$Trip, [switch]$Unified)
$ErrorActionPreference = 'Stop'
$p1Root = Split-Path -Parent $PSScriptRoot
$p1Build = Join-Path $p1Root 'Builds/P1-Integrated/DeepDiveGame-P1.exe'
if (-not (Test-Path -LiteralPath $p1Build)) { throw 'Build-P1.ps1 -Integrated calistirin.' }
$p1Run = Join-Path $p1Root ('Logs/P1-integrated-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $Players)
New-Item -ItemType Directory -Path $p1Run | Out-Null
$p1Processes = [System.Collections.Generic.List[object]]::new()
function Start-P1Integrated([string]$Name, [string]$Mode, [string]$Reason = '') {
    $p1Report = Join-Path $p1Run ($Name + '.json')
    $p1Log = Join-Path $p1Run ($Name + '.log')
    if ($Unified) {
        $p1Args = @('-batchmode', '-p3-unified-role', $Mode, '-p3-unified-count', $Players,
            '-p3-unified-port', $Port, '-p3-unified-report', ('"{0}"' -f $p1Report), '-logFile', ('"{0}"' -f $p1Log))
    } else {
        $p1Args = @('-batchmode', '-p1-integrated', $Mode, '-p1-count', $Players,
            '-p1-port', $Port, '-p1-report', ('"{0}"' -f $p1Report), '-logFile', ('"{0}"' -f $p1Log))
    }
    if ($Capture -and $Name -eq 'host') {
        $capturePath = Join-Path $p1Run 'room.png'
        $p1Args += @('-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
        if ($Unified) { $p1Args += @('-p3-unified-screenshot', ('"{0}"' -f $capturePath)) }
        else { $p1Args += @('-p1-screenshot', ('"{0}"' -f $capturePath)) }
    } else { $p1Args += '-nographics' }
    if (-not $Unified) {
        if ($Reason) { $p1Args += @('-p1-reason', $Reason) }
        if ($Hunt) { $p1Args += @('-p2-hunt', '1') }
        if ($Record) { $p1Args += @('-p3-record', '1') }
        if ($Event) { $p1Args += @('-p3-event', '1') }
        if ($Town) { $p1Args += @('-p3-town', '1') }
        if ($Boat) { $p1Args += @('-p3-boat', '1') }
        if ($Trip) { $p1Args += @('-p3-trip', '1') }
    }
    $p1Process = Start-Process -FilePath $p1Build -ArgumentList $p1Args -WindowStyle Hidden -PassThru
    $p1Processes.Add([pscustomobject]@{Name=$Name; Process=$p1Process; Report=$p1Report})
}
function Wait-P1Marker([string]$Marker, [int]$Seconds) {
    $p1Limit = (Get-Date).AddSeconds($Seconds)
    do {
        Start-Sleep -Milliseconds 200
        $p1Found = Select-String -LiteralPath (Join-Path $p1Run 'host.log') -SimpleMatch $Marker -Quiet -ErrorAction SilentlyContinue
    } while (-not $p1Found -and (Get-Date) -lt $p1Limit)
    if (-not $p1Found) { throw "Host asamaya gelemedi: $Marker; $p1Run" }
}
try {
    Start-P1Integrated 'host' 'host'
    Start-Sleep -Seconds 2
    for ($i = 1; $i -lt $Players; $i++) {
        $mode = if ($Unified) { 'client' } elseif ($i -eq 1) { 'rejoin' } else { 'client' }
        Start-P1Integrated "client$i" $mode
    }
    if ($Players -eq 4 -and -not $Unified) {
        Wait-P1Marker 'P1_INTEGRATED_LOBBY_READY' 25
        Start-P1Integrated 'fifth' 'reject' 'RoomFull'
        Wait-P1Marker 'P1_SCENE name=DiveTestArea success=True' 40
        Start-P1Integrated 'late-dive' 'reject' 'WrongPhase'
    }
    $seconds = if ($Unified) { 360 } elseif ($Town) { 105 } elseif ($Event) { 150 } elseif ($Trip) { 175 } elseif ($Boat) { 95 } elseif ($Hunt -or $Record) { 90 } else { 60 }
    $p1Deadline = (Get-Date).AddSeconds($seconds)
    while (@($p1Processes | Where-Object {-not $_.Process.HasExited}).Count -gt 0 -and (Get-Date) -lt $p1Deadline) { Start-Sleep -Milliseconds 500 }
    $p1Failures = @()
    foreach ($p1Entry in $p1Processes) {
        if (-not (Test-Path -LiteralPath $p1Entry.Report)) { $p1Failures += "$($p1Entry.Name): rapor yok"; continue }
        $p1Result = Get-Content -Raw -LiteralPath $p1Entry.Report | ConvertFrom-Json
        if ($Unified) {
            [pscustomobject]@{Process=$p1Entry.Name; Passed=$p1Result.passed; Players=$p1Result.maxPlayers;
                Camera=$p1Result.cameraBought; Recording=$p1Result.recordingStopped; Hunt=$p1Result.catchDespawned;
                Repair=$p1Result.boatRepaired; Services=$p1Result.servicesCleared; Trip=$p1Result.tripDone;
                SaveLoad=$p1Result.saveReload; Reason=$p1Result.reason}
        } else {
            [pscustomobject]@{Process=$p1Entry.Name; Passed=$p1Result.passed; Players=$p1Result.maxPlayers;
                Walk=$p1Result.walk; Swim=$p1Result.swim; ReadyReset=$p1Result.readyReset; Rejoined=$p1Result.clientRejoined; Reason=$p1Result.reason}
        }
        if (-not $p1Result.passed -or $p1Entry.Process.ExitCode -ne 0) { $p1Failures += "$($p1Entry.Name): $($p1Result.errors -join ', ')" }
    }
    Write-Output "Yerel entegre test raporlari: $p1Run"
    if ($p1Failures.Count -gt 0) { throw ($p1Failures -join '; ') }
} finally {
    foreach ($p1Entry in $p1Processes) { if (-not $p1Entry.Process.HasExited) { Stop-Process -Id $p1Entry.Process.Id -Force } }
}
