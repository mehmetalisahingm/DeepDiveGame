# #218 evidence: real Windows processes (host with a graphics device, clients without) of the integrated build, and which fish
# species each one sees in the live scene; the host also writes a near and a far picture of one real fish per species.
# Observation only: it reports what the probe saw and judges nothing. Needs tools\Build-P1.ps1 -Integrated first.
param([ValidateSet(1,2,4)][int]$Players = 2, [int]$Port = 18911)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'Builds/P1-Integrated/DeepDiveGame-P1.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build-P1.ps1 -Integrated calistirin.' }
$run = Join-Path $root ('Logs/P411-species-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $Players)
New-Item -ItemType Directory -Path $run | Out-Null
$shots = Join-Path $run 'species'
$processes = [System.Collections.Generic.List[object]]::new()

function Start-Role([string]$name, [string]$mode, [bool]$graphics) {
    $report = Join-Path $run ($name + '.json'); $log = Join-Path $run ($name + '.log')
    $args = @('-batchmode', '-p1-integrated', $mode, '-p1-count', $Players, '-p1-port', $Port,
        '-p1-report', ('"{0}"' -f $report), '-logFile', ('"{0}"' -f $log), '-p411-species', ('"{0}"' -f $shots))
    if ($graphics) { $args += @('-screen-width', '960', '-screen-height', '540', '-screen-fullscreen', '0') } else { $args += '-nographics' }
    $p = Start-Process -FilePath $exe -ArgumentList $args -WindowStyle Hidden -PassThru
    $processes.Add([pscustomobject]@{Name=$name; Process=$p; Log=$log})
}

try {
    Start-Role 'host' 'host' $true
    Start-Sleep -Seconds 2
    for ($i = 1; $i -lt $Players; $i++) { Start-Role "client$i" 'client' $false }
    $deadline = (Get-Date).AddSeconds(150)
    while ((Get-Date) -lt $deadline) {
        $done = @(Get-ChildItem -LiteralPath $shots -Filter 'species-*.json' -ErrorAction SilentlyContinue).Count
        if ($done -ge $Players) { break }
        Start-Sleep -Milliseconds 500
    }
    Start-Sleep -Seconds 3   # the host's pictures are written after its json
}
finally {
    foreach ($e in $processes) { if (-not $e.Process.HasExited) { Stop-Process -Id $e.Process.Id -Force -ErrorAction SilentlyContinue } }
}

Write-Host "Run: $run"
foreach ($f in Get-ChildItem -LiteralPath $shots -Filter 'species-*.json' -ErrorAction SilentlyContinue) {
    $r = Get-Content -Raw -LiteralPath $f.FullName | ConvertFrom-Json
    $line = ($r.species | ForEach-Object { '{0}={1}/{2}vis' -f $_.id, $_.spawned, $_.withVisibleRenderer }) -join ' '
    Write-Host ("{0} pid={1} graphics={2} spawnedFish={3} : {4}" -f $r.role, $r.pid, $r.graphics, $r.spawnedFish, $line)
}
Write-Host ("processes with a species report: {0} of {1}" -f @(Get-ChildItem -LiteralPath $shots -Filter 'species-*.json' -ErrorAction SilentlyContinue).Count, $Players)
Write-Host ("pictures: {0}" -f @(Get-ChildItem -LiteralPath $shots -Filter '*.png' -ErrorAction SilentlyContinue).Count)
