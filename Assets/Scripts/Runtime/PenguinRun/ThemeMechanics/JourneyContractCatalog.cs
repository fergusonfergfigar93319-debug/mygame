using System;

namespace PenguinRun
{
    internal static class JourneyContractCatalog
    {
        private static readonly JourneyContractDefinition[] All =
        {
            new(JourneyContractKind.PerfectStreak, "连续回响", "连续完成 3 次完美环境应对", 3),
            new(JourneyContractKind.FlawlessEvents, "无伤穿越", "无伤完成 2 个主题事件", 2),
            new(JourneyContractKind.ResonanceFish, "丰收回响", "一次回响状态中收集 20 条鱼", 20),
            new(JourneyContractKind.SignatureComplete, "地标征服", "完成一个标志性主题事件", 1),
            new(JourneyContractKind.SkillBeforeDistance, "技巧冲刺", "800 米前取得 1200 技巧分", 1200),
        };

        public static ReadOnlySpan<JourneyContractDefinition> Definitions => All;

        public static JourneyContractDefinition Select(ThemeMechanicMode mode, IRunRandom random,
            bool allowSkillBeforeDistance = true)
        {
            var allowSignature = ThemeMechanicCatalog.PolicyFor(mode).AllowSignature;
            var eligibleCount = All.Length - (allowSignature ? 0 : 1) - (allowSkillBeforeDistance ? 0 : 1);
            var selected = random.Range(0, eligibleCount);
            for (var index = 0; index < All.Length; index++)
            {
                if ((!allowSignature && All[index].Kind == JourneyContractKind.SignatureComplete)
                    || (!allowSkillBeforeDistance && All[index].Kind == JourneyContractKind.SkillBeforeDistance))
                {
                    continue;
                }

                if (selected-- == 0)
                {
                    return All[index];
                }
            }

            return All[0];
        }
    }
}
