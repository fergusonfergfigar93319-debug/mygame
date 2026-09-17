# Static gate for the 2026-09-17 run-feel and in-run UI batch.
#
# What this batch changed and what must not silently regress:
#   1. Every full-screen in-run presentation (flash, Boss edge warning, death hold,
#      countdown, theme-transition curtain) is retained uGUI in RunnerScreenOverlay.
#      The IMGUI OnGUI path in PenguinRunnerGame is gone. IMGUI ran twice per frame
#      and decremented the flash timer inside the draw call, so flash decay depended
#      on how many IMGUI passes happened that frame.
#   2. Feedback recipes (flash / shake / FOV kick / hit-stop / haptic) live in one
#      table, RunFeedbackProfile. The controller reports a RunFeedbackKind; the
#      MonoBehaviour dispatches. No per-case Color literals in the controller.
#   3. Hit-stop scales the simulation dt in the controller and the scenery dt in
#      the game loop. Both must read the same time scale or the road keeps moving
#      while obstacles freeze.
#   4. Near-miss is a real skill action (CollisionSystem -> SkillAction.NearMiss).
#   5. The difficulty score multiplier is applied, not clamped away by Max().
#   6. Haptics are one authority (RunFeedbackProfile.Haptic) with a strength
#      parameter; CollisionSystem.ApplyOutcome no longer pulses on its own.
#   7. In-run HUD uses one frost palette; no neon AccentCyan / TextSecondary /
#      PaperWarm mixed onto the same dark card. Boss HUD vulnerable state is
#      PineGreen, the same "active" hue the lobby uses.
#   8. Symbol glyphs that render as tofu on the dynamic CJK atlas are gone from
#      in-run feedback strings.
#
# ASCII only on purpose. Without a BOM, PowerShell 5.1 reads these files as ANSI
# and non-ASCII comment bytes surface as misleading parse errors.
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

# Strip // comments before matching "forbidden" patterns: the comments that explain
# a rule tend to quote the very token being banned.
function Strip-Comments([string]$content) {
    if ($null -eq $content) { return $null }
    return (($content -split "`n") | Where-Object { $_ -notmatch '^\s*//' }) -join "`n"
}

