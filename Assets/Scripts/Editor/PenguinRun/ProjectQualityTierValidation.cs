#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// 校验画质分档表：三档必须互不相同、按档单调，且 <c>Apply()</c> 真的把预算写进
    /// QualitySettings。
    ///
    /// 这条门禁存在的原因是上一版分档是「死代码」：<see cref="RunnerPostFx"/> 读
    /// QualitySettings 的等级来决定是否关 Bloom，但没有任何代码设置过那个等级，
    /// 而整段又被 <c>#if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c> 挡在编辑器之外，
    /// 所以「没生效」这件事没有任何机制能发现。
    /// </summary>
    public static class ProjectQualityTierValidation
    {
        public static void ValidateFromCommandLine()
        {
            try
            {
                Validate();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("PenguinRun/Validate/Quality Tiers")]
        public static void Validate()
        {
            var errors = new List<string>();
            var tiers = new[] { RunnerQualityTier.Low, RunnerQualityTier.Medium, RunnerQualityTier.High };

            var budgets = new RunnerQualityBudget[tiers.Length];
            for (var i = 0; i < tiers.Length; i++) budgets[i] = RunnerQualityBudget.For(tiers[i]);

            for (var i = 1; i < budgets.Length; i++)
            {
                var lower = budgets[i - 1];
                var higher = budgets[i];
                var label = $"{tiers[i - 1]}->{tiers[i]}";

                if (higher.ShadowDistance < lower.ShadowDistance)
                    errors.Add($"{label}: shadow distance goes down ({lower.ShadowDistance} -> {higher.ShadowDistance}).");
                if (higher.ParticleRaycastBudget < lower.ParticleRaycastBudget)
                    errors.Add($"{label}: particle raycast budget goes down.");
                if (higher.LodBias < lower.LodBias)
                    errors.Add($"{label}: LOD bias goes down.");
                // mip limit 越小越清晰，所以高档不得比低档大。
                if (higher.TextureMipmapLimit > lower.TextureMipmapLimit)
                    errors.Add($"{label}: texture mipmap limit goes up (textures get blurrier at a higher tier).");
                if (ShadowRank(higher.Shadows) < ShadowRank(lower.Shadows))
                    errors.Add($"{label}: shadow quality goes down.");
                if (SkinRank(higher.SkinWeights) < SkinRank(lower.SkinWeights))
                    errors.Add($"{label}: skin weights go down.");
                if (lower.BloomEnabled && !higher.BloomEnabled)
                    errors.Add($"{label}: bloom turns off at a higher tier.");
                if (Describe(lower) == Describe(higher))
                    errors.Add($"{label}: both tiers resolve to an identical budget, so the split does nothing.");
            }

            // 桌面/编辑器必须落在最高档，否则本机截图门禁看到的画面与发布不一致。
            if (RunnerDeviceTier.Detect() != RunnerQualityTier.High)
                errors.Add("Non-mobile platforms must resolve to the High tier.");

            errors.AddRange(ValidateApplyWritesSettings());

            if (errors.Count > 0)
            {
                var message = "[ProjectQualityTierValidation] FAIL:\n" + string.Join("\n", errors);
                Debug.LogError(message);
                throw new UnityException(message);
            }

            Debug.Log("[ProjectQualityTierValidation] PASS: " +
                      $"Low={Describe(budgets[0])} | Medium={Describe(budgets[1])} | High={Describe(budgets[2])}");
        }

        /// <summary>
        /// 逐档强制覆盖后调用 Apply()，确认预算真的落到 QualitySettings 上。
        /// 编辑器里写 QualitySettings 会弄脏 ProjectSettings 资产，所以前后做快照与还原。
        /// </summary>
        private static IEnumerable<string> ValidateApplyWritesSettings()
        {
            var errors = new List<string>();

            var savedShadows = QualitySettings.shadows;
            var savedDistance = QualitySettings.shadowDistance;
            var savedCascades = QualitySettings.shadowCascades;
            var savedParticles = QualitySettings.particleRaycastBudget;
            var savedLodBias = QualitySettings.lodBias;
            var savedSkin = QualitySettings.skinWeights;
            var savedMip = QualitySettings.globalTextureMipmapLimit;
            // Apply() 也会抬高 asyncUploadTimeSlice；漏掉它会让本校验在 git 里留下
            // 一处 ProjectSettings/QualitySettings.asset 的意外改动。
            var savedUploadSlice = QualitySettings.asyncUploadTimeSlice;
            var savedOverride = RunnerDeviceTier.Override;

            try
            {
                foreach (var tier in new[] { RunnerQualityTier.Low, RunnerQualityTier.Medium, RunnerQualityTier.High })
                {
                    RunnerDeviceTier.Override = tier;
                    var budget = RunnerDeviceTier.Apply();

                    if (QualitySettings.shadows != budget.Shadows)
                        errors.Add($"{tier}: Apply() did not write shadow quality.");
                    if (!Mathf.Approximately(QualitySettings.lodBias, budget.LodBias))
                        errors.Add($"{tier}: Apply() did not write LOD bias.");
                    if (QualitySettings.particleRaycastBudget != budget.ParticleRaycastBudget)
                        errors.Add($"{tier}: Apply() did not write particle raycast budget.");
                    if (QualitySettings.globalTextureMipmapLimit != budget.TextureMipmapLimit)
                        errors.Add($"{tier}: Apply() did not write texture mipmap limit.");
                    if (QualitySettings.skinWeights != budget.SkinWeights)
                        errors.Add($"{tier}: Apply() did not write skin weights.");
                }
            }
            finally
            {
                RunnerDeviceTier.Override = savedOverride;
                QualitySettings.shadows = savedShadows;
                QualitySettings.shadowDistance = savedDistance;
                QualitySettings.shadowCascades = savedCascades;
                QualitySettings.particleRaycastBudget = savedParticles;
                QualitySettings.lodBias = savedLodBias;
                QualitySettings.skinWeights = savedSkin;
                QualitySettings.globalTextureMipmapLimit = savedMip;
                QualitySettings.asyncUploadTimeSlice = savedUploadSlice;
            }

            return errors;
        }

        private static int ShadowRank(ShadowQuality quality) => quality switch
        {
            ShadowQuality.Disable => 0,
            ShadowQuality.HardOnly => 1,
            _ => 2,
        };

        private static int SkinRank(SkinWeights weights) => weights switch
        {
            SkinWeights.OneBone => 1,
            SkinWeights.TwoBones => 2,
            SkinWeights.FourBones => 4,
            _ => 5,
        };

        private static string Describe(RunnerQualityBudget budget) =>
            $"bloom={budget.BloomEnabled}/{budget.BloomFastMode} shadows={budget.Shadows}@{budget.ShadowDistance} " +
            $"cascades={budget.ShadowCascades} particles={budget.ParticleRaycastBudget} lod={budget.LodBias} " +
            $"skin={budget.SkinWeights} mip={budget.TextureMipmapLimit}";
    }
}
#endif
