using System;
using System.Collections.Generic;
using PenguinRun.Game.Mission;
using PenguinRun.Game.Save;

namespace PenguinRun.Game.Growth
{
    public readonly struct GrowthTransactionInput
    {
        public GrowthTransactionInput(int score, int distanceMeters, int coins, float survivalSeconds,
            bool dailyMode, int routeEventsCompleted, int riskyRoutesCompleted, int skillScore,
            IReadOnlyList<string> completedRouteEventIds, string themedRelicId)
        {
            Score = Math.Max(0, score);
            DistanceMeters = Math.Max(0, distanceMeters);
            Coins = Math.Max(0, coins);
            SurvivalSeconds = Math.Max(0f, survivalSeconds);
            DailyMode = dailyMode;
            RouteEventsCompleted = Math.Max(0, routeEventsCompleted);
            RiskyRoutesCompleted = Math.Max(0, riskyRoutesCompleted);
            SkillScore = Math.Max(0, skillScore);
            CompletedRouteEventIds = completedRouteEventIds ?? Array.AsReadOnly(Array.Empty<string>());
            ThemedRelicId = themedRelicId ?? string.Empty;
        }
        public int Score { get; }
        public int DistanceMeters { get; }
        public int Coins { get; }
        public float SurvivalSeconds { get; }
        public bool DailyMode { get; }
        public int RouteEventsCompleted { get; }
        public int RiskyRoutesCompleted { get; }
        public int SkillScore { get; }
        public IReadOnlyList<string> CompletedRouteEventIds { get; }
        public string ThemedRelicId { get; }
    }

    public readonly struct GrowthTransactionResult
    {
        public GrowthTransactionResult(CompanionBondAward bondAward, IReadOnlyList<string> newDiscoveryIds,
            int newlyClaimableGoals, string strongestSummary)
        {
            BondAward = bondAward;
            NewDiscoveryIds = newDiscoveryIds ?? Array.AsReadOnly(Array.Empty<string>());
            NewlyClaimableGoals = Math.Max(0, newlyClaimableGoals);
            StrongestSummary = strongestSummary ?? string.Empty;
        }
        public CompanionBondAward BondAward { get; }
        public IReadOnlyList<string> NewDiscoveryIds { get; }
        public int NewlyClaimableGoals { get; }
        public string StrongestSummary { get; }
        public bool HasProgress => BondAward.TotalXp > 0 || NewDiscoveryIds.Count > 0 || NewlyClaimableGoals > 0;
        public static GrowthTransactionResult Empty => new(default,
            Array.AsReadOnly(Array.Empty<string>()), 0, string.Empty);
    }

    public static class GrowthTransaction
    {
        public static GrowthTransactionResult Apply(GrowthTransactionInput input)
        {
            var claimableBefore = MissionStore.CountClaimableMissions(false);
            MissionStore.RecordAfterRun(input.Score, input.DistanceMeters, input.Coins, input.DailyMode);
            CodexUnlocks.ApplyAfterRun(input.Score, input.DistanceMeters, input.SurvivalSeconds, input.DailyMode);

            var newDiscoveries = new List<string>();
            foreach (var eventId in input.CompletedRouteEventIds)
                if (CodexDiscoveryStore.Discover(eventId)) newDiscoveries.Add(eventId);
            if (!string.IsNullOrEmpty(input.ThemedRelicId) && CodexDiscoveryStore.Discover(input.ThemedRelicId))
                newDiscoveries.Add(input.ThemedRelicId);

            var bondAward = CompanionBondStore.AwardAfterRun(input.RouteEventsCompleted,
                input.RiskyRoutesCompleted, input.SkillScore, input.DailyMode,
                !string.IsNullOrEmpty(input.ThemedRelicId));
            var newlyClaimable = Math.Max(0, MissionStore.CountClaimableMissions(false) - claimableBefore);
            var immutableDiscoveries = Array.AsReadOnly(newDiscoveries.ToArray());
            return new GrowthTransactionResult(bondAward, immutableDiscoveries, newlyClaimable,
                BuildStrongestSummary(bondAward, immutableDiscoveries, newlyClaimable));
        }

        private static string BuildStrongestSummary(CompanionBondAward bondAward,
            IReadOnlyList<string> discoveries, int newlyClaimable)
        {
            if (discoveries.Count > 0)
            {
                var first = CodexDiscoveryCatalog.For(discoveries[0]);
                return discoveries.Count > 1
                    ? $"图鉴新增 {discoveries.Count} 页 · {first?.Title} 等"
                    : $"图鉴新增 · {first?.Title ?? "未知发现"}";
            }
            if (bondAward.TuanTuanLevelUp || bondAward.DrorLevelUp) return "伙伴羁绊提升，解锁新的同行回应";
            if (newlyClaimable > 0) return $"新增 {newlyClaimable} 项可领取旅程目标";
            if (bondAward.TotalXp > 0) return $"伙伴羁绊 +{bondAward.TotalXp} 经验";
            return "营地已记录本次旅程";
        }
    }
}
