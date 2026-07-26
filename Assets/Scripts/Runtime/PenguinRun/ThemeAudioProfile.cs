namespace PenguinRun
{
    /// <summary>
    /// 每个主题的听感档案。此前六张图共用同一条 ambience 与同一套撞击/拾取音，
    /// 音频上的「主题差异」实际只有 BGM 一层——BGM 一停或重复，六张图听起来完全一样。
    ///
    /// 与 <see cref="ThemePresentationProfile"/> 一样，本档案是纯表现层：
    /// 它不改变任何碰撞、伤害或生成规则。
    /// </summary>
    internal readonly struct ThemeAudioProfile
    {
        /// <summary>循环环境音床。由 tools/generate_theme_audio.py 合成，无第三方授权。</summary>
        public readonly string AmbienceClip;
        public readonly float AmbiencePitch;
        public readonly float AmbienceVolumeScale;

        /// <summary>落地与受击共用的「地面材质」音色，让脚下的东西听起来像这张图。</summary>
        public readonly string SurfaceClip;
        public readonly float SurfacePitch;

        /// <summary>金币音的半音偏移，给每张图一个可辨识的调性。</summary>
        public readonly float CoinPitchScale;

        /// <summary>Boss 招式击发音的音调缩放，叠在既有的每招差异之上。</summary>
        public readonly float BossStrikePitchScale;

        private ThemeAudioProfile(string ambienceClip, float ambiencePitch, float ambienceVolumeScale,
            string surfaceClip, float surfacePitch, float coinPitchScale, float bossStrikePitchScale)
        {
            AmbienceClip = ambienceClip;
            AmbiencePitch = ambiencePitch;
            AmbienceVolumeScale = ambienceVolumeScale;
            SurfaceClip = surfaceClip;
            SurfacePitch = surfacePitch;
            CoinPitchScale = coinPitchScale;
            BossStrikePitchScale = bossStrikePitchScale;
        }

        public static ThemeAudioProfile For(RunnerMapTheme theme) => theme switch
        {
            // 冰湖回音谷：空旷低风 + 稀疏冰裂回声；脚下清脆，金币偏高。谱心约 2543Hz。
            RunnerMapTheme.IceLakeEcho => new(
                "ambience_theme_ice_lake", 1.00f, 1.00f,
                "surface_ice_lake", 1.00f, 1.06f, 1.00f),

            // 雪松废墟：林间中频风 + 木料吱呀；脚下沉闷木质，金币最低。谱心约 1275Hz。
            RunnerMapTheme.CedarRuins => new(
                "ambience_theme_cedar_ruins", 0.96f, 1.05f,
                "surface_cedar_ruins", 0.94f, 0.92f, 0.90f),

            // 极光磁场：电磁嗡鸣 + 扫频嘶声；脚下带静电噼啪，整体最亮。谱心约 4945Hz。
            RunnerMapTheme.AuroraField => new(
                "ambience_theme_aurora_field", 1.04f, 0.92f,
                "surface_aurora_field", 1.10f, 1.14f, 1.12f),

            // 北境雾堤：可视距离本身是机制，音频同样闷——高频滚降 + 远处雾号。谱心约 449Hz。
            RunnerMapTheme.MistDike => new(
                "ambience_theme_mist_dike", 0.92f, 1.12f,
                "surface_mist_dike", 0.86f, 0.88f, 0.84f),

            // 海洋珊瑚礁：低频涌动 + 上升气泡；脚下水花。谱心约 879Hz。
            RunnerMapTheme.OceanReef => new(
                "ambience_theme_ocean_reef", 0.98f, 1.06f,
                "surface_ocean_reef", 1.04f, 1.00f, 0.96f),

            // 天空飞翔：全场唯一日光图，宽频气流 + 高空哨音。谱心约 3189Hz。
            RunnerMapTheme.SkyFlight => new(
                "ambience_theme_sky_flight", 1.06f, 0.96f,
                "surface_sky_flight", 1.16f, 1.10f, 1.06f),

            _ => For(RunnerMapTheme.IceLakeEcho),
        };
    }
}
