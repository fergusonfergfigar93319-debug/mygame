using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Presentation-only pickup styling. Gameplay effects and colliders remain owned by existing systems.
    /// 唯一道具配色来源：SegmentSpawner.GetPowerUpColors 委托到这里，避免双色表漂移。
    /// </summary>
    internal readonly struct PowerUpVisualProfile
    {
        public readonly Color CoreColor;
        public readonly Color HaloColor;
        public readonly float PulseScale;

        private PowerUpVisualProfile(Color core, Color halo, float pulse)
        {
            CoreColor = core; HaloColor = halo; PulseScale = pulse;
        }

        public static PowerUpVisualProfile For(PowerUpKind kind, RunnerMapTheme theme)
        {
            PowerUpVisualProfile profile = kind switch
            {
                PowerUpKind.Dash => new(new Color(0.25f, 1f, 0.82f), new Color(0.15f, 0.8f, 0.65f, 0.38f), 1.2f),
                PowerUpKind.Magnet => new(new Color(0.52f, 0.68f, 1f), new Color(0.35f, 0.5f, 0.9f, 0.38f), 1.16f),
                PowerUpKind.Shield => new(new Color(1f, 0.88f, 0.35f), new Color(0.85f, 0.65f, 0.2f, 0.34f), 1.12f),
                PowerUpKind.ScoreStar => new(new Color(1f, 0.58f, 0.9f), new Color(0.9f, 0.35f, 0.75f, 0.36f), 1.15f),
                PowerUpKind.DoubleFishSnack => new(new Color(1f, 0.72f, 0.38f), new Color(0.9f, 0.55f, 0.25f, 0.36f), 1.15f),
                PowerUpKind.TimeHourglass => new(new Color(0.72f, 0.95f, 1f), new Color(0.55f, 0.8f, 0.9f, 0.3f), 1.1f),
                PowerUpKind.GlideFeather => new(new Color(0.92f, 0.96f, 1f), new Color(0.75f, 0.85f, 0.95f, 0.3f), 1.18f),
                PowerUpKind.BubbleShield => new(new Color(0.25f, 0.85f, 0.95f), new Color(0.6f, 0.95f, 1f, 0.32f), 1.12f),
                PowerUpKind.SeahorseBoost => new(new Color(1f, 0.45f, 0.65f), new Color(1f, 0.7f, 0.55f, 0.35f), 1.2f),
                PowerUpKind.CloudWalk => new(new Color(0.95f, 0.98f, 1f), new Color(0.75f, 0.85f, 1f, 0.3f), 1.18f),
                PowerUpKind.WindRider => new(new Color(0.35f, 0.75f, 1f), new Color(0.6f, 0.9f, 1f, 0.32f), 1.18f),
                PowerUpKind.FishBomb => new(new Color(1f, 0.4f, 0.3f), new Color(1f, 0.85f, 0.4f, 0.34f), 1.24f),
                PowerUpKind.SecondHeart => new(new Color(1f, 0.42f, 0.55f), new Color(1f, 0.78f, 0.85f, 0.34f), 1.15f),
                PowerUpKind.IceMirror => new(new Color(0.75f, 0.95f, 1f), new Color(0.9f, 0.98f, 1f, 0.3f), 1.12f),
                PowerUpKind.AuroraChain => new(new Color(0.55f, 0.25f, 1f), new Color(0.3f, 1f, 0.8f, 0.36f), 1.16f),
                PowerUpKind.FogLantern => new(new Color(0.92f, 0.88f, 0.62f), new Color(1f, 0.98f, 0.82f, 0.32f), 1.1f),
                PowerUpKind.TreantArmor => new(new Color(0.35f, 0.58f, 0.28f), new Color(0.62f, 0.45f, 0.28f, 0.34f), 1.12f),
                PowerUpKind.CoralBounce => new(new Color(1f, 0.35f, 0.65f), new Color(0.4f, 0.95f, 0.88f, 0.34f), 1.12f),
                PowerUpKind.ThunderFeather => new(new Color(1f, 0.95f, 0.2f), new Color(0.4f, 0.7f, 1f, 0.34f), 1.18f),
                _ => new(new Color(0.9f, 0.9f, 1f), new Color(0.4f, 0.75f, 1f, 0.3f), 1.1f),
            };

            if (theme == RunnerMapTheme.OceanReef)
                return new PowerUpVisualProfile(profile.CoreColor, Color.Lerp(profile.HaloColor, new Color(0.2f, 0.9f, 0.85f, profile.HaloColor.a), 0.45f), profile.PulseScale);
            return profile;
        }
    }
}
