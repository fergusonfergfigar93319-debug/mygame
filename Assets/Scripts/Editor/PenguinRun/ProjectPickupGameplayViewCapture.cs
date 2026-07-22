#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 以跑酷真实镜头参数（FOV 72、机位后上方）拍摄全部 20 种道具在 10–30m 玩法视距下的读感，
    /// 复刻运行时装饰（悬浮高度 + 贴地光环 + 核心球），LOD 自动选择（不 ForceLOD）。
    /// 用于验收“道具在玩法距离可辨识”。
    /// </summary>
    public static class ProjectPickupGameplayViewCapture
    {
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

        [MenuItem("PenguinRun/Visuals/Capture Pickup Gameplay View")]
        public static void Capture()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "PickupGameplayView.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);

            var sceneRoot = new GameObject("PickupGameplayViewScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            try
            {
                BuildTrack(sceneRoot.transform);
                PlacePickups(sceneRoot.transform);
                BuildLights(sceneRoot.transform);

                // 跑酷镜头（RunnerCameraTuning：behind 10.5 / height 6.5 / aimAhead 19 / FOV 72），玩家脚底 y=0.9
                var cameraObject = new GameObject("GameplayCamera", typeof(Camera));
                cameraObject.transform.SetParent(sceneRoot.transform, false);
                cameraObject.transform.position = new Vector3(0f, 0.9f + 6.5f, -10.5f);
                cameraObject.transform.LookAt(new Vector3(0f, 0.9f + 0.25f, 19f));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 72f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.10f, 0.18f);
                camera.allowHDR = true;

                renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes(output, image.EncodeToPNG());
                Debug.Log($"[ProjectPickupGameplayViewCapture] Wrote {output}");
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) Object.DestroyImmediate(image);
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(sceneRoot);
            }
        }

        private static void BuildTrack(Transform parent)
        {
            var track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.name = "Track";
            track.transform.SetParent(parent, false);
            track.transform.position = new Vector3(0f, 0.45f, 18f);
            track.transform.localScale = new Vector3(9.6f, 0.9f, 70f);
            var trackMat = new Material(Shader.Find("Standard")) { color = new Color(0.42f, 0.78f, 0.92f) };
            track.GetComponent<Renderer>().sharedMaterial = trackMat;
        }

        private static void PlacePickups(Transform parent)
        {
            var ids = (PickupVisualId[])System.Enum.GetValues(typeof(PickupVisualId));
            // 5 列（含道旁展示列）× 4 排（z=10/16/22/28，覆盖玩法读取距离）
            for (var index = 0; index < ids.Length; index++)
            {
                var row = index / 5;
                var column = index % 5;
                var x = (column - 2) * 3f;
                var z = 10f + row * 6f;
                var hover = 0.9f + 0.55f;

                var root = new GameObject($"Pickup {ids[index]}");
                root.transform.SetParent(parent, false);
                root.transform.position = new Vector3(x, hover, z);

                var path = $"Assets/Resources/PenguinRun/Prefabs/Pickups/{RunnerVisualAssetCatalog.PickupSlug(ids[index])}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new FileNotFoundException(path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                instance.transform.localPosition = Vector3.zero;

                // 复刻运行时贴地光环
                var halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                halo.name = "Pickup Ring";
                Object.DestroyImmediate(halo.GetComponent<Collider>());
                halo.transform.SetParent(root.transform, false);
                halo.transform.localPosition = new Vector3(0f, -0.42f, 0f);
                halo.transform.localScale = new Vector3(1.0f, 0.015f, 1.0f);
                var haloMat = new Material(Shader.Find("Standard"));
                haloMat.SetFloat("_Mode", 3f);
                haloMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                haloMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                haloMat.SetInt("_ZWrite", 0);
                haloMat.EnableKeyword("_ALPHABLEND_ON");
                haloMat.renderQueue = 3000;
                haloMat.color = new Color(1f, 0.85f, 0.4f, 0.26f);
                halo.GetComponent<Renderer>().sharedMaterial = haloMat;
            }
        }

        private static void BuildLights(Transform parent)
        {
            var keyLight = new GameObject("KeyLight", typeof(Light));
            keyLight.transform.SetParent(parent, false);
            keyLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = keyLight.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(0.92f, 0.95f, 1f);
        }
    }
}
#endif
