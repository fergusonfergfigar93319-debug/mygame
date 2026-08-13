using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Per-theme terrain modulation: each map theme gets a distinct ride feel.
    /// </summary>
    public struct MapThemeTerrainProfile
    {
        public float HeightAmplitude;    // overall vertical scale
        public float WaveFrequency;      // how quickly terrain undulates
        public float CurveStrength;      // how sharp the S-curve bends are
        public float RampStrength;       // long gradual slope intensity
        public float BumpDensity;        // small-scale roughness (micro-bumps)

        public static MapThemeTerrainProfile ForTheme(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.IceLakeEcho => new MapThemeTerrainProfile
            {
                HeightAmplitude = 0.55f, WaveFrequency = 1.4f, CurveStrength = 1.35f,
                RampStrength = 0.45f, BumpDensity = 0.15f,
            },
            RunnerMapTheme.CedarRuins => new MapThemeTerrainProfile
            {
                HeightAmplitude = 1.1f, WaveFrequency = 1.05f, CurveStrength = 0.95f,
                RampStrength = 0.9f, BumpDensity = 0.55f,
            },
            RunnerMapTheme.AuroraField => new MapThemeTerrainProfile
            {
                HeightAmplitude = 0.45f, WaveFrequency = 0.5f, CurveStrength = 0.6f,
                RampStrength = 0.35f, BumpDensity = 0.12f,
            },
            RunnerMapTheme.MistDike => new MapThemeTerrainProfile
            {
                HeightAmplitude = 1.3f, WaveFrequency = 1.2f, CurveStrength = 0.8f,
                RampStrength = 0.95f, BumpDensity = 0.7f,
            },
            RunnerMapTheme.OceanReef => new MapThemeTerrainProfile
            {
                HeightAmplitude = 1.6f, WaveFrequency = 1.55f, CurveStrength = 1.1f,
                RampStrength = 1.15f, BumpDensity = 0.85f,
            },
            RunnerMapTheme.SkyFlight => new MapThemeTerrainProfile
            {
                HeightAmplitude = 1.9f, WaveFrequency = 0.35f, CurveStrength = 0.4f,
                RampStrength = 1.5f, BumpDensity = 0.05f,
            },
            _ => new MapThemeTerrainProfile
            {
                HeightAmplitude = 1f, WaveFrequency = 1f, CurveStrength = 1f,
                RampStrength = 0.82f, BumpDensity = 0.35f,
            },
        };
    }

    /// <summary>
    /// 沿前进方向起伏的路面高度 + 摄像机轻微转向/横移，营造「有坡度、在转弯」的感觉；
    /// 逻辑跑道仍是三车道直线，碰撞与车道不变。
    /// 每个地图主题拥有独特的地形特征（崎岖度、弯道强度、坡度等）。
    /// </summary>
    internal static class TrackTerrain
    {
        private static MapThemeTerrainProfile _activeProfile = MapThemeTerrainProfile.ForTheme(RunnerMapTheme.IceLakeEcho);

        /// <summary>设置当前地图主题的地形参数，在主题切换时调用。</summary>
        public static void SetThemeProfile(RunnerMapTheme theme)
        {
            _activeProfile = MapThemeTerrainProfile.ForTheme(theme);
        }

        private static float Progress01(float d, float startMeters, float endMeters)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(startMeters, endMeters, d));
        }

        /// <summary>角色脚底下的路面世界 Y（runDistance = WorldDirector.Distance，worldZ = 物体/角色 z）。</summary>
        public static float SurfaceY(float worldZ, float runDistanceMeters, float baseGroundY)
        {
            var d = runDistanceMeters;
            var z = worldZ;
            var p = _activeProfile;

            // 开局地面尽量平直，随距离递增再逐步放大地形起伏。
            // 渐入提前收满（520→360）：让海洋的"糙"、天空的"高"在第一分钟内就读得出来。
            var terrain01 = Progress01(d, 45f, 360f);
            var w1 = Mathf.Sin(d * 0.0038f * p.WaveFrequency + z * 0.013f) * Mathf.Lerp(0.06f, 0.34f * p.HeightAmplitude, terrain01);
            var w2 = Mathf.Sin(d * 0.0072f * p.WaveFrequency - z * 0.009f) * Mathf.Lerp(0.04f, 0.2f * p.HeightAmplitude, terrain01);
            var w3 = Mathf.Sin(z * 0.022f + d * 0.0046f) * Mathf.Lerp(0.03f, 0.14f * p.HeightAmplitude, terrain01);

            // Micro-bumps: small high-frequency noise for texture feel
            var micro = Mathf.Sin(z * 0.085f + d * 0.012f) * Mathf.Lerp(0.01f, 0.06f * p.BumpDensity, terrain01)
                      + Mathf.Cos(z * 0.13f - d * 0.009f) * Mathf.Lerp(0.005f, 0.04f * p.BumpDensity, terrain01);

            var ramp = Mathf.Sin(d * 0.00165f) * Mathf.Lerp(0f, 0.82f * p.RampStrength, terrain01);
            return baseGroundY + w1 + w2 + w3 + micro + ramp;
        }

        /// <summary>摄像机绕 Y 的额外偏角（度），幅度加大以呈现转弯感。</summary>
        public static float CurveYawDegrees(float runDistanceMeters)
        {
            var strength = Progress01(runDistanceMeters, 90f, 520f);
            var s = _activeProfile.CurveStrength;
            return (Mathf.Sin(runDistanceMeters * 0.0034f) * 7.5f * s
                 + Mathf.Sin(runDistanceMeters * 0.0018f) * 4.2f * s) * strength;
        }

        public static float CurveCameraX(float runDistanceMeters)
        {
            var strength = Progress01(runDistanceMeters, 90f, 520f);
            var s = _activeProfile.CurveStrength;
            return Mathf.Sin(runDistanceMeters * 0.0031f + 0.7f) * 3.2f * s * strength;
        }

        /// <summary>相机绕 Z 轴的侧倾角（度）——入弯时向内倾斜，强化转弯体感。</summary>
        public static float BankAngleDegrees(float runDistanceMeters)
        {
            var d = runDistanceMeters;
            var strength = Progress01(d, 100f, 500f);
            var s = _activeProfile.CurveStrength;
            return -(Mathf.Cos(d * 0.0034f) * 3.1f * s + Mathf.Cos(d * 0.0018f) * 0.88f * s) * strength;
        }

        // ── 跑道物理弯曲 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 跑道中心线在世界 X 方向的偏移；周期恰好等于 <see cref="RoadLoopLength"/>（128 m），
        /// 保证路面瓦片循环时无缝衔接。
        /// </summary>
        public const float RoadLoopLength = 128f;

        public static float WorldCurveX(float worldZ, float runDistanceMeters)
        {
            const float k = Mathf.PI * 2f / RoadLoopLength;
            var strength = Progress01(runDistanceMeters, 110f, 560f);
            var s = _activeProfile.CurveStrength;
            // 单谐波 S 形，worldZ=0 及其整数倍处均为 0，保证瓦片循环时X无跳变
            return Mathf.Sin(worldZ * k) * 1.3f * s * strength;
        }

        /// <summary>跑道切线在 XZ 平面内的 Y 轴偏角（度），用于路面瓦片朝向。</summary>
        public static float WorldCurveTangentYaw(float worldZ, float runDistanceMeters)
        {
            const float k = Mathf.PI * 2f / RoadLoopLength;
            var strength = Progress01(runDistanceMeters, 110f, 560f);
            var s = _activeProfile.CurveStrength;
            var dxdz = Mathf.Cos(worldZ * k) * 1.3f * s * k * strength;
            return Mathf.Atan2(dxdz, 1f) * Mathf.Rad2Deg;
        }
    }
}
