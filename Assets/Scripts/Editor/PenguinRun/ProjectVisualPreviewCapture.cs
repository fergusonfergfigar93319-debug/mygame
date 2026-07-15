#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ProjectVisualPreviewCapture
    {
        [MenuItem("PenguinRun/Visuals/Capture Batch One Previews")]
        public static void CaptureAll()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "VisualPreviews");
            Directory.CreateDirectory(output);
            foreach (RunnerMapTheme theme in System.Enum.GetValues(typeof(RunnerMapTheme)))
                CaptureTheme(theme, Path.Combine(output, RunnerVisualAssetCatalog.ThemeSlug(theme) + ".png"));
            Debug.Log($"[ProjectVisualPreviewCapture] Wrote six previews to {output}");
        }

        public static void CaptureAllFromCommandLine()
        {
            CaptureAll();
            EditorApplication.Exit(0);
        }

        private static void CaptureTheme(RunnerMapTheme theme, string outputPath)
        {
            var sceneRoot = new GameObject("VisualPreviewScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Material groundMaterial = null;
            try
            {
                Instantiate(RunnerVisualAssetCatalog.ForObstacle(theme, ObstacleKind.StandardHigh), sceneRoot.transform,
                    new Vector3(-4.2f, 0f, 0f), 1.15f);
                Instantiate(RunnerVisualAssetCatalog.ForObstacle(theme, ObstacleKind.SlideGate), sceneRoot.transform,
                    new Vector3(-2.25f, .25f, 0f), 1.35f);
                Instantiate(RunnerVisualAssetCatalog.ForObstacle(theme, ObstacleKind.WideWall), sceneRoot.transform,
                    new Vector3(.45f, 0f, .1f), .92f);
                Instantiate(RunnerVisualAssetCatalog.ForPickup(PickupVisualId.ScoreStar), sceneRoot.transform,
                    new Vector3(3.65f, 1.0f, 0f), 1.25f);

                Instantiate(RunnerVisualAssetCatalog.ForEnvironment(theme, EnvironmentPropId.Landmark), sceneRoot.transform,
                    new Vector3(-3.9f, 0f, 3.1f), .74f);
                Instantiate(RunnerVisualAssetCatalog.ForEnvironment(theme, EnvironmentPropId.Signpost), sceneRoot.transform,
                    new Vector3(-1.25f, 0f, 3.1f), .92f);
                Instantiate(RunnerVisualAssetCatalog.ForEnvironment(theme, EnvironmentPropId.Light), sceneRoot.transform,
                    new Vector3(.95f, 0f, 3.1f), 1.0f);
                Instantiate(RunnerVisualAssetCatalog.ForEnvironment(theme, EnvironmentPropId.EdgeProp), sceneRoot.transform,
                    new Vector3(3.35f, 0f, 3.1f), 1.18f);

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "PreviewGround";
                ground.transform.SetParent(sceneRoot.transform, false);
                ground.transform.position = new Vector3(0f, -.03f, 1.6f);
                ground.transform.localScale = new Vector3(1.25f, 1f, .72f);
                groundMaterial = new Material(Shader.Find("Standard"));
                groundMaterial.color = theme switch
                {
                    RunnerMapTheme.CedarRuins => new Color(.16f,.21f,.18f),
                    RunnerMapTheme.AuroraField => new Color(.10f,.12f,.18f),
                    RunnerMapTheme.MistDike => new Color(.17f,.21f,.22f),
                    RunnerMapTheme.OceanReef => new Color(.08f,.25f,.28f),
                    RunnerMapTheme.SkyFlight => new Color(.37f,.55f,.70f),
                    _ => new Color(.20f,.33f,.43f),
                };
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                var keyLight = new GameObject("KeyLight", typeof(Light));
                keyLight.transform.SetParent(sceneRoot.transform, false);
                keyLight.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                var light = keyLight.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1f, .88f, .74f);

                var fillLight = new GameObject("FillLight", typeof(Light));
                fillLight.transform.SetParent(sceneRoot.transform, false);
                fillLight.transform.position = new Vector3(-4f, 5f, -2f);
                var fill = fillLight.GetComponent<Light>();
                fill.type = LightType.Point;
                fill.range = 18f;
                fill.intensity = 2.5f;
                fill.color = new Color(.48f,.72f,1f);

                var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
                cameraObject.transform.SetParent(sceneRoot.transform, false);
                cameraObject.transform.position = new Vector3(0f, 4.6f, -11.4f);
                cameraObject.transform.LookAt(new Vector3(0f, 1.25f, 1.6f));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 42f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.055f,.075f,.11f);
                camera.allowHDR = true;

                renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                    useMipMap = false,
                };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                if (!HasVisibleContent(image))
                    throw new UnityException(
                        $"Preview for {theme} contains no visible geometry. Run capture with graphics enabled (omit -nographics).");
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) Object.DestroyImmediate(image);
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
                if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
                Object.DestroyImmediate(sceneRoot);
            }
        }

        private static bool HasVisibleContent(Texture2D image)
        {
            var pixels = image.GetPixels32();
            if (pixels.Length == 0) return false;
            var sample = pixels[0];
            var changed = 0;
            const int channelTolerance = 8;
            foreach (var pixel in pixels)
            {
                var distance = Mathf.Abs(pixel.r - sample.r) +
                               Mathf.Abs(pixel.g - sample.g) +
                               Mathf.Abs(pixel.b - sample.b);
                if (distance <= channelTolerance) continue;
                changed++;
                if (changed >= pixels.Length / 100) return true;
            }
            return false;
        }

        private static void Instantiate(RunnerVisualDescriptor descriptor, Transform parent,
            Vector3 position, float scale)
        {
            var path = $"Assets/Resources/{descriptor.ResourcePath}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException(path);
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) throw new UnityException($"Could not instantiate {path}");
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            instance.transform.localScale = Vector3.one * scale;
            foreach (var lod in instance.GetComponentsInChildren<LODGroup>(true)) lod.ForceLOD(0);
        }
    }
}
#endif
