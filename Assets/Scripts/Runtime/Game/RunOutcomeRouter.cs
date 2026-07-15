using System;
using System.Collections.Generic;
using PenguinRun.Game.Leaderboard;
using PenguinRun.Game.Mission;
using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;
using PenguinRun.Game.Growth;
using UnityEngine;

namespace PenguinRun.Game
{
    /// <summary>
    /// 跑酷一局结束后的结算路由：写最高分、加鱼干、上本地榜、记任务进度。
    /// 把曾经在 Kotlin 里 UnityRunnerBridge.applyRunResult 的逻辑收敛到这里。
    /// </summary>
    public static class RunOutcomeRouter
    {
        public sealed class Result
        {
            public int Score;
            public int DistanceMeters;
            public int Coins;
            public float SurvivalSeconds;
            public bool NewBest;
            public int RankByScore;
            public int? RankInChallengeBucket;
            public bool Daily;

            /// <summary>结算阶段从基础值升到最终值的鱼干增量（含营地强化与本局消耗券）。</summary>
            public int BonusCoins;
            /// <summary>同上，得分增量。</summary>
            public int BonusScore;
            /// <summary>本局消耗了「双倍鱼干券」。</summary>
            public bool ConsumedDoubleFishTicket;
            /// <summary>本局消耗了「得分加成券」。</summary>
            public bool ConsumedScoreBoostTicket;
            public string LastBossSpeedTier;
            public int LastBossSpeedFishBonus;
            public int LastBossSpeedScoreBonus;
            public int RouteEventsCompleted;
            public int RiskyRoutesCompleted;
            public int SkillScore;
            public int PeakSkillChain;
            public IReadOnlyList<RunUpgradeId> SelectedUpgradeIds;
            public IReadOnlyList<string> CompletedRouteEventIds;
            public string ThemedRelicId;
            public ThemeMasteryRunSnapshot ThemeMastery;
            public GrowthTransactionResult Growth;
            public bool CompanionHelperTriggered;
            public int CompanionHelperFishBonus;
        }

