#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// Opens MainMenu and enters Play Mode so a human can inspect runtime visuals.
    /// Launch without -batchmode/-quit so the Editor window stays up.
    /// </summary>
    internal static class ProjectEnterPlayMode
    {
        private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";

        [MenuItem("PenguinRun/Play/Enter Main Menu Play Mode")]
        public static void EnterFromMenu() => Enter();

        public static void EnterFromCommandLine()
        {
            EditorApplication.delayCall += Enter;
        }

        private static void Enter()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!System.IO.File.Exists(MainMenuScene))
            {
                Debug.LogError($"[ProjectEnterPlayMode] Missing scene: {MainMenuScene}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            Debug.Log("[ProjectEnterPlayMode] Entering Play Mode on MainMenu — use 翻开旅程 / 出发 to run.");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
