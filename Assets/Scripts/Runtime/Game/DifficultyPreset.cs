namespace PenguinRun.Game
{
    /// <summary>
    /// 游戏难度级别。
    /// </summary>
    public enum DifficultyKind
    {
        Easy   = 0,
        Normal = 1,
        Hard   = 2,
        Expert = 3,
    }

    /// <summary>
    /// 各难度对应的速度、加速度、Boss 时机等参数预设。
    /// 通过 <see cref="ForKind"/> 取得具体预设后注入 <see cref="RunnerSessionConfig"/>。
    /// </summary>
    public sealed class DifficultyPreset
    {
        public DifficultyKind Kind;
        public string DisplayName;
        public string Tag;          // 简短标签，用于 HUD 显示
        public string Description;

        // ── 速度参数 ──────────────────────────────────────────
        /// <summary>初始跑步速度（m/s）。</summary>
        public float StartSpeed;
        /// <summary>最大速度上限（m/s）。</summary>
        public float MaxSpeed;
        /// <summary>每米距离对应的速度增量。</summary>
        public float SpeedRampPerMeter;

        // ── Boss 时机 ─────────────────────────────────────────
        /// <summary>第一只 Boss 出现的距离（米）。</summary>
        public float FirstBossDistance;
        /// <summary>之后每隔多少米再次触发 Boss 战。</summary>
        public float BossInterval;
        /// <summary>Boss 攻击最短间隔（秒）。</summary>
        public float BossPatternMinSeconds;
        /// <summary>Boss 攻击最长间隔（秒）。</summary>
        public float BossPatternMaxSeconds;

        // ── 得分倍率 ──────────────────────────────────────────
        /// <summary>难度得分倍率（显示在结算界面）。</summary>
        public float ScoreMultiplier;

        // ── 预设集合 ──────────────────────────────────────────
        public static readonly DifficultyPreset Easy = new()
        {
            Kind                 = DifficultyKind.Easy,
            DisplayName          = "轻松",
            Tag                  = "EASY",
            Description          = "速度温和，Boss 很晚出现，适合新手入门",
            StartSpeed           = 7f,
            MaxSpeed             = 22f,
            SpeedRampPerMeter    = 0.010f,
            FirstBossDistance    = 1400f,
            BossInterval         = 1200f,
            BossPatternMinSeconds = 2.6f,
            BossPatternMaxSeconds = 4.0f,
            ScoreMultiplier      = 0.75f,
        };

        public static readonly DifficultyPreset Normal = new()
        {
            Kind                 = DifficultyKind.Normal,
            DisplayName          = "普通",
            Tag                  = "NORMAL",
            Description          = "标准跑酷体验，Boss 600 米出现",
            StartSpeed           = 10f,
            MaxSpeed             = 32f,
            SpeedRampPerMeter    = 0.018f,
            FirstBossDistance    = 800f,
            BossInterval         = 800f,
            BossPatternMinSeconds = 1.8f,
            BossPatternMaxSeconds = 2.8f,
            ScoreMultiplier      = 1.0f,
        };

        public static readonly DifficultyPreset Hard = new()
        {
            Kind                 = DifficultyKind.Hard,
            DisplayName          = "困难",
            Tag                  = "HARD",
            Description          = "加速更猛，Boss 在 400 米出现，攻击间隔更短",
            StartSpeed           = 13f,
            MaxSpeed             = 40f,
            SpeedRampPerMeter    = 0.026f,
            FirstBossDistance    = 400f,
            BossInterval         = 500f,
            BossPatternMinSeconds = 1.2f,
            BossPatternMaxSeconds = 2.0f,
            ScoreMultiplier      = 1.5f,
        };

        public static readonly DifficultyPreset Expert = new()
        {
            Kind                 = DifficultyKind.Expert,
            DisplayName          = "专家",
            Tag                  = "EXPERT",
            Description          = "极限速度，Boss 狂暴，得分翻倍，挑战极限",
            StartSpeed           = 16f,
            MaxSpeed             = 52f,
            SpeedRampPerMeter    = 0.035f,
            FirstBossDistance    = 250f,
            BossInterval         = 350f,
            BossPatternMinSeconds = 0.9f,
            BossPatternMaxSeconds = 1.5f,
            ScoreMultiplier      = 2.0f,
        };

        public static DifficultyPreset[] All => new[] { Easy, Normal, Hard, Expert };

        public static DifficultyPreset ForKind(DifficultyKind kind) => kind switch
        {
            DifficultyKind.Easy   => Easy,
            DifficultyKind.Hard   => Hard,
            DifficultyKind.Expert => Expert,
            _                     => Normal,
        };
    }
}
