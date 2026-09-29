param([int]$Port = 19052)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'Builds/P1-Integrated/DeepDiveGame-P1.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build-P1.ps1 -Integrated calistirin.' }
$run = Join-Path $root ('Logs/P4-media-product-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run | Out-Null
$processes = [System.Collections.Generic.List[object]]::new()

function Start-Role([string]$name, [string]$mode, [bool]$graphics) {
    $report = Join-Path $run ($name + '.json')
    $log = Join-Path $run ($name + '.log')
    $args = @('-batchmode', '-p1-integrated', $mode, '-p1-count', '2', '-p1-port', $Port,
        '-p1-report', ('"{0}"' -f $report), '-logFile', ('"{0}"' -f $log),
        '-p3-record', '1', '-p4-media-product', '1')
    if ($graphics) {
        $args += @('-screen-width', '960', '-screen-height', '540', '-screen-fullscreen', '0')
    } else {
        $args += '-nographics'
    }
    $p = Start-Process -FilePath $exe -ArgumentList $args -WindowStyle Hidden -PassThru
    $processes.Add([pscustomobject]@{Name=$name; Process=$p; Report=$report; Log=$log})
}

try {
    Start-Role 'host' 'host' $true
    Start-Sleep -Seconds 2
    Start-Role 'client1' 'client' $false

    $deadline = (Get-Date).AddSeconds(110)
    while (@($processes | Where-Object {-not $_.Process.HasExited}).Count -gt 0 -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
    }

    $failures = @()
    foreach ($entry in $processes) {
        if (-not $entry.Process.HasExited) { $failures += "$($entry.Name): timeout"; continue }
        if (-not (Test-Path -LiteralPath $entry.Report)) { $failures += "$($entry.Name): rapor yok"; continue }
        $r = Get-Content -Raw -LiteralPath $entry.Report | ConvertFrom-Json
        [pscustomobject]@{Process=$entry.Name; Passed=$r.passed; RecordingStarted=$r.recordingStarted;
            RecordingStopped=$r.recordingStopped; RecordingClaimed=$r.recordingClaimed; RecordingSafe=$r.recordingSafe;
            Errors=($r.errors -join ' | ')}
        if (-not $r.passed -or $entry.Process.ExitCode -ne 0) {
            $failures += "$($entry.Name): $($r.errors -join ', ')"
        }
    }

    $hostLog = Join-Path $run 'host.log'
    $productOk = Test-Path -LiteralPath $hostLog -and
        (Select-String -LiteralPath $hostLog -SimpleMatch 'P4_MEDIA_PRODUCT_OK' -Quiet -ErrorAction SilentlyContinue)
    if (-not $productOk) { $failures += 'host: P4_MEDIA_PRODUCT_OK yok (gercek capture/archive/playback zinciri kanitlanmadi)' }

    Write-Output "P4.2-A media product raporlari: $run"
    if ($failures.Count -gt 0) { throw ($failures -join '; ') }
} finally {
    foreach ($entry in $processes) {
        if (-not $entry.Process.HasExited) { Stop-Process -Id $entry.Process.Id -Force }
    }
}
