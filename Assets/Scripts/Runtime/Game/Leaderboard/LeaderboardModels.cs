using System;

namespace PenguinRun.Game.Leaderboard
{
    public enum LeaderboardSort
    {
        ByTotalScore,
        ByDistance,
        BySurvivalTime,
    }

    [Serializable]
    public sealed class LeaderboardEntry
    {
        public string id;
        public string playerId;
        public string nickname;
        public int totalScore;
        public float distanceScoreUnits;
        public int fishSnacks;
        public int beaconCount;
        public int lorePageCount;
        public float survivalSeconds;
        public long timestampMillis;
        public bool rescuedTuanTuan;
        public string mode;

        /// <summary>挑战桶（如 "yyyy-MM-dd"）；休闲模式为 null/空。</summary>
        public string challengeBucket;
    }

    public sealed class LeaderboardSubmitResult
    {
        public int RankByScore;
        public bool MadeTop20;
        public int? RankInChallengeBucket;
    }
}
