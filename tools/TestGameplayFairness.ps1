param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run with pwsh (PowerShell 7).' }
$sources = @(
    'tools/tests/RunnerInputEngineBoundary.cs',
    'tools/tests/GameplayFairnessBoundary.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerInput.cs',
    'Assets/Scripts/Runtime/PenguinRun/Input/RunnerGestureClassifier.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerMapTheme.cs',
    'Assets/Scripts/Runtime/PenguinRun/ObstacleColliderSpec.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/CollisionSystem.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerSegmentCatalog.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentWaveDirector.cs',
    'tools/tests/GameplayFairnessRegression.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
Add-Type -Path $sources
$failures = [PenguinRun.Tests.GameplayFairnessRegression]::Run()
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host "PASS: $([PenguinRun.Tests.GameplayFairnessRegression]::CaseCount) collision-scoring / segment-selection scenarios."
