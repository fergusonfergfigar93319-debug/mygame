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

            report = failures.Count == 0
                ? "PASS: six theme mechanic definitions are valid."
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
    }
}
#endif
