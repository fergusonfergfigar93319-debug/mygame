using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 每种 Boss 招式的静态动效与身体动画参数。
    /// </summary>
    internal sealed class BossPatternVfxProfile
    {
        public float WindupBodyLean;
        public float StrikeBodyLungeZ;
        public float StrikeTorsoPitch;
        public float StrikeHeadPitch;
        public float ImpactBurstScale = 1f;
        public float CameraShakeAmp = 0.09f;
        public float CameraShakeDuration = 0.14f;
        public string SfxStrikeId = "shield_bounce";

        public static BossPatternVfxProfile Get(BossPattern pattern) =>
            pattern switch
            {
                BossPattern.SweepLow => Sweep,
                BossPattern.DiveHigh => Dive,
                BossPattern.ChargeAcross => Charge,
                BossPattern.RangedSalvo => Salvo,
                BossPattern.CenterBeam => Beam,
                BossPattern.QuakePulse => Quake,
                _ => Default,
            };

        private static readonly BossPatternVfxProfile Default = new()
        {
            WindupBodyLean = 10f,
            StrikeBodyLungeZ = 0.35f,
            StrikeTorsoPitch = 8f,
            CameraShakeAmp = 0.08f,
            SfxStrikeId = "shield_bounce",
        };

        private static readonly BossPatternVfxProfile Sweep = new()
        {
            WindupBodyLean = 16f,
            StrikeBodyLungeZ = 0.42f,
            StrikeTorsoPitch = -22f,
            StrikeHeadPitch = -12f,
            ImpactBurstScale = 1.1f,
            CameraShakeAmp = 0.095f,
            CameraShakeDuration = 0.13f,
            SfxStrikeId = "shield_bounce",
        };

        private static readonly BossPatternVfxProfile Dive = new()
        {
            WindupBodyLean = 8f,
            StrikeBodyLungeZ = 0.95f,
            StrikeTorsoPitch = 38f,
            StrikeHeadPitch = 28f,
            ImpactBurstScale = 1.15f,
            CameraShakeAmp = 0.11f,
            CameraShakeDuration = 0.16f,
            SfxStrikeId = "boss_land",
        };

        private static readonly BossPatternVfxProfile Charge = new()
        {
            WindupBodyLean = 22f,
            StrikeBodyLungeZ = 0.72f,
            StrikeTorsoPitch = 48f,
            StrikeHeadPitch = 18f,
            ImpactBurstScale = 1.2f,
            CameraShakeAmp = 0.105f,
            CameraShakeDuration = 0.15f,
            SfxStrikeId = "crash",
        };

        private static readonly BossPatternVfxProfile Salvo = new()
        {
            WindupBodyLean = 12f,
            StrikeBodyLungeZ = 0.28f,
            StrikeTorsoPitch = 14f,
            ImpactBurstScale = 0.95f,
            CameraShakeAmp = 0.075f,
            CameraShakeDuration = 0.1f,
            SfxStrikeId = "ice_crack",
        };

        private static readonly BossPatternVfxProfile Beam = new()
        {
            WindupBodyLean = 14f,
            StrikeBodyLungeZ = 0.38f,
            StrikeTorsoPitch = 26f,
            StrikeHeadPitch = 8f,
            ImpactBurstScale = 1.05f,
            CameraShakeAmp = 0.12f,
            CameraShakeDuration = 0.17f,
            SfxStrikeId = "boss_shield_break",
        };

        private static readonly BossPatternVfxProfile Quake = new()
        {
            WindupBodyLean = 24f,
            StrikeBodyLungeZ = 0.18f,
            StrikeTorsoPitch = -32f,
            StrikeHeadPitch = -18f,
            ImpactBurstScale = 1.35f,
            CameraShakeAmp = 0.14f,
            CameraShakeDuration = 0.2f,
            SfxStrikeId = "boss_windup",
        };
    }
}
