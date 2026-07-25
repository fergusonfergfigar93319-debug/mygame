#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PenguinRun.Editor
{
    internal static class RunnerAvatarAssetBuilder
    {
        private const string Root = "Assets/Resources/PenguinRun/Characters/Gugugaga";
        private const string ModelPath = Root + "/gugugaga_avatar.fbx";
        private const string Lod1Path = Root + "/gugugaga_lod1.fbx";
        private const string Lod2Path = Root + "/gugugaga_lod2.fbx";
        private const string ControllerPath = Root + "/GugugagaBase.controller";
        private const string MaterialPath = Root + "/M_Gugugaga_Stylized.mat";
        private const string AnimationSetPath = Root + "/GugugagaAnimations.asset";
        private const string AppearancePath = Root + "/GugugagaAppearance.asset";
        private const string SocketsPath = Root + "/GugugagaSockets.asset";
        private const string PrefabPath = Root + "/GugugagaAvatar.prefab";
        private const string DefinitionPath = Root + "/GugugagaAvatar.asset";

        private static readonly string[] RequiredClipNames =
        {
            "Idle", "Run", "LaneLeft", "LaneRight", "Jump", "Fall", "Land", "Slide",
            "Hurt", "Defeat", "Celebrate", "FishPickup", "ShieldActivate", "DashActivate",
            "MagnetActivate", "BossCounter",
        };

        [MenuItem("PenguinRun/Avatar/Build Gugugaga Avatar")]
        public static void Build()
        {
            var clips = LoadClips();
            var material = BuildMaterial();
            var controller = BuildController(clips);
            var animationSet = BuildAnimationSet(clips);
            var appearance = RecreateAsset<RunnerAppearanceSet>(AppearancePath);
            var prefab = BuildPrefab(controller, material);
            var sockets = BuildSocketSet(prefab);
            BuildDefinition(prefab, controller, animationSet, appearance, sockets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ProjectRunnerAvatarValidation.ThrowIfInvalid();
            Debug.Log("[RunnerAvatarAssetBuilder] Gugugaga avatar built and validated.");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static Dictionary<string, AnimationClip> LoadClips()
        {
            var imported = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            foreach (var required in RequiredClipNames)
            {
                var clip = imported.FirstOrDefault(candidate =>
                    candidate.name.Equals(required, StringComparison.OrdinalIgnoreCase) ||
                    candidate.name.EndsWith("|" + required, StringComparison.OrdinalIgnoreCase));
                if (clip == null) throw new InvalidOperationException($"Required clip '{required}' was not imported from {ModelPath}.");
                clips[required] = clip;
            }
            return clips;
        }

        private static Material BuildMaterial()
        {
            DeleteAssetIfPresent(MaterialPath);
            var shader = Shader.Find("PenguinRun/Gugugaga Stylized");
            if (shader == null) throw new InvalidOperationException("Gugugaga stylized shader was not imported.");
            var material = new Material(shader) { name = "M_Gugugaga_Stylized" };
            material.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/T_gugugaga_normal.png"));
            material.SetTexture("_MaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/T_gugugaga_mask.png"));
            material.SetColor("_RimColor", new Color(0.18f, 0.65f, 0.9f, 1f));
            material.SetFloat("_RimPower", 3.5f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static AnimatorController BuildController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            DeleteAssetIfPresent(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AddParameter(controller, "Running", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Sliding", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "GameOver", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Speed01", AnimatorControllerParameterType.Float);
            AddParameter(controller, "LaneLean", AnimatorControllerParameterType.Float);
            AddParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "Hurt", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Celebrate", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "PerfectDodge", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Interaction", AnimatorControllerParameterType.Int);
            AddParameter(controller, "PlayInteraction", AnimatorControllerParameterType.Trigger);

            BuildBaseLayer(controller, clips);
            controller.AddLayer("Actions");
            controller.AddLayer("Expression");
            BuildActionLayer(controller, clips);
            controller.layers[1].defaultWeight = 1f;
            controller.layers[2].defaultWeight = 1f;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void BuildBaseLayer(AnimatorController controller, IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var machine = controller.layers[0].stateMachine;
            machine.name = "Base";
            var idle = machine.AddState("Idle"); idle.motion = clips["Idle"];
            var run = machine.AddState("Run");
            var locomotion = new BlendTree
            {
                name = "Run Lane Blend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "LaneLean",
                useAutomaticThresholds = false,
            };
            locomotion.AddChild(clips["LaneLeft"], -1f);
            locomotion.AddChild(clips["Run"], 0f);
            locomotion.AddChild(clips["LaneRight"], 1f);
            AssetDatabase.AddObjectToAsset(locomotion, controller);
            run.motion = locomotion;
            var jump = machine.AddState("Jump"); jump.motion = clips["Jump"];
            var fall = machine.AddState("Fall"); fall.motion = clips["Fall"];
            var land = machine.AddState("Land"); land.motion = clips["Land"];
            var slide = machine.AddState("Slide"); slide.motion = clips["Slide"];
            var defeat = machine.AddState("Defeat"); defeat.motion = clips["Defeat"];
            machine.defaultState = idle;

            AddTransition(idle, run, false, 0.08f, (AnimatorConditionMode.If, 0f, "Running"));
            AddTransition(run, idle, false, 0.08f, (AnimatorConditionMode.IfNot, 0f, "Running"));
            AddAnyTransition(machine, jump, (AnimatorConditionMode.IfNot, 0f, "Grounded"), (AnimatorConditionMode.Greater, 0.05f, "VerticalSpeed"));
            AddAnyTransition(machine, fall, (AnimatorConditionMode.IfNot, 0f, "Grounded"), (AnimatorConditionMode.Less, 0.05f, "VerticalSpeed"));
            AddTransition(jump, land, false, 0.06f, (AnimatorConditionMode.If, 0f, "Grounded"));
            AddTransition(fall, land, false, 0.06f, (AnimatorConditionMode.If, 0f, "Grounded"));
            AddTransition(land, run, true, 0.08f, (AnimatorConditionMode.If, 0f, "Running"));
            AddTransition(land, idle, true, 0.08f, (AnimatorConditionMode.IfNot, 0f, "Running"));
            AddAnyTransition(machine, slide, (AnimatorConditionMode.If, 0f, "Sliding"));
            AddTransition(slide, run, false, 0.08f, (AnimatorConditionMode.IfNot, 0f, "Sliding"), (AnimatorConditionMode.If, 0f, "Running"));
            AddTransition(slide, idle, false, 0.08f, (AnimatorConditionMode.IfNot, 0f, "Sliding"), (AnimatorConditionMode.IfNot, 0f, "Running"));
            AddAnyTransition(machine, defeat, (AnimatorConditionMode.If, 0f, "GameOver"));
        }

        private static void BuildActionLayer(AnimatorController controller, IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var machine = controller.layers[1].stateMachine;
            var empty = machine.AddState("Empty");
            machine.defaultState = empty;
            var hurt = machine.AddState("Hurt"); hurt.motion = clips["Hurt"];
            var celebrate = machine.AddState("Celebrate"); celebrate.motion = clips["Celebrate"];
            AddAnyTransition(machine, hurt, (AnimatorConditionMode.If, 0f, "Hurt"));
            AddAnyTransition(machine, celebrate, (AnimatorConditionMode.If, 0f, "Celebrate"));
            AddExit(hurt, empty); AddExit(celebrate, empty);

            var interactionClips = new[] { "FishPickup", "ShieldActivate", "DashActivate", "MagnetActivate", "BossCounter" };
            for (var i = 0; i < interactionClips.Length; i++)
            {
                var state = machine.AddState(interactionClips[i]);
                state.motion = clips[interactionClips[i]];
                AddAnyTransition(machine, state,
                    (AnimatorConditionMode.If, 0f, "PlayInteraction"),
                    (AnimatorConditionMode.Equals, i, "Interaction"));
                AddExit(state, empty);
            }
        }

        private static GameObject BuildPrefab(RuntimeAnimatorController controller, Material material)
        {
            var root = new GameObject("GugugagaAvatar");
            try
            {
                var lodObjects = new[]
                {
                    InstantiateModel(ModelPath, "LOD0", root.transform, controller, material),
                    InstantiateModel(Lod1Path, "LOD1", root.transform, controller, material),
                    InstantiateModel(Lod2Path, "LOD2", root.transform, controller, material),
                };
                root.AddComponent<RunnerExpressionDriver>();
                root.AddComponent<RunnerSecondaryMotion>();
                var group = root.AddComponent<LODGroup>();
                group.SetLODs(new[]
                {
                    new LOD(0.18f, lodObjects[0].GetComponentsInChildren<Renderer>(true)),
                    new LOD(0.055f, lodObjects[1].GetComponentsInChildren<Renderer>(true)),
                    new LOD(0.012f, lodObjects[2].GetComponentsInChildren<Renderer>(true)),
                });
                group.RecalculateBounds();
                DeleteAssetIfPresent(PrefabPath);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject InstantiateModel(string path, string name, Transform parent, RuntimeAnimatorController controller, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new InvalidOperationException($"Model asset is missing: {path}");
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException($"Could not instantiate model: {path}");
            instance.name = name;
            instance.transform.SetParent(parent, false);
            var animator = instance.GetComponent<Animator>();
            // Unity's imported-model components can compare equal to null while still
            // producing a non-null CLR reference, so null-coalescing is unsafe here.
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[Math.Max(1, renderer.sharedMaterials.Length)];
                for (var i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
            return instance;
        }

        private static RunnerAnimationSet BuildAnimationSet(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var set = RecreateAsset<RunnerAnimationSet>(AnimationSetPath);
            var serialized = new SerializedObject(set);
            foreach (var pair in clips)
            {
                var fieldName = char.ToLowerInvariant(pair.Key[0]) + pair.Key.Substring(1);
                serialized.FindProperty(fieldName).objectReferenceValue = pair.Value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private static RunnerAccessorySocketSet BuildSocketSet(GameObject prefab)
        {
            var set = RecreateAsset<RunnerAccessorySocketSet>(SocketsPath);
            var serialized = new SerializedObject(set);
            var sockets = serialized.FindProperty("sockets");
            var ids = new[] { "head", "face", "neck", "back" };
            var boneNames = new[] { "Socket_Head", "Socket_Face", "Socket_Neck", "Socket_Back" };
            sockets.arraySize = ids.Length;
            for (var i = 0; i < ids.Length; i++)
            {
                var bone = FindDeep(prefab.transform.Find("LOD0"), boneNames[i]);
                if (bone == null) throw new InvalidOperationException($"Socket bone '{boneNames[i]}' is missing.");
                var entry = sockets.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = ids[i];
                entry.FindPropertyRelative("relativePath").stringValue = AnimationUtility.CalculateTransformPath(bone, prefab.transform);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private static void BuildDefinition(GameObject prefab, RuntimeAnimatorController controller,
            RunnerAnimationSet animations, RunnerAppearanceSet appearance, RunnerAccessorySocketSet sockets)
        {
            var definition = RecreateAsset<RunnerAvatarDefinition>(DefinitionPath);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("avatarId").stringValue = "gugugaga";
            serialized.FindProperty("modelPrefab").objectReferenceValue = prefab;
            serialized.FindProperty("baseController").objectReferenceValue = controller;
            serialized.FindProperty("animations").objectReferenceValue = animations;
            serialized.FindProperty("appearance").objectReferenceValue = appearance;
            serialized.FindProperty("sockets").objectReferenceValue = sockets;
            serialized.FindProperty("localPosition").vector3Value = Vector3.zero;
            serialized.FindProperty("localEulerAngles").vector3Value = new Vector3(0f, 180f, 0f);
            serialized.FindProperty("localScale").vector3Value = Vector3.one;
            serialized.FindProperty("fallbackToProcedural").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T RecreateAsset<T>(string path) where T : ScriptableObject
        {
            DeleteAssetIfPresent(path);
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type) =>
            controller.AddParameter(name, type);

        private static void AddAnyTransition(AnimatorStateMachine machine, AnimatorState target,
            params (AnimatorConditionMode mode, float threshold, string parameter)[] conditions)
        {
            var transition = machine.AddAnyStateTransition(target);
            transition.hasExitTime = false;
            transition.duration = 0.06f;
            // Prevent AnyState from re-entering the same state every frame, which pins
            // gameplay poses (Slide/Defeat/Jump) near normalizedTime≈0 and looks like bind pose.
            transition.canTransitionToSelf = false;
            foreach (var condition in conditions) transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        private static void AddTransition(AnimatorState source, AnimatorState target, bool hasExitTime, float duration,
            params (AnimatorConditionMode mode, float threshold, string parameter)[] conditions)
        {
            var transition = source.AddTransition(target);
            transition.hasExitTime = hasExitTime;
            transition.exitTime = hasExitTime ? 0.86f : 0f;
            transition.duration = duration;
            foreach (var condition in conditions) transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        private static void AddExit(AnimatorState source, AnimatorState target) => AddTransition(source, target, true, 0.08f);

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        }
    }
}
#endif
