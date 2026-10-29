param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $relativePath))) {
        $failures.Add("Missing: $relativePath")
    }
}

function Require-Pattern([string]$relativePath, [string]$pattern, [string]$label) {
    $fullPath = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath) -or
        (Get-Content -Raw -Encoding utf8 -LiteralPath $fullPath) -notmatch $pattern) {
        $failures.Add("Missing contract: $label")
    }
}

function Forbid-Pattern([string]$relativePath, [string]$pattern, [string]$label) {
    $fullPath = Join-Path $ProjectRoot $relativePath
    if ((Test-Path -LiteralPath $fullPath) -and
        (Get-Content -Raw -Encoding utf8 -LiteralPath $fullPath) -match $pattern) {
        $failures.Add("Forbidden contract: $label")
    }
}

Require-File 'Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs'
Require-File 'Assets/Scripts/Editor/PenguinRun/ProjectOnboardingValidation.cs'
Require-File 'docs/adr/0004-competence-driven-onboarding.md'

Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs' `
    'ForceGraduateAfterRuns\s*=\s*3' 'run-count fuse constant'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs' `
    'LaneDemonstrated' 'lane competence bit'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs' `
    'JumpDemonstrated' 'jump competence bit'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs' `
    'SlideDemonstrated' 'slide competence bit'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs' `
    'CollectDemonstrated' 'collect competence bit'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs' `
    'IsOnboarding\s*\(' 'derived onboarding predicate'

Require-Pattern 'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs' `
    'onboarding_v1_lane' 'persisted lane bit key'
Require-Pattern 'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs' `
    'onboarding_v1_jump' 'persisted jump bit key'
Require-Pattern 'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs' `
    'onboarding_v1_slide' 'persisted slide bit key'
Require-Pattern 'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs' `
    'onboarding_v1_collect' 'persisted collect bit key'

Require-Pattern 'Assets/Scripts/Runtime/Game/Save/SaveSchema.cs' `
    'CurrentVersion\s*=\s*4' 'schema version 4'
Require-Pattern 'Assets/Scripts/Runtime/Game/Save/SaveSchema.cs' `
    'MigrateOnboardingCompetence' 'onboarding migration'

Require-Pattern 'Assets/Scripts/Runtime/Game/RunnerSessionConfig.cs' `
    'IsOnboarding' 'session snapshot onboarding flag'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentWaveDirector.cs' `
    'OnboardingRotation' 'deterministic onboarding rotation'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentWaveDirector.cs' `
    'IsExcludedOnboardingPattern' 'hard-pattern exclusion helper'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs' `
    'suppressed' 'route event suppression'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/MapMechanicDirector.cs' `
    'suppressed' 'theme mechanic suppression'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/WorldEventScheduler.cs' `
    'suppressed' 'world event suppression'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs' `
    '!config\.IsOnboarding' 'enemy spawn suppression'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs' `
    'config\.IsOnboarding\s*\?\s*2\s*:\s*5' 'seed obstacle reduction'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs' `
    'PowerUpKind\.Dash' 'onboarding first pickup is Dash'
Require-Pattern 'CONTEXT.md' `
    '第一次道具固定为鱼干冲刺' 'glossary first-pickup dash rule'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs' `
    'onboarding\.Persist\(\)' 'persist competence at settlement'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs' `
    'SuppressMotorActionLabels' 'action labels gated to onboarding'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' `
    'WorldEventScheduler\(config\.IsOnboarding\)' 'world events receive onboarding flag'
Require-Pattern 'Assets/Scripts/Editor/PenguinRun/ProjectOnboardingValidation.cs' `
    'ValidateFromCommandLine' 'editor batch entry point'
Require-Pattern 'CONTEXT.md' `
    'Onboarding Period' 'glossary onboarding term'
Require-Pattern 'CONTEXT.md' `
    'Runner3DTutorialDone' 'codex-domain clarification'

# Slide-gate tutorial must no longer write the legacy prefs flag from the spawner.
Forbid-Pattern "Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs" `
    "RunnerSlideGateTutorialDone\s*=" "spawner must not write legacy slide tutorial flag"

if ($failures.Count -gt 0) {
    Write-Host "FAIL: VerifyFirstRunOnboarding" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "PASS: VerifyFirstRunOnboarding" -ForegroundColor Green
Write-Host "Static contracts for competence-driven first-run onboarding are locked."
exit 0
