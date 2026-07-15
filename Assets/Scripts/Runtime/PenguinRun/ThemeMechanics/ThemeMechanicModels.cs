using UnityEngine;

namespace PenguinRun
{
    internal enum ThemeMechanicId
    {
        IceInertia,
        CedarCollapse,
        AuroraPolarity,
        MistBeacon,
        OceanTide,
        SkyWind,
    }

    internal enum ThemeMechanicPhase
    {
        Idle,
        Telegraph,
        Active,
        Resolve,
        Reward,
        Cooldown,
        Cancelled,
    }

    internal enum ThemeMechanicMode
    {
        Story,
        Endless,
        Daily,
    }

    internal enum ThemeMechanicIntensity
    {
        Low,
        Medium,
        High,
        Signature,
    }

    internal enum ThemeMechanicGrade
    {
        None,
        Completed,
        Perfect,
        Flawless,
    }

    internal enum ThemeMechanicCancelReason
    {
        None,
        Boss,
        RouteEvent,
        ThemeTransition,
        Timeout,
        MissingCriticalAsset,
    }

    internal readonly struct ThemeMechanicDefinition
    {
        public ThemeMechanicDefinition(
            ThemeMechanicId id,
            RunnerMapTheme theme,
            string displayName,
            string actionHint,
            string apparatusA,
            string apparatusB,
            string signature,
            float telegraphSeconds,
            float activeSeconds,
            float rewardSeconds,
            float baseWeight)
        {
            Id = id;
            Theme = theme;
            DisplayName = displayName ?? string.Empty;
            ActionHint = actionHint ?? string.Empty;
            ApparatusA = apparatusA ?? string.Empty;
            ApparatusB = apparatusB ?? string.Empty;
            Signature = signature ?? string.Empty;
            TelegraphSeconds = Mathf.Max(2f, telegraphSeconds);
            ActiveSeconds = Mathf.Clamp(activeSeconds, 6f, 12f);
            RewardSeconds = Mathf.Clamp(rewardSeconds, 3f, 6f);
            BaseWeight = Mathf.Max(0.01f, baseWeight);
        }

        public ThemeMechanicId Id { get; }
        public RunnerMapTheme Theme { get; }
        public string DisplayName { get; }
        public string ActionHint { get; }
        public string ApparatusA { get; }
        public string ApparatusB { get; }
        public string Signature { get; }
        public float TelegraphSeconds { get; }
        public float ActiveSeconds { get; }
        public float RewardSeconds { get; }
        public float BaseWeight { get; }
    }

    internal readonly struct ThemeMechanicModifiers
    {
        public ThemeMechanicModifiers(
            float laneResponse,
            float laneDrift,
            float gravity,
            float glideDuration,
            float visibility)
        {
            LaneResponseMultiplier = Mathf.Clamp(laneResponse, 0.55f, 1.35f);
            LaneDriftLanesPerSecond = Mathf.Clamp(laneDrift, -0.32f, 0.32f);
            GravityMultiplier = Mathf.Clamp(gravity, 0.65f, 1.35f);
            GlideDurationMultiplier = Mathf.Clamp(glideDuration, 1f, 1.8f);
            Visibility01 = Mathf.Clamp(visibility, 0.35f, 1f);
        }

        public float LaneResponseMultiplier { get; }
        public float LaneDriftLanesPerSecond { get; }
        public float GravityMultiplier { get; }
        public float GlideDurationMultiplier { get; }
        public float Visibility01 { get; }

        public static ThemeMechanicModifiers Identity => new(1f, 0f, 1f, 1f, 1f);
    }

    internal readonly struct ThemeMechanicSnapshot
    {
        public ThemeMechanicSnapshot(
            ThemeMechanicId id,
            ThemeMechanicPhase phase,
            ThemeMechanicIntensity intensity,
            string title,
            string hint,
            float progress01,
            ThemeMechanicModifiers modifiers,
            bool visible)
        {
            Id = id;
            Phase = phase;
            Intensity = intensity;
            Title = title ?? string.Empty;
            Hint = hint ?? string.Empty;
            Progress01 = Mathf.Clamp01(progress01);
            Modifiers = modifiers;
            Visible = visible;
        }

        public ThemeMechanicId Id { get; }
        public ThemeMechanicPhase Phase { get; }
        public ThemeMechanicIntensity Intensity { get; }
        public string Title { get; }
        public string Hint { get; }
        public float Progress01 { get; }
        public ThemeMechanicModifiers Modifiers { get; }
        public bool Visible { get; }

        public static ThemeMechanicSnapshot Empty => new(
            default,
            ThemeMechanicPhase.Idle,
            ThemeMechanicIntensity.Low,
            string.Empty,
            string.Empty,
            0f,
            ThemeMechanicModifiers.Identity,
            false);
    }
}
