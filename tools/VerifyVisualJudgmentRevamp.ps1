param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

# Static gate for the 2026-07-20 modeling/VFX/judgment/UI revamp batch.
# Mirrors the contract style of the other Verify*.ps1 gates.

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-Required([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }
    Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

function Forbid([string]$content, [string]$pattern, [string]$description) {
    if ($null -ne $content -and $content -match $pattern) {
        $failures.Add("Forbidden pattern present: $description")
    }
}

function Require-File([string]$relativePath) {
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $relativePath))) {
        $failures.Add("Missing required asset: $relativePath")
    }
}

function Forbid-File([string]$relativePath) {
    if (Test-Path -LiteralPath (Join-Path $ProjectRoot $relativePath)) {
        $failures.Add("Dead file should be deleted: $relativePath")
    }
}

# ── Phase A: judgment / feel ──────────────────────────────────────────────
$resolver = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHitResolver.cs'
Require $resolver 'consumeShield:\s*true' 'boss shield hit consumes the shield'
Require $resolver 'invulnAfterShieldBreak' 'shield break grants invulnerability on boss path'
foreach ($layer in 'IceMirrorTimer', 'TryConsumeTreantArmor', 'TryConsumeCoralBounce') {
    Require $resolver "TryResolveBossHit[\s\S]*$layer" "boss defense layer $layer"
}

$bossSystem = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs'
Require $bossSystem '\bNotifyBossDamageTaken\b' 'deferred boss damage bookkeeping'
Require $bossSystem '\blastGeometricHitPattern\b' 'geometric hit pattern capture'
Forbid $bossSystem 'playerShieldTimer > 0f\)\s*\{\s*feedbackText = "护盾抵挡' 'old in-place boss shield early-return'

$motor = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs'
foreach ($name in 'BodyHeight', 'StandingBodyHeight', 'SlidingBodyHeight', 'queuedSlideOnLand') {
    Require $motor "\b$name\b" "crouch hitbox / slide buffering: $name"
}
Require $motor 'jumpBufferTimer[\s\S]{0,700}PerfectActionWindow = combatTuning\.perfectWindowJumpSeconds' 'buffered jump grants perfect window'

$spec = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ObstacleColliderSpec.cs'
Require $spec '\bPassUnderMaxHeight\b' 'pass-under height on collider spec'
Require $spec 'WideWall => new\(false, 1\.05f' 'WideWall half width narrowed to 1.05'
Require $spec 'WideSlideOnly => new\(true, 1\.02f' 'WideSlideOnly half width narrowed to 1.02'

$collision = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/CollisionSystem.cs'
Require $collision '\bclearedByCrouch\b' 'geometric crouch pass-under in collision system'
Require $collision 'warnedMissingSpecs' 'missing-spec dev warning'

$spawner = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
foreach ($name in 'SpawnTimedGate', 'SpawnMovingBarrier') {
    Require $spawner "\b$name\b" "dynamic obstacle spawn: $name"
}
Require $spawner 'AddComponent<ShatterWall>' 'shatter wall wired into wide walls'
Require $spawner 'obstacleBehaviors\.Add' 'obstacle behaviors registered'

$behavior = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/ObstacleBehavior.cs'
Require $behavior 'root\.position = pos' 'MovingBarrier moves the collision root'

# ── Phase B: CC0 rigged bosses / pickups / penguin ────────────────────────
foreach ($boss in 'boss_snow_king', 'boss_cedar_sentinel', 'boss_aurora_serpent', 'boss_mist_guardian', 'boss_coral_kraken', 'boss_storm_eagle') {
    Require-File "Assets/Resources/PenguinRun/Models/Bosses/$boss.fbx"
}
Require-File 'Assets/Resources/PenguinRun/Models/Bosses/Atlas_Monsters.png'

$rig = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/BossRigAnimator.cs'
foreach ($name in 'PlayIdle', 'PlayAttack', 'PlayHit', 'PlayDazed', 'PlayDeath', 'PlayMove') {
    Require $rig "\b$name\b" "rig animator clip control: $name"
}

$encounter = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs'
Require $encounter 'PenguinRun/Models/Bosses/' 'boss rigged model resource path'
Require $encounter 'rigAnimator\.Bind' 'rig animator bound to loaded model'

