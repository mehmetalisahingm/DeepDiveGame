param([ValidateSet(1,2,4)][int]$Players = 4, [int]$Port = 18777, [switch]$Capture, [switch]$Hunt, [switch]$Record, [switch]$Event, [switch]$Town, [switch]$Boat, [switch]$Trip, [switch]$Day, [switch]$HomeSleep, [switch]$Storage, [switch]$Explore, [switch]$Media, [switch]$Acceptance, [switch]$Fleet, [switch]$Deep, [switch]$RoleEffects, [switch]$WorldConditions, [switch]$Unified, [string]$BuildPath = '', [string]$TripVehicle = '', [string]$TripRoute = '', [string]$CampaignSeed = '')
$ErrorActionPreference = 'Stop'
$p1Root = Split-Path -Parent $PSScriptRoot
$p1IsWindows = $env:OS -eq 'Windows_NT'
if ($BuildPath) { $p1Build = $BuildPath }
elseif ($p1IsWindows) { $p1Build = Join-Path $p1Root 'Builds/P1-Integrated/DeepDiveGame-P1.exe' }
else { $p1Build = Join-Path $p1Root 'Builds/P1-Integrated/DeepDiveGame-P1.x86_64' }
if (-not (Test-Path -LiteralPath $p1Build)) { throw "Integrated build bulunamadi: $p1Build" }
$p1Run = Join-Path $p1Root ('Logs/P1-integrated-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $Players)
New-Item -ItemType Directory -Path $p1Run | Out-Null
if ($CampaignSeed) {
    if (-not [System.IO.Path]::IsPathRooted($CampaignSeed)) { $CampaignSeed = Join-Path $p1Root $CampaignSeed }
    if (-not (Test-Path -LiteralPath $CampaignSeed)) { throw "Campaign seed bulunamadi: $CampaignSeed" }
    Copy-Item -LiteralPath $CampaignSeed -Destination (Join-Path $p1Run 'host.json.campaign.json') -Force
}
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
    if ($Acceptance -and $Name -eq 'host' -and -not $Capture) {
        # Real clip capture needs a real graphics device (`-nographics` disables it): the acceptance host renders, like Test-P4-MediaProduct.ps1.
        $p1Args += @('-screen-width', '960', '-screen-height', '540', '-screen-fullscreen', '0')
    } elseif ($Capture -and $Name -eq 'host') {
        $capturePath = Join-Path $p1Run 'room.png'
        $p1Args += @('-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
        if ($Unified) { $p1Args += @('-p3-unified-screenshot', ('"{0}"' -f $capturePath)) }
        else { $p1Args += @('-p1-screenshot', ('"{0}"' -f $capturePath)) }
    } else {
        if ($p1IsWindows) { $p1Args += '-nographics' }
        else { $p1Args += @('-screen-width', '320', '-screen-height', '200', '-screen-fullscreen', '0') }
    }
    if (-not $Unified) {
        if ($Reason) { $p1Args += @('-p1-reason', $Reason) }
        if ($Hunt) { $p1Args += @('-p2-hunt', '1') }
        if ($Record) { $p1Args += @('-p3-record', '1') }
        if ($Event) { $p1Args += @('-p3-event', '1') }
        if ($Town) { $p1Args += @('-p3-town', '1') }
        if ($Boat) { $p1Args += @('-p3-boat', '1') }
        if ($Trip) {
            $p1Args += @('-p3-trip', '1')
            if ($TripVehicle) { $p1Args += @('-p4-trip-vehicle', $TripVehicle) }
            if ($TripRoute) { $p1Args += @('-p4-trip-route', $TripRoute) }
        }
        if ($Day) { $p1Args += @('-p4-day', '1') }
        if ($HomeSleep) { $p1Args += @('-p4-home', '1') }
        if ($Explore) { $p1Args += @('-p4-explore', '1') }
        if ($Media) { $p1Args += @('-p4-media', '1') }
        if ($Deep) { $p1Args += @('-p4-deep', '1') }
        if ($RoleEffects) { $p1Args += @('-p45-role-effects', '1') }
        if ($WorldConditions) { $p1Args += @('-p45-world', '1') }
        if ($Fleet) { $p1Args += @('-p4-fleet', '1') }   # #109: real walk to shop + harbor vendor, real purchase/select RPCs, host reload
        if ($Acceptance) { $p1Args += @('-p4-acceptance', '1', '-p3-record', '1', '-p4-media-product', '1') }   # #106: no fixture, real capture -> PC -> channel -> day -> reload
        if ($Storage) { $p1Args += @('-p4-storage', '1') }
    }
    if ($p1IsWindows) { $p1Process = Start-Process -FilePath $p1Build -ArgumentList $p1Args -WindowStyle Hidden -PassThru }
    else { $p1Process = Start-Process -FilePath $p1Build -ArgumentList $p1Args -PassThru }
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
function Test-True($Value) { return $Value -eq $true }
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
    $seconds = if ($Unified) { 360 } elseif ($RoleEffects -or $WorldConditions) { 105 } elseif ($Town) { 105 } elseif ($Event) { 150 } elseif ($Storage) { 150 } elseif ($Deep) { 230 } elseif ($Fleet) { 230 } elseif ($Acceptance) { 230 } elseif ($Media) { 150 } elseif ($Explore) { 95 } elseif ($HomeSleep) { 150 } elseif ($Day) { 125 } elseif ($Trip -and $TripVehicle) { 310 } elseif ($Trip) { 175 } elseif ($Boat) { 95 } elseif ($Record) { 130 } elseif ($Hunt) { 90 } else { 60 }
    $p1Deadline = (Get-Date).AddSeconds($seconds)
    while (@($p1Processes | Where-Object {-not $_.Process.HasExited}).Count -gt 0 -and (Get-Date) -lt $p1Deadline) { Start-Sleep -Milliseconds 500 }

    $p1Failures = @()
    $p1Parsed = @()
    foreach ($p1Entry in $p1Processes) {
        if (-not (Test-Path -LiteralPath $p1Entry.Report)) { $p1Failures += "$($p1Entry.Name): rapor yok"; continue }
        $p1Result = Get-Content -Raw -LiteralPath $p1Entry.Report | ConvertFrom-Json
        $p1Parsed += [pscustomobject]@{Entry=$p1Entry; Result=$p1Result}
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

    if ($Unified -and $p1Parsed.Count -eq $Players) {
        $hostEvidence = @($p1Parsed | Where-Object {$_.Entry.Name -eq 'host'})
        if ($hostEvidence.Count -ne 1) { $p1Failures += 'unified: host raporu tek degil' }
        else {
            $h = $hostEvidence[0].Result
            $hostOk = (Test-True $h.fixtureBalanceSeeded) -and (Test-True $h.recordingClaimed) -and
                (Test-True $h.inventoryAdded) -and (Test-True $h.safeReturned) -and
                (Test-True $h.boatHullFound) -and (Test-True $h.boatFuelTankFound) -and
                (Test-True $h.boatDuplicateRejected) -and (Test-True $h.tripUniqueSeats) -and
                (Test-True $h.servicesCleared) -and (Test-True $h.saveReload) -and
                $h.boatPartsSequence.Count -ge 4 -and $h.boatPartsSequence[-1] -eq 3
            if (-not $hostOk) { $p1Failures += 'unified host evidence eksik' }
        }

        $actors = @($p1Parsed | Where-Object {$_.Entry.Name -ne 'host' -and $_.Result.cameraBought})
        if ($Players -eq 1) { $actors = $hostEvidence }
        if ($actors.Count -ne 1) { $p1Failures += "unified: tam bir actor bekleniyordu, bulundu=$($actors.Count)" }
        else {
            $a = $actors[0].Result
            $actorOk = (Test-True $a.cameraBought) -and (Test-True $a.cameraDuplicateRejected) -and
                (Test-True $a.recordingStarted) -and (Test-True $a.recordingStopped) -and
                (Test-True $a.catchObserved) -and (Test-True $a.catchDespawned) -and
                (Test-True $a.boatEngineFound) -and (Test-True $a.recordingTurnedIn) -and
                (Test-True $a.recordingDuplicateNoPay) -and (Test-True $a.fishSold) -and
                (Test-True $a.fishDuplicateNoPay)
            if (-not $actorOk) { $p1Failures += 'unified actor evidence eksik' }
        }

        $markerEvidence = @($p1Parsed | Where-Object {$_.Result.tripReturnMarkerSeen -and $_.Result.tripReboarded})
        if ($markerEvidence.Count -lt 1) { $p1Failures += 'unified: anchor return marker + reboard kaniti yok' }

        foreach ($row in $p1Parsed) {
            $r = $row.Result
            $commonOk = (Test-True $r.boatRepaired) -and (Test-True $r.boatPartsHidden) -and
                (Test-True $r.tripBoarded) -and (Test-True $r.tripDuplicateBoardHeld) -and
                (Test-True $r.tripMapDockedAtDock) -and (Test-True $r.tripMapUnderwayMoved) -and
                (Test-True $r.tripMapAnchoredAtAnchor) -and (Test-True $r.tripDockedEmpty) -and
                (Test-True $r.tripDone) -and $r.tripMapPlayersMax -ge $Players -and
                (($r.tripPhases -join ',') -eq 'Docked,Outbound,Anchored,Inbound,Docked')
            if (-not $commonOk) { $p1Failures += "unified common evidence eksik: $($row.Entry.Name)" }
        }
    }

    if (($Day -or $Media -or $Acceptance -or $Fleet -or $Deep) -and $p1Failures.Count -eq 0) {
        # A real second launch of the host on the campaign file the first run left behind.
        $p1Campaign = Join-Path $p1Run 'host.json.campaign.json'
        $p1Reload = Join-Path $p1Run 'host-reload.json'
        Copy-Item -LiteralPath $p1Campaign -Destination ($p1Reload + '.campaign.json')
        $p1ReloadGraphics = if ($Acceptance) { @('-screen-width', '960', '-screen-height', '540', '-screen-fullscreen', '0') } elseif ($p1IsWindows) { @('-nographics') } else { @('-screen-width', '320', '-screen-height', '200', '-screen-fullscreen', '0') }
        $p1ReloadArgs = (@('-batchmode') + $p1ReloadGraphics + @(
            '-p1-integrated', 'host', '-p1-count', 1, '-p1-port', ($Port + 1), '-p1-report', ('"{0}"' -f $p1Reload),
            '-logFile', ('"{0}"' -f (Join-Path $p1Run 'host-reload.log')), $(if ($Deep) { '-p4-deep-reload' } elseif ($Fleet) { '-p4-fleet-reload' } elseif ($Acceptance) { '-p4-acceptance-reload' } elseif ($Media) { '-p4-media-reload' } else { '-p4-day-reload' }), '1'))
        if ($p1IsWindows) { $p1ReloadProcess = Start-Process -FilePath $p1Build -WindowStyle Hidden -PassThru -ArgumentList $p1ReloadArgs }
        else { $p1ReloadProcess = Start-Process -FilePath $p1Build -PassThru -ArgumentList $p1ReloadArgs }
        $p1ReloadProcess.WaitForExit(60000) | Out-Null
        if (-not (Test-Path -LiteralPath $p1Reload)) { $p1Failures += 'host-reload: rapor yok' }
        else {
            $p1ReloadResult = Get-Content -Raw -LiteralPath $p1Reload | ConvertFrom-Json
            [pscustomobject]@{Process='host-reload'; Passed=$p1ReloadResult.passed; Day=$p1ReloadResult.dayReloadNumber; History=$p1ReloadResult.dayReloadHistory; Minute=$p1ReloadResult.dayReloadMinute}
            if (-not $p1ReloadResult.passed) { $p1Failures += "host-reload: $($p1ReloadResult.errors -join ', ')" }
            if ($Acceptance) {
                # The reload must hand back exactly what the first run left: same real clip (id/recording/hash/size), same post and result, same balance.
                $p1First = ($p1Parsed | Where-Object { $_.Entry.Name -eq 'host' } | Select-Object -First 1).Result
                $p1Same = ($p1ReloadResult.acceptReloadClipId -eq $p1First.acceptClipId) -and ($p1ReloadResult.acceptReloadRecordingId -eq $p1First.acceptRecordingId) -and
                    ($p1ReloadResult.acceptReloadHash -eq $p1First.acceptHash) -and ($p1ReloadResult.acceptReloadBytes -eq $p1First.acceptBytes) -and
                    ($p1ReloadResult.acceptReloadBalance -eq $p1First.acceptBalance) -and ($p1ReloadResult.acceptReloadIncome -eq $p1First.acceptIncome) -and
                    ($p1ReloadResult.acceptReloadDay -eq $p1First.acceptDayAfter) -and ($p1ReloadResult.acceptReloadExploreObs -eq $p1First.acceptExploreObs)
                [pscustomobject]@{Process='acceptance-reload-vs-first'; Same=$p1Same; Clip=$p1ReloadResult.acceptReloadClipId; Bytes=$p1ReloadResult.acceptReloadBytes;
                    Balance="$($p1First.acceptBalance) -> $($p1ReloadResult.acceptReloadBalance)"; Income="$($p1First.acceptIncome) -> $($p1ReloadResult.acceptReloadIncome)";
                    Day="$($p1First.acceptDayAfter) -> $($p1ReloadResult.acceptReloadDay)"; ExploreObs="$($p1First.acceptExploreObs) -> $($p1ReloadResult.acceptReloadExploreObs)"; Playable=$p1ReloadResult.acceptReloadPlayable}
                if (-not $p1Same) { $p1Failures += 'acceptance: reload farkli klip/yayin/bakiye/gun dondurdu' }
                # The clip file itself: its real bytes must match the manifest's size and SHA-256 (the smoke only proves the manifest + playability).
                $p1ClipFile = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE 'AppData/LocalLow') -Recurse -Filter ($p1First.acceptClipId + '.ddclip') -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
                if ($null -eq $p1ClipFile) { $p1Failures += 'acceptance: klip dosyasi bulunamadi' }
                else {
                    $p1FileHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $p1ClipFile.FullName).Hash.ToLowerInvariant()
                    [pscustomobject]@{Process='acceptance-clip-file'; Bytes=$p1ClipFile.Length; ManifestBytes=$p1First.acceptBytes; HashMatches=($p1FileHash -eq $p1First.acceptHash.ToLowerInvariant()); File=$p1ClipFile.FullName}
                    if ($p1ClipFile.Length -ne $p1First.acceptBytes -or $p1FileHash -ne $p1First.acceptHash.ToLowerInvariant()) { $p1Failures += 'acceptance: klip dosyasi manifest boyutu/hash ile uyusmuyor' }
                }
                $p1HostLog = Join-Path $p1Run 'host.log'
                if (-not (Select-String -LiteralPath $p1HostLog -SimpleMatch 'P4_MEDIA_PRODUCT_OK' -Quiet -ErrorAction SilentlyContinue)) { $p1Failures += 'acceptance: host P4_MEDIA_PRODUCT_OK yok' }
            }
        }
        if (-not $p1ReloadProcess.HasExited) { Stop-Process -Id $p1ReloadProcess.Id -Force }
    }

    Write-Output "Yerel entegre test raporlari: $p1Run"
    if ($p1Failures.Count -gt 0) { throw ($p1Failures -join '; ') }
} finally {
    foreach ($p1Entry in $p1Processes) { if (-not $p1Entry.Process.HasExited) { Stop-Process -Id $p1Entry.Process.Id -Force } }
}
