#if UNITY_EDITOR
using System.Collections.Generic;
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

            report = failures.Count == 0
                ? "PASS: six theme mechanic definitions and 10,000 director beats are valid."
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
    }
}
#endif
