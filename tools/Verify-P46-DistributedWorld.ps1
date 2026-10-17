# Run after copying each participant's evidence directory to the same review PC.
# Machine names distinguish reported PCs; this is not independent proof of human gameplay.
# Default: every slot on a different machine. -MinMachines 2 accepts only 2+2 technical evidence.
param(
    [Parameter(Mandatory=$true)][string[]]$EvidenceDirs,
    [int]$MinMachines = 0
)
$ErrorActionPreference = 'Stop'
if ($EvidenceDirs.Count -notin @(2,4)) { throw 'Supply 2 or 4 distinct computer evidence folders.' }
$rows = @($EvidenceDirs | ForEach-Object {
    $root = (Resolve-Path -LiteralPath $_).Path
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $root 'machine-evidence.json') | ConvertFrom-Json
    $game = Get-Content -Raw -LiteralPath (Join-Path $root $manifest.SourceReport) | ConvertFrom-Json
    # Evidence written before the 2+2 mode existed is one process per computer.
    $mode = if ($manifest.Mode) { [string]$manifest.Mode } else { 'separate-pc' }
    $perMachine = if ($manifest.ProcessesPerMachine) { [int]$manifest.ProcessesPerMachine } else { 1 }
    [pscustomobject]@{Manifest=$manifest;Game=$game;Folder=$root;Mode=$mode;PerMachine=$perMachine}
})
$modes = @($rows | ForEach-Object { $_.Mode } | Select-Object -Unique)
if ($modes.Count -ne 1) { throw "Mixed evidence modes ($($modes -join ', ')); every folder must come from the same mode." }
$mode = $modes[0]
if ($mode -notin @('separate-pc','two-per-pc')) { throw "Unknown evidence mode: $mode" }
$expected = $rows[0].Manifest.Players
if ($expected -ne $rows.Count) { throw "Expected $expected player folders, received $($rows.Count)." }
if ($MinMachines -eq 0) { $MinMachines = $expected }
if ($MinMachines -lt 2 -or $MinMachines -gt $expected) { throw "-MinMachines must be between 2 and $expected." }
$reference = $rows[0].Manifest
$referenceFiles = @($reference.Files | Sort-Object File | ForEach-Object { "$($_.File):$($_.Sha256)" }) -join '|'
$slots = @($rows | ForEach-Object { [int]$_.Manifest.Slot } | Sort-Object)
for ($i=0; $i -lt $expected; $i++) { if ($slots[$i] -ne $i) { throw "Missing/duplicate player slot: expected 0..$($expected-1)" } }
$machines = @($rows | ForEach-Object { $_.Manifest.Machine } | Select-Object -Unique)
if ($MinMachines -eq $expected) {
    if ($mode -ne 'separate-pc') { throw "Evidence mode is $mode; verify 2+2 evidence with -MinMachines 2." }
    if ($machines.Count -ne $expected) { throw 'Reports must name distinct machines (real PCs); identical machine names are not accepted.' }
} else {
    if ($mode -ne 'two-per-pc') { throw "-MinMachines $MinMachines accepts only two-per-pc evidence; received $mode." }
    $perMachine = @($rows | ForEach-Object { $_.PerMachine } | Select-Object -Unique)
    if ($perMachine.Count -ne 1 -or $perMachine[0] -ne 2) { throw 'All two-per-pc manifests must report ProcessesPerMachine 2.' }
    if ($machines.Count -lt $MinMachines) { throw "Need at least $MinMachines distinct machines, received $($machines.Count)." }
    foreach ($group in @($rows | Group-Object { $_.Manifest.Machine })) {
        if ($group.Count -gt $perMachine[0]) { throw "Machine $($group.Name) ran $($group.Count) slots; limit is $($perMachine[0])." }
    }
    $hostMachine = @($rows | Where-Object { [int]$_.Manifest.Slot -in @(0,1) } | ForEach-Object { $_.Manifest.Machine } | Select-Object -Unique)
    if ($hostMachine.Count -ne 1) { throw 'Slots 0 and 1 must run on the same (host) machine.' }
}
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
if ($mode -eq 'two-per-pc') {
    Write-Output "AUTOMATED 2+2 network route verified: $($machines.Count) machines x 2 processes = technical network test; this is NOT four-real-human acceptance. Human control, 3 consecutive days, 00:00 and host-loss remain UNTESTED by this harness."
} else {
    Write-Output 'AUTOMATED real separate-computer network route verified; human control, 3 consecutive days, 00:00 and host-loss remain UNTESTED by this harness.'
}