foreach ($pickup in 'aurora_chain', 'bubble_shield', 'cloud_walk', 'coral_bounce', 'double_fish_snack', 'fish_bomb', 'fog_lantern', 'ice_mirror', 'seahorse_boost', 'second_heart', 'thunder_feather', 'time_hourglass', 'treant_armor', 'wind_rider') {
    Require-File "Assets/Resources/PenguinRun/Prefabs/Pickups/$pickup.prefab"
}

$profile = Read-Required 'Assets/Scripts/Runtime/PenguinRun/PowerUpVisualProfile.cs'
Require $profile '唯一道具配色来源' 'unified power-up color source'

$character = Read-Required 'Assets/Scripts/Runtime/PenguinRun/PenguinCharacter.cs'
Require $character '\bTriggerCelebration\b' 'penguin celebration presentation'

$credits = Read-Required 'Assets/Resources/PenguinRun/OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt'
Require $credits 'Quaternius' 'Quaternius pack recorded in credits'
Require $credits 'UiIcons' 'UI icon set recorded in credits'
Forbid $credits 'Status: PLACEHOLDER' 'planned-model placeholders should be resolved'

# ── Phase C: VFX / post-processing ───────────────────────────────────────
$fxPool = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/BossPatternFxPool.cs'
Require $fxPool 'MaxPoolSize = 48' 'FX pool cap stays at 48'
Require $fxPool '\bParticleSystem\b' 'FX pool upgraded with particle systems'

$vfx = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/BossPatternVfxPlayer.cs'
Require $vfx 'FireTelegraphVfx' 'telegraph phase gets dedicated FX'
foreach ($strike in 'SpawnSnowKingIceStrike', 'SpawnCedarRootStrike', 'SpawnAuroraPolarityStrike', 'SpawnMistVeilStrike', 'SpawnCoralSplashStrike', 'SpawnStormBoltStrike') {
    Require $vfx "\b$strike\b" "signature strike: $strike"
}

$particles = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/ParticleEffectPool.cs'
Require $particles '\bEmitDirected\b' 'directed emission (magnet fly-line)'

$hud = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs'
foreach ($buff in '星光', '沙漏', '冰镜', '雾灯', '雷羽') {
    Require $hud $buff "HUD buff summary covers $buff"
}

$postfx = Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunnerPostFx.cs'
Require $postfx 'PostProcessLayer' 'post-processing layer attach'
Require-File 'Assets/Resources/PenguinRun/PostFx/postfx_rig.prefab'
Require-File 'Assets/Resources/PenguinRun/PostFx/postfx_profile.asset'
$manifest = Read-Required 'Packages/manifest.json'
Require $manifest 'com\.unity\.postprocessing' 'post-processing package dependency'

Require-File 'Assets/Scripts/Runtime/PenguinRun/SceneryBillboards.cs'
Require-File 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicMotion.cs'

# ── Phase D: UI icons / TMP / cleanup ────────────────────────────────────
$uiIcons = Read-Required 'Assets/Scripts/Runtime/Game/UI/UiIcons.cs'
foreach ($name in 'HeartFull', 'HeartEmpty', 'Timer', 'Back', 'Load', 'CreateImage') {
    Require $uiIcons "\b$name\b" "UiIcons registry member $name"
}
foreach ($icon in 'icon_star', 'icon_back', 'icon_pause', 'icon_gear', 'icon_trophy', 'icon_check', 'icon_cross', 'icon_locked', 'icon_heart_full', 'icon_heart_empty', 'icon_timer') {
    Require-File "Assets/Resources/PenguinRun/UiIcons/$icon.png"
    Require-File "Assets/Resources/PenguinRun/UiIcons/$icon.png.meta"
}

Require $hud '\bheartIcons\b' 'HUD lives rendered as heart sprites'
Forbid $hud "'♥'" 'unicode heart literals in HUD'
$mission = Read-Required 'Assets/Scripts/Runtime/Game/UI/MissionPanel.cs'
Forbid $mission '◷|★' 'unicode icon literals in MissionPanel'
$header = Read-Required 'Assets/Scripts/Runtime/Game/UI/PanelHeader.cs'
Forbid $header '"←"' 'unicode back arrow literal in PanelHeader'
Require $header 'UiIcons\.Back' 'PanelHeader uses icon sprite for back'

