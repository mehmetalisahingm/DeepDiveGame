# #218 — P4.11 animated fish source importer.
# Pinned public mirrors of the original Quaternius Animated Cute Fish CC0 FBX pack.
# No fees, no API keys, no unchecked replacement of an existing fish asset.
param(
    [switch]$AuthorScene,
    [string]$UnityEditor = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$target = Join-Path $root 'Assets/DeepDive/Art/Supplied/Fish'
$metaTemplate = Join-Path $target 'CoralGrouper.fbx.meta'
if (-not (Test-Path -LiteralPath $metaTemplate)) {
    throw 'Missing existing model importer template: CoralGrouper.fbx.meta'
}
$sourceRef = '25c512ed6c7c70b904671ba2be76353f26c332ab'
$baseUrl = "https://raw.githubusercontent.com/algosup/2024-2025-project-2-serious-game-team-1/$sourceRef/src/assets/designs/Fishs/FBX"
$models = @(
    [pscustomobject]@{
        Name = 'Humphead'; Length = 303292
        GitBlob = '97b60b917df325a8ee95ecb7ab5ea4afa7c9130c'
        Guid = '40c765de98bf4852ae668f1ba42ac970'
    },
    [pscustomobject]@{
        Name = 'Anglerfish'; Length = 318268
        GitBlob = 'a2972f59901f54a34339c0e4457b03808b064506'
        Guid = '7a4c3bc0937f411dbe6708c81c8dfe54'
    }
)

function Get-GitBlobHash([string]$path) {
    [byte[]]$data = [System.IO.File]::ReadAllBytes($path)
    [byte[]]$header = [Text.Encoding]::ASCII.GetBytes("blob $($data.Length)`0")
    [byte[]]$buffer = New-Object byte[] ($header.Length + $data.Length)
    [Array]::Copy($header, 0, $buffer, 0, $header.Length)
    [Array]::Copy($data, 0, $buffer, $header.Length, $data.Length)
    $sha = [System.Security.Cryptography.SHA1]::Create()
    try { return -join ($sha.ComputeHash($buffer) | ForEach-Object { $_.ToString('x2') }) }
    finally { $sha.Dispose() }
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
$template = [System.IO.File]::ReadAllText($metaTemplate)
if ($template -notmatch '(?m)^guid: [0-9a-f]{32}$') { throw 'Source ModelImporter meta has invalid GUID' }
foreach ($model in $models) {
    $dest = Join-Path $target ($model.Name + '.fbx')
    if (-not (Test-Path -LiteralPath $dest)) {
        $temp = Join-Path $target ('P411_' + $model.Name + '.download')
        try {
            $url = "$baseUrl/$($model.Name).fbx"
            Write-Output "Fetching CC0 animation source $($model.Name) from pinned GitHub mirror..."
            Invoke-WebRequest -Uri $url -OutFile $temp -MaximumRedirection 5
            if ((Get-Item -LiteralPath $temp).Length -ne $model.Length) {
                throw "Unexpected size for $($model.Name).fbx"
            }
            $hash = Get-GitBlobHash $temp
            if ($hash -ne $model.GitBlob) {
                throw "SOURCE_HASH_MISMATCH $($model.Name).fbx: $hash"
            }
            Move-Item -LiteralPath $temp -Destination $dest
        }
        finally {
            if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force }
        }
    }
    if ((Get-Item -LiteralPath $dest).Length -ne $model.Length) {
        throw "Existing FBX length mismatch: $dest"
    }
    $verify = Get-GitBlobHash $dest
    if ($verify -ne $model.GitBlob) {
        throw "Existing FBX hash mismatch: $dest"
    }
    $metaPath = "$dest.meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        $newMeta = [regex]::Replace($template, '(?m)^guid: [0-9a-f]{32}$', "guid: $($model.Guid)", 1)
        [System.IO.File]::WriteAllText($metaPath, $newMeta, (New-Object System.Text.UTF8Encoding($false)))
    }
    elseif (([System.IO.File]::ReadAllText($metaPath)) -notmatch "(?m)^guid: $($model.Guid)$") {
        throw "Existing Unity meta GUID differs for $($model.Name); resolve before overwriting."
    }
    Write-Output "VERIFIED $($model.Name).fbx $($model.Length) bytes git-blob=$verify"
}

if ($AuthorScene) {
    if ([string]::IsNullOrWhiteSpace($UnityEditor) -or -not (Test-Path -LiteralPath $UnityEditor)) {
        throw 'To author the scene, pass -UnityEditor <path-to-Unity-6000.3.23f1 executable>'
    }
    $log = Join-Path $root 'Logs/P411-five-species-import.log'
    New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
    Write-Output "Authoring five real fish species in a local Unity Editor batch: $log"
    & $UnityEditor -batchmode -quit -projectPath $root -executeMethod DeepDive.World.Editor.P411FiveSpeciesSceneSetup.Apply -logFile $log
    if ($LASTEXITCODE -ne 0) { throw "Unity fish scene authoring failed ($LASTEXITCODE). See $log" }
    Write-Output 'P411_FIVE_SPECIES_AUTHOR_COMMAND_SUCCEEDED: review generated prefab/scene changes, animation/rig and 1/2/4 runtime evidence before accepting.'
}
else {
    Write-Output 'Two authenticated CC0 FBX sources and deterministic Unity .meta files are ready. Scene not authored: rerun with -AuthorScene and -UnityEditor.'
}
