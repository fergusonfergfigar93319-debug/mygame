using PenguinRun.Game.Save;

namespace PenguinRun.Game.UI
{
    /// <summary>Immutable journey page shared by in-run death and lobby result flows.</summary>
    public readonly struct JourneyResultViewModel
    {
        public readonly string Headline;
        public readonly int Score;
        public readonly int DistanceMeters;
        public readonly int Fish;
        public readonly string BestComparison;
        public readonly string ReviewHint;
        public readonly string ProgressionPreview;

        public JourneyResultViewModel(string headline, int score, int distanceMeters, int fish,
            string bestComparison, string reviewHint, string progressionPreview)
        {
            Headline = headline ?? string.Empty;
            Score = System.Math.Max(0, score);
            DistanceMeters = System.Math.Max(0, distanceMeters);
            Fish = System.Math.Max(0, fish);
            BestComparison = bestComparison ?? string.Empty;
            ReviewHint = reviewHint ?? string.Empty;
            ProgressionPreview = progressionPreview ?? string.Empty;
        }

        public static JourneyResultViewModel FromDeath(int score, int distanceMeters, int fish)
        {
            var comparison = PlayerSave.BestScore > 0
                ? $"当前最佳 {PlayerSave.BestScore:N0} 分"
                : "第一段旅程已经写下";
            return new JourneyResultViewModel("旅程暂告一段落", score, distanceMeters, fish,
                comparison, RunnerDeathOverlay.GetDeathAnalysisHint(distanceMeters),
                "返回营地后可查看任务、伙伴与图鉴推进");
        }

        public static JourneyResultViewModel FromRunResult(RunOutcomeRouter.Result result)
        {
            var comparison = result.NewBest
                ? "新的个人最佳纪录"
                : $"本地排名第 {result.RankByScore} 名";
            var review = result.LastBossSpeedFishBonus > 0
                ? $"Boss 速胜 {result.LastBossSpeedTier} · 额外鱼干 {result.LastBossSpeedFishBonus}"
                : "这次旅程已记录到营地手记";
            var progression = result.BonusCoins > 0 || result.BonusScore > 0
                ? $"成长加成：鱼干 +{result.BonusCoins} · 得分 +{result.BonusScore}"
                : "任务、图鉴与营地状态已同步";
            return new JourneyResultViewModel(result.NewBest ? "新纪录写入旅程册" : "旅程完成",
                result.Score, result.DistanceMeters, result.Coins, comparison, review, progression);
        }
    }
}
