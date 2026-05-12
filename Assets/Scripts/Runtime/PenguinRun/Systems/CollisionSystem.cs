using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class CollisionSystem
    {
        private readonly RunnerAudio audio;
        // 已记完美闪避的障碍（避免重复计分）
        private readonly HashSet<GameObject> perfectDodged = new();

        public CollisionSystem(RunnerAudio audio)
        {
            this.audio = audio;
        }

        public void Reset() => perfectDodged.Clear();

        /// <summary>雷羽可破坏的小型障碍：<see cref="ObstacleColliderSpec.SmallJumpable"/> 等价规格。</summary>
        private static bool ObstacleColliderSpecLooksSmallDestructible(in ObstacleColliderSpec spec)
        {
            var s = ObstacleColliderSpec.SmallJumpable;
            return !spec.IsLow && Mathf.Abs(spec.HalfDx - s.HalfDx) < 0.02f &&
                   Mathf.Abs(spec.HalfDz - s.HalfDz) < 0.02f &&
                   Mathf.Abs(spec.FeetClearanceAboveRoot - s.FeetClearanceAboveRoot) < 0.02f;
        }

        public bool CheckCollisions(
            Transform player,
            float playerY,
            float groundY,
            float slideTimerSeconds,
            WorldDirector world,
            SegmentSpawner spawner,
            List<GameObject> obstacles,
            Dictionary<GameObject, ObstacleColliderSpec> obstacleSpecs,
            ref string feedbackText,
            ref float feedbackTimer,
            System.Action pulseHaptic,
            PlayerMotor motor = null)
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
                if (dz > spec.HalfDz) continue;
                var dx = Mathf.Abs(obstacle.transform.position.x - px);
                if (dx > spec.HalfDx) continue;

                // 通用跳跃越障判定：脚底高度超过障碍要求的清除高度即可跳过
                var clearedByJump = playerY > obstacle.transform.position.y + spec.FeetClearanceAboveRoot;

                // ── 完美闪避识别（在 critical action 窗口内成功避开）──
                // 滑铲穿过障碍、跳跃越过障碍 都算完美闪避（仅统计一次）
                if (motor != null && motor.PerfectActionWindow > 0f && !perfectDodged.Contains(obstacle))
                {
                    if (slideTimerSeconds > 0f && motor.LastCriticalAction == RunnerAction.Slide)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "\u26A1 完美滑铲！+25";
                        feedbackTimer = 0.7f;
                        audio.PlayPerfectDodge();
                    }
                    else if (clearedByJump && motor.LastCriticalAction == RunnerAction.Jump)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "\u26A1 完美跳跃！+25";
                        feedbackTimer = 0.7f;
                        audio.PlayPerfectDodge();
                    }
                }

                // 滑铲：从障碍下方掠过（低矮横梁/小障碍都可以滑铲通过）
                if (slideTimerSeconds > 0f) continue;
                // 跳跃：脚底高度超过障碍要求的清除高度
                if (clearedByJump) continue;

                if (world.ShieldTimer > 0f)
                {
                    world.ShieldTimer = 0f;
                    feedbackText = "护盾抵消";
                    feedbackTimer = 0.8f;
                    audio.PlayShieldBreak();
                    pulseHaptic?.Invoke();
                    spawner.TryRemoveObstacle(obstacle);
                    return false;
                }

                if (world.TryConsumeFishBomb())
                {
                    feedbackText = "鱼弹反弹！";
                    feedbackTimer = 0.85f;
                    audio.PlayShieldBreak();
                    pulseHaptic?.Invoke();
                    spawner.TryRemoveObstacle(obstacle);
                    world.SetHitInvulnerability(1.2f);
                    return false;
                }

                // 冰镜：一次冲撞消耗整段镜面时间，弹开障碍
                if (world.IceMirrorTimer > 0f)
                {
                    world.IceMirrorTimer = 0f;
                    feedbackText = "冰镜折射！";
                    feedbackTimer = 0.82f;
                    audio.PlayShieldBreak();
                    pulseHaptic?.Invoke();
                    spawner.TryRemoveObstacle(obstacle);
                    world.SetHitInvulnerability(1.0f);
                    return false;
                }

                // 树人护甲：吸收伤害层数
                if (world.TryConsumeTreantArmor())
                {
                    feedbackText = $"树皮挡下！护甲×{world.TreantArmorHits}";
                    feedbackTimer = 0.82f;
                    audio.PlayShieldBreak();
                    pulseHaptic?.Invoke();
                    spawner.TryRemoveObstacle(obstacle);
                    world.SetHitInvulnerability(1.1f);
                    return false;
                }

                // 珊瑚回弹：预备态时抵消一次冲撞并触发冲刺/无敌（与护盾可叠：护盾先耗尽）
                if (world.TryConsumeCoralBounce())
                {
                    feedbackText = "珊瑚回弹！";
                    feedbackTimer = 0.9f;
                    audio.PlayShieldBreak();
                    pulseHaptic?.Invoke();
                    spawner.TryRemoveObstacle(obstacle);
                    return false;
                }

                // 雷羽：冲刺态下可粉碎小型可跳障碍
                if (world.ThunderFeatherTimer > 0f && ObstacleColliderSpecLooksSmallDestructible(spec))
                {
                    feedbackText = "雷羽击破！";
                    feedbackTimer = 0.65f;
                    audio.PlayShieldBreak();
                    pulseHaptic?.Invoke();
                    spawner.TryRemoveObstacle(obstacle);
                    return false;
                }

                spawner.TryRemoveObstacle(obstacle);
                world.SetHitInvulnerability(2f);
                var gameOver = world.LoseOneLife();
                if (gameOver)
                {
                    feedbackText = "生命耗尽";
                    feedbackTimer = 1.4f;
                    audio.PlayCrash();
                    pulseHaptic?.Invoke();
                    return true;
                }

                feedbackText = $"受伤！剩余 {world.Lives} 心（2秒无敌）";
                feedbackTimer = 0.95f;
                audio.PlayPlayerHurt();
                pulseHaptic?.Invoke();
                return false;
            }

            return false;
        }
    }
}
