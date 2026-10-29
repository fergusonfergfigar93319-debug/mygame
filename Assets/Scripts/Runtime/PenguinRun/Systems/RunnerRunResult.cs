using System;
using System.Collections.Generic;

namespace PenguinRun
{
    public readonly struct RunnerRunResult
    {
        public RunnerRunResult(int score, int distanceMeters, int coins, float survivalSeconds,
            string lastBossSpeedTier = "C", int lastBossSpeedFishBonus = 0, int lastBossSpeedScoreBonus = 0,
            int routeEventsCompleted = 0, int riskyRoutesCompleted = 0,
            int skillScore = 0, int peakSkillChain = 0,
            IReadOnlyList<RunUpgradeId> selectedUpgradeIds = null,
            IReadOnlyList<string> completedRouteEventIds = null,
            string themedRelicId = "",
            ThemeMasteryRunSnapshot themeMastery = default,
            IReadOnlyList<ThemeMasteryRunSnapshot> themeMasteries = null)
        {
            Score = score;
            DistanceMeters = distanceMeters;
            Coins = coins;
            SurvivalSeconds = survivalSeconds;
            LastBossSpeedTier = string.IsNullOrEmpty(lastBossSpeedTier) ? "C" : lastBossSpeedTier;
            LastBossSpeedFishBonus = lastBossSpeedFishBonus;
            LastBossSpeedScoreBonus = lastBossSpeedScoreBonus;
            RouteEventsCompleted = Math.Max(0, routeEventsCompleted);
            RiskyRoutesCompleted = Math.Max(0, riskyRoutesCompleted);
            SkillScore = Math.Max(0, skillScore);
            PeakSkillChain = Math.Max(0, peakSkillChain);
            var copiedIds = selectedUpgradeIds == null
                ? Array.Empty<RunUpgradeId>()
                : CopyIds(selectedUpgradeIds);
            SelectedUpgradeIds = Array.AsReadOnly(copiedIds);
            var copiedRouteIds = completedRouteEventIds == null
                ? Array.Empty<string>()
                : CopyStrings(completedRouteEventIds);
            CompletedRouteEventIds = Array.AsReadOnly(copiedRouteIds);
            ThemedRelicId = themedRelicId ?? string.Empty;
            ThemeMastery = themeMastery;
            ThemeMasteries = ThemeMasterySnapshots.CopyDistinct(themeMasteries, themeMastery);
        }

        public int Score { get; }
        public int DistanceMeters { get; }
        public int Coins { get; }
        public float SurvivalSeconds { get; }
        public string LastBossSpeedTier { get; }
        public int LastBossSpeedFishBonus { get; }
        public int LastBossSpeedScoreBonus { get; }
        public int RouteEventsCompleted { get; }
        public int RiskyRoutesCompleted { get; }
        public int SkillScore { get; }
        public int PeakSkillChain { get; }
        public IReadOnlyList<RunUpgradeId> SelectedUpgradeIds { get; }
        public IReadOnlyList<string> CompletedRouteEventIds { get; }
        public string ThemedRelicId { get; }
        public ThemeMasteryRunSnapshot ThemeMastery { get; }
        public IReadOnlyList<ThemeMasteryRunSnapshot> ThemeMasteries { get; }

        private static RunUpgradeId[] CopyIds(IReadOnlyList<RunUpgradeId> source)
        {
            var copy = new RunUpgradeId[source.Count];
            for (var i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            var copy = new string[source.Count];
            for (var i = 0; i < source.Count; i++) copy[i] = source[i] ?? string.Empty;
            return copy;
        }
    }
}