function Require([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

function Forbid([string]$content, [string]$pattern, [string]$description) {
    if ($null -ne $content -and (Strip-Comments $content) -match $pattern) {
        $failures.Add("Forbidden contract remains: $description")
    }
}

$game       = Read-Required 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
$controller = Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs'
$overlay    = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerScreenOverlay.cs'
$profile    = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunFeedbackProfile.cs'
$kinds      = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunFeedbackKind.cs'
$collision  = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/CollisionSystem.cs'
$skill      = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/SkillScoreTracker.cs'
$router     = Read-Required 'Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs'
$hud        = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs'
$bossHud    = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/BossHudPanel.cs'
$bossSystem = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs'
$audio      = Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunnerAudio.cs'
$deathCard  = Read-Required 'Assets/Scripts/Runtime/Game/UI/RunnerDeathOverlay.cs'

# 1. Retained overlay replaces IMGUI.
Forbid  $game 'void\s+OnGUI\s*\(' 'PenguinRunnerGame still has an OnGUI path'
Forbid  $game '\bGUI\.(DrawTexture|Label|color)\b' 'PenguinRunnerGame still draws through IMGUI'
Forbid  $game '\bGUIStyle\b' 'PenguinRunnerGame still allocates GUIStyle'
Require $game 'RunnerScreenOverlay\.Create\(\)' 'runner creates the retained screen overlay'
Require $game 'screenOverlay\?\.Reset\(\)' 'retry resets the screen overlay'
Require $game 'screenOverlay\?\.Dispose\(\)' 'teardown disposes the screen overlay'
Require $overlay 'GraphicRaycaster' 'overlay explicitly removes its raycaster so flashes never eat HUD taps'
Require $overlay 'Object\.Destroy\(raycaster\)' 'overlay raycaster is destroyed'
Require $overlay 'public void Flash\(' 'overlay exposes one-shot flash'
Require $overlay 'public void SetHold\(' 'overlay exposes the death hold curtain'
Require $overlay 'public void ShowCountdown\(' 'overlay renders the entrance countdown'
Require $overlay 'public void SetThemeTransition\(' 'overlay renders the theme curtain'
Require $overlay 'Time\.unscaledTime' 'overlay pulses on unscaled time'
Require $game 'screenOverlay\.Tick\(Time\.unscaledDeltaTime\)' 'flash decay uses unscaled frame time exactly once per frame'

# 2. One feedback recipe table.
Require $profile 'readonly struct RunFeedbackProfile' 'feedback profile is a value type'
Require $profile 'public static RunFeedbackProfile For\(RunFeedbackKind kind\)' 'profile lookup by kind'
Require $profile 'HitStopTimeScale\s*=\s*0\.3f' 'hit-stop time scale pinned at 0.3'
foreach ($kind in 'PerfectDodge','NearMiss','ObstacleDamage','ObstacleDeath','BossHit','BossDeath','DefenseBlock','DashStart') {
    Require $profile "RunFeedbackKind\.$kind\s*=>" "profile covers RunFeedbackKind.$kind"
    Require $kinds "\b$kind\b" "RunFeedbackKind.$kind exists"
}
Require $controller 'System\.Action<RunFeedbackKind> onFeedback' 'controller reports feedback kinds, not colours'
Forbid  $controller 'onScreenFlash\?\.Invoke\(new Color\(1f, 0\.35f' 'controller still hard-codes the damage flash colour'
Require $game 'private void HandleRunFeedback\(RunFeedbackKind kind\)' 'runner owns feedback dispatch'
Require $game 'RunFeedbackProfile\.For\(kind\)' 'dispatch reads the profile table'

# 3. Hit-stop is applied to both simulation and scenery.
Require $controller 'dt \*= RunFeedbackProfile\.HitStopTimeScale' 'controller scales simulation dt during hit-stop'
Require $controller 'public float SimulationTimeScale' 'controller exposes the current time scale'
Require $game 'AnimateScenery\(dt \* game\.SimulationTimeScale' 'scenery scroll follows the same time scale'
Require $controller 'hitStopTimer = 0f;' 'hit-stop timer resets with the run'

# 4. Near-miss is a skill action.
Require $skill '\bNearMiss\b' 'SkillAction.NearMiss exists'
Require $collision 'System\.Action onNearMiss' 'collision system reports near misses'
Require $collision 'NearMissBandX\s*=\s*0\.55f' 'near-miss band pinned at 0.55m'
Require $collision 'overlapsNow && onNearMiss != null' 'near-miss only counts real same-frame overlap, not tunnel compensation'
Require $controller 'AwardSkill\(SkillAction\.NearMiss' 'controller awards near-miss skill score'
Require $audio 'public void PlayNearMiss\(\)' 'near-miss has an audio cue'

# 5. Difficulty multiplier is real.
Forbid  $router 'score = Mathf\.Max\(score, Mathf\.RoundToInt\(baseScore \* scoreMul\)\)' 'difficulty score multiplier is still clamped away'
Require $router 'score = Mathf\.RoundToInt\(baseScore \* scoreMul\);' 'difficulty score multiplier applies as written'

# 6. Haptics: one authority with strength.
Require $game 'private void PulseHaptic\(float strength01\)' 'haptic pulse takes a strength'
Require $game 'config\.HapticIntensity' 'haptic pulse honours the player setting'
Forbid  $collision 'case HitOutcomeKind\.Damage:\s*\n\s*EventBus\.Emit\(GameEvents\.PlayerHurt\);\s*\n\s*pulseHaptic\?\.Invoke\(\);' 'collision system still pulses haptics on damage itself'
Forbid  $controller 'EventBus\.Emit\(GameEvents\.PlayerHurt\);\s*\n\s*pulseHaptic\?\.Invoke\(\);' 'controller still pulses haptics on Boss damage itself'

# 7. One in-run palette.
Forbid  $hud 'UiTheme\.AccentCyan' 'runner HUD still uses the neon cyan accent'
Forbid  $hud 'UiTheme\.TextSecondary' 'runner HUD still uses the legacy light-on-dark secondary text'
Forbid  $hud 'UiTheme\.PaperWarm' 'runner HUD paints paper-cream text on dark cards'
Require $hud 'CardStroke' 'runner HUD cards share one stroke token'
Require $hud 'new Vector2\(40f, 40f\)' 'HUD hearts are at least 40px'
Require $hud '"Resume", pauseRoot, "[^"]+", UiTheme\.CoralAction' 'pause primary action speaks the lobby CTA language'
Forbid  $bossHud 'UiTheme\.Success' 'Boss HUD bypasses the palette remap with neon success green'
Require $deathCard 'addBorder: false, addShadow: true' 'death card has a single stitched outline, not a doubled neon one'

# 8. No tofu glyphs in in-run feedback strings.
foreach ($pair in @(
    @{ File = $controller; Name = 'RunnerGameController' },
    @{ File = $bossSystem; Name = 'BossSystem' },
    @{ File = $collision;  Name = 'CollisionSystem' },
    @{ File = $hud;        Name = 'RunnerHudCanvas' })) {
    $code = Strip-Comments $pair.File
    # U+26A0 warning, U+26A1 bolt, U+2190/U+2192 arrows, U+2B05/U+27A1 heavy arrows,
    # plus the high-surrogate range that every emoji starts with.
    if ($null -ne $code -and $code -match '[\u26A0\u26A1\u2190\u2192\u2B05\u27A1\uD83C-\uDBFF]') {
        $failures.Add("$($pair.Name) contains a symbol glyph the dynamic CJK atlas renders as tofu")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}

Write-Host 'PASS: run-feel and in-run UI batch contracts hold.'
