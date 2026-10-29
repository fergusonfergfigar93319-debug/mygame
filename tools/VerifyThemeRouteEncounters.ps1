# Source-wiring gate. Behavior tests live in TestThemeRouteEncounters.ps1.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
function Require([string]$Path, [string]$Pattern, [string]$Label) {
    $full = Join-Path $ProjectRoot $Path
    if (-not (Test-Path -LiteralPath $full) -or -not (Get-Content -LiteralPath $full -Raw -Encoding utf8).Contains($Pattern)) { $failures.Add($Label) }
}
$route = 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.RouteEncounters.cs'
$controller = 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Routes.cs'
$main = 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
Require $route 'ThemeRouteEncounterCatalog.TryCreate' 'Spawner must consume real catalog'
Require $route 'SpawnStandardObstacle(spawnZ + obstacle.OffsetZ, obstacle.Lane, obstacle.Slide' 'Real route obstacles use existing collision registrations'
Require $route 'SpawnCoin(spawnZ + fish.OffsetZ, fish.Lane, runDistance, fish.BonusY)' 'Fish use existing pickup registry'
Require $route 'FarthestRouteContentZ' 'Start after existing content instead of clearing hazards'
Require $route 'TryRemoveObstacle(obstacle)' 'Cancel uses existing obstacle cleanup authority'
Require $route 'routeReservation.Clear()' 'Cancel releases reservation'
Require $main '!SpawnPaused && !RouteEncounterActive' 'Normal waves cannot consume reserved route interval'
Require $controller 'routeEventDirector.AwaitingChoice || spawner.RouteEncounterActive' 'Mechanics excluded for complete route outcome window'
Require $controller 'routeEventDirector.Cancel(world.Distance)' 'Cancellation does not settle route rewards'
Require $controller 'spawner.CancelRouteEncounter()' 'World-authority cancellation cleans route entities'
Require $controller 'IsBossApproaching(world.Distance, 120f)' 'Boss approach wins before prelude'
Require $controller 'world.GlideTimer > 0f || spawner.HasPendingGlidePickup' 'Pending glide pickups must be included in jump recovery budget'
Require 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' 'game?.Spawner.CancelRouteEncounter();' 'Owner destruction releases route content'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs' 'CancelRoutesForState(to)' 'Death and theme state transitions cancel routes'
Require 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Themes.cs' 'if (bossSystem.BossActive || bossSystem.HasSpawnedPrelude' 'Boss prelude cancels mechanics when route reservation releases'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs' 'ThemeRoutePresentation.ChoiceLabels' 'Target prompts describe the physical action'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs' 'CreateHudCard("Lives", root,' 'Life remains outside Boss-hidden normal statistics'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.RoutePresentation.cs' 'WorldCurveX' 'Optional landmarks follow track curvature'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.RoutePresentation.cs' 'GetComponentsInChildren<Collider>' 'Optional landmarks strip colliders'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.RoutePresentation.cs' 'LandmarkLateralOffset = 10.5f' 'Optional landmarks stay outside the 8.5m corridor'
Require 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.RoutePresentation.cs' 'bounds.extents' 'Landmark mesh footprint, not just its pivot, clears the corridor'
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: theme-route production lifecycle and spawning wiring.'
