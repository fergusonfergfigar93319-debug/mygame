using System;
using System.Collections.Generic;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Game.Mission
{
    /// <summary>
    /// 任务进度持久化（PlayerPrefs），并在跑酷结束后更新计数与档位。
    /// 与 Kotlin 版 MissionRepository 等价。
    /// </summary>
    public static class MissionStore
    {
        private const string KeyDailyBucket = "mission_daily_bucket_yyyy_mm_dd";
        private const string KeyLifetimeRuns = "mission_lifetime_runs_total";
        private const string KeyLifetimeBestScore = "mission_lifetime_best_score";
        private const string KeyPrefixDailyProgress = "mission_daily_progress";
        private const string KeyPrefixDailyClaimed = "mission_daily_claimed";
        private const string KeyPrefixAchProgress = "mission_ach_progress";
        private const string KeyPrefixAchClaimed = "mission_ach_claimed";

        private static string TodayBucket() => DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>距离次日本地 0 点的毫秒数。</summary>
        public static long MillisUntilNextDailyReset()
        {
            var now = DateTime.Now;
            var tomorrowMidnight = now.Date.AddDays(1);
            var span = tomorrowMidnight - now;
            return span.TotalMilliseconds > 0 ? (long)span.TotalMilliseconds : 0L;
        }

        /// <summary>跨日时清零每日任务的进度与领取标记。</summary>
        public static void EnsureDailyBucket()
        {
            var today = TodayBucket();
            var stored = PlayerPrefs.GetString(KeyDailyBucket, null);
            if (stored == today) return;
            PlayerPrefs.SetString(KeyDailyBucket, today);
            foreach (var def in MissionCatalog.DefinitionsFor(MissionTrack.Daily))
            {
                PlayerPrefs.DeleteKey(ProgressKeyDaily(def.Id));
                PlayerPrefs.DeleteKey(ClaimedKeyDaily(def.Id));
            }
            PlayerPrefs.Save();
        }

        public static List<MissionUiState> MissionStates(MissionTrack track)
        {
            if (track == MissionTrack.Daily) EnsureDailyBucket();
            var list = new List<MissionUiState>();
            foreach (var def in MissionCatalog.DefinitionsFor(track))
            {
                var progress = ReadProgress(def);
                var claimed = ReadClaimed(def);
                var completed = progress >= def.Target;
                list.Add(new MissionUiState
                {
                    Definition = def,
                    Progress = Mathf.Min(progress, def.Target),
                    Completed = completed,
                    Claimed = claimed,
                    Claimable = completed && !claimed,
                });
            }
            return list;
        }

        /// <summary>是否存在可领取奖励（首页角标）。</summary>
        public static bool HasClaimableMissions()
        {
            EnsureDailyBucket();
            foreach (MissionTrack t in Enum.GetValues(typeof(MissionTrack)))
            {
                foreach (var s in MissionStates(t))
                {
                    if (s.Claimable) return true;
                }
            }
            return false;
        }

        /// <summary>跑酷结算后调用，更新所有相关计量；dailyMode=true 时计入「今日挑战」分数任务。</summary>
        public static void RecordAfterRun(int score, int distanceMeters, int coins, bool dailyMode)
        {
            EnsureDailyBucket();
            foreach (var def in MissionCatalog.DefinitionsFor(MissionTrack.Daily))
            {
                var key = ProgressKeyDaily(def.Id);
                var cur = PlayerPrefs.GetInt(key, 0);
                var next = def.Metric switch
                {
                    MissionMetric.CompleteRunsInBucket => cur + 1,
                    MissionMetric.BestDistanceInBucket => Mathf.Max(cur, distanceMeters),
                    MissionMetric.BestCoinsInBucket => Mathf.Max(cur, coins),
                    MissionMetric.BestScoreInBucket => dailyMode ? Mathf.Max(cur, score) : cur,
                    _ => cur,
                };
                PlayerPrefs.SetInt(key, next);
            }

            var lifeRuns = PlayerPrefs.GetInt(KeyLifetimeRuns, 0) + 1;
            PlayerPrefs.SetInt(KeyLifetimeRuns, lifeRuns);

            var lifeBest = Mathf.Max(PlayerPrefs.GetInt(KeyLifetimeBestScore, 0), score);
            PlayerPrefs.SetInt(KeyLifetimeBestScore, lifeBest);

            foreach (var def in MissionCatalog.DefinitionsFor(MissionTrack.Achievement))
            {
                var key = ProgressKeyAch(def.Id);
                var next = def.Metric switch
                {
                    MissionMetric.LifetimeRuns => lifeRuns,
                    MissionMetric.LifetimeBestScore => lifeBest,
                    _ => PlayerPrefs.GetInt(key, 0),
                };
                PlayerPrefs.SetInt(key, next);
            }

            PlayerPrefs.Save();
        }

        /// <summary>已完成并累计的局数（每次 <see cref="RecordAfterRun"/> +1）。图鉴等系统可读。</summary>
        public static int LifetimeRunsTotal => PlayerPrefs.GetInt(KeyLifetimeRuns, 0);

        /// <summary>领取奖励；成功时发放鱼干并标记已领。</summary>
        public static bool ClaimMission(string definitionId)
        {
            MissionDefinition def = null;
            foreach (var d in MissionCatalog.All)
            {
                if (d.Id == definitionId) { def = d; break; }
            }
            if (def == null) return false;

            MissionUiState state = null;
            foreach (var s in MissionStates(def.Track))
            {
                if (s.Definition.Id == definitionId) { state = s; break; }
            }
            if (state == null || !state.Claimable) return false;

            PlayerSave.AddFishSnacks(def.RewardFishSnacks);
            switch (def.Track)
            {
                case MissionTrack.Daily:
                    PlayerPrefs.SetInt(ClaimedKeyDaily(def.Id), 1);
                    break;
                case MissionTrack.Achievement:
                    PlayerPrefs.SetInt(ClaimedKeyAch(def.Id), 1);
                    break;
            }
            PlayerPrefs.Save();
            return true;
        }

        private static int ReadProgress(MissionDefinition def)
        {
            if (def.Track == MissionTrack.Daily)
            {
                return PlayerPrefs.GetInt(ProgressKeyDaily(def.Id), 0);
            }
            return def.Metric switch
            {
                MissionMetric.LifetimeRuns => PlayerPrefs.GetInt(KeyLifetimeRuns, 0),
                MissionMetric.LifetimeBestScore => PlayerPrefs.GetInt(KeyLifetimeBestScore, 0),
                _ => PlayerPrefs.GetInt(ProgressKeyAch(def.Id), 0),
            };
        }

        private static bool ReadClaimed(MissionDefinition def) =>
            def.Track switch
            {
                MissionTrack.Daily => PlayerPrefs.GetInt(ClaimedKeyDaily(def.Id), 0) != 0,
                MissionTrack.Achievement => PlayerPrefs.GetInt(ClaimedKeyAch(def.Id), 0) != 0,
                _ => false,
            };

        private static string ProgressKeyDaily(string id) => $"{KeyPrefixDailyProgress}_{id}";
        private static string ClaimedKeyDaily(string id) => $"{KeyPrefixDailyClaimed}_{id}";
        private static string ProgressKeyAch(string id) => $"{KeyPrefixAchProgress}_{id}";
        private static string ClaimedKeyAch(string id) => $"{KeyPrefixAchClaimed}_{id}";
    }
}
