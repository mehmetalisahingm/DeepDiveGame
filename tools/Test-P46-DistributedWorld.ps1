# One real Unity process per physical Windows computer.
# Automated network/geometry evidence, NOT human-controlled gameplay acceptance.
param(
    [ValidateSet(2,4)][int]$Players = 4,
    [ValidateRange(0,3)][int]$Slot,
    [ValidateRange(1024,65535)][int]$Port = 27677,
    [string]$HostIp = '',
    [string]$BuildPath = '',
    [ValidatePattern('^[a-zA-Z0-9._-]{3,60}$')][string]$RunId = 'p46-distributed'
)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Separate Windows PC acceptance runner only.' }
if ($Slot -ge $Players) { throw 'Slot must be lower than Players.' }
if ($Slot -ne 0 -and [string]::IsNullOrWhiteSpace($HostIp)) {
    throw 'Guest requires -HostIp of the actual reachable host computer.'
}
$root = Split-Path -Parent $PSScriptRoot
if (-not $BuildPath) { $BuildPath = Join-Path $root 'Builds/P1-Integrated/DeepDiveGame-P1.exe' }
if (-not (Test-Path -LiteralPath $BuildPath)) { throw "Unity build missing: $BuildPath" }
$exe = (Resolve-Path -LiteralPath $BuildPath).Path
$dir = Join-Path $root ("Logs/P46-distributed-" + $RunId + "-slot" + $Slot)
New-Item -ItemType Directory -Path $dir -Force | Out-Null
$reportPath = Join-Path $dir "player$Slot.json"
$logPath = Join-Path $dir "player$Slot.log"

# Each machine emits the identical-build comparison fingerprint (not just EXE).
$managed = Join-Path (Split-Path $exe -Parent) 'DeepDiveGame-P1_Data/Managed'
$hashInputs = @($exe)
if (Test-Path -LiteralPath $managed) {
    $hashInputs += @(Get-ChildItem -LiteralPath $managed -File -Filter 'DeepDive.*.dll' |
        Sort-Object Name | ForEach-Object { $_.FullName })
}
$hashes = @($hashInputs | ForEach-Object {
    [pscustomobject]@{File = (Split-Path $_ -Leaf); Sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}
})
$role = if ($Slot -eq 0) { 'host' } else { 'client' }
$args = @('-batchmode', '-nographics',
    '-p46-world-role', $role, '-p46-world-count', $Players,
    '-p46-world-slot', $Slot, '-p46-world-port', $Port,
    '-p1-report', ('"{0}"' -f $reportPath), '-logFile', ('"{0}"' -f $logPath))
if ($Slot -gt 0) { $args += @('-p46-world-host', $HostIp) }
$startedUtc = [DateTime]::UtcNow.ToString('o')
Write-Output "Starting $role slot $Slot/$Players on port $Port; run $RunId"
Write-Output "Start host, then guests on their separate computers using the SAME game build."
$p = Start-Process -FilePath $exe -ArgumentList $args -WindowStyle Hidden -PassThru
try {
    if (-not $p.WaitForExit(300000)) { throw "Process timed out; log: $logPath" }
    if (-not (Test-Path -LiteralPath $reportPath)) { throw "No machine report. Log: $logPath" }
    $report = Get-Content -Raw -LiteralPath $reportPath | ConvertFrom-Json
    $manifest = [pscustomobject]@{
        RunId=$RunId; Players=$Players; Slot=$Slot; Role=$role; Port=$Port
        Machine=[Environment]::MachineName
        StartedUtc=$startedUtc; FinishedUtc=[DateTime]::UtcNow.ToString('o')
        ExitCode=$p.ExitCode; Passed=($report.passed -eq $true)
        WorldLoads=$report.worldLoads; Meters=$report.distance
        FirstDay=$report.firstDay; NextDay=$report.morningDay
        SourceReport=(Split-Path $reportPath -Leaf)
        Files=$hashes
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $dir 'machine-evidence.json') -Encoding UTF8
    Write-Output "Evidence folder: $dir"
    if ($p.ExitCode -ne 0 -or $report.passed -ne $true) {
        throw "Unity process failed or route validation failed; see $reportPath and $logPath"
    }
    Write-Output "Local $role automated physical route PASS; this is not human play approval."
} finally {
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force }
}
