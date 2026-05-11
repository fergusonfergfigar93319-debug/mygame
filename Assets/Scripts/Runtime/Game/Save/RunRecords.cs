using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PenguinRun.Game.Save
{
    /// <summary>
    /// 跑酷记录系统：保存每次无尽模式的详细数据，用于展示历史战绩和统计。
    /// 最多保留最近 50 条记录，按时间倒序排列。
    /// </summary>
    public static class RunRecords
    {
        private const string KeyRecords = "run_records_v1";
        private const int MaxRecords = 50;

        [Serializable]
        public sealed class Record
        {
            public long timestampMillis;
            public int score;
            public int distanceMeters;
            public int fishSnacks;
            public float survivalSeconds;
            public bool wasNewBest;
            public string mode; // "endless" or "daily"
            public int rankAtTime; // 当时的历史排名

            public DateTime Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(timestampMillis).LocalDateTime;
        }

        [Serializable]
        private sealed class RecordList
        {
            public List<Record> records = new();
        }

        /// <summary>添加一条新记录。如果是新纪录，wasNewBest 标记为 true。</summary>
        public static void AddRecord(int score, int distance, int fish, float survival, bool newBest, string mode, int rank)
        {
            var list = LoadList();
            var entry = new Record
            {
                timestampMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                score = Mathf.Max(0, score),
                distanceMeters = Mathf.Max(0, distance),
                fishSnacks = Mathf.Max(0, fish),
                survivalSeconds = Mathf.Max(0f, survival),
                wasNewBest = newBest,
                mode = string.IsNullOrEmpty(mode) ? "endless" : mode,
                rankAtTime = Mathf.Max(1, rank),
            };
            list.records.Insert(0, entry);
            if (list.records.Count > MaxRecords)
                list.records.RemoveRange(MaxRecords, list.records.Count - MaxRecords);
            SaveList(list);
        }

        /// <summary>获取所有记录（已按时间倒序）。</summary>
        public static IReadOnlyList<Record> GetAll()
        {
            return LoadList().records;
        }

        /// <summary>获取最近 N 条记录。</summary>
        public static List<Record> GetRecent(int count)
        {
            var list = LoadList().records;
            return list.Take(Mathf.Min(count, list.Count)).ToList();
        }

        /// <summary>获取统计摘要：总次数、平均得分、最高单次鱼干、总生存时间。</summary>
        public static StatsSummary GetStats()
        {
            var list = LoadList().records;
            if (list.Count == 0)
                return new StatsSummary();

            var totalRuns = list.Count;
            var totalScore = list.Sum(r => r.score);
            var bestScore = list.Max(r => r.score);
            var totalFish = list.Sum(r => r.fishSnacks);
            var bestFish = list.Max(r => r.fishSnacks);
            var totalDistance = list.Sum(r => r.distanceMeters);
            var bestDistance = list.Max(r => r.distanceMeters);
            var totalSurvival = list.Sum(r => r.survivalSeconds);
            var bestSurvival = list.Max(r => r.survivalSeconds);
            var newBestCount = list.Count(r => r.wasNewBest);

            return new StatsSummary
            {
                TotalRuns = totalRuns,
                AverageScore = totalRuns > 0 ? Mathf.RoundToInt((float)totalScore / totalRuns) : 0,
                BestScore = bestScore,
                TotalFishSnacks = totalFish,
                BestFishInOneRun = bestFish,
                TotalDistance = totalDistance,
                BestDistance = bestDistance,
                TotalSurvivalSeconds = totalSurvival,
                BestSurvivalSeconds = bestSurvival,
                NewBestCount = newBestCount,
            };
        }

        /// <summary>获取最佳纪录（用于展示个人巅峰）。</summary>
        public static Record GetPersonalBest()
        {
            var list = LoadList().records;
            return list.OrderByDescending(r => r.score).FirstOrDefault();
        }

        /// <summary>清空所有记录。</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyRecords);
        }

        private static RecordList LoadList()
        {
            var json = PlayerPrefs.GetString(KeyRecords, null);
            if (string.IsNullOrEmpty(json))
                return new RecordList();
            try
            {
                return JsonUtility.FromJson<RecordList>(json) ?? new RecordList();
            }
            catch
            {
                return new RecordList();
            }
        }

        private static void SaveList(RecordList list)
        {
            var json = JsonUtility.ToJson(list);
            PlayerPrefs.SetString(KeyRecords, json);
        }

        public sealed class StatsSummary
        {
            public int TotalRuns;
            public int AverageScore;
            public int BestScore;
            public int TotalFishSnacks;
            public int BestFishInOneRun;
            public int TotalDistance;
            public int BestDistance;
            public float TotalSurvivalSeconds;
            public float BestSurvivalSeconds;
            public int NewBestCount;
        }
    }
}
