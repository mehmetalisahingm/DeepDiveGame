# Run after copying each participant's evidence directory to the same review PC.
# Machine names distinguish reported PCs; this is not independent proof of human gameplay.
param([Parameter(Mandatory=$true)][string[]]$EvidenceDirs)
$ErrorActionPreference = 'Stop'
if ($EvidenceDirs.Count -notin @(2,4)) { throw 'Supply 2 or 4 distinct computer evidence folders.' }
$rows = @($EvidenceDirs | ForEach-Object {
    $root = (Resolve-Path -LiteralPath $_).Path
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $root 'machine-evidence.json') | ConvertFrom-Json
    $game = Get-Content -Raw -LiteralPath (Join-Path $root $manifest.SourceReport) | ConvertFrom-Json
    [pscustomobject]@{Manifest=$manifest;Game=$game;Folder=$root}
})
$expected = $rows[0].Manifest.Players
if ($expected -ne $rows.Count) { throw "Expected $expected different machines, received $($rows.Count)." }
$reference = $rows[0].Manifest
$referenceFiles = @($reference.Files | Sort-Object File | ForEach-Object { "$($_.File):$($_.Sha256)" }) -join '|'
$slots = @($rows | ForEach-Object { [int]$_.Manifest.Slot } | Sort-Object)
for ($i=0; $i -lt $expected; $i++) { if ($slots[$i] -ne $i) { throw "Missing/duplicate player slot: expected 0..$($expected-1)" } }
$machines = @($rows | ForEach-Object { $_.Manifest.Machine } | Select-Object -Unique)
if ($machines.Count -ne $expected) { throw 'Reports must name distinct machines (real PCs); identical machine names are not accepted.' }
foreach ($row in $rows) {
    $m = $row.Manifest; $g = $row.Game
    $got = @($m.Files | Sort-Object File | ForEach-Object { "$($_.File):$($_.Sha256)" }) -join '|'
    if ($m.RunId -ne $reference.RunId -or $m.Port -ne $reference.Port -or $m.Players -ne $expected) {
        throw "Mismatch in common run/port/count: $($row.Folder)"
    }
    if ($m.Slot -eq 0 -and $m.Role -ne 'host' -or $m.Slot -gt 0 -and $m.Role -ne 'client') {
        throw "Wrong role for slot $($m.Slot)"
    }
    if ($got -ne $referenceFiles) { throw "Build fingerprint mismatch on slot $($m.Slot)" }
    if ($m.ExitCode -ne 0 -or $m.Passed -ne $true -or $g.passed -ne $true -or
        $g.worldLoads -ne 1 -or $g.players -ne $expected -or
        $g.distance -le 120 -or $g.morningDay -ne ($g.firstDay + 1) -or
        @($g.errors).Count -ne 0) {
        throw "Real-route contract failed for slot $($m.Slot): $($row.Folder)"
    }
}
$rows | Sort-Object { $_.Manifest.Slot } | ForEach-Object {
    [pscustomobject]@{Slot=$_.Manifest.Slot;Machine=$_.Manifest.Machine;Distance=$_.Game.distance;
        Day=$_.Game.morningDay;Passed=$_.Game.passed;Folder=$_.Folder}
}
Write-Output 'AUTOMATED real separate-computer network route verified; human control, 3 consecutive days, 00:00 and host-loss remain UNTESTED by this harness.'