$uiBuilder = Read-Required 'Assets/Scripts/Runtime/Game/UI/UiBuilder.cs'
Require $uiBuilder 'public static TextMeshProUGUI CreateText' 'CreateText produces TMP'
Forbid-File 'Assets/Scripts/Runtime/Game/UI/CampPanel.cs'
Forbid-File 'Assets/Scripts/Runtime/Game/UI/ShopPanel.cs'

$bootstrap = Read-Required 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'
foreach ($dead in 'PulseTween', 'FloatTween', 'ClickScaleTween', 'RunCycleTween', 'RunwayScrollTween', 'MarqueeTween', 'SnowfallEmitter', 'SpeedLineEmitter') {
    Forbid $bootstrap "class $dead" "dead tween class $dead still embedded in MainMenuBootstrap"
}
Require-File 'Assets/Scripts/Runtime/Game/UI/LobbyAmbientTweens.cs'

# ── LOD sanity: gameplay-distance thresholds, no invisible empty-renderer band ──
foreach ($builder in 'Assets/Scripts/Editor/PenguinRun/PenguinVisualPrefabBuilder.cs', 'Assets/Scripts/Editor/PenguinRun/ThemeMechanicPrefabBuilder.cs') {
    $builderSrc = Read-Required $builder
    Require $builderSrc 'new LOD\(0\.025f' "gameplay-distance LOD0 threshold in $builder"
    Forbid $builderSrc 'Array\.Empty<Renderer>\(\)' "empty-renderer LOD band in $builder"
}
$pickupBuilder = Read-Required 'Assets/Scripts/Editor/PenguinRun/PenguinVisualPrefabBuilder.cs'
Require $pickupBuilder 'neverCull' 'pickup prefabs opt into never-cull LOD'
Require $pickupBuilder '0\.0001f' 'pickup LOD1 never-cull relative height'
$catalogSrc = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualAssetCatalog.cs'
Require $catalogSrc '1\.85f' 'pickup attach scale boost'
$spawnerSrc = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
Require $spawnerSrc 'SetPickupCoreVisible' 'hide pickup core when custom visual attaches'
foreach ($sample in 'Assets/Resources/PenguinRun/Prefabs/Pickups/fish_snack.prefab', 'Assets/Resources/PenguinRun/Prefabs/Obstacles/ice_lake/standard_high.prefab', 'Assets/Resources/PenguinRun/Prefabs/ThemeMechanics/ice/cracked_plate.prefab', 'Assets/Resources/PenguinRun/Prefabs/Environment/ice_lake/signpost.prefab') {
    $prefabSrc = Read-Required $sample
    Require $prefabSrc 'screenRelativeHeight: 0\.025' "rebuilt LOD0 threshold in $sample"
    Forbid $prefabSrc 'renderers: \[\]' "invisible empty LOD in $sample"
}
$fishPrefab = Read-Required 'Assets/Resources/PenguinRun/Prefabs/Pickups/fish_snack.prefab'
Require $fishPrefab 'screenRelativeHeight: 0\.0001' 'fish_snack never-cull LOD1 threshold'

# ── Docs ─────────────────────────────────────────────────────────────────
$context = Read-Required 'CONTEXT.md'
Require $context '蹲姿判定盒' 'CONTEXT glossary: crouch hitbox'
Require $context 'BossRigAnimator' 'CONTEXT glossary: boss rig animator'
Require $context 'RunnerHudCanvas' 'CONTEXT FramingReference mentions uGUI HUD'
Forbid $context 'IMGUI HUD..`RunnerHud`' 'stale IMGUI HUD description in CONTEXT'
Require-File 'docs/adr/0002-cc0-rigged-boss-models.md'
$design = Read-Required 'docs/CURRENT_DESIGN.md'
Require $design '2026-07-20 建模/特效/判定/页面全面优化' 'CURRENT_DESIGN batch entry'
Forbid $design '将移动端输入阈值改成响应式' 'stale responsive-threshold todo'

if ($failures.Count -gt 0) {
    Write-Host "VerifyVisualJudgmentRevamp FAILED ($($failures.Count)):" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "  - $f" -ForegroundColor Red }
    exit 1
}

Write-Host 'VerifyVisualJudgmentRevamp PASSED' -ForegroundColor Green
exit 0
