#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 从玩家视角捕捉 6 个 Boss 的静态预览（rigged 模型或程序化回退），
    /// 用于核对朝向、体型、贴图与落地对齐。输出 Artifacts/BossPreviews/。
    /// </summary>
    public static class ProjectBossPreviewCapture
    {
        [MenuItem("PenguinRun/Visuals/Capture Boss Previews")]
        public static void CaptureAll()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "BossPreviews");
            Directory.CreateDirectory(output);
            foreach (var def in BossDefinitions.GetAllBossesOrdered())
            {
                CaptureBoss(def, Path.Combine(output, def.BossId + ".png"));
            }
            Debug.Log($"[ProjectBossPreviewCapture] Wrote previews to {output}");
        }

        public static void CaptureAllFromCommandLine()
        {
            CaptureAll();
            EditorApplication.Exit(0);
        }

        private static void CaptureBoss(BossDefinition def, string outputPath)
        {
            var sceneRoot = new GameObject("BossPreviewScene");
            BossEncounter boss = null;
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Material groundMaterial = null;
            try
            {
                boss = new BossEncounter(def, 0f, 3f, 0f);
                boss.Root.transform.SetParent(sceneRoot.transform, false);

                // 快进出场：让 Boss 落位到 BossActiveZ 并进入 Active 姿态
                for (var i = 0; i < 240; i++)
                {
                    boss.Tick(1f / 60f, 0f, 0f);
                }

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "PreviewGround";
                ground.transform.SetParent(sceneRoot.transform, false);
                ground.transform.position = new Vector3(0f, -0.02f, 12f);
                ground.transform.localScale = new Vector3(2.4f, 1f, 2.4f);
                groundMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.16f, 0.24f, 0.34f) };
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                var keyLight = new GameObject("KeyLight", typeof(Light));
                keyLight.transform.SetParent(sceneRoot.transform, false);
                keyLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                var light = keyLight.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.9f, 0.78f);

                // 玩家视角：站在 z≈0 后上方向前看
                var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
                cameraObject.transform.SetParent(sceneRoot.transform, false);
                cameraObject.transform.position = new Vector3(0f, 4.4f, 3.6f);
                cameraObject.transform.LookAt(new Vector3(0f, 2.0f, BossSystem.BossActiveZ));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 46f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.08f, 0.12f);

                renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = null;
                boss?.Dispose();
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
                if (image != null) Object.DestroyImmediate(image);
                if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
                Object.DestroyImmediate(sceneRoot);
            }
        }
    }
}
#endif
