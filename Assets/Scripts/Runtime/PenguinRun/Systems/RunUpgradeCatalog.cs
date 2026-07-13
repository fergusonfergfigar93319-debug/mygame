using System.Collections.Generic;

namespace PenguinRun
{
    internal static class RunUpgradeCatalog
    {
        private static readonly RunUpgradeDefinition[] Definitions =
        {
            Upgrade(RunUpgradeId.LongDash, RunUpgradeCategory.Movement, "长尾冲刺", "冲刺持续时间 +15%", RunUpgradeEffect.DashDuration, 0.15f),
            Upgrade(RunUpgradeId.QuickSteps, RunUpgradeCategory.Movement, "轻快脚步", "换道响应 +12%", RunUpgradeEffect.LaneResponse, 0.12f),
            Upgrade(RunUpgradeId.FeatherJump, RunUpgradeCategory.Movement, "羽毛跃步", "空中操控 +10%", RunUpgradeEffect.AirControl, 0.1f),

            Upgrade(RunUpgradeId.WideMagnet, RunUpgradeCategory.Collection, "宽幅磁场", "磁吸范围 +18%", RunUpgradeEffect.MagnetRadius, 0.18f),
            Upgrade(RunUpgradeId.FishSatchel, RunUpgradeCategory.Collection, "鱼干挎包", "路线鱼干奖励 +12%", RunUpgradeEffect.FishValue, 0.12f),
            Upgrade(RunUpgradeId.LastingSupplies, RunUpgradeCategory.Collection, "耐用补给", "道具持续时间 +12%", RunUpgradeEffect.PowerUpDuration, 0.12f),

            Upgrade(RunUpgradeId.ShieldPadding, RunUpgradeCategory.Defense, "软垫护盾", "护盾破裂后宽限 +0.18秒", RunUpgradeEffect.ShieldGrace, 0.18f),
            Upgrade(RunUpgradeId.IceGuard, RunUpgradeCategory.Defense, "冰纹护甲", "受击无敌 +0.12秒", RunUpgradeEffect.HitInvulnerability, 0.12f),

            Upgrade(RunUpgradeId.RescueWhistle, RunUpgradeCategory.Companion, "伙伴口哨", "伙伴事件奖励 +15%", RunUpgradeEffect.CompanionFind, 0.15f),
            Upgrade(RunUpgradeId.DodgeCheers, RunUpgradeCategory.Companion, "闪避喝彩", "完美闪避分数 +20%", RunUpgradeEffect.PerfectDodgeScore, 0.2f),
            Upgrade(RunUpgradeId.TrailScout, RunUpgradeCategory.Companion, "前路侦察", "路线分数奖励 +15%", RunUpgradeEffect.RouteReward, 0.15f),
            Upgrade(RunUpgradeId.BossSignal, RunUpgradeCategory.Companion, "反击信号", "Boss反击分数 +20%", RunUpgradeEffect.BossCounterScore, 0.2f),
        };

        public static IReadOnlyList<RunUpgradeDefinition> All => Definitions;

        public static RunUpgradeDefinition For(RunUpgradeId id)
        {
            for (var i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].Id == id)
                {
                    return Definitions[i];
                }
            }

            return default;
        }

        private static RunUpgradeDefinition Upgrade(
            RunUpgradeId id,
            RunUpgradeCategory category,
            string title,
            string description,
            RunUpgradeEffect effect,
            float amount) => new(id, category, title, description, effect, amount);
    }
}
