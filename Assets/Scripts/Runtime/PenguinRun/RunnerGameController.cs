using PenguinRun.Core;
using PenguinRun.Game;
using PenguinRun.Game.Shop;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>开局倒计时 overlay 子阶段（FSM Countdown 内）。</summary>
    public enum EntrancePhase
    {
        None,
        Numbers,
        GoText,
    }

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
        private readonly System.Action<Color, float> onScreenFlash;
        private readonly System.Action<RunnerRunResult> onFinish;

        private readonly GameStateMachine stateMachine = new();
        private string feedbackText = "";
        private float feedbackTimer;
        private bool finishRequested;
        private float pauseHoldUI;
        private bool waitingForGuidedReaction;
        private float manualDashCooldownTimer;
        private int lastLifeCount = 3;
        private bool entranceSkipped;
        private EntrancePhase entrancePhase;
        private float entranceTimer;
        private int countdownNumber;

        public RunnerGameController(
            RunnerTuning tuning,
            RunnerSessionConfig config,
            RunnerAudio runnerAudio,
            Transform playerTransform,
            PenguinCharacter character,
            Transform sceneRoot,
            System.Action pulseHaptic,
            System.Action<Color, float> onScreenFlash,
            System.Action<RunnerRunResult> onFinish)
        {
            this.tuning = tuning;
            this.config = config;
            this.runnerAudio = runnerAudio;
            this.playerTransform = playerTransform;
            this.character = character;
            this.sceneRoot = sceneRoot;
            this.pulseHaptic = pulseHaptic;
            this.onScreenFlash = onScreenFlash;
            this.onFinish = onFinish;

            var waveDirector = new SegmentWaveDirector(config.MapTheme);
            spawner = new SegmentSpawner(tuning.world, tuning.powerUp, tuning.movement.laneWidth, config, waveDirector, tuning.movement.groundY);
            motor = new PlayerMotor(playerTransform, runnerAudio, tuning.movement, tuning.powerUp, tuning.combat);
            world = new WorldDirector(tuning.world, tuning.pickup);
            pickups = new PickupSystem(tuning.pickup, tuning.powerUp, config);
            collisions = new CollisionSystem(runnerAudio, tuning.combat);
            bossSystem = new BossSystem(runnerAudio, config, sceneRoot, tuning.movement.groundY, tuning.movement.laneWidth, tuning.combat);
            stateMachine.OnStateChanged += HandleStateChanged;
        }

        /// <summary>开局前（Idle / 倒计时 / 加载），未进入跑酷模拟。</summary>
        public bool IsPreRun =>
            stateMachine.Current == GameStateMachine.State.Idle ||
            stateMachine.Current == GameStateMachine.State.Countdown ||
            stateMachine.Current == GameStateMachine.State.Loading;

        /// <summary>本局跑酷会话进行中（含 Boss、暂停、主题过渡、死亡序列）。</summary>
        public bool Running => !IsPreRun && stateMachine.Current != GameStateMachine.State.Finished;

        public bool GameOver => stateMachine.IsGameOver;

        public bool IsCountdown => stateMachine.Current == GameStateMachine.State.Countdown;

        public EntrancePhase EntrancePhase => entrancePhase;

        public int CountdownNumber => countdownNumber;

        public float EntranceTimer => entranceTimer;

        public bool IsThemeTransition => stateMachine.Current == GameStateMachine.State.ThemeTransition;

        /// <summary>死亡演出：冻结 + 红屏（FSM DeathSequence）。</summary>
        public bool IsDeathSequence => stateMachine.Current == GameStateMachine.State.DeathSequence;

        /// <summary>死亡结算 overlay（FSM GameOver，等待点击）。</summary>
        public bool IsDeathResult => stateMachine.Current == GameStateMachine.State.GameOver;

        /// <summary>任意死亡呈现阶段（DeathSequence 或 GameOver）。</summary>
        public bool IsDeathPresentation => IsDeathSequence || IsDeathResult;

        public GameStateMachine.State CurrentState => stateMachine.Current;

        public WorldDirector World => world;

        public SegmentSpawner Spawner => spawner;

        public PlayerMotor Motor => motor;

        public BossSystem Boss => bossSystem;

        public RunnerAudio RunnerAudio => runnerAudio;

        public string FeedbackText { get => feedbackText; set => feedbackText = value; }

        public float FeedbackTimer { get => feedbackTimer; set => feedbackTimer = value; }

        public void ResetRun(bool start)
        {
            spawner.Reset();
            motor.ResetToStart();
            world.ResetRun();
            if (start && ShopStore.TryConsumePendingLuckyStartForRunStart())
                world.AddExtraLife();
            bossSystem.Reset();
            collisions.Reset();
            stateMachine.ForceTransitionTo(start ? GameStateMachine.State.Running : GameStateMachine.State.Idle);
            finishRequested = false;
            entranceSkipped = false;
            entrancePhase = EntrancePhase.None;
            entranceTimer = 0f;
            countdownNumber = 3;
            feedbackText = "";
            feedbackTimer = 0f;
            waitingForGuidedReaction = false;
            manualDashCooldownTimer = 0f;
            lastLifeCount = world.Lives;

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

        /// <summary>开局前 tick：自动启动倒计时或 Idle 等待点击。</summary>
        public void TickPreRun(float rawDt)
        {
            if (stateMachine.Current == GameStateMachine.State.Idle)
            {
                if (!entranceSkipped)
                {
                    BeginEntranceCountdown();
                }

                if (stateMachine.Current == GameStateMachine.State.Countdown)
                {
                    TickEntranceCountdown(rawDt);
                }
                else
                {
                    TickIdle(rawDt);
                }

                return;
            }

            if (stateMachine.Current == GameStateMachine.State.Countdown)
            {
                TickEntranceCountdown(rawDt);
            }
        }

        private void TickEntranceCountdown(float rawDt)
        {
            var action = input.Poll();
            if (action != RunnerAction.None)
            {
                entrancePhase = EntrancePhase.None;
                entranceTimer = 0f;
                ResetRun(true);
                feedbackText = "出发";
                feedbackTimer = 0.8f;
                return;
            }

            var dt = Mathf.Min(rawDt, 0.05f);
            feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);
            character.Animate(dt, false, motor.Grounded, false, false, 0f, 0f,
                world.DashTimer, world.MagnetTimer, world.ShieldTimer, world.HitInvulnerabilityTimer);
            runnerAudio.UpdateRunState(0f, false, false, false, false);

            if (entrancePhase == EntrancePhase.None)
            {
                entrancePhase = EntrancePhase.Numbers;
                entranceTimer = 0f;
                countdownNumber = 3;
            }

            entranceTimer += dt;
            if (entrancePhase == EntrancePhase.Numbers)
            {
                var step = Mathf.FloorToInt(entranceTimer / 0.7f);
                var num = 3 - step;
                if (num <= 0)
                {
                    entrancePhase = EntrancePhase.GoText;
                    entranceTimer = 0f;
                    countdownNumber = 0;
                }
                else
                {
                    countdownNumber = num;
                }
            }
            else if (entrancePhase == EntrancePhase.GoText && entranceTimer >= 0.6f)
            {
                entrancePhase = EntrancePhase.None;
                entranceTimer = 0f;
                ResetRun(true);
                feedbackText = "出发！";
                feedbackTimer = 0.8f;
            }
        }

        /// <summary>开局 3-2-1 倒计时开始（Idle → Countdown）。</summary>
        public void BeginEntranceCountdown()
        {
            if (stateMachine.Current != GameStateMachine.State.Idle) return;
            entrancePhase = EntrancePhase.None;
            entranceTimer = 0f;
            countdownNumber = 3;
            stateMachine.TransitionTo(GameStateMachine.State.Countdown);
        }

        /// <summary>跳过倒计时 overlay，回到 Idle 等待点击开始。</summary>
        public void SkipEntranceCountdown()
        {
            entranceSkipped = true;
            entrancePhase = EntrancePhase.None;
            entranceTimer = 0f;
            if (stateMachine.Current == GameStateMachine.State.Countdown)
            {
                stateMachine.ForceTransitionTo(GameStateMachine.State.Idle);
            }
        }

        /// <summary>尝试进入主题切换过渡（Running → ThemeTransition）。</summary>
        public bool TryBeginThemeTransition()
        {
            if (stateMachine.Current != GameStateMachine.State.Running) return false;
            if (bossSystem.BossActive) return false;
            return stateMachine.TransitionTo(GameStateMachine.State.ThemeTransition);
        }

        /// <summary>主题切换完成，恢复跑酷（ThemeTransition → Running）。</summary>
        public void CompleteThemeTransition()
        {
            if (stateMachine.Current != GameStateMachine.State.ThemeTransition) return;
            stateMachine.TransitionTo(GameStateMachine.State.Running);
        }

        /// <summary>死亡演出结束，进入结算 overlay（DeathSequence → GameOver）。</summary>
        public void AdvanceDeathToResult()
        {
            if (stateMachine.Current != GameStateMachine.State.DeathSequence) return;
            stateMachine.TransitionTo(GameStateMachine.State.GameOver);
        }

        /// <summary>结束本局并把结果回调给 PenguinRunnerGame（再由它走 RunOutcomeRouter + 场景切换）。</summary>
        public void FinishRun()
        {
            if (finishRequested) return;
            finishRequested = true;
            stateMachine.ForceTransitionTo(GameStateMachine.State.Finished);
            var dist = Mathf.Max(0, Mathf.RoundToInt(world.Distance));
            var result = new RunnerRunResult(world.Score, dist, world.Coins, world.RunTime,
                world.LastBossSpeedTier, world.LastBossSpeedFishBonus, world.LastBossSpeedScoreBonus);
            onFinish?.Invoke(result);
        }

        public void TogglePause()
        {
            if (GameOver || !Running || IsThemeTransition) return;
            var cur = stateMachine.Current;
            if (cur != GameStateMachine.State.Running &&
                cur != GameStateMachine.State.BossFight &&
                cur != GameStateMachine.State.Paused)
            {
                return;
            }

            world.Paused = !world.Paused;
            runnerAudio.PlayPause();
            feedbackText = world.Paused ? "已暂停" : "继续";
            feedbackTimer = 0.7f;
            pauseHoldUI = world.Paused ? 0.3f : 0f;

            if (world.Paused)
            {
                stateMachine.TransitionTo(GameStateMachine.State.Paused);
            }
            else
            {
                stateMachine.TransitionTo(GameStateMachine.State.Running);
                SyncBossFightState();
            }
        }

        public bool IsPaused => world.Paused;
        public bool IsSimulationFrozen => world.Paused || waitingForGuidedReaction;

        public float PauseUiHold => pauseHoldUI;

        public void TickIdle(float rawDt)
        {
            var action = input.Poll();
            var idleDt = Mathf.Min(rawDt, 0.05f);
            feedbackTimer = Mathf.Max(0f, feedbackTimer - idleDt);
            character.Animate(idleDt, false, motor.Grounded, false, GameOver, 0f, 0f, world.DashTimer, world.MagnetTimer, world.ShieldTimer, world.HitInvulnerabilityTimer);
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
                character.Animate(dt, false, motor.Grounded, false, GameOver, 0f, 0f,
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
                character.Animate(dt, stateMachine.IsRunning, motor.Grounded, motor.SlideTimer > 0f, GameOver, 0f, 0f,
                    world.DashTimer, world.MagnetTimer, world.ShieldTimer, world.HitInvulnerabilityTimer);
                runnerAudio.UpdateRunState(0f, stateMachine.IsRunning, world.DashTimer > 0f, world.MagnetTimer > 0f, world.ShieldTimer > 0f);
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
                stateMachine.IsRunning,
                motor.Grounded,
                motor.SlideTimer > 0f,
                GameOver,
                speed01,
                motor.LaneLean01,
                world.DashTimer,
                world.MagnetTimer,
                world.ShieldTimer,
                world.HitInvulnerabilityTimer);
            runnerAudio.UpdateRunState(speed01, stateMachine.IsRunning, world.DashTimer > 0f, world.MagnetTimer > 0f, world.ShieldTimer > 0f);

            return effectiveSpeed;
        }

        public void TickRunningWorld(float dt, float effectiveSpeed)
        {
            if (world.Paused) return;
            spawner.SpawnPaused = bossSystem.ShouldPauseSpawning;
            spawner.Tick(dt, effectiveSpeed, world.Distance);
            TickSegmentBanner(dt);

            if (spawner.TryConsumeSlideGateTutorial(out var slideTutorial))
            {
                feedbackText = slideTutorial;
                feedbackTimer = 2.4f;
            }

            // 速度门检测
            if (spawner.CheckSpeedGatePass(playerTransform, motor.PlayerY, out var gateFeedback))
            {
                world.DashTimer = Mathf.Max(world.DashTimer, 0.8f);
                feedbackText = gateFeedback;
                feedbackTimer = 0.7f;
            }
            var groundY = TrackTerrain.SurfaceY(playerTransform.position.z, world.Distance, tuning.movement.groundY);
            var invulnBeforeCollision = world.HitInvulnerabilityTimer;

            // 距离里程碑奖励
            var milestoneReward = world.TryHitDistanceMilestone();
            if (milestoneReward > 0)
            {
                feedbackText = $"🎯 {world.DistanceMilestonesHit * 500}m 里程碑！+{milestoneReward} 鱼干";
                feedbackTimer = 1.5f;
                runnerAudio.PlayCheckpoint();
            }

            pickups.CollectCoins(playerTransform, motor.PlayerY, world, spawner.coins, ref feedbackText, ref feedbackTimer);
            pickups.CollectPowerUps(playerTransform, world, spawner.powerUps, ref feedbackText, ref feedbackTimer);

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

            // Boss 即将出现前，生成前奏片段 + 预警提示
            if (bossSystem.IsBossApproaching(world.Distance, 100f) && !bossSystem.HasSpawnedPrelude)
            {
                bossSystem.MarkPreludeSpawned();
                var nextDef = bossSystem.NextBossDefinition;
                if (nextDef != null)
                {
                    spawner.SpawnBossPrelude(world.Distance, nextDef);
                    feedbackText = $"⚠ 前方高能：{nextDef.DisplayName} 即将出现！\n{nextDef.SignatureMechanic}";
                    feedbackTimer = 3.0f;
                    EmitRunFeedback(RunFeedbackKind.BossWarning);
                }
            }

            if (bossHitsPlayer && world.HitInvulnerabilityTimer <= 0f)
            {
                var hadShield = world.ShieldTimer > 0f;
                var outcome = RunnerHitResolver.TryResolveBossHit(world, tuning.combat, hadShield);
                if (!string.IsNullOrEmpty(outcome.FeedbackText))
                {
                    feedbackText = outcome.FeedbackText;
                    feedbackTimer = outcome.FeedbackDuration;
                }

                if (outcome.InvulnerabilitySeconds > 0f)
                    world.SetHitInvulnerability(outcome.InvulnerabilitySeconds);

                if (outcome.Kind == HitOutcomeKind.BlockedByDefense)
                {
                    EmitRunFeedback(RunFeedbackKind.DefenseBlock);
                }
                else if (outcome.Kind == HitOutcomeKind.GameOver)
                {
                    EmitRunFeedback(RunFeedbackKind.BossDeath);
                    EnterDeathSequence();
                    return;
                }
                else if (outcome.Kind == HitOutcomeKind.Damage)
                {
                    character.TriggerHurtFlash();
                    EventBus.Emit(GameEvents.PlayerHurt);
                    pulseHaptic?.Invoke();
                    EmitRunFeedback(RunFeedbackKind.BossHit);
                }
            }

            // Boss 渐进式临近预警：距离越近，警告越强
            if (!bossSystem.BossActive && !bossSystem.HasSpawnedPrelude)
            {
                for (var tier = 0; tier < 4; tier++)
                {
                    var dist = 100f - tier * 20f; // 100, 80, 60, 40m
                    if (bossSystem.IsBossApproaching(world.Distance, dist))
                    {
                        var intensity = 0.04f + tier * 0.04f;
                        onScreenFlash?.Invoke(new Color(0.7f, 0.05f, 0.02f, intensity), 0.2f);
                        break;
                    }
                }
            }

            var gameOverNow = collisions.CheckCollisions(
                    playerTransform,
                    motor.PlayerY,
                    groundY,
                    motor.SlideTimer,
                    world.DashTimer,
                    world,
                    spawner,
                    spawner.obstacles,
                    spawner.obstacleSpecs,
                    ref feedbackText,
                    ref feedbackTimer,
                    pulseHaptic,
                    motor,
                    () => EmitRunFeedback(RunFeedbackKind.PerfectDodge));

            if (world.Lives < lastLifeCount)
            {
                character.TriggerHurtFlash();
                lastLifeCount = world.Lives;
            }

            // 非致命伤害闪屏
            if (!gameOverNow && world.HitInvulnerabilityTimer > invulnBeforeCollision + 0.01f)
            {
                EmitRunFeedback(RunFeedbackKind.ObstacleDamage);
            }

            SyncBossFightState();

            if (gameOverNow)
            {
                EmitRunFeedback(RunFeedbackKind.ObstacleDeath);
                EnterDeathSequence();
            }
        }

        public float SegmentBannerTimer => segmentBannerTimer;
        public SegmentPatternKind SegmentBannerKind => segmentBannerKind;

        private float segmentBannerTimer;
        private SegmentPatternKind segmentBannerKind;

        private void TickSegmentBanner(float dt)
        {
            segmentBannerTimer = Mathf.Max(0f, segmentBannerTimer - dt);
            if (!spawner.TryConsumeSegmentBanner(out var kind)) return;
            segmentBannerKind = kind;
            segmentBannerTimer = 1.2f;
            EmitRunFeedback(kind == SegmentPatternKind.HazardRhythm
                ? RunFeedbackKind.SegmentHazard
                : RunFeedbackKind.SegmentReward);
        }

        private void EmitRunFeedback(RunFeedbackKind kind)
        {
            switch (kind)
            {
                case RunFeedbackKind.PerfectDodge:
                    onScreenFlash?.Invoke(new Color(1f, 0.85f, 0.2f, 0.22f), 0.25f);
                    character.TriggerPerfectDodgeBurst();
                    break;
                case RunFeedbackKind.ObstacleDamage:
                    onScreenFlash?.Invoke(new Color(1f, 0.35f, 0.2f, 0.4f), 0.28f);
                    break;
                case RunFeedbackKind.ObstacleDeath:
                    onScreenFlash?.Invoke(new Color(1f, 0.1f, 0.05f, 0.65f), 0.45f);
                    break;
                case RunFeedbackKind.BossWarning:
                    onScreenFlash?.Invoke(new Color(0.8f, 0.45f, 0.08f, 0.15f), 0.4f);
                    break;
                case RunFeedbackKind.BossHit:
                    onScreenFlash?.Invoke(new Color(1f, 0.2f, 0.15f, 0.55f), 0.35f);
                    break;
                case RunFeedbackKind.BossDeath:
                    onScreenFlash?.Invoke(new Color(1f, 0.05f, 0.02f, 0.7f), 0.6f);
                    runnerAudio.PlayCrash();
                    pulseHaptic?.Invoke();
                    break;
                case RunFeedbackKind.DefenseBlock:
                    onScreenFlash?.Invoke(new Color(0.4f, 0.85f, 1f, 0.28f), 0.22f);
                    break;
                case RunFeedbackKind.SegmentHazard:
                    onScreenFlash?.Invoke(new Color(1f, 0.55f, 0.35f, 0.12f), 0.35f);
                    break;
                case RunFeedbackKind.SegmentReward:
                    onScreenFlash?.Invoke(new Color(1f, 0.85f, 0.4f, 0.12f), 0.35f);
                    break;
            }
        }

        private void EnterDeathSequence()
        {
            if (stateMachine.Current == GameStateMachine.State.DeathSequence) return;
            if (!stateMachine.TransitionTo(GameStateMachine.State.DeathSequence))
            {
                stateMachine.ForceTransitionTo(GameStateMachine.State.DeathSequence);
            }
        }

        private void SyncBossFightState()
        {
            if (GameOver ||
                stateMachine.Current == GameStateMachine.State.Paused ||
                stateMachine.Current == GameStateMachine.State.DeathSequence ||
                stateMachine.Current == GameStateMachine.State.ThemeTransition)
            {
                return;
            }

            if (bossSystem.BossActive && stateMachine.Current == GameStateMachine.State.Running)
            {
                stateMachine.TransitionTo(GameStateMachine.State.BossFight);
            }
            else if (!bossSystem.BossActive && stateMachine.Current == GameStateMachine.State.BossFight)
            {
                stateMachine.TransitionTo(GameStateMachine.State.Running);
            }
        }

        private void HandleStateChanged(GameStateMachine.State from, GameStateMachine.State to)
        {
            switch (to)
            {
                case GameStateMachine.State.Running:
                    switch (from)
                    {
                        case GameStateMachine.State.Idle:
                        case GameStateMachine.State.Countdown:
                            EventBus.Emit(GameEvents.GameStarted);
                            break;
                        case GameStateMachine.State.Paused:
                            EventBus.Emit(GameEvents.GameResumed);
                            break;
                        case GameStateMachine.State.ThemeTransition:
                            EventBus.Emit(GameEvents.ThemeChanged);
                            break;
                    }
                    break;
                case GameStateMachine.State.Paused:
                    EventBus.Emit(GameEvents.GamePaused);
                    break;
                case GameStateMachine.State.ThemeTransition:
                    EventBus.Emit(GameEvents.SceneTransitionStart);
                    break;
                case GameStateMachine.State.GameOver:
                    EventBus.Emit(GameEvents.GameOver);
                    break;
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
