param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }

    return Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require-Pattern([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

$routeDirector = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs'
Require-Pattern $routeDirector '\bTick\s*\(' 'RouteEventDirector.Tick'
Require-Pattern $routeDirector '\bChoose\s*\(' 'RouteEventDirector.Choose'
Require-Pattern $routeDirector 'FirstEventDistance\s*=\s*220f' 'route events start no earlier than 220m'
Require-Pattern $routeDirector 'MinimumEventSpacing\s*=\s*280f' 'route events keep at least 280m spacing'

$routeModels = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventModels.cs'
foreach ($kind in 'RiskFork', 'CompanionRescue', 'RelicCache', 'WeatherTrial', 'BossChase', 'SafeExit') {
    Require-Pattern $routeModels "\b$kind\b" "RouteEventKind.$kind"
}
foreach ($field in 'SafeOutcome', 'RiskOutcome', 'ScoreReward', 'FishReward', 'SegmentPattern') {
    Require-Pattern $routeModels "\b$field\b" "route event definition includes $field"
}

$routeCatalog = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventCatalog.cs'
foreach ($theme in 'IceLakeEcho', 'CedarRuins', 'AuroraField', 'MistDike', 'OceanReef', 'SkyFlight') {
    Require-Pattern $routeCatalog "RunnerMapTheme\.$theme" "route catalog covers $theme"
}

$segmentSpawner = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
Require-Pattern $segmentSpawner '\bSpawnRouteEvent\s*\(' 'SegmentSpawner.SpawnRouteEvent'
Require-Pattern $segmentSpawner '\bTryConsumeRouteChoice\s*\(' 'SegmentSpawner.TryConsumeRouteChoice'
Require-Pattern $segmentSpawner '\bSpawnRouteEventOutcome\s*\(' 'SegmentSpawner.SpawnRouteEventOutcome reward lane'
Require-Pattern $segmentSpawner 'collider-free|Collider-free' 'route markers are explicitly collider-free'

$runnerController = Require-File 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs'
Require-Pattern $runnerController '\bApplyRouteEventOutcome\s*\(' 'RunnerGameController.ApplyRouteEventOutcome'
Require-Pattern $runnerController 'routeEventDirector\.Tick\s*\(' 'controller advances route event director'

$worldDirector = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/WorldDirector.cs'
Require-Pattern $worldDirector '\bAddRouteEventReward\s*\(' 'WorldDirector.AddRouteEventReward explicit route reward command'

$upgradeState = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeState.cs'
Require-Pattern $upgradeState '\bBuildOffer\s*\(' 'RunUpgradeState.BuildOffer'
Require-Pattern $upgradeState '\bSelect\s*\(' 'RunUpgradeState.Select'
Require-Pattern $upgradeState 'MaxSelections\s*=\s*6\b' 'run upgrade selection cap is six'
Require-Pattern $upgradeState 'MaxStacksPerUpgrade\s*=\s*3\b' 'run upgrade stack cap is three'
foreach ($query in 'DashDurationMultiplier', 'MagnetRadiusMultiplier', 'PerfectDodgeScoreMultiplier', 'ShieldGraceSeconds') {
    Require-Pattern $upgradeState "\b$query\b" "RunUpgradeState.$query"
}
Require-Pattern $runnerController 'runUpgradeState\.Reset\s*\(' 'controller resets in-run upgrades per run'

$upgradeModels = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeModels.cs'
foreach ($category in 'Movement', 'Collection', 'Defense', 'Companion') {
    Require-Pattern $upgradeModels "\b$category\b" "RunUpgradeCategory.$category"
}
Require-Pattern $upgradeModels '\bMaxStacks\b' 'upgrade definition carries a stack cap'

$upgradeCatalog = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeCatalog.cs'
Require-Pattern $upgradeCatalog '\bAll\b' 'run upgrade catalog exposes data definitions'
if ($upgradeCatalog -match 'PlayerPrefs|PlayerSave') {
    $failures.Add('RunUpgradeCatalog must not write or read permanent save data')
}

$upgradeOverlay = Require-File 'Assets/Scripts/Runtime/Game/UI/RunUpgradeChoiceOverlay.cs'
Require-Pattern $upgradeOverlay 'CardCount\s*=\s*3\b' 'upgrade overlay renders exactly three cards'
foreach ($field in 'Category', 'Description', 'Stack') {
    Require-Pattern $upgradeOverlay "\b$field\b" "upgrade card displays $field text"
}
Require-Pattern $upgradeOverlay 'Action<RunUpgradeId>' 'upgrade overlay exposes one selection callback'
Require-Pattern $upgradeOverlay 'AutoSelectSeconds\s*=\s*12f' 'upgrade overlay auto-selects after 12 seconds'
Require-Pattern $upgradeOverlay 'unscaledDeltaTime' 'upgrade overlay uses unscaled UI time'
Require-Pattern $runnerController '\bTryBuildUpgradeOffer\s*\(' 'controller triggers route-completion upgrade offers'
Require-Pattern $runnerController '\bSelectRunUpgrade\s*\(' 'controller applies one selected upgrade'
Require-Pattern $runnerController 'CompletedCount\s*==\s*1' 'first completed route event triggers an upgrade offer'
Require-Pattern $runnerController '\(routeEventDirector\.CompletedCount\s*-\s*1\)\s*%\s*2' 'later upgrade offers are spaced by two completed route events'

$skillTracker = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/SkillScoreTracker.cs'
Require-Pattern $skillTracker '\bRecord\s*\(' 'SkillScoreTracker.Record'

$traitCatalog = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/BossThemeTraitCatalog.cs'
Require-Pattern $traitCatalog '\bFor\s*\(' 'BossThemeTraitCatalog.For'

$bossSystem = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs'
Require-Pattern $bossSystem '\bCheckPatternHit\s*\(' 'BossSystem.CheckPatternHit damage authority'
Require-Pattern $bossSystem 'RunnerHitResolver\.IsBossPatternHit\s*\(' 'BossSystem delegates pattern hits to RunnerHitResolver'

$hitResolver = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHitResolver.cs'
Require-Pattern $hitResolver '\bIsBossPatternHit\s*\(' 'RunnerHitResolver.IsBossPatternHit'
Require-Pattern $bossSystem '\bIsPatternDangerous\b' 'boss dangerous-window authority'

$fxPool = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/BossPatternFxPool.cs'
Require-Pattern $fxPool 'MaxPoolSize\s*=\s*48\b' 'BossPatternFxPool capacity remains 48'

$vfxPlayer = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/BossPatternVfxPlayer.cs'
Require-Pattern $vfxPlayer 'MaxShardBurstCount\s*=\s*6\b' 'Boss pattern shard burst remains capped at six'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Run-depth phase-two static contracts passed.'
