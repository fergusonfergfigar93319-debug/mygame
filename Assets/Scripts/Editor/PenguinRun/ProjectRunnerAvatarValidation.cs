#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
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

        [MenuItem("PenguinRun/Avatar/Diagnose Animation Playback")]
        public static void DiagnoseAnimationPlayback()
        {
            var report = new StringBuilder();
            report.AppendLine("[AvatarAnimDiagnose] BEGIN");
            var definition = Resources.Load<RunnerAvatarDefinition>(DefinitionResourcePath);
            if (definition == null || definition.Animations == null)
            {
                Debug.LogError("[AvatarAnimDiagnose] definition or animations missing");
                return;
            }

            foreach (var pair in definition.Animations.RequiredClips)
            {
                var clip = pair.Value;
                if (clip == null)
                {
                    report.AppendLine($"CLIP {pair.Key}: NULL");
                    continue;
                }

                var bindings = AnimationUtility.GetCurveBindings(clip);
                report.AppendLine(
                    $"CLIP {pair.Key}: name='{clip.name}' length={clip.length:F3} empty={clip.empty} curves={bindings.Length}");
                for (var i = 0; i < Mathf.Min(3, bindings.Length); i++)
                {
                    var b = bindings[i];
                    report.AppendLine($"  path='{b.path}' prop='{b.propertyName}' type={b.type.Name}");
                }
            }

            var parent = new GameObject("AvatarAnimDiagnoseRoot");
            try
            {
                var character = PlayerAvatarPresenter.Create(parent.transform, definition);
                var animators = parent.GetComponentsInChildren<Animator>(true);
                report.AppendLine($"PRESENTERS: animators={animators.Length} root='{character.Root.name}'");
                foreach (var animator in animators)
                {
                    report.AppendLine(
                        $"ANIMATOR ctrl={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")} " +
                        $"enabled={animator.enabled} isInitialized={animator.isInitialized}");
                    if (animator.runtimeAnimatorController != null)
                    {
                        foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        {
                            if (clip == null) continue;
                            report.AppendLine($"  runtimeClip='{clip.name}' length={clip.length:F3} empty={clip.empty}");
                        }
                    }
                }

                Transform FindBone(string name)
                {
                    foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                        if (t.name == name) return t;
                    return null;
                }

                var pelvis = FindBone("Pelvis");
                var chest = FindBone("Chest");
                report.AppendLine($"BONES pelvis={(pelvis != null)} chest={(chest != null)}");
                if (pelvis != null) report.AppendLine($"  Pelvis path={GetPath(pelvis, character.Root)}");

                foreach (var skin in parent.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var boneCount = skin.bones != null ? skin.bones.Length : 0;
                    var nullBones = 0;
                    if (skin.bones != null)
                        foreach (var bone in skin.bones) if (bone == null) nullBones++;
                    var bindCount = skin.sharedMesh != null ? skin.sharedMesh.bindposes.Length : -1;
                    report.AppendLine(
                        $"SKIN '{skin.name}' bones={boneCount} nullBones={nullBones} bindposes={bindCount} " +
                        $"rootBone={(skin.rootBone != null ? skin.rootBone.name : "null")} quality={skin.quality}");
                }

                var slideClip = definition.Animations.Overrides["Slide"];
                if (slideClip != null)
                {
                    var pelvisBindings = 0;
                    foreach (var binding in AnimationUtility.GetCurveBindings(slideClip))
                    {
                        if (binding.path.IndexOf("Pelvis", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            pelvisBindings++;
                    }
                    report.AppendLine($"SLIDE_CLIP pelvisBindings={pelvisBindings} length={slideClip.length:F3}");
                }

                var slideState = new RunnerAvatarState(true, true, true, false, 0.72f, 0f, 0f, RunnerAvatarBuffFlags.None);
                foreach (var animator in animators)
                {
                    animator.Play("Slide", 0, 0.55f);
                    animator.Update(0f);
                }
                character.Tick(0f, slideState);

                var beforePelvis = pelvis != null ? pelvis.localRotation : Quaternion.identity;
                var beforePos = pelvis != null ? pelvis.localPosition : Vector3.zero;
                Mesh bakeBefore = null;
                Mesh bakeAfter = null;
                var primarySkin = parent.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (primarySkin != null)
                {
                    bakeBefore = new Mesh();
                    primarySkin.BakeMesh(bakeBefore);
                }

                for (var i = 0; i < 30; i++)
                {
                    character.Tick(1f / 60f, slideState);
                    foreach (var animator in animators) animator.Update(1f / 60f);
                }

                var afterPelvis = pelvis != null ? pelvis.localRotation : Quaternion.identity;
                var afterPos = pelvis != null ? pelvis.localPosition : Vector3.zero;
                var rotDelta = Quaternion.Angle(beforePelvis, afterPelvis);
                var posDelta = Vector3.Distance(beforePos, afterPos);
                report.AppendLine($"SLIDE_DELTA rotDeg={rotDelta:F3} pos={posDelta:F4} beforePos={beforePos} afterPos={afterPos}");

                if (primarySkin != null)
                {
                    bakeAfter = new Mesh();
                    primarySkin.BakeMesh(bakeAfter);
                    var vertsBefore = bakeBefore.vertices;
                    var vertsAfter = bakeAfter.vertices;
                    var count = Mathf.Min(vertsBefore.Length, vertsAfter.Length);
                    double accum = 0;
                    var maxDelta = 0f;
                    for (var i = 0; i < count; i++)
                    {
                        var d = Vector3.Distance(vertsBefore[i], vertsAfter[i]);
                        accum += d;
                        if (d > maxDelta) maxDelta = d;
                    }
                    report.AppendLine($"MESH_DELTA mean={accum / Mathf.Max(1, count):F5} max={maxDelta:F5} verts={count}");
                    Object.DestroyImmediate(bakeBefore);
                    Object.DestroyImmediate(bakeAfter);
                }

                if (animators.Length > 0)
                {
                    var info = animators[0].GetCurrentAnimatorStateInfo(0);
                    report.AppendLine(
                        $"STATE layer0 hash={info.shortNameHash} normalizedTime={info.normalizedTime:F3} length={info.length:F3} isNameSlide={info.IsName("Slide")}");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }

            report.AppendLine("[AvatarAnimDiagnose] END");
            Debug.Log(report.ToString());
        }

        public static void DiagnoseAnimationPlaybackFromCommandLine()
        {
            DiagnoseAnimationPlayback();
            EditorApplication.Exit(0);
        }

        private static string GetPath(Transform t, Transform root)
        {
            var parts = new List<string>();
            var cur = t;
            while (cur != null && cur != root)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
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
