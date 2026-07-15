#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ThemeMechanicPrefabBuilder
    {
        public const string ResourceRoot = "PenguinRun/Prefabs/ThemeMechanics";

        [MenuItem("PenguinRun/Theme Mechanics/Build 18 Apparatus Prefabs")]
        public static void BuildBatch()
        {
            var built = 0;
            foreach (var definition in ThemeMechanicCatalog.Definitions)
            {
                var palette = PenguinVisualPrefabBuilder.ThemePalette(definition.Theme);
                BuildPrefab(definition.ApparatusA, palette);
                BuildPrefab(definition.ApparatusB, palette);
                BuildPrefab(definition.Signature, palette);
                built += 3;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ThemeMechanicPrefabBuilder] Built {built} detailed dual-LOD apparatus prefabs.");
        }

        public static void BuildBatchFromCommandLine()
        {
            try
            {
                BuildBatch();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static string AssetPath(string apparatusPath) =>
            $"Assets/Resources/{ResourceRoot}/{apparatusPath}.prefab";

        private static void BuildPrefab(string apparatusPath, SculpturePalette palette)
        {
            var assetPath = AssetPath(apparatusPath);
            PenguinVisualPrefabBuilder.EnsureFolder(
                Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));

            var root = new GameObject(Path.GetFileNameWithoutExtension(assetPath));
            try
            {
                var lod0 = new GameObject("LOD0");
                lod0.transform.SetParent(root.transform, false);
                ThemeMechanicSculptureBuilder.Build(lod0.transform, apparatusPath, palette, false);

                var lod1 = new GameObject("LOD1");
                lod1.transform.SetParent(root.transform, false);
                ThemeMechanicSculptureBuilder.Build(lod1.transform, apparatusPath, palette, true);

                var lodGroup = root.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[]
                {
                    new LOD(.60f, lod0.GetComponentsInChildren<Renderer>(true)),
                    new LOD(.22f, lod1.GetComponentsInChildren<Renderer>(true)),
                    new LOD(.03f, Array.Empty<Renderer>()),
                });
                lodGroup.fadeMode = LODFadeMode.CrossFade;
                lodGroup.animateCrossFading = true;
                lodGroup.RecalculateBounds();

                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
