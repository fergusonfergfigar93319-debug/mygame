param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run in a fresh pwsh process.' }
$sources = @(
    'tools/tests/ThemeContinuityBoundary.cs',
    'tools/tests/ThemeMasterySettlementBoundary.cs',
    'tools/tests/ThemeMasterySessionContracts.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerMapTheme.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerLane.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicModels.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/DeterministicRunRandom.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicCatalog.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/JourneyContractCatalog.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasteryModels.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasteryTracker.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasterySnapshots.cs',
    'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasterySessionLedger.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeModels.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeCatalog.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerRunResult.cs',
    'Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs',
    'Assets/Scripts/Runtime/Game/Growth/CompanionBondModels.cs',
    'Assets/Scripts/Runtime/Game/Growth/ThemeMasteryStore.cs',
    'Assets/Scripts/Runtime/Game/Growth/GrowthTransaction.cs',
    'Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
Add-Type -Path $sources
$failures = [PenguinRun.Tests.ThemeMasterySessionContracts]::Run()
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: 7 theme mastery session/settlement scenarios (including 200 late-visit seed/theme cases).'
