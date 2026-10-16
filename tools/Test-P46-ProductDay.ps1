param([ValidateSet(1,2,4)][int]$Players=1, [int]$Port=24777)
$ErrorActionPreference='Stop'
$productRoot=Split-Path -Parent $PSScriptRoot
$productBuild=Join-Path $productRoot 'Builds/P1-Integrated/DeepDiveGame-P1.exe'
if (-not (Test-Path -LiteralPath $productBuild)) { throw 'Build the integrated Windows player first.' }
$productRun=Join-Path $productRoot ('Logs/P46-product-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $Players)
New-Item -ItemType Directory -Path $productRun | Out-Null
$productProcesses=@()
try {
    for ($productSlot=0;$productSlot -lt $Players;$productSlot++) {
        $productReport=Join-Path $productRun "player$productSlot.json"
        $productLog=Join-Path $productRun "player$productSlot.log"
        $productArgs=@('-batchmode','-p46-day-role',$(if ($productSlot -eq 0) {'host'} else {'client'}),
            '-p46-day-count',$Players,'-p46-day-slot',$productSlot,'-p46-day-port',$Port,
            '-p1-report',('"{0}"' -f $productReport),'-logFile',('"{0}"' -f $productLog))
        if ($productSlot -eq 0) { $productArgs+=@('-screen-width','960','-screen-height','540','-screen-fullscreen','0') }
        else { $productArgs+='-nographics' }
        $productProcess=Start-Process -FilePath $productBuild -ArgumentList $productArgs -WindowStyle Hidden -PassThru
        $productProcesses += [pscustomobject]@{Slot=$productSlot;Process=$productProcess;Report=$productReport}
        $productConnectLimit=(Get-Date).AddSeconds(20)
        do {
            Start-Sleep -Milliseconds 200
            $productConnected=Select-String -LiteralPath $productLog -SimpleMatch "P1_CONNECTED id=$productSlot" -Quiet -ErrorAction SilentlyContinue
        } while (-not $productConnected -and -not $productProcess.HasExited -and (Get-Date) -lt $productConnectLimit)
        if (-not $productConnected) { throw "Player $productSlot could not connect: $productLog" }
    }
    $productLimit=(Get-Date).AddSeconds(1050)
    while (@($productProcesses | Where-Object {-not $_.Process.HasExited}).Count -gt 0 -and (Get-Date) -lt $productLimit) {
        Start-Sleep -Milliseconds 500
    }
    $productFailed=@()
    foreach ($productEntry in $productProcesses) {
        if (-not (Test-Path -LiteralPath $productEntry.Report)) { $productFailed+="Player $($productEntry.Slot) missing report"; continue }
        $productResult=Get-Content -LiteralPath $productEntry.Report -Raw|ConvertFrom-Json
        [pscustomobject]@{Player=$productEntry.Slot;Passed=$productResult.passed;Step=$productResult.step;Dives=$productResult.diveCount;
            FishSold=$productResult.soldCount;Day=$productResult.day;Income=$productResult.income;Error=$productResult.failure}
        if (-not $productResult.passed -or $productEntry.Process.ExitCode -ne 0) { $productFailed+="Player $($productEntry.Slot): $($productResult.errors -join '; ')" }
    }
    Write-Output "Evidence: $productRun"
    if ($productFailed.Count -gt 0) { throw ($productFailed -join "`n") }
    $productCheckpoint=Get-Content -LiteralPath (Join-Path $productRun 'player0.json.campaign.json') -Raw|ConvertFrom-Json
    if ($productCheckpoint.Day.DayNumber -ne 2 -or @($productCheckpoint.Day.ClosedCloseIds).Count -ne 1 -or
        @($productCheckpoint.Day.SummaryHistory).Count -ne 1 -or @($productCheckpoint.SoldCaptureIds).Count -ne 2 -or
        @($productCheckpoint.BoatPartIds).Count -ne 3 -or @($productCheckpoint.Media.Publications).Count -ne 1 -or
        @($productCheckpoint.ChannelSettleIds).Count -ne 1) { throw 'Campaign does not contain exactly one settled day/publication and the earned repair/catches.' }
    $productReload=Join-Path $productRun 'reload.json'
    Copy-Item -LiteralPath (Join-Path $productRun 'player0.json.campaign.json') -Destination ($productReload+'.campaign.json')
    $productReloadArgs=@('-batchmode','-screen-width','960','-screen-height','540','-screen-fullscreen','0',
        '-p46-day-role','host','-p46-day-count',1,'-p46-day-slot',0,'-p46-day-port',($Port+1),
        '-p46-day-reload',1,'-p46-day-baseline',('"{0}"' -f (Join-Path $productRun 'player0.json')),
        '-p1-report',('"{0}"' -f $productReload),'-logFile',('"{0}"' -f (Join-Path $productRun 'reload.log')))
    $productReloadProcess=Start-Process -FilePath $productBuild -ArgumentList $productReloadArgs -WindowStyle Hidden -PassThru
    $productProcesses += [pscustomobject]@{Slot='reload';Process=$productReloadProcess;Report=$productReload}
    if (-not $productReloadProcess.WaitForExit(45000)) { throw 'Reload timed out.' }
    if (-not (Test-Path -LiteralPath $productReload)) { throw 'Missing reload report.' }
    $productReloadResult=Get-Content -LiteralPath $productReload -Raw|ConvertFrom-Json
    if (-not $productReloadResult.passed -or $productReloadProcess.ExitCode -ne 0) { throw "Reload failed: $($productReloadResult.errors -join '; ')" }
    Write-Output 'Fresh-process reload/clip playback/duplicate publish PASS.'
} finally {
    foreach ($productEntry in $productProcesses) {
        if (-not $productEntry.Process.HasExited) { Stop-Process -Id $productEntry.Process.Id -Force }
    }
}
