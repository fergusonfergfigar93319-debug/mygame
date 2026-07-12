using UnityEngine;

namespace PenguinRun
{
    /// <summary>Presentation-only pickup styling. Gameplay effects and colliders remain owned by existing systems.</summary>
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
                PowerUpKind.Dash or PowerUpKind.SeahorseBoost => new(new Color(1f, 0.5f, 0.18f), new Color(1f, 0.78f, 0.22f, 0.35f), 1.2f),
                PowerUpKind.Magnet or PowerUpKind.AuroraChain => new(new Color(0.38f, 0.9f, 1f), new Color(0.5f, 0.3f, 1f, 0.38f), 1.16f),
                PowerUpKind.Shield or PowerUpKind.BubbleShield or PowerUpKind.TreantArmor or PowerUpKind.CoralBounce => new(new Color(0.24f, 0.9f, 0.72f), new Color(0.25f, 1f, 0.8f, 0.32f), 1.12f),
                PowerUpKind.GlideFeather or PowerUpKind.CloudWalk or PowerUpKind.WindRider or PowerUpKind.ThunderFeather => new(new Color(0.92f, 0.96f, 1f), new Color(0.4f, 0.76f, 1f, 0.3f), 1.18f),
                PowerUpKind.ScoreStar or PowerUpKind.DoubleFishSnack or PowerUpKind.SecondHeart => new(new Color(1f, 0.82f, 0.25f), new Color(1f, 0.65f, 0.2f, 0.36f), 1.15f),
                PowerUpKind.TimeHourglass or PowerUpKind.FogLantern => new(new Color(0.72f, 0.7f, 1f), new Color(0.55f, 0.7f, 1f, 0.3f), 1.1f),
                PowerUpKind.FishBomb => new(new Color(1f, 0.3f, 0.25f), new Color(1f, 0.15f, 0.1f, 0.34f), 1.24f),
                PowerUpKind.IceMirror => new(new Color(0.5f, 0.9f, 1f), new Color(0.32f, 0.78f, 1f, 0.3f), 1.12f),
                _ => new(new Color(0.9f, 0.9f, 1f), new Color(0.4f, 0.75f, 1f, 0.3f), 1.1f),
            };

            if (theme == RunnerMapTheme.OceanReef)
                return new PowerUpVisualProfile(profile.CoreColor, Color.Lerp(profile.HaloColor, new Color(0.2f, 0.9f, 0.85f, profile.HaloColor.a), 0.45f), profile.PulseScale);
            return profile;
        }
    }
}
