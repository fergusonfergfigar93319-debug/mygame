using System;
using System.Collections.Generic;

namespace PenguinRun.Core
{
    /// <summary>
    /// 轻量事件总线：零分配发布-订阅，解耦各系统间通信。
    /// 用法：EventBus.On("CoinCollected", data => { ... }); EventBus.Emit("CoinCollected", 5);
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<string, Delegate> events = new();
        private static readonly List<Entry> pending = new(); // 延迟清理避免迭代中修改
        private static bool emitting;

        private struct Entry { public string Key; public Delegate Handler; public bool Remove; }

        public static void On(string key, Action handler)
        {
            if (!events.TryGetValue(key, out var existing))
            {
                events[key] = handler;
                return;
            }
            events[key] = (Action)existing + handler;
        }

        public static void On<T>(string key, Action<T> handler)
        {
            if (!events.TryGetValue(key, out var existing))
            {
                events[key] = handler;
                return;
            }
            events[key] = (Action<T>)existing + handler;
        }

        public static void Off(string key, Action handler)
        {
            if (!events.TryGetValue(key, out var existing)) return;
            events[key] = (Action)existing - handler;
        }

        public static void Off<T>(string key, Action<T> handler)
        {
            if (!events.TryGetValue(key, out var existing)) return;
            events[key] = (Action<T>)existing - handler;
        }

        public static void Emit(string key)
        {
            if (!events.TryGetValue(key, out var d) || d is not Action action) return;
            emitting = true;
            try { action(); }
            finally { emitting = false; FlushPending(); }
        }

        public static void Emit<T>(string key, T arg)
        {
            if (!events.TryGetValue(key, out var d) || d is not Action<T> action) return;
            emitting = true;
            try { action(arg); }
            finally { emitting = false; FlushPending(); }
        }

        /// <summary>清除所有监听（场景切换时调用）。</summary>
        public static void Clear()
        {
            events.Clear();
            pending.Clear();
        }

        private static void FlushPending()
        {
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var entry = pending[i];
                if (entry.Remove)
                {
                    if (events.TryGetValue(entry.Key, out var existing))
                    {
                        events[entry.Key] = Delegate.Remove(existing, entry.Handler);
                    }
                }
                pending.RemoveAt(i);
            }
        }
    }

    /// <summary>项目全局事件常量。</summary>
    public static class GameEvents
    {
        public const string CoinCollected = "CoinCollected";
        public const string PowerUpCollected = "PowerUpCollected";
        public const string PlayerHurt = "PlayerHurt";
        public const string PlayerLanded = "PlayerLanded";
        public const string PlayerJumped = "PlayerJumped";
        public const string PlayerSlid = "PlayerSlid";
        public const string PerfectDodge = "PerfectDodge";
        public const string BossDefeated = "BossDefeated";
        public const string BossHit = "BossHit";
        public const string BossPhaseChanged = "BossPhaseChanged";
        public const string ThemeChanged = "ThemeChanged";
        public const string DistanceMilestone = "DistanceMilestone";
        public const string GameStarted = "GameStarted";
        public const string GameOver = "GameOver";
        public const string GamePaused = "GamePaused";
        public const string GameResumed = "GameResumed";
        public const string SpeedChanged = "SpeedChanged";
        public const string LivesChanged = "LivesChanged";
        public const string SceneTransitionStart = "SceneTransitionStart";
        public const string SceneTransitionComplete = "SceneTransitionComplete";
    }
}
