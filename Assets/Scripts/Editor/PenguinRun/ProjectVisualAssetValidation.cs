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
            foreach (var key in RunnerVisualAssetCatalog.AllBatchOneObstacleKeys())
                Validate(RunnerVisualAssetCatalog.ForObstacle(key.theme, key.kind), failures);
            foreach (var id in RunnerVisualAssetCatalog.AllBatchOnePickupKeys())
                Validate(RunnerVisualAssetCatalog.ForPickup(id), failures);
            foreach (var key in RunnerVisualAssetCatalog.AllBatchOneEnvironmentKeys())
                Validate(RunnerVisualAssetCatalog.ForEnvironment(key.theme, key.id), failures);

            report = failures.Count == 0
                ? "PASS: custom map asset batch one is valid."
                : "FAIL:\n" + string.Join("\n", failures);
            return failures.Count == 0;
        }

        public static void BuildAndValidateFromCommandLine()
        {
            PenguinVisualPrefabBuilder.BuildBatchOnePrefabs();
            var passed = RunNow(out var report);
            if (passed) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Validate(RunnerVisualDescriptor descriptor, List<string> failures)
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

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
#endif
