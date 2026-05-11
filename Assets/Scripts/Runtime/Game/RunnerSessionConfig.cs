using PenguinRun.Game.Save;

namespace PenguinRun.Game
{
    /// <summary>
    /// 一局跑酷的"配置入参"集合：模式、地图主题、玩家信息、营地等级、设置项。
    /// 取代旧的 AndroidUnityBridge，全部从 PlayerSave + RunSession 派生。
    /// </summary>
    public sealed class RunnerSessionConfig
    {
        public string RunMode = "runner_3d";
        public bool Daily;
        public string Nickname = PlayerSave.DefaultNickname;
        public int BestScore;
        public int DashLevel;
        public int TuanLevel;
        public int MagnetLevel;
        public int PolarLevel;
        public bool SfxEnabled = true;
        public bool BgmEnabled = true;
        public string RunBgmTrack = PlayerSave.DefaultRunBgmTrack;
        public string AmbienceTrack = PlayerSave.DefaultAmbienceTrack;
        public string SfxStyle = PlayerSave.DefaultSfxStyle;
        public float HapticIntensity = 0.7f;
        public RunnerMapTheme MapTheme = RunnerMapTheme.IceLakeEcho;

        /// <summary>本局难度（影响速度曲线和 Boss 触发时机）。</summary>
        public DifficultyKind Difficulty = DifficultyKind.Normal;

        /// <summary>便捷访问：当前难度预设对象。</summary>
        public DifficultyPreset DifficultyPreset => DifficultyPreset.ForKind(Difficulty);

        public static RunnerSessionConfig Snapshot()
        {
            return new RunnerSessionConfig
            {
                RunMode = RunSession.NextRunDaily ? "daily_3d" : "runner_3d",
                Daily = RunSession.NextRunDaily,
                Nickname = PlayerSave.PlayerNickname,
                BestScore = PlayerSave.BestScore,
                DashLevel = CampUpgrades.GetLevel(CampUpgradeKind.Dash),
                TuanLevel = CampUpgrades.GetLevel(CampUpgradeKind.Tuan),
                MagnetLevel = CampUpgrades.GetLevel(CampUpgradeKind.Magnet),
                PolarLevel = CampUpgrades.GetLevel(CampUpgradeKind.Polar),
                SfxEnabled = PlayerSave.SfxEnabled,
                BgmEnabled = PlayerSave.BgmEnabled,
                RunBgmTrack = PlayerSave.RunBgmTrack,
                AmbienceTrack = PlayerSave.AmbienceTrack,
                SfxStyle = PlayerSave.SfxStyle,
                HapticIntensity = PlayerSave.HapticIntensity,
                MapTheme = RunSession.NextRunTheme,
                Difficulty = RunSession.NextRunDifficulty,
            };
        }
    }
}
