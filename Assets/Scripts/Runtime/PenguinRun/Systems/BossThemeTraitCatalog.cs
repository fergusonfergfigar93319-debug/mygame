using UnityEngine;

namespace PenguinRun
{
    internal enum BossThemeTraitKind
    {
        IceFracture,
        CedarRootGuard,
        AuroraPolarity,
        MistDecoy,
        CoralRebound,
        StormDraft,
    }

    internal readonly struct BossThemeTrait
    {
        public readonly BossThemeTraitKind Kind;
        public readonly string DisplayName;
        public readonly string ThemedRelicId;
        public readonly Color AccentColor;
        public readonly float AttackIntervalMultiplier;
        public readonly float VulnerableWindowMultiplier;
        public readonly int StrikeBurstCount;

        public BossThemeTrait(
            BossThemeTraitKind kind,
            string displayName,
            string themedRelicId,
            Color accentColor,
            float attackIntervalMultiplier,
            float vulnerableWindowMultiplier,
            int strikeBurstCount)
        {
            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            ThemedRelicId = themedRelicId ?? string.Empty;
            AccentColor = accentColor;
            AttackIntervalMultiplier = Mathf.Clamp(attackIntervalMultiplier, 0.75f, 1.25f);
            VulnerableWindowMultiplier = Mathf.Clamp(vulnerableWindowMultiplier, 0.75f, 1.35f);
            StrikeBurstCount = Mathf.Clamp(strikeBurstCount, 1, 6);
        }
    }

    internal static class BossThemeTraitCatalog
    {
        public static BossThemeTrait For(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.IceLakeEcho => new(
                BossThemeTraitKind.IceFracture, "冰裂回声", "relic_ice_fracture",
                new Color(0.42f, 0.9f, 1f, 0.82f), 0.95f, 1.05f, 6),
            RunnerMapTheme.CedarRuins => new(
                BossThemeTraitKind.CedarRootGuard, "雪松根卫", "relic_cedar_root",
                new Color(0.48f, 0.78f, 0.42f, 0.82f), 1.12f, 0.9f, 4),
            RunnerMapTheme.AuroraField => new(
                BossThemeTraitKind.AuroraPolarity, "极光换极", "relic_aurora_polarity",
                new Color(0.45f, 1f, 0.78f, 0.82f), 0.9f, 1.12f, 5),
            RunnerMapTheme.MistDike => new(
                BossThemeTraitKind.MistDecoy, "雾影分身", "relic_mist_decoy",
                new Color(0.72f, 0.82f, 0.95f, 0.58f), 1.05f, 1.18f, 3),
            RunnerMapTheme.OceanReef => new(
                BossThemeTraitKind.CoralRebound, "珊瑚回弹", "relic_coral_rebound",
                new Color(1f, 0.48f, 0.52f, 0.78f), 1f, 1.08f, 5),
            RunnerMapTheme.SkyFlight => new(
                BossThemeTraitKind.StormDraft, "风暴升流", "relic_storm_draft",
                new Color(1f, 0.88f, 0.42f, 0.82f), 0.82f, 0.92f, 6),
            _ => For(RunnerMapTheme.IceLakeEcho),
        };
    }
}
