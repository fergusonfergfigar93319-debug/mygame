using UnityEngine;

namespace PenguinRun
{
    public enum SegmentObstaclePalette
    {
        IceLake,
        CedarWood,
        AuroraGlow,
        MistFog,
        OceanCoral,
        SkyCloud,
    }

    public enum SegmentPatternKind
    {
        Mixed,
        LaneWeave,
        JumpArc,
        SlideTunnel,
        RewardRun,
        SplitGate,
        HazardRhythm,   // 高密度连续危险段：考验节奏感
        EnemyAmbush,    // 移动敌人集中出现段
        PowerUpTrial,   // 道具密集段：激励玩家拾取并展示效果
    }

    public readonly struct RunnerSegmentDefinition
    {
        public readonly string DebugName;
        public readonly int WaveCount;
        public readonly float PowerChanceDelta;
        public readonly float CoinLineChanceDelta;
        public readonly float SingleObstacleChanceDelta;
        public readonly float LowObstacleBiasDelta;
        public readonly float RollingSpawnChance;
        public readonly float WideWallChance;
        public readonly SegmentObstaclePalette Palette;
        public readonly SegmentPatternKind PatternKind;

        public RunnerSegmentDefinition(
            string debugName,
            int waveCount,
            float powerChanceDelta,
            float coinLineChanceDelta,
            float singleObstacleChanceDelta,
            float lowObstacleBiasDelta,
            float rollingSpawnChance,
            float wideWallChance,
            SegmentObstaclePalette palette,
            SegmentPatternKind patternKind = SegmentPatternKind.Mixed)
        {
            DebugName = debugName;
            WaveCount = Mathf.Max(1, waveCount);
            PowerChanceDelta = powerChanceDelta;
            CoinLineChanceDelta = coinLineChanceDelta;
            SingleObstacleChanceDelta = singleObstacleChanceDelta;
            LowObstacleBiasDelta = lowObstacleBiasDelta;
            RollingSpawnChance = rollingSpawnChance;
            WideWallChance = wideWallChance;
            Palette = palette;
            PatternKind = patternKind;
        }
    }

    public readonly struct SegmentSpawnModifiers
    {
        public readonly float PowerChanceDelta;
        public readonly float CoinLineChanceDelta;
        public readonly float SingleObstacleChanceDelta;
        public readonly float LowObstacleBiasDelta;
        public readonly float RollingSpawnChance;
        public readonly float WideWallChance;
        public readonly SegmentObstaclePalette Palette;
        public readonly SegmentPatternKind PatternKind;

        public SegmentSpawnModifiers(
            float powerChanceDelta,
            float coinLineChanceDelta,
            float singleObstacleChanceDelta,
            float lowObstacleBiasDelta,
            float rollingSpawnChance,
            float wideWallChance,
            SegmentObstaclePalette palette,
            SegmentPatternKind patternKind)
        {
            PowerChanceDelta = powerChanceDelta;
            CoinLineChanceDelta = coinLineChanceDelta;
            SingleObstacleChanceDelta = singleObstacleChanceDelta;
            LowObstacleBiasDelta = lowObstacleBiasDelta;
            RollingSpawnChance = rollingSpawnChance;
            WideWallChance = wideWallChance;
            Palette = palette;
            PatternKind = patternKind;
        }

        public static SegmentSpawnModifiers FromDefinition(in RunnerSegmentDefinition def)
        {
            return new SegmentSpawnModifiers(
                def.PowerChanceDelta,
                def.CoinLineChanceDelta,
                def.SingleObstacleChanceDelta,
                def.LowObstacleBiasDelta,
                def.RollingSpawnChance,
                def.WideWallChance,
                def.Palette,
                def.PatternKind);
        }
    }

    /// <summary>
    /// 每个地图主题的片段池定义。每次 <see cref="SegmentWaveDirector"/> 结束一个片段时
    /// 从池中随机选取下一个，驱动障碍/敌人/道具的生成节拍。
    /// </summary>
    public static class RunnerSegmentCatalog
    {
        public static RunnerSegmentDefinition[] GetPool(RunnerMapTheme theme)
        {
            return theme switch
            {
                RunnerMapTheme.CedarRuins => CedarRuinsPool(),
                RunnerMapTheme.AuroraField => AuroraPool(),
                RunnerMapTheme.MistDike => MistPool(),
                RunnerMapTheme.OceanReef => OceanReefPool(),
                RunnerMapTheme.SkyFlight => SkyFlightPool(),
                _ => IceLakePool(),
            };
        }

