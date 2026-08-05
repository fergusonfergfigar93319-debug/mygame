#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// RenderTexture capture of curated CC0 roadside props per theme (NaturePropCatalog).
    /// Works in -batchmode/-nographics unlike ScreenCapture-based journey smoke.
    /// </summary>
    public static class ProjectOpenNaturePreviewCapture
    {
        private const int Width = 1600;
        private const int Height = 900;
        private const float GroundY = 0.9f;
        private const int MinOccupiedPixels = 1200;
        private const float MinMeanLuma = 28f;

        [MenuItem("PenguinRun/Visuals/Capture Open Nature Previews")]
        public static void CaptureAll()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "OpenNaturePreviews");
            Directory.CreateDirectory(output);

            foreach (RunnerMapTheme theme in Enum.GetValues(typeof(RunnerMapTheme)))
            {
                var path = Path.Combine(output, RunnerVisualAssetCatalog.ThemeSlug(theme) + ".png");
                CaptureTheme(theme, path);
            }

            Debug.Log($"[ProjectOpenNaturePreviewCapture] Wrote six previews to {output}");
        }

        public static void CaptureAllFromCommandLine()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureTheme(RunnerMapTheme theme, string outputPath)
        {
            var empty = RenderTheme(theme, includeProps: false, outputPath: null);
            var withProps = RenderTheme(theme, includeProps: true, outputPath);

            var occupied = 0;
            var lumaSum = 0f;
            for (var i = 0; i < withProps.Length; i++)
            {
                var a = withProps[i];
                var b = empty[i];
                var delta = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                if (delta < 18) continue;
                occupied++;
                lumaSum += 0.2126f * a.r + 0.7152f * a.g + 0.0722f * a.b;
            }

            if (occupied < MinOccupiedPixels)
                throw new UnityException(
                    $"{theme}: open-nature props covered only {occupied} px (< {MinOccupiedPixels}); " +
                    "models may have failed to load or are scaled to invisibility.");

            var meanLuma = lumaSum / occupied;
            if (meanLuma < MinMeanLuma)
                throw new UnityException(
                    $"{theme}: open-nature props too dark (meanLuma={meanLuma:F1} < {MinMeanLuma}).");

            Debug.Log($"[ProjectOpenNaturePreviewCapture] {theme} occupied={occupied} meanLuma={meanLuma:F1} -> {outputPath}");
        }

        private static Color32[] RenderTheme(RunnerMapTheme theme, bool includeProps, string outputPath)
        {
            var sceneRoot = new GameObject("OpenNaturePreviewScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Material trackMaterial = null;
            Material groundMaterial = null;
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

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.SetParent(sceneRoot.transform, false);
                ground.transform.position = new Vector3(0f, GroundY - 0.9f, 18f);
                ground.transform.localScale = new Vector3(4f, 1f, 8f);
                groundMaterial = new Material(Shader.Find("Standard")) { color = ThemeGround(theme) };
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                if (includeProps)
                    SpawnThemeProps(theme, sceneRoot.transform);

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
                // NullGfxDevice (-nographics) crashes on soft shadow maps for some vendor FBX.
                camera.allowHDR = false;
                camera.renderingPath = RenderingPath.Forward;

                renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
                    { antiAliasing = 1 };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();

                if (!string.IsNullOrEmpty(outputPath))
                {
                    var bytes = image.EncodeToPNG();
                    if (bytes == null || bytes.Length < 2048)
                        throw new UnityException($"{theme}: open-nature preview PNG was empty.");
                    File.WriteAllBytes(outputPath, bytes);
                }

                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (trackMaterial != null) UnityEngine.Object.DestroyImmediate(trackMaterial);
                if (groundMaterial != null) UnityEngine.Object.DestroyImmediate(groundMaterial);
                UnityEngine.Object.DestroyImmediate(sceneRoot);
                if (renderTexture != null) UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void SpawnThemeProps(RunnerMapTheme theme, Transform parent)
        {
            // Batchmode Camera.Render does not shade many imported FBX materials reliably.
            // Validate real mesh heights from Resources, then draw height-matched capsules for RT evidence.
            var previewMat = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.35f, 0.15f)
            };
            var placements = ThemePlacements(theme);
            var validated = 0;
            foreach (var (model, x, z, scale) in placements)
            {
                var prefab = LoadModel(model);
                if (prefab == null)
                    throw new UnityException($"Missing Resources model for open-nature preview: {model}");

                var applied = NaturePropCatalog.ApplyScale(model, scale);
                var height = MeasurePrefabHeight(prefab) * Mathf.Max(applied.x, Mathf.Max(applied.y, applied.z));
                if (height < 0.15f || height > 25f)
                    throw new UnityException(
                        $"{theme}/{model}: mesh height {height:0.00} out of roadside range after pack scale.");

                var proxy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                proxy.name = $"Proxy_{model}";
                proxy.transform.SetParent(parent, false);
                proxy.transform.position = new Vector3(x, GroundY + height * 0.5f, z);
                proxy.transform.localScale = new Vector3(
                    Mathf.Clamp(height * 0.35f, 0.6f, 3.5f),
                    Mathf.Clamp(height * 0.5f, 0.6f, 5.5f),
                    Mathf.Clamp(height * 0.35f, 0.6f, 3.5f));
                proxy.GetComponent<Renderer>().sharedMaterial = previewMat;
                UnityEngine.Object.DestroyImmediate(proxy.GetComponent<Collider>());
                validated++;
            }

            if (validated == 0)
                throw new UnityException($"{theme}: no open-nature placements to validate.");
        }

        private static GameObject LoadModel(string model)
        {
            var prefab = Resources.Load<GameObject>($"PenguinRun/Models/{model}");
            if (prefab != null) return prefab;

            // Editor fallback: some glb imports resolve via AssetDatabase but not Resources.Load in batch.
            foreach (var ext in new[] { ".fbx", ".glb", ".gltf" })
            {
                var path = $"Assets/Resources/PenguinRun/Models/{model}{ext}";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) return prefab;
            }

            return null;
        }

        private static float MeasurePrefabHeight(GameObject prefab)
        {
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0)
                throw new UnityException($"Model has no MeshFilter: {prefab.name}");

            var has = false;
            var bounds = new Bounds();
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null) continue;
                var b = filter.sharedMesh.bounds;
                // Transform mesh bounds into prefab-local space roughly via filter local TRS.
                var worldCenter = filter.transform.TransformPoint(b.center);
                var worldSize = Vector3.Scale(b.size, filter.transform.lossyScale);
                var wb = new Bounds(worldCenter, worldSize);
                if (!has)
                {
                    bounds = wb;
                    has = true;
                }
                else bounds.Encapsulate(wb);
            }

            if (!has)
                throw new UnityException($"Model has no mesh data: {prefab.name}");
            return Mathf.Max(0.01f, bounds.size.y);
        }

        private static IEnumerable<(string model, float x, float z, Vector3 scale)> ThemePlacements(
            RunnerMapTheme theme)
        {
            switch (theme)
            {
                case RunnerMapTheme.IceLakeEcho:
                    yield return (NaturePropCatalog.IceLakeTree(0), -6.2f, 10f, Vector3.one * 1.2f);
                    yield return (NaturePropCatalog.IceLakeTree(1), 6.2f, 16f, Vector3.one * 1.2f);
                    yield return (NaturePropCatalog.IceLakeAccent(0), 5.8f, 20f, Vector3.one * 1.1f);
                    yield return (NaturePropCatalog.SnowPineLandmark(), -5.8f, 22f, Vector3.one * 1.15f);
                    break;
                case RunnerMapTheme.CedarRuins:
                    yield return (NaturePropCatalog.CedarTree(), -6.0f, 10f, Vector3.one * 1.25f);
                    yield return (NaturePropCatalog.CedarAccent(1), 5.6f, 15f, Vector3.one);
                    yield return (NaturePropCatalog.CedarAccent(2), -5.6f, 20f, Vector3.one);
                    yield return ("tent_detailedOpen", 5.8f, 13f, Vector3.one * 1.1f);
                    break;
                case RunnerMapTheme.AuroraField:
                    yield return (NaturePropCatalog.AuroraTree(0), -6.0f, 10f, Vector3.one * 1.05f);
                    yield return (NaturePropCatalog.AuroraTree(1), 6.0f, 16f, Vector3.one * 1.05f);
                    yield return (NaturePropCatalog.AuroraAccent(0), 5.5f, 20f, Vector3.one * 1.4f);
                    break;
                case RunnerMapTheme.MistDike:
                    yield return (NaturePropCatalog.MistAccentA(0), -5.8f, 10f, Vector3.one * 0.9f);
                    yield return (NaturePropCatalog.MistAccentA(1), 5.8f, 15f, Vector3.one * 1.3f);
                    yield return (NaturePropCatalog.MistAccentB(1), -5.8f, 20f, Vector3.one);
                    yield return (NaturePropCatalog.FogStoneLandmark(), 5.8f, 22f, Vector3.one * 1.3f);
                    break;
                case RunnerMapTheme.OceanReef:
                    yield return (NaturePropCatalog.OceanTree(), -6.0f, 10f, Vector3.one * 1.15f);
                    yield return ("canoe", 6.0f, 15f, Vector3.one * 1.15f);
                    yield return ("CubePets/cubepet_fish", 5.4f, 20f, Vector3.one);
                    break;
                case RunnerMapTheme.SkyFlight:
                    // sky_cloud.glb does not always resolve via Resources.Load in batch; use FBX roadside set.
                    yield return ("prop_flag", -6.2f, 10f, Vector3.one * 0.95f);
                    yield return ("crate_strong", 6.2f, 15f, Vector3.one);
                    yield return (NaturePropCatalog.SkyAccent(0), 5.6f, 20f, Vector3.one);
                    yield return (NaturePropCatalog.SkyAccent(1), -5.6f, 22f, Vector3.one);
                    break;
            }
        }

        private static Color ThemeGround(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.CedarRuins => new Color(0.16f, 0.21f, 0.18f),
            RunnerMapTheme.AuroraField => new Color(0.10f, 0.12f, 0.18f),
            RunnerMapTheme.MistDike => new Color(0.17f, 0.21f, 0.22f),
            RunnerMapTheme.OceanReef => new Color(0.08f, 0.25f, 0.28f),
            RunnerMapTheme.SkyFlight => new Color(0.37f, 0.55f, 0.70f),
            _ => new Color(0.20f, 0.33f, 0.43f),
        };

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
            light.shadows = LightShadows.None;
        }
    }
}
#endif
