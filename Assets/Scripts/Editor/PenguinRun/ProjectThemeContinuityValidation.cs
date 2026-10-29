#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using PenguinRun.Core;
using PenguinRun.Game;
using PenguinRun.Game.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PenguinRun.Editor
{
    /// <summary>Real controller/Boss/spawner integration in an isolated Play Mode scene.</summary>
    public static class ProjectThemeContinuityValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static void Put(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
        private static void Check(bool value, string message)
        {
            checks++;
            if (!value) throw new InvalidOperationException(message);
        }

        public static void ValidateFromCommandLine()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            try { ValidateInPlayMode(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        [MenuItem("Tools/Penguin Run/Validation/Theme Continuity (Play Mode)")]
        public static void ValidateInPlayMode()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            // The production constructor reads this store. Refuse its initial migration here;
            // validation must not initialize or settle the user's progression.
            if (PlayerSave.ReadGrowthInt("theme_mastery_schema", 0) <= 0)
                throw new InvalidOperationException("Requires an already initialized mastery profile; validator will not write saves.");
            var originalScene = SceneManager.GetActiveScene();
            var originalRandom = UnityEngine.Random.state;
            var profileField = typeof(TrackTerrain).GetField("_activeProfile", BindingFlags.Static | BindingFlags.NonPublic);
            var originalProfile = profileField.GetValue(null);
            var scene = SceneManager.CreateScene("ThemeContinuity-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(scene);
            checks = 0;
            try
            {
                ValidateClockAndBoundary();
                ValidateCommitAndReset();
                ValidateBossCache();
                ValidateRewards();
                ValidateDaily();
                Debug.Log($"[ProjectThemeContinuityValidation] PASS: {checks} real-controller assertions.");
            }
            finally
            {
                UnityEngine.Random.state = originalRandom;
                profileField.SetValue(null, originalProfile);
                SceneManager.SetActiveScene(originalScene);
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static void ValidateClockAndBoundary()
        {
            var game = Create();
            game.TickRunningSimulation(.05f);
            Check(Mathf.Abs(game.ThemeSwitchElapsedSeconds - .05f) < .001f, "running advances clock");
            game.SetPaused(true, false); game.TickRunningSimulation(.05f);
            Check(Mathf.Abs(game.ThemeSwitchElapsedSeconds - .05f) < .001f, "pause freezes clock");
            game.SetPaused(false, false);
            Put(game, "waitingForGuidedReaction", true); game.TickRunningSimulation(.05f);
            Check(Mathf.Abs(game.ThemeSwitchElapsedSeconds - .05f) < .001f, "tutorial freezes clock");
            Put(game, "waitingForGuidedReaction", false);
            Put(game, "upgradeOfferPending", true); game.TickRunningSimulation(.05f);
            Check(Mathf.Abs(game.ThemeSwitchElapsedSeconds - .05f) < .001f, "upgrade wait freezes clock");
            Put(game, "upgradeOfferPending", false);
            var state = Get<RunThemeSessionState>(game, "themeSession");
            state.RequestSwitch();
            Check(game.CanApplyPendingThemeSwitch(), "empty running track is safe");
            Put(game.Boss, "nextBossDistance", game.World.Distance + 180f);
            Check(!game.TryBeginThemeTransition(), "180m boss guard wins");
            Put(game.Boss, "nextBossDistance", 99999f);
            var route = Get<RouteEventDirector>(game, "routeEventDirector");
            route.Tick(220);
            Check(!game.CanApplyPendingThemeSwitch(), "visible choice blocks switch");
            route.Cancel(250);
            var reservation = Get<RouteEncounterReservation>(game.Spawner, "routeReservation");
            reservation.TryReserve(100, 120);
            Check(!game.CanApplyPendingThemeSwitch(), "route tail blocks switch");
            reservation.Clear();
            var runtime = Get<ThemeMechanicRuntime>(game, "mechanicRuntime");
            runtime.Begin(new ThemeMechanicRequest(true, ThemeMechanicCatalog.ForTheme(game.CurrentTheme), ThemeMechanicIntensity.Low, false));
            for (var i = 0; i < 6; i++)
            {
                Check(!game.CanApplyPendingThemeSwitch(), "nonterminal mechanic blocks switch");
                runtime.Tick(i == 0 ? 2f : .1f);
            }
            runtime.Cancel(ThemeMechanicCancelReason.MissingCriticalAsset); runtime.Tick(0);
            Check(game.TryBeginThemeTransition(), "released content permits transition");
        }

        private static void ValidateCommitAndReset()
        {
            var game = Create();
            var state = Get<RunThemeSessionState>(game, "themeSession");
            var routes = Get<RouteEventDirector>(game, "routeEventDirector");
            var mechanics = Get<MapMechanicDirector>(game, "mechanicDirector");
            routes.Tick(220); routes.Choose(true, 250);
            mechanics.RecordBeat(true, false);
            var debt = mechanics.RewardDebt;
            var random = Get<IRunRandom>(mechanics, "random");
            var distance = game.World.Distance; var lives = game.World.Lives;
            state.RequestSwitch(); Check(game.TryBeginThemeTransition(), "begin commit");
            Check(!game.TryApplyCurrentTheme((RunnerMapTheme)99), "invalid theme rejected");
            Check(game.TryApplyCurrentTheme(RunnerMapTheme.CedarRuins), "commit cedar");
            Check(game.CurrentTheme == routes.CurrentTheme && game.CurrentTheme == game.Boss.CurrentTheme
                && Get<RunnerMapTheme>(game.Spawner, "routeTheme") == game.CurrentTheme, "all consumers agree");
            Check(!game.TryApplyCurrentTheme(RunnerMapTheme.CedarRuins), "duplicate commit no-op");
            Check(routes.CompletedCount == 1 && routes.RiskyCompletedCount == 1
                && mechanics.RewardDebt == debt && ReferenceEquals(random, Get<IRunRandom>(mechanics, "random")), "run-global progress/random preserved");
            Check(game.World.Distance == distance && game.World.Lives == lives && !game.ThemeSwitchPending, "resources preserved and request consumed");
            game.CompleteThemeTransition();
            Check(Get<ThemeMasterySessionLedger>(game, "masterySession").Snapshots.Count == 2, "two visited mastery trackers");
            game.ResetRun(false);
            Check(game.CurrentTheme == RunnerMapTheme.IceLakeEcho && routes.CurrentTheme == game.CurrentTheme
                && game.Boss.CurrentTheme == game.CurrentTheme && Get<RunnerMapTheme>(game.Spawner, "routeTheme") == game.CurrentTheme, "reset restores every authority");
            Check(game.CaptureResult().ThemeMasteries.Count == 1 && routes.CompletedCount == 0, "retry drops previous attempt ledger/counters");
        }

        private static void ValidateBossCache()
        {
            var game = Create(); var boss = game.Boss;
            var first = boss.NextBossDefinition;
            Check(first != null && ReferenceEquals(first, boss.NextBossDefinition), "preview cached");
            Check(boss.SetTheme(RunnerMapTheme.CedarRuins) && Get<BossDefinition>(boss, "nextBossDef") == null, "unannounced cache invalidated");
            var announced = boss.NextBossDefinition;
            boss.MarkPreludeSpawned();
            Check(!boss.SetTheme(RunnerMapTheme.AuroraField) && ReferenceEquals(announced, boss.NextBossDefinition), "prelude definition frozen");
            Get<RunThemeSessionState>(game, "themeSession").RequestSwitch();
            Check(!game.TryBeginThemeTransition(), "prelude blocks controller transition");
            boss.Reset();
            Check(boss.CurrentTheme == RunnerMapTheme.IceLakeEcho && !boss.HasSpawnedPrelude, "boss reset restores opening theme");
        }

        private static void ValidateRewards()
        {
            var game = Create(); var spawner = game.Spawner;
            var unrelated = (GameObject)Call(spawner, "SpawnSpecificPowerUp", 600f, 0f, PowerUpKind.Magnet);
            spawner.SpawnBossReward(0, game.Boss.NextBossDefinition);
            var owned = Get<List<GameObject>>(spawner, "bossRewardEntities");
            Check(spawner.HasOwnedBossRewardEntities && !owned.Contains(unrelated), "only generated reward batch is owned");
            Get<RunThemeSessionState>(game, "themeSession").RequestSwitch();
            Check(!game.CanApplyPendingThemeSwitch(), "reward tail blocks transition");
            var count = owned.Count;
            spawner.Tick(0, 20, 0);
            Check(owned.Count == count && spawner.HasOwnedBossRewardEntities, "pause preserves reward window");
            foreach (var entity in owned.ToArray()) entity.SetActive(false);
            Check(!spawner.HasOwnedBossRewardEntities && game.CanApplyPendingThemeSwitch(), "collected batch releases despite unrelated pickup");
            spawner.SpawnBossReward(0, game.Boss.NextBossDefinition);
            foreach (var entity in owned.ToArray()) entity.transform.position = new Vector3(0, 0, -1000);
            Check(!spawner.HasOwnedBossRewardEntities, "recycled tail releases");
            spawner.SpawnBossReward(0, game.Boss.NextBossDefinition);
            // Force both bounded pools to recycle references before the ownership getter runs.
            for (var i = 0; i < 250; i++) Call(spawner, "SpawnCoin", 200f, RunnerLane.Center, 0f, 0f, false);
            for (var i = 0; i < 100; i++) Call(spawner, "SpawnSpecificPowerUp", 200f, 0f, PowerUpKind.Magnet);
            Check(!spawner.HasOwnedBossRewardEntities, "pool reuse cannot transfer stale ownership");
            var definition = RouteEventCatalog.For(RunnerMapTheme.IceLakeEcho)[0];
            spawner.SpawnRouteEventOutcome(new RouteEventResolution(true, definition.Id, definition.Kind,
                false, definition.SafeOutcome), 0, 50);
            var routeFish = Get<List<GameObject>>(spawner, "routeFish").ToArray();
            Check(routeFish.Length == 6, "route registers each fish when pool count stays at capacity");
            spawner.CancelRouteEncounter();
            foreach (var fish in routeFish)
                Check(!fish.activeSelf && !spawner.coins.Contains(fish), "route cancellation removes recycled owned fish");
        }

        private static void ValidateDaily()
        {
            foreach (RunnerMapTheme theme in Enum.GetValues(typeof(RunnerMapTheme)))
            {
                var game = Create(theme, true);
                Get<RunThemeSessionState>(game, "themeSession").Tick(9999, true);
                Check(!game.ThemeSwitchPending && !game.TryBeginThemeTransition() && game.CurrentTheme == theme, "daily stays in each opening theme");
            }
        }

        private static RunnerGameController Create(RunnerMapTheme theme = RunnerMapTheme.IceLakeEcho, bool daily = false)
        {
            var root = new GameObject("ThemeContinuityFixture");
            var game = new RunnerGameController(new RunnerTuning(), new RunnerSessionConfig { MapTheme = theme, Daily = daily },
                root.AddComponent<RunnerAudio>(), root.transform, new Presentation(root.transform), root.transform, null, null, null);
            Get<GameStateMachine>(game, "stateMachine").ForceTransitionTo(GameStateMachine.State.Running);
            return game;
        }

        private sealed class Presentation : IPlayerCharacterPresentation
        {
            public Transform Root { get; }
            public Presentation(Transform root) { Root = root; }
            public void Tick(float dt, in RunnerAvatarState state) { }
            public void TriggerHurt() { }
            public void TriggerPerfectDodge() { }
            public void TriggerCelebration() { }
            public void TriggerInteraction(RunnerAvatarInteraction interaction) { }
        }
    }
}
#endif
