using System.Collections.Generic;
using PenguinRun.Core;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class CollisionSystem
    {
        private const float PlayerMarginX = 0.04f;
        private const float PlayerMarginZ = 0.02f;
        private const int PerfectDodgePruneThreshold = 64;

        // 擦身带：站立着地且未跳铲时，从可碰撞障碍判定盒外侧掠过才算「擦身而过」。
        // 0.55m 约为一颗鱼干的宽度——比车道间距 3m 小一个量级，只有贴着障碍走才触发。
        private const float NearMissBandX = 0.55f;

        private readonly RunnerAudio audio;
        private readonly RunnerCombatTuning combat;
        private readonly RunUpgradeState upgrades;
        private readonly HashSet<GameObject> perfectDodged = new();
        private readonly HashSet<GameObject> nearMissed = new();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly HashSet<GameObject> warnedMissingSpecs = new();
#endif

        public CollisionSystem(RunnerAudio audio, RunnerCombatTuning combat, RunUpgradeState upgrades)
        {
            this.audio = audio;
            this.combat = combat;
            this.upgrades = upgrades;
        }

        public void Reset()
        {
            perfectDodged.Clear();
            nearMissed.Clear();
        }

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
            System.Action onPerfectDodge = null,
            System.Action onPlayerDamaged = null,
            float obstacleSweepDz = 0f,
            System.Action onNearMiss = null)
        {
            if (world.HitInvulnerabilityTimer > 0f) return false;

            // 完美闪避去重表整局只增不减，长局会攒下上千个已销毁障碍的引用。
            // 同时处于判定带内的障碍只有个位数，超过阈值即清掉已销毁项。
            if (perfectDodged.Count > PerfectDodgePruneThreshold)
            {
                perfectDodged.RemoveWhere(o => o == null);
            }
            if (nearMissed.Count > PerfectDodgePruneThreshold)
            {
                nearMissed.RemoveWhere(o => o == null);
            }

            // 蹲姿判定盒：滑铲立即压低、结束后逐渐起身；无 motor 时按站姿处理
            var bodyHeight = motor?.BodyHeight ?? 1.56f;

            var px = player.position.x;
            var pz = player.position.z;
            for (var i = obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = obstacles[i];
                var hasSpec = obstacleSpecs.TryGetValue(obstacle, out var s);
                var spec = hasSpec ? s : ObstacleColliderSpec.SmallJumpable;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // 按对象引用去重，而不是按名字：obstacle.name 每次取值都会 marshal 出新 string，
                // 而这一行位于每帧每障碍的路径上（缺规格时会持续命中）。
                if (!hasSpec && warnedMissingSpecs.Add(obstacle))
                {
                    Debug.LogWarning($"[CollisionSystem] 障碍 \"{obstacle.name}\" 未注册 ObstacleColliderSpec，回退 SmallJumpable（违反领域规则）");
                }
#endif

                // Z 轴判定：常规同帧重叠 + 掉帧穿透补判。
                //
                // 障碍每帧统一沿 -Z 平移 obstacleSweepDz，本帧从 oz+sweep 走到 oz。只测当前位置时，
                // 若单帧位移大于判定带宽度，障碍会整个跳过玩家而不触发命中——
                // maxSpeed 32m/s 下 30fps 单帧走 1.07m，而 LowSlideGate 判定带只有 0.68m 宽。
                //
                // 补判必须严格限定在「上一帧还在身前、这一帧已到身后」的障碍上：直接把判定带
                // 向 +Z 拉长会把上一帧已经判过、此刻已在身后的障碍重新拉进判定，导致玩家跳过去
                // 之后又在落地帧被判中。tunneled 条件只在 sweep > 2*halfDz 时才可能成立，
                // 因此 60fps 常规帧率下行为与扫掠前完全一致。
                var relZ = pz - obstacle.transform.position.z;
                var halfDz = spec.HalfDz + PlayerMarginZ;
                var overlapsNow = relZ >= -halfDz && relZ <= halfDz;
                var tunneledPast = relZ > halfDz && relZ < obstacleSweepDz - halfDz;
                if (!overlapsNow && !tunneledPast) continue;
                if (obstacle.TryGetComponent<ObstacleBehavior>(out var behavior) && !behavior.IsCollidable)
                    continue;

                var dx = Mathf.Abs(obstacle.transform.position.x - px);
                var hitHalfDx = spec.HalfDx + PlayerMarginX;
                if (dx > hitHalfDx)
                {
                    // 擦身而过：只看本帧真实重叠带（tunneled 补判不算），且玩家没有靴底净空——
                    // 跳铲动作不计擦身，同一障碍与完美闪避奖励互斥。
                    if (overlapsNow && onNearMiss != null && dx < hitHalfDx + NearMissBandX
                        && slideTimerSeconds <= 0f && (motor == null || (motor.Grounded && !motor.IsLowPosture))
                        && playerY <= obstacle.transform.position.y + spec.FeetClearanceAboveRoot
                        && !perfectDodged.Contains(obstacle) && nearMissed.Add(obstacle))
                    {
                        feedbackText = "擦身而过！";
                        feedbackTimer = 0.55f;
                        onNearMiss.Invoke();
                    }
                    continue;
                }

                var clearedByJump = playerY > obstacle.transform.position.y + spec.FeetClearanceAboveRoot;
                var clearedByCrouch = spec.IsLow && bodyHeight <= spec.PassUnderMaxHeight;

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

                if (motor != null && motor.PerfectActionWindow > 0f
                    && !perfectDodged.Contains(obstacle) && !nearMissed.Contains(obstacle))
                {
                    if (clearedByCrouch && slideTimerSeconds > 0f && motor.LastCriticalAction == RunnerAction.Slide)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "完美滑铲！+25";
                        feedbackTimer = 0.7f;
                        EventBus.Emit(GameEvents.PerfectDodge);
                        onPerfectDodge?.Invoke();
                    }
                    else if (clearedByJump && motor.LastCriticalAction == RunnerAction.Jump)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "完美跳跃！+25";
                        feedbackTimer = 0.7f;
                        EventBus.Emit(GameEvents.PerfectDodge);
                        onPerfectDodge?.Invoke();
                    }
                }

                if (clearedByCrouch) continue;
                if (clearedByJump) continue;

                var outcome = RunnerHitResolver.TryResolveObstacleDefense(world, spec, combat);
                ApplyOutcome(outcome, world, spawner, obstacle, ref feedbackText, ref feedbackTimer, pulseHaptic);
                if (outcome.Kind == HitOutcomeKind.Damage || outcome.Kind == HitOutcomeKind.GameOver)
                    onPlayerDamaged?.Invoke();
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
                world.SetHitInvulnerability(outcome.InvulnerabilitySeconds + shieldGrace
                    + upgrades.HitInvulnerabilityBonusSeconds);
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
                // 振动统一由 RunFeedbackProfile 按反馈种类决定，这里不再直接触发，避免同一次受伤震两回。
                case HitOutcomeKind.BlockedByDefense:
                    audio.PlayShieldBreak();
                    break;
                case HitOutcomeKind.Damage:
                    EventBus.Emit(GameEvents.PlayerHurt);
                    break;
                case HitOutcomeKind.GameOver:
                    audio.PlayCrash();
                    break;
            }
        }
    }
}
