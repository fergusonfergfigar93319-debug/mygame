using System.Collections.Generic;

namespace PenguinRun.Game.Mission
{
    public enum MissionTrack
    {
        Daily,
        Achievement,
    }

    /// <summary>成就页分组标题（每日任务不使用）。</summary>
    public enum AchievementGroup
    {
        Journey,
        PeakScore,
    }

    public static class AchievementGroups
    {
        public static string SectionTitle(this AchievementGroup g) =>
            g switch
            {
                AchievementGroup.Journey => "旅程与耐力",
                AchievementGroup.PeakScore => "巅峰纪录",
                _ => "其他成就",
            };
    }

    public enum MissionMetric
    {
        CompleteRunsInBucket,
        BestDistanceInBucket,
        BestCoinsInBucket,
        BestScoreInBucket,
        LifetimeRuns,
        LifetimeBestScore,
    }

    public sealed class MissionDefinition
    {
        public string Id;
        public MissionTrack Track;
        public string Title;
        public string Description;
        public MissionMetric Metric;
        public int Target;
        public int RewardFishSnacks;
        public AchievementGroup? AchievementGroup;
    }

    public sealed class MissionUiState
    {
        public MissionDefinition Definition;
        public int Progress;
        public bool Completed;
        public bool Claimed;
        public bool Claimable;
    }

    public static class MissionCatalog
    {
        public static readonly IReadOnlyList<MissionDefinition> All = new List<MissionDefinition>
        {
            new()
            {
                Id = "daily_finish_runs_3",
                Track = MissionTrack.Daily,
                Title = "今日跑者",
                Description = "完成 3 局 Unity 跑酷结算",
                Metric = MissionMetric.CompleteRunsInBucket,
                Target = 3,
                RewardFishSnacks = 25,
            },
            new()
            {
                Id = "daily_distance_800",
                Track = MissionTrack.Daily,
                Title = "远征热身",
                Description = "任意一局跑出 800m 以上距离",
                Metric = MissionMetric.BestDistanceInBucket,
                Target = 800,
                RewardFishSnacks = 18,
            },
            new()
            {
                Id = "daily_coins_25",
                Track = MissionTrack.Daily,
                Title = "鱼干搜集",
                Description = "任意一局累计拾取 25 枚金币",
                Metric = MissionMetric.BestCoinsInBucket,
                Target = 25,
                RewardFishSnacks = 20,
            },
            new()
            {
                Id = "daily_challenge_score",
                Track = MissionTrack.Daily,
                Title = "今日挑战",
                Description = "在今日挑战模式中一局得分达到 1200",
                Metric = MissionMetric.BestScoreInBucket,
                Target = 1200,
                RewardFishSnacks = 35,
            },
            new()
            {
                Id = "ach_runs_20",
                Track = MissionTrack.Achievement,
                Title = "冰原常客",
                Description = "累计完成 20 局跑酷结算",
                Metric = MissionMetric.LifetimeRuns,
                Target = 20,
                RewardFishSnacks = 60,
                AchievementGroup = Mission.AchievementGroup.Journey,
            },
            new()
            {
                Id = "ach_runs_100",
                Track = MissionTrack.Achievement,
                Title = "不倦旅途",
                Description = "累计完成 100 局跑酷结算",
                Metric = MissionMetric.LifetimeRuns,
                Target = 100,
                RewardFishSnacks = 200,
                AchievementGroup = Mission.AchievementGroup.Journey,
            },
            new()
            {
                Id = "ach_best_score_8000",
                Track = MissionTrack.Achievement,
                Title = "星光追忆",
                Description = "历史最高分达到 8000",
                Metric = MissionMetric.LifetimeBestScore,
                Target = 8000,
                RewardFishSnacks = 150,
                AchievementGroup = Mission.AchievementGroup.PeakScore,
            },
        };

        public static IEnumerable<MissionDefinition> DefinitionsFor(MissionTrack track)
        {
            foreach (var def in All)
            {
                if (def.Track == track) yield return def;
            }
        }
    }
}
