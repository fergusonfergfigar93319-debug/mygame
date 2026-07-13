using System.Collections.Generic;

namespace PenguinRun
{
    internal static class RouteEventCatalog
    {
        private static readonly RouteEventDefinition[] IceLakeEcho =
        {
            Event("ice-fracture", RouteEventKind.RiskFork, "冰镜分岔", "沿岸稳行", "穿越裂冰",
                Safe("稳稳收网", 140, 4, RouteSegmentPattern.CalmLane),
                Risk("踏冰寻光", 360, 8, RouteSegmentPattern.JumpArc)),
            Event("ice-cache", RouteEventKind.RelicCache, "冰封储藏", "拾取散鱼", "敲开晶核",
                Safe("散鱼入袋", 120, 5, RouteSegmentPattern.FishRibbon),
                Risk("晶核回响", 320, 9, RouteSegmentPattern.Slalom)),
        };

        private static readonly RouteEventDefinition[] CedarRuins =
        {
            Event("cedar-rescue", RouteEventKind.CompanionRescue, "雪松呼救", "清理近路", "深入根廊",
                Safe("护送归队", 160, 4, RouteSegmentPattern.CalmLane),
                Risk("救出伙伴", 420, 7, RouteSegmentPattern.SlideTunnel)),
            Event("cedar-exit", RouteEventKind.SafeExit, "遗迹岔口", "走石灯道", "翻越断墙",
                Safe("灯火相随", 150, 3, RouteSegmentPattern.CalmLane),
                Risk("断墙捷径", 350, 8, RouteSegmentPattern.JumpArc)),
        };

        private static readonly RouteEventDefinition[] AuroraField =
        {
            Event("aurora-trial", RouteEventKind.WeatherTrial, "极光磁暴", "顺着蓝极", "逆行赤极",
                Safe("磁流护航", 170, 4, RouteSegmentPattern.FishRibbon),
                Risk("逆极冲刺", 440, 8, RouteSegmentPattern.Slalom)),
            Event("aurora-fork", RouteEventKind.RiskFork, "光带交叉", "跟随柔光", "追逐强光",
                Safe("柔光引路", 150, 5, RouteSegmentPattern.CalmLane),
                Risk("强光连跃", 390, 7, RouteSegmentPattern.JumpArc)),
        };

        private static readonly RouteEventDefinition[] MistDike =
        {
            Event("mist-chase", RouteEventKind.BossChase, "雾中追影", "守住堤灯", "追入浓雾",
                Safe("堤灯未灭", 180, 4, RouteSegmentPattern.SlideTunnel),
                Risk("识破幻影", 480, 8, RouteSegmentPattern.ChaseLine)),
            Event("mist-rescue", RouteEventKind.CompanionRescue, "迷雾铃声", "沿绳索走", "循铃寻友",
                Safe("绳索归途", 140, 4, RouteSegmentPattern.CalmLane),
                Risk("铃声相会", 410, 8, RouteSegmentPattern.Slalom)),
        };

        private static readonly RouteEventDefinition[] OceanReef =
        {
            Event("reef-cache", RouteEventKind.RelicCache, "珊瑚宝湾", "捡拾贝壳", "潜入回流",
                Safe("贝壳满袋", 150, 6, RouteSegmentPattern.FishRibbon),
                Risk("回流宝藏", 430, 10, RouteSegmentPattern.Slalom)),
            Event("reef-trial", RouteEventKind.WeatherTrial, "潮汐试炼", "借顺潮", "踏逆浪",
                Safe("顺潮而行", 170, 4, RouteSegmentPattern.CalmLane),
                Risk("逆浪连跳", 450, 8, RouteSegmentPattern.JumpArc)),
        };

        private static readonly RouteEventDefinition[] SkyFlight =
        {
            Event("sky-chase", RouteEventKind.BossChase, "风暴追逐", "贴云滑行", "乘风追鹰",
                Safe("云层掩护", 180, 4, RouteSegmentPattern.SlideTunnel),
                Risk("追上风眼", 500, 8, RouteSegmentPattern.ChaseLine)),
            Event("sky-exit", RouteEventKind.SafeExit, "浮岛出口", "降落缓坡", "跃过断桥",
                Safe("平稳着陆", 150, 4, RouteSegmentPattern.CalmLane),
                Risk("飞越断桥", 400, 8, RouteSegmentPattern.JumpArc)),
        };

        public static IReadOnlyList<RouteEventDefinition> For(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.IceLakeEcho => IceLakeEcho,
            RunnerMapTheme.CedarRuins => CedarRuins,
            RunnerMapTheme.AuroraField => AuroraField,
            RunnerMapTheme.MistDike => MistDike,
            RunnerMapTheme.OceanReef => OceanReef,
            RunnerMapTheme.SkyFlight => SkyFlight,
            _ => IceLakeEcho,
        };

        private static RouteEventDefinition Event(
            string id,
            RouteEventKind kind,
            string title,
            string safeLabel,
            string riskLabel,
            RouteEventOutcome safeOutcome,
            RouteEventOutcome riskOutcome) =>
            new(id, kind, title, safeLabel, riskLabel, safeOutcome, riskOutcome);

        private static RouteEventOutcome Safe(
            string label,
            int score,
            int fish,
            RouteSegmentPattern pattern) => new(label, score, fish, pattern);

        private static RouteEventOutcome Risk(
            string label,
            int score,
            int fish,
            RouteSegmentPattern pattern) => new(label, score, fish, pattern);
    }
}
