param(
    [string]$BuildPath = './Builds/P1-Integrated/DeepDiveGame-P1.x86_64',
    [int]$Port = 18977
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not [System.IO.Path]::IsPathRooted($BuildPath)) { $BuildPath = Join-Path $root $BuildPath }
if (-not (Test-Path -LiteralPath $BuildPath)) { throw "Build bulunamadi: $BuildPath" }

$run = Join-Path $root ('Logs/P44-boss-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run | Out-Null
$procs = @()

function Start-Game([string]$name, [string]$mode, [int]$count, [int]$port, [switch]$Reload) {
    $report = Join-Path $run ($name + '.json')
    $log = Join-Path $run ($name + '.log')
    $args = @(
        '-batchmode', '-nographics',
        '-p1-integrated', $mode,
        '-p1-count', $count,
        '-p1-port', $port,
        '-p1-report', $report,
        '-logFile', $log
    )
    if ($Reload) { $args += @('-p4-boss-acceptance-reload', '1') }
    else { $args += @('-p4-boss-acceptance', '1') }

    $p = Start-Process -FilePath $BuildPath -ArgumentList $args -PassThru
    return [pscustomobject]@{ Name=$name; Process=$p; Report=$report; Log=$log }
}

try {
    $host = Start-Game 'host' 'host' 2 $Port
    $procs += $host
    Start-Sleep -Seconds 2
    $client = Start-Game 'client1' 'client' 2 $Port
    $procs += $client

    $deadline = (Get-Date).AddSeconds(360)
    while (@($procs | Where-Object { -not $_.Process.HasExited }).Count -gt 0 -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
    }

    $failures = @()
    foreach ($entry in $procs) {
        if (-not $entry.Process.HasExited) {
            $failures += "$($entry.Name): timeout"
            continue
        }
        if (-not (Test-Path -LiteralPath $entry.Report)) {
            $failures += "$($entry.Name): rapor yok"
            continue
        }
        $r = Get-Content -Raw -LiteralPath $entry.Report | ConvertFrom-Json
        Write-Output ([pscustomobject]@{
            Process=$entry.Name
            Passed=$r.passed
            Rumor=$r.bossRumor
            Trace=$r.bossTraceStage
            Arena=$r.bossArenaUnlocked
            Active=$r.bossActiveSeen
            Shot=$r.bossShotSent
            Completed=$r.bossCompletedSeen
            TwoAttackers=$r.bossTwoAttackers
            Saved=$r.bossSaved
            Errors=($r.errors -join '; ')
        })
        if (-not $r.passed -or $entry.Process.ExitCode -ne 0) {
            $failures += "$($entry.Name): $($r.errors -join ', ')"
        }
    }

    if ($failures.Count -eq 0) {
        $campaign = $host.Report + '.campaign.json'
        if (-not (Test-Path -LiteralPath $campaign)) { $failures += 'campaign dosyasi yok' }
        else {
            $reloadReport = Join-Path $run 'host-reload.json'
            Copy-Item -LiteralPath $campaign -Destination ($reloadReport + '.campaign.json')
            $reload = Start-Game 'host-reload' 'host' 1 ($Port + 1) -Reload
            # Start-Game created a different report path; use the path it returns as campaign destination.
            Copy-Item -LiteralPath $campaign -Destination ($reload.Report + '.campaign.json') -Force
            if (-not $reload.Process.WaitForExit(60000)) {
                Stop-Process -Id $reload.Process.Id -Force
                $failures += 'host-reload: timeout'
            } elseif (-not (Test-Path -LiteralPath $reload.Report)) {
                $failures += 'host-reload: rapor yok'
            } else {
                $rr = Get-Content -Raw -LiteralPath $reload.Report | ConvertFrom-Json
                Write-Output ([pscustomobject]@{ Process='host-reload'; Passed=$rr.passed; Reload=$rr.bossReloadPass; Errors=($rr.errors -join '; ') })
                if (-not $rr.passed -or -not $rr.bossReloadPass) { $failures += 'host-reload: progression/completion restore FAIL' }
            }
        }
    }

    Write-Output "P4.4 boss acceptance evidence: $run"
    if ($failures.Count -gt 0) { throw ($failures -join '; ') }
}
finally {
    foreach ($entry in $procs) {
        if (-not $entry.Process.HasExited) { Stop-Process -Id $entry.Process.Id -Force }
    }
}
