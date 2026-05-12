using System.Collections.Generic;
using System.Linq;
using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Boss 特色定位：决定其主机制风格
    /// </summary>
    internal enum BossArchetype
    {
        Balanced,      // 均衡型：冰霜雪王
        Grounded,      // 地面型：雪松哨兵，强调滑铲
        Evasive,       // 机动型：极光长蛇，强调换道
        Deceptive,     // 诡诈型：雾堤守卫，真假预警
        Defensive,     // 防御型：珊瑚海怪，护盾互动
        Aerial,        // 空战型：雷云苍鹰，强调跳跃/滑翔
    }

    /// <summary>
    /// 单只 boss 的静态数据：视觉、配色、攻击模式池、特色机制等。
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

        // ── Boss 身份与机制 ──────────────────────────────────
        public BossArchetype Archetype;
        /// <summary>主机制描述（用于教学提示）</summary>
        public string SignatureMechanic;
        /// <summary>推荐反制动作描述</summary>
        public string RecommendedCounter;
        /// <summary>专属奖励：击败后额外掉落</summary>
        public string SignatureReward;
        /// <summary>阶段曲线：Easy=慢节奏，Hard=快节奏</summary>
        public float PhaseIntensity;
        /// <summary>特色道具权重提升（在 SegmentSpawner 中使用）</summary>
        public PowerUpKind[] PreferredPowerUps;

        // ── 专属 BGM ─────────────────────────────────────────
        /// <summary>
        /// 在 Resources/PenguinRun/ 下的音频文件名（不含扩展名）。
        /// 若为空则回退到 RunnerAudio 内部的 switch 映射。
        /// </summary>
        public string BgmClipName;

        // ── 攻击动画强化参数 ──────────────────────────────────
        /// <summary>进入攻击前摇时的身体前倾角（度），越大越有压迫感。</summary>
        public float AttackWindupLean = 12f;
        /// <summary>击发瞬间的身体冲刺幅度（0-1）。</summary>
        public float AttackStrikePunch = 1.0f;
        /// <summary>破绽期的颤抖频率乘数（相对于默认抖动）。</summary>
        public float VulnerableShakeFreq = 1.0f;
        /// <summary>是否在受击时触发全身闪烁（false=仅头部）。</summary>
        public bool FullBodyHitFlash = false;
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
        private static BossDefinition[] AllBosses => new[]
        {
            SnowKing,
            CedarSentinel,
            AuroraSerpent,
            MistGuardian,
            CoralKraken,
            StormEagle,
        };

        /// <summary>Boss 刷新条件上下文</summary>
        internal sealed class BossSpawnContext
        {
            public RunnerMapTheme MapTheme;
            public DifficultyKind Difficulty;
            public bool IsDaily;
            public float RunDistance;
            public int BossesDefeated;
            public HashSet<string> DefeatedBossIds;
            public string ActivityTag;
        }

        /// <summary>Boss 刷新条件配置</summary>
        internal sealed class BossSpawnCondition
        {
            public RunnerMapTheme[] AllowedThemes;
            public float MinDistance;
            public DifficultyKind? MinDifficulty;
            public string[] RequiresAnyBossDefeated; // 需要击败任意一个指定 Boss
            public int? MinTotalBossesDefeated;
            public bool? OnlyInDaily;
            public bool IsTutorialBoss; // 教学 Boss，无视其他条件
        }

        private static readonly Dictionary<string, BossSpawnCondition> spawnConditions = new()
        {
            ["snow_king"] = new BossSpawnCondition
            {
                AllowedThemes = new[] { RunnerMapTheme.IceLakeEcho, RunnerMapTheme.CedarRuins, RunnerMapTheme.AuroraField, RunnerMapTheme.MistDike },
                MinDistance = 0f,
                IsTutorialBoss = true, // 首个教学 Boss，无条件出现
            },
            ["cedar_sentinel"] = new BossSpawnCondition
            {
                AllowedThemes = new[] { RunnerMapTheme.CedarRuins },
                MinDistance = 400f,
                MinTotalBossesDefeated = 0, // 任意击败一只后即可遇到
            },
            ["aurora_serpent"] = new BossSpawnCondition
            {
                AllowedThemes = new[] { RunnerMapTheme.AuroraField },
                MinDistance = 600f,
                MinTotalBossesDefeated = 1,
            },
            ["mist_guardian"] = new BossSpawnCondition
            {
                AllowedThemes = new[] { RunnerMapTheme.MistDike },
                MinDistance = 800f,
                RequiresAnyBossDefeated = new[] { "cedar_sentinel" },
                MinDifficulty = DifficultyKind.Normal,
            },
            ["coral_kraken"] = new BossSpawnCondition
            {
                AllowedThemes = new[] { RunnerMapTheme.OceanReef },
                MinDistance = 500f,
                MinTotalBossesDefeated = 1,
            },
            ["storm_eagle"] = new BossSpawnCondition
            {
                AllowedThemes = new[] { RunnerMapTheme.SkyFlight },
                MinDistance = 1000f,
                MinTotalBossesDefeated = 3,
                MinDifficulty = DifficultyKind.Hard,
            },
        };

        /// <summary>根据主题和条件选择合适的 Boss</summary>
        public static BossDefinition PickFor(RunnerMapTheme theme, int defeated)
        {
            // 基础轮换逻辑（保留原有功能）
            var pool = theme switch
            {
                RunnerMapTheme.IceLakeEcho => new[] { SnowKing, CedarSentinel, MistGuardian, AuroraSerpent, CoralKraken, StormEagle },
                RunnerMapTheme.CedarRuins => new[] { CedarSentinel, SnowKing, MistGuardian, StormEagle, CoralKraken, AuroraSerpent },
                RunnerMapTheme.AuroraField => new[] { AuroraSerpent, StormEagle, SnowKing, MistGuardian, CedarSentinel, CoralKraken },
                RunnerMapTheme.MistDike => new[] { MistGuardian, SnowKing, CedarSentinel, AuroraSerpent, CoralKraken, StormEagle },
                RunnerMapTheme.OceanReef => new[] { CoralKraken, MistGuardian, SnowKing, CedarSentinel, AuroraSerpent, StormEagle },
                RunnerMapTheme.SkyFlight => new[] { StormEagle, AuroraSerpent, SnowKing, MistGuardian, CedarSentinel, CoralKraken },
                _ => AllBosses,
            };

            if (pool == null || pool.Length == 0)
            {
                return SnowKing;
            }

            var index = Mathf.Abs(defeated) % pool.Length;
            return pool[index];
        }

        /// <summary>根据完整上下文条件筛选 Boss</summary>
        public static BossDefinition PickFor(BossSpawnContext ctx)
        {
            // 优先尝试符合条件的主题专属 Boss
            var eligible = new List<BossDefinition>();

            foreach (var boss in AllBosses)
            {
                if (!spawnConditions.TryGetValue(boss.BossId, out var cond))
                    continue;

                // 检查是否是教学 Boss（无条件通过）
                if (cond.IsTutorialBoss)
                {
                    if (ctx.BossesDefeated == 0 || ctx.DefeatedBossIds?.Count == 0)
                    {
                        eligible.Add(boss);
                        continue;
                    }
                }

                // 检查主题匹配
                if (cond.AllowedThemes != null && !cond.AllowedThemes.Contains(ctx.MapTheme))
                    continue;

                // 检查距离条件
                if (ctx.RunDistance < cond.MinDistance)
                    continue;

                // 检查难度条件
                if (cond.MinDifficulty.HasValue && ctx.Difficulty < cond.MinDifficulty.Value)
                    continue;

                // 检查前置 Boss 击败条件
                if (cond.RequiresAnyBossDefeated != null && ctx.DefeatedBossIds != null)
                {
                    var hasRequired = false;
                    foreach (var reqId in cond.RequiresAnyBossDefeated)
                    {
                        if (ctx.DefeatedBossIds.Contains(reqId))
                        {
                            hasRequired = true;
                            break;
                        }
                    }
                    if (!hasRequired)
                        continue;
                }

                // 检查总击败数条件
                if (cond.MinTotalBossesDefeated.HasValue && ctx.BossesDefeated < cond.MinTotalBossesDefeated.Value)
                    continue;

                // 检查每日模式条件
                if (cond.OnlyInDaily.HasValue && cond.OnlyInDaily.Value != ctx.IsDaily)
                    continue;

                eligible.Add(boss);
            }

            // 如果没有符合条件的，返回教学 Boss 或默认
            if (eligible.Count == 0)
            {
                return SnowKing;
            }

            // 按击败次数轮换
            var index = Mathf.Abs(ctx.BossesDefeated) % eligible.Count;
            return eligible[index];
        }

        /// <summary>检查指定 Boss 是否会在当前场景出现（用于 HUD 预告）</summary>
        public static bool WillSpawnInTheme(string bossId, RunnerMapTheme theme)
        {
            if (!spawnConditions.TryGetValue(bossId, out var cond))
                return false;

            return cond.AllowedThemes?.Contains(theme) ?? false;
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
            },
            DefeatCodexLabel = "击败冰霜雪王",
            Archetype = BossArchetype.Balanced,
            SignatureMechanic = "慢节奏重击，破绽期较长",
            RecommendedCounter = "观察预警，把握跳跃时机，破绽出现时冲刺反击",
            SignatureReward = "雪王之冠（图鉴）",
            PhaseIntensity = 0.8f,
            PreferredPowerUps = new[] { PowerUpKind.Dash, PowerUpKind.Shield },
            BgmClipName = "bgm_boss_snow_king",
            AttackWindupLean = 14f,
            AttackStrikePunch = 1.15f,
            VulnerableShakeFreq = 0.85f,
            FullBodyHitFlash = true,
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
            Archetype = BossArchetype.Grounded,
            SignatureMechanic = "路障/木桩主题，地面攻击为主",
            RecommendedCounter = "多用滑铲，注意选择安全车道",
            SignatureReward = "哨兵之角（图鉴）",
            PhaseIntensity = 0.9f,
            PreferredPowerUps = new[] { PowerUpKind.GlideFeather, PowerUpKind.Shield, PowerUpKind.TreantArmor },
            BgmClipName = "bgm_boss_cedar_sentinel",
            AttackWindupLean = 18f,
            AttackStrikePunch = 1.22f,
            VulnerableShakeFreq = 0.7f,
            FullBodyHitFlash = false,
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
                BossPattern.RangedSalvo, BossPattern.DiveHigh,
                BossPattern.CenterBeam, BossPattern.SweepLow,
            },
            DefeatCodexLabel = "击败极光长蛇",
            Archetype = BossArchetype.Evasive,
            SignatureMechanic = "招式预警短但有清晰颜色轨迹",
            RecommendedCounter = "善用磁吸道具有规划的路线，快速换道",
            SignatureReward = "极光鳞片（图鉴）",
            PhaseIntensity = 1.1f,
            PreferredPowerUps = new[] { PowerUpKind.Magnet, PowerUpKind.Dash, PowerUpKind.AuroraChain },
            BgmClipName = "bgm_boss_aurora_serpent",
            AttackWindupLean = 10f,
            AttackStrikePunch = 0.88f,
            VulnerableShakeFreq = 1.5f,
            FullBodyHitFlash = true,
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
            Archetype = BossArchetype.Deceptive,
            SignatureMechanic = "真假预警，雾区遮挡视线",
            RecommendedCounter = "仔细观察 HUD 提示，相信预警圈而非视觉假象",
            SignatureReward = "雾核（图鉴）",
            PhaseIntensity = 1.0f,
            PreferredPowerUps = new[] { PowerUpKind.Shield, PowerUpKind.TimeHourglass, PowerUpKind.FogLantern },
            BgmClipName = "bgm_boss_mist_guardian",
            AttackWindupLean = 12f,
            AttackStrikePunch = 0.96f,
            VulnerableShakeFreq = 1.1f,
            FullBodyHitFlash = false,
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
            Archetype = BossArchetype.Defensive,
            SignatureMechanic = "多点投射物，护盾可反弹造成伤害",
            RecommendedCounter = "拾取护盾道具，在破绽期用护盾撞击",
            SignatureReward = "珊瑚碎片（图鉴）",
            PhaseIntensity = 1.0f,
            PreferredPowerUps = new[] { PowerUpKind.Shield, PowerUpKind.BubbleShield, PowerUpKind.CoralBounce },
            BgmClipName = "bgm_boss_coral_kraken",
            AttackWindupLean = 10f,
            AttackStrikePunch = 1.08f,
            VulnerableShakeFreq = 1.2f,
            FullBodyHitFlash = true,
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
                BossPattern.RangedSalvo, BossPattern.CenterBeam,
            },
            DefeatCodexLabel = "击败雷云苍鹰",
            Archetype = BossArchetype.Aerial,
            SignatureMechanic = "高机动俯冲和全场冲锋，需要跳跃/滑翔应对",
            RecommendedCounter = "拾取滑翔羽毛，善用空中移动躲避空袭",
            SignatureReward = "苍鹰之羽（图鉴）",
            PhaseIntensity = 1.2f,
            PreferredPowerUps = new[] { PowerUpKind.GlideFeather, PowerUpKind.WindRider, PowerUpKind.ThunderFeather },
            BgmClipName = "bgm_boss_storm_eagle",
            AttackWindupLean = 8f,
            AttackStrikePunch = 1.18f,
            VulnerableShakeFreq = 1.8f,
            FullBodyHitFlash = true,
        };
    }
}
