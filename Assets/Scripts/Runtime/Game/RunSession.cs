using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;
using UnityEngine;

namespace PenguinRun.Game
{
    public enum RunnerRunMode
    {
        Story,
        Endless,
        Daily,
    }

    /// <summary>
    /// 跨场景共享的轻量状态：上一局结算结果、下一局的模式与主题。
    /// 不持久化；进程内静态。
    /// </summary>
    public static class RunSession
    {
        public static RunnerRunMode NextRunMode { get; private set; } = RunnerRunMode.Endless;
        public static bool NextRunDaily => NextRunMode == RunnerRunMode.Daily;
        public static int NextRunSeed { get; private set; } = 1;
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
        private static int endlessAttempt;
        private static readonly int[] StoryAttempts = new int[ThemeCycle.Length];

        public static RunOutcomeRouter.Result LastResult;

        public static void ConsumeLastResult()
        {
            LastResult = null;
        }

        /// <summary>主菜单点击「开始 3D」时调用，准备一局休闲跑酷。</summary>
        public static void PrepareEndlessRun(RunnerMapTheme? theme = null, DifficultyKind difficulty = DifficultyKind.Normal)
        {
            NextRunMode = RunnerRunMode.Endless;
            NextRunTheme = ResolveNextTheme(false, theme);
            NextRunDifficulty = difficulty;
            endlessAttempt++;
            NextRunSeed = ResolveRunSeed(
                NextRunMode, NextRunTheme, endlessAttempt, System.DateTime.UtcNow);
            ShopStore.ConsumeOneOfEachForRunStart();
        }

        public static void PrepareStoryRun(
            RunnerMapTheme theme,
            DifficultyKind difficulty = DifficultyKind.Normal)
        {
            NextRunMode = RunnerRunMode.Story;
            NextRunTheme = theme;
            NextRunDifficulty = difficulty;
            var index = System.Math.Clamp((int)theme, 0, StoryAttempts.Length - 1);
            StoryAttempts[index]++;
            NextRunSeed = ResolveRunSeed(
                NextRunMode, NextRunTheme, StoryAttempts[index], System.DateTime.UtcNow);
            ShopStore.ConsumeOneOfEachForRunStart();
        }

        /// <summary>主菜单点击「今日挑战」时调用，准备一局每日模式跑酷。</summary>
        public static void PrepareDailyRun(RunnerMapTheme? theme = null, DifficultyKind difficulty = DifficultyKind.Normal)
        {
            if (!TryPrepareDailyRun(System.DateTime.UtcNow, difficulty, out var error, theme))
            {
                throw new System.InvalidOperationException(error);
            }
        }

        public static bool TryPrepareDailyRun(
            System.DateTime utc,
            DifficultyKind difficulty,
            out string error,
            RunnerMapTheme? theme = null)
        {
            try
            {
                var normalizedUtc = utc.Kind == System.DateTimeKind.Utc ? utc : utc.ToUniversalTime();
                var nextTheme = ResolveNextTheme(true, theme, normalizedUtc);
                var nextSeed = ResolveRunSeed(RunnerRunMode.Daily, nextTheme, 0, normalizedUtc);
                if (nextSeed == 0)
                {
                    error = "今日挑战种子无效，请稍后重试。";
                    return false;
                }

                NextRunMode = RunnerRunMode.Daily;
                NextRunTheme = nextTheme;
                NextRunDifficulty = difficulty;
                NextRunSeed = nextSeed;
                ShopStore.ConsumeOneOfEachForRunStart();
                error = string.Empty;
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                error = "今日挑战准备失败，请返回营地后重试。";
                return false;
            }
        }

        private static RunnerMapTheme ResolveNextTheme(
            bool daily,
            RunnerMapTheme? requestedTheme,
            System.DateTime? utcOverride = null)
        {
            if (requestedTheme.HasValue)
            {
                return requestedTheme.Value;
            }

            if (daily)
            {
                // 每日挑战按日期轮换主题，保证每天有变化
                var utcNow = utcOverride ?? System.DateTime.UtcNow;
                var idx = (utcNow.Year * 367 + utcNow.DayOfYear) % ThemeCycle.Length;
                return ThemeCycle[idx];
            }

            // 无尽模式按局轮换主题，保证连续多局可见不同场景
            endlessThemeCursor = (endlessThemeCursor + 1) % ThemeCycle.Length;
            return ThemeCycle[endlessThemeCursor];
        }

        internal static int ResolveRunSeed(
            RunnerRunMode mode,
            RunnerMapTheme theme,
            int attempt,
            System.DateTime utc)
        {
            unchecked
            {
                var day = utc.Year * 1000 + utc.DayOfYear;
                return mode switch
                {
                    RunnerRunMode.Daily => day * 486187739 ^ (int)theme * 16777619,
                    RunnerRunMode.Story => 0x13579BDF ^ (int)theme * 73856093 ^ attempt * 19349663,
                    _ => System.Environment.TickCount ^ (int)theme * 83492791 ^ attempt * 297121507,
                };
            }
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
