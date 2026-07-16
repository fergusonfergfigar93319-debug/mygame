#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ProjectPlayerExperienceValidation
    {
        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();

        public static void ValidateFromCommandLine()
        {
            var failures = new List<string>();
            ValidateSourceContracts(failures);
            ValidateAuditReportContract(failures);

            if (failures.Count > 0)
            {
                throw new Exception("[ProjectPlayerExperienceValidation] FAIL\n" +
                    string.Join("\n", failures));
            }

            Debug.Log("[ProjectPlayerExperienceValidation] PASS: journey experience contracts are covered.");
        }

        private static void ValidateSourceContracts(List<string> failures)
        {
            RequireSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "TrySettleCurrentAttempt", "runner must centralize settlement", failures);
            RequireSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "ResetPresentationForRetry", "retry must reset presentation state", failures);
            ForbidSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "IsReturnButtonTouch", "invisible corner return must be removed", failures);
            RequireSource("Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs",
                "LobbyModalStack", "lobby must own modal back navigation", failures);
            RequireSource("Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs",
                "RunLaunchGate", "lobby must reject duplicate launches", failures);
        }

        private static void ValidateAuditReportContract(List<string> failures)
        {
            var relativePath = "docs/PLAYER_EXPERIENCE_AUDIT.md";
            var text = ReadSource(relativePath, failures);
            if (text == null) return;
            if (!text.Contains("| PX-001 | P0 |", StringComparison.Ordinal))
                failures.Add("audit report must contain the seeded P0 issue table");
            if (!text.Contains("Verification", StringComparison.Ordinal))
                failures.Add("audit report must include verification evidence");
        }

        private static void RequireSource(string path, string token, string message,
            List<string> failures)
        {
            var text = ReadSource(path, failures);
            if (text != null && !text.Contains(token, StringComparison.Ordinal))
                failures.Add(message);
        }

        private static void ForbidSource(string path, string token, string message,
            List<string> failures)
        {
            var text = ReadSource(path, failures);
            if (text != null && text.Contains(token, StringComparison.Ordinal))
                failures.Add(message);
        }

        private static string ReadSource(string relativePath, List<string> failures)
        {
            var path = Path.Combine(ProjectRoot, relativePath);
            if (File.Exists(path)) return File.ReadAllText(path);
            failures.Add($"missing required audit source: {relativePath}");
            return null;
        }
    }
}
#endif

