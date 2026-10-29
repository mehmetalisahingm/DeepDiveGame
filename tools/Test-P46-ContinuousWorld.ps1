param([ValidateSet(1,2,4)][int]$Players = 2, [int]$Port = 19777, [string]$BuildPath = '', [switch]$Render)
$ErrorActionPreference = 'Stop'
$worldRoot = Split-Path -Parent $PSScriptRoot
$worldIsWindows = $env:OS -eq 'Windows_NT'
if ($BuildPath) { $worldBuild = $BuildPath }
elseif ($worldIsWindows) { $worldBuild = Join-Path $worldRoot 'Builds/P1-Integrated/DeepDiveGame-P1.exe' }
else { $worldBuild = Join-Path $worldRoot 'Builds/P1-Integrated/DeepDiveGame-P1.x86_64' }
if (-not (Test-Path -LiteralPath $worldBuild)) { throw "Integrated player missing: $worldBuild" }
$worldRun = Join-Path $worldRoot ('Logs/P46-world-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $Players)
New-Item -ItemType Directory -Path $worldRun | Out-Null
$worldProcesses = @()
try {
    for ($worldSlot = 0; $worldSlot -lt $Players; $worldSlot++) {
        $worldReport = Join-Path $worldRun ("player$worldSlot.json")
        $worldLog = Join-Path $worldRun ("player$worldSlot.log")
        $worldGraphics = if ($worldIsWindows -and -not $Render) { @('-nographics') }
            else { @('-screen-width', '320', '-screen-height', '200', '-screen-fullscreen', '0') }
        $worldArgs = @('-batchmode') + $worldGraphics + @('-p46-world-role', $(if ($worldSlot -eq 0) {'host'} else {'client'}),
            '-p46-world-count', $Players, '-p46-world-slot', $worldSlot, '-p46-world-port', $Port,
            '-p1-report', ('"{0}"' -f $worldReport), '-logFile', ('"{0}"' -f $worldLog))
        if ($worldIsWindows) { $worldProcess = Start-Process -FilePath $worldBuild -ArgumentList $worldArgs -WindowStyle Hidden -PassThru }
        else { $worldProcess = Start-Process -FilePath $worldBuild -ArgumentList $worldArgs -PassThru }
        $worldProcesses += [pscustomobject]@{Process=$worldProcess; Report=$worldReport; Slot=$worldSlot}
        # Reserve admission slots in a known order; this is not an actor position fixture.
        $worldConnectLimit = (Get-Date).AddSeconds(18)
        do {
            Start-Sleep -Milliseconds 200
            $worldConnected = Select-String -LiteralPath $worldLog -SimpleMatch "P1_CONNECTED id=$worldSlot" -Quiet -ErrorAction SilentlyContinue
        } while (-not $worldConnected -and -not $worldProcess.HasExited -and (Get-Date) -lt $worldConnectLimit)
        if (-not $worldConnected) { throw "Player $worldSlot did not connect. $worldLog" }
    }
    $worldLimit = (Get-Date).AddSeconds(240)
    while (@($worldProcesses | Where-Object {-not $_.Process.HasExited}).Count -gt 0 -and (Get-Date) -lt $worldLimit) {
        Start-Sleep -Milliseconds 500
    }
    $worldFailures = @()
    foreach ($worldEntry in $worldProcesses) {
        if (-not (Test-Path -LiteralPath $worldEntry.Report)) { $worldFailures += "Player $($worldEntry.Slot): missing report"; continue }
        $worldResult = Get-Content -LiteralPath $worldEntry.Report -Raw | ConvertFrom-Json
        [pscustomobject]@{Player=$worldEntry.Slot; Passed=$worldResult.passed; Players=$worldResult.players;
            WorldLoads=$worldResult.worldLoads; Distance=$worldResult.distance; Step=$worldResult.completedStep;
            Errors=($worldResult.errors -join '; ')}
        if (-not $worldResult.passed -or $worldEntry.Process.ExitCode -ne 0) { $worldFailures += "Player $($worldEntry.Slot): $($worldResult.errors -join '; ')" }
    }
    Write-Output "Evidence: $worldRun"
    if ($worldFailures.Count -gt 0) { throw ($worldFailures -join "`n") }
    # A fresh process restores the same campaign's day/summary and enters the home world.
    $worldHost = Get-Content -LiteralPath (Join-Path $worldRun 'player0.json') -Raw | ConvertFrom-Json
    $worldReloadReport = Join-Path $worldRun 'reload.json'
    Copy-Item -LiteralPath (Join-Path $worldRun 'player0.json.campaign.json') -Destination ($worldReloadReport + '.campaign.json')
    $worldReloadGraphics = if ($worldIsWindows -and -not $Render) { @('-nographics') }
        else { @('-screen-width', '320', '-screen-height', '200', '-screen-fullscreen', '0') }
    $worldReloadArgs = @('-batchmode') + $worldReloadGraphics + @('-p46-world-role', 'host', '-p46-world-count', 1,
        '-p46-world-slot', 0, '-p46-world-port', ($Port + 1), '-p46-world-reload-day', $worldHost.morningDay,
        '-p1-report', ('"{0}"' -f $worldReloadReport), '-logFile', ('"{0}"' -f (Join-Path $worldRun 'reload.log')))
    if ($worldIsWindows) { $worldReloadProcess = Start-Process -FilePath $worldBuild -ArgumentList $worldReloadArgs -WindowStyle Hidden -PassThru }
    else { $worldReloadProcess = Start-Process -FilePath $worldBuild -ArgumentList $worldReloadArgs -PassThru }
    $worldProcesses += [pscustomobject]@{Process=$worldReloadProcess; Report=$worldReloadReport; Slot='reload'}
    if (-not $worldReloadProcess.WaitForExit(45000)) { throw 'Reload process timed out.' }
    if (-not (Test-Path -LiteralPath $worldReloadReport)) { throw 'Reload report missing.' }
    $worldReload = Get-Content -LiteralPath $worldReloadReport -Raw | ConvertFrom-Json
    if (-not $worldReload.passed -or $worldReloadProcess.ExitCode -ne 0) { throw "Reload failed: $($worldReload.errors -join '; ')" }
    Write-Output "Fresh-process reload PASS: day $($worldReload.firstDay), home spawn, previous early-sleep summary."
} finally {
    foreach ($worldEntry in $worldProcesses) {
        if (-not $worldEntry.Process.HasExited) { Stop-Process -Id $worldEntry.Process.Id -Force }
    }
}
