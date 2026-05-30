using System;

namespace PenguinRun.Core
{
    /// <summary>
    /// 跑酷游戏状态机：替代 RunnerGameController 中的 boolean flag 组合。
    /// 状态转换有明确的进入/退出回调，防止非法状态组合。
    /// </summary>
    public sealed class GameStateMachine
    {
        public enum State
        {
            /// <summary>场景加载中，等待初始化完成。</summary>
            Loading,
            /// <summary>开局倒计时 3-2-1。</summary>
            Countdown,
            /// <summary>等待玩家点击开始。</summary>
            Idle,
            /// <summary>正常跑酷中。</summary>
            Running,
            /// <summary>暂停中。</summary>
            Paused,
            /// <summary>Boss 战进行中。</summary>
            BossFight,
            /// <summary>主题切换过渡中。</summary>
            ThemeTransition,
            /// <summary>死亡序列播放中。</summary>
            DeathSequence,
            /// <summary>游戏结束，等待操作。</summary>
            GameOver,
            /// <summary>本局结算完成，即将切换场景。</summary>
            Finished
        }

        public State Current { get; private set; } = State.Loading;
        public State Previous { get; private set; } = State.Loading;

        public event Action<State, State> OnStateChanged;

        public bool IsRunning => Current == State.Running || Current == State.BossFight;
        public bool CanAcceptInput => Current == State.Idle || Current == State.Running || Current == State.BossFight;
        public bool IsGameOver => Current == State.GameOver || Current == State.DeathSequence || Current == State.Finished;

        /// <summary>尝试转换到新状态。返回是否成功。</summary>
        public bool TransitionTo(State next)
        {
            if (!IsValidTransition(Current, next))
            {
                return false;
            }

            Previous = Current;
            Current = next;
            OnStateChanged?.Invoke(Previous, Current);
            return true;
        }

        /// <summary>强制转换（跳过验证，用于紧急情况）。</summary>
        public void ForceTransitionTo(State next)
        {
            Previous = Current;
            Current = next;
            OnStateChanged?.Invoke(Previous, Current);
        }

        private static bool IsValidTransition(State from, State to)
        {
            if (from == to) return false;

            return to switch
            {
                State.Loading => from == State.Finished,
                State.Countdown => from == State.Loading || from == State.Idle,
                State.Idle => from == State.Countdown || from == State.Running || from == State.BossFight
                           || from == State.Paused || from == State.ThemeTransition || from == State.GameOver,
                State.Running => from == State.Idle || from == State.Countdown || from == State.BossFight
                              || from == State.ThemeTransition || from == State.Paused,
                State.Paused => from == State.Running || from == State.BossFight,
                State.BossFight => from == State.Running,
                State.ThemeTransition => from == State.Running,
                State.DeathSequence => from == State.Running || from == State.BossFight,
                State.GameOver => from == State.DeathSequence,
                State.Finished => from == State.GameOver,
                _ => false,
            };
        }

        public void Reset()
        {
            Previous = Current;
            Current = State.Loading;
        }
    }
}
