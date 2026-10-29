# Static wiring gate for current-theme authority and safe switching.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
function Require([string]$path, [string]$text, [string]$label) {
    $full = Join-Path $ProjectRoot $path
    if (-not (Test-Path -LiteralPath $full) -or -not (Get-Content -LiteralPath $full -Raw -Encoding utf8).Replace("`r`n", "`n").Contains($text)) { $failures.Add($label) }
}
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/RunThemeSessionState.cs' 'SwitchIntervalSeconds = 52f' 'theme state owns the 52 second interval'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/RunThemeSessionState.cs' 'SwitchPending' 'theme state has one pending bit'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'public RunnerMapTheme CurrentTheme' 'controller exposes current gameplay theme'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'routeEventDirector.SetTheme(nextTheme' 'route pool changes with current theme'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'bossSystem.SetTheme(nextTheme)' 'future Boss context changes with current theme'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Routes.cs' 'themeSession.SwitchPending' 'pending switch blocks new route scheduling'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'themeSession.SwitchPending' 'pending switch blocks new mechanic scheduling'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'HasOwnedBossRewardEntities' 'reward tail blocks theme switching'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs' 'TickThemeSwitchClock(dt)' 'theme timer is simulation-state driven'
Require 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' 'game.CanApplyPendingThemeSwitch()' 'theme transition waits for safe boundary'
Require 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' 'worldEvents.Tick(game.World.Distance, eventDt, !game.ThemeSwitchPending)' 'pending switch suppresses new world events'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs' 'public void SetTheme' 'route director preserves run state while swapping pool'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs' 'if (CurrentBoss != null || preludeSpawned) return' 'Boss preview is stable once prelude starts'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'masterySession.SetTheme(nextTheme, world.Distance)' 'mastery ownership changes at gameplay commit'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' "CurrentTheme,`n                config.ThemeMechanicMode" 'mechanic request uses current authority'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'TrackTerrain.SetThemeProfile(nextTheme)' 'terrain switches before spawner commit'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs' 'masterySession.RecordSkillAward(awarded, world.Distance)' 'contract skill uses awarded theme-local delta'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs' 'themeMastery.RunSnapshot, masterySession.Snapshots' 'capture carries every visited theme'
Require 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' 'result.ThemeMastery, result.ThemeMasteries' 'terminal router receives collection'
Require 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' 'if (!game.TryApplyCurrentTheme(nextTheme)) return;' 'presentation follows successful gameplay commit'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs' 'MapTheme = currentTheme' 'Boss selection uses current theme'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs' 'BossThemeTraitCatalog.For(currentTheme)' 'Boss traits use current theme'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs' 'eventOrdinals.TryGetValue(theme' 'route randomness has per-theme ordinals'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs' 'bossRewardEntities.Remove(coin)' 'coin pool reuse releases previous spawn ownership'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs' 'bossRewardEntities.Remove(root)' 'power-up pool reuse releases previous spawn ownership'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.BossContent.cs' 'RegisterBossReward(SpawnSpecificPowerUp' 'only actual Boss reward spawns are registered'
Require 'Assets/Scripts/Editor/PenguinRun/ProjectThemeContinuityValidation.cs' 'ValidateRewards()' 'real spawner reward/reuse regression is available'
# These are wiring assertions only; Test scripts and Unity validator supply behavior evidence.
$game = (Get-Content (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs') -Raw) -replace '(?s)/\*.*?\*/|(?m)//[^\r\n]*', ''
if ($game -match 'nextThemeSwitchTime|game\.TickThemeSwitchClock') { $failures.Add('wall-frame rotation clock must not return') }
$spawner = (Get-Content (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs') -Raw) -replace '(?s)/\*.*?\*/|(?m)//[^\r\n]*', ''
if ($spawner -match 'For\(kind, config\.MapTheme\)|TryAttachObstacle\([^\n]*config\.MapTheme') { $failures.Add('spawn visuals must use current theme') }
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: theme continuity and safe-switch wiring.'
