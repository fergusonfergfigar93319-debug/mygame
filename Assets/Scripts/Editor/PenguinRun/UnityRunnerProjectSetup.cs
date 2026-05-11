using System.IO;
using PenguinRun;
using PenguinRun.Game.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 全 Unity 化后的项目脚手架与构建入口：
    /// - 创建/保证 MainMenu 与 PenguinRunner 两个场景；
    /// - 配置 PlayerSettings；
    /// - 直接构建 Android APK（不再 export Android library）。
    /// </summary>
    [InitializeOnLoad]
    public static class UnityRunnerProjectSetup
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RunnerScenePath = "Assets/Scenes/PenguinRunner.unity";
        private const string ApkOutputDir = "Build/Android";
        private const string ApkOutputName = "PenguinRun.apk";

        static UnityRunnerProjectSetup()
        {
            EditorApplication.delayCall += ConfigureEditorStartupScene;
        }

        [MenuItem("Penguin Run/Setup Project")]
        public static void SetupProject()
        {
            EnsureMainMenuScene();
            EnsureRunnerScene();
            ConfigureBuildSettings();
            ConfigureEditorStartupScene();
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Penguin Run/Open Main Menu")]
        public static void OpenMainMenu()
        {
            EnsureMainMenuScene();
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Penguin Run/Open Runner Scene")]
        public static void OpenRunnerScene()
        {
            EnsureRunnerScene();
            EditorSceneManager.OpenScene(RunnerScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Penguin Run/Build Android APK")]
        public static void BuildAndroidApk()
        {
            SetupProject();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            Directory.CreateDirectory(ApkOutputDir);
            var outPath = Path.Combine(ApkOutputDir, ApkOutputName);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainMenuScenePath, RunnerScenePath },
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception($"Unity Android APK build failed: {report.summary.result}");
            }
            Debug.Log($"APK built: {outPath}");
        }

        private static void EnsureMainMenuScene()
        {
            if (File.Exists(MainMenuScenePath))
            {
                return;
            }
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MainMenuRoot");
            root.AddComponent<MainMenuBootstrap>();
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void EnsureRunnerScene()
        {
            if (File.Exists(RunnerScenePath))
            {
                return;
            }
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Penguin Runner Game");
            go.AddComponent<PenguinRunnerGame>();
            EditorSceneManager.SaveScene(scene, RunnerScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(RunnerScenePath, true),
            };
        }

        private static void ConfigureEditorStartupScene()
        {
            var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (mainMenu != null)
            {
                EditorSceneManager.playModeStartScene = mainMenu;
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "PenguinRun";
            PlayerSettings.productName = "企鹅快跑";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.example.mygame");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        }
    }
}
