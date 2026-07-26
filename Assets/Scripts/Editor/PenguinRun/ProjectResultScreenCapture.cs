#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using PenguinRun.Game;
using PenguinRun.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 拍摄结算页与死亡页，并断言下半区的三个区块（统计格 / 奖励横幅 / 主按钮）互不重叠。
    ///
    /// 这条门禁补的是一个此前没有任何机制能发现的缺陷：奖励横幅 56px 里有 50px 压在
    /// 主按钮下面，双倍鱼干券与 Boss 速杀奖励被告知在玩家看不见的地方。它只在玩家真的
    /// 拿到奖励时才出现，而 ProjectPlayerJourneySmokeTest 的样本局没有奖励——加上它在
    /// 本机 batchmode 下截不到帧，这个缺陷可以一直存在下去。
    ///
    /// 用矩形相交判定而不是像素比对：像素阈值会随配色和字体漂移，而「两个可见区块不许
    /// 叠在一起」是一条稳定的布局契约。
    /// </summary>
    public static class ProjectResultScreenCapture
    {
        private const int Width = 1280;
        private const int Height = 720;

        public static void CaptureFromCommandLine()
        {
            try
            {
                Capture();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("PenguinRun/Visuals/Capture Result Screens")]
        public static void Capture()
        {
            var outputDir = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "ResultScreens");
            Directory.CreateDirectory(outputDir);

            var errors = new List<string>();

            // 带奖励的一局：只有这种局面才会出现奖励横幅，也正是出过遮挡缺陷的那种局面。
            var rich = new RunOutcomeRouter.Result
            {
                Score = 18420,
                DistanceMeters = 2480,
                Coins = 312,
                SurvivalSeconds = 184f,
                NewBest = true,
                RankByScore = 3,
                Daily = false,
                BonusCoins = 96,
                BonusScore = 1500,
                ConsumedDoubleFishTicket = true,
                ConsumedScoreBoostTicket = true,
                LastBossSpeedTier = "S",
                LastBossSpeedFishBonus = 40,
                LastBossSpeedScoreBonus = 600,
            };

            CaptureResult(outputDir, "result-new-best.png", rich, errors);

            var plain = new RunOutcomeRouter.Result
            {
                Score = 640,
                DistanceMeters = 310,
                Coins = 24,
                SurvivalSeconds = 27f,
                NewBest = false,
                RankByScore = 41,
                Daily = false,
            };
            CaptureResult(outputDir, "result-plain.png", plain, errors);

            if (errors.Count > 0)
            {
                var message = "[ProjectResultScreenCapture] FAIL:\n" + string.Join("\n", errors);
                Debug.LogError(message);
                throw new UnityException(message);
            }

            Debug.Log($"[ProjectResultScreenCapture] PASS: result screens captured to {outputDir}");
        }

        private static void CaptureResult(string outputDir, string fileName,
            RunOutcomeRouter.Result result, List<string> errors)
        {
            var root = new GameObject("ResultCaptureScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            try
            {
                var cameraObject = new GameObject("UiCamera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.09f, 0.14f);
                camera.orthographic = true;

                var canvasObject = new GameObject("Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                var canvas = canvasObject.GetComponent<Canvas>();
                // Overlay canvases cannot be pointed at a RenderTexture; camera mode can.
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 1f;

                RunResultOverlay.Show(canvasObject.transform, result, null);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvasObject.transform);

                AssertNoOverlap(canvasObject.transform, fileName, errors);

                renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
                    { antiAliasing = 4 };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();

                var bytes = image.EncodeToPNG();
                if (bytes == null || bytes.Length < 1024)
                    errors.Add($"{fileName}: capture was empty.");
                else
                    File.WriteAllBytes(Path.Combine(outputDir, fileName), bytes);
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) Object.DestroyImmediate(image);
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>下半区的可见区块两两不得相交。</summary>
        private static void AssertNoOverlap(Transform canvas, string label, List<string> errors)
        {
            var names = new[] { "Stat_距离", "Stat_鱼干", "Stat_存活", "BonusBanner", "Dismiss" };
            var found = new List<(string name, Rect rect)>();

            foreach (var name in names)
            {
                var target = FindDeep(canvas, name);
                // 奖励横幅只在有奖励时存在；缺席不算错误。
                if (target == null) continue;
                found.Add((name, WorldRect(target)));
            }

            if (found.Count < 4)
            {
                errors.Add($"{label}: expected at least 4 layout blocks, found {found.Count}.");
                return;
            }

            for (var i = 0; i < found.Count; i++)
            {
                for (var j = i + 1; j < found.Count; j++)
                {
                    if (!found[i].rect.Overlaps(found[j].rect)) continue;
                    errors.Add($"{label}: '{found[i].name}' overlaps '{found[j].name}' " +
                               $"({found[i].rect} vs {found[j].rect}).");
                }
            }
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var min = Vector2.Min(corners[0], corners[2]);
            var max = Vector2.Max(corners[0], corners[2]);
            return new Rect(min, max - min);
        }

        private static RectTransform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent as RectTransform;
            for (var i = 0; i < parent.childCount; i++)
            {
                var hit = FindDeep(parent.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
#endif
