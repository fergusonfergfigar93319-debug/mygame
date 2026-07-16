param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-Required([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }
    return Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

function Forbid([string]$content, [string]$pattern, [string]$description) {
    if ($null -ne $content -and $content -match $pattern) {
        $failures.Add("Forbidden contract remains: $description")
    }
}

$modalStack = Read-Required 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyModalStack.cs'
$launchGate = Read-Required 'Assets/Scripts/Runtime/Game/RunLaunchGate.cs'
$gestureClassifier = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Input/RunnerGestureClassifier.cs'
$settlementGate = Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunSettlementGate.cs'
$validator = Read-Required 'Assets/Scripts/Editor/PenguinRun/ProjectPlayerExperienceValidation.cs'
$journeySmoke = Read-Required 'Assets/Editor/ProjectPlayerJourneySmokeTest.cs'
$report = Read-Required 'docs/PLAYER_EXPERIENCE_AUDIT.md'
$runner = Read-Required 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
$hud = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs'
$menu = Read-Required 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'
$androidBuild = Read-Required 'tools/BuildLatestAndroidApk.ps1'

Require $modalStack '\bLobbyModalStack\b' 'lobby modal stack helper'
Require $launchGate '\bRunLaunchGate\b' 'run launch gate helper'
Require $gestureClassifier '\bRunnerGestureClassifier\b' 'responsive gesture classifier'
Require $settlementGate '\bRunSettlementGate\b' 'run settlement gate helper'
Require $validator '\bValidateFromCommandLine\b' 'player experience editor validator entrypoint'
Require $journeySmoke '\bRunFromCommandLine\b' 'complete player journey smoke entrypoint'
Require $report '\| PX-001 \| P0 \|' 'experience audit issue table'
Require $runner '\bTrySettleCurrentAttempt\b' 'shared exactly-once settlement path'
Require $runner '\bResetPresentationForRetry\b' 'retry presentation reset'
Require $hud '"Pause"' 'visible pause button object'
Require $hud '"Resume"' 'visible resume button object'
Forbid $runner '\bIsReturnButtonTouch\b' 'invisible corner exit path'
Require $menu '\bLobbyModalStack\b' 'lobby modal back ownership'
Require $menu '\bRunLaunchGate\b' 'duplicate run launch protection'
Require $androidBuild "'VerifyThemeMechanicsPhase4\.ps1'[\s\S]*'VerifyPlayerExperienceAudit\.ps1'" 'player experience audit runs after the theme gate in Android builds'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Player experience audit static contracts passed.'
