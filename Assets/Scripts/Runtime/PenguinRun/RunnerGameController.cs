using PenguinRun.Core;
using PenguinRun.Game;
using PenguinRun.Game.Growth;
using PenguinRun.Game.Save;
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
        private readonly IPlayerCharacterPresentation character;
        private readonly Transform playerTransform;
        private readonly SegmentSpawner spawner;
        private readonly PlayerMotor motor;
        private readonly WorldDirector world;
        private readonly PickupSystem pickups;
        private readonly CollisionSystem collisions;
        private readonly BossSystem bossSystem;
        private readonly RouteEventDirector routeEventDirector;
        private readonly RunUpgradeState runUpgradeState;
        private readonly SkillScoreTracker skillScoreTracker = new();
        private readonly MapMechanicDirector mechanicDirector;
        private readonly ThemeMechanicRuntime mechanicRuntime;
        private readonly ThemeMechanicSpawner mechanicSpawner;
        private readonly ThemeMasteryTracker themeMastery;

        // 每帧传给 CollectCoins 的两个回调。捕获 this 的 lambda 与实例方法组都不会被
        // 编译器缓存，写在调用点上等于每帧各新建一个委托；这里建一次复用整局。
        private readonly System.Action onAirCollection;
        private readonly System.Action<int> onFishCollected;
        private readonly OnboardingState onboarding;
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
        private bool upgradeOfferPending;
        private bool segmentTrackingActive;
        private bool segmentDamaged;
        private EntrancePhase entrancePhase;
        private float entranceTimer;
        private int countdownNumber;
        private int mechanicKeypointIndex;
        private RunnerAction lastRunnerAction;
        private float secondsSinceDamage = 99f;
        private int lastCoinCountForOnboarding;

        public RunnerGameController(
            RunnerTuning tuning,
            RunnerSessionConfig config,
            RunnerAudio runnerAudio,
            Transform playerTransform,
            IPlayerCharacterPresentation character,
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

            onboarding = OnboardingState.LoadFromSave();
            var waveDirector = new SegmentWaveDirector(config.MapTheme, config.IsOnboarding);
            spawner = new SegmentSpawner(tuning.world, tuning.powerUp, tuning.movement.laneWidth, config, waveDirector, tuning.movement.groundY);
            routeEventDirector = new RouteEventDirector(config.MapTheme, config.Daily, PlayerSave.TotalRuns, config.IsOnboarding);
            runUpgradeState = new RunUpgradeState(config.MapTheme, config.Daily, PlayerSave.TotalRuns);
            motor = new PlayerMotor(playerTransform, runnerAudio, tuning.movement, tuning.powerUp, tuning.combat);
            world = new WorldDirector(tuning.world, tuning.pickup);
            mechanicDirector = new MapMechanicDirector(config.ThemeMechanicMode, config.RunSeed, config.IsOnboarding);
            mechanicRuntime = new ThemeMechanicRuntime();
            mechanicSpawner = new ThemeMechanicSpawner(
                sceneRoot, tuning.movement.laneWidth, tuning.movement.groundY);
            themeMastery = new ThemeMasteryTracker(
                config.MapTheme, config.ThemeMechanicMode, config.RunSeed,
                ThemeMasteryStore.For(config.MapTheme).TutorialSeen);
            onAirCollection = () => AwardSkill(SkillAction.AirCollection, 60);
            onFishCollected = themeMastery.RecordFish;
            pickups = new PickupSystem(tuning.pickup, tuning.powerUp, config, runUpgradeState);
            collisions = new CollisionSystem(runnerAudio, tuning.combat, runUpgradeState);
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

        public void TickAvatarPresentation(float deltaTime, bool running, bool gameOver)
        {
            TickCharacter(deltaTime, running, motor.Grounded, motor.SlideTimer > 0f,
                gameOver, world.Speed01(), motor.LaneLean01);
        }

        public BossSystem Boss => bossSystem;

        public RouteEventSnapshot RouteEvent => routeEventDirector.Snapshot;

        public RunUpgradeState RunUpgrades => runUpgradeState;

        public SkillScoreSnapshot SkillScore => skillScoreTracker.Snapshot;

        public ThemeMechanicSnapshot ThemeMechanic => mechanicRuntime.Snapshot;

        public ThemeMasteryHudSnapshot ThemeMasteryHud => themeMastery.HudSnapshot;

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
            routeEventDirector.Reset();
            runUpgradeState.Reset();
            skillScoreTracker.Reset();
            themeMastery.Reset();
            mechanicDirector.Reset();
            mechanicRuntime.Cancel(ThemeMechanicCancelReason.ThemeTransition);
            mechanicRuntime.Tick(0f);
            mechanicSpawner.Clear();
            spawner.ClearMechanicVisuals();
            mechanicKeypointIndex = 0;
            lastRunnerAction = RunnerAction.None;
            secondsSinceDamage = 99f;
            collisions.Reset();
            stateMachine.ForceTransitionTo(start ? GameStateMachine.State.Running : GameStateMachine.State.Idle);
            finishRequested = false;
            upgradeOfferPending = false;
            segmentTrackingActive = false;
            segmentDamaged = false;
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
                Random.InitState(config.RunSeed);
            }

            lastCoinCountForOnboarding = world.Coins;
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
            TickCharacter(dt, false, motor.Grounded, false, false, 0f, 0f);
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

        public RunnerRunResult CaptureResult()
        {
            var dist = Mathf.Max(0, Mathf.RoundToInt(world.Distance));
            var skill = skillScoreTracker.Snapshot;
            return new RunnerRunResult(world.Score, dist, world.Coins, world.RunTime,
                world.LastBossSpeedTier, world.LastBossSpeedFishBonus, world.LastBossSpeedScoreBonus,
                routeEventDirector.CompletedCount, routeEventDirector.RiskyCompletedCount,
                skill.TotalScore, skill.PeakChain, runUpgradeState.SelectedIds,
                routeEventDirector.CompletedEventIds,
                bossSystem.LastThemedRelicId,
                themeMastery.RunSnapshot);
        }

        /// <summary>结束本局并把结果回调给 PenguinRunnerGame（再由它走 RunOutcomeRouter + 场景切换）。</summary>
        public void FinishRun()
        {
            if (finishRequested) return;
            finishRequested = true;
            onboarding.Persist();
            PlayerSave.Flush();
            stateMachine.ForceTransitionTo(GameStateMachine.State.Finished);
            onFinish?.Invoke(CaptureResult());
        }

        public bool SetPaused(bool paused, bool playFeedback = true)
        {
            if (GameOver || !Running || IsThemeTransition) return false;
            var cur = stateMachine.Current;
            if (paused)
            {
                if (world.Paused ||
                    (cur != GameStateMachine.State.Running && cur != GameStateMachine.State.BossFight))
                    return false;
            }
            else if (!world.Paused || cur != GameStateMachine.State.Paused)
            {
                return false;
            }

            world.Paused = paused;
            if (playFeedback) runnerAudio.PlayPause();
            feedbackText = paused ? "已暂停" : "继续";
            feedbackTimer = 0.7f;
            pauseHoldUI = paused ? 0.3f : 0f;

            if (paused)
            {
                stateMachine.TransitionTo(GameStateMachine.State.Paused);
            }
            else
            {
                stateMachine.TransitionTo(GameStateMachine.State.Running);
                SyncBossFightState();
            }
            return true;
        }

        public void TogglePause() => SetPaused(!world.Paused);

        public bool IsPaused => world.Paused;
        public bool IsSimulationFrozen => world.Paused || waitingForGuidedReaction;

        public float PauseUiHold => pauseHoldUI;

        public void TickIdle(float rawDt)
        {
            var action = input.Poll();
            var idleDt = Mathf.Min(rawDt, 0.05f);
            feedbackTimer = Mathf.Max(0f, feedbackTimer - idleDt);
            TickCharacter(idleDt, false, motor.Grounded, false, GameOver, 0f, 0f);
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
                TickCharacter(dt, false, motor.Grounded, false, GameOver, 0f, 0f);
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
                TickCharacter(dt, stateMachine.IsRunning, motor.Grounded,
                    motor.SlideTimer > 0f, GameOver, 0f, 0f);
                runnerAudio.UpdateRunState(0f, stateMachine.IsRunning, world.DashTimer > 0f, world.MagnetTimer > 0f, world.ShieldTimer > 0f);
                return 0f;
            }

            skillScoreTracker.Tick(dt);
            themeMastery.Tick(dt);
            secondsSinceDamage += dt;
            var effectiveSpeed = world.Tick(dt);
            var groundY = TrackTerrain.SurfaceY(playerTransform.position.z, world.Distance, tuning.movement.groundY);
            if (action == RunnerAction.Dash)
            {
                TriggerManualDash(ref feedbackText, ref feedbackTimer);
                action = RunnerAction.None;
            }
            var laneBeforeAction = motor.Lane;
            var feedbackBeforeAction = feedbackText;
            var feedbackTimerBeforeAction = feedbackTimer;
            motor.ApplyAction(action, ref feedbackText, ref feedbackTimer, world.GlideTimer, groundY);
            lastRunnerAction = action;
            var laneChanged = motor.Lane != laneBeforeAction;
            if (laneChanged)
            {
                AwardSkill(SkillAction.LaneChain, 35);
            }
            if (action != RunnerAction.None)
            {
                onboarding.Record(action, laneChanged);
            }
            motor.Tick(dt, tuning.movement.laneWidth,
                tuning.movement.laneLerp * runUpgradeState.LaneResponseMultiplier,
                ref feedbackText, ref feedbackTimer, world.GlideTimer, groundY,
                mechanicRuntime.Snapshot.Modifiers);
            if (!config.IsOnboarding)
            {
                SuppressMotorActionLabels(ref feedbackText, ref feedbackTimer,
                    feedbackBeforeAction, feedbackTimerBeforeAction);
            }

            feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);

            var speed01 = world.Speed01();
            TickCharacter(dt, stateMachine.IsRunning, motor.Grounded,
                motor.SlideTimer > 0f, GameOver, speed01, motor.LaneLean01);
            runnerAudio.UpdateRunState(speed01, stateMachine.IsRunning, world.DashTimer > 0f, world.MagnetTimer > 0f, world.ShieldTimer > 0f);

            return effectiveSpeed;
        }

        public void TickRunningWorld(float dt, float effectiveSpeed)
        {
            if (world.Paused) return;
            CancelMechanicForWorldAuthority();
            var phaseBeforeTick = mechanicRuntime.Snapshot.Phase;
            mechanicRuntime.Tick(dt);
            if (phaseBeforeTick == ThemeMechanicPhase.Active
                && mechanicRuntime.Snapshot.Phase == ThemeMechanicPhase.Resolve)
            {
                ResolveThemeMechanic();
            }
            if (mechanicRuntime.Snapshot.Phase == ThemeMechanicPhase.Active)
            {
                mechanicSpawner.Tick(mechanicRuntime.PhaseElapsedSeconds, world.Distance);
                EvaluateMechanicKeypoints();
                if (mechanicSpawner.CriticalAssetMissing)
                {
                    mechanicRuntime.Cancel(ThemeMechanicCancelReason.MissingCriticalAsset);
                    mechanicSpawner.Clear();
                    spawner.ClearMechanicVisuals();
                }
            }
            var routeCanStart =
                stateMachine.Current == GameStateMachine.State.Running &&
                !bossSystem.BossActive &&
                !bossSystem.HasSpawnedPrelude &&
                !bossSystem.IsBossApproaching(world.Distance, 180f);
            if (routeEventDirector.AwaitingChoice)
            {
                routeEventDirector.Tick(world.Distance);
            }
            else if (routeCanStart && routeEventDirector.Tick(world.Distance))
            {
                if (!spawner.SpawnRouteEvent(routeEventDirector.Snapshot, world.Distance))
                {
                    routeEventDirector.Expire(world.Distance);
                }
            }

            spawner.SpawnPaused = bossSystem.ShouldPauseSpawning
                || routeEventDirector.AwaitingChoice
                || (mechanicRuntime.CurrentRequest.Signature
                    && mechanicRuntime.Snapshot.Phase == ThemeMechanicPhase.Active);
            spawner.Tick(dt, effectiveSpeed, world.Distance);
            var mechanicBeat = TickSegmentBanner(dt, out var segmentKind);
            if (mechanicBeat)
            {
                mechanicDirector.RecordBeat(
                    segmentKind == SegmentPatternKind.HazardRhythm
                        || segmentKind == SegmentPatternKind.EnemyAmbush,
                    segmentKind == SegmentPatternKind.RewardRun
                        || segmentKind == SegmentPatternKind.PowerUpTrial);
                TryStartThemeMechanic();
            }

            if (routeEventDirector.AwaitingChoice &&
                spawner.TryConsumeRouteChoice(motor.Lane, out var choseRisk))
            {
                ApplyRouteEventOutcome(routeEventDirector.Choose(choseRisk, world.Distance));
            }

            if (config.IsOnboarding)
            {
                if (spawner.TryConsumeSlideGateTutorial(out _)
                    && onboarding.TryConsumeSlideOpportunityHint(out var slideTutorial))
                {
                    feedbackText = slideTutorial;
                    feedbackTimer = 2.4f;
                }
                else if (spawner.TryConsumeOnboardingPatternHint(out var patternKind)
                         && onboarding.TryConsumePatternHint(patternKind, out var patternHint))
                {
                    feedbackText = patternHint;
                    feedbackTimer = 2.4f;
                }
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

            pickups.CollectCoins(playerTransform, motor.PlayerY, !motor.Grounded,
                onAirCollection,
                world, spawner.coins, ref feedbackText, ref feedbackTimer,
                onFishCollected);
            if (world.Coins > lastCoinCountForOnboarding)
            {
                onboarding.RecordCollect();
                lastCoinCountForOnboarding = world.Coins;
            }
            pickups.CollectPowerUps(playerTransform, world, spawner.powerUps, ref feedbackText, ref feedbackTimer);

            // ── Boss 战每帧逻辑 ─────────────────────────────────
            var px = playerTransform.position.x;
            var py = motor.PlayerY;
            var prevBossActive = bossSystem.BossActive;
            var bossHitsPlayer = bossSystem.Tick(dt, world.Distance, px, py, groundY,
                motor.SlideTimer > 0f, world.DashTimer, world.ShieldTimer,
                ref feedbackText, ref feedbackTimer);
            if (bossSystem.TryConsumeCounter())
            {
                AwardSkill(SkillAction.BossCounter, 250);
            }
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
                {
                    var shieldGrace = outcome.ConsumeShield ? runUpgradeState.ShieldGraceSeconds : 0f;
                    world.SetHitInvulnerability(outcome.InvulnerabilitySeconds + shieldGrace);
                }

                if (outcome.Kind == HitOutcomeKind.BlockedByDefense)
                {
                    EmitRunFeedback(RunFeedbackKind.DefenseBlock);
                }
                else if (outcome.Kind == HitOutcomeKind.GameOver)
                {
                    bossSystem.NotifyBossDamageTaken(true, ref feedbackText, ref feedbackTimer);
                    RecordThemeDamage();
                    BreakSkillChain();
                    EmitRunFeedback(RunFeedbackKind.BossDeath);
                    EnterDeathSequence();
                    return;
                }
                else if (outcome.Kind == HitOutcomeKind.Damage)
                {
                    bossSystem.NotifyBossDamageTaken(false, ref feedbackText, ref feedbackTimer);
                    RecordThemeDamage();
                    BreakSkillChain();
                    character.TriggerHurt();
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

            var livesBeforeCollision = world.Lives;
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
                    HandlePerfectDodge,
                    RecordThemeDamage);

            if (world.Lives < livesBeforeCollision)
            {
                BreakSkillChain();
            }

            if (world.Lives < lastLifeCount)
            {
                character.TriggerHurt();
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

        private void ApplyRouteEventOutcome(RouteEventResolution resolution)
        {
            if (!resolution.IsValid) return;

            var fishReward = Mathf.RoundToInt(
                resolution.Outcome.FishReward * runUpgradeState.FishRewardMultiplier);
            var scoreReward = Mathf.RoundToInt(
                resolution.Outcome.ScoreReward * runUpgradeState.RouteRewardMultiplier);
            world.AddRouteEventReward(fishReward, scoreReward);
            spawner.SpawnRouteEventOutcome(resolution.Outcome, resolution.Risky, world.Distance);
            if (resolution.Risky)
            {
                AwardSkill(SkillAction.RiskRoute, 140);
            }
            feedbackText = resolution.Risky
                ? $"冒险路线 · {resolution.Outcome.Label}  +{scoreReward}分"
                : $"安心路线 · {resolution.Outcome.Label}  +{fishReward}鱼干";
            feedbackTimer = 1.5f;
            runnerAudio.PlayCheckpoint();

            if (runUpgradeState.CanSelect &&
                (routeEventDirector.CompletedCount == 1 ||
                 (routeEventDirector.CompletedCount - 1) % 2 == 0))
            {
                upgradeOfferPending = true;
            }
        }

        private void CancelMechanicForWorldAuthority()
        {
            var activeBeforeCancel = mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Idle
                && mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Cancelled;
            if (bossSystem.BossActive)
            {
                mechanicRuntime.Cancel(ThemeMechanicCancelReason.Boss);
            }
            else if (routeEventDirector.AwaitingChoice)
            {
                mechanicRuntime.Cancel(ThemeMechanicCancelReason.RouteEvent);
            }
            else if (IsThemeTransition)
            {
                mechanicRuntime.Cancel(ThemeMechanicCancelReason.ThemeTransition);
            }

            if (activeBeforeCancel && mechanicRuntime.Snapshot.Phase == ThemeMechanicPhase.Cancelled)
            {
                themeMastery.CancelEvent();
                mechanicSpawner.Clear();
                spawner.ClearMechanicVisuals();
                mechanicKeypointIndex = 0;
            }
        }

        private void TryStartThemeMechanic()
        {
            if (mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Idle)
            {
                return;
            }

            var context = new ThemeMechanicContext(
                config.MapTheme,
                config.ThemeMechanicMode,
                world.Distance,
                world.Lives,
                secondsSinceDamage,
                bossSystem.BossActive,
                routeEventDirector.AwaitingChoice,
                IsThemeTransition,
                Mathf.Clamp01((int)config.Difficulty / 3f));
            if (!mechanicDirector.TrySchedule(context, out var request))
            {
                return;
            }

            var pattern = ThemeMechanicPatternCatalog.For(request.Definition.Id, request.Signature);
            if (pattern.Length == 0 || !mechanicRuntime.Begin(request))
            {
                return;
            }

            mechanicKeypointIndex = 0;
            mechanicSpawner.Begin(pattern, world.Distance);
            themeMastery.BeginEvent(pattern.Length, request.Signature);
            for (var index = 0; index < pattern.Length; index++)
            {
                var beat = pattern[index];
                spawner.SpawnMechanicObstacle(
                    beat.ApparatusPath,
                    beat.Lane,
                    30f + beat.Seconds * 6f,
                    world.Distance);
            }
        }

        private void EvaluateMechanicKeypoints()
        {
            var request = mechanicRuntime.CurrentRequest;
            var pattern = ThemeMechanicPatternCatalog.For(
                request.Definition.Id, request.Signature);
            while (mechanicKeypointIndex < pattern.Length
                && pattern[mechanicKeypointIndex].Seconds <= mechanicRuntime.PhaseElapsedSeconds)
            {
                var beat = pattern[mechanicKeypointIndex];
                var laneCorrect = motor.Lane == beat.Lane;
                var actionCorrect = (!beat.RequiresJump || !motor.Grounded || lastRunnerAction == RunnerAction.Jump)
                    && (!beat.RequiresSlide || motor.SlideTimer > 0f || lastRunnerAction == RunnerAction.Slide);
                var perfect = laneCorrect && actionCorrect;
                mechanicRuntime.RecordKeypoint(perfect);
                themeMastery.RecordKeypoint(perfect);
                if (perfect && beat.Rewarding)
                {
                    AwardSkill(SkillAction.NoHitSegment, 90);
                }
                mechanicKeypointIndex++;
            }
        }

        private void ResolveThemeMechanic()
        {
            var grade = themeMastery.ResolveEvent(mechanicRuntime.CurrentRequest.Signature);
            mechanicSpawner.Clear();
            spawner.ClearMechanicVisuals();
            mechanicKeypointIndex = 0;
            if (grade != ThemeMechanicGrade.None)
            {
                feedbackText = grade switch
                {
                    ThemeMechanicGrade.Flawless => "环境应对 · 无伤",
                    ThemeMechanicGrade.Perfect => "环境应对 · 完美",
                    _ => "环境应对 · 完成",
                };
                feedbackTimer = 1.2f;
            }
        }

        private void RecordThemeDamage()
        {
            secondsSinceDamage = 0f;
            themeMastery.RecordDamage();
        }

        public bool TryBuildUpgradeOffer(out RunUpgradeOffer offer)
        {
            if (config.IsOnboarding || !upgradeOfferPending)
            {
                offer = RunUpgradeOffer.Empty;
                return false;
            }

            offer = runUpgradeState.BuildOffer();
            upgradeOfferPending = false;
            return offer.Count == 3;
        }

        public bool SelectRunUpgrade(RunUpgradeId id)
        {
            if (!runUpgradeState.Select(id)) return false;

            var definition = RunUpgradeCatalog.For(id);
            feedbackText = $"札记生效 · {definition.Title}  {runUpgradeState.GetStack(id)}/{definition.MaxStacks}层";
            feedbackTimer = 1.8f;
            return true;
        }

        public float SegmentBannerTimer => segmentBannerTimer;
        public SegmentPatternKind SegmentBannerKind => segmentBannerKind;

        private float segmentBannerTimer;
        private SegmentPatternKind segmentBannerKind;

        private bool TickSegmentBanner(float dt, out SegmentPatternKind consumedKind)
        {
            segmentBannerTimer = Mathf.Max(0f, segmentBannerTimer - dt);
            if (!spawner.TryConsumeSegmentBanner(out var kind))
            {
                consumedKind = default;
                return false;
            }
            if (segmentTrackingActive && !segmentDamaged)
            {
                AwardSkill(SkillAction.NoHitSegment, 180);
            }
            segmentTrackingActive = true;
            segmentDamaged = false;
            segmentBannerKind = kind;
            segmentBannerTimer = 1.2f;
            EmitRunFeedback(kind == SegmentPatternKind.HazardRhythm
                ? RunFeedbackKind.SegmentHazard
                : RunFeedbackKind.SegmentReward);
            consumedKind = kind;
            return true;
        }

        private void HandlePerfectDodge()
        {
            AwardSkill(SkillAction.PerfectDodge,
                Mathf.RoundToInt(100f * runUpgradeState.PerfectDodgeScoreMultiplier));
            EmitRunFeedback(RunFeedbackKind.PerfectDodge);
        }

        private void AwardSkill(SkillAction action, int baseScore)
        {
            world.AddSkillScore(skillScoreTracker.Record(action, baseScore));
            themeMastery.RecordSkill(skillScoreTracker.Snapshot.TotalScore, world.Distance);
        }

        private void BreakSkillChain()
        {
            segmentDamaged = true;
            skillScoreTracker.BreakChain();
        }

        private void EmitRunFeedback(RunFeedbackKind kind)
        {
            switch (kind)
            {
                case RunFeedbackKind.PerfectDodge:
                    onScreenFlash?.Invoke(new Color(1f, 0.85f, 0.2f, 0.22f), 0.25f);
                    character.TriggerPerfectDodge();
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

        private static void SuppressMotorActionLabels(
            ref string feedbackText,
            ref float feedbackTimer,
            string previousText,
            float previousTimer)
        {
            if (!IsMotorActionLabel(feedbackText)) return;
            feedbackText = previousText;
            feedbackTimer = previousTimer;
        }

        private static bool IsMotorActionLabel(string text)
        {
            return text is "左移" or "右移" or "跳跃" or "滑铲" or "预输入滑铲" or "快速下落";
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

        private void TickCharacter(float deltaTime, bool running, bool grounded,
            bool sliding, bool gameOver, float speed01, float laneLean)
        {
            var buffs = RunnerAvatarBuffFlags.None;
            if (world.DashTimer > 0f) buffs |= RunnerAvatarBuffFlags.Dash;
            if (world.MagnetTimer > 0f) buffs |= RunnerAvatarBuffFlags.Magnet;
            if (world.ShieldTimer > 0f) buffs |= RunnerAvatarBuffFlags.Shield;
            if (world.HitInvulnerabilityTimer > 0f) buffs |= RunnerAvatarBuffFlags.Invulnerable;
            if (world.GlideTimer > 0f) buffs |= RunnerAvatarBuffFlags.Glide;
            if (world.ScoreBoostTimer > 0f) buffs |= RunnerAvatarBuffFlags.ScoreBoost;
            if (world.SlowMoTimer > 0f) buffs |= RunnerAvatarBuffFlags.SlowMo;
            if (world.DoubleFishTimer > 0f) buffs |= RunnerAvatarBuffFlags.DoubleFish;
            if (world.IceMirrorTimer > 0f) buffs |= RunnerAvatarBuffFlags.IceMirror;
            if (world.AuroraChainTimer > 0f) buffs |= RunnerAvatarBuffFlags.AuroraChain;
            if (world.FogLanternTimer > 0f) buffs |= RunnerAvatarBuffFlags.FogLantern;
            if (world.TreantArmorHits > 0) buffs |= RunnerAvatarBuffFlags.TreantArmor;
            if (world.CoralBounceReady) buffs |= RunnerAvatarBuffFlags.CoralBounce;
            if (world.ThunderFeatherTimer > 0f) buffs |= RunnerAvatarBuffFlags.ThunderFeather;
            if (world.FishBombs > 0) buffs |= RunnerAvatarBuffFlags.FishBomb;

            var state = new RunnerAvatarState(running, grounded, sliding, gameOver,
                speed01, laneLean, motor.VerticalVelocity, buffs);
            character.Tick(deltaTime, state);
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
