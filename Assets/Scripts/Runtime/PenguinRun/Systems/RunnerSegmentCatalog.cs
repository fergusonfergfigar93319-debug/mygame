using UnityEngine;

namespace PenguinRun
{
    public enum SegmentObstaclePalette
    {
        IceLake,
        CedarWood,
        AuroraGlow,
        MistFog,
        OceanCoral,   // 海洋珊瑚礁
        SkyCloud,     // 天空云朵
    }

    public enum SegmentPatternKind
    {
        Mixed,
        LaneWeave,
        JumpArc,
        SlideTunnel,
        RewardRun,
        SplitGate,
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
    /// Theme-specific segment pools (6–8 logical waves each); waves advance inside <see cref="SegmentWaveDirector"/>.
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

        private static RunnerSegmentDefinition[] IceLakePool()
        {
            var p = SegmentObstaclePalette.IceLake;
            return new[]
            {
                new RunnerSegmentDefinition("回音热身", 2, -0.02f, 0.06f, -0.06f, 0.1f, 0f, 0f, p),
                new RunnerSegmentDefinition("宽道鱼干", 2, -0.03f, 0.14f, -0.12f, 0.06f, 0f, 0f, p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("单障练习", 3, 0f, -0.05f, 0.06f, 0.04f, 0f, 0.02f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("混合节奏", 3, 0.02f, -0.02f, 0.02f, 0f, 0.03f, 0.04f, p),
                new RunnerSegmentDefinition("喘息段", 2, -0.04f, 0.12f, -0.14f, 0.12f, 0f, 0f, p),
                new RunnerSegmentDefinition("双线压力", 2, 0.03f, -0.08f, 0.1f, -0.06f, 0f, 0.06f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("道具窗口", 2, 0.12f, 0f, -0.06f, 0f, 0f, 0f, p),
            };
        }

        private static RunnerSegmentDefinition[] CedarRuinsPool()
        {
            var p = SegmentObstaclePalette.CedarWood;
            return new[]
            {
                new RunnerSegmentDefinition("木桥入场", 2, -0.02f, 0.04f, -0.04f, 0.14f, 0.02f, 0f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("断裂预兆", 3, 0f, -0.06f, 0.08f, 0.08f, 0.06f, 0.08f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("滚雪球威胁", 2, 0.02f, -0.08f, 0.06f, -0.02f, 0.14f, 0.05f, p),
                new RunnerSegmentDefinition("窄线金币", 2, -0.04f, 0.18f, -0.1f, 0.06f, 0f, 0f, p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("箱体低矮", 3, 0f, 0.02f, 0.02f, 0.18f, 0f, 0.04f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("雪松加压", 2, 0.04f, -0.12f, 0.14f, -0.08f, 0.08f, 0.12f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("节拍喘息", 2, -0.06f, 0.1f, -0.12f, 0.16f, 0f, 0f, p),
            };
        }

        private static RunnerSegmentDefinition[] AuroraPool()
        {
            var p = SegmentObstaclePalette.AuroraGlow;
            return new[]
            {
                new RunnerSegmentDefinition("磁场热身", 2, 0.08f, 0.08f, -0.14f, 0.04f, 0f, 0f, p),
                new RunnerSegmentDefinition("金币流", 3, 0.02f, 0.2f, -0.18f, 0f, 0f, 0f, p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("光柱阶梯", 3, 0.04f, -0.04f, 0f, -0.02f, 0f, 0.05f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("加速缝隙", 2, 0.06f, -0.06f, 0.06f, -0.04f, 0.04f, 0.04f, p),
                new RunnerSegmentDefinition("磁针橱窗", 2, 0.14f, 0.02f, -0.12f, 0f, 0f, 0f, p),
                new RunnerSegmentDefinition("低压收集", 2, -0.02f, 0.16f, -0.2f, 0.06f, 0f, 0f, p),
                new RunnerSegmentDefinition("极光加压", 2, 0.05f, -0.1f, 0.12f, -0.06f, 0.05f, 0.06f, p, SegmentPatternKind.LaneWeave),
            };
        }

        private static RunnerSegmentDefinition[] MistPool()
        {
            var p = SegmentObstaclePalette.MistFog;
            return new[]
            {
                new RunnerSegmentDefinition("雾堤切入", 2, 0f, 0f, 0.02f, 0f, 0.06f, 0.05f, p),
                new RunnerSegmentDefinition("能见度低", 3, -0.02f, -0.04f, 0.12f, -0.02f, 0.1f, 0.08f, p, SegmentPatternKind.SplitGate),
                new RunnerSegmentDefinition("狭缝金币", 2, -0.04f, 0.12f, -0.02f, 0.08f, 0.08f, 0f, p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("压迫连续", 3, 0.05f, -0.14f, 0.16f, -0.1f, 0.12f, 0.1f, p, SegmentPatternKind.SlideTunnel),
                new RunnerSegmentDefinition("喘息雾气", 2, -0.06f, 0.08f, -0.14f, 0.12f, 0f, 0f, p),
                new RunnerSegmentDefinition("道具求生", 2, 0.1f, -0.06f, -0.02f, 0f, 0.05f, 0f, p),
                new RunnerSegmentDefinition("尾段加压", 2, 0.03f, -0.08f, 0.1f, -0.06f, 0.14f, 0.12f, p, SegmentPatternKind.LaneWeave),
            };
        }

        private static RunnerSegmentDefinition[] OceanReefPool()
        {
            var p = SegmentObstaclePalette.OceanCoral;
            return new[]
            {
                new RunnerSegmentDefinition("浅海热身", 2, 0.1f, 0.06f, -0.08f, 0.04f, 0f, 0f, p),
                new RunnerSegmentDefinition("珊瑚迷宫", 3, 0.02f, -0.04f, 0.08f, 0.02f, 0f, 0.1f, p, SegmentPatternKind.LaneWeave),
                new RunnerSegmentDefinition("鱼群金币流", 2, -0.02f, 0.22f, -0.16f, 0f, 0f, 0f, p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("海葵陷阱", 3, 0.08f, -0.08f, 0.1f, -0.04f, 0.06f, 0.06f, p),
                new RunnerSegmentDefinition("泡泡冲刺", 2, 0.14f, 0f, -0.1f, 0f, 0f, 0f, p),
                new RunnerSegmentDefinition("深海下降", 2, 0.04f, 0.08f, -0.06f, 0.14f, 0.02f, 0.02f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("暗流加压", 2, 0.06f, -0.1f, 0.12f, -0.08f, 0.1f, 0.08f, p, SegmentPatternKind.SplitGate),
            };
        }

        private static RunnerSegmentDefinition[] SkyFlightPool()
        {
            var p = SegmentObstaclePalette.SkyCloud;
            return new[]
            {
                new RunnerSegmentDefinition("云端热身", 2, 0.08f, 0.08f, -0.1f, 0.02f, 0f, 0f, p),
                new RunnerSegmentDefinition("浮空金币", 3, 0f, 0.24f, -0.18f, -0.04f, 0f, 0f, p, SegmentPatternKind.RewardRun),
                new RunnerSegmentDefinition("风柱跃升", 2, 0.12f, -0.06f, 0.04f, 0.18f, 0f, 0.02f, p, SegmentPatternKind.JumpArc),
                new RunnerSegmentDefinition("气流隧道", 3, 0.06f, -0.04f, 0.08f, 0.08f, 0.04f, 0.06f, p, SegmentPatternKind.SlideTunnel),
                new RunnerSegmentDefinition("滑翔收集", 2, -0.02f, 0.16f, -0.12f, 0.12f, 0f, 0f, p),
                new RunnerSegmentDefinition("羽衣橱窗", 2, 0.16f, 0.02f, -0.14f, 0f, 0f, 0f, p),
                new RunnerSegmentDefinition("高空加压", 2, 0.04f, -0.12f, 0.1f, -0.06f, 0.08f, 0.1f, p, SegmentPatternKind.LaneWeave),
            };
        }
    }
}
