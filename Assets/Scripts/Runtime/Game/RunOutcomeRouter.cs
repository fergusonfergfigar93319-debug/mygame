using System;
using PenguinRun.Game.Leaderboard;
using PenguinRun.Game.Mission;
using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;
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
        }

        /// <summary>
        /// 应用一局结算：参数为 RunnerGameController 在 FinishRun 时算出的各项数据。
        /// </summary>
        public static Result Apply(int score, int distanceMeters, int coins, float survivalSeconds, bool dailyMode,
            string lastBossSpeedTier = "C", int lastBossSpeedFishBonus = 0, int lastBossSpeedScoreBonus = 0)
        {
            score = Math.Max(0, score);
            distanceMeters = Math.Max(0, distanceMeters);
            coins = Math.Max(0, coins);
            survivalSeconds = Math.Max(0f, survivalSeconds);

            // 1) 先消耗本局的临时道具（在前一帧 PrepareXxxRun 写入），算出本局的额外乘数。
            var (ticketFishMul, ticketScoreMul) = ShopStore.ConsumePendingForOutcome();
            var consumedDoubleFish = ticketFishMul > 1.001f;
            var consumedScoreBoost = ticketScoreMul > 1.001f;

            // 2) 叠加营地的全局乘数（鱼干收益 / 得分加成）。
            var fishMul = CampUpgrades.GetFishGainMultiplier() * ticketFishMul;
            var scoreMul = CampUpgrades.GetScoreMultiplier() * ticketScoreMul;

            var baseCoins = coins;
            var baseScore = score;
            coins = Mathf.Max(coins, Mathf.RoundToInt(baseCoins * fishMul));
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

            MissionStore.RecordAfterRun(score, distanceMeters, coins, dailyMode);
            CodexUnlocks.ApplyAfterRun(score, distanceMeters, survivalSeconds, dailyMode);

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
            };
        }
    }
}
