namespace PenguinRun
{
    /// <summary>
    /// 主题的展示名与一句话简介，供大厅「下一章」等界面复用。
    ///
    /// 简介写的是玩家在那张图上**实际会遇到什么**（与 ThemePresentationProfile 的光照档案、
    /// ThemeAudioProfile 的听感、以及各主题机关一致），不是泛用的氛围词。
    /// </summary>
    internal static class RunnerMapThemeInfo
    {
        /// <summary>剧情推进顺序，也是「下一章」的检查顺序。</summary>
        public static readonly RunnerMapTheme[] ProgressionOrder =
        {
            RunnerMapTheme.IceLakeEcho,
            RunnerMapTheme.CedarRuins,
            RunnerMapTheme.AuroraField,
            RunnerMapTheme.MistDike,
            RunnerMapTheme.OceanReef,
            RunnerMapTheme.SkyFlight,
        };

        public static string DisplayName(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.CedarRuins => "雪松废墟",
            RunnerMapTheme.AuroraField => "极光磁场",
            RunnerMapTheme.MistDike => "北境雾堤",
            RunnerMapTheme.OceanReef => "海洋珊瑚礁",
            RunnerMapTheme.SkyFlight => "天空飞翔",
            _ => "冰湖回音谷",
        };

        public static string Tagline(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.CedarRuins => "穿过低雾中的旧木桥，避开倒伏雪松和滚落石块。",
            RunnerMapTheme.AuroraField => "极光是唯一的亮源，磁场会在换道时牵着你走。",
            RunnerMapTheme.MistDike => "能见度本身就是机关，相信预警而不是眼睛。",
            RunnerMapTheme.OceanReef => "涌流和气泡之间，珊瑚会把你弹去另一条道。",
            RunnerMapTheme.SkyFlight => "全程唯一的日光赛段，风替你决定落点。",
            _ => "极夜月光下的冰面，回声会提前告诉你薄冰在哪。",
        };
    }
}
