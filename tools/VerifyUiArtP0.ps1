param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-Path([string]$relativePath) {
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

$theme = Require-Path 'Assets/Scripts/Runtime/Game/UI/UiTheme.cs'
Require-Pattern $theme 'class\s+UiTheme' 'UiTheme is the centralized design-token type'
Require-Pattern $theme 'Resources\.Load<Font>\("PenguinRun/Fonts/NotoSansCJKsc-Regular"\)' 'UiTheme loads the CJK fallback font asset'
$manifest = Require-Path 'Packages/manifest.json'
Require-Pattern $manifest '"com\.unity\.textmeshpro"\s*:\s*"3\.0\.6"' 'TextMeshPro package dependency'
foreach ($token in 'Display', 'H1', 'H2', 'Body', 'Caption', 'Micro', 'Spacing1', 'Spacing6', 'RadiusS', 'RadiusM', 'RadiusL', 'DurFast', 'DurBase', 'DurSlow') {
    Require-Pattern $theme "\b$token\b" "UiTheme.$token"
}

$uiBuilder = Require-Path 'Assets/Scripts/Runtime/Game/UI/UiBuilder.cs'
Require-Pattern $uiBuilder 'UiTheme\.AccentCyan' 'UiBuilder consumes UiTheme color tokens'
Require-Pattern $uiBuilder 'UiTheme\.Body' 'UiBuilder consumes UiTheme typography tokens'
Require-Pattern $uiBuilder 'TextMeshProUGUI' 'UiBuilder exposes a TMP text factory'
Require-Pattern $uiBuilder 'CreateTmpText' 'UiBuilder creates TMP text'
$legacyFontHits = Get-ChildItem -Path (Join-Path $ProjectRoot 'Assets') -Recurse -File `
        -Include *.cs, *.shader, *.asset, *.prefab, *.unity, *.mat, *.json, *.txt |
    Select-String -Pattern 'LegacyRuntime\.ttf' -List
if ($legacyFontHits) {
    $failures.Add('Runtime assets still reference LegacyRuntime.ttf')
}

$shader = Require-Path 'Assets/Shaders/GradientSkybox.shader'
Require-Pattern $shader 'Shader\s+"PenguinRun/GradientSkybox"' 'GradientSkybox shader registration'
Require-Pattern $shader '_HorizonColor' 'GradientSkybox horizon color property'
Require-Pattern $shader '_ZenithColor' 'GradientSkybox zenith color property'

$visuals = Require-Path 'Assets/Scripts/Runtime/PenguinRun/RunnerVisuals.cs'
Require-Pattern $visuals 'Dictionary<' 'RunnerVisuals material cache'
Require-Pattern $visuals 'MaterialPropertyBlock' 'RunnerVisuals per-renderer color updates use MaterialPropertyBlock'
Require-Pattern $visuals 'sharedMaterial' 'RunnerVisuals preserves shared materials'

$deathOverlay = Require-Path 'Assets/Scripts/Runtime/Game/UI/RunnerDeathOverlay.cs'
Require-Pattern $deathOverlay 'class\s+RunnerDeathOverlay' 'runner death overlay type'
Require-Pattern $deathOverlay 'CreatePrimaryButton\("Retry"' 'runner death overlay retry action'
Require-Pattern $deathOverlay 'CreateSecondaryButton\("ReturnToLobby"' 'runner death overlay return action'
Require-Pattern $deathOverlay 'GetDeathAnalysisHint' 'runner death overlay analysis hint'
Require-Pattern $deathOverlay 'CreateTmpText' 'runner death overlay uses TMP text'

$runnerGame = Require-Path 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
Require-Pattern $runnerGame 'RunnerDeathOverlay\.Show' 'PenguinRunnerGame invokes the uGUI death overlay'
Require-Pattern $runnerGame 'RestartRunFromDeathOverlay' 'PenguinRunnerGame supports the retry short path'

$particles = Require-Path 'Assets/Scripts/Runtime/PenguinRun/Systems/ParticleEffectPool.cs'
Require-Pattern $particles 'ParticleSystem' 'particle pool uses Unity ParticleSystem'
Require-Pattern $particles 'EmitParams' 'particle pool emits configured particles'
Require-Pattern $particles 'maxParticles\s*=\s*256' 'particle pool caps particle count'

$themeProfile = Require-Path 'Assets/Scripts/Runtime/PenguinRun/ThemePresentationProfile.cs'
Require-Pattern $themeProfile '(class|struct)\s+ThemePresentationProfile' 'theme presentation profile type'
foreach ($themeName in 'IceLakeEcho', 'CedarRuins', 'AuroraField', 'MistDike', 'OceanReef', 'SkyFlight') {
    Require-Pattern $themeProfile "RunnerMapTheme\.$themeName" "theme profile for $themeName"
}
Require-Pattern $themeProfile 'PrimaryLandmark' 'primary landmark recipe'
Require-Pattern $themeProfile 'SecondaryLandmark' 'secondary landmark recipe'
Require-Pattern $runnerGame 'ThemePresentationProfile\.For\(theme\)' 'runner scene consumes the theme presentation profile'
Require-Pattern $runnerGame 'AddProfileLandmark' 'runner scene builds profile-selected landmarks'
Require-Pattern $runnerGame 'ThemeLandmarkKind\.CloudIsland' 'runner scene supports cloud island landmarks'

$powerProfile = Require-Path 'Assets/Scripts/Runtime/PenguinRun/PowerUpVisualProfile.cs'
Require-Pattern $powerProfile 'struct\s+PowerUpVisualProfile' 'power-up visual profile type'
Require-Pattern $powerProfile 'CoreColor' 'power-up core color'
Require-Pattern $powerProfile 'HaloColor' 'power-up halo color'
Require-Pattern $powerProfile 'PulseScale' 'power-up pulse scale'
$spawner = Require-Path 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
Require-Pattern $spawner 'PowerUpVisualProfile\.For\(kind, config\.MapTheme\)' 'spawner applies power-up visual profile'
Require-Pattern $runnerGame 'ApplyMobileQualityBudget' 'runner applies mobile quality budget'
Require-Pattern $runnerGame 'particleRaycastBudget\s*=\s*64' 'mobile budget caps particle raycasts'

$bossEncounter = Require-Path 'Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs'
Require-Pattern $bossEncounter 'SnowKingIceCore' 'Snow King ice core module'
Require-Pattern $bossEncounter 'SnowKingShoulderIce' 'Snow King shoulder ice modules'
Require-Pattern $bossEncounter 'OnPatternStrikeMoment' 'Boss strike callback remains the strike feedback origin'
$bossVfx = Require-Path 'Assets/Scripts/Runtime/PenguinRun/Systems/BossPatternVfxPlayer.cs'
Require-Pattern $bossVfx 'SpawnSnowKingIceStrike' 'Snow King strike burst is pool-rented'
Require-Pattern $bossVfx 'BossSilhouette\.SnowKing' 'Snow King strike burst is silhouette scoped'
Require-Pattern $bossVfx 'MaxShardBurstCount\s*=\s*6' 'Boss strike shard burst is mobile capped'
Require-Pattern $bossVfx 'Mathf\.Min\(count, MaxShardBurstCount\)' 'Boss shard burst applies the mobile cap'
Require-Pattern $bossEncounter 'spawnTimer\s*=\s*1\.8f' 'Boss intro has readable minimum duration'
Require-Pattern $bossEncounter 'CedarHeartCore' 'Cedar Sentinel heart core module'
Require-Pattern $bossEncounter 'CedarRootShoulder' 'Cedar Sentinel root shoulder modules'
Require-Pattern $bossEncounter 'AuroraMagnetCore' 'Aurora Serpent magnet core module'
Require-Pattern $bossEncounter 'AuroraFin' 'Aurora Serpent fin modules'
Require-Pattern $bossEncounter 'MistCrownHorn' 'Mist Guardian crown horn modules'
Require-Pattern $bossEncounter 'SpawnDeceptiveDecoyTelegraphsIfNeeded' 'Mist decoy telegraphs remain visual-only'
Require-Pattern $bossEncounter 'CoralHeartCore' 'Coral Kraken heart core module'
Require-Pattern $bossEncounter 'CoralCrest' 'Coral Kraken crest modules'
Require-Pattern $bossEncounter 'StormCore' 'Storm Eagle lightning core module'
Require-Pattern $bossEncounter 'StormTailFeather' 'Storm Eagle tail feather modules'

$fontAsset = Join-Path $ProjectRoot 'Assets/Resources/PenguinRun/Fonts/NotoSansCJKsc-Regular.otf'
if (-not (Test-Path -LiteralPath $fontAsset)) { $failures.Add('Missing bundled Noto Sans CJK SC font asset') }
$visualCredits = Require-Path 'Assets/Resources/PenguinRun/OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt'
Require-Pattern $visualCredits 'Noto Sans CJK SC Regular' 'font license credit'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output 'P0 UI/art static contracts passed.'
