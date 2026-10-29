#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using PenguinRun.Core;
using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun.Editor
{
    public static class ProjectInputPauseValidation
    {
        private static readonly List<string> Failures = new();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Put(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);
        private static T Get<T>(object target, string name) =>
            (T)target.GetType().GetField(name, Private).GetValue(target);
        private static void Check(bool condition, string message)
        {
            if (!condition) Failures.Add(message);
        }
        private static void Near(float actual, float expected, string message) =>
            Check(Mathf.Abs(actual - expected) < 0.0001f, message + $" (expected {expected}, got {actual})");

        public static void ValidateFromCommandLine()
        {
            Failures.Clear();
            ValidatePausedTimers();
            ValidateBufferedActions();
            ValidateGuidedTimers();
            ValidateNormalTimers();
            ValidateThemeTransition();
            if (Failures.Count > 0)
                throw new Exception("[ProjectInputPauseValidation] FAIL: " + string.Join("; ", Failures));
            Debug.Log("[ProjectInputPauseValidation] PASS: five real-controller pause/input scenarios.");
        }

        private static void ValidatePausedTimers()
        {
            using var f = new Fixture();
            Put(f.Game, "manualDashCooldownTimer", 1.2f);
            f.Game.World.DashTimer = 0.8f;
            var position = f.Player.position;
            Check(f.Game.SetPaused(true, false), "first pause must succeed");
            Check(!f.Game.SetPaused(true, false), "repeated pause must be idempotent");
            for (var i = 0; i < 20; i++) Near(f.Game.TickRunningSimulation(0.05f), 0f, "paused speed");
            Near(f.Game.ManualDashCooldown, 1.2f, "pause must freeze cooldown");
            Near(f.Game.World.DashTimer, 0.8f, "pause must freeze active dash");
            Check(f.Player.position == position, "pause must preserve physical position");
            Check(f.Game.SetPaused(false, false), "resume must succeed");
            Near(f.Game.ManualDashCooldown, 1.2f, "resume must not reset cooldown");
        }

        private static void ValidateBufferedActions()
        {
            using var f = new Fixture();
            Put(f.Game, "manualDashCooldownTimer", 0.01f);
            Put(f.Game, "dashBufferTimer", 0.16f);
            Put(f.Game.Motor, "jumpBufferTimer", 0.18f);
            Put(f.Game.Motor, "slideBufferTimer", 0.16f);
            Put(f.Game.Motor, "velocityY", 5f);
            var lane = f.Game.Motor.Lane;
            f.Game.SetPaused(true, false);
            Near(Get<float>(f.Game, "dashBufferTimer"), 0f, "pause clears queued dash");
            Near(Get<float>(f.Game.Motor, "jumpBufferTimer"), 0f, "pause clears queued jump");
            Near(Get<float>(f.Game.Motor, "slideBufferTimer"), 0f, "pause clears queued slide");
            Near(f.Game.Motor.VerticalVelocity, 5f, "clearing input must preserve airborne velocity");
            Check(f.Game.Motor.Lane == lane, "clearing input must preserve lane");
            f.Game.TickRunningSimulation(0.02f);
            Near(f.Game.World.DashTimer, 0f, "buffered dash must not fire while paused");
            f.Game.SetPaused(false, false);
            f.Game.TickRunningSimulation(0.02f);
            Near(f.Game.World.DashTimer, 0f, "old dash must not fire on resume");
        }

        private static void ValidateGuidedTimers()
        {
            using var f = new Fixture();
            Put(f.Game, "waitingForGuidedReaction", true);
            Put(f.Game, "manualDashCooldownTimer", 0.01f);
            Put(f.Game, "dashBufferTimer", 0.16f);
            f.Game.TickRunningSimulation(0.02f);
            Near(f.Game.ManualDashCooldown, 0.01f, "guided freeze must preserve cooldown");
            Near(f.Game.World.DashTimer, 0f, "guided freeze must not fire queued dash");
        }

        private static void ValidateNormalTimers()
        {
            using var f = new Fixture();
            Put(f.Game, "manualDashCooldownTimer", 0.01f);
            Put(f.Game, "dashBufferTimer", 0.16f);
            f.Game.TickRunningSimulation(0.02f);
            Near(f.Game.ManualDashCooldown, RunnerGameController.ManualDashCooldownSeconds,
                "running cooldown completion must still fire buffered dash");
            Check(f.Game.World.DashTimer > 1f, "ordinary buffered dash remains functional");
        }

        private static void ValidateThemeTransition()
        {
            using var f = new Fixture();
            Put(f.Game, "dashBufferTimer", 0.16f);
            Put(f.Game.Motor, "jumpBufferTimer", 0.18f);
            Get<RunThemeSessionState>(f.Game, "themeSession").RequestSwitch();
            Check(f.Game.TryBeginThemeTransition(), "theme transition begins from running");
            Near(Get<float>(f.Game, "dashBufferTimer"), 0f, "transition clears queued dash");
            Near(Get<float>(f.Game.Motor, "jumpBufferTimer"), 0f, "transition clears queued jump");
            f.Game.CompleteThemeTransition();
            Check(f.Game.CurrentState == GameStateMachine.State.Running, "theme transition returns to running");
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject root = new("InputPauseValidation");
            public readonly RunnerGameController Game;
            public Transform Player => root.transform;
            public Fixture()
            {
                var audio = root.AddComponent<RunnerAudio>();
                Game = new RunnerGameController(new RunnerTuning(), new RunnerSessionConfig(),
                    audio, Player, new Presentation(Player), Player, null, null, null);
                Get<GameStateMachine>(Game, "stateMachine").ForceTransitionTo(GameStateMachine.State.Running);
            }
            public void Dispose() => UnityEngine.Object.DestroyImmediate(root);
        }

        // Rendering is irrelevant to these tests; controller, FSM, motor and world are real.
        private sealed class Presentation : IPlayerCharacterPresentation
        {
            public Transform Root { get; }
            public Presentation(Transform root) { Root = root; }
            public void Tick(float dt, in RunnerAvatarState state) { }
            public void TriggerHurt() { }
            public void TriggerPerfectDodge() { }
            public void TriggerCelebration() { }
            public void TriggerInteraction(RunnerAvatarInteraction interaction) { }
        }
    }
}
#endif
