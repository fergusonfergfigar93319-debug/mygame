using UnityEngine;

namespace PenguinRun
{
    internal enum RunnerQualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    /// 每档的渲染预算。此前项目里有「分档」的分支（<see cref="RunnerPostFx"/> 读
    /// <c>QualitySettings.GetQualityLevel()</c> 决定是否关 Bloom），却没有任何代码
    /// 按设备能力设置过那个等级——结果骁龙 4 系与 8 系拿到完全相同的画质，
    /// 分档只存在于名义上。本结构补上缺失的决策层。
    ///
    /// 只收录不影响构图与判定的杠杆：分辨率缩放会与 `FramingReference`
    /// （`ApplyRunnerCameraViewport169` 读 Screen.width/height）纠缠，在没有实机
    /// 验证的前提下不纳入。
    /// </summary>
    internal readonly struct RunnerQualityBudget
    {
        public readonly bool BloomEnabled;
        public readonly bool BloomFastMode;
        public readonly ShadowQuality Shadows;
        public readonly float ShadowDistance;
        public readonly int ShadowCascades;
        public readonly int ParticleRaycastBudget;
        public readonly float LodBias;
        public readonly SkinWeights SkinWeights;
        /// <summary>贴图 mip 起始级别；1 = 只加载半分辨率，省显存与带宽。</summary>
        public readonly int TextureMipmapLimit;

        private RunnerQualityBudget(bool bloomEnabled, bool bloomFastMode, ShadowQuality shadows,
            float shadowDistance, int shadowCascades, int particleRaycastBudget,
            float lodBias, SkinWeights skinWeights, int textureMipmapLimit)
        {
            BloomEnabled = bloomEnabled;
            BloomFastMode = bloomFastMode;
            Shadows = shadows;
            ShadowDistance = shadowDistance;
            ShadowCascades = shadowCascades;
            ParticleRaycastBudget = particleRaycastBudget;
            LodBias = lodBias;
            SkinWeights = skinWeights;
            TextureMipmapLimit = textureMipmapLimit;
        }

        public static RunnerQualityBudget For(RunnerQualityTier tier) => tier switch
        {
            // 低档：关 Bloom 与实时阴影，主角骨骼权重降到 2（四个 LOD 全是蒙皮网格，
            // 这是低端机上少数确定有效的省法），贴图降半分辨率。
            RunnerQualityTier.Low => new(
                false, false, ShadowQuality.Disable, 0f, 1, 16,
                0.7f, SkinWeights.TwoBones, 1),

            // 中档：仍关 Bloom（后处理是移动端最贵的一项），保留硬阴影但压近距离。
            RunnerQualityTier.Medium => new(
                false, false, ShadowQuality.HardOnly, 32f, 1, 48,
                1.0f, SkinWeights.TwoBones, 0),

            // 高档：Bloom 走 fastMode，软阴影，完整 LOD 距离。
            RunnerQualityTier.High => new(
                true, true, ShadowQuality.All, 45f, 2, 64,
                1.2f, SkinWeights.FourBones, 0),

            _ => For(RunnerQualityTier.Medium),
        };
    }

    /// <summary>按设备能力选档，并把选中的预算真正写进 QualitySettings。</summary>
    internal static class RunnerDeviceTier
    {
        private static RunnerQualityTier? resolved;

        /// <summary>测试与工具用的强制覆盖；传 null 恢复自动探测。</summary>
        internal static RunnerQualityTier? Override { get; set; }

        public static RunnerQualityTier Current => Override ?? (resolved ??= Detect());

        /// <summary>
        /// 依据内存、核心数与显存分档。三项都取保守方向：任意一项落在低端区间即降档，
        /// 因为跑酷是持续 60fps 的负载，偶发掉帧比整体画质低更伤手感。
        /// </summary>
        public static RunnerQualityTier Detect()
        {
            if (!Application.isMobilePlatform) return RunnerQualityTier.High;

            var systemMemory = SystemInfo.systemMemorySize;      // MB
            var graphicsMemory = SystemInfo.graphicsMemorySize;  // MB
            var cores = SystemInfo.processorCount;

            if (systemMemory < 3072 || cores <= 4 || graphicsMemory < 1024)
                return RunnerQualityTier.Low;

            if (systemMemory >= 6144 && cores >= 8 && graphicsMemory >= 2048)
                return RunnerQualityTier.High;

            return RunnerQualityTier.Medium;
        }

        /// <summary>
        /// 把当前档位写进 QualitySettings。必须在 <see cref="RunnerPostFx.TryAttach"/>
        /// 之前调用——后处理档位读的就是这里设置的结果。
        /// </summary>
        public static RunnerQualityBudget Apply()
        {
            var budget = RunnerQualityBudget.For(Current);

            QualitySettings.shadows = budget.Shadows;
            QualitySettings.shadowDistance = budget.ShadowDistance;
            QualitySettings.shadowCascades = budget.ShadowCascades;
            QualitySettings.particleRaycastBudget = budget.ParticleRaycastBudget;
            QualitySettings.lodBias = budget.LodBias;
            QualitySettings.skinWeights = budget.SkinWeights;
            QualitySettings.globalTextureMipmapLimit = budget.TextureMipmapLimit;
            QualitySettings.asyncUploadTimeSlice = Mathf.Max(QualitySettings.asyncUploadTimeSlice, 4);

            return budget;
        }
    }
}
