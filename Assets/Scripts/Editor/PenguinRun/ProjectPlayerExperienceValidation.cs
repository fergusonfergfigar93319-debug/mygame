#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PenguinRun.Game;
using PenguinRun.Game.UI.Lobby;
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
            ValidateLaunchGate(failures);
            ValidateModalStack(failures);
            ValidateSourceContracts(failures);
            ValidateAuditReportContract(failures);

            if (failures.Count > 0)
            {
                throw new Exception("[ProjectPlayerExperienceValidation] FAIL\n" +
                    string.Join("\n", failures));
            }

            Debug.Log("[ProjectPlayerExperienceValidation] PASS: journey experience contracts are covered.");
        }

        private static void ValidateLaunchGate(List<string> failures)
        {
            var gate = new RunLaunchGate();
            Expect(gate.TryBegin(), "first launch must begin", failures);
            Expect(gate.IsBusy, "launch gate must expose busy state", failures);
            Expect(!gate.TryBegin(), "duplicate launch must be rejected", failures);
            gate.Reset();
            Expect(!gate.IsBusy, "reset must clear busy state", failures);
            Expect(gate.TryBegin(), "failed preparation must allow a new launch", failures);
        }

        private static void ValidateModalStack(List<string> failures)
        {
            GameObject first = null;
            GameObject second = null;
            var firstDismissals = 0;
            var secondDismissals = 0;
            try
            {
                first = new GameObject("AuditModalFirst");
                second = new GameObject("AuditModalSecond");
                var stack = new LobbyModalStack(UnityEngine.Object.DestroyImmediate);
                stack.Register(first, () => firstDismissals++);
                stack.Register(second, () => secondDismissals++);

                Expect(stack.HasOpenModal, "registered modal must be visible to back navigation", failures);
                Expect(stack.TryDismissTop(), "top modal must dismiss", failures);
                Expect(second == null, "top modal must be destroyed first", failures);
                Expect(first != null, "lower modal must remain after first dismissal", failures);
                Expect(secondDismissals == 1, "top modal callback must run once", failures);

                Expect(stack.TryDismissTop(), "lower modal must dismiss next", failures);
                Expect(first == null, "lower modal must be destroyed second", failures);
                Expect(firstDismissals == 1, "lower modal callback must run once", failures);
                Expect(!stack.TryDismissTop(), "empty modal stack must not consume back", failures);
                Expect(!stack.HasOpenModal, "empty modal stack must report no open modal", failures);
            }
            finally
            {
                if (first != null) UnityEngine.Object.DestroyImmediate(first);
                if (second != null) UnityEngine.Object.DestroyImmediate(second);
            }
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

        private static void Expect(bool condition, string message, List<string> failures)
        {
            if (!condition) failures.Add(message);
        }
    }
}
#endif
