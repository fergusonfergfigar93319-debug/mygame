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
            var review = BuildRunDepthReview(result);
            var growthSummary = result.Growth.StrongestSummary;
            var progression = result.BonusCoins > 0 || result.BonusScore > 0
                ? $"{growthSummary} · 加成：鱼干 +{result.BonusCoins} / 得分 +{result.BonusScore}"
                : string.IsNullOrEmpty(growthSummary) ? "营地已记录本次旅程" : growthSummary;
            return new JourneyResultViewModel(result.NewBest ? "新纪录写入旅程册" : "旅程完成",
                result.Score, result.DistanceMeters, result.Coins, comparison, review, progression);
        }

        private static string BuildRunDepthReview(RunOutcomeRouter.Result result)
        {
            var selectedCount = result.SelectedUpgradeIds?.Count ?? 0;
            var skillStrength = result.SkillScore + result.PeakSkillChain * 80;
            var routeStrength = result.RiskyRoutesCompleted * 260 + result.RouteEventsCompleted * 45;
            var buildStrength = selectedCount * 170;

            string highlight;
            if (skillStrength >= routeStrength && skillStrength >= buildStrength && result.SkillScore > 0)
            {
                highlight = $"技巧高光：{result.SkillScore:N0} 分 · 峰值连段 ×{result.PeakSkillChain}";
            }
            else if (routeStrength >= buildStrength && result.RouteEventsCompleted > 0)
            {
                highlight = $"路线高光：完成 {result.RouteEventsCompleted} 次 · 冒险 {result.RiskyRoutesCompleted} 次";
            }
            else if (selectedCount > 0)
            {
                var first = RunUpgradeCatalog.For(result.SelectedUpgradeIds[0]);
                highlight = $"构筑高光：{first.Title} 等 {selectedCount} 张札记";
            }
            else if (result.LastBossSpeedFishBonus > 0)
            {
                highlight = $"Boss 速胜 {result.LastBossSpeedTier} · 额外鱼干 {result.LastBossSpeedFishBonus}";
            }
            else
            {
                highlight = "这次旅程已记录到营地手记";
            }

            if (!string.IsNullOrWhiteSpace(result.ThemedRelicId))
            {
                highlight += $" · 发现{RelicLabel(result.ThemedRelicId)}";
            }
            return highlight;
        }

        private static string RelicLabel(string relicId) => relicId switch
        {
            "relic_ice_fracture" => "冰裂遗物",
            "relic_cedar_root" => "雪松根徽",
            "relic_aurora_polarity" => "极光极芯",
            "relic_mist_decoy" => "雾影铃",
            "relic_coral_rebound" => "回弹珊瑚",
            "relic_storm_draft" => "风暴羽片",
            _ => "主题遗物",
        };
    }
}
