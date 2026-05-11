namespace PenguinRun
{
    public readonly struct RunnerRunResult
    {
        public RunnerRunResult(int score, int distanceMeters, int coins, float survivalSeconds,
            string lastBossSpeedTier = "C", int lastBossSpeedFishBonus = 0, int lastBossSpeedScoreBonus = 0)
        {
            Score = score;
            DistanceMeters = distanceMeters;
            Coins = coins;
            SurvivalSeconds = survivalSeconds;
            LastBossSpeedTier = string.IsNullOrEmpty(lastBossSpeedTier) ? "C" : lastBossSpeedTier;
            LastBossSpeedFishBonus = lastBossSpeedFishBonus;
            LastBossSpeedScoreBonus = lastBossSpeedScoreBonus;
        }

        public int Score { get; }
        public int DistanceMeters { get; }
        public int Coins { get; }
        public float SurvivalSeconds { get; }
        public string LastBossSpeedTier { get; }
        public int LastBossSpeedFishBonus { get; }
        public int LastBossSpeedScoreBonus { get; }
    }
}