        // ── 冰湖回音谷 ─────────────────────────────────────────
        private static RunnerSegmentDefinition[] IceLakePool()
        {
            var p = SegmentObstaclePalette.IceLake;
            return new[]
            {
                // 原有片段（名称修正）
                new RunnerSegmentDefinition("围石热身",       2, -0.02f,  0.06f, -0.06f,  0.10f, 0f,    0f,    p),
                new RunnerSegmentDefinition("宽道鱼干",       2, -0.03f,  0.14f, -0.12f,  0.06f, 0f,    0f,    p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("单障练习",       3,  0.00f, -0.05f,  0.06f,  0.04f, 0f,    0.02f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("混合节奏",       3,  0.02f, -0.02f,  0.02f,  0.00f, 0.03f, 0.04f, p),
                new RunnerSegmentDefinition("喘息平台",       2, -0.04f,  0.12f, -0.14f,  0.12f, 0f,    0f,    p),
                new RunnerSegmentDefinition("双线压力",       2,  0.03f, -0.08f,  0.10f, -0.06f, 0f,    0.06f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("道具窗口",       2,  0.12f,  0.00f, -0.06f,  0.00f, 0f,    0f,    p),
                new RunnerSegmentDefinition("crystal wake",  3,  0.02f, -0.06f,  0.10f, -0.02f, 0.08f, 0.08f, p, SegmentPatternKind.SplitGate),
                // 新增片段
                new RunnerSegmentDefinition("冰裂节奏",       3,  0.00f, -0.10f,  0.18f,  0.06f, 0.12f, 0.06f, p, SegmentPatternKind.HazardRhythm),
                new RunnerSegmentDefinition("影兽伏击",       2,  0.06f, -0.04f,  0.08f, -0.02f, 0.10f, 0.04f, p, SegmentPatternKind.EnemyAmbush),
                new RunnerSegmentDefinition("薄冰道具",       2,  0.18f,  0.04f, -0.10f,  0.02f, 0f,    0f,    p, SegmentPatternKind.PowerUpTrial),
                new RunnerSegmentDefinition("薄冰急转",       2,  0.02f, -0.06f,  0.08f,  0.04f, 0.06f, 0.04f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("冰洞滑铲",       3,  0.00f,  0.04f, -0.02f,  0.22f, 0f,    0.04f, p, SegmentPatternKind.SlideTunnel),
            };
        }

        // ── 雪松废墟 ──────────────────────────────────────────
        private static RunnerSegmentDefinition[] CedarRuinsPool()
        {
            var p = SegmentObstaclePalette.CedarWood;
            return new[]
            {
                // 原有片段（名称修正）
                new RunnerSegmentDefinition("木桥入场",       2, -0.02f,  0.04f, -0.04f,  0.14f, 0.02f, 0f,    p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("断桥预兆",       3,  0.00f, -0.06f,  0.08f,  0.08f, 0.06f, 0.08f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("rolling snowball", 2, 0.02f,-0.08f,  0.06f, -0.02f, 0.14f, 0.05f, p),
                new RunnerSegmentDefinition("箭线金币",       2, -0.04f,  0.18f, -0.10f,  0.06f, 0f,    0f,    p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("简体低障",       3,  0.00f,  0.02f,  0.02f,  0.18f, 0f,    0.04f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("落石加压",       2,  0.04f, -0.12f,  0.14f, -0.08f, 0.08f, 0.12f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("节拍喘息",       2, -0.06f,  0.10f, -0.12f,  0.16f, 0f,    0f,    p),
                new RunnerSegmentDefinition("fallen pines",   3,  0.02f, -0.10f,  0.16f,  0.12f, 0.10f, 0.10f, p, SegmentPatternKind.SlideTunnel),
                // 新增片段
                new RunnerSegmentDefinition("落木狂奔",       3,  0.00f, -0.12f,  0.20f,  0.04f, 0.16f, 0.10f, p, SegmentPatternKind.HazardRhythm),
                new RunnerSegmentDefinition("林兽伏击",       2,  0.06f, -0.04f,  0.08f,  0.00f, 0.12f, 0.04f, p, SegmentPatternKind.EnemyAmbush),
                new RunnerSegmentDefinition("木桥道具",       2,  0.18f,  0.06f, -0.12f,  0.08f, 0f,    0f,    p, SegmentPatternKind.PowerUpTrial),
                new RunnerSegmentDefinition("废墟回廊",       3,  0.00f,  0.06f, -0.02f,  0.20f, 0.04f, 0.06f, p, SegmentPatternKind.SlideTunnel),
                new RunnerSegmentDefinition("窄道急转",       2,  0.03f, -0.08f,  0.10f, -0.04f, 0.08f, 0.08f, p, SegmentPatternKind.LaneWeave),
            };
        }

        // ── 极光磁场 ──────────────────────────────────────────
        private static RunnerSegmentDefinition[] AuroraPool()
        {
            var p = SegmentObstaclePalette.AuroraGlow;
            return new[]
            {
                // 原有片段（名称修正）
                new RunnerSegmentDefinition("磁场热身",       2,  0.08f,  0.08f, -0.14f,  0.04f, 0f,    0f,    p),
                new RunnerSegmentDefinition("coin stream",    3,  0.02f,  0.20f, -0.18f,  0.00f, 0f,    0f,    p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("光柱阻挡",       3,  0.04f, -0.04f,  0.00f, -0.02f, 0f,    0.05f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("boost gap",      2,  0.06f, -0.06f,  0.06f, -0.04f, 0.04f, 0.04f, p),
                new RunnerSegmentDefinition("磁针窗口",       2,  0.14f,  0.02f, -0.12f,  0.00f, 0f,    0f,    p),
                new RunnerSegmentDefinition("低压收集",       2, -0.02f,  0.16f, -0.20f,  0.06f, 0f,    0f,    p),
                new RunnerSegmentDefinition("极光加压",       2,  0.05f, -0.10f,  0.12f, -0.06f, 0.05f, 0.06f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("prism blades",   3,  0.08f, -0.08f,  0.12f, -0.04f, 0.08f, 0.04f, p, SegmentPatternKind.JumpArc),
                // 新增片段
                new RunnerSegmentDefinition("棱镜刃阵",       3,  0.02f, -0.14f,  0.22f, -0.02f, 0.10f, 0.06f, p, SegmentPatternKind.HazardRhythm),
                new RunnerSegmentDefinition("磁场扰乱",       2,  0.08f, -0.02f,  0.06f,  0.00f, 0.12f, 0.04f, p, SegmentPatternKind.EnemyAmbush),
                new RunnerSegmentDefinition("极光道具",       2,  0.22f,  0.06f, -0.16f,  0.00f, 0f,    0f,    p, SegmentPatternKind.PowerUpTrial),
                new RunnerSegmentDefinition("极光涡旋",       3,  0.06f, -0.08f,  0.10f,  0.04f, 0.06f, 0.06f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("光束隧道",       2,  0.04f,  0.04f, -0.04f,  0.16f, 0f,    0.04f, p, SegmentPatternKind.SlideTunnel),
            };
        }

        // ── 北境雾堤 ──────────────────────────────────────────
        private static RunnerSegmentDefinition[] MistPool()
        {
            var p = SegmentObstaclePalette.MistFog;
            return new[]
            {
                // 原有片段（名称修正）
                new RunnerSegmentDefinition("霾障切入",       2,  0.00f,  0.00f,  0.02f,  0.00f, 0.06f, 0.05f, p),
                new RunnerSegmentDefinition("能见度低",       3, -0.02f, -0.04f,  0.12f, -0.02f, 0.10f, 0.08f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("独行金币",       2, -0.04f,  0.12f, -0.02f,  0.08f, 0.08f, 0f,    p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("压迫连续",       3,  0.05f, -0.14f,  0.16f, -0.10f, 0.12f, 0.10f, p, SegmentPatternKind.SlideTunnel),
                new RunnerSegmentDefinition("喘息霾气",       2, -0.06f,  0.08f, -0.14f,  0.12f, 0f,    0f,    p),
                new RunnerSegmentDefinition("道具求生",       2,  0.10f, -0.06f, -0.02f,  0.00f, 0.05f, 0f,    p),
                new RunnerSegmentDefinition("尖锋加压",       2,  0.03f, -0.08f,  0.10f, -0.06f, 0.14f, 0.12f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("fog ambush",     3,  0.04f, -0.10f,  0.14f, -0.06f, 0.16f, 0.08f, p, SegmentPatternKind.SplitGate),
                // 新增片段
                new RunnerSegmentDefinition("雾墙狂奔",       3,  0.00f, -0.12f,  0.20f,  0.00f, 0.18f, 0.08f, p, SegmentPatternKind.HazardRhythm),
                new RunnerSegmentDefinition("影兽伏击",       2,  0.06f, -0.04f,  0.10f,  0.00f, 0.14f, 0.04f, p, SegmentPatternKind.EnemyAmbush),
                new RunnerSegmentDefinition("雾中道具",       2,  0.20f,  0.04f, -0.14f,  0.02f, 0f,    0f,    p, SegmentPatternKind.PowerUpTrial),
                new RunnerSegmentDefinition("浓雾压迫",       3,  0.02f, -0.06f,  0.08f,  0.12f, 0.10f, 0.06f, p, SegmentPatternKind.SlideTunnel),
                new RunnerSegmentDefinition("假预警通道",     2,  0.04f, -0.08f,  0.12f, -0.04f, 0.08f, 0.10f, p, SegmentPatternKind.SplitGate),
            };
        }

        // ── 海洋珊瑚礁 ────────────────────────────────────────
        private static RunnerSegmentDefinition[] OceanReefPool()
        {
            var p = SegmentObstaclePalette.OceanCoral;
            return new[]
            {
                // 原有片段（名称修正）
                new RunnerSegmentDefinition("浅海热身",       2,  0.10f,  0.06f, -0.08f,  0.04f, 0f,    0f,    p),
                new RunnerSegmentDefinition("蟹礁迷宫",       3,  0.02f, -0.04f,  0.08f,  0.02f, 0f,    0.10f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("fish coin stream",2, -0.02f,  0.22f, -0.16f,  0.00f, 0f,    0f,    p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("海沟陷阱",       3,  0.08f, -0.08f,  0.10f, -0.04f, 0.06f, 0.06f, p),
                new RunnerSegmentDefinition("泡泡冲刺",       2,  0.14f,  0.00f, -0.10f,  0.00f, 0f,    0f,    p),
                new RunnerSegmentDefinition("深海下降",       2,  0.04f,  0.08f, -0.06f,  0.14f, 0.02f, 0.02f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("暗流加压",       2,  0.06f, -0.10f,  0.12f, -0.08f, 0.10f, 0.08f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("anemone teeth",  3,  0.10f, -0.08f,  0.16f,  0.02f, 0.08f, 0.12f, p, SegmentPatternKind.LaneWeave),
                // 新增片段
                new RunnerSegmentDefinition("珊瑚刺阵",       3,  0.02f, -0.14f,  0.22f,  0.04f, 0.08f, 0.08f, p, SegmentPatternKind.HazardRhythm),
                new RunnerSegmentDefinition("鱼群敌袭",       2,  0.10f, -0.04f,  0.06f,  0.00f, 0.12f, 0.04f, p, SegmentPatternKind.EnemyAmbush),
                new RunnerSegmentDefinition("泡泡道具",       2,  0.24f,  0.04f, -0.14f,  0.00f, 0f,    0f,    p, SegmentPatternKind.PowerUpTrial),
                new RunnerSegmentDefinition("暗流涌动",       3,  0.06f, -0.06f,  0.10f, -0.02f, 0.10f, 0.08f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("礁穴滑铲",       2,  0.02f,  0.06f, -0.04f,  0.24f, 0f,    0.04f, p, SegmentPatternKind.SlideTunnel),
            };
        }

        // ── 天空飞翔 ──────────────────────────────────────────
        private static RunnerSegmentDefinition[] SkyFlightPool()
        {
            var p = SegmentObstaclePalette.SkyCloud;
            return new[]
            {
                // 原有片段（名称修正）
                new RunnerSegmentDefinition("云端热身",       2,  0.08f,  0.08f, -0.10f,  0.02f, 0f,    0f,    p),
                new RunnerSegmentDefinition("浮空金币",       3,  0.00f,  0.24f, -0.18f, -0.04f, 0f,    0f,    p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("风柱跳升",       2,  0.12f, -0.06f,  0.04f,  0.18f, 0f,    0.02f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("气流隧道",       3,  0.06f, -0.04f,  0.08f,  0.08f, 0.04f, 0.06f, p, SegmentPatternKind.SlideTunnel),
                new RunnerSegmentDefinition("滑返收集",       2, -0.02f,  0.16f, -0.12f,  0.12f, 0f,    0f,    p),
                new RunnerSegmentDefinition("翎衣窗口",       2,  0.16f,  0.02f, -0.14f,  0.00f, 0f,    0f,    p),
                new RunnerSegmentDefinition("高空加压",       2,  0.04f, -0.12f,  0.10f, -0.06f, 0.08f, 0.10f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("storm pinwheel", 3,  0.10f, -0.10f,  0.14f,  0.06f, 0.10f, 0.08f, p, SegmentPatternKind.SlideTunnel),
                // 新增片段
                new RunnerSegmentDefinition("雷云轰炸",       3,  0.02f, -0.16f,  0.24f, -0.02f, 0.12f, 0.08f, p, SegmentPatternKind.HazardRhythm),
                new RunnerSegmentDefinition("鹰击长空",       2,  0.10f, -0.04f,  0.08f,  0.00f, 0.10f, 0.04f, p, SegmentPatternKind.EnemyAmbush),
                new RunnerSegmentDefinition("飞羽道具",       2,  0.22f,  0.06f, -0.16f,  0.00f, 0f,    0f,    p, SegmentPatternKind.PowerUpTrial),
                new RunnerSegmentDefinition("风暴涡旋",       3,  0.06f, -0.08f,  0.12f,  0.06f, 0.08f, 0.08f, p, SegmentPatternKind.SlideTunnel),
                new RunnerSegmentDefinition("云门连跳",       2,  0.08f, -0.06f,  0.08f,  0.14f, 0.04f, 0.06f, p, SegmentPatternKind.JumpArc),
            };
        }
    }
}
