#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Batch-mode play test for the main menu startup path.</summary>
public static class ProjectRuntimeSmokeTest
{
    private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    private const double PlayDurationSeconds = 8d;
    private const double HardTimeoutSeconds = 45d;

    private static readonly List<string> Errors = new();
    private static double playDeadline;
    private static double hardDeadline;
    private static bool enteredPlayMode;
    private static bool stopRequested;
    private static bool originalEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions originalEnterPlayModeOptions;

    public static void RunMainMenu()
    {
        Errors.Clear();
        enteredPlayMode = false;
        stopRequested = false;
        hardDeadline = EditorApplication.timeSinceStartup + HardTimeoutSeconds;

        originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions | EnterPlayModeOptions.DisableDomainReload;

        Application.logMessageReceived += OnLogMessage;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;

        EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        Debug.Log($"[ProjectRuntimeSmokeTest] Starting {MainMenuScene}.");
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            enteredPlayMode = true;
            playDeadline = EditorApplication.timeSinceStartup + PlayDurationSeconds;
            Debug.Log("[ProjectRuntimeSmokeTest] Entered play mode.");
        }
        else if (state == PlayModeStateChange.EnteredEditMode && stopRequested)
        {
            Complete();
        }
    }

    private static void OnEditorUpdate()
    {
        var now = EditorApplication.timeSinceStartup;
        if (now >= hardDeadline)
        {
            Errors.Add("Timed out while running the main-menu play-mode smoke test.");
            stopRequested = true;
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            else
                Complete();
            return;
        }

        if (enteredPlayMode && !stopRequested && now >= playDeadline)
        {
            stopRequested = true;
            EditorApplication.isPlaying = false;
        }
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type is LogType.Error or LogType.Exception or LogType.Assert)
            Errors.Add($"{type}: {condition}\n{stackTrace}");
    }

    private static void Complete()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
        EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;

        if (Errors.Count == 0)
        {
            Debug.Log("[ProjectRuntimeSmokeTest] PASS: main menu ran without errors or exceptions.");
            EditorApplication.Exit(0);
            return;
        }

        Debug.LogError("[ProjectRuntimeSmokeTest] FAIL:\n" + string.Join("\n\n", Errors));
        EditorApplication.Exit(1);
    }
}
#endif
