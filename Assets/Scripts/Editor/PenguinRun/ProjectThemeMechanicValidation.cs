#if UNITY_EDITOR
using System.Collections.Generic;
using PenguinRun.Game;
using PenguinRun.Game.Growth;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ProjectThemeMechanicValidation
    {
        [MenuItem("PenguinRun/Theme Mechanics/Validate Phase Four")]
        public static void ValidateFromMenu()
        {
            var passed = RunNow(out var report);
            if (passed)
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }
        }

        public static bool RunNow(out string report)
        {
            var failures = new List<string>();
            ValidateCatalog(failures);
            ValidateDirectorDeterminism(failures);
            ValidateDirectorReset(failures);
            ValidateDirectorCooldown(failures);
            ValidateDirectorExclusions(failures);
            ValidateMechanicLifecycle(failures);
            ValidateMechanicCancellation(failures);
            ValidateIcePatterns(failures);
            ValidateAllThemePatterns(failures);
            ValidateMechanicPool(failures);
            ValidateMasteryGrades(failures);
            ValidateResonance(failures);
            ValidateJourneyContracts(failures);
            ValidateRunSeeds(failures);
            ValidateThemeMasteryPersistence(failures);

            report = failures.Count == 0
                ? "PASS: six definitions, 10,000 director beats, 12 patterns/65 beats, pool reset, five contracts, and resonance are valid."
                : "FAIL:\n" + string.Join("\n", failures);
            return failures.Count == 0;
        }

        public static void ValidateFromCommandLine()
        {
            var passed = RunNow(out var report);
            if (passed)
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }

            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void ValidateCatalog(List<string> failures)
        {
            var definitions = ThemeMechanicCatalog.Definitions;
            if (definitions.Length != 6)
            {
                failures.Add($"Catalog contains {definitions.Length} definitions; expected 6.");
            }

            var ids = new HashSet<ThemeMechanicId>();
            var themes = new HashSet<RunnerMapTheme>();
            foreach (var definition in definitions)
            {
                if (!ids.Add(definition.Id))
                {
                    failures.Add($"Duplicate mechanic id: {definition.Id}.");
                }

                if (!themes.Add(definition.Theme))
                {
                    failures.Add($"Duplicate mechanic theme: {definition.Theme}.");
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName)
                    || string.IsNullOrWhiteSpace(definition.ActionHint))
                {
                    failures.Add($"{definition.Id} is missing readable presentation text.");
                }

                if (string.IsNullOrWhiteSpace(definition.ApparatusA)
                    || string.IsNullOrWhiteSpace(definition.ApparatusB)
                    || string.IsNullOrWhiteSpace(definition.Signature))
                {
                    failures.Add($"{definition.Id} is missing an apparatus resource path.");
                }

                if (definition.TelegraphSeconds < 2f)
                {
                    failures.Add($"{definition.Id} telegraph is shorter than two seconds.");
                }

                if (definition.ActiveSeconds < 6f || definition.ActiveSeconds > 12f)
                {
                    failures.Add($"{definition.Id} active duration is outside [6, 12].");
                }

                if (definition.RewardSeconds < 3f || definition.RewardSeconds > 6f)
                {
                    failures.Add($"{definition.Id} reward duration is outside [3, 6].");
                }

                if (definition.BaseWeight <= 0f)
                {
                    failures.Add($"{definition.Id} has a non-positive weight.");
                }

                if (!ThemeMechanicCatalog.TryForTheme(definition.Theme, out var resolved)
                    || resolved.Id != definition.Id)
                {
                    failures.Add($"{definition.Id} cannot be resolved by theme.");
                }
            }

            ValidateModePolicy(ThemeMechanicMode.Story, 150f, 3, failures);
            ValidateModePolicy(ThemeMechanicMode.Endless, 100f, int.MaxValue, failures);
            ValidateModePolicy(ThemeMechanicMode.Daily, 120f, 8, failures);
        }

        private static void ValidateModePolicy(
            ThemeMechanicMode mode,
            float expectedFirstDistance,
            int expectedMaximum,
            List<string> failures)
        {
            var policy = ThemeMechanicCatalog.PolicyFor(mode);
            if (!Mathf.Approximately(policy.FirstDistance, expectedFirstDistance)
                || policy.MaxPerRun != expectedMaximum
                || policy.CooldownBeats != 3
                || policy.BaseChance <= 0f
                || !policy.AllowSignature)
            {
                failures.Add($"{mode} mode policy is invalid.");
            }
        }

        private static void ValidateDirectorDeterminism(List<string> failures)
        {
            var first = new MapMechanicDirector(ThemeMechanicMode.Daily, 20260716);
            var second = new MapMechanicDirector(ThemeMechanicMode.Daily, 20260716);
            for (var beat = 0; beat < 500; beat++)
            {
                var dangerous = beat % 3 == 0;
                var rewarding = beat % 5 == 0;
                first.RecordBeat(dangerous, rewarding);
                second.RecordBeat(dangerous, rewarding);
                var context = StandardContext(ThemeMechanicMode.Daily, 200f + beat * 12f);
                var firstScheduled = first.TrySchedule(context, out var firstRequest);
                var secondScheduled = second.TrySchedule(context, out var secondRequest);
                if (firstScheduled != secondScheduled
                    || firstRequest.IsValid != secondRequest.IsValid
                    || firstRequest.Definition.Id != secondRequest.Definition.Id
                    || firstRequest.Intensity != secondRequest.Intensity
                    || firstRequest.Signature != secondRequest.Signature)
                {
                    failures.Add($"Director determinism diverged at beat {beat}.");
                    return;
                }
            }
        }

        private static void ValidateDirectorCooldown(List<string> failures)
        {
            var director = new MapMechanicDirector(ThemeMechanicMode.Endless, 20260716);
            var lastScheduledBeat = -1000;
            var scheduled = 0;
            var maximumGap = 0;
            for (var beat = 0; beat < 10000; beat++)
            {
                director.RecordBeat(beat % 4 == 0, beat % 7 == 0);
                if (!director.TrySchedule(StandardContext(ThemeMechanicMode.Endless, 200f + beat * 3f), out _))
                {
                    continue;
                }

                var gap = beat - lastScheduledBeat;
                if (lastScheduledBeat >= 0 && gap <= 3)
                {
                    failures.Add($"Director scheduled mechanics with only {gap - 1} intervening beats.");
                    return;
                }

                if (lastScheduledBeat >= 0)
                {
                    maximumGap = Mathf.Max(maximumGap, gap);
                }

                lastScheduledBeat = beat;
                scheduled++;
            }

            if (scheduled < 100 || maximumGap > 180)
            {
                failures.Add($"Director reward pacing starved: {scheduled} events, maximum gap {maximumGap}.");
            }

            var story = new MapMechanicDirector(ThemeMechanicMode.Story, 42);
            for (var beat = 0; beat < 500; beat++)
            {
                story.RecordBeat(false, false);
                var distance = beat;
                if (!story.TrySchedule(StandardContext(ThemeMechanicMode.Story, distance), out var request))
                {
                    continue;
                }

                if (distance < 150f || request.Intensity != ThemeMechanicIntensity.Low || request.Signature)
                {
                    failures.Add("First Story mechanic violated tutorial distance or low-intensity rules.");
                }
                break;
            }
        }

        private static void ValidateDirectorReset(List<string> failures)
        {
            const int beats = 200;
            var scheduled = new bool[beats];
            var intensities = new ThemeMechanicIntensity[beats];
            var director = new MapMechanicDirector(ThemeMechanicMode.Daily, 314159);
            for (var beat = 0; beat < beats; beat++)
            {
                director.RecordBeat(beat % 3 == 0, beat % 8 == 0);
                scheduled[beat] = director.TrySchedule(
                    StandardContext(ThemeMechanicMode.Daily, 200f + beat * 9f),
                    out var request);
                intensities[beat] = request.Intensity;
            }

            director.Reset();
            for (var beat = 0; beat < beats; beat++)
            {
                director.RecordBeat(beat % 3 == 0, beat % 8 == 0);
                var replayScheduled = director.TrySchedule(
                    StandardContext(ThemeMechanicMode.Daily, 200f + beat * 9f),
                    out var replayRequest);
                if (scheduled[beat] != replayScheduled || intensities[beat] != replayRequest.Intensity)
                {
                    failures.Add($"Director reset did not restore its seeded stream at beat {beat}.");
                    return;
                }
            }
        }

        private static void ValidateDirectorExclusions(List<string> failures)
        {
            ValidateExcludedContext("Boss", new ThemeMechanicContext(
                RunnerMapTheme.IceLakeEcho, ThemeMechanicMode.Endless, 900f, 3, 20f,
                true, false, false, 0.5f), failures);
            ValidateExcludedContext("route event", new ThemeMechanicContext(
                RunnerMapTheme.IceLakeEcho, ThemeMechanicMode.Endless, 900f, 3, 20f,
                false, true, false, 0.5f), failures);
            ValidateExcludedContext("theme transition", new ThemeMechanicContext(
                RunnerMapTheme.IceLakeEcho, ThemeMechanicMode.Endless, 900f, 3, 20f,
                false, false, true, 0.5f), failures);
        }

        private static void ValidateExcludedContext(
            string label,
            ThemeMechanicContext context,
            List<string> failures)
        {
            var director = new MapMechanicDirector(ThemeMechanicMode.Endless, 7);
            for (var beat = 0; beat < 100; beat++)
            {
                director.RecordBeat(false, false);
                if (director.TrySchedule(context, out _))
                {
                    failures.Add($"Director scheduled during {label}.");
                    return;
                }
            }
        }

        private static ThemeMechanicContext StandardContext(ThemeMechanicMode mode, float distance)
        {
            return new ThemeMechanicContext(
                RunnerMapTheme.IceLakeEcho,
                mode,
                distance,
                3,
                20f,
                false,
                false,
                false,
                0.65f);
        }

        private static void ValidateMechanicLifecycle(List<string> failures)
        {
            var runtime = new ThemeMechanicRuntime();
            var definition = ThemeMechanicCatalog.ForTheme(RunnerMapTheme.IceLakeEcho);
            var request = new ThemeMechanicRequest(
                true,
                definition,
                ThemeMechanicIntensity.High,
                false);
            if (!runtime.Begin(request) || runtime.Snapshot.Phase != ThemeMechanicPhase.Telegraph)
            {
                failures.Add("Lifecycle did not begin in Telegraph.");
                return;
            }

            var phases = new[]
            {
                ThemeMechanicPhase.Active,
                ThemeMechanicPhase.Resolve,
                ThemeMechanicPhase.Reward,
                ThemeMechanicPhase.Cooldown,
                ThemeMechanicPhase.Idle,
            };
            foreach (var expected in phases)
            {
                if (!AdvanceToPhase(runtime, expected))
                {
                    failures.Add($"Lifecycle did not reach {expected}.");
                    return;
                }

                if (expected == ThemeMechanicPhase.Active)
                {
                    runtime.RecordKeypoint(true);
                    if (runtime.CompletedKeypoints != 1 || runtime.PerfectKeypoints != 1)
                    {
                        failures.Add("Active keypoint progress was not recorded.");
                    }
                }
                else if (!IsIdentity(runtime.Snapshot.Modifiers))
                {
                    failures.Add($"Lifecycle retained movement modifiers during {expected}.");
                }
            }
        }

        private static void ValidateMechanicCancellation(List<string> failures)
        {
            var reasons = new[]
            {
                ThemeMechanicCancelReason.Boss,
                ThemeMechanicCancelReason.RouteEvent,
                ThemeMechanicCancelReason.ThemeTransition,
                ThemeMechanicCancelReason.Timeout,
                ThemeMechanicCancelReason.MissingCriticalAsset,
            };
            foreach (var reason in reasons)
            {
                var runtime = new ThemeMechanicRuntime();
                var request = new ThemeMechanicRequest(
                    true,
                    ThemeMechanicCatalog.ForTheme(RunnerMapTheme.SkyFlight),
                    ThemeMechanicIntensity.Signature,
                    true);
                runtime.Begin(request);
                if (!AdvanceToPhase(runtime, ThemeMechanicPhase.Active))
                {
                    failures.Add($"Cancellation setup failed for {reason}.");
                    continue;
                }

                runtime.Cancel(reason);
                if (runtime.Snapshot.Phase != ThemeMechanicPhase.Cancelled
                    || runtime.LastCancelReason != reason
                    || !IsIdentity(runtime.Snapshot.Modifiers))
                {
                    failures.Add($"Cancellation {reason} did not restore identity immediately.");
                    continue;
                }

                runtime.Tick(0f);
                if (runtime.Snapshot.Phase != ThemeMechanicPhase.Idle
                    || !IsIdentity(runtime.Snapshot.Modifiers))
                {
                    failures.Add($"Cancellation {reason} did not clear to Idle.");
                }
            }
        }

        private static bool AdvanceToPhase(ThemeMechanicRuntime runtime, ThemeMechanicPhase phase)
        {
            for (var step = 0; step < 300; step++)
            {
                runtime.Tick(0.1f);
                if (runtime.Snapshot.Phase == phase)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsIdentity(ThemeMechanicModifiers modifiers)
        {
            return Mathf.Approximately(modifiers.LaneResponseMultiplier, 1f)
                && Mathf.Approximately(modifiers.LaneDriftLanesPerSecond, 0f)
                && Mathf.Approximately(modifiers.GravityMultiplier, 1f)
                && Mathf.Approximately(modifiers.GlideDurationMultiplier, 1f)
                && Mathf.Approximately(modifiers.Visibility01, 1f);
        }

        private static void ValidateIcePatterns(List<string> failures)
        {
            var normal = ThemeMechanicPatternCatalog.For(ThemeMechanicId.IceInertia, false);
            var signature = ThemeMechanicPatternCatalog.For(ThemeMechanicId.IceInertia, true);
            if (normal.Length != 5 || signature.Length != 5)
            {
                failures.Add($"Ice patterns contain {normal.Length}/{signature.Length} beats; expected 5/5.");
                return;
            }

            var cracked = 0;
            var rebound = 0;
            for (var index = 0; index < normal.Length; index++)
            {
                var beat = normal[index];
                if (beat.ApparatusPath == "ice/cracked_plate") cracked++;
                if (beat.ApparatusPath == "ice/crystal_rebound_gate") rebound++;
            }
            if (cracked != 3 || rebound != 2)
            {
                failures.Add($"Ice normal identity is {cracked} cracked/{rebound} rebound; expected 3/2.");
            }
        }

        private static void ValidateAllThemePatterns(List<string> failures)
        {
            var totalBeats = 0;
            foreach (var definition in ThemeMechanicCatalog.Definitions)
            {
                var normal = ThemeMechanicPatternCatalog.For(definition.Id, false);
                var signature = ThemeMechanicPatternCatalog.For(definition.Id, true);
                totalBeats += normal.Length + signature.Length;
                if (normal.Length < 5 || signature.Length < 5)
                {
                    failures.Add($"{definition.Id} has only {normal.Length}/{signature.Length} normal/signature beats.");
                    continue;
                }

                var hasA = false;
                var hasB = false;
                var hasReward = false;
                for (var index = 0; index < normal.Length; index++)
                {
                    hasA |= normal[index].ApparatusPath == definition.ApparatusA;
                    hasB |= normal[index].ApparatusPath == definition.ApparatusB;
                    hasReward |= normal[index].Rewarding;
                }
                if (!hasA || !hasB || !hasReward)
                {
                    failures.Add($"{definition.Id} normal pattern is missing apparatus identity or reward beat.");
                }
            }

            if (totalBeats < 60)
            {
                failures.Add($"Theme pattern catalog has {totalBeats} beats; expected at least 60.");
            }
        }

        private static void ValidateMechanicPool(List<string> failures)
        {
            var sceneRoot = new GameObject("ThemeMechanicPoolValidation");
            try
            {
                var spawner = new ThemeMechanicSpawner(sceneRoot.transform, 4f, 0f);
                spawner.Begin(ThemeMechanicPatternCatalog.For(ThemeMechanicId.IceInertia, true), 0f);
                spawner.Tick(10f, 0f);
                if (spawner.ActiveCount != 5 || spawner.ActiveCount > ThemeMechanicSpawner.Capacity)
                {
                    failures.Add($"Ice signature activated {spawner.ActiveCount} pooled roots; expected 5.");
                }
                if (!spawner.CriticalAssetMissing)
                {
                    failures.Add("Missing Ice signature prefab was not reported as a critical asset failure.");
                }

                spawner.Clear();
                spawner.Begin(ThemeMechanicPatternCatalog.For(ThemeMechanicId.IceInertia, false), 0f);
                spawner.Tick(10f, 0f);
                foreach (var child in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("floe_bridge"))
                    {
                        failures.Add("Mechanic pool retained a floe bridge after normal-pattern reuse.");
                        break;
                    }
                }
                spawner.Clear();
                if (spawner.ActiveCount != 0)
                {
                    failures.Add("Mechanic pool did not return to zero active roots.");
                }
            }
            finally
            {
                Object.DestroyImmediate(sceneRoot);
            }
        }

        private static void ValidateMasteryGrades(List<string> failures)
        {
            var completed = NewMastery(11);
            completed.BeginEvent(3, false);
            completed.RecordKeypoint(false);
            completed.RecordKeypoint(false);
            completed.RecordKeypoint(false);
            if (completed.ResolveEvent(false) != ThemeMechanicGrade.Completed)
            {
                failures.Add("Three completed keypoints did not grade Completed.");
            }

            var perfect = NewMastery(12);
            perfect.BeginEvent(3, false);
            perfect.RecordKeypoint(true);
            perfect.RecordKeypoint(true);
            perfect.RecordKeypoint(true);
            perfect.RecordDamage();
            if (perfect.ResolveEvent(false) != ThemeMechanicGrade.Perfect)
            {
                failures.Add("All-perfect damaged event did not grade Perfect.");
            }

            var flawless = NewMastery(13);
            flawless.BeginEvent(3, true);
            flawless.RecordKeypoint(true);
            flawless.RecordKeypoint(true);
            flawless.RecordKeypoint(true);
            if (flawless.ResolveEvent(true) != ThemeMechanicGrade.Flawless
                || !flawless.RunSnapshot.FlawlessSignatureCompleted)
            {
                failures.Add("All-perfect undamaged signature did not grade Flawless.");
            }
        }

        private static void ValidateResonance(List<string> failures)
        {
            var tracker = NewMastery(21);
            CompleteFlawlessEvent(tracker, false);
            var beforeDamage = tracker.Resonance01;
            tracker.BeginEvent(1, false);
            tracker.RecordDamage();
            if (!Mathf.Approximately(beforeDamage - tracker.Resonance01, 0.35f))
            {
                failures.Add("Damage did not remove exactly 35 resonance points.");
            }

            CompleteFlawlessEvent(tracker, false);
            CompleteFlawlessEvent(tracker, false);
            if (!tracker.IsResonating || !Mathf.Approximately(tracker.ResonanceSecondsRemaining, 8f))
            {
                failures.Add("Full resonance did not activate for exactly eight seconds.");
                return;
            }
            tracker.Tick(7.99f);
            if (!tracker.IsResonating)
            {
                failures.Add("Resonance ended before eight seconds.");
            }
            tracker.Tick(0.02f);
            if (tracker.IsResonating || tracker.Resonance01 > 0f)
            {
                failures.Add("Resonance did not clear after eight seconds.");
            }
        }

        private static void ValidateJourneyContracts(List<string> failures)
        {
            if (JourneyContractCatalog.Definitions.Length != 5)
            {
                failures.Add($"Journey contract catalog contains {JourneyContractCatalog.Definitions.Length}; expected 5.");
            }

            var first = NewMastery(20260716);
            var second = NewMastery(20260716);
            if (first.RunSnapshot.ContractKind != second.RunSnapshot.ContractKind)
            {
                failures.Add("Same seed/theme/mode selected different journey contracts.");
            }

            var tutorial = new ThemeMasteryTracker(
                RunnerMapTheme.IceLakeEcho,
                ThemeMechanicMode.Story,
                99,
                false);
            tutorial.BeginEvent(1, false);
            if (!tutorial.HudSnapshot.ShowTutorial || !tutorial.RunSnapshot.TutorialSeen)
            {
                failures.Add("First theme event did not expose its one-time tutorial state.");
            }
        }

        private static ThemeMasteryTracker NewMastery(int seed)
        {
            return new ThemeMasteryTracker(
                RunnerMapTheme.IceLakeEcho,
                ThemeMechanicMode.Daily,
                seed,
                true);
        }

        private static void CompleteFlawlessEvent(ThemeMasteryTracker tracker, bool signature)
        {
            tracker.BeginEvent(1, signature);
            tracker.RecordKeypoint(true);
            tracker.ResolveEvent(signature);
        }

        private static void ValidateRunSeeds(List<string> failures)
        {
            var utc = new System.DateTime(2026, 7, 16, 12, 0, 0, System.DateTimeKind.Utc);
            var dailyA = RunSession.ResolveRunSeed(
                RunnerRunMode.Daily, RunnerMapTheme.MistDike, 0, utc);
            var dailyB = RunSession.ResolveRunSeed(
                RunnerRunMode.Daily, RunnerMapTheme.MistDike, 99, utc);
            if (dailyA != dailyB)
            {
                failures.Add("Daily run seed changed for the same UTC date and theme.");
            }

            var storyA = RunSession.ResolveRunSeed(
                RunnerRunMode.Story, RunnerMapTheme.CedarRuins, 2, utc);
            var storyB = RunSession.ResolveRunSeed(
                RunnerRunMode.Story, RunnerMapTheme.CedarRuins, 2, utc.AddDays(1));
            if (storyA != storyB)
            {
                failures.Add("Story run seed changed for the same theme and attempt.");
            }

            var endlessA = RunSession.ResolveRunSeed(
                RunnerRunMode.Endless, RunnerMapTheme.IceLakeEcho, 1, utc);
            var endlessB = RunSession.ResolveRunSeed(
                RunnerRunMode.Endless, RunnerMapTheme.IceLakeEcho, 2, utc);
            if (endlessA == endlessB)
            {
                failures.Add("Consecutive Endless attempts produced the same seed.");
            }
        }

        private static void ValidateThemeMasteryPersistence(List<string> failures)
        {
            var theme = RunnerMapTheme.SkyFlight;
            var suffixes = new[]
            {
                "events", "perfect", "flawless", "best_streak",
                "signature", "contracts", "stars", "tutorial",
            };
            var keys = new string[suffixes.Length];
            var existed = new bool[suffixes.Length];
            var values = new int[suffixes.Length];
            for (var index = 0; index < suffixes.Length; index++)
            {
                keys[index] = $"growth_theme_mastery_v1_{(int)theme}_{suffixes[index]}";
                existed[index] = PlayerPrefs.HasKey(keys[index]);
                values[index] = PlayerPrefs.GetInt(keys[index], 0);
                PlayerPrefs.DeleteKey(keys[index]);
            }

            try
            {
                var empty = ThemeMasteryStore.For(theme);
                if (empty.EventsCompleted != 0 || empty.Stars != 0 || empty.TutorialSeen)
                {
                    failures.Add("Theme mastery migration did not create zero-default records.");
                    return;
                }

                var run = new ThemeMasteryRunSnapshot(theme, 3, 5, 1, 3,
                    true, false, true, JourneyContractKind.PerfectStreak, 2, true);
                ThemeMasteryStore.ApplyRun(run);
                var twice = ThemeMasteryStore.ApplyRun(run);
                if (twice.EventsCompleted != 6 || twice.PerfectCount != 10
                    || twice.ContractsCompleted != 2 || twice.Stars != 2 || !twice.TutorialSeen)
                {
                    failures.Add("Theme mastery store did not accumulate two run snapshots exactly.");
                }

                var flawlessSignature = new ThemeMasteryRunSnapshot(theme, 1, 1, 1, 1,
                    true, true, false, JourneyContractKind.SignatureComplete, 3, true);
                if (ThemeMasteryStore.ApplyRun(flawlessSignature).Stars != 3)
                {
                    failures.Add("Flawless signature did not clamp permanent mastery to three stars.");
                }
            }
            finally
            {
                for (var index = 0; index < keys.Length; index++)
                {
                    if (existed[index]) PlayerPrefs.SetInt(keys[index], values[index]);
                    else PlayerPrefs.DeleteKey(keys[index]);
                }
            }
        }
    }
}
#endif
