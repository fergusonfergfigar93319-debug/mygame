using PenguinRun.Game.Mission;

namespace PenguinRun.Game.Save
{
    /// <summary>
    /// 根据单局成绩与累计局数更新图鉴解锁（当前跑酷无独立剧情关卡时的进度近似规则）。
    /// 需在 <see cref="MissionStore.RecordAfterRun"/> 之后调用，以便读取累计局数。
    /// </summary>
    public static class CodexUnlocks
    {
        public static void ApplyAfterRun(int score, int distanceMeters, float survivalSeconds, bool dailyMode)
        {
            var runs = MissionStore.LifetimeRunsTotal;

            if (!PlayerSave.Runner3DTutorialDone && runs >= 1)
                PlayerSave.Runner3DTutorialDone = true;

            if (!PlayerSave.RescuedTuanTuan && (distanceMeters >= 450 || score >= 1000))
                PlayerSave.RescuedTuanTuan = true;

            if (!PlayerSave.CompanionDrorUnlocked && (dailyMode || distanceMeters >= 900 || runs >= 5))
                PlayerSave.CompanionDrorUnlocked = true;

            if (!PlayerSave.TakamatsuDefeated && (score >= 2800 || survivalSeconds >= 70f))
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

        public const int EntryTotal = 5;
    }
}
