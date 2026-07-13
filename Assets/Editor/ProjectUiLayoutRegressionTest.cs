#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PenguinRun.Game.UI;
using PenguinRun.Game.UI.Lobby;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Marker-driven edit-mode regression checks. This lets the already-open editor
/// validate runtime-built UI without replacing the user's current scene.
/// </summary>
[InitializeOnLoad]
public static class ProjectUiLayoutRegressionTest
{
    private static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
    private static string RequestPath => Path.Combine(ProjectRoot, "Temp", "PenguinRunUiRegression.request");
    private static string ResultPath => Path.Combine(ProjectRoot, "Temp", "PenguinRunUiRegression.result");

    static ProjectUiLayoutRegressionTest()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    private static void RunIfRequested()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(RequestPath);
        var passed = RunNow(out var report);
        File.WriteAllText(ResultPath, report);

        if (passed) Debug.Log("[ProjectUiLayoutRegressionTest] " + report);
        else Debug.LogError("[ProjectUiLayoutRegressionTest] " + report);
    }

    public static bool RunNow(out string report)
    {
        var failures = new List<string>();
        ValidateRunnerResultTextWidths(failures);
        ValidatePanelReplacement(failures);

        var passed = failures.Count == 0;
        report = passed
            ? "PASS: UI layout regressions are covered."
            : "FAIL:\n" + string.Join("\n", failures);
        return passed;
    }

    private static void ValidateRunnerResultTextWidths(List<string> failures)
    {
        GameObject canvas = null;
        try
        {
            canvas = RunnerDeathOverlay.Show(12345, 2532, 187, null, null);
            Canvas.ForceUpdateCanvases();

            RequireWidth(canvas, "RunnerDeathOverlay/ResultCard/Title", 500f, failures);
            RequireWidth(canvas, "RunnerDeathOverlay/ResultCard/Score", 500f, failures);
            RequireWidth(canvas, "RunnerDeathOverlay/ResultCard/ScoreLabel", 500f, failures);
        }
        catch (Exception ex)
        {
            failures.Add("Runner result layout threw: " + ex.Message);
        }
        finally
        {
            if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
        }
    }

    private static void ValidatePanelReplacement(List<string> failures)
    {
        var layer = new GameObject("PanelLayer", typeof(RectTransform));
        try
        {
            var first = new GameObject("FirstPanel", typeof(RectTransform));
            first.transform.SetParent(layer.transform, false);
            var second = new GameObject("SecondPanel", typeof(RectTransform));
            second.transform.SetParent(layer.transform, false);

            var router = new LobbyPanelRouter(layer.transform, null, null);
            router.Open(first, "First");
            router.Open(second, "Second");

            if (first != null && first.activeSelf)
                failures.Add("Panel router leaves the previous page active during replacement.");

            var activeChildren = 0;
            for (var i = 0; i < layer.transform.childCount; i++)
            {
                if (layer.transform.GetChild(i).gameObject.activeSelf) activeChildren++;
            }
            if (activeChildren != 1)
                failures.Add($"Panel layer has {activeChildren} active pages; expected exactly 1.");
        }
        catch (Exception ex)
        {
            failures.Add("Panel replacement check threw: " + ex.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(layer);
        }
    }

    private static void RequireWidth(GameObject root, string path, float minimum, List<string> failures)
    {
        var target = root.transform.Find(path) as RectTransform;
        if (target == null)
        {
            failures.Add("Missing UI element: " + path);
            return;
        }

        if (target.rect.width < minimum)
            failures.Add($"{path} width is {target.rect.width:F1}; expected at least {minimum:F1}.");
    }
}
#endif
