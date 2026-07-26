#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 以跑酷真实机位（RunnerCameraTuning：behind 10.5 / height 6.5 / aimAhead 19 / FOV 72）
    /// 拍摄主角在三条车道上的**背面**读感，并断言角色区域仍有可分辨的明度层次。
    ///
    /// 这条门禁对应 CURRENT_DESIGN 里长期挂着的一条瑕疵：「跑酷实距下红围巾与护目镜糊进
    /// 深色身体，仅橙色脚掌清晰可读」。近距离的 ProjectPenguinPreviewCapture 看不出这个问题
    /// ——它在 4m 用 34° 镜头拍，而玩家整局看到的是 10.5m 外 72° 广角下的背影。
    /// </summary>
    public static class ProjectAvatarGameplayViewCapture
    {
        // 角色像素统计（0-255），2026-07-26 实测：
        //   世界空间固定主光（旧）: meanLuma=53.8 detailStdDev=38.43
        //   视角空间主光（现）:     meanLuma=76.0 detailStdDev=36.89
        //
        // meanLuma 是主判据：它区分「角色被背光压成黑块」与「朝向镜头的一面有光」，
        // 而旧着色器的 53.8 会被 65 卡住——阈值必须真的能挡回归，否则等于没有门禁。
        // detailStdDev 兜底剪影内部层次（围巾/脚掌/鳍肢不能与身体同色），取实测值的一半。
        private const float MinMeanLuma = 65f;
        private const float MinDetailStdDev = 18f;

        private const int Width = 1600;
        private const int Height = 900;

        // 与 RunnerTuningConfig.GroundY / RunnerCameraTuning 保持一致。
        private const float GroundY = 0.9f;
        private const float LaneWidth = 3f;
        private const float PlayerVisualScale = 0.86f;

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

        [MenuItem("PenguinRun/Visuals/Capture Avatar Gameplay View")]
        public static void Capture()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "AvatarGameplayView.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);

            // 同机位拍两张：带角色与不带角色。差值即角色像素掩码，
            // 这样统计只覆盖角色本体，不被跑道和天空稀释。
            var withAvatar = Render(true, output);
            var withoutAvatar = Render(false, null);

            var luminances = new List<float>(withAvatar.Length / 8);
            for (var i = 0; i < withAvatar.Length; i++)
            {
                var a = withAvatar[i];
                var b = withoutAvatar[i];
                var delta = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                if (delta < 24) continue;
                luminances.Add(0.2126f * a.r + 0.7152f * a.g + 0.0722f * a.b);
            }

            if (luminances.Count < 2000)
                throw new UnityException(
                    $"Avatar covered only {luminances.Count} px at gameplay distance; the capture scene is wrong.");

            var mean = 0f;
            foreach (var value in luminances) mean += value;
            mean /= luminances.Count;

            var variance = 0f;
            foreach (var value in luminances) variance += (value - mean) * (value - mean);
            var stdDev = Mathf.Sqrt(variance / luminances.Count);

            Debug.Log($"[ProjectAvatarGameplayViewCapture] avatarPixels={luminances.Count} " +
                      $"meanLuma={mean:F1} detailStdDev={stdDev:F2} -> {output}");

            if (mean < MinMeanLuma)
                throw new UnityException(
                    $"Avatar is unlit at gameplay distance: meanLuma={mean:F1} < {MinMeanLuma}. " +
                    "The key light must stay on the camera-facing side of the character.");

            if (stdDev < MinDetailStdDev)
                throw new UnityException(
                    $"Avatar reads as a flat silhouette at gameplay distance: detailStdDev={stdDev:F2} < {MinDetailStdDev}.");
        }

        private static Color32[] Render(bool includeAvatar, string outputPath)
        {
            var sceneRoot = new GameObject("AvatarGameplayViewScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Material trackMaterial = null;
            try
            {
                var track = GameObject.CreatePrimitive(PrimitiveType.Cube);
                track.name = "Track";
                track.transform.SetParent(sceneRoot.transform, false);
                track.transform.position = new Vector3(0f, GroundY - 0.45f, 18f);
                track.transform.localScale = new Vector3(9.6f, 0.9f, 70f);
                trackMaterial = new Material(Shader.Find("Standard"))
                    { color = new Color(0.30f, 0.55f, 0.72f) };
                track.GetComponent<Renderer>().sharedMaterial = trackMaterial;

                if (includeAvatar)
                {
                    var definition = Resources.Load<RunnerAvatarDefinition>(
                        "PenguinRun/Characters/Gugugaga/GugugagaAvatar");
                    // 三条车道各放一个，跑离镜头（绕 Y 轴 180°），这是玩家整局看到的角度。
                    foreach (var lane in new[] { -LaneWidth, 0f, LaneWidth })
                    {
                        var player = new GameObject($"Penguin Lane {lane}");
                        player.transform.SetParent(sceneRoot.transform, false);
                        player.transform.position = new Vector3(lane, GroundY, 0f);
                        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                        player.transform.localScale = Vector3.one * PlayerVisualScale;
                        DriveRunPose(PlayerAvatarPresenter.Create(player.transform, definition), player);
                    }
                }

                CreateLight(sceneRoot.transform, "KeyLight", Quaternion.Euler(42f, -32f, 0f),
                    1.45f, new Color(1f, 0.92f, 0.82f));
                CreateLight(sceneRoot.transform, "RimLight", Quaternion.Euler(25f, 145f, 0f),
                    0.75f, new Color(0.45f, 0.72f, 1f));

                var cameraObject = new GameObject("GameplayCamera", typeof(Camera));
                cameraObject.transform.SetParent(sceneRoot.transform, false);
                cameraObject.transform.position = new Vector3(0f, GroundY + 6.5f, -10.5f);
                cameraObject.transform.LookAt(new Vector3(0f, GroundY + 0.25f, 19f));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 72f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.06f, 0.1f);
                camera.allowHDR = true;

                renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
                    { antiAliasing = 4 };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();

                if (!string.IsNullOrEmpty(outputPath))
                {
                    var bytes = image.EncodeToPNG();
                    if (bytes == null || bytes.Length < 1024)
                        throw new UnityException("Avatar gameplay view was empty.");
                    File.WriteAllBytes(outputPath, bytes);
                }

                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) Object.DestroyImmediate(image);
                if (trackMaterial != null) Object.DestroyImmediate(trackMaterial);
                Object.DestroyImmediate(sceneRoot);
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
            }
        }

        private static void DriveRunPose(IPlayerCharacterPresentation character, GameObject player)
        {
            var state = new RunnerAvatarState(true, true, false, false,
                0.72f, 0f, 0f, RunnerAvatarBuffFlags.None);
            var animators = player.GetComponentsInChildren<Animator>(true);

            for (var i = 0; i < 4; i++)
            {
                character.Tick(1f / 60f, state);
                foreach (var animator in animators) animator.Update(1f / 60f);
            }

            // 与 ProjectPenguinPreviewCapture 一致：强制取片段中段，避免停在 Idle->Run 的混合帧。
            foreach (var animator in animators)
            {
                animator.Play("Run", 0, 0.4f);
                animator.Update(0f);
            }

            for (var i = 0; i < 24; i++)
            {
                character.Tick(1f / 60f, state);
                foreach (var animator in animators) animator.Update(1f / 60f);
            }

            foreach (var animator in animators)
            {
                animator.Play("Run", 0, 0.4f);
                animator.Update(0f);
            }
        }

        private static void CreateLight(Transform parent, string name, Quaternion rotation,
            float intensity, Color color)
        {
            var lightObject = new GameObject(name, typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = rotation;
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.Soft;
        }
    }
}
#endif
