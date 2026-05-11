using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 沿前进方向起伏的路面高度 + 摄像机轻微转向/横移，营造「有坡度、在转弯」的感觉；
    /// 逻辑跑道仍是三车道直线，碰撞与车道不变。
    /// </summary>
    internal static class TrackTerrain
    {
        private static float Progress01(float d, float startMeters, float endMeters)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(startMeters, endMeters, d));
        }

        /// <summary>角色脚底下的路面世界 Y（runDistance = WorldDirector.Distance，worldZ = 物体/角色 z）。</summary>
        public static float SurfaceY(float worldZ, float runDistanceMeters, float baseGroundY)
        {
            var d = runDistanceMeters;
            var z = worldZ;
            // 开局地面尽量平直，随距离递增再逐步放大地形起伏。
            var terrain01 = Progress01(d, 55f, 520f);
            var w1 = Mathf.Sin(d * 0.0038f + z * 0.013f) * Mathf.Lerp(0.06f, 0.34f, terrain01);
            var w2 = Mathf.Sin(d * 0.0072f - z * 0.009f) * Mathf.Lerp(0.04f, 0.2f, terrain01);
            var w3 = Mathf.Sin(z * 0.022f + d * 0.0046f) * Mathf.Lerp(0.03f, 0.14f, terrain01);
            var ramp = Mathf.Sin(d * 0.00165f) * Mathf.Lerp(0f, 0.82f, terrain01);
            return baseGroundY + w1 + w2 + w3 + ramp;
        }

        /// <summary>摄像机绕 Y 的额外偏角（度），幅度加大以呈现转弯感。</summary>
        public static float CurveYawDegrees(float runDistanceMeters)
        {
            var strength = Progress01(runDistanceMeters, 120f, 780f);
            return (Mathf.Sin(runDistanceMeters * 0.0034f) * 7.5f
                 + Mathf.Sin(runDistanceMeters * 0.0018f) * 4.2f) * strength;
        }

        public static float CurveCameraX(float runDistanceMeters)
        {
            var strength = Progress01(runDistanceMeters, 120f, 780f);
            return Mathf.Sin(runDistanceMeters * 0.0031f + 0.7f) * 3.2f * strength;
        }

        /// <summary>相机绕 Z 轴的侧倾角（度）——入弯时向内倾斜，强化转弯体感。</summary>
        public static float BankAngleDegrees(float runDistanceMeters)
        {
            var d = runDistanceMeters;
            var strength = Progress01(d, 130f, 760f);
            return -(Mathf.Cos(d * 0.0034f) * 3.1f + Mathf.Cos(d * 0.0018f) * 0.88f) * strength;
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
            var strength = Progress01(runDistanceMeters, 140f, 860f);
            // 单谐波 S 形，worldZ=0 及其整数倍处均为 0，保证瓦片循环时X无跳变
            return Mathf.Sin(worldZ * k) * 1.3f * strength;
        }

        /// <summary>跑道切线在 XZ 平面内的 Y 轴偏角（度），用于路面瓦片朝向。</summary>
        public static float WorldCurveTangentYaw(float worldZ, float runDistanceMeters)
        {
            const float k = Mathf.PI * 2f / RoadLoopLength;
            var strength = Progress01(runDistanceMeters, 140f, 860f);
            var dxdz = Mathf.Cos(worldZ * k) * 1.3f * k * strength;
            return Mathf.Atan2(dxdz, 1f) * Mathf.Rad2Deg;
        }
    }
}
