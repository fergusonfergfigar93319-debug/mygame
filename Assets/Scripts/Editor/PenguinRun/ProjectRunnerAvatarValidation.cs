#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    internal static class ProjectRunnerAvatarValidation
    {
        private const string DefinitionResourcePath = "PenguinRun/Characters/Gugugaga/GugugagaAvatar";
        private static readonly int[] TriangleBudgets = { 50000, 22000, 8000 };

        [MenuItem("PenguinRun/Avatar/Validate Gugugaga Avatar")]
        public static void ValidateMenu()
        {
            ThrowIfInvalid();
            Debug.Log("[ProjectRunnerAvatarValidation] PASS");
        }

        public static void ThrowIfInvalid()
        {
            var definition = Resources.Load<RunnerAvatarDefinition>(DefinitionResourcePath);
            var errors = ValidateDefinition(definition);
            ValidateProceduralFallback(errors);
            if (errors.Count > 0)
            {
                throw new System.InvalidOperationException(string.Join("\n", errors));
            }
        }

        public static List<string> ValidateDefinition(RunnerAvatarDefinition definition)
        {
            var errors = new List<string>();
            if (definition == null)
            {
                errors.Add("[unknown] Avatar definition is missing.");
                return errors;
            }

            var avatarId = string.IsNullOrWhiteSpace(definition.AvatarId) ? "unknown" : definition.AvatarId;
            if (avatarId == "unknown") Fail(errors, avatarId, "Avatar ID is empty.");
            if (definition.ModelPrefab == null) Fail(errors, avatarId, "Model prefab is missing.");
            if (definition.BaseController == null) Fail(errors, avatarId, "Base AnimatorController is missing.");
            if (definition.Animations == null) Fail(errors, avatarId, "Animation set is missing.");

            if (definition.Animations != null)
            {
                foreach (var pair in definition.Animations.RequiredClips)
                {
                    if (pair.Value == null) Fail(errors, avatarId, $"Required clip '{pair.Key}' is missing.");
                }
            }

            var prefab = definition.ModelPrefab;
            if (prefab == null) return errors;

            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null) Fail(errors, avatarId, "Prefab has no Animator.");
            else if (animator.applyRootMotion) Fail(errors, avatarId, "Animator root motion must be disabled.");

            ValidateSockets(errors, avatarId, prefab.transform, definition.Sockets);
            ValidateLods(errors, avatarId, prefab);
            ValidateMaterials(errors, avatarId, prefab);
            return errors;
        }

        public static void ValidateFromCommandLine()
        {
            try
            {
                ThrowIfInvalid();
                Debug.Log("[ProjectRunnerAvatarValidation] PASS");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ProjectRunnerAvatarValidation] FAIL:\n" + ex.Message);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateSockets(List<string> errors, string avatarId, Transform root, RunnerAccessorySocketSet set)
        {
            if (set == null)
            {
                Fail(errors, avatarId, "Accessory socket set is missing.");
                return;
            }

            var requiredIds = new HashSet<string> { "head", "face", "neck", "back" };
            foreach (var socket in set.Sockets)
            {
                if (socket == null || string.IsNullOrWhiteSpace(socket.Id)) continue;
                requiredIds.Remove(socket.Id);
                if (string.IsNullOrWhiteSpace(socket.RelativePath) || root.Find(socket.RelativePath) == null)
                    Fail(errors, avatarId, $"Socket '{socket.Id}' cannot resolve '{socket.RelativePath}'.");
            }

            foreach (var missingId in requiredIds) Fail(errors, avatarId, $"Required socket '{missingId}' is missing.");
        }

        private static void ValidateLods(List<string> errors, string avatarId, GameObject prefab)
        {
            var group = prefab.GetComponentInChildren<LODGroup>(true);
            if (group == null)
            {
                Fail(errors, avatarId, "LODGroup is missing.");
                return;
            }

            var lods = group.GetLODs();
            if (lods.Length < TriangleBudgets.Length)
            {
                Fail(errors, avatarId, $"Expected at least {TriangleBudgets.Length} LOD levels, found {lods.Length}.");
                return;
            }

            for (var i = 0; i < TriangleBudgets.Length; i++)
            {
                var triangles = CountTriangles(lods[i].renderers);
                if (triangles > TriangleBudgets[i])
                    Fail(errors, avatarId, $"LOD{i} has {triangles} triangles; budget is {TriangleBudgets[i]}.");
            }
        }

        private static int CountTriangles(Renderer[] renderers)
        {
            var triangles = 0;
            foreach (var renderer in renderers)
            {
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else if (renderer != null && renderer.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
                if (mesh != null) triangles += (int)(mesh.GetIndexCount(0) / 3);
            }
            return triangles;
        }

        private static void ValidateMaterials(List<string> errors, string avatarId, GameObject prefab)
        {
            var materials = new HashSet<Material>();
            var textures = new HashSet<Texture2D>();
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || !materials.Add(material)) continue;
                    foreach (var propertyName in material.GetTexturePropertyNames())
                    {
                        if (material.GetTexture(propertyName) is Texture2D texture) textures.Add(texture);
                    }
                }
            }

            if (materials.Count > 3) Fail(errors, avatarId, $"Prefab uses {materials.Count} materials; budget is 3.");
            foreach (var texture in textures)
            {
                if (texture.width > 2048 || texture.height > 2048)
                    Fail(errors, avatarId, $"Texture '{texture.name}' is {texture.width}x{texture.height}; maximum is 2048.");
            }
        }

        private static void ValidateProceduralFallback(List<string> errors)
        {
            var parent = new GameObject("Avatar Fallback Validation");
            try
            {
                var fallback = PlayerAvatarPresenter.Create(parent.transform, null);
                if (fallback?.Root == null || fallback.Root.name != "Cute Penguin")
                    Fail(errors, "fallback", "Null definition did not create the procedural Cute Penguin.");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static void Fail(List<string> errors, string avatarId, string message) =>
            errors.Add($"[{avatarId}] {message}");
    }
}
#endif
