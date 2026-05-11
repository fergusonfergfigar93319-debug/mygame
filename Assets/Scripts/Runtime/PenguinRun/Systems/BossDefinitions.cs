using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 单只 boss 的静态数据：视觉、配色、攻击模式池等。
    /// </summary>
    internal sealed class BossDefinition
    {
        public string BossId;
        public string DisplayName;
        public Color BodyColor;
        public Color TrimColor;
        public Color GlowColor;
        public BossSilhouette Silhouette;
        public BossPattern[] PatternPool;
        public string DefeatCodexLabel;
    }

    internal enum BossSilhouette
    {
        SnowKing,       // 冰湖回音谷 / 默认
        CedarSentinel,  // 雪松废墟
        AuroraSerpent,  // 极光磁场
        MistGuardian,   // 雾堤
        CoralKraken,    // 海洋珊瑚礁
        StormEagle,     // 天空飞翔
    }

    /// <summary>
    /// 根据地图主题选择 boss 定义；同一主题可能有多只 boss（按击败次数轮换）。
    /// </summary>
    internal static class BossDefinitions
    {
        public static BossDefinition PickFor(RunnerMapTheme theme, int defeated)
        {
            return theme switch
            {
                RunnerMapTheme.CedarRuins => CedarSentinel,
                RunnerMapTheme.AuroraField => AuroraSerpent,
                RunnerMapTheme.MistDike => MistGuardian,
                RunnerMapTheme.OceanReef => CoralKraken,
                RunnerMapTheme.SkyFlight => StormEagle,
                _ => SnowKing,
            };
        }

        public static readonly BossDefinition SnowKing = new()
        {
            BossId = "snow_king",
            DisplayName = "冰霜雪王",
            BodyColor = new Color(0.32f, 0.55f, 0.78f),
            TrimColor = new Color(0.85f, 0.95f, 1f),
            GlowColor = new Color(0.45f, 0.92f, 1f, 0.65f),
            Silhouette = BossSilhouette.SnowKing,
            PatternPool = new[]
            {
                BossPattern.SweepLow, BossPattern.DiveHigh,
                BossPattern.ChargeAcross, BossPattern.RangedSalvo,
                BossPattern.CenterBeam, BossPattern.QuakePulse,
            },
            DefeatCodexLabel = "击败冰霜雪王",
        };

        public static readonly BossDefinition CedarSentinel = new()
        {
            BossId = "cedar_sentinel",
            DisplayName = "雪松哨兵",
            BodyColor = new Color(0.45f, 0.32f, 0.18f),
            TrimColor = new Color(0.85f, 0.62f, 0.35f),
            GlowColor = new Color(1f, 0.55f, 0.25f, 0.7f),
            Silhouette = BossSilhouette.CedarSentinel,
            PatternPool = new[]
            {
                BossPattern.SweepLow, BossPattern.RangedSalvo,
                BossPattern.ChargeAcross, BossPattern.QuakePulse,
            },
            DefeatCodexLabel = "击败雪松哨兵",
        };

        public static readonly BossDefinition AuroraSerpent = new()
        {
            BossId = "aurora_serpent",
            DisplayName = "极光长蛇",
            BodyColor = new Color(0.52f, 0.32f, 0.85f),
            TrimColor = new Color(0.4f, 1f, 0.85f),
            GlowColor = new Color(0.6f, 0.4f, 1f, 0.7f),
            Silhouette = BossSilhouette.AuroraSerpent,
            PatternPool = new[]
            {
                BossPattern.RangedSalvo, BossPattern.SweepLow,
                BossPattern.DiveHigh, BossPattern.CenterBeam,
            },
            DefeatCodexLabel = "击败极光长蛇",
        };

        public static readonly BossDefinition MistGuardian = new()
        {
            BossId = "mist_guardian",
            DisplayName = "雾堤守卫",
            BodyColor = new Color(0.48f, 0.55f, 0.62f),
            TrimColor = new Color(0.82f, 0.92f, 0.98f),
            GlowColor = new Color(0.7f, 0.85f, 0.95f, 0.7f),
            Silhouette = BossSilhouette.MistGuardian,
            PatternPool = new[]
            {
                BossPattern.DiveHigh, BossPattern.SweepLow,
                BossPattern.RangedSalvo, BossPattern.QuakePulse,
            },
            DefeatCodexLabel = "击败雾堤守卫",
        };

        public static readonly BossDefinition CoralKraken = new()
        {
            BossId = "coral_kraken",
            DisplayName = "珊瑚海怪",
            BodyColor = new Color(0.85f, 0.32f, 0.45f),
            TrimColor = new Color(1f, 0.85f, 0.4f),
            GlowColor = new Color(0.3f, 0.95f, 0.95f, 0.7f),
            Silhouette = BossSilhouette.CoralKraken,
            PatternPool = new[]
            {
                BossPattern.RangedSalvo, BossPattern.ChargeAcross,
                BossPattern.SweepLow, BossPattern.CenterBeam,
            },
            DefeatCodexLabel = "击败珊瑚海怪",
        };

        public static readonly BossDefinition StormEagle = new()
        {
            BossId = "storm_eagle",
            DisplayName = "雷云苍鹰",
            BodyColor = new Color(0.32f, 0.4f, 0.55f),
            TrimColor = new Color(1f, 0.9f, 0.35f),
            GlowColor = new Color(1f, 0.95f, 0.4f, 0.8f),
            Silhouette = BossSilhouette.StormEagle,
            PatternPool = new[]
            {
                BossPattern.DiveHigh, BossPattern.ChargeAcross,
                BossPattern.RangedSalvo, BossPattern.CenterBeam, BossPattern.QuakePulse,
            },
            DefeatCodexLabel = "击败雷云苍鹰",
        };
    }
}
