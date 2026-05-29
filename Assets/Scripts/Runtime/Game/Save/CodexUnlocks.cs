using PenguinRun;
using PenguinRun.Game.Mission;

namespace PenguinRun.Game.Save
{
    /// <summary>
    /// 根据单局成绩与累计局数更新图鉴解锁（当前跑酷无独立剧情关卡时的进度近似规则）。
    /// 需在 <see cref="MissionStore.RecordAfterRun"/> 之后调用，以便读取累计局数。
    /// </summary>
    public static class CodexUnlocks
    {
        // ── 与剧情图鉴提示文案共用（勿与 ApplyAfterRun 硬编码两套数）────────────────
        public const int RescueTuanTuanDistanceMeters = 450;
        public const int RescueTuanTuanMinScore = 1000;
        public const int CompanionDrorMinDistanceMeters = 900;
        public const int CompanionDrorMinLifetimeRuns = 5;
        public const int TakamatsuMinScore = 2800;
        public const float TakamatsuMinSurvivalSeconds = 70f;

        /// <summary>剧情向独立条目数（进度条母数不变，避免老玩家突然「缩水」）。</summary>
        public const int StoryEntryTotal = 5;

        /// <summary>图鉴中 Boss 战绩条目数（与 PenguinRun.BossDefinitions 顺序总数一致）。</summary>
        public const int BossEntryTotal = 6;

        public static void ApplyAfterRun(int score, int distanceMeters, float survivalSeconds, bool dailyMode)
        {
            var runs = MissionStore.LifetimeRunsTotal;

            if (!PlayerSave.Runner3DTutorialDone && runs >= 1)
                PlayerSave.Runner3DTutorialDone = true;

            if (!PlayerSave.RescuedTuanTuan && (distanceMeters >= RescueTuanTuanDistanceMeters || score >= RescueTuanTuanMinScore))
                PlayerSave.RescuedTuanTuan = true;

            if (!PlayerSave.CompanionDrorUnlocked && (dailyMode || distanceMeters >= CompanionDrorMinDistanceMeters || runs >= CompanionDrorMinLifetimeRuns))
                PlayerSave.CompanionDrorUnlocked = true;

            if (!PlayerSave.TakamatsuDefeated && (score >= TakamatsuMinScore || survivalSeconds >= TakamatsuMinSurvivalSeconds))
                PlayerSave.TakamatsuDefeated = true;
        }

        public static int UnlockedCount()
        {
            var n = 0;
            if (PlayerSave.RescuedTuanTuan) n++;
            if (PlayerSave.CompanionDrorUnlocked) n++;
            if (PlayerSave.TakamatsuDefeated) n++;
            if (PlayerSave.VisitedCamp) n++;
            if (PlayerSave.Runner3DTutorialDone) n++;
            return n;
        }

        public static int BossesDefeatedCount()
        {
            var n = 0;
            foreach (var b in BossDefinitions.GetAllBossesOrdered())
            {
                if (PlayerSave.HasDefeatedBoss(b.BossId)) n++;
            }
            return n;
        }
    }
}
