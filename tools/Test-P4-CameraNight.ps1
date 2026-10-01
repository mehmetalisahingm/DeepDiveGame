param([int]$Port = 18843)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$build = Join-Path $root 'Builds/P1-Integrated/DeepDiveGame-P1.exe'
if (-not (Test-Path -LiteralPath $build)) { throw 'Build-P1.ps1 -Integrated calistirin.' }

function Test-True($value) { return $value -eq $true }

function Invoke-CameraNightVariant([string]$Variant, [int]$VariantPort) {
    $run = Join-Path $root ('Logs/P4-camera-night-' + $Variant + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $run | Out-Null
    $processes = [System.Collections.Generic.List[object]]::new()

    function Start-Actor([string]$Name, [string]$Mode) {
        $report = Join-Path $run ($Name + '.json')
        $log = Join-Path $run ($Name + '.log')
        $args = @(
            '-batchmode', '-nographics',
            '-p1-integrated', $Mode,
            '-p1-count', '2',
            '-p1-port', $VariantPort,
            '-p1-report', ('"{0}"' -f $report),
            '-p3-record', '1',
            '-p4-camera-smoke', $Variant,
            '-logFile', ('"{0}"' -f $log)
        )
        $process = Start-Process -FilePath $build -ArgumentList $args -WindowStyle Hidden -PassThru
        $processes.Add([pscustomobject]@{ Name=$Name; Process=$process; Report=$report; Log=$log })
    }

    try {
        Start-Actor 'host' 'host'
        Start-Sleep -Seconds 2
        Start-Actor 'client1' 'client'

        $deadline = (Get-Date).AddSeconds(140)
        while (@($processes | Where-Object { -not $_.Process.HasExited }).Count -gt 0 -and (Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 500
        }

        foreach ($entry in $processes) {
            if (-not $entry.Process.HasExited) { try { $entry.Process.Kill() } catch {} }
            if (-not (Test-Path -LiteralPath $entry.Report)) { throw "$Variant/$($entry.Name): rapor yok; $run" }
        }

        $host = Get-Content -Raw -LiteralPath (Join-Path $run 'host.json') | ConvertFrom-Json
        $client = Get-Content -Raw -LiteralPath (Join-Path $run 'client1.json') | ConvertFrom-Json
        $hostLog = Get-Content -Raw -LiteralPath (Join-Path $run 'host.log')

        $commonHost = (Test-True $host.connected) -and (Test-True $host.roster) -and (Test-True $host.ready) -and
            (Test-True $host.prep) -and (Test-True $host.dive) -and (Test-True $host.returned) -and
            (Test-True $host.readyReset) -and (Test-True $host.walk) -and (Test-True $host.swim) -and
            (Test-True $host.collisions) -and (Test-True $host.cameras) -and (Test-True $host.stopped) -and
            (Test-True $host.recordingRoleResolved) -and (Test-True $host.recordingStarted)
        $commonClient = (Test-True $client.connected) -and (Test-True $client.roster) -and (Test-True $client.ready) -and
            (Test-True $client.prep) -and (Test-True $client.dive) -and (Test-True $client.returned) -and
            (Test-True $client.readyReset) -and (Test-True $client.walk) -and (Test-True $client.swim) -and
            (Test-True $client.collisions) -and (Test-True $client.cameras) -and (Test-True $client.stopped) -and
            (Test-True $client.recordingRoleResolved) -and (Test-True $client.recordingRecorder) -and
            (Test-True $client.recordingStarted) -and (Test-True $client.recordingStopped)

        $marker = $hostLog -match ("P4_CAMERA_SMOKE_NIGHT_READY mode=" + $Variant) -and
            $hostLog -match 'provider=True' -and $hostLog -match 'rules=bound' -and
            $hostLog -match 'basicGate=TooDark' -and $hostLog -match 'proGate=Accepted'

        if ($Variant -eq 'basic') {
            # The normal -Record result is intentionally false here: the real Basic take starts/stops,
            # but every otherwise-valid night frame is TooDark, so no payable claim can exist.
            $ok = $commonHost -and $commonClient -and $marker -and
                (-not (Test-True $host.recordingClaimed)) -and ([double]$host.recordingValidSeconds -lt 0.1) -and
                ([int]$host.recordingQuality -eq 0)
        }
        else {
            # Same real scene/day/subject, but the real catalog purchase made the recorder Professional.
            # Existing -Record acceptance must now finish normally and bank a payable take.
            $ok = $commonHost -and $commonClient -and $marker -and
                (Test-True $host.passed) -and (Test-True $client.passed) -and
                (Test-True $host.recordingClaimed) -and ([double]$host.recordingValidSeconds -gt 0.5) -and
                ([int]$host.recordingQuality -gt 0) -and $hostLog -match 'tier=Professional'
        }

        [pscustomobject]@{
            Variant=$Variant
            Passed=$ok
            HostPassed=$host.passed
            ClientPassed=$client.passed
            Claimed=$host.recordingClaimed
            ValidSeconds=$host.recordingValidSeconds
            Quality=$host.recordingQuality
            Evidence=$run
        }

        if (-not $ok) { throw "P4 camera night smoke FAIL ($Variant): $run" }
    }
    finally {
        foreach ($entry in $processes) {
            if ($entry.Process -and -not $entry.Process.HasExited) { try { $entry.Process.Kill() } catch {} }
        }
    }
}

Invoke-CameraNightVariant 'basic' $Port
Invoke-CameraNightVariant 'professional' ($Port + 1)
Write-Host 'P4_CAMERA_NIGHT_SMOKE_OK basic=TooDark professional=Accepted'
