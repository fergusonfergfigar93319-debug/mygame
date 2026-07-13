using System.Collections.Generic;
using PenguinRun.Core;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class CollisionSystem
    {
        private const float PlayerMarginX = 0.06f;
        private const float PlayerMarginZ = 0.10f;

        private readonly RunnerAudio audio;
        private readonly RunnerCombatTuning combat;
        private readonly RunUpgradeState upgrades;
        private readonly HashSet<GameObject> perfectDodged = new();

        public CollisionSystem(RunnerAudio audio, RunnerCombatTuning combat, RunUpgradeState upgrades)
        {
            this.audio = audio;
            this.combat = combat;
            this.upgrades = upgrades;
        }

        public void Reset() => perfectDodged.Clear();

        public bool CheckCollisions(
            Transform player,
            float playerY,
            float groundY,
            float slideTimerSeconds,
            float dashTimerSeconds,
            WorldDirector world,
            SegmentSpawner spawner,
            List<GameObject> obstacles,
            Dictionary<GameObject, ObstacleColliderSpec> obstacleSpecs,
            ref string feedbackText,
            ref float feedbackTimer,
            System.Action pulseHaptic,
            PlayerMotor motor = null,
            System.Action onPerfectDodge = null)
        {
            if (world.HitInvulnerabilityTimer > 0f) return false;

            var px = player.position.x;
            var pz = player.position.z;
            for (var i = obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = obstacles[i];
                var spec = obstacleSpecs.TryGetValue(obstacle, out var s)
                    ? s
                    : ObstacleColliderSpec.SmallJumpable;

                var dz = Mathf.Abs(obstacle.transform.position.z - pz);
                if (dz > spec.HalfDz + PlayerMarginZ) continue;
                var dx = Mathf.Abs(obstacle.transform.position.x - px);
                if (dx > spec.HalfDx + PlayerMarginX) continue;

                if (obstacle.TryGetComponent<ObstacleBehavior>(out var behavior) && !behavior.IsCollidable)
                    continue;

                var clearedByJump = playerY > obstacle.transform.position.y + spec.FeetClearanceAboveRoot;

                if (dashTimerSeconds > 0f && obstacle.TryGetComponent<ShatterWall>(out var shatterWall))
                {
                    if (shatterWall.TryShatter())
                    {
                        feedbackText = "冲刺碎墙！";
                        feedbackTimer = 0.65f;
                        audio.PlayShieldBreak();
                        pulseHaptic?.Invoke();
                        continue;
                    }
                }

                if (motor != null && motor.PerfectActionWindow > 0f && !perfectDodged.Contains(obstacle))
                {
                    if (slideTimerSeconds > 0f && spec.IsLow && motor.LastCriticalAction == RunnerAction.Slide)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "\u26A1 完美滑铲！+25";
                        feedbackTimer = 0.7f;
                        EventBus.Emit(GameEvents.PerfectDodge);
                        onPerfectDodge?.Invoke();
                    }
                    else if (clearedByJump && motor.LastCriticalAction == RunnerAction.Jump)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "\u26A1 完美跳跃！+25";
                        feedbackTimer = 0.7f;
                        EventBus.Emit(GameEvents.PerfectDodge);
                        onPerfectDodge?.Invoke();
                    }
                }

                if (slideTimerSeconds > 0f && spec.IsLow) continue;
                if (clearedByJump) continue;

                var outcome = RunnerHitResolver.TryResolveObstacleDefense(world, spec, combat);
                ApplyOutcome(outcome, world, spawner, obstacle, ref feedbackText, ref feedbackTimer, pulseHaptic);
                if (outcome.Kind == HitOutcomeKind.GameOver) return true;
                if (outcome.Kind != HitOutcomeKind.None) return false;
            }

            return false;
        }

        private void ApplyOutcome(
            HitOutcome outcome,
            WorldDirector world,
            SegmentSpawner spawner,
            GameObject obstacle,
            ref string feedbackText,
            ref float feedbackTimer,
            System.Action pulseHaptic)
        {
            if (outcome.Kind == HitOutcomeKind.None) return;

            if (outcome.ConsumeShield)
                world.ShieldTimer = 0f;

            if (outcome.RemoveObstacle)
                spawner.TryRemoveObstacle(obstacle);

            if (outcome.InvulnerabilitySeconds > 0f)
            {
                var shieldGrace = outcome.ConsumeShield ? upgrades.ShieldGraceSeconds : 0f;
                world.SetHitInvulnerability(outcome.InvulnerabilitySeconds + shieldGrace);
            }

            if (outcome.Kind == HitOutcomeKind.Damage || outcome.Kind == HitOutcomeKind.GameOver)
                world.BreakPerfectDodgeStreak();

            if (!string.IsNullOrEmpty(outcome.FeedbackText))
            {
                feedbackText = outcome.FeedbackText;
                feedbackTimer = outcome.FeedbackDuration;
            }

            switch (outcome.Kind)
            {
                case HitOutcomeKind.BlockedByDefense:
                    audio.PlayShieldBreak();
                    pulseHaptic?.Invoke();
                    break;
                case HitOutcomeKind.Damage:
                    EventBus.Emit(GameEvents.PlayerHurt);
                    pulseHaptic?.Invoke();
                    break;
                case HitOutcomeKind.GameOver:
                    audio.PlayCrash();
                    pulseHaptic?.Invoke();
                    break;
            }
        }
    }
}
