#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ProjectVisualAssetValidation
    {
        [MenuItem("PenguinRun/Visuals/Validate Batch One")]
        public static void ValidateFromMenu()
        {
            var passed = RunNow(out var report);
            if (passed) Debug.Log(report);
            else Debug.LogError(report);
        }

        public static bool RunNow(out string report)
        {
            var failures = new List<string>();
            var stats = new VisualStats();
            foreach (var key in RunnerVisualAssetCatalog.AllBatchOneObstacleKeys())
                Validate(RunnerVisualAssetCatalog.ForObstacle(key.theme, key.kind), failures, stats);
            foreach (var id in RunnerVisualAssetCatalog.AllBatchOnePickupKeys())
                Validate(RunnerVisualAssetCatalog.ForPickup(id), failures, stats);
            foreach (var key in RunnerVisualAssetCatalog.AllBatchOneEnvironmentKeys())
                Validate(RunnerVisualAssetCatalog.ForEnvironment(key.theme, key.id), failures, stats);
            ValidateMissingFallback(failures);
            ValidatePoolReset(failures);

            report = failures.Count == 0
                ? $"PASS: custom map asset batch one is valid. " +
                  $"{stats.Prefabs} prefabs; LOD0 {stats.Lod0Parts} parts/{stats.Lod0Triangles} triangles; " +
                  $"LOD1 {stats.Lod1Parts} parts/{stats.Lod1Triangles} triangles."
                : "FAIL:\n" + string.Join("\n", failures);
            return failures.Count == 0;
        }

        public static void BuildAndValidateFromCommandLine()
        {
            PenguinVisualPrefabBuilder.BuildBatchOnePrefabs();
            ValidateFromCommandLine();
        }

        public static void ValidateFromCommandLine()
        {
            var passed = RunNow(out var report);
            if (passed) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Validate(RunnerVisualDescriptor descriptor, List<string> failures, VisualStats stats)
        {
            var path = $"Assets/Resources/{descriptor.ResourcePath}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                failures.Add($"Missing prefab: {descriptor.ResourcePath}");
                return;
            }

            var groups = prefab.GetComponentsInChildren<LODGroup>(true);
            if (groups.Length != 1)
            {
                failures.Add($"{descriptor.ResourcePath} has {groups.Length} LODGroups; expected 1.");
                return;
            }
            var lods = groups[0].GetLODs();
            if (lods.Length < 2 || lods[0].renderers.Length == 0 || lods[1].renderers.Length == 0)
                failures.Add($"{descriptor.ResourcePath} is missing rendered LOD0 or LOD1.");
            else
            {
                stats.Prefabs++;
                stats.Lod0Parts += lods[0].renderers.Length;
                stats.Lod1Parts += lods[1].renderers.Length;
                stats.Lod0Triangles += TriangleCount(lods[0].renderers);
                stats.Lod1Triangles += TriangleCount(lods[1].renderers);
            }

            if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
                failures.Add($"{descriptor.ResourcePath} contains a visual collider.");

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                        failures.Add($"{descriptor.ResourcePath}/{renderer.name} uses a missing or error material.");
                }
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                failures.Add($"{descriptor.ResourcePath} could not be instantiated.");
                return;
            }
            try
            {
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    failures.Add($"{descriptor.ResourcePath} has no renderers.");
                    return;
                }
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                if (!Finite(bounds.min) || !Finite(bounds.max) || bounds.size.sqrMagnitude < .0001f)
                    failures.Add($"{descriptor.ResourcePath} has invalid or zero bounds.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ValidateMissingFallback(List<string> failures)
        {
            var root = new GameObject("MissingVisualRegression");
            var factory = new RunnerVisualFactory();
            var path = "PenguinRun/Prefabs/__validation__/definitely_missing";
            var warnings = 0;
            Application.LogCallback callback = (condition, _, type) =>
            {
                if (type == LogType.Warning && condition.Contains(path)) warnings++;
            };
            Application.logMessageReceived += callback;
            try
            {
                var descriptor = new RunnerVisualDescriptor(path, Vector3.zero, Vector3.zero, Vector3.one);
                var first = factory.TryAttachForValidation(root.transform, descriptor);
                var second = factory.TryAttachForValidation(root.transform, descriptor);
                if (first || second)
                    failures.Add("Missing custom visual unexpectedly resolved.");
                if (DirectAttachmentCount(root.transform) != 0)
                    failures.Add("Missing custom visual left a CustomVisual child.");
                if (warnings != 1)
                    failures.Add($"Missing custom visual emitted {warnings} warnings; expected exactly 1.");
            }
            finally
            {
                Application.logMessageReceived -= callback;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidatePoolReset(List<string> failures)
        {
            var root = new GameObject("PickupPoolRegression");
            var factory = new RunnerVisualFactory();
            try
            {
                if (!factory.TryAttachPickup(root.transform, PickupVisualId.Dash))
                {
                    failures.Add("Dash custom pickup could not attach in pool regression.");
                    return;
                }
                factory.ClearAttachment(root.transform);
                if (!factory.TryAttachPickup(root.transform, PickupVisualId.Shield))
                {
                    failures.Add("Shield custom pickup could not attach after pool reset.");
                    return;
                }
                if (DirectAttachmentCount(root.transform) != 1)
                    failures.Add("Pooled pickup reset did not leave exactly one CustomVisual child.");
                var childNames = root.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
                if (!childNames.Contains("ShieldBoss") || childNames.Contains("SledHull"))
                    failures.Add("Pooled pickup reset retained Dash parts or lost Shield identity.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static int DirectAttachmentCount(Transform root)
        {
            var count = 0;
            for (var index = 0; index < root.childCount; index++)
                if (root.GetChild(index).name == RunnerVisualFactory.AttachmentName) count++;
            return count;
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static int TriangleCount(IEnumerable<Renderer> renderers)
        {
            var triangles = 0;
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null) triangles += (int)mesh.GetIndexCount(0) / 3;
            }
            return triangles;
        }

        private sealed class VisualStats
        {
            public int Prefabs;
            public int Lod0Parts;
            public int Lod1Parts;
            public int Lod0Triangles;
            public int Lod1Triangles;
        }
    }
}
#endif
