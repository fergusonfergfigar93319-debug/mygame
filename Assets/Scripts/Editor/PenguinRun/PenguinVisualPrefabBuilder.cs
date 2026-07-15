#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class PenguinVisualPrefabBuilder
    {
        private const string MaterialFolder = "Assets/Art/PenguinRun/Materials";

        [MenuItem("PenguinRun/Visuals/Build Batch One Prefabs")]
        public static void BuildBatchOnePrefabs()
        {
            EnsureFolder(MaterialFolder);
            var count = 0;
            foreach (var key in RunnerVisualAssetCatalog.AllBatchOneObstacleKeys())
            {
                var descriptor = RunnerVisualAssetCatalog.ForObstacle(key.theme, key.kind);
                var palette = ThemePalette(key.theme);
                BuildPrefab(descriptor.ResourcePath, (root, low) =>
                    RunnerSculptureBuilder.BuildObstacle(root, key.theme, key.kind, palette, low));
                count++;
            }

            var pickupPalette = PickupPalette();
            foreach (var id in RunnerVisualAssetCatalog.AllBatchOnePickupKeys())
            {
                var descriptor = RunnerVisualAssetCatalog.ForPickup(id);
                BuildPrefab(descriptor.ResourcePath, (root, low) =>
                    RunnerSculptureBuilder.BuildPickup(root, id, pickupPalette, low));
                count++;
            }

            foreach (var key in RunnerVisualAssetCatalog.AllBatchOneEnvironmentKeys())
            {
                var descriptor = RunnerVisualAssetCatalog.ForEnvironment(key.theme, key.id);
                var palette = ThemePalette(key.theme);
                BuildPrefab(descriptor.ResourcePath, (root, low) =>
                    RunnerSculptureBuilder.BuildEnvironment(root, key.theme, key.id, palette, low));
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PenguinVisualPrefabBuilder] Built {count} detailed prefabs with LOD0/LOD1.");
        }

        private static void BuildPrefab(string resourcePath, Action<Transform, bool> build)
        {
            var assetPath = $"Assets/Resources/{resourcePath}.prefab";
            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));

            var root = new GameObject(Path.GetFileNameWithoutExtension(assetPath));
            try
            {
                var lod0 = new GameObject("LOD0");
                lod0.transform.SetParent(root.transform, false);
                build(lod0.transform, false);

                var lod1 = new GameObject("LOD1");
                lod1.transform.SetParent(root.transform, false);
                build(lod1.transform, true);

                var lodGroup = root.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[]
                {
                    new LOD(0.60f, lod0.GetComponentsInChildren<Renderer>(true)),
                    new LOD(0.22f, lod1.GetComponentsInChildren<Renderer>(true)),
                    new LOD(0.03f, Array.Empty<Renderer>()),
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

        internal static SculpturePalette ThemePalette(RunnerMapTheme theme)
        {
            var slug = RunnerVisualAssetCatalog.ThemeSlug(theme);
            var colors = theme switch
            {
                RunnerMapTheme.IceLakeEcho => (new Color(.40f,.73f,.88f), new Color(.70f,.91f,.96f), new Color(.25f,.53f,.72f), new Color(.16f,.27f,.34f), new Color(.65f,.96f,1f)),
                RunnerMapTheme.CedarRuins => (new Color(.43f,.25f,.14f), new Color(.64f,.39f,.20f), new Color(.72f,.51f,.29f), new Color(.19f,.14f,.12f), new Color(.96f,.69f,.28f)),
                RunnerMapTheme.AuroraField => (new Color(.20f,.24f,.30f), new Color(.30f,.39f,.47f), new Color(.72f,.36f,.16f), new Color(.10f,.12f,.17f), new Color(.35f,1f,.73f)),
                RunnerMapTheme.MistDike => (new Color(.34f,.40f,.39f), new Color(.48f,.55f,.51f), new Color(.57f,.28f,.16f), new Color(.16f,.20f,.20f), new Color(.96f,.82f,.43f)),
                RunnerMapTheme.OceanReef => (new Color(.17f,.64f,.65f), new Color(.29f,.81f,.74f), new Color(.94f,.32f,.42f), new Color(.12f,.28f,.32f), new Color(.36f,1f,.86f)),
                _ => (new Color(.61f,.45f,.28f), new Color(.82f,.67f,.45f), new Color(.31f,.65f,.85f), new Color(.21f,.20f,.24f), new Color(.58f,.88f,1f)),
            };
            return new SculpturePalette(
                Material($"M_{slug}_Primary", colors.Item1),
                Material($"M_{slug}_Secondary", colors.Item2),
                Material($"M_{slug}_Accent", colors.Item3, metallic: theme is RunnerMapTheme.AuroraField or RunnerMapTheme.MistDike),
                Material($"M_{slug}_Dark", colors.Item4),
                Material("M_Shared_Snow", new Color(.91f,.95f,.97f)),
                Material($"M_{slug}_Glow", colors.Item5, emission: true));
        }

        private static SculpturePalette PickupPalette() => new(
            Material("M_Pickup_Blue", new Color(.25f,.58f,.82f)),
            Material("M_Pickup_Pine", new Color(.17f,.48f,.41f)),
            Material("M_Pickup_Gold", new Color(.96f,.63f,.19f), metallic: true),
            Material("M_Pickup_Leather", new Color(.22f,.14f,.10f)),
            Material("M_Shared_Snow", new Color(.91f,.95f,.97f)),
            Material("M_Pickup_Glow", new Color(1f,.72f,.28f), emission: true));

        internal static Material Material(string name, Color color, bool metallic = false, bool emission = false)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Metallic", metallic ? .72f : .06f);
            material.SetFloat("_Glossiness", metallic ? .52f : .28f);
            material.enableInstancing = true;
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.35f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static void EnsureFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(parent)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
#endif
