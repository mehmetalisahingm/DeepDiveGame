param([string]$UnityEditor)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$versionLine = Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') |
    Where-Object { $_ -match '^m_EditorVersion: ' }
$editorVersion = ($versionLine -split ': ', 2)[1]
if (-not $UnityEditor) {
    $UnityEditor = "C:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityEditor)) {
    throw "Unity $editorVersion bulunamadi. -UnityEditor ile Unity.exe yolunu belirtin."
}
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile')) {
    throw 'Bu projeyi acan Unity Editor penceresini kapatip tekrar deneyin.'
}
$logsPath = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logsPath | Out-Null
$logPath = Join-Path $logsPath 'P0-build.log'
$arguments = @(
    '-batchmode', '-nographics', '-quit', '-buildTarget', 'Win64',
    '-projectPath', ('"{0}"' -f $projectRoot),
    '-executeMethod', 'DeepDive.Editor.P0Build.BuildWindows',
    '-logFile', ('"{0}"' -f $logPath)
)
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
# Wait for the editor itself, not background services Unity may leave running.
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Build basarisiz ($($process.ExitCode)). Log: $logPath" }
if (-not (Select-String -LiteralPath $logPath -SimpleMatch 'P0_BUILD_SUCCEEDED' -Quiet)) {
    throw "Unity tamamlandi ama build basarisi dogrulanamadi. Log: $logPath"
}
Write-Output "Build hazir: $(Join-Path $projectRoot 'Builds/P0-Windows/DeepDiveGame-P0.exe')"
