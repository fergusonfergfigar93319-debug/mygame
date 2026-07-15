#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ProjectThemeMechanicPreviewCapture
    {
        [MenuItem("PenguinRun/Theme Mechanics/Capture Six Apparatus Previews")]
        public static void CaptureAll()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "ThemeMechanicPreviews");
            Directory.CreateDirectory(output);
            foreach (var definition in ThemeMechanicCatalog.Definitions)
            {
                CaptureTheme(definition,
                    Path.Combine(output, RunnerVisualAssetCatalog.ThemeSlug(definition.Theme) + ".png"));
            }
            Debug.Log($"[ProjectThemeMechanicPreviewCapture] Wrote six previews to {output}");
        }

        public static void CaptureFromCommandLine()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureTheme(ThemeMechanicDefinition definition, string outputPath)
        {
            var sceneRoot = new GameObject("ThemeMechanicPreviewScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Material groundMaterial = null;
            try
            {
                Instantiate(definition.ApparatusA, sceneRoot.transform,
                    new Vector3(-3.6f, 0f, 0f), 1.12f, -12f);
                Instantiate(definition.ApparatusB, sceneRoot.transform,
                    new Vector3(0f, 0f, .2f), 1.08f, 8f);
                Instantiate(definition.Signature, sceneRoot.transform,
                    new Vector3(3.65f, 0f, .15f), .92f, 18f);

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "PreviewGround";
                ground.transform.SetParent(sceneRoot.transform, false);
                ground.transform.position = new Vector3(0f, -.04f, 1.2f);
                ground.transform.localScale = new Vector3(1.35f, 1f, .70f);
                groundMaterial = new Material(Shader.Find("Standard"));
                groundMaterial.color = GroundColor(definition.Theme);
                groundMaterial.SetFloat("_Glossiness", .18f);
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                var keyObject = new GameObject("KeyLight", typeof(Light));
                keyObject.transform.SetParent(sceneRoot.transform, false);
                keyObject.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
                var key = keyObject.GetComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.45f;
                key.color = new Color(1f, .90f, .78f);
                key.shadows = LightShadows.Soft;

                var fillObject = new GameObject("FillLight", typeof(Light));
                fillObject.transform.SetParent(sceneRoot.transform, false);
                fillObject.transform.position = new Vector3(-4f, 4.8f, -2.5f);
                var fill = fillObject.GetComponent<Light>();
                fill.type = LightType.Point;
                fill.range = 18f;
                fill.intensity = 2.8f;
                fill.color = new Color(.42f, .72f, 1f);

                var rimObject = new GameObject("RimLight", typeof(Light));
                rimObject.transform.SetParent(sceneRoot.transform, false);
                rimObject.transform.position = new Vector3(4.8f, 3.2f, 3f);
                var rim = rimObject.GetComponent<Light>();
                rim.type = LightType.Point;
                rim.range = 15f;
                rim.intensity = 2.2f;
                rim.color = new Color(.35f, 1f, .78f);

                var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
                cameraObject.transform.SetParent(sceneRoot.transform, false);
                cameraObject.transform.position = new Vector3(0f, 4.2f, -12.5f);
                cameraObject.transform.LookAt(new Vector3(0f, 1.05f, .55f));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 40f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.045f, .065f, .105f);
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
                    throw new UnityException($"Preview for {definition.Theme} contains no visible geometry.");
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

        public static bool HasVisibleContent(Texture2D image)
        {
            var pixels = image.GetPixels32();
            if (pixels.Length == 0) return false;
            var sample = pixels[0];
            var changed = 0;
            foreach (var pixel in pixels)
            {
                var distance = Mathf.Abs(pixel.r - sample.r)
                    + Mathf.Abs(pixel.g - sample.g)
                    + Mathf.Abs(pixel.b - sample.b);
                if (distance <= 8) continue;
                if (++changed >= pixels.Length / 100) return true;
            }
            return false;
        }

        private static void Instantiate(string apparatusPath, Transform parent,
            Vector3 position, float scale, float yaw)
        {
            var assetPath = ThemeMechanicPrefabBuilder.AssetPath(apparatusPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) throw new FileNotFoundException(assetPath);
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) throw new UnityException($"Could not instantiate {assetPath}");
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;
            foreach (var lod in instance.GetComponentsInChildren<LODGroup>(true)) lod.ForceLOD(0);
        }

        private static Color GroundColor(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.CedarRuins => new Color(.14f, .19f, .15f),
            RunnerMapTheme.AuroraField => new Color(.075f, .10f, .16f),
            RunnerMapTheme.MistDike => new Color(.14f, .19f, .20f),
            RunnerMapTheme.OceanReef => new Color(.055f, .20f, .23f),
            RunnerMapTheme.SkyFlight => new Color(.31f, .47f, .61f),
            _ => new Color(.16f, .28f, .38f),
        };
    }
}
#endif
