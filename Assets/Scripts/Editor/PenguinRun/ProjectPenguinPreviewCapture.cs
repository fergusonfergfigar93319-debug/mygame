#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 玩家企鹅静态预览：跑动/滑铲/死亡三种姿态 × 背面（游戏视角）+ 正面机位，
    /// 输出 Artifacts/PenguinPreviews/ 用于核对单层网格、朝向与配饰。
    /// </summary>
    public static class ProjectPenguinPreviewCapture
    {
        [MenuItem("PenguinRun/Visuals/Capture Penguin Previews")]
        public static void CaptureAll()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "PenguinPreviews");
            Directory.CreateDirectory(output);
            Capture(output, "run_back", sliding: false, gameOver: false, frontView: false);
            Capture(output, "run_front", sliding: false, gameOver: false, frontView: true);
            Capture(output, "slide_back", sliding: true, gameOver: false, frontView: false);
            Capture(output, "defeat_front", sliding: false, gameOver: true, frontView: true);
            Debug.Log($"[ProjectPenguinPreviewCapture] Wrote previews to {output}");
        }

        public static void CaptureAllFromCommandLine()
        {
            CaptureAll();
            EditorApplication.Exit(0);
        }

        private static void Capture(string outputDir, string name, bool sliding, bool gameOver, bool frontView)
        {
            var sceneRoot = new GameObject("PenguinPreviewScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Material groundMaterial = null;
            try
            {
                var player = new GameObject("Penguin Character");
                player.transform.SetParent(sceneRoot.transform, false);
                var character = PenguinCharacter.CreateCute(player.transform);
                player.transform.localScale = Vector3.one * 0.86f;

                // 推进若干帧让姿态收敛（lerp 动画）
                for (var i = 0; i < 120; i++)
                {
                    character.Animate(1f / 60f, true, true, sliding, gameOver, 0.5f, 0f, 0f, 0f, 0f, 0f);
                }

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "PreviewGround";
                ground.transform.SetParent(sceneRoot.transform, false);
                ground.transform.position = new Vector3(0f, -0.02f, 0f);
                groundMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.33f, 0.43f) };
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                var keyLight = new GameObject("KeyLight", typeof(Light));
                keyLight.transform.SetParent(sceneRoot.transform, false);
                keyLight.transform.rotation = Quaternion.Euler(46f, -30f, 0f);
                var light = keyLight.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.3f;
                light.color = new Color(1f, 0.92f, 0.82f);

                var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
                cameraObject.transform.SetParent(sceneRoot.transform, false);
                cameraObject.transform.position = frontView
                    ? new Vector3(0.6f, 1.5f, 3.2f)
                    : new Vector3(0f, 2.2f, -3.4f);
                cameraObject.transform.LookAt(new Vector3(0f, 0.8f, 0f));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 40f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.08f, 0.12f);

                renderTexture = new RenderTexture(960, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(960, 720, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = null;
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
                if (image != null) Object.DestroyImmediate(image);
                if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
                Object.DestroyImmediate(sceneRoot);
            }
        }
    }
}
#endif
