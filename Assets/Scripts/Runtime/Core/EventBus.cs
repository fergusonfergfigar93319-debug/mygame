using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun.Core
{
    /// <summary>
    /// 轻量事件总线：热路径无消息对象分配，解耦各系统间通信。
    ///
    /// 约束：
    /// - 同一 key 只能注册一种签名（Action 或 Action&lt;T&gt;），混用会抛异常。
    /// - Emit 回调中调用 Off 是安全的（delegate 快照调用）。
    /// - 场景切换时应调用 Clear() 清理所有监听。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<string, Delegate> events = new();

        // ── 注册 ──────────────────────────────────────────────

        public static void On(string key, Action handler)
        {
            if (events.TryGetValue(key, out var existing))
            {
                if (existing is not Action)
                    throw new InvalidOperationException(
                        $"EventBus.On: key '{key}' already registered as Action<>, cannot register as Action.");
                events[key] = (Action)existing + handler;
            }
            else
            {
                events[key] = handler;
            }
        }

        public static void On<T>(string key, Action<T> handler)
        {
            if (events.TryGetValue(key, out var existing))
            {
                if (existing is not Action<T>)
                    throw new InvalidOperationException(
                        $"EventBus.On<T>: key '{key}' already registered as Action, cannot register as Action<{typeof(T).Name}>.");
                events[key] = (Action<T>)existing + handler;
            }
            else
            {
                events[key] = handler;
            }
        }

        // ── 注销 ──────────────────────────────────────────────

        public static void Off(string key, Action handler)
        {
            if (!events.TryGetValue(key, out var existing) || existing is not Action action) return;
            events[key] = action - handler;
        }

        public static void Off<T>(string key, Action<T> handler)
        {
            if (!events.TryGetValue(key, out var existing) || existing is not Action<T> action) return;
            events[key] = action - handler;
        }

        // ── 发射 ──────────────────────────────────────────────

        public static void Emit(string key)
        {
            if (!events.TryGetValue(key, out var d) || d is not Action action) return;
            // 快照：防止回调内 Off 修改字典
            var snapshot = action;
            try { snapshot(); }
            catch (Exception e) { Debug.LogError($"EventBus.Emit('{key}'): {e}"); }
        }

        public static void Emit<T>(string key, T arg)
        {
            if (!events.TryGetValue(key, out var d) || d is not Action<T> action) return;
            var snapshot = action;
            try { snapshot(arg); }
            catch (Exception e) { Debug.LogError($"EventBus.Emit<{typeof(T).Name}>('{key}'): {e}"); }
        }

        public static void Clear()
        {
            events.Clear();
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
