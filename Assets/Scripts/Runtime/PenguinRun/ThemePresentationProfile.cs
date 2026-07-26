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

        // 光照档案。此前六个主题共用同一套方向光与雾距，只有雾色不同，
        // 结果每张图的「光」完全一样。以下字段让主色调、光向和景深随主题走。
        public readonly Color KeyLightColor;
        public readonly float KeyLightIntensity;
        public readonly Vector3 KeyLightEuler;
        public readonly Color RimLightColor;
        public readonly float RimLightIntensity;
        public readonly Color AmbientLight;
        public readonly Color FogColor;
        public readonly float FogStart;
        public readonly float FogEnd;

        private ThemePresentationProfile(Color ground, Color horizon, Color zenith, Color accent,
            ThemeLandmarkKind primary, ThemeLandmarkKind secondary,
            Color keyLight, float keyIntensity, Vector3 keyEuler,
            Color rimLight, float rimIntensity,
            Color ambient, Color fog, float fogStart, float fogEnd)
        {
            GroundTint = ground; HorizonColor = horizon; ZenithColor = zenith; AccentColor = accent;
            PrimaryLandmark = primary; SecondaryLandmark = secondary;
            Landmark = EnvironmentPropId.Landmark;
            Signpost = EnvironmentPropId.Signpost;
            Light = EnvironmentPropId.Light;
            EdgeProp = EnvironmentPropId.EdgeProp;
            KeyLightColor = keyLight; KeyLightIntensity = keyIntensity; KeyLightEuler = keyEuler;
            RimLightColor = rimLight; RimLightIntensity = rimIntensity;
            AmbientLight = ambient; FogColor = fog; FogStart = fogStart; FogEnd = fogEnd;
        }

        public static ThemePresentationProfile For(RunnerMapTheme theme) => theme switch
        {
            // 冰湖回音谷：极夜月光，高色温、低斜角，长雾带拉出冰面纵深。
            RunnerMapTheme.IceLakeEcho => new(
                new Color(0.32f, 0.78f, 0.9f), new Color(0.17f, 0.52f, 0.73f), new Color(0.01f, 0.05f, 0.16f), new Color(0.3f, 0.9f, 1f),
                ThemeLandmarkKind.IceCrystal, ThemeLandmarkKind.IceArch,
                new Color(0.78f, 0.88f, 1f), 1.2f, new Vector3(48f, -28f, 0f),
                new Color(0.6f, 0.74f, 1f), 0.35f,
                new Color(0.42f, 0.58f, 0.68f), new Color(0.66f, 0.82f, 0.9f), 55f, 180f),

            // 雪松废墟：低角度暮色侧光，暖橙主光配紫调环境，雾带压近制造压迫感。
            RunnerMapTheme.CedarRuins => new(
                new Color(0.36f, 0.58f, 0.46f), new Color(0.57f, 0.31f, 0.42f), new Color(0.06f, 0.025f, 0.1f), new Color(0.8f, 0.56f, 0.35f),
                ThemeLandmarkKind.CedarRuin, ThemeLandmarkKind.SnowPine,
                new Color(1f, 0.76f, 0.55f), 1.05f, new Vector3(26f, -52f, 0f),
                new Color(0.68f, 0.5f, 0.78f), 0.42f,
                new Color(0.48f, 0.38f, 0.42f), new Color(0.55f, 0.42f, 0.55f), 40f, 150f),

            // 极光磁场：主光压暗让极光成为唯一亮源，青绿边光强化剪影。
            RunnerMapTheme.AuroraField => new(
                new Color(0.35f, 0.82f, 0.76f), new Color(0.08f, 0.43f, 0.46f), new Color(0.015f, 0.035f, 0.13f), new Color(0.28f, 1f, 0.78f),
                ThemeLandmarkKind.AuroraBeacon, ThemeLandmarkKind.MagneticCrystal,
                new Color(0.62f, 0.86f, 0.92f), 0.85f, new Vector3(58f, -12f, 0f),
                new Color(0.3f, 1f, 0.8f), 0.58f,
                new Color(0.36f, 0.44f, 0.58f), new Color(0.35f, 0.52f, 0.62f), 48f, 165f),

            // 北境雾堤：无方向感的漫射光，雾带最近——可视距离本身就是这张图的机制。
            RunnerMapTheme.MistDike => new(
                new Color(0.48f, 0.58f, 0.66f), new Color(0.31f, 0.38f, 0.48f), new Color(0.045f, 0.07f, 0.13f), new Color(0.54f, 0.64f, 0.8f),
                ThemeLandmarkKind.MistVeil, ThemeLandmarkKind.FogStone,
                new Color(0.82f, 0.86f, 0.9f), 0.78f, new Vector3(66f, -20f, 0f),
                new Color(0.7f, 0.76f, 0.85f), 0.3f,
                new Color(0.32f, 0.38f, 0.44f), new Color(0.42f, 0.46f, 0.5f), 28f, 118f),

            // 海洋珊瑚礁：高角度水下焦散光，暖珊瑚色边光对抗整体青绿。
            RunnerMapTheme.OceanReef => new(
                new Color(0.2f, 0.72f, 0.7f), new Color(0.04f, 0.44f, 0.56f), new Color(0.01f, 0.06f, 0.2f), new Color(1f, 0.45f, 0.45f),
                ThemeLandmarkKind.CoralReef, ThemeLandmarkKind.ShellCluster,
                new Color(0.62f, 0.95f, 0.98f), 1.1f, new Vector3(72f, -8f, 0f),
                new Color(1f, 0.58f, 0.52f), 0.4f,
                new Color(0.25f, 0.55f, 0.6f), new Color(0.2f, 0.55f, 0.65f), 34f, 132f),

            // 天空飞翔：全场唯一的日光场景，强主光 + 最远雾带换取开阔感。
            RunnerMapTheme.SkyFlight => new(
                new Color(0.72f, 0.86f, 0.98f), new Color(0.58f, 0.8f, 0.96f), new Color(0.14f, 0.33f, 0.7f), new Color(1f, 0.77f, 0.4f),
                ThemeLandmarkKind.SkyArch, ThemeLandmarkKind.CloudIsland,
                new Color(1f, 0.96f, 0.86f), 1.45f, new Vector3(38f, -36f, 0f),
                new Color(0.72f, 0.85f, 1f), 0.28f,
                new Color(0.65f, 0.75f, 0.9f), new Color(0.7f, 0.85f, 0.98f), 70f, 215f),

            _ => For(RunnerMapTheme.IceLakeEcho),
        };
    }
}
