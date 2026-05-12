using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless CI / local APK build via Unity batchmode:
/// -executeMethod PenguinRunCiBuild.BuildAndroidApkForTesting
/// </summary>
public static class PenguinRunCiBuild
{
    /// <summary>Default output: Builds/Android/PenguinRun-Test-v{version}-{bundleCode}-{utc-timestamp}.apk (timestamp avoids Windows file lock on overwrite).</summary>
    public static void BuildAndroidApkForTesting()
    {
        BuildAndroidApkForTesting(Path.Combine(Application.dataPath, "..", "Builds", "Android"));
    }

    public static void BuildAndroidApkForTesting(string outputDirectory)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("EditorBuildSettings has no enabled scenes.");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(outputDirectory);
        string fileName =
            $"PenguinRun-Test-v{Application.version}-{PlayerSettings.Android.bundleVersionCode}-{System.DateTime.UtcNow:yyyyMMdd-HHmmss}.apk";
        string apkPath = Path.Combine(outputDirectory, fileName);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.buildAppBundle = false;

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);

        BuildSummary summary = report.summary;

        Debug.Log($"Build finished: result={summary.result}, size={summary.totalSize} bytes, path={apkPath}");

        if (summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
