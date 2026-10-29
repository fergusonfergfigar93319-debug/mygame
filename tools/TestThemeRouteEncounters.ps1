param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run in a fresh pwsh process.' }
$sources = @(
    'tools/tests/ThemeRouteEncounterBoundary.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerLane.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerMapTheme.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RunThemeSessionState.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventModels.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventCatalog.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/ThemeRouteEncounterCatalog.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEncounterReservation.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/ThemeRoutePresentation.cs',
    'tools/tests/ThemeRouteEncounterRegression.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
foreach ($source in $sources) {
    if (-not (Test-Path -LiteralPath $source)) { Write-Host "FAIL: route behavior implementation missing: $source"; exit 1 }
}
Add-Type -Path $sources
$failures = [PenguinRun.Tests.ThemeRouteEncounterRegression]::Run()
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host "PASS: $([PenguinRun.Tests.ThemeRouteEncounterRegression]::CaseCount) theme-route behavior scenarios."
