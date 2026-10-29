#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PenguinRun.Game;
using PenguinRun.Game.UI;
using PenguinRun.Game.UI.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            ValidateHudActions(failures);
            ValidateResponsiveGestures(failures);
            ValidateLaneFollow(failures);
            ValidateSettlementGate(failures);
            ValidateDeathRetryCopy(failures);
            ValidateRetryResetDependencies(failures);
            ValidateSourceContracts(failures);
            ValidateAuditReportContract(failures);

            if (failures.Count > 0)
            {
                throw new Exception("[ProjectPlayerExperienceValidation] FAIL\n" +
                    string.Join("\n", failures));
            }

            Debug.Log("[ProjectPlayerExperienceValidation] PASS: journey experience contracts are covered.");
        }

        private static void ValidateRetryResetDependencies(List<string> failures)
        {
            var events = new WorldEventScheduler();
            Expect(events.Tick(500f, 0.02f), "world event scheduler must trigger at its first milestone", failures);
            Expect(events.EventActive, "world event must be active before reset", failures);
            events.Reset();
            Expect(!events.EventActive, "world event reset must clear the active event", failures);
            Expect(!events.Tick(499f, 0.02f), "world event reset must restore the first milestone", failures);

            GameObject parent = null;
            try
            {
                parent = new GameObject("AuditParticlePool");
                var pool = new ParticleEffectPool(parent.transform, 0f, 4);
                pool.Emit(Vector3.up, Color.white, 2, lifetime: 2f);
                Expect(CountActiveChildren(parent.transform) == 2,
                    "particle pool must activate emitted particles", failures);
                pool.ReleaseAll();
                Expect(CountActiveChildren(parent.transform) == 0,
                    "particle pool reset must release every active particle", failures);
            }
            finally
            {
                if (parent != null) UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        private static void ValidateDeathRetryCopy(List<string> failures)
        {
            var model = JourneyResultViewModel.FromDeath(1200, 240, 12);
            Expect(model.ProgressionPreview.Contains("已记录", StringComparison.Ordinal),
                "death overlay must explain that the completed attempt is recorded before retry", failures);
        }

        private static void ValidateSettlementGate(List<string> failures)
        {
            var gate = new RunSettlementGate();
            Expect(gate.TryBeginSettlement(), "first settlement must begin", failures);
            Expect(gate.IsSettled, "settlement gate must expose settled state", failures);
            Expect(!gate.TryBeginSettlement(), "duplicate settlement must be rejected", failures);
            gate.ResetForNextAttempt();
            Expect(!gate.IsSettled, "retry reset must clear settlement state", failures);
            Expect(gate.TryBeginSettlement(), "retry attempt must settle once", failures);
            Expect(!gate.TryBeginSettlement(), "retry duplicate settlement must be rejected", failures);
        }

        private static void ValidateResponsiveGestures(List<string> failures)
        {
            var viewports = new[]
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1440),
            };

            foreach (var viewport in viewports)
            {
                var thresholds = RunnerGestureThresholds.ForViewport(viewport.x, viewport.y);
                var basis = Mathf.Min(viewport.x, viewport.y);
                Expect(RunnerGestureClassifier.ClassifySwipe(
                        new Vector2(viewport.x * 0.12f, viewport.y * 0.01f), thresholds) == RunnerAction.Right,
                    $"right swipe at {viewport}", failures);
                Expect(RunnerGestureClassifier.ClassifySwipe(
                        new Vector2(viewport.x * -0.12f, viewport.y * 0.01f), thresholds) == RunnerAction.Left,
                    $"left swipe at {viewport}", failures);
                Expect(RunnerGestureClassifier.ClassifySwipe(
                        new Vector2(viewport.x * 0.01f, viewport.y * 0.12f), thresholds) == RunnerAction.Jump,
                    $"up swipe at {viewport}", failures);
                Expect(RunnerGestureClassifier.ClassifySwipe(
                        new Vector2(basis * 0.08f, basis * 0.08f), thresholds) == RunnerAction.None,
                    $"ambiguous diagonal at {viewport}", failures);
                Expect(RunnerGestureClassifier.IsTap(
                        new Vector2(basis * 0.01f, basis * 0.01f), thresholds),
                    $"tap at {viewport}", failures);
                Expect(RunnerGestureClassifier.IsDoubleTap(
                        basis * 0.03f, 0.2f, thresholds),
                    $"near double tap at {viewport}", failures);
                Expect(!RunnerGestureClassifier.IsDoubleTap(
                        basis * 0.09f, 0.2f, thresholds),
                    $"far double tap at {viewport}", failures);
            }

            Expect(RunnerGestureClassifier.IsTwoFingerPause(2, true),
                "two-finger began must pause", failures);
            Expect(!RunnerGestureClassifier.IsTwoFingerPause(2, false),
                "two stationary fingers must not repeat pause", failures);
        }

        private static void ValidateLaneFollow(List<string> failures)
        {
            const float lerp = 29f;
            var dt60 = 1f / 60f;
            var at60 = RunnerMotion.Follow01(lerp, dt60);
            Expect(Mathf.Abs(at60 - lerp * dt60) < 0.0001f,
                "Follow01 must match historical laneLerp * dt at 60fps", failures);

            Expect(RunnerMotion.Follow01(lerp, 1f / 30f) < 0.85f,
                "one 30fps lane step must not nearly snap to the target", failures);

            var remain = 1f;
            remain *= 1f - RunnerMotion.Follow01(lerp, 1f / 120f);
            remain *= 1f - RunnerMotion.Follow01(lerp, 1f / 120f);
            Expect(Mathf.Abs(1f - remain - at60) < 0.04f,
                "two 120fps lane steps must stay close to one 60fps step", failures);
            Expect(RunnerMotion.Follow01(lerp, 0f) == 0f, "zero dt follow must be 0", failures);
        }

        private static void ValidateHudActions(List<string> failures)
        {
            GameObject eventSystem = null;
            GameObject canvas = null;
            var pauseCount = 0;
            var resumeCount = 0;
            var returnCount = 0;
            try
            {
                eventSystem = new GameObject("AuditHudEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                RunnerHudCanvas.Create(new RunnerSessionConfig(),
                    () => pauseCount++, () => resumeCount++, () => returnCount++);
                canvas = GameObject.Find("RunnerHudCanvas");
                Expect(canvas != null, "runner HUD must create a canvas", failures);
                if (canvas == null) return;

                var pause = FindButton(canvas, "SafeArea/Pause", failures);
                var resume = FindButton(canvas, "SafeArea/Paused/Resume", failures);
                var returnToCamp = FindButton(canvas, "SafeArea/Paused/ReturnToCamp", failures);
                Expect(pause != null && pause.gameObject.activeInHierarchy,
                    "pause action must be visible during a run", failures);
                var pausedRoot = canvas.transform.Find("SafeArea/Paused");
                Expect(pausedRoot != null && !pausedRoot.gameObject.activeSelf,
                    "pause overlay must start hidden", failures);
                var emptyBuffs = canvas.transform.Find("SafeArea/NormalStats/Buffs");
                Expect(emptyBuffs != null && !emptyBuffs.gameObject.activeSelf,
                    "empty buff card must not occupy HUD space", failures);
                var dashReady = canvas.transform.Find("SafeArea/DashReady");
                Expect(dashReady != null && dashReady.gameObject.activeSelf,
                    "manual dash readiness chip must stay visible outside boss stats", failures);

                pause?.onClick.Invoke();
                resume?.onClick.Invoke();
                returnToCamp?.onClick.Invoke();
                Expect(pauseCount == 1, "pause button must invoke only pause", failures);
                Expect(resumeCount == 1, "resume button must invoke only resume", failures);
                Expect(returnCount == 1, "return button must invoke only return", failures);
            }
            finally
            {
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
                if (eventSystem != null) UnityEngine.Object.DestroyImmediate(eventSystem);
            }
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
            RequireSource("Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs",
                "CaptureResult", "controller must expose side-effect-free result capture", failures);
            RequireSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "ResetPresentationForRetry", "retry must reset presentation state", failures);
            RequireSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "worldEvents.Reset();", "retry must reset world events", failures);
            RequireSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "particlePool.ReleaseAll();", "retry must release pooled particles", failures);
            RequireSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "currentTheme = config.MapTheme;", "retry must restore the configured map theme", failures);
            ForbidSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "IsReturnButtonTouch", "invisible corner return must be removed", failures);
            ForbidSource("Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs",
                "config.MapTheme = nextTheme;", "runtime visual theme changes must not mutate session identity",
                failures);
            RequireSource("Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs",
                "LobbyModalStack", "lobby must own modal back navigation", failures);
            RequireSource("Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs",
                "RunLaunchGate", "lobby must reject duplicate launches", failures);
            ForbidSource("Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs",
                "TextAnchor.LowerLeft, UiBuilder.TextTertiary",
                "difficulty speed and Boss timing must not use low-contrast tertiary text", failures);
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

        private static Button FindButton(GameObject root, string path, List<string> failures)
        {
            var target = root.transform.Find(path);
            if (target != null) return target.GetComponent<Button>();
            failures.Add("missing HUD action: " + path);
            return null;
        }

        private static int CountActiveChildren(Transform parent)
        {
            var count = 0;
            for (var i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).gameObject.activeSelf) count++;
            }
            return count;
        }
    }
}
#endif
