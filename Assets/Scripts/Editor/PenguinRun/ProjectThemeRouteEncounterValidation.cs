#if UNITY_EDITOR
using System;
using System.Reflection;
using PenguinRun.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PenguinRun.Editor
{
    /// <summary>Real registered entities in an isolated additive scene; never reads/writes a save.</summary>
    public static class ProjectThemeRouteEncounterValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;

        [MenuItem("Tools/Penguin Run/Validation/Theme Route Encounters (Play Mode)")]
        public static void ValidateInPlayMode()
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("Enter Play Mode before running route encounter validation.");
            var originalScene = SceneManager.GetActiveScene();
            var originalRandom = UnityEngine.Random.state;
            var testScene = SceneManager.CreateScene("ThemeRouteValidation-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(testScene);
            checks = 0;
            try
            {
                foreach (var theme in new[] { RunnerMapTheme.IceLakeEcho, RunnerMapTheme.CedarRuins })
                foreach (var risky in new[] { false, true }) ValidateRoute(theme, risky);
                ValidateOnboardingAndFallback();
                Debug.Log($"[ProjectThemeRouteEncounterValidation] PASS: {checks} real-spawner assertions.");
            }
            finally
            {
                UnityEngine.Random.state = originalRandom;
                SceneManager.SetActiveScene(originalScene);
                SceneManager.UnloadSceneAsync(testScene);
            }
        }

        private static void ValidateRoute(RunnerMapTheme theme, bool risky)
        {
            var config = new RunnerSessionConfig { MapTheme = theme };
            var world = new RunnerWorldTuning();
            var director = new SegmentWaveDirector(theme);
            var spawner = new SegmentSpawner(world, new RunnerPowerUpTuning(), 3f, config, director, 0.9f);
            var definition = RouteEventCatalog.For(theme)[0];
            var resolution = new RouteEventResolution(true, definition.Id, definition.Kind, risky,
                risky ? definition.RiskOutcome : definition.SafeOutcome);
            // Seed a real pre-existing registered obstacle; a route must leave it untouched.
            typeof(SegmentSpawner).GetMethod("SpawnStandardObstacle", Private).Invoke(spawner,
                new object[] { 200f, RunnerLane.Left, false, director.CurrentPalette, 0f });
            var existing = spawner.obstacles[0];
            spawner.SpawnRouteEventOutcome(resolution, 0f, 50f);
            var expectedObstacles = risky ? 3 : 0;
            var expectedFish = risky && theme == RunnerMapTheme.CedarRuins ? 0 : 6;
            Check(spawner.RouteEncounterActive, "route reservation active");
            Check(spawner.obstacles.Count == expectedObstacles + 1 && spawner.coins.Count == expectedFish,
                "real entity counts match route plan");
            for (var i = 1; i < spawner.obstacles.Count; i++)
            {
                var obstacle = spawner.obstacles[i];
                var expected = theme == RunnerMapTheme.IceLakeEcho
                    ? ObstacleColliderSpec.SmallJumpable : ObstacleColliderSpec.LowSlideGate;
                Check(spawner.obstacleSpecs.TryGetValue(obstacle, out var actual) && actual.Equals(expected),
                    "real collider spec matches jump/slide");
                Check(obstacle.transform.position.z > 212f && obstacle.transform.position.x == 3f,
                    "entry does not overlap existing content; left and center stay escapable");
            }
            var waveCount = Get<int>(director, "wavesLeftInSegment");
            spawner.Tick(0f, 32f, 0f);
            Check(existing.transform.position.z == 200f, "zero dt freezes spawner");
            if (spawner.coins.Count > 0)
            {
                var collected = spawner.coins[0];
                collected.SetActive(false);
                spawner.coins.RemoveAt(0); // PickupSystem's production ownership contract.
            }
            spawner.Tick(0.02f, 10f, 0.2f);
            Check(Get<int>(director, "wavesLeftInSegment") == waveCount, "route does not consume normal waves");
            spawner.CancelRouteEncounter();
            spawner.CancelRouteEncounter();
            Check(!spawner.RouteEncounterActive && !spawner.HasRouteChoiceMarker && spawner.coins.Count == 0,
                "repeat cancellation clears reservation, markers and owned fish");
            Check(spawner.obstacles.Count == 1 && spawner.obstacles[0] == existing,
                "cancellation preserves unrelated registered obstacle");
            spawner.Tick(1f, 10f, 10.2f);
            Check(director.LastConsumeWasFirstWave, "normal scheduling resumes after cancellation");
            spawner.SpawnRouteEventOutcome(resolution, 10.2f, 50f);
            spawner.ApplyTheme(theme == RunnerMapTheme.IceLakeEcho ? RunnerMapTheme.CedarRuins : RunnerMapTheme.IceLakeEcho);
            Check(!spawner.RouteEncounterActive && spawner.obstacles.Count == 0 && spawner.coins.Count == 0,
                "theme transition clears active encounter");
            spawner.Reset();
        }

        private static void ValidateOnboardingAndFallback()
        {
            var config = new RunnerSessionConfig();
            typeof(RunnerSessionConfig).GetField("<IsOnboarding>k__BackingField", Private).SetValue(config, true);
            var director = new SegmentWaveDirector(config.MapTheme, true);
            var spawner = new SegmentSpawner(new RunnerWorldTuning(), new RunnerPowerUpTuning(), 3f, config, director, 0.9f);
            var definition = RouteEventCatalog.For(config.MapTheme)[0];
            spawner.SpawnRouteEventOutcome(new RouteEventResolution(true, definition.Id, definition.Kind, true, definition.RiskOutcome), 500f, 50f);
            Check(!spawner.RouteEncounterActive && spawner.obstacles.Count == 0 && spawner.coins.Count == 0,
                "onboarding cannot spawn outcome even through direct call");
            typeof(RunnerSessionConfig).GetField("<IsOnboarding>k__BackingField", Private).SetValue(config, false);
            var legacy = RouteEventCatalog.For(config.MapTheme)[1];
            spawner.SpawnRouteEventOutcome(new RouteEventResolution(true, legacy.Id, legacy.Kind, false, legacy.SafeOutcome), 500f, 50f);
            Check(!spawner.RouteEncounterActive && spawner.obstacles.Count == 0 && spawner.coins.Count == 12,
                "non-target FishRibbon retains legacy pickup count");
            spawner.Reset();
        }

        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Check(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException("[ProjectThemeRouteEncounterValidation] FAIL: " + message);
        }
    }
}
#endif