        /// <summary>
        /// 应用一局结算：参数为 RunnerGameController 在 FinishRun 时算出的各项数据。
        /// </summary>
        public static Result Apply(int score, int distanceMeters, int coins, float survivalSeconds, bool dailyMode,
            DifficultyKind difficulty,
            string lastBossSpeedTier = "C", int lastBossSpeedFishBonus = 0, int lastBossSpeedScoreBonus = 0,
            int routeEventsCompleted = 0, int riskyRoutesCompleted = 0,
            int skillScore = 0, int peakSkillChain = 0,
            IReadOnlyList<RunUpgradeId> selectedUpgradeIds = null,
            IReadOnlyList<string> completedRouteEventIds = null,
            string themedRelicId = "",
            ThemeMasteryRunSnapshot themeMastery = default)
        {
            score = Math.Max(0, score);
            distanceMeters = Math.Max(0, distanceMeters);
            coins = Math.Max(0, coins);
            survivalSeconds = Math.Max(0f, survivalSeconds);

            // 1) 先消耗本局的临时道具（在前一帧 PrepareXxxRun 写入），算出本局的额外乘数。
            var (ticketFishMul, ticketScoreMul) = ShopStore.ConsumePendingForOutcome();
            var consumedDoubleFish = ticketFishMul > 1.001f;
            var consumedScoreBoost = ticketScoreMul > 1.001f;

            // 2) 叠加营地的全局乘数（鱼干收益 / 得分加成）与难度得分倍率。
            var bondFishMul = 1f + CompanionBondStore.GetPassiveHelperBonus(CompanionId.TuanTuan);
            var fishMul = CampUpgrades.GetFishGainMultiplier() * ticketFishMul * bondFishMul;
            var difficultyScoreMul = Mathf.Max(0.01f, DifficultyPreset.ForKind(difficulty).ScoreMultiplier);
            var scoreMul = CampUpgrades.GetScoreMultiplier() * ticketScoreMul * difficultyScoreMul;
            var helperTriggered = riskyRoutesCompleted > 0 && ShouldTriggerCompanionHelp(
                CompanionBondStore.GetConditionalHelperChance(CompanionId.Dror), score, distanceMeters);
            var helperFishBonus = helperTriggered ? 4 + Math.Min(12, riskyRoutesCompleted * 2) : 0;

            var baseCoins = coins;
            var baseScore = score;
            coins = Mathf.Max(coins, Mathf.RoundToInt(baseCoins * fishMul));
            coins += helperFishBonus;
            score = Mathf.Max(score, Mathf.RoundToInt(baseScore * scoreMul));

            var prevBest = PlayerSave.BestScore;
            var newBest = score > prevBest;
            if (newBest) PlayerSave.BestScore = score;
            if (coins > 0) PlayerSave.AddFishSnacks(coins);

            string bucket = dailyMode ? DateTime.Now.ToString("yyyy-MM-dd") : null;
            var entry = new LeaderboardEntry
            {
                id = LeaderboardStore.NewEntryId(),
                playerId = PlayerSave.GetOrCreatePlayerId(),
                nickname = PlayerSave.PlayerNickname,
                totalScore = score,
                distanceScoreUnits = distanceMeters,
                fishSnacks = coins,
                beaconCount = 0,
                lorePageCount = 0,
                survivalSeconds = survivalSeconds,
                timestampMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                rescuedTuanTuan = PlayerSave.RescuedTuanTuan,
                mode = dailyMode ? "unity_daily" : "unity_runner",
                challengeBucket = bucket,
            };

            var submit = LeaderboardStore.Submit(entry);

            var growth = GrowthTransaction.Apply(new GrowthTransactionInput(
                score, distanceMeters, coins, survivalSeconds, dailyMode,
                routeEventsCompleted, riskyRoutesCompleted, skillScore,
                completedRouteEventIds, themedRelicId, themeMastery));

            PlayerSave.IncrementTotalRuns();

            // 记录本次跑酷数据到历史记录
            RunRecords.AddRecord(
                score, distanceMeters, coins, survivalSeconds,
                newBest, dailyMode ? "daily" : "endless",
                submit.RankByScore);

            PlayerSave.Flush();

            return new Result
            {
                Score = score,
                DistanceMeters = distanceMeters,
                Coins = coins,
                SurvivalSeconds = survivalSeconds,
                NewBest = newBest,
                RankByScore = submit.RankByScore,
                RankInChallengeBucket = submit.RankInChallengeBucket,
                Daily = dailyMode,
                BonusCoins = Math.Max(0, coins - baseCoins),
                BonusScore = Math.Max(0, score - baseScore),
                ConsumedDoubleFishTicket = consumedDoubleFish,
                ConsumedScoreBoostTicket = consumedScoreBoost,
                LastBossSpeedTier = string.IsNullOrEmpty(lastBossSpeedTier) ? "C" : lastBossSpeedTier,
                LastBossSpeedFishBonus = Mathf.Max(0, lastBossSpeedFishBonus),
                LastBossSpeedScoreBonus = Mathf.Max(0, lastBossSpeedScoreBonus),
                RouteEventsCompleted = Math.Max(0, routeEventsCompleted),
                RiskyRoutesCompleted = Math.Max(0, riskyRoutesCompleted),
                SkillScore = Math.Max(0, skillScore),
                PeakSkillChain = Math.Max(0, peakSkillChain),
                SelectedUpgradeIds = CopyUpgradeIds(selectedUpgradeIds),
                CompletedRouteEventIds = CopyStrings(completedRouteEventIds),
                ThemedRelicId = themedRelicId ?? string.Empty,
                ThemeMastery = themeMastery,
                Growth = growth,
                CompanionHelperTriggered = helperTriggered,
                CompanionHelperFishBonus = helperFishBonus,
            };
        }

        private static IReadOnlyList<RunUpgradeId> CopyUpgradeIds(IReadOnlyList<RunUpgradeId> source)
        {
            if (source == null || source.Count == 0)
                return Array.AsReadOnly(Array.Empty<RunUpgradeId>());

            var copy = new RunUpgradeId[source.Count];
            for (var i = 0; i < source.Count; i++) copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<string> CopyStrings(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
                return Array.AsReadOnly(Array.Empty<string>());
            var copy = new string[source.Count];
            for (var i = 0; i < source.Count; i++) copy[i] = source[i] ?? string.Empty;
            return Array.AsReadOnly(copy);
        }

        private static bool ShouldTriggerCompanionHelp(float chance, int score, int distanceMeters)
        {
            if (chance <= 0f) return false;
            unchecked
            {
                var hash = score * 73856093 ^ distanceMeters * 19349663 ^ PlayerSave.TotalRuns * 83492791;
                var roll = (uint)hash % 10000u;
                return roll < (uint)Mathf.RoundToInt(Mathf.Clamp01(chance) * 10000f);
            }
        }
    }
}
