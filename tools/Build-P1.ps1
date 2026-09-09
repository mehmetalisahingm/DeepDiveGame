param([string]$UnityEditor, [switch]$GenerateFixtures, [switch]$Tests, [switch]$Integrated, [switch]$ConnectScenes)
$ErrorActionPreference = 'Stop'
$p1Root = Split-Path -Parent $PSScriptRoot
if (-not $UnityEditor) { $UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' }
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Unity 6000.3.23f1 bulunamadi.' }
$p1LockPath = Join-Path $p1Root 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $p1LockPath) {
    try { $p1Lock = [System.IO.File]::Open($p1LockPath, 'Open', 'ReadWrite', 'None'); $p1Lock.Dispose() }
    catch { throw 'Bu projeyi acan Unity Editor penceresini kapatin.' }
}
$p1Logs = Join-Path $p1Root 'Logs'
New-Item -ItemType Directory -Force -Path $p1Logs | Out-Null
$p1Log = Join-Path $p1Logs $(if ($Tests) { 'P1-tests.log' } else { 'P1-build.log' })
$p1Args = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $p1Root), '-logFile', ('"{0}"' -f $p1Log))
if ($Tests) {
    $p1Args += @('-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'DeepDive.P1.Tests', '-testResults', ('"{0}"' -f (Join-Path $p1Logs 'P1-tests.xml')))
} else {
    $p1Method = if ($ConnectScenes) { 'DeepDive.Editor.P1IntegratedBuild.GenerateAndBuild' }
        elseif ($Integrated) { 'DeepDive.Editor.P1IntegratedBuild.BuildWindows' }
        elseif ($GenerateFixtures) { 'DeepDive.Editor.P1Build.GenerateAndBuild' } else { 'DeepDive.Editor.P1Build.BuildWindows' }
    $p1Args += @('-quit', '-buildTarget', 'Win64', '-executeMethod', $p1Method)
}
$p1Process = Start-Process -FilePath $UnityEditor -ArgumentList $p1Args -WindowStyle Hidden -PassThru
$p1Process.WaitForExit()
if ($p1Process.ExitCode -ne 0) { throw "Unity islemi basarisiz. Log: $p1Log" }
if ($Tests) {
    [xml]$p1Results = Get-Content -LiteralPath (Join-Path $p1Logs 'P1-tests.xml')
    if ($p1Results.'test-run'.result -ne 'Passed') { throw 'P1 testleri gecmedi.' }
    Write-Output "P1 testleri gecti: $($p1Results.'test-run'.passed)"
} elseif (-not (Select-String -LiteralPath $p1Log -SimpleMatch 'P1_BUILD_SUCCEEDED' -Quiet)) { throw 'Build basarisi dogrulanamadi.' }
else { Write-Output $(if ($Integrated -or $ConnectScenes) { 'P1 build: Builds/P1-Integrated/DeepDiveGame-P1.exe' } else { 'P1 build: Builds/P1-NetworkLab/DeepDiveGame-P1.exe' }) }
