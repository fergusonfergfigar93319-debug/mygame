param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run in a fresh pwsh process.' }
$sources = @(
    'tools/tests/ThemeContinuityBoundary.cs',
    'tools/tests/ThemeContinuityRegression.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerMapTheme.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RunThemeSessionState.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventModels.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventCatalog.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/WorldEventScheduler.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
Add-Type -Path $sources
$failures = [PenguinRun.Tests.ThemeContinuityRegression]::Run()
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host "PASS: $([PenguinRun.Tests.ThemeContinuityRegression]::CaseCount) theme-continuity scenarios."
