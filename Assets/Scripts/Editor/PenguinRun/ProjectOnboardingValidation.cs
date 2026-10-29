#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PenguinRun.Game;
using PenguinRun.Game.Save;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ProjectOnboardingValidation
    {
        [MenuItem("PenguinRun/Onboarding/Validate First-Run Gate")]
        public static void ValidateFromMenu()
        {
            var passed = RunNow(out var report);
            if (passed) Debug.Log(report);
            else Debug.LogError(report);
        }

        public static bool RunNow(out string report)
        {
            var failures = new List<string>();
            ValidateCompetenceGraduation(failures);
            ValidateRunCountFuse(failures);
            ValidateWaveRotation(failures);
            ValidateSuppressedDirectors(failures);
            ValidateMigrationSemantics(failures);
            ValidateSourceContracts(failures);

            report = failures.Count == 0
                ? "PASS: competence onboarding, run-count fuse, wave rotation, 1200m suppression, migration, and source contracts are valid."
                : "FAIL:\n" + string.Join("\n", failures);
            return failures.Count == 0;
        }

        public static void ValidateFromCommandLine()
        {
            var passed = RunNow(out var report);
            if (passed) Debug.Log(report);
            else Debug.LogError(report);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void ValidateCompetenceGraduation(List<string> failures)
        {
            var state = OnboardingState.Fresh();
            Expect(state.IsOnboarding(0), "fresh save must be in onboarding", failures);

            state.Record(RunnerAction.Left, laneChanged: true);
            state.Record(RunnerAction.Jump, laneChanged: false);
            state.Record(RunnerAction.Slide, laneChanged: false);
            Expect(state.IsOnboarding(0), "partial competence must stay in onboarding", failures);

            state.RecordCollect();
            Expect(state.AllDemonstrated, "four bits must mark all demonstrated", failures);
            Expect(!state.IsOnboarding(0), "full competence must graduate even at TotalRuns=0", failures);
        }

        private static void ValidateRunCountFuse(List<string> failures)
        {
            var state = OnboardingState.Fresh();
            Expect(state.IsOnboarding(2), "TotalRuns=2 must still be onboarding", failures);
            Expect(!state.IsOnboarding(OnboardingState.ForceGraduateAfterRuns),
                "TotalRuns>=3 fuse must graduate", failures);
        }

        private static void ValidateWaveRotation(List<string> failures)
        {
            foreach (RunnerMapTheme theme in Enum.GetValues(typeof(RunnerMapTheme)))
            {
                var director = new SegmentWaveDirector(theme, isOnboarding: true);
                var firstFour = new List<SegmentPatternKind>();
                for (var n = 0; n < 48 && firstFour.Count < 4; n++)
                {
                    var modifiers = director.ConsumeWave();
                    if (director.LastConsumeWasFirstWave)
                        firstFour.Add(modifiers.PatternKind);
                }

                Expect(firstFour.Count == 4,
                    $"{theme}: onboarding rotation must yield 4 segment kinds, got {firstFour.Count}",
                    failures);
                if (firstFour.Count < 4) continue;

                Expect(firstFour[0] == SegmentPatternKind.LaneWeave,
                    $"{theme}: rotation[0] must be LaneWeave, got {firstFour[0]}", failures);
                Expect(firstFour[1] == SegmentPatternKind.JumpArc,
                    $"{theme}: rotation[1] must be JumpArc, got {firstFour[1]}", failures);
                Expect(firstFour[2] == SegmentPatternKind.SlideTunnel,
                    $"{theme}: rotation[2] must be SlideTunnel, got {firstFour[2]}", failures);
                Expect(firstFour[3] == SegmentPatternKind.RewardRun,
                    $"{theme}: rotation[3] must be RewardRun, got {firstFour[3]}", failures);

                for (var n = 0; n < 40; n++)
                {
                    var modifiers = director.ConsumeWave();
                    if (SegmentWaveDirector.IsExcludedOnboardingPattern(modifiers.PatternKind))
                    {
                        failures.Add($"{theme}: onboarding selected excluded pattern {modifiers.PatternKind}");
                        break;
                    }
                }
            }
        }

        private static void ValidateSuppressedDirectors(List<string> failures)
        {
            var route = new RouteEventDirector(RunnerMapTheme.IceLakeEcho, daily: false, runCount: 0, suppressed: true);
            for (var distance = 0f; distance <= 1200f; distance += 1f)
            {
                if (route.Tick(distance))
                {
                    failures.Add("suppressed RouteEventDirector must not fire within 1200m");
                    break;
                }
            }

            var mechanic = new MapMechanicDirector(ThemeMechanicMode.Endless, seed: 42, suppressed: true);
            for (var beat = 0; beat < 400; beat++)
            {
                mechanic.RecordBeat(dangerous: beat % 3 == 0, rewarding: beat % 7 == 0);
                var context = new ThemeMechanicContext(
                    RunnerMapTheme.IceLakeEcho,
                    ThemeMechanicMode.Endless,
                    distance: 100f + beat * 3f,
                    lives: 3,
                    secondsSinceHit: 20f,
                    bossActive: false,
                    routeEventActive: false,
                    themeTransition: false,
                    difficulty01: 0.4f);
                if (mechanic.TrySchedule(context, out _))
                {
                    failures.Add("suppressed MapMechanicDirector must not schedule");
                    break;
                }
            }

            var worldEvents = new WorldEventScheduler(suppressed: true);
            Expect(!worldEvents.Tick(500f, 0.02f), "suppressed WorldEventScheduler must not fire at 500m", failures);
            Expect(!worldEvents.Tick(1000f, 0.02f), "suppressed WorldEventScheduler must not fire at 1000m", failures);
            Expect(!worldEvents.EventActive, "suppressed WorldEventScheduler must stay inactive", failures);

            var openRoute = new RouteEventDirector(RunnerMapTheme.IceLakeEcho, daily: false, runCount: 0, suppressed: false);
            var fired = false;
            for (var distance = 0f; distance <= 400f; distance += 1f)
            {
                if (openRoute.Tick(distance))
                {
                    fired = true;
                    break;
                }
            }
            Expect(fired, "non-suppressed RouteEventDirector must still fire for veterans", failures);
        }

        private static void ValidateMigrationSemantics(List<string> failures)
        {
            const string laneKey = "onboarding_v1_lane";
            const string jumpKey = "onboarding_v1_jump";
            const string slideKey = "onboarding_v1_slide";
            const string collectKey = "onboarding_v1_collect";
            const string slideLegacyKey = "runner_slide_gate_tutorial_done";
            const string totalRunsKey = "total_runs";
            const string schemaKey = "growth_save_schema_version";

            var backup = new Dictionary<string, int>
            {
                [laneKey] = PlayerPrefs.GetInt(laneKey, 0),
                [jumpKey] = PlayerPrefs.GetInt(jumpKey, 0),
                [slideKey] = PlayerPrefs.GetInt(slideKey, 0),
                [collectKey] = PlayerPrefs.GetInt(collectKey, 0),
                [slideLegacyKey] = PlayerPrefs.GetInt(slideLegacyKey, 0),
                [totalRunsKey] = PlayerPrefs.GetInt(totalRunsKey, 0),
                [schemaKey] = PlayerPrefs.GetInt(schemaKey, 0),
            };

            try
            {
                ClearOnboardingPrefs(laneKey, jumpKey, slideKey, collectKey, slideLegacyKey, totalRunsKey, schemaKey);

                PlayerPrefs.SetInt(totalRunsKey, 5);
                PlayerPrefs.SetInt(schemaKey, 3);
                PlayerPrefs.Save();
                SaveSchema.EnsureMigrated();
                Expect(PlayerSave.OnboardingLaneDemonstrated
                       && PlayerSave.OnboardingJumpDemonstrated
                       && PlayerSave.OnboardingSlideDemonstrated
                       && PlayerSave.OnboardingCollectDemonstrated,
                    "TotalRuns>=3 migration must mark all competence bits", failures);
                Expect(!OnboardingState.LoadFromSave().IsOnboarding(PlayerSave.TotalRuns),
                    "veteran migration must not re-enter onboarding", failures);

                ClearOnboardingPrefs(laneKey, jumpKey, slideKey, collectKey, slideLegacyKey, totalRunsKey, schemaKey);
                PlayerPrefs.SetInt(totalRunsKey, 0);
                PlayerPrefs.SetInt(slideLegacyKey, 1);
                PlayerPrefs.SetInt(schemaKey, 3);
                PlayerPrefs.Save();
                SaveSchema.EnsureMigrated();
                Expect(PlayerSave.OnboardingSlideDemonstrated,
                    "legacy slide-gate flag must migrate into slide competence bit", failures);
                Expect(!PlayerSave.OnboardingLaneDemonstrated,
                    "legacy slide migration must not force other bits", failures);
            }
            finally
            {
                foreach (var pair in backup)
                    PlayerPrefs.SetInt(pair.Key, pair.Value);
                PlayerPrefs.Save();
            }
        }

        private static void ClearOnboardingPrefs(params string[] keys)
        {
            foreach (var key in keys)
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private static void ValidateSourceContracts(List<string> failures)
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            ExpectSource(root, "Assets/Scripts/Runtime/PenguinRun/Onboarding/OnboardingState.cs",
                "ForceGraduateAfterRuns", failures);
            ExpectSource(root, "Assets/Scripts/Runtime/Game/RunnerSessionConfig.cs",
                "IsOnboarding", failures);
            ExpectSource(root, "Assets/Scripts/Runtime/PenguinRun/Systems/SegmentWaveDirector.cs",
                "OnboardingRotation", failures);
            ExpectSource(root, "Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs",
                "config.IsOnboarding", failures);
            ExpectSource(root, "Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs",
                "onboarding.Persist()", failures);
            ExpectSource(root, "Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs",
                "SuppressMotorActionLabels", failures);
            ExpectSource(root, "Assets/Scripts/Runtime/Game/Save/SaveSchema.cs",
                "MigrateOnboardingCompetence", failures);
            ExpectSource(root, "Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs",
                "!config.IsOnboarding", failures);
        }

        private static void ExpectSource(string root, string relative, string needle, List<string> failures)
        {
            var path = Path.Combine(root, relative);
            if (!File.Exists(path))
            {
                failures.Add($"missing source file: {relative}");
                return;
            }

            var text = File.ReadAllText(path);
            if (text.IndexOf(needle, StringComparison.Ordinal) < 0)
                failures.Add($"missing contract '{needle}' in {relative}");
        }

        private static void Expect(bool condition, string message, List<string> failures)
        {
            if (!condition) failures.Add(message);
        }
    }
}
#endif
