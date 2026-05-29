using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PenguinRun.Game.Leaderboard
{
    /// <summary>
    /// 本地排行榜（JSON 文件位于 Application.persistentDataPath）。
    /// 与 Kotlin 版 LocalLeaderboardRepository 行为一致：上限 80 条、分数排序、按桶名次计算。
    /// </summary>
    public static class LeaderboardStore
    {
        private const string FileName = "leaderboard.json";
        private const int MaxStored = 80;

        [Serializable]
        private sealed class EntriesWrapper
        {
            public List<LeaderboardEntry> entries = new();
        }

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static List<LeaderboardEntry> GetTopEntries(
            int limit = 20,
            LeaderboardSort sort = LeaderboardSort.ByTotalScore,
            string challengeBucket = null)
        {
            var all = LoadAll();
            if (challengeBucket != null)
            {
                all.RemoveAll(e => e.challengeBucket != challengeBucket);
            }
            all.Sort(ComparatorFor(sort));
            if (all.Count > limit) all.RemoveRange(limit, all.Count - limit);
            return all;
        }

        public static LeaderboardSubmitResult Submit(LeaderboardEntry entry)
        {
            var all = LoadAll();
            all.Add(entry);

            if (all.Count > MaxStored)
            {
                all.Sort((a, b) => a.timestampMillis.CompareTo(b.timestampMillis));
                all.RemoveRange(0, all.Count - MaxStored);
            }

            SaveAll(all);

            var ranked = new List<LeaderboardEntry>(all);
            ranked.Sort(ComparatorFor(LeaderboardSort.ByTotalScore));
            var rank = ranked.FindIndex(e => e.id == entry.id) + 1;

            int? rankInChallenge = null;
            var bucket = entry.challengeBucket;
            var bucketTop20 = false;
            if (!string.IsNullOrEmpty(bucket))
            {
                var rankedInBucket = new List<LeaderboardEntry>();
                foreach (var e in all) if (e.challengeBucket == bucket) rankedInBucket.Add(e);
                rankedInBucket.Sort(ComparatorFor(LeaderboardSort.ByTotalScore));
                var ri = rankedInBucket.FindIndex(e => e.id == entry.id);
                rankInChallenge = ri < 0 ? null : ri + 1;
                bucketTop20 = ri >= 0 && ri < 20;
            }

            var madeTop20 = string.IsNullOrEmpty(bucket)
                ? rank > 0 && rank <= 20
                : bucketTop20;

            return new LeaderboardSubmitResult
            {
                RankByScore = Mathf.Max(1, rank),
                MadeTop20 = madeTop20,
                RankInChallengeBucket = rankInChallenge,
            };
        }

        /// <summary>在给定 challenge 桶（null = 不限）内，统计该玩家的历史峰值，用于「我的最好一局」高亮。</summary>
        public static bool TryGetPlayerPeakMetrics(
            string playerId,
            string challengeBucket,
            out int peakTotalScore,
            out float peakDistance,
            out float peakSurvival)
        {
            peakTotalScore = 0;
            peakDistance = 0f;
            peakSurvival = 0f;
            if (string.IsNullOrEmpty(playerId)) return false;
            var found = false;
            foreach (var e in LoadAll())
            {
                if (e.playerId != playerId) continue;
                if (!string.IsNullOrEmpty(challengeBucket) &&
                    !string.Equals(e.challengeBucket, challengeBucket, StringComparison.Ordinal))
                    continue;
                found = true;
                if (e.totalScore > peakTotalScore) peakTotalScore = e.totalScore;
                if (e.distanceScoreUnits > peakDistance) peakDistance = e.distanceScoreUnits;
                if (e.survivalSeconds > peakSurvival) peakSurvival = e.survivalSeconds;
            }
            return found;
        }

        public static string NewEntryId() => Guid.NewGuid().ToString();

        private static Comparison<LeaderboardEntry> ComparatorFor(LeaderboardSort sort) =>
            sort switch
            {
                LeaderboardSort.ByDistance => (a, b) =>
                {
                    var c = b.distanceScoreUnits.CompareTo(a.distanceScoreUnits);
                    return c != 0 ? c : b.totalScore.CompareTo(a.totalScore);
                },
                LeaderboardSort.BySurvivalTime => (a, b) =>
                {
                    var c = b.survivalSeconds.CompareTo(a.survivalSeconds);
                    return c != 0 ? c : b.totalScore.CompareTo(a.totalScore);
                },
                _ => (a, b) =>
                {
                    var c = b.totalScore.CompareTo(a.totalScore);
                    return c != 0 ? c : b.distanceScoreUnits.CompareTo(a.distanceScoreUnits);
                },
            };

        private static List<LeaderboardEntry> LoadAll()
        {
            try
            {
                if (!File.Exists(FilePath)) return new List<LeaderboardEntry>();
                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<LeaderboardEntry>();
                var wrap = JsonUtility.FromJson<EntriesWrapper>(json);
                return wrap?.entries ?? new List<LeaderboardEntry>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Leaderboard load failed: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        private static void SaveAll(List<LeaderboardEntry> list)
        {
            try
            {
                var wrap = new EntriesWrapper { entries = list };
                var json = JsonUtility.ToJson(wrap);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Leaderboard save failed: {ex.Message}");
            }
        }
    }
}
