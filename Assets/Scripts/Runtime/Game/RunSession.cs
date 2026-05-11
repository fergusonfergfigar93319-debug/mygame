using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;

namespace PenguinRun.Game
{
    /// <summary>
    /// 跨场景共享的轻量状态：上一局结算结果、下一局的模式与主题。
    /// 不持久化；进程内静态。
    /// </summary>
    public static class RunSession
    {
        public static bool NextRunDaily;
        public static RunnerMapTheme NextRunTheme = RunnerMapTheme.IceLakeEcho;
        public static DifficultyKind NextRunDifficulty = DifficultyKind.Normal;

        public static RunOutcomeRouter.Result LastResult;

        public static void ConsumeLastResult()
        {
            LastResult = null;
        }

        /// <summary>主菜单点击「开始 3D」时调用，准备一局休闲跑酷。</summary>
        public static void PrepareEndlessRun(RunnerMapTheme theme = RunnerMapTheme.IceLakeEcho, DifficultyKind difficulty = DifficultyKind.Normal)
        {
            NextRunDaily = false;
            NextRunTheme = theme;
            NextRunDifficulty = difficulty;
            ShopStore.ConsumeOneOfEachForRunStart();
        }

        /// <summary>主菜单点击「今日挑战」时调用，准备一局每日模式跑酷。</summary>
        public static void PrepareDailyRun(RunnerMapTheme theme = RunnerMapTheme.IceLakeEcho, DifficultyKind difficulty = DifficultyKind.Normal)
        {
            NextRunDaily = true;
            NextRunTheme = theme;
            NextRunDifficulty = difficulty;
            ShopStore.ConsumeOneOfEachForRunStart();
        }

        public static string ResolveDefaultTheme()
        {
            return PlayerSave.ResumeLevel switch
            {
                PlayerSave.ResumeLevelIce => "ice",
                PlayerSave.ResumeLevelMist => "mist",
                _ => "cedar",
            };
        }
    }
}
