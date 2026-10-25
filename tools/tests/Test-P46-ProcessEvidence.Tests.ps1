# Real report parsing/terminal evidence regression, no Unity process or Pester required.
$ErrorActionPreference='Stop'
. (Join-Path (Split-Path -Parent $PSScriptRoot) 'P46-ProcessEvidence.ps1')
$evidenceRoot=Join-Path ([IO.Path]::GetTempPath()) ('p46-process-evidence-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
$checks=0
function Assert-Evidence([bool]$Condition,[string]$Name) {
    if(-not $Condition){throw $Name}; $script:checks++; Write-Output "PASS $Name"
}
function Entry([string]$Name,[int]$Code) {
    [pscustomobject]@{Slot=$Name; Report=(Join-Path $evidenceRoot ($Name+'.json'));
        Process=[pscustomobject]@{HasExited=$true;ExitCode=$Code}}
}
try {
    $crash=Entry 'crash' 17
    Assert-Evidence ((Get-P46ProcessFailure @($crash)) -like '*code 17*') 'Crash detected without waiting for session timeout'
    Complete-P46ProcessEvidence @($crash) $evidenceRoot 'crash provenance' 1
    $report=Get-Content -LiteralPath $crash.Report -Raw|ConvertFrom-Json
    Assert-Evidence (-not $report.passed -and $report.runnerGenerated -and $report.processExitCode -eq 17) 'Missing report becomes explicitly generated failure'
    Assert-Evidence (-not (Get-Content -LiteralPath (Join-Path $evidenceRoot 'runner.json') -Raw|ConvertFrom-Json).passed) 'Runner never calls generated evidence PASS'
    $bad=Entry 'bad' 0
    Set-Content -LiteralPath $bad.Report -Value '{broken'
    Assert-Evidence ((Get-P46ProcessFailure @($bad)) -like '*invalid JSON*') 'Truncated JSON is a failure'
    $writing=[pscustomobject]@{Slot='writer';Report=$bad.Report;Process=[pscustomobject]@{HasExited=$false}}
    Assert-Evidence ($null -eq (Get-P46ProcessFailure @($writing))) 'Live partial writer rechecked before declaring malformed evidence'
    Complete-P46ProcessEvidence @($bad) $evidenceRoot 'invalid report' 1
    Assert-Evidence (Test-Path -LiteralPath ($bad.Report+'.invalid')) 'Original malformed evidence preserved'
    $failure=Entry 'failure' 1
    $bytes='{"passed":false,"failure":"original gameplay assertion","errors":["original gameplay assertion"]}'
    Set-Content -LiteralPath $failure.Report -Value $bytes -NoNewline
    Complete-P46ProcessEvidence @($failure) $evidenceRoot 'another process failed' 1
    Assert-Evidence ((Get-Content -LiteralPath $failure.Report -Raw) -eq $bytes) 'Original gameplay failure never overwritten'
    $ok=Entry 'ok' 0; $reload=Entry 'reload' 0
    @($ok,$reload)|ForEach-Object {Set-Content -LiteralPath $_.Report -Value '{"passed":true}'}
    Assert-Evidence ($null -eq (Get-P46ProcessFailure @($ok,$reload))) 'Successful exited processes accepted'
    Complete-P46ProcessEvidence @($ok,$reload) $evidenceRoot '' 1
    Assert-Evidence ((Get-Content -LiteralPath (Join-Path $evidenceRoot 'runner.json') -Raw|ConvertFrom-Json).passed) 'Successful player plus fresh reload yields runner PASS'
    Complete-P46ProcessEvidence @($ok) $evidenceRoot '' 1
    Assert-Evidence (-not (Get-Content -LiteralPath (Join-Path $evidenceRoot 'runner.json') -Raw|ConvertFrom-Json).passed) 'Missing reload cannot yield runner PASS'
    Write-Output "$checks checks PASS; fixtures: $evidenceRoot"
} finally {
    # Delete only this test's checked absolute temporary directory.
    $resolved=[IO.Path]::GetFullPath($evidenceRoot)
    if(-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase)) {throw 'Unexpected fixture path'}
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
