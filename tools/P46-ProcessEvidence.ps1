# Terminal evidence for started QA processes. A runner-generated report is always FAIL.
function Get-P46ProcessFailure($Entries) {
    foreach ($entry in $Entries) {
        if (Test-Path -LiteralPath $entry.Report) {
            try { $data = Get-Content -LiteralPath $entry.Report -Raw | ConvertFrom-Json }
            catch { return "Player $($entry.Slot) wrote invalid JSON: $($_.Exception.Message)" }
            if (-not $data.passed) { return "Player $($entry.Slot) failed: $($data.failure)" }
        }
        if ($entry.Process.HasExited) {
            if ($entry.Process.ExitCode -ne 0) { return "Player $($entry.Slot) exited with code $($entry.Process.ExitCode)" }
            if (-not (Test-Path -LiteralPath $entry.Report)) { return "Player $($entry.Slot) exited without a report" }
        }
    }
    return $null
}

function Complete-P46ProcessEvidence($Entries, [string]$Run, [string]$Failure, [int]$Players) {
    $live = @($Entries | Where-Object { -not $_.Process.HasExited })
    foreach ($entry in $live) {
        # The development driver reads only its own report's abort file.
        $abortReason = if ($Failure) { $Failure } else { 'Runner stopped before this process completed' }
        Set-Content -LiteralPath ($entry.Report + '.abort') -Value $abortReason -Encoding utf8
    }
    $grace = [Diagnostics.Stopwatch]::StartNew()
    while (@($live | Where-Object { -not $_.Process.HasExited }).Count -gt 0 -and $grace.Elapsed.TotalSeconds -lt 5) {
        Start-Sleep -Milliseconds 100
    }
    $results = @()
    foreach ($entry in $Entries) {
        if (-not $entry.Process.HasExited) {
            Stop-Process -Id $entry.Process.Id -Force -ErrorAction SilentlyContinue
            $entry.Process.WaitForExit()
        }
        $data = $null
        if (Test-Path -LiteralPath $entry.Report) {
            try { $data = Get-Content -LiteralPath $entry.Report -Raw | ConvertFrom-Json } catch { }
        }
        if ($null -eq $data) {
            $reason = if ($Failure) { $Failure } else { 'Process ended without valid gameplay evidence' }
            if (Test-Path -LiteralPath $entry.Report) {
                Move-Item -LiteralPath $entry.Report -Destination ($entry.Report + '.invalid')
            }
            $data = [ordered]@{passed=$false; runnerGenerated=$true; slot=$entry.Slot; players=$Players;
                failure=$reason; errors=@($reason); processExitCode=$entry.Process.ExitCode}
            $data | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $entry.Report -Encoding utf8
        }
        $results += [ordered]@{slot=$entry.Slot; report=[IO.Path]::GetFileName($entry.Report);
            exitCode=$entry.Process.ExitCode; passed=([bool]$data.passed -and $entry.Process.ExitCode -eq 0);
            runnerGenerated=[bool]$data.runnerGenerated}
    }
    [ordered]@{passed=(-not $Failure -and $results.Count -eq ($Players+1) -and
        @($results | Where-Object { -not $_.passed }).Count -eq 0);
        requestedPlayers=$Players; failure=$Failure; processes=$results} |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Run 'runner.json') -Encoding utf8
}
