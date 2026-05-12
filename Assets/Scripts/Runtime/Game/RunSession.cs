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
        private static readonly RunnerMapTheme[] ThemeCycle =
        {
            RunnerMapTheme.IceLakeEcho,
            RunnerMapTheme.CedarRuins,
            RunnerMapTheme.AuroraField,
            RunnerMapTheme.MistDike,
            RunnerMapTheme.OceanReef,
            RunnerMapTheme.SkyFlight,
        };
        private static int endlessThemeCursor = -1;

        public static RunOutcomeRouter.Result LastResult;

        public static void ConsumeLastResult()
        {
            LastResult = null;
        }

        /// <summary>主菜单点击「开始 3D」时调用，准备一局休闲跑酷。</summary>
        public static void PrepareEndlessRun(RunnerMapTheme? theme = null, DifficultyKind difficulty = DifficultyKind.Normal)
        {
            NextRunDaily = false;
            NextRunTheme = ResolveNextTheme(false, theme);
            NextRunDifficulty = difficulty;
            ShopStore.ConsumeOneOfEachForRunStart();
        }

        /// <summary>主菜单点击「今日挑战」时调用，准备一局每日模式跑酷。</summary>
        public static void PrepareDailyRun(RunnerMapTheme? theme = null, DifficultyKind difficulty = DifficultyKind.Normal)
        {
            NextRunDaily = true;
            NextRunTheme = ResolveNextTheme(true, theme);
            NextRunDifficulty = difficulty;
            ShopStore.ConsumeOneOfEachForRunStart();
        }

        private static RunnerMapTheme ResolveNextTheme(bool daily, RunnerMapTheme? requestedTheme)
        {
            if (requestedTheme.HasValue)
            {
                return requestedTheme.Value;
            }

            if (daily)
            {
                // 每日挑战按日期轮换主题，保证每天有变化
                var utcNow = System.DateTime.UtcNow;
                var idx = (utcNow.Year * 367 + utcNow.DayOfYear) % ThemeCycle.Length;
                return ThemeCycle[idx];
            }

            // 无尽模式按局轮换主题，保证连续多局可见不同场景
            endlessThemeCursor = (endlessThemeCursor + 1) % ThemeCycle.Length;
            return ThemeCycle[endlessThemeCursor];
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
