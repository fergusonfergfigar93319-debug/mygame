#if UNITY_EDITOR
using System.Collections.Generic;
using PenguinRun.Game.UI;
using PenguinRun.Game.UI.Lobby;
using PenguinRun.Game.UI.Storybook;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 断言大厅里**会显示出来**的美术都是真资源，而不是降级占位。
    ///
    /// 背景：`mascot_gugugaga.png` 与 `lobby_panorama.png` 的 .meta 只有 60 字节
    /// （只有 fileFormatVersion 和 guid，没有 TextureImporter 块），资源从未被正确导入。
    /// 于是 `StorybookArtCatalog.Mascot` 一路回退到 `CreateFallbackMascot()` —— 也就是
    /// `UiBuilder.CircleSprite`，一个纯白圆。大厅中央的吉祥物就是这么变成白色圆块的。
    ///
    /// 整条回退链是静默的：没有异常、没有日志，画面上只是少了个角色。这条门禁把它变成
    /// 构建期失败。
    /// </summary>
    public static class ProjectLobbyArtValidation
    {
        public static void ValidateFromCommandLine()
        {
            try
            {
                Validate();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("PenguinRun/Validate/Lobby Art")]
        public static void Validate()
        {
            var errors = new List<string>();
            var circle = UiBuilder.CircleSprite;

            // 吉祥物是大厅的视觉主体，必须是真资源。
            var mascot = CampVisualAssetCatalog.Mascot;
            if (mascot == null)
                errors.Add("Lobby mascot resolves to null.");
            else if (mascot == circle)
                errors.Add("Lobby mascot fell back to the placeholder circle; its texture failed to import.");

            if (StorybookArtCatalog.LobbyPanorama == null)
                errors.Add("Lobby panorama failed to load; check its .meta has a TextureImporter block.");

            // 伙伴允许缺席（美术尚未产出），但**不允许**退化成吉祥物或占位圆——
            // 那会在大厅里画出两个同样的角色。
            foreach (var drorUnlocked in new[] { false, true })
            {
                var companion = CampVisualAssetCatalog.Companion(drorUnlocked);
                if (companion == null) continue;
                if (companion == circle)
                    errors.Add($"Companion (dror={drorUnlocked}) fell back to the placeholder circle.");
                if (companion == mascot)
                    errors.Add($"Companion (dror={drorUnlocked}) resolves to the mascot sprite; " +
                               "the lobby would draw the same character twice.");
            }

            if (errors.Count > 0)
            {
                var message = "[ProjectLobbyArtValidation] FAIL:\n" + string.Join("\n", errors);
                Debug.LogError(message);
                throw new UnityException(message);
            }

            var companionPresent = CampVisualAssetCatalog.Companion(false) != null;
            Debug.Log("[ProjectLobbyArtValidation] PASS: mascot and panorama are real assets; " +
                      $"companion art {(companionPresent ? "present" : "absent (node hidden)")}.");
        }
    }
}
#endif
