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
                    : ObstacleColliderSpec.DefaultHigh;

                var dz = Mathf.Abs(obstacle.transform.position.z - pz);
                if (dz > spec.HalfDz) continue;
                var dx = Mathf.Abs(obstacle.transform.position.x - px);
                if (dx > spec.HalfDx) continue;

                var lowObstacle = spec.IsLow;

                // ── 完美闪避识别（在 critical action 窗口内成功避开）──
                // 滑铲穿过高障碍 / 跳跃越过低障碍 都算完美闪避（仅统计一次）
                if (motor != null && motor.PerfectActionWindow > 0f && !perfectDodged.Contains(obstacle))
                {
                    if (slideTimerSeconds > 0f && !lowObstacle && motor.LastCriticalAction == RunnerAction.Slide)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "\u26A1 完美滑铲！+25";
                        feedbackTimer = 0.7f;
                        audio.PlayPerfectDodge();
                    }
                    else if (playerY > groundY + 0.58f && lowObstacle && motor.LastCriticalAction == RunnerAction.Jump)
                    {
                        perfectDodged.Add(obstacle);
                        world.AddPerfectDodge();
                        feedbackText = "\u26A1 完美跳跃！+25";
                        feedbackTimer = 0.7f;
                        audio.PlayPerfectDodge();
                    }
                }

                if (slideTimerSeconds > 0f && !lowObstacle) continue;
                if (playerY > groundY + 0.58f && lowObstacle) continue;

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
