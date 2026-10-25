param([ValidateSet(1,2,4)][int]$Players=1, [int]$Port=24777, [string]$BuildPath='', [switch]$RepairOnly, [switch]$RepairInterruptions, [switch]$RepairPurchase, [switch]$RepairVideo, [switch]$HuntFirst, [switch]$Mute, [switch]$AllowConcurrent, [ValidateRange(-1,3)][int]$FaultSlot=-1)
$ErrorActionPreference='Stop'
if ($RepairInterruptions -and (-not $RepairOnly -or $Players -ne 4)) { throw 'RepairInterruptions requires RepairOnly and four processes.' }
if ($RepairPurchase -and (-not $RepairOnly -or $RepairInterruptions)) { throw 'RepairPurchase requires RepairOnly and cannot combine with RepairInterruptions.' }
if ($RepairVideo -and -not $RepairPurchase) { throw 'RepairVideo requires RepairPurchase (host repairs while guest observes).' }
if ($HuntFirst -and $RepairOnly) { throw '-HuntFirst and -RepairOnly are exclusive' }
$productRoot=Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'P46-ProcessEvidence.ps1')
if ($FaultSlot -ge $Players) { throw 'FaultSlot must identify a started player.' }
$productIsWindows=$env:OS -eq 'Windows_NT'
if ($BuildPath) { $productBuild=$BuildPath }
elseif ($productIsWindows) { $productBuild=Join-Path $productRoot 'Builds/P1-Integrated/DeepDiveGame-P1.exe' }
else { $productBuild=Join-Path $productRoot 'Builds/P1-Integrated/DeepDiveGame-P1.x86_64' }
if (-not (Test-Path -LiteralPath $productBuild)) { throw "Integrated build not found: $productBuild" }
$productLease=$null
if (-not $AllowConcurrent) {
    New-Item -ItemType Directory -Path (Join-Path $productRoot 'Logs') -Force | Out-Null
    try { $productLease=[IO.File]::Open((Join-Path $productRoot 'Logs/product-day-active.lock'), 'OpenOrCreate', 'ReadWrite', 'None') }
    catch { throw 'Another product-day run is active. Wait for it to finish, or explicitly use -AllowConcurrent for a load experiment.' }
}
$productRun=Join-Path $productRoot ('Logs/P46-product-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $Players)
$productProcesses=@()
$productFailure=$null
try {
    New-Item -ItemType Directory -Path $productRun | Out-Null
    for ($productSlot=0;$productSlot -lt $Players;$productSlot++) {
        $productReport=Join-Path $productRun "player$productSlot.json"
        $productLog=Join-Path $productRun "player$productSlot.log"
        $productArgs=@('-batchmode','-p46-day-role',$(if ($productSlot -eq 0) {'host'} else {'client'}),
            '-p46-day-count',$Players,'-p46-day-slot',$productSlot,'-p46-day-port',$Port,
            '-p1-report',('"{0}"' -f $productReport),'-logFile',('"{0}"' -f $productLog))
        if ($RepairOnly) { $productArgs+=@('-p187-repair-only','1') }
        if ($RepairInterruptions) { $productArgs+=@('-p187-interruptions','1') }
        if ($RepairPurchase) { $productArgs+=@('-p187-purchase','1') }
        if ($RepairVideo) { $productArgs+=@('-p187-video','1') }
        if ($HuntFirst) { $productArgs+=@('-p188-hunt-first','1') }
        if ($Mute) { $productArgs+=@('-p46-mute','1') }
        if ($FaultSlot -ge 0) { $productArgs+=@('-p46-fault-slot',$FaultSlot) }
        if ($productSlot -eq 0) { $productArgs+=@('-screen-width','960','-screen-height','540','-screen-fullscreen','0') }
        elseif ($productIsWindows -and -not $RepairVideo) { $productArgs+='-nographics' }
        else { $productArgs+=@('-screen-width','320','-screen-height','200','-screen-fullscreen','0') }
        if ($productIsWindows) {
            $productProcess=Start-Process -FilePath $productBuild -ArgumentList $productArgs -WindowStyle Hidden -PassThru
        } else {
            $productProcess=Start-Process -FilePath $productBuild -ArgumentList $productArgs -PassThru
        }
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
        $productFailure=Get-P46ProcessFailure $productProcesses
        if ($productFailure) { throw $productFailure }
        Start-Sleep -Milliseconds 500
    }
    if (@($productProcesses | Where-Object {-not $_.Process.HasExited}).Count -gt 0) { throw 'Product-day process deadline exceeded.' }
    $productFailed=@()
    $repairRecovered=$false
    foreach ($productEntry in $productProcesses) {
        if (-not (Test-Path -LiteralPath $productEntry.Report)) { $productFailed+="Player $($productEntry.Slot) missing report"; continue }
        $productResult=Get-Content -LiteralPath $productEntry.Report -Raw|ConvertFrom-Json
        $repairRecovered=$repairRecovered -or $productResult.cancelRecovered
        [pscustomobject]@{Player=$productEntry.Slot;Passed=$productResult.passed;Step=$productResult.step;Dives=$productResult.diveCount;
            FishSold=$productResult.soldCount;PartsAtFirstSale=$productResult.partsAtFirstSale;Day=$productResult.day;Income=$productResult.income;Error=$productResult.failure}
        if (-not $productResult.passed -or $productEntry.Process.ExitCode -ne 0) { $productFailed+="Player $($productEntry.Slot): $($productResult.errors -join '; ')" }
    }
    Write-Output "Evidence: $productRun"
    if ($productFailed.Count -gt 0) { throw ($productFailed -join "`n") }
    $productCheckpoint=Get-Content -LiteralPath (Join-Path $productRun 'player0.json.campaign.json') -Raw|ConvertFrom-Json
    if ($RepairOnly) {
        if ((-not $RepairInterruptions -and -not $repairRecovered) -or @($productCheckpoint.BoatPartIds).Count -ne 3 -or
            @($productCheckpoint.UnusedBoatPartIds).Count -ne 0 -or $productCheckpoint.SchemaVersion -ne 9) {
            throw 'Physical repair did not recover cancelled material or persist exactly three completed parts.'
        }
        Write-Output 'Physical repair owner-input/cancel recovery/mirror/disk PASS.'
    }
    if (-not $RepairOnly -and ($productCheckpoint.Day.DayNumber -ne 2 -or @($productCheckpoint.Day.ClosedCloseIds).Count -ne 1 -or
        @($productCheckpoint.Day.SummaryHistory).Count -ne 1 -or @($productCheckpoint.SoldCaptureIds).Count -ne 2 -or
        @($productCheckpoint.BoatPartIds).Count -ne 3 -or @($productCheckpoint.Media.Publications).Count -ne 1 -or
        @($productCheckpoint.ChannelSettleIds).Count -ne 1)) { throw 'Campaign does not contain exactly one settled day/publication and the earned repair/catches.' }
    $productReload=Join-Path $productRun 'reload.json'
    Copy-Item -LiteralPath (Join-Path $productRun 'player0.json.campaign.json') -Destination ($productReload+'.campaign.json')
    $productReloadArgs=@('-batchmode','-screen-width','960','-screen-height','540','-screen-fullscreen','0',
        '-p46-day-role','host','-p46-day-count',1,'-p46-day-slot',0,'-p46-day-port',($Port+1),
        '-p46-day-reload',1,'-p46-day-baseline',('"{0}"' -f (Join-Path $productRun 'player0.json')),
        '-p1-report',('"{0}"' -f $productReload),'-logFile',('"{0}"' -f (Join-Path $productRun 'reload.log')))
    if ($RepairOnly) { $productReloadArgs+=@('-p187-repair-only','1') }
    if ($Mute) { $productReloadArgs+=@('-p46-mute','1') }
    if ($productIsWindows) {
        $productReloadProcess=Start-Process -FilePath $productBuild -ArgumentList $productReloadArgs -WindowStyle Hidden -PassThru
    } else {
        $productReloadProcess=Start-Process -FilePath $productBuild -ArgumentList $productReloadArgs -PassThru
    }
    $productProcesses += [pscustomobject]@{Slot='reload';Process=$productReloadProcess;Report=$productReload}
    if (-not $productReloadProcess.WaitForExit(45000)) { throw 'Reload timed out.' }
    if (-not (Test-Path -LiteralPath $productReload)) { throw 'Missing reload report.' }
    $productReloadResult=Get-Content -LiteralPath $productReload -Raw|ConvertFrom-Json
    if (-not $productReloadResult.passed -or $productReloadProcess.ExitCode -ne 0) { throw "Reload failed: $($productReloadResult.errors -join '; ')" }
    if ($RepairOnly) { Write-Output 'Fresh-process repaired campaign/material/balance/sale/day reload PASS.' }
    else { Write-Output 'Fresh-process reload/clip playback/duplicate publish PASS.' }
} catch {
    $productFailure=$_.Exception.Message
    throw
} finally {
    try { Complete-P46ProcessEvidence $productProcesses $productRun $productFailure $Players }
    finally { if ($null -ne $productLease) { $productLease.Dispose() } }
}
