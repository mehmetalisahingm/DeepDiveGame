# #326: dedicated 4/4 dock regression on one already-built player.
# All runs use ordinary controller input and fresh campaigns; failed runs remain on disk.
param(
    [Parameter(Mandatory=$true)][string]$BuildPath,
    [ValidateRange(1,10)][int]$Repeats = 3,
    [ValidateRange(1024,65000)][int]$Port = 27077
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$runner = Join-Path $PSScriptRoot 'Test-P46-ContinuousWorld.ps1'
$logs = Join-Path $root 'Logs'
$failures = @()
for ($run = 0; $run -lt $Repeats; $run++) {
    foreach ($reverse in @($false,$true)) {
        $runPort = $Port + 4 * $run + 2 * [int]$reverse
        $before = @(Get-ChildItem -LiteralPath $logs -Directory -Filter 'P46-world-*' -ErrorAction SilentlyContinue | ForEach-Object FullName)
        try {
            & $runner -Players 4 -Port $runPort -BuildPath $BuildPath -DockRush -DockReverse:$reverse
            $newRuns = @(Get-ChildItem -LiteralPath $logs -Directory -Filter 'P46-world-*' | Where-Object { $_.FullName -notin $before })
            if ($newRuns.Count -ne 1) { throw 'Cannot identify one fresh evidence directory; run this suite in isolation.' }
            $evidence = $newRuns[0].FullName
            $stops = @()
            for ($player = 0; $player -lt 4; $player++) {
                $r = Get-Content -LiteralPath (Join-Path $evidence "player$player.json") -Raw | ConvertFrom-Json
                if (-not $r.passed -or -not $r.dockReached -or $r.dockIndex -notin @(0,1,2,3)) { throw "Invalid dock arrival report for player $player" }
                if ($null -eq $r.dockArrivalPosition) { throw "Missing actual arrival position for player $player" }
                $x = 8.55 + ($r.dockIndex % 2) * 0.9
                $z = -4.5 - [Math]::Floor($r.dockIndex / 2.0)
                $dx = $r.dockArrivalPosition.x - $x
                $dz = $r.dockArrivalPosition.z - $z
                if ([Math]::Sqrt($dx*$dx + $dz*$dz) -gt 0.3) { throw "Player $player did not physically reach its claimed stop" }
                $stops += [int]$r.dockIndex
            }
            if (@($stops | Sort-Object -Unique).Count -ne 4) { throw "Duplicate dock stops: $($stops -join ',')" }
            Write-Output "P326 4/4 distinct stops PASS run=$($run+1) reverse=$reverse evidence=$evidence"
        } catch {
            $failures += "run=$($run+1) reverse=$reverse : $($_.Exception.Message)"
            Write-Warning $failures[-1]
        }
    }
}
if ($failures.Count -gt 0) { throw ($failures -join "`n") }
Write-Output "P326 dock arrival suite PASS: $($Repeats*2) real four-process runs. Boarding and separate-PC acceptance are not covered."
