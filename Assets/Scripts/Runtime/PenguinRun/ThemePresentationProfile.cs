using UnityEngine;

namespace PenguinRun
{
    internal enum ThemeLandmarkKind
    {
        IceCrystal, IceArch, CedarRuin, SnowPine, AuroraBeacon, MagneticCrystal,
        MistVeil, FogStone, CoralReef, ShellCluster, SkyArch, CloudIsland,
    }

    /// <summary>Visual-only theme configuration; it never changes obstacle or pickup collision rules.</summary>
    internal readonly struct ThemePresentationProfile
    {
        public readonly Color GroundTint;
        public readonly Color HorizonColor;
        public readonly Color ZenithColor;
        public readonly Color AccentColor;
        public readonly ThemeLandmarkKind PrimaryLandmark;
        public readonly ThemeLandmarkKind SecondaryLandmark;
        public readonly EnvironmentPropId Landmark;
        public readonly EnvironmentPropId Signpost;
        public readonly EnvironmentPropId Light;
        public readonly EnvironmentPropId EdgeProp;

        private ThemePresentationProfile(Color ground, Color horizon, Color zenith, Color accent, ThemeLandmarkKind primary, ThemeLandmarkKind secondary)
        {
            GroundTint = ground; HorizonColor = horizon; ZenithColor = zenith; AccentColor = accent;
            PrimaryLandmark = primary; SecondaryLandmark = secondary;
            Landmark = EnvironmentPropId.Landmark;
            Signpost = EnvironmentPropId.Signpost;
            Light = EnvironmentPropId.Light;
            EdgeProp = EnvironmentPropId.EdgeProp;
        }

        public static ThemePresentationProfile For(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.IceLakeEcho => new(new Color(0.32f, 0.78f, 0.9f), new Color(0.17f, 0.52f, 0.73f), new Color(0.01f, 0.05f, 0.16f), new Color(0.3f, 0.9f, 1f), ThemeLandmarkKind.IceCrystal, ThemeLandmarkKind.IceArch),
            RunnerMapTheme.CedarRuins => new(new Color(0.36f, 0.58f, 0.46f), new Color(0.57f, 0.31f, 0.42f), new Color(0.06f, 0.025f, 0.1f), new Color(0.8f, 0.56f, 0.35f), ThemeLandmarkKind.CedarRuin, ThemeLandmarkKind.SnowPine),
            RunnerMapTheme.AuroraField => new(new Color(0.35f, 0.82f, 0.76f), new Color(0.08f, 0.43f, 0.46f), new Color(0.015f, 0.035f, 0.13f), new Color(0.28f, 1f, 0.78f), ThemeLandmarkKind.AuroraBeacon, ThemeLandmarkKind.MagneticCrystal),
            RunnerMapTheme.MistDike => new(new Color(0.48f, 0.58f, 0.66f), new Color(0.31f, 0.38f, 0.48f), new Color(0.045f, 0.07f, 0.13f), new Color(0.54f, 0.64f, 0.8f), ThemeLandmarkKind.MistVeil, ThemeLandmarkKind.FogStone),
            RunnerMapTheme.OceanReef => new(new Color(0.2f, 0.72f, 0.7f), new Color(0.04f, 0.44f, 0.56f), new Color(0.01f, 0.06f, 0.2f), new Color(1f, 0.45f, 0.45f), ThemeLandmarkKind.CoralReef, ThemeLandmarkKind.ShellCluster),
            RunnerMapTheme.SkyFlight => new(new Color(0.72f, 0.86f, 0.98f), new Color(0.58f, 0.8f, 0.96f), new Color(0.14f, 0.33f, 0.7f), new Color(1f, 0.77f, 0.4f), ThemeLandmarkKind.SkyArch, ThemeLandmarkKind.CloudIsland),
            _ => For(RunnerMapTheme.IceLakeEcho),
        };
    }
}
