using PenguinRun.Game.Mission;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Game.Growth
{
    public enum GrowthRecommendationKind
    {
        ClaimGoal,
        ReviewDiscovery,
        RebuildCamp,
        UpgradeCamp,
        StartJourney,
    }

    public readonly struct GrowthRecommendation
    {
        public GrowthRecommendation(GrowthRecommendationKind kind, string title, string body,
            string actionLabel, float progress01)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            ActionLabel = actionLabel ?? string.Empty;
            Progress01 = Mathf.Clamp01(progress01);
        }
        public GrowthRecommendationKind Kind { get; }
        public string Title { get; }
        public string Body { get; }
        public string ActionLabel { get; }
        public float Progress01 { get; }

        public static GrowthRecommendation Build()
        {
            var claimable = MissionStore.CountClaimableMissions();
            if (claimable > 0)
                return new(GrowthRecommendationKind.ClaimGoal, "营地来信",
                    $"有 {claimable} 份旅程奖励正在等你领取。", "领取目标奖励", 1f);

            var newDiscoveries = CodexDiscoveryStore.CountNew();
            if (newDiscoveries > 0)
                return new(GrowthRecommendationKind.ReviewDiscovery, "旅程册添了新页",
                    $"本次带回 {newDiscoveries} 条新路线或遗物记录。", "查看新发现",
                    CodexDiscoveryStore.CountDiscovered() / (float)CodexDiscoveryCatalog.All.Count);

            var camp = CampRebuildStore.Snapshot();
            if (camp.AffordableNodes > 0)
            {
                var next = CampRebuildStore.NextAffordableNode();
                return new(GrowthRecommendationKind.RebuildCamp, "让营火更亮一些",
                    next.HasValue ? $"{next.Value.Definition.Title}已经备齐修复材料。" : "营地有建筑可以修复。",
                    "重建营地", camp.Progress01);
            }

            var upgrades = CampUpgrades.CountAffordableUpgrades();
            if (upgrades > 0)
                return new(GrowthRecommendationKind.UpgradeCamp, "工坊有新的想法",
                    $"现有鱼干可以完成 {upgrades} 项能力强化。", "强化能力",
                    CampUpgrades.TotalLevelsCount() / (float)CampUpgrades.MaxTotalLevels);

            return new(GrowthRecommendationKind.StartJourney, "风雪后的第一束光",
                "带上伙伴，继续寻找散落在六片雪原里的故事残页。", "翻开新旅程",
                PlayerSave.PlayerLevelProgress);
        }
    }
}
