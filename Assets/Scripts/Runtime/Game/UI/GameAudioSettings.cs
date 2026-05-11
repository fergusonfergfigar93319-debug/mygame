namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 大厅等场景监听设置变更，避免找不到 MainMenu 组件引用。
    /// </summary>
    public static class GameAudioSettings
    {
        public static event System.Action LobbyBgmChanged;

        public static void NotifyLobbyBgmChanged() => LobbyBgmChanged?.Invoke();
    }
}
