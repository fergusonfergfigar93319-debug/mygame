#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 烘焙后处理装配资产：PostProcessProfile（Bloom/Vignette/ColorGrading）
    /// + Resources 预制体（携带 PostProcessResources 引用，保证打包进真机构建）。
    /// </summary>
    public static class PenguinRunPostFxRigBuilder
    {
        private const string Folder = "Assets/Resources/PenguinRun/PostFx";
        private const string ProfilePath = Folder + "/postfx_profile.asset";
        private const string RigPath = Folder + "/postfx_rig.prefab";

        [MenuItem("PenguinRun/Visuals/Build PostFx Rig")]
        public static void Build()
        {
            PenguinVisualPrefabBuilder.EnsureFolder(Folder);

            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            ConfigureProfile(profile);
            EditorUtility.SetDirty(profile);

            var resources = FindPostProcessResources();
            if (resources == null)
            {
                Debug.LogError("[PostFxRigBuilder] PostProcessResources not found — is com.unity.postprocessing installed?");
                return;
            }

            var rigRoot = new GameObject("postfx_rig");
            try
            {
                var rig = rigRoot.AddComponent<PostFxRigAsset>();
                rig.Resources = resources;
                rig.Profile = profile;
                PrefabUtility.SaveAsPrefabAsset(rigRoot, RigPath);
            }
            finally
            {
                Object.DestroyImmediate(rigRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PostFxRigBuilder] PostFx rig baked.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
            EditorApplication.Exit(0);
        }

        private static void ConfigureProfile(PostProcessProfile profile)
        {
            var bloom = profile.HasSettings<Bloom>() ? profile.GetSetting<Bloom>() : profile.AddSettings<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(2.1f);
            bloom.threshold.Override(1.12f);
            bloom.softKnee.Override(0.5f);
            bloom.diffusion.Override(6.4f);
            bloom.fastMode.Override(false);

            var vignette = profile.HasSettings<Vignette>() ? profile.GetSetting<Vignette>() : profile.AddSettings<Vignette>();
            vignette.enabled.Override(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.62f);
            vignette.rounded.Override(false);

            var grading = profile.HasSettings<ColorGrading>() ? profile.GetSetting<ColorGrading>() : profile.AddSettings<ColorGrading>();
            grading.enabled.Override(true);
            grading.gradingMode.Override(GradingMode.LowDefinitionRange);
            grading.saturation.Override(9f);
            grading.contrast.Override(7f);
            grading.postExposure.Override(0.08f);
        }

        private static PostProcessResources FindPostProcessResources()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:PostProcessResources"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var res = AssetDatabase.LoadAssetAtPath<PostProcessResources>(path);
                if (res != null) return res;
            }
            return null;
        }
    }
}
#endif
