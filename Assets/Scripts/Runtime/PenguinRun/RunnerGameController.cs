using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Owns one run lifecycle: input → simulation → world advance expectations are ordered by <see cref="PenguinRunnerGame"/>.
    /// </summary>
    internal sealed class RunnerGameController
    {
        private readonly RunnerInput input = new();
        private readonly RunnerTuning tuning;
        private readonly RunnerSessionConfig config;
        private readonly RunnerAudio runnerAudio;
        private readonly PenguinCharacter character;
        private readonly Transform playerTransform;
        private readonly SegmentSpawner spawner;
        private readonly PlayerMotor motor;
        private readonly WorldDirector world;
        private readonly PickupSystem pickups;
        private readonly CollisionSystem collisions;
        private readonly BossSystem bossSystem;
        private readonly Transform sceneRoot;
        private readonly System.Action pulseHaptic;
        private readonly System.Action<RunnerRunResult> onFinish;

        private bool running;
        private bool gameOver;
        private string feedbackText = "";
        private float feedbackTimer;
        private bool finishRequested;
        private float pauseHoldUI;
        private bool waitingForGuidedReaction;
        private float manualDashCooldownTimer;

        public RunnerGameController(
            RunnerTuning tuning,
            RunnerSessionConfig config,
            RunnerAudio runnerAudio,
            Transform playerTransform,
            PenguinCharacter character,
            Transform sceneRoot,
            System.Action pulseHaptic,
            System.Action<RunnerRunResult> onFinish)
        {
            this.tuning = tuning;
            this.config = config;
            this.runnerAudio = runnerAudio;
            this.playerTransform = playerTransform;
            this.character = character;
            this.sceneRoot = sceneRoot;
            this.pulseHaptic = pulseHaptic;
            this.onFinish = onFinish;

            var waveDirector = new SegmentWaveDirector(config.MapTheme);
            spawner = new SegmentSpawner(tuning.world, tuning.powerUp, tuning.movement.laneWidth, config, waveDirector, tuning.movement.groundY);
            motor = new PlayerMotor(playerTransform, runnerAudio, tuning.movement, tuning.powerUp);
            world = new WorldDirector(tuning.world, tuning.pickup);
            pickups = new PickupSystem(tuning.pickup, tuning.powerUp, runnerAudio, config);
            collisions = new CollisionSystem(runnerAudio);
            bossSystem = new BossSystem(runnerAudio, config, sceneRoot, tuning.movement.groundY, tuning.movement.laneWidth);
        }

        public bool Running => running;

        public bool GameOver => gameOver;

        public WorldDirector World => world;

        public SegmentSpawner Spawner => spawner;

        public PlayerMotor Motor => motor;

        public BossSystem Boss => bossSystem;

        public string FeedbackText => feedbackText;

        public float FeedbackTimer => feedbackTimer;

        public void ResetRun(bool start)
        {
            spawner.Reset();
            motor.ResetToStart();
            world.ResetRun();
            bossSystem.Reset();
            collisions.Reset();
            running = start;
            gameOver = false;
            feedbackText = "";
            feedbackTimer = 0f;
            waitingForGuidedReaction = false;
            manualDashCooldownTimer = 0f;

            if (start)
            {
                if (config.Daily)
                {
                    var t = System.DateTime.UtcNow;
                    Random.InitState(t.Year * 100000 + t.Month * 1000 + t.Day * 13 + 7);
                }
                else
                {
                    Random.InitState((int)(Time.realtimeSinceStartup * 1000f) ^ (int)(System.DateTime.UtcNow.Ticks % int.MaxValue));
                }
            }

            spawner.SeedInitialObstacles(world.Distance);
        }

        /// <summary>结束本局并把结果回调给 PenguinRunnerGame（再由它走 RunOutcomeRouter + 场景切换）。</summary>
        public void FinishRun()
        {
            if (finishRequested) return;
            finishRequested = true;
            var dist = Mathf.Max(0, Mathf.RoundToInt(world.Distance));
            var result = new RunnerRunResult(world.Score, dist, world.Coins, world.RunTime,
                world.LastBossSpeedTier, world.LastBossSpeedFishBonus, world.LastBossSpeedScoreBonus);
            onFinish?.Invoke(result);
        }

        public void TogglePause()
        {
            if (!running || gameOver) return;
            world.Paused = !world.Paused;
            runnerAudio.PlayPause();
            feedbackText = world.Paused ? "已暂停" : "继续";
            feedbackTimer = 0.7f;
            pauseHoldUI = world.Paused ? 0.3f : 0f;
        }

        public bool IsPaused => world.Paused;
        public bool IsSimulationFrozen => world.Paused || waitingForGuidedReaction;

        public float PauseUiHold => pauseHoldUI;

        public void TickIdle(float rawDt)
        {
            var action = input.Poll();
            var idleDt = Mathf.Min(rawDt, 0.05f);
            feedbackTimer = Mathf.Max(0f, feedbackTimer - idleDt);
            character.Animate(idleDt, false, motor.Grounded, false, gameOver, 0f, 0f, world.DashTimer, world.MagnetTimer, world.ShieldTimer, world.HitInvulnerabilityTimer);
            runnerAudio.UpdateRunState(0f, false, false, false, false);

            if (action != RunnerAction.None)
            {
                ResetRun(true);
                feedbackText = "出发";
                feedbackTimer = 0.8f;
            }
        }

        public float TickRunningSimulation(float rawDt)
        {
            var action = input.Poll();
            // Pause 输入只在运行中处理
            if (action == RunnerAction.Pause)
            {
                TogglePause();
                action = RunnerAction.None;
            }
            var dt = Mathf.Min(rawDt, 0.05f);
            manualDashCooldownTimer = Mathf.Max(0f, manualDashCooldownTimer - dt);
            if (world.Paused)
            {
                pauseHoldUI = Mathf.Max(0f, pauseHoldUI - dt);
                feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);
                character.Animate(dt, false, motor.Grounded, false, gameOver, 0f, 0f,
                    world.DashTimer, world.MagnetTimer, world.ShieldTimer, world.HitInvulnerabilityTimer);
                runnerAudio.UpdateRunState(0f, false, false, false, false);
                return 0f;
            }

            if (waitingForGuidedReaction)
            {
                if (IsReactionAction(action))
                {
                    waitingForGuidedReaction = false;
                    // 解除教学暂停后，让提示再停留一小会，避免瞬间消失。
                    if (feedbackTimer > 3f)
                    {
                        feedbackTimer = 1.1f;
                    }
                }
                else
                {
                    action = RunnerAction.None;
                }
            }

            if (waitingForGuidedReaction)
            {
                feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);
                character.Animate(dt, running, motor.Grounded, motor.SlideTimer > 0f, gameOver, 0f, 0f,
                    world.DashTimer, world.MagnetTimer, world.ShieldTimer, world.HitInvulnerabilityTimer);
                runnerAudio.UpdateRunState(0f, running, world.DashTimer > 0f, world.MagnetTimer > 0f, world.ShieldTimer > 0f);
                return 0f;
            }

            var effectiveSpeed = world.Tick(dt);
            var groundY = TrackTerrain.SurfaceY(playerTransform.position.z, world.Distance, tuning.movement.groundY);
            if (action == RunnerAction.Dash)
            {
                TriggerManualDash(ref feedbackText, ref feedbackTimer);
                action = RunnerAction.None;
            }
            motor.ApplyAction(action, ref feedbackText, ref feedbackTimer, world.GlideTimer, groundY);
            motor.Tick(dt, tuning.movement.laneWidth, tuning.movement.laneLerp, ref feedbackText, ref feedbackTimer, world.GlideTimer, groundY);

            feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);

            var speed01 = world.Speed01();
            character.Animate(
                dt,
                running,
                motor.Grounded,
                motor.SlideTimer > 0f,
                gameOver,
                speed01,
                motor.LaneLean01,
                world.DashTimer,
                world.MagnetTimer,
                world.ShieldTimer,
                world.HitInvulnerabilityTimer);
            runnerAudio.UpdateRunState(speed01, running, world.DashTimer > 0f, world.MagnetTimer > 0f, world.ShieldTimer > 0f);

            return effectiveSpeed;
        }

        public void TickRunningWorld(float dt, float effectiveSpeed)
        {
            if (world.Paused) return;
            spawner.SpawnPaused = bossSystem.ShouldPauseSpawning;
            spawner.Tick(dt, effectiveSpeed, world.Distance);
            var groundY = TrackTerrain.SurfaceY(playerTransform.position.z, world.Distance, tuning.movement.groundY);
            pickups.CollectCoins(playerTransform, motor.PlayerY, world, spawner.coins, ref feedbackText, ref feedbackTimer);
            pickups.CollectPowerUps(playerTransform, world, spawner.powerUps, ref feedbackText, ref feedbackTimer, pulseHaptic);

            // ── Boss 战每帧逻辑 ─────────────────────────────────
            var px = playerTransform.position.x;
            var py = motor.PlayerY;
            var prevBossActive = bossSystem.BossActive;
            var bossHitsPlayer = bossSystem.Tick(dt, world.Distance, px, py, groundY,
                motor.SlideTimer > 0f, world.DashTimer, world.ShieldTimer,
                ref feedbackText, ref feedbackTimer);
            if (bossSystem.TryConsumeGuidancePauseRequest())
            {
                waitingForGuidedReaction = true;
                // 等待玩家操作期间固定展示引导提示。
                feedbackTimer = Mathf.Max(feedbackTimer, 999f);
            }

            if (prevBossActive && !bossSystem.BossActive)
            {
                // boss 战刚结束，发奖励（boss 内部已在击败时设置标记）
                if (bossSystem.TryConsumeDefeatReward(out var fishBonus, out var scoreBonus,
                        out var speedTier, out var speedFish, out var speedScore))
                {
                    world.AddBossReward(fishBonus, scoreBonus);
                    world.SetBossSpeedResult(speedTier, speedFish, speedScore);

                    // 生成 Boss 战后奖励段
                    var lastDef = bossSystem.LastDefeatedBoss;
                    if (lastDef != null)
                    {
                        spawner.SpawnBossReward(world.Distance, lastDef);
                    }
                }
            }

            // Boss 即将出现前，生成前奏片段
            if (bossSystem.IsBossApproaching(world.Distance, 100f) && !bossSystem.HasSpawnedPrelude)
            {
                bossSystem.MarkPreludeSpawned();
                var nextDef = bossSystem.NextBossDefinition;
                if (nextDef != null)
                {
                    spawner.SpawnBossPrelude(world.Distance, nextDef);
                }
            }

            if (bossHitsPlayer && world.HitInvulnerabilityTimer <= 0f)
            {
                if (world.TryConsumeFishBomb())
                {
                    world.SetHitInvulnerability(1.2f);
                    feedbackText = "鱼弹反弹 BOSS 攻击！";
                    feedbackTimer = 1f;
                }
                else
                {
                    world.SetHitInvulnerability(2f);
                    if (world.LoseOneLife())
                    {
                        feedbackText = "被 BOSS 击倒";
                        feedbackTimer = 1.4f;
                        runnerAudio.PlayCrash();
                        pulseHaptic?.Invoke();
                        running = false;
                        gameOver = true;
                        return;
                    }
                }
            }

            if (collisions.CheckCollisions(
                    playerTransform,
                    motor.PlayerY,
                    groundY,
                    motor.SlideTimer,
                    world,
                    spawner,
                    spawner.obstacles,
                    spawner.obstacleSpecs,
                    ref feedbackText,
                    ref feedbackTimer,
                    pulseHaptic,
                    motor))
            {
                running = false;
                gameOver = true;
            }
        }

        private static bool IsReactionAction(RunnerAction action)
        {
            return action == RunnerAction.Jump ||
                   action == RunnerAction.Slide ||
                   action == RunnerAction.Left ||
                   action == RunnerAction.Right ||
                   action == RunnerAction.Dash ||
                   action == RunnerAction.Start;
        }

        private void TriggerManualDash(ref string feedbackText, ref float feedbackTimer)
        {
            if (manualDashCooldownTimer > 0f)
            {
                feedbackText = $"冲刺冷却 {manualDashCooldownTimer:0.0}s";
                feedbackTimer = 0.45f;
                return;
            }

            const float touchDashSeconds = 1.2f;
            world.DashTimer = Mathf.Max(world.DashTimer, touchDashSeconds);
            manualDashCooldownTimer = 2.5f;
            feedbackText = "双击冲刺！";
            feedbackTimer = 0.65f;
        }
    }
}
