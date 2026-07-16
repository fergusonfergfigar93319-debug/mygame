#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PenguinRun.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PenguinRun.Editor
{
    public static class ProjectPlayerJourneySmokeTest
    {
        private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
        private const double HardTimeoutSeconds = 150d;
        private const double StageTimeoutSeconds = 30d;

        private enum Stage
        {
            Idle,
            WaitHome,
            Capture,
            OpenDifficulty,
            WaitDifficulty,
            StartRun,
            WaitRunner,
            PauseRun,
            WaitPaused,
            ResumeRun,
            WaitResumed,
            PauseForReturn,
            WaitPausedForReturn,
            WaitResult,
            Stop,
        }

        private static readonly List<string> Errors = new();
        private static readonly string[] ScreenshotNames =
        {
            "01-home.png",
            "02-difficulty.png",
            "03-running.png",
            "04-paused.png",
            "05-result.png",
        };

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        private static string ScreenshotDirectory =>
            Path.Combine(ProjectRoot, "Artifacts", "PlayerExperienceAudit");

        private static Stage stage;
        private static Stage captureNextStage;
        private static double hardDeadline;
        private static double stageDeadline;
        private static double runnerReadyAt;
        private static int captureTargetFrame;
        private static bool captureIssued;
        private static string capturePath;
        private static int penguinRunnerLoads;
        private static bool stopRequested;
        private static bool originalEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions originalEnterPlayModeOptions;

        public static void RunFromCommandLine()
        {
            Errors.Clear();
            stage = Stage.Idle;
            stopRequested = false;
            penguinRunnerLoads = 0;
            hardDeadline = EditorApplication.timeSinceStartup + HardTimeoutSeconds;

            Directory.CreateDirectory(ScreenshotDirectory);
            foreach (var name in ScreenshotNames)
            {
                var path = Path.Combine(ScreenshotDirectory, name);
                if (File.Exists(path)) File.Delete(path);
            }

            originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            Application.logMessageReceived += OnLogMessage;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            SceneManager.sceneLoaded += OnSceneLoaded;

            EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            Debug.Log("[ProjectPlayerJourneySmokeTest] Starting real-scene player journey.");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                Screen.SetResolution(1280, 720, false);
                SetStage(Stage.WaitHome);
            }
            else if (stateChange == PlayModeStateChange.EnteredEditMode && stopRequested)
            {
                Complete();
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, "PenguinRunner", StringComparison.Ordinal)) return;
            penguinRunnerLoads++;
            runnerReadyAt = EditorApplication.timeSinceStartup + 4d;
            if (penguinRunnerLoads > 1)
                Fail("PenguinRunner loaded more than once after a double StartFinal dispatch.");
        }

        private static void OnEditorUpdate()
        {
            if (stage == Stage.Idle || stopRequested) return;
            var now = EditorApplication.timeSinceStartup;
            if (now >= hardDeadline)
            {
                Fail("Hard timeout while running the complete player journey.");
                return;
            }
            if (now >= stageDeadline)
            {
                Fail("Stage timeout: " + stage);
                return;
            }

            switch (stage)
            {
                case Stage.WaitHome:
                    TickWaitHome();
                    break;
                case Stage.Capture:
                    TickCapture();
                    break;
                case Stage.OpenDifficulty:
                    ClickRequired("PrimaryChapterAction");
                    if (!stopRequested) SetStage(Stage.WaitDifficulty);
                    break;
                case Stage.WaitDifficulty:
                    if (FindSceneObject("DifficultyOverlay", true) != null &&
                        FindButton("StartFinal", true) != null)
                        BeginCapture("02-difficulty.png", Stage.StartRun);
                    break;
                case Stage.StartRun:
                    TickStartRun();
                    break;
                case Stage.WaitRunner:
                    TickWaitRunner(now);
                    break;
                case Stage.PauseRun:
                    ClickRequired("Pause");
                    if (!stopRequested) SetStage(Stage.WaitPaused);
                    break;
                case Stage.WaitPaused:
                    if (IsActive("Paused")) BeginCapture("04-paused.png", Stage.ResumeRun);
                    break;
                case Stage.ResumeRun:
                    ClickRequired("Resume");
                    if (!stopRequested) SetStage(Stage.WaitResumed);
                    break;
                case Stage.WaitResumed:
                    if (!IsActive("Paused")) SetStage(Stage.PauseForReturn);
                    break;
                case Stage.PauseForReturn:
                    ClickRequired("Pause");
                    if (!stopRequested) SetStage(Stage.WaitPausedForReturn);
                    break;
                case Stage.WaitPausedForReturn:
                    if (!IsActive("Paused")) break;
                    ClickRequired("ReturnToCamp");
                    if (!stopRequested) SetStage(Stage.WaitResult);
                    break;
                case Stage.WaitResult:
                    if (SceneManager.GetActiveScene().name == "MainMenu" && IsActive("RunResultOverlay") &&
                        (SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.IsTransitioning))
                        BeginCapture("05-result.png", Stage.Stop);
                    break;
                case Stage.Stop:
                    RequestStop();
                    break;
            }
        }

        private static void TickWaitHome()
        {
            if (FindFirstObjectByType<MainMenuBootstrap>() == null ||
                FindSceneObject("MenuCanvas", true) == null)
                return;

            var firstLogin = FindSceneObject("FirstLoginOverlay", false);
            if (firstLogin != null)
            {
                ClickRequired("Skip");
                return;
            }

            var checkIn = FindSceneObject("CheckInOverlay", false);
            if (checkIn != null)
            {
                ClickRequired("OK");
                return;
            }

            if (FindButton("PrimaryChapterAction", false) != null)
                BeginCapture("01-home.png", Stage.OpenDifficulty);
        }

        private static void TickStartRun()
        {
            var button = FindButton("StartFinal", false);
            if (button == null)
            {
                Fail("Missing active StartFinal button.");
                return;
            }

            button.onClick.Invoke();
            if (button.interactable)
            {
                Fail("StartFinal stayed interactable after the first launch dispatch.");
                return;
            }
            button.onClick.Invoke();
            SetStage(Stage.WaitRunner);
        }

        private static void TickWaitRunner(double now)
        {
            if (penguinRunnerLoads != 1 || now < runnerReadyAt) return;
            if (FindFirstObjectByType<PenguinRunnerGame>() == null ||
                FindSceneObject("RunnerHudCanvas", true) == null)
                return;
            BeginCapture("03-running.png", Stage.PauseRun);
        }

        private static void BeginCapture(string fileName, Stage nextStage)
        {
            capturePath = Path.Combine(ScreenshotDirectory, fileName);
            captureNextStage = nextStage;
            captureTargetFrame = Time.frameCount + 2;
            captureIssued = false;
            SetStage(Stage.Capture);
        }

        private static void TickCapture()
        {
            if (Time.frameCount < captureTargetFrame) return;

            if (!captureIssued)
            {
                ScreenCapture.CaptureScreenshot(capturePath);
                captureIssued = true;
                return;
            }

            if (!File.Exists(capturePath) || new FileInfo(capturePath).Length < 256) return;
            if (!ValidateScreenshot(capturePath, out var error))
            {
                Fail(error);
                return;
            }
            SetStage(captureNextStage);
        }

        private static bool ValidateScreenshot(string path, out string error)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    error = "Could not decode screenshot: " + Path.GetFileName(path);
                    return false;
                }
                if (texture.width < 1280 || texture.height < 720)
                {
                    error = $"Screenshot {Path.GetFileName(path)} is only {texture.width}x{texture.height}.";
                    return false;
                }

                var samples = 0;
                var sum = 0d;
                var sumSquares = 0d;
                var stepX = Mathf.Max(1, texture.width / 32);
                var stepY = Mathf.Max(1, texture.height / 18);
                for (var y = 0; y < texture.height; y += stepY)
                for (var x = 0; x < texture.width; x += stepX)
                {
                    var color = texture.GetPixel(x, y);
                    var luminance = color.r * 0.2126 + color.g * 0.7152 + color.b * 0.0722;
                    sum += luminance;
                    sumSquares += luminance * luminance;
                    samples++;
                }
                var mean = sum / Math.Max(1, samples);
                var variance = sumSquares / Math.Max(1, samples) - mean * mean;
                if (variance < 0.0002d)
                {
                    error = "Screenshot is blank or nearly uniform: " + Path.GetFileName(path);
                    return false;
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }

            error = null;
            return true;
        }

        private static void ClickRequired(string name)
        {
            var button = FindButton(name, false);
            if (button == null)
            {
                Fail("Missing active button: " + name);
                return;
            }
            button.onClick.Invoke();
        }

        private static Button FindButton(string name, bool includeInactive)
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button.gameObject.scene.IsValid() && button.gameObject.name == name)
                    return button;
            }
            return null;
        }

        private static T FindFirstObjectByType<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }

        private static GameObject FindSceneObject(string name, bool includeInactive)
        {
            var objects = UnityEngine.Object.FindObjectsByType<GameObject>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (var gameObject in objects)
            {
                if (gameObject.scene.IsValid() && gameObject.name == name)
                    return gameObject;
            }
            return null;
        }

        private static bool IsActive(string name)
        {
            var gameObject = FindSceneObject(name, true);
            return gameObject != null && gameObject.activeInHierarchy;
        }

        private static void SetStage(Stage next)
        {
            stage = next;
            stageDeadline = EditorApplication.timeSinceStartup + StageTimeoutSeconds;
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (condition.Contains("UnityConnectWebRequestException", StringComparison.Ordinal) &&
                condition.Contains("Token Exchange", StringComparison.Ordinal))
                return;
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                Errors.Add($"{type}: {condition}\n{stackTrace}");
        }

        private static void Fail(string message)
        {
            Errors.Add(message);
            RequestStop();
        }

        private static void RequestStop()
        {
            if (stopRequested) return;
            stopRequested = true;
            stage = Stage.Stop;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            else Complete();
        }

        private static void Complete()
        {
            Application.logMessageReceived -= OnLogMessage;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;

            foreach (var name in ScreenshotNames)
            {
                var path = Path.Combine(ScreenshotDirectory, name);
                if (!File.Exists(path)) Errors.Add("Missing screenshot: " + name);
            }

            if (Errors.Count == 0)
            {
                Debug.Log("[ProjectPlayerJourneySmokeTest] PASS: home-to-result journey completed with five screenshots.");
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError("[ProjectPlayerJourneySmokeTest] FAIL:\n" + string.Join("\n\n", Errors));
            EditorApplication.Exit(1);
        }
    }
}
#endif
