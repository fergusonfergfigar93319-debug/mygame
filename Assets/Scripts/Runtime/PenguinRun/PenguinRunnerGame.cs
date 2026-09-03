using System.Collections.Generic;
using PenguinRun.Core;
using PenguinRun.Game;
using PenguinRun.Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
#if DOTWEEN
using DG.Tweening;
#endif

namespace PenguinRun
{
    public sealed class PenguinRunnerGame : MonoBehaviour
    {
        [SerializeField] private RunnerTuningConfig tuningConfig = RunnerTuningConfig.Default();

        private RunnerTuning tuning;
        private RunnerGameController game;
        private readonly List<GameObject> laneGuides = new();
        private readonly List<Renderer> laneGuideRenderers = new();
        private readonly List<GameObject> trackMarkers = new();
        private readonly List<GameObject> sceneryProps = new();

        // 与 sceneryProps 索引对齐的逐道具缓存；两者只在 AddSceneryProp 追加、在重建主题时一起清空。
        private readonly List<SceneryPropInfo> sceneryPropInfos = new();
        private readonly List<GameObject> roadTiles = new();
        private readonly Dictionary<string, Texture2D> textureCache = new();
        private readonly Dictionary<string, GameObject> modelPrefabCache = new();
        private readonly RunnerVisualFactory visualFactory = new();
        private readonly RunSettlementGate settlementGate = new();

        private RunnerSessionConfig config;
        private GameObject player;
        private IPlayerCharacterPresentation character;
        private RunnerAudio runnerAudio;
        private Camera runnerCamera;
        private RunnerPostFx postFx;
        private Light keyLight;
        private Light rimFillLight;
        private readonly FogLanternFogDriver fogLantern = new();
        private Material gradientSkyboxMaterial;
        private GameObject deathOverlay;
        private Vector3 cameraVelocity;
        private float cameraYawSmoothed;
        private float cameraYawVel;
        private RunnerHudCanvas hudCanvas;
        private RunUpgradeChoiceOverlay upgradeOverlay;
        private ParticleEffectPool particlePool;
        private WorldEventScheduler worldEvents;
        private bool exiting;
        private bool warnedGameMissing;
        private RunnerMapTheme currentTheme;
        private RunnerMapTheme pendingTheme;
        private float nextThemeSwitchTime;
        private bool themeAppliedInTransition;
        private float themeTransitionTimer;
        private GUIStyle themeTransitionLabelStyle;
        private GUIStyle entranceNumberStyle;
        private GUIStyle entranceGoStyle;

        // ── Boss 战镜头特效 ──────────────────────────────────
        private float cameraShakeTimer = 0f;
        private float cameraShakeIntensity = 0f;
        private float defeatFreezeTimer = 0f;
        private bool wasBossDefeated = false;

        // ── 屏幕闪屏特效 ───────────────────────────────────────
        private float screenFlashTimer;
        private float screenFlashDuration;
        private Color screenFlashColor = Color.clear;
        private Color screenFlashCurrent = Color.clear;
        private bool bossSpawnFlashPending;
        private float bossSpawnFlashIntensity;

        // ── 开局倒计时 overlay（逻辑在 RunnerGameController FSM Countdown）──
        // ── 死亡序列 ────────────────────────────────────────────
        private enum DeathState { None, Freeze, FadeRed, ShowResult, WaitingTap }
        private DeathState deathState;
        private float deathStateTimer;

        private const float ThemeSwitchIntervalSeconds = 52f;
        private const float ThemeTransitionFadeOutSeconds = 0.45f;
        private const float ThemeTransitionHoldSeconds = 0.22f;
        private const float ThemeTransitionFadeInSeconds = 0.68f;

        // Playable view corridor — scenery must not block the lane sightline near camera/player.
        private const float RoadHalfWidth = 5.1f;
        private const float SceneryMinAbsX = 8.5f;
        private const float ViewCorridorHalfWidth = 5.2f;
        private const float ViewCorridorNearZBehindCamera = 0.5f;
        private const float ViewCorridorNearZAheadPlayer = 26f;
        private const float ViewCorridorMaxY = 10.5f;
        private const float ViewCorridorFadedAlpha = 0.08f;

        private readonly Dictionary<GameObject, Renderer[]> sceneryPropRenderers = new();
        private readonly Dictionary<int, float> sceneryRendererBaseAlpha = new();
        private readonly Dictionary<int, bool> sceneryRendererBaseEnabled = new();

        private void ApplyRunnerCameraViewport169()
        {
            if (runnerCamera == null) return;

            var windowW = Screen.width;
            var windowH = Screen.height;
            if (windowW <= 0 || windowH <= 0) return;

            runnerCamera.pixelRect = new Rect(0f, 0f, windowW, windowH);
        }

        private void Awake()
        {
            // 确保场景过渡管理器存在（可能已在 MainMenu 创建）
            if (SceneTransitionManager.Instance == null)
            {
                var tm = new GameObject("SceneTransitionManager");
                tm.AddComponent<SceneTransitionManager>();
            }

            tuning = RunnerTuning.FromConfig(tuningConfig);
            ConfigureRuntimePerformance(true);
            ApplyMobileQualityBudget();
            config = RunnerSessionConfig.Snapshot();
            currentTheme = config.MapTheme;
            nextThemeSwitchTime = ThemeSwitchIntervalSeconds;

            // 应用难度预设：覆盖速度曲线参数
            var diff = config.DifficultyPreset;
            tuning.world.startSpeed       = diff.StartSpeed;
            tuning.world.maxSpeed         = diff.MaxSpeed;
            tuning.world.speedPerDistance = diff.SpeedRampPerMeter;
            runnerAudio = gameObject.AddComponent<RunnerAudio>();
            runnerAudio.Initialize(config.Daily, config.SfxEnabled, config.BgmEnabled, config.RunBgmTrack, config.AmbienceTrack, config.SfxStyle);
            BuildScene();
            particlePool = new ParticleEffectPool(transform, tuning.movement.groundY);
            worldEvents = new WorldEventScheduler(config.IsOnboarding);
            ApplyMapThemeVisuals(currentTheme);
            game = new RunnerGameController(tuning, config, runnerAudio, player.transform, character, transform, PulseHaptic, TriggerScreenFlash, OnRunFinished);
            game.ResetRun(false);
            hudCanvas = RunnerHudCanvas.Create(config, RequestPauseFromHud,
                RequestResumeFromHud, RequestFinishRunFromHud);
            lastLives = game.World.Lives;

            // 道具拾取专属粒子环（音效由 RunnerAudio 处理，这里只做视觉）
            EventBus.On<PowerUpKind>(GameEvents.PowerUpCollected, OnPowerUpCollectedFx);

            // ── 注册核心服务 ──────────────────────────────────────
            ServiceLocator.Register(runnerAudio);
            ServiceLocator.Register(config);
            EventBus.Emit(GameEvents.SceneTransitionComplete);
        }

        private void OnPowerUpCollectedFx(PowerUpKind kind)
        {
            if (player == null || particlePool == null) return;
            character?.TriggerInteraction(kind switch
            {
                PowerUpKind.Shield => RunnerAvatarInteraction.ShieldActivate,
                PowerUpKind.Dash => RunnerAvatarInteraction.DashActivate,
                PowerUpKind.Magnet => RunnerAvatarInteraction.MagnetActivate,
                _ => RunnerAvatarInteraction.FishPickup,
            });
            var profile = PowerUpVisualProfile.For(kind, currentTheme);
            var pos = player.transform.position;
            particlePool.EmitRing(pos, profile.CoreColor, 12, 1.1f, 3.2f, 0.55f);
            particlePool.EmitBurst(pos + Vector3.up * 0.6f, profile.HaloColor, 8, 2.6f, 0.6f);
        }

        private void OnRunFinished(RunnerRunResult result)
        {
            if (exiting) return;
            exiting = true;
            TrySettleCurrentAttempt(result, true);
            SceneTransitionManager.Instance.TransitionTo("MainMenu");
        }

        private RunOutcomeRouter.Result TrySettleCurrentAttempt(RunnerRunResult result,
            bool publishAsLastResult)
        {
            if (!settlementGate.TryBeginSettlement()) return null;
            var applied = RunOutcomeRouter.Apply(
                result.Score, result.DistanceMeters, result.Coins, result.SurvivalSeconds, config.Daily,
                config.Difficulty,
                result.LastBossSpeedTier, result.LastBossSpeedFishBonus, result.LastBossSpeedScoreBonus,
                result.RouteEventsCompleted, result.RiskyRoutesCompleted,
                result.SkillScore, result.PeakSkillChain,
                result.SelectedUpgradeIds, result.CompletedRouteEventIds, result.ThemedRelicId,
                result.ThemeMastery);
            if (publishAsLastResult) RunSession.LastResult = applied;
            return applied;
        }

        private void OnDestroy()
        {
            EventBus.Off<PowerUpKind>(GameEvents.PowerUpCollected, OnPowerUpCollectedFx);
            postFx?.Dispose();
            postFx = null;
            hudCanvas?.Dispose();
            hudCanvas = null;
            if (upgradeOverlay != null)
                Destroy(upgradeOverlay.gameObject);
            if (deathOverlay != null)
                Destroy(deathOverlay);
            if (RenderSettings.skybox == gradientSkyboxMaterial)
                RenderSettings.skybox = null;
            if (gradientSkyboxMaterial != null)
                Destroy(gradientSkyboxMaterial);
            EventBus.Clear();
            ServiceLocator.Clear();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            ConfigureRuntimePerformance(hasFocus);
            if (!hasFocus) game?.SetPaused(true, false);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            ConfigureRuntimePerformance(!pauseStatus);
            if (pauseStatus) game?.SetPaused(true, false);
        }

        private void Update()
        {
            if (game == null)
            {
                if (Application.isPlaying && !warnedGameMissing)
                {
                    warnedGameMissing = true;
                    Debug.LogWarning(
                        "PenguinRunnerGame: game controller is null — Awake probably threw before RunnerGameController was created. Check the first error in the Console (often inside BuildScene).");
                }
                return;
            }

            // ── 死亡序列：由 FSM DeathSequence / GameOver 驱动 ─────────
            if (game.IsDeathPresentation)
            {
                TickDeathPresentation(Time.deltaTime);
                return;
            }

            if (upgradeOverlay != null)
            {
                return;
            }

            if (game.IsCountdown && Input.GetKeyDown(KeyCode.Escape))
            {
                game.SkipEntranceCountdown();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                game.SetPaused(!game.IsPaused);
                return;
            }

            if (!game.Running)
            {
                game.TickPreRun(Time.deltaTime);
                var idleDt = Mathf.Min(Time.deltaTime, 0.05f);
                UpdateCamera(idleDt);
                UpdateLaneGuides(idleDt);
                fogLantern.Tick(idleDt, game.World.FogLanternTimer > 0f);
                particlePool.Tick(idleDt, game.World.Distance);
                return;
            }

            // ── 主题切换（优先于跑酷模拟）──────────────────────────────
            if (game.IsThemeTransition)
            {
                TickThemeTransition(Time.deltaTime);
                var frozenDt = Mathf.Min(Time.deltaTime, 0.05f);
                UpdateCamera(frozenDt);
                UpdateLaneGuides(frozenDt);
                return;
            }

            var dt = Mathf.Min(Time.deltaTime, 0.05f);
            var effectiveSpeed = game.TickRunningSimulation(Time.deltaTime);
            UpdateCamera(dt);
            UpdateLaneGuides(dt);
            fogLantern.Tick(dt, game.World.FogLanternTimer > 0f);
            if (!game.IsSimulationFrozen)
            {
                AnimateScenery(dt, effectiveSpeed);
                game.TickRunningWorld(dt, effectiveSpeed);
                TryShowRunUpgradeChoice();
                TickParticleEvents(dt);
            }

            // 驱动粒子池和世界事件
            particlePool.Tick(dt, game.World.Distance);

            if (worldEvents.Tick(game.World.Distance))
            {
                // 事件触发：显示提示
                game.FeedbackText = worldEvents.EventDisplayName;
                game.FeedbackTimer = 2f;
            }

            // 世界事件粒子
            if (worldEvents.EventActive)
            {
                EmitWorldEventParticles(dt);
            }

            // 检测 Boss 被击败，触发特效
            CheckBossDefeatEffects();

            TryStartThemeTransition();
        }

        private void TryShowRunUpgradeChoice()
        {
            if (upgradeOverlay != null || game == null || game.IsPaused) return;
            if (!game.TryBuildUpgradeOffer(out var offer)) return;

            game.TogglePause();
            upgradeOverlay = RunUpgradeChoiceOverlay.Show(
                offer,
                id => game.RunUpgrades.GetStack(id),
                OnRunUpgradeSelected);
            if (upgradeOverlay == null && game.IsPaused)
            {
                game.TogglePause();
            }
        }

        private void OnRunUpgradeSelected(RunUpgradeId id)
        {
            game?.SelectRunUpgrade(id);
            upgradeOverlay = null;
            if (game != null && game.IsPaused)
            {
                game.TogglePause();
            }
        }

        private void LateUpdate()
        {
            if (game == null || hudCanvas == null) return;
            var world = game.World;
            hudCanvas.RefreshDynamic(world, world.Score, game.FeedbackText, game.FeedbackTimer);
            hudCanvas.SetBossMode(game.Boss);
            hudCanvas.SetRouteEvent(game.RouteEvent);
            hudCanvas.SetThemeMechanic(game.ThemeMechanic);
            hudCanvas.SetThemeMastery(game.ThemeMasteryHud);
            hudCanvas.SetUpgradeSummary(game.RunUpgrades);
            hudCanvas.SetSkillSnapshot(game.SkillScore);
            hudCanvas.SetPaused(game.IsPaused);
            UpdateBossAtmosphere();
        }

        /// <summary>Boss 战氛围后处理：危险窗口收暗角压红，破绽窗口转暖绿。纯表现层。</summary>
        private void UpdateBossAtmosphere()
        {
            if (postFx == null) return;
            var boss = game.Boss;
            var encounter = boss?.CurrentBoss;
            if (encounter == null || !boss.BossActive)
            {
                postFx.ClearBossEmphasis();
                return;
            }

            var danger = encounter.IsPatternDangerous ? 1f : 0f;
            var vulnerable = encounter.Phase == BossPhase.Vulnerable ? 1f : 0f;
            postFx.SetBossEmphasis(danger, vulnerable);
        }

        private void RequestFinishRunFromHud()
        {
            if (game != null && !exiting) game.FinishRun();
        }

        private void RequestPauseFromHud()
        {
            game?.SetPaused(true);
        }

        private void RequestResumeFromHud()
        {
            game?.SetPaused(false);
        }

        // ── 死亡序列（FSM DeathSequence → GameOver，本地子阶段管 overlay）──
        private void TickDeathPresentation(float rawDt)
        {
            if (deathState == DeathState.None)
            {
                deathState = DeathState.Freeze;
                deathStateTimer = 0f;
                TriggerScreenFlash(new Color(0.85f, 0.15f, 0.08f, 0.5f), 0.5f);
                TriggerCameraShake(0.35f, 0.08f);
            }

            var dt = Mathf.Min(rawDt, 0.05f);
            deathStateTimer += dt;

            switch (deathState)
            {
                case DeathState.Freeze:
                    if (deathStateTimer >= 0.4f)
                    {
                        deathState = DeathState.FadeRed;
                        deathStateTimer = 0f;
                    }
                    game.TickAvatarPresentation(dt, false, true);
                    game.RunnerAudio.UpdateRunState(0f, false, false, false, false);
                    UpdateCamera(dt);
                    break;

                case DeathState.FadeRed:
                    var redAlpha = Mathf.Clamp01(deathStateTimer / 0.6f) * 0.55f;
                    screenFlashColor = new Color(0.75f, 0.08f, 0.02f, redAlpha);
                    screenFlashCurrent = screenFlashColor;
                    screenFlashTimer = 0.02f;
                    screenFlashDuration = 0.02f;

                    game.TickAvatarPresentation(dt, false, true);
                    UpdateCamera(dt);

                    postFx?.SetDeathEmphasis(Mathf.Clamp01(deathStateTimer / 0.6f));
                    if (deathStateTimer >= 0.6f)
                    {
                        deathState = DeathState.ShowResult;
                        deathStateTimer = 0f;
                        game.AdvanceDeathToResult();
                        ShowDeathOverlay();
                    }
                    break;

                case DeathState.ShowResult:
                    game.TickAvatarPresentation(dt, false, true);
                    UpdateCamera(dt);
                    break;

                case DeathState.WaitingTap:
                    game.TickAvatarPresentation(dt, false, true);
                    UpdateCamera(dt);

                    break;
            }
        }

        private void FinishDeathPresentation()
        {
            deathState = DeathState.None;
            deathStateTimer = 0f;
            postFx?.SetDeathEmphasis(0f);

            if (!exiting)
            {
                game.FinishRun();
            }
        }

        private void ShowDeathOverlay()
        {
            if (deathOverlay != null || game == null) return;
            var world = game.World;
            deathOverlay = RunnerDeathOverlay.Show(world.Score, Mathf.RoundToInt(world.Distance), world.Coins,
                RestartRunFromDeathOverlay, FinishDeathPresentation);
        }

        private void RestartRunFromDeathOverlay()
        {
            TrySettleCurrentAttempt(game.CaptureResult(), false);
            deathOverlay = null;
            deathState = DeathState.None;
            deathStateTimer = 0f;
            postFx?.SetDeathEmphasis(0f);
            ResetPresentationForRetry();
            game.ResetRun(true);
            settlementGate.ResetForNextAttempt();
            lastLives = game.World.Lives;
            lastCoinCount = game.World.Coins;
            lastPerfectDodges = game.World.PerfectDodges;
        }

        private void ResetPresentationForRetry()
        {
            worldEvents.Reset();
            particlePool.ReleaseAll();
            worldEventParticleCooldown = 0f;

            if (upgradeOverlay != null)
            {
                Destroy(upgradeOverlay.gameObject);
                upgradeOverlay = null;
            }

            currentTheme = config.MapTheme;
            pendingTheme = currentTheme;
            nextThemeSwitchTime = ThemeSwitchIntervalSeconds;
            themeTransitionTimer = 0f;
            themeAppliedInTransition = false;
            ApplyMapThemeVisuals(currentTheme);
            RebuildThemeScenery(currentTheme);
            game.Spawner.ApplyTheme(currentTheme, clearSpawnedEntities: true);

            lastCoinCount = 0;
            lastPerfectDodges = 0;
            lastLives = 0;
            wasGroundedLastFrame = true;
            wasDodgingLastFrame = false;
            landingBounceTimer = 0f;
            landingBounceOffset = 0f;
            slideHeightSmooth = 0f;

            cameraVelocity = Vector3.zero;
            cameraYawSmoothed = 0f;
            cameraYawVel = 0f;
            cameraShakeTimer = 0f;
            cameraShakeIntensity = 0f;
            defeatFreezeTimer = 0f;
            wasBossDefeated = false;
#if DOTWEEN
            if (runnerCamera != null) runnerCamera.transform.DOKill();
#endif

            screenFlashTimer = 0f;
            screenFlashDuration = 0f;
            screenFlashColor = Color.clear;
            screenFlashCurrent = Color.clear;
            bossSpawnFlashPending = false;
            bossSpawnFlashIntensity = 0f;
        }

        private void DrawDeathOverlay()
        {
            if (!game.IsDeathPresentation) return;

            // 红屏覆盖
            if (deathState == DeathState.FadeRed)
            {
                var alpha = Mathf.Clamp01(deathStateTimer / 0.6f) * 0.45f;
                GUI.color = new Color(0.7f, 0.05f, 0.02f, alpha);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        // ── 粒子事件跟踪状态 ──────────────────────────────────────
        private int lastCoinCount;
        private int lastPerfectDodges;
        private int lastLives;
        private bool wasGroundedLastFrame = true;
        private bool wasDodgingLastFrame;

        private void TickParticleEvents(float dt)
        {
            var world = game.World;
            var motor = game.Motor;
            var playerPos = player.transform.position;

            // ── 以下仅负责粒子发射，音效由行为源通过 EventBus 驱动 ──

            // 收集金币粒子（音效由 PickupSystem → EventBus → RunnerAudio）
            if (world.Coins > lastCoinCount)
            {
                var diff = world.Coins - lastCoinCount;
                var burstCount = Mathf.Min(diff * 2, 8);
                particlePool.Emit(playerPos, new Color(1f, 0.9f, 0.3f), burstCount, 1.5f, 0.4f, true);
                lastCoinCount = world.Coins;
            }
            else if (world.Coins < lastCoinCount)
            {
                lastCoinCount = world.Coins;
            }

            // 受伤粒子（音效由 CollisionSystem/Boss → EventBus → RunnerAudio）
            if (world.Lives < lastLives)
            {
                particlePool.EmitBurst(playerPos, new Color(1f, 0.2f, 0.1f), 12, 4f, 0.7f);
                EventBus.Emit(GameEvents.LivesChanged, world.Lives);
                lastLives = world.Lives;
            }
            else if (world.Lives > lastLives)
            {
                particlePool.EmitRing(playerPos, new Color(0.3f, 1f, 0.5f), 10, 1f, 2.5f, 0.5f);
                EventBus.Emit(GameEvents.LivesChanged, world.Lives);
                lastLives = world.Lives;
            }

            // 完美闪避粒子（音效由 CollisionSystem → EventBus → RunnerAudio）
            if (world.PerfectDodges > lastPerfectDodges)
            {
                particlePool.EmitBurst(playerPos, new Color(0.4f, 0.85f, 1f), 8, 3f, 0.6f);
                lastPerfectDodges = world.PerfectDodges;
            }

            // 着陆粒子（音效由 PlayerMotor → EventBus → RunnerAudio）
            if (motor.Grounded && !wasGroundedLastFrame)
            {
                var groundPos = new Vector3(playerPos.x, tuning.movement.groundY + 0.02f, playerPos.z);
                particlePool.Emit(groundPos, new Color(0.8f, 0.95f, 1f), 5, 1.2f, 0.35f, false);
            }
            wasGroundedLastFrame = motor.Grounded;

            // 滑铲粒子（音效由 PlayerMotor.ApplyAction → EventBus → RunnerAudio）
            if (motor.SlideTimer > 0f && !wasDodgingLastFrame)
            {
                var slidePos = new Vector3(playerPos.x, tuning.movement.groundY + 0.02f, playerPos.z - 0.5f);
                particlePool.Emit(slidePos, new Color(0.6f, 0.9f, 1f), 3, 0.8f, 0.3f, false);
            }
            wasDodgingLastFrame = motor.SlideTimer > 0f;

            TickMagnetStreaks(dt, world, playerPos);
        }

        // 磁吸飞线节流
        private float magnetStreakCooldown;

        /// <summary>磁力生效时，附近鱼干向玩家射出金色流光（纯视觉，不改收集半径判定）。</summary>
        private void TickMagnetStreaks(float dt, WorldDirector world, Vector3 playerPos)
        {
            if (world.MagnetTimer <= 0f) return;
            magnetStreakCooldown -= dt;
            if (magnetStreakCooldown > 0f) return;
            magnetStreakCooldown = 0.08f;

            var coins = game.Spawner.coins;
            var emitted = 0;
            for (var i = 0; i < coins.Count && emitted < 3; i++)
            {
                var coin = coins[i];
                if (coin == null || !coin.activeSelf) continue;
                var cp = coin.transform.position;
                var dz = cp.z - playerPos.z;
                if (dz < 0.5f || dz > 7.5f) continue;
                if (Mathf.Abs(cp.x - playerPos.x) > 5f) continue;

                var flightTime = 0.28f;
                var target = playerPos + Vector3.up * 0.7f;
                particlePool.EmitDirected(cp, (target - cp) / flightTime,
                    new Color(1f, 0.86f, 0.35f, 0.9f), flightTime, 0.07f);
                emitted++;
            }
        }

        private float worldEventParticleCooldown;

        private void EmitWorldEventParticles(float dt)
        {
            worldEventParticleCooldown -= dt;
            if (worldEventParticleCooldown > 0f) return;

            var eventType = worldEvents.ActiveEvent;
            var playerPos = player.transform.position;
            var spreadX = 6f;
            var spreadZ = 12f;

            switch (eventType)
            {
                case WorldEventScheduler.WorldEventType.MeteorShower:
                    worldEventParticleCooldown = 0.15f;
                    // 金色粒子从上方落下
                    var meteorPos = new Vector3(
                        playerPos.x + Random.Range(-spreadX, spreadX),
                        playerPos.y + Random.Range(5f, 10f),
                        playerPos.z + Random.Range(0f, spreadZ));
                    particlePool.Emit(meteorPos, new Color(1f, 0.9f, 0.3f), 3, 0.5f, 0.8f, true);
                    break;

                case WorldEventScheduler.WorldEventType.Blizzard:
                    worldEventParticleCooldown = 0.08f;
                    // 白色雪花密集飘落
                    var snowPos = new Vector3(
                        playerPos.x + Random.Range(-spreadX, spreadX),
                        playerPos.y + Random.Range(4f, 9f),
                        playerPos.z + Random.Range(-4f, spreadZ));
                    particlePool.Emit(snowPos, new Color(0.9f, 0.95f, 1f), 1, 0.3f, 1.5f, true);
                    break;

                case WorldEventScheduler.WorldEventType.AuroraBurst:
                    worldEventParticleCooldown = 0.2f;
                    // 彩色光点缓慢飘升
                    var auroraPos = new Vector3(
                        playerPos.x + Random.Range(-spreadX, spreadX),
                        playerPos.y + 0.1f,
                        playerPos.z + Random.Range(-2f, spreadZ));
                    var auroraColor = Random.value < 0.5f
                        ? new Color(0.3f, 1f, 0.8f)
                        : new Color(0.5f, 0.5f, 1f);
                    particlePool.Emit(auroraPos, auroraColor, 2, 1.5f, 1f, false);
                    break;
            }
        }

        /// <summary>检测 Boss 被击败状态并触发镜头特效</summary>
        private void CheckBossDefeatEffects()
        {
            if (game == null) return;

            var bossSystem = game.Boss;
            if (bossSystem == null || bossSystem.CurrentBoss == null) return;

            var boss = bossSystem.CurrentBoss;
            var isDefeated = boss.HitsRemaining <= 0 && boss.Phase == BossPhase.Retreating;

            if (isDefeated && !wasBossDefeated)
            {
                // Boss 首次被击败：定格 + 震动 + 金色闪屏 + 企鹅庆祝演出
                defeatFreezeTimer = 0.25f;
                cameraShakeTimer = 0.5f;
                cameraShakeIntensity = 0.15f;
                TriggerScreenFlash(new Color(1f, 0.85f, 0.2f, 0.45f), 0.55f);
                wasBossDefeated = true;
                character?.TriggerCelebration();

                // 金色粒子爆发（音效由 BossSystem → EventBus → RunnerAudio）
                if (player != null)
                    particlePool.EmitBurst(player.transform.position, new Color(1f, 0.85f, 0.2f), 30, 6f, 1.2f);
            }
            else if (!isDefeated)
            {
                wasBossDefeated = false;
            }

            // Boss 击发/危险窗口：按招式配置的震屏幅度
            if (boss.Phase == BossPhase.Active && (boss.IsPatternDangerous || boss.AttackPhase == BossAttackPhase.Strike))
            {
                var amp = boss.PatternStrikeCameraShakeAmp > 0.001f
                    ? boss.PatternStrikeCameraShakeAmp
                    : 0.085f;
                var dur = boss.PatternStrikeCameraShakeDuration > 0.001f
                    ? boss.PatternStrikeCameraShakeDuration
                    : 0.12f;
                if (boss.Pattern == BossPattern.QuakePulse || boss.Pattern == BossPattern.CenterBeam)
                {
                    amp *= 1.12f;
                }

                cameraShakeTimer = Mathf.Max(cameraShakeTimer, dur);
                cameraShakeIntensity = Mathf.Max(cameraShakeIntensity, amp);
            }
        }

        /// <summary>触发相机震动（供外部调用）。DOTween 可用时优先使用高性能补间。</summary>
        public void TriggerCameraShake(float duration, float intensity)
        {
#if DOTWEEN
            if (runnerCamera != null)
                runnerCamera.PlayCameraShake(duration, intensity);
#else
            cameraShakeTimer = duration;
            cameraShakeIntensity = intensity;
#endif
        }

        /// <summary>触发全屏闪屏特效（Boss 攻击击发 / 玩家受伤 / Boss 击败）。</summary>
        public void TriggerScreenFlash(Color color, float duration)
        {
            screenFlashColor = color;
            screenFlashDuration = duration;
            screenFlashTimer = duration;
            screenFlashCurrent = color;
        }

        /// <summary>Boss 出场时触发屏幕边缘红光预警。</summary>
        public void TriggerBossSpawnWarning(float intensity)
        {
            bossSpawnFlashPending = true;
            bossSpawnFlashIntensity = intensity;
        }

        public void FinishFromAndroidButton(string _)
        {
            if (game == null) return;
            game.FinishRun();
        }

        private void PulseHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (config.HapticIntensity > 0.05f)
            {
                Handheld.Vibrate();
            }
#endif
        }

        private void ApplyMapThemeVisuals(RunnerMapTheme theme)
        {
            TrackTerrain.SetThemeProfile(theme);
            var profile = ThemePresentationProfile.For(theme);

            // 听感与视觉在同一处切换，避免主题切换后画面已变、环境音还停在上一张图。
            if (runnerAudio != null) runnerAudio.ApplyTheme(theme);

            RenderSettings.fogColor = profile.FogColor;
            RenderSettings.ambientLight = profile.AmbientLight;
            fogLantern.SetThemeBaseline(profile.FogStart, profile.FogEnd);
            if (runnerCamera != null) runnerCamera.backgroundColor = profile.HorizonColor;

            // 主光/边光随主题走：光向、色温与强度是各主题之间最强的区分手段。
            if (keyLight != null)
            {
                keyLight.color = profile.KeyLightColor;
                keyLight.intensity = profile.KeyLightIntensity;
                keyLight.transform.rotation = Quaternion.Euler(profile.KeyLightEuler);
            }
            if (rimFillLight != null)
            {
                rimFillLight.color = profile.RimLightColor;
                rimFillLight.intensity = profile.RimLightIntensity;
            }

            postFx?.ApplyThemeGrading(profile.PostTemperature, profile.PostSaturation);
            ApplyThemeSkybox(theme);
        }

        private void SetupGradientSkybox()
        {
            var shader = Shader.Find("PenguinRun/GradientSkybox");
            if (shader == null)
            {
                Debug.LogWarning("Gradient skybox shader is unavailable; keeping the current camera background.");
                return;
            }

            gradientSkyboxMaterial ??= new Material(shader) { name = "PenguinRun Gradient Skybox" };
            RenderSettings.skybox = gradientSkyboxMaterial;
            if (runnerCamera != null)
                runnerCamera.clearFlags = CameraClearFlags.Skybox;
        }

        private void ApplyThemeSkybox(RunnerMapTheme theme)
        {
            if (gradientSkyboxMaterial == null) return;
            var profile = ThemePresentationProfile.For(theme);
            gradientSkyboxMaterial.SetColor("_HorizonColor", profile.HorizonColor);
            gradientSkyboxMaterial.SetColor("_ZenithColor", profile.ZenithColor);
            // 黑色 = 该主题天上没有极光带（雪松/雾堤/海洋/天空）。
            gradientSkyboxMaterial.SetColor("_AuroraColor", profile.SkyAuroraColor);
            gradientSkyboxMaterial.SetFloat("_StarIntensity", theme == RunnerMapTheme.SkyFlight ? 0.04f : 0.38f);
            DynamicGI.UpdateEnvironment();
        }

        private static void EnsureLandscape()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                if (Screen.orientation == ScreenOrientation.Portrait ||
                    Screen.orientation == ScreenOrientation.PortraitUpsideDown)
                {
                    Screen.orientation = ScreenOrientation.LandscapeLeft;
                }
                else if (Screen.orientation == ScreenOrientation.AutoRotation ||
                         Screen.orientation == ScreenOrientation.LandscapeLeft ||
                         Screen.orientation == ScreenOrientation.LandscapeRight)
                {
                    // 已是横屏或自动旋转，保留玩家当前握姿
                }
                else
                {
                    Screen.orientation = ScreenOrientation.LandscapeLeft;
                }
            }
#endif
        }

        private void BuildScene()
        {
            EnsureLandscape();
            runnerCamera = Camera.main;
            if (runnerCamera == null)
            {
                var cameraGo = new GameObject("Runner Camera");
                runnerCamera = cameraGo.AddComponent<Camera>();
                cameraGo.tag = "MainCamera";
            }

            if (runnerCamera.GetComponent<AudioListener>() == null)
                runnerCamera.gameObject.AddComponent<AudioListener>();

            // 跑酷常用低位第三人称：看得到角色背影，同时保证前方障碍可读。
            var cam = tuning.camera;
            var s0 = TrackTerrain.SurfaceY(0f, 0f, tuning.movement.groundY);
            runnerCamera.transform.position = new Vector3(0f, s0 + cam.heightAboveSurface, -cam.behindDistance);
            var aim0 = new Vector3(0f, s0 + cam.aimHeightAboveSurface, cam.aimAhead);
            runnerCamera.transform.rotation = Quaternion.LookRotation(aim0 - runnerCamera.transform.position, Vector3.up);
            cameraYawSmoothed = 0f;
            cameraYawVel = 0f;
            runnerCamera.fieldOfView = cam.fieldOfView;
            runnerCamera.nearClipPlane = 0.16f;
            runnerCamera.farClipPlane = 260f; // 增加远裁剪面，让后方跑道显示更远
            runnerCamera.clearFlags = CameraClearFlags.SolidColor;
            runnerCamera.backgroundColor = new Color(0.54f, 0.77f, 0.95f);
            SetupGradientSkybox();
            ApplyRunnerCameraViewport169();
            postFx = RunnerPostFx.TryAttach(runnerCamera);

            RenderSettings.ambientLight = new Color(0.42f, 0.58f, 0.68f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.66f, 0.82f, 0.9f);
            RenderSettings.fogMode = FogMode.Linear;
            fogLantern.SetThemeBaseline(55f, 180f); // 起点后移 + 终点增加，后方跑道可见更远

            // 主光与边光的颜色/强度/朝向由 ApplyMapThemeVisuals 按主题覆盖；此处只建常态默认值。
            keyLight = new GameObject("Key Light").AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.2f;
            keyLight.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            // 反向低强度补光（Rim），提升角色与障碍立体感
            rimFillLight = new GameObject("Rim Fill Light").AddComponent<Light>();
            rimFillLight.type = LightType.Directional;
            rimFillLight.intensity = 0.35f;
            rimFillLight.color = new Color(0.6f, 0.74f, 1f);
            rimFillLight.transform.rotation = Quaternion.Euler(24f, 152f, 0f);
            rimFillLight.shadows = LightShadows.None;

            player = new GameObject("Penguin Character");
            // Prefer Kenney Cube Pets penguin (CC0); fall back to Gugugaga Mecanim, then procedural cute mesh.
            character = CubePetPenguinCharacter.TryCreate(player.transform);
            if (character == null)
            {
                var avatarDefinition = Resources.Load<RunnerAvatarDefinition>(
                    "PenguinRun/Characters/Gugugaga/GugugagaAvatar");
                character = PlayerAvatarPresenter.Create(player.transform, avatarDefinition);
            }
            player.transform.localScale = Vector3.one * Mathf.Clamp(tuning.camera.playerVisualScale, 0.72f, 1f);
            SetPlayerRenderersVisible(player.transform, true);

            BuildIceWorld(config.MapTheme);

            var lw = tuning.movement.laneWidth;
            for (var i = -1; i <= 1; i++)
            {
                var guide = RunnerVisuals.CreatePrimitive(
                    $"Lane Guide {i}",
                    PrimitiveType.Cube,
                    new Vector3(i * lw, 0.035f, 28f),
                    new Vector3(0.08f, 0.04f, 86f),
                    i == 0 ? new Color(0.58f, 1f, 0.9f, 0.72f) : new Color(0.85f, 0.95f, 1f, 0.36f),
                    true);
                guide.transform.position = new Vector3(i * lw, 0.02f, 28f);
                laneGuides.Add(guide);
                laneGuideRenderers.Add(guide.GetComponent<Renderer>());
            }
        }

        private static void SetPlayerRenderersVisible(Transform root, bool visible)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }
        }

        private void BuildIceWorld(RunnerMapTheme theme)
        {
            var roadBase = theme switch
            {
                RunnerMapTheme.CedarRuins => new Color(0.18f, 0.38f, 0.46f),
                RunnerMapTheme.AuroraField => new Color(0.12f, 0.32f, 0.68f),
                RunnerMapTheme.MistDike => new Color(0.22f, 0.34f, 0.42f),
                RunnerMapTheme.OceanReef => new Color(0.1f, 0.35f, 0.55f),  // 海洋：深蓝跑道
                RunnerMapTheme.SkyFlight => new Color(0.55f, 0.75f, 0.95f), // 天空：天蓝跑道
                _ => new Color(0.06f, 0.36f, 0.68f),                              // 冰湖：更深的极夜蓝跑道
            };
            var roadGloss = theme switch
            {
                RunnerMapTheme.CedarRuins => new Color(0.58f, 0.88f, 0.86f, 0.18f),
                RunnerMapTheme.AuroraField => new Color(0.5f, 0.9f, 1f, 0.28f),
                RunnerMapTheme.MistDike => new Color(0.78f, 0.9f, 0.96f, 0.16f),
                RunnerMapTheme.OceanReef => new Color(0.25f, 0.65f, 0.8f, 0.35f), // 海洋：青蓝色光泽
                RunnerMapTheme.SkyFlight => new Color(0.75f, 0.88f, 1f, 0.3f),    // 天空：白云光泽
                _ => new Color(0.45f, 0.78f, 1f, 0.28f),
            };
            var snowColor = theme switch
            {
                RunnerMapTheme.CedarRuins => new Color(0.74f, 0.82f, 0.82f),
                RunnerMapTheme.AuroraField => new Color(0.74f, 0.9f, 1f),
                RunnerMapTheme.MistDike => new Color(0.68f, 0.76f, 0.8f),
                RunnerMapTheme.OceanReef => new Color(0.8f, 0.92f, 0.95f),   // 海洋：浅蓝白色
                RunnerMapTheme.SkyFlight => new Color(0.98f, 0.98f, 1f),      // 天空：纯白
                _ => new Color(0.52f, 0.68f, 0.86f),                          // 冰湖：灰蓝雪，避免一片死白
            };
            var edgeGlow = theme switch
            {
                RunnerMapTheme.CedarRuins => new Color(0.7f, 0.95f, 0.72f, 0.58f),
                RunnerMapTheme.AuroraField => new Color(0.66f, 0.54f, 1f, 0.78f),
                RunnerMapTheme.MistDike => new Color(0.8f, 0.94f, 1f, 0.42f),
                RunnerMapTheme.OceanReef => new Color(0.3f, 0.9f, 0.95f, 0.75f), // 海洋：亮青色
                RunnerMapTheme.SkyFlight => new Color(1f, 1f, 0.6f, 0.6f),        // 天空：阳光黄
                _ => new Color(1f, 0.62f, 0.32f, 0.88f),                         // 冰湖：琥珀描边暖点
            };

            // 用可循环的弯道瓦片替换静态直线路面
            BuildCurvedRoadTiles(theme, roadBase, roadGloss, snowColor, edgeGlow);

            // 根据主题添加不同的地面细节
            if (theme == RunnerMapTheme.OceanReef)
            {
                BuildOceanDetails(edgeGlow);
            }
            else if (theme == RunnerMapTheme.SkyFlight)
            {
                BuildSkyDetails(edgeGlow);
            }
            else
            {
                BuildIceDetails(edgeGlow);
            }

            for (var i = 0; i < 14; i++)
            {
                var z = i * 8.5f - 6f;
                var marker = RunnerVisuals.CreatePrimitive("Speed Stripe", PrimitiveType.Cube, new Vector3(0f, 0.04f, z), new Vector3(8.4f, 0.035f, 0.16f), Color.Lerp(edgeGlow, Color.white, 0.25f), true);
                trackMarkers.Add(marker);
            }

            // 天象（月亮/太阳/极光带）按主题布置：天空白昼不再挂月亮、海洋不再飘极光。
            ThemeSkyDressing.Build(theme, prop => AddSceneryProp(prop));

            // 冰晶簇与雪堆只属于寒带主题；海洋/天空的侧景交给各自的主题细节与套件。
            var wantCrystals = theme is RunnerMapTheme.IceLakeEcho or RunnerMapTheme.AuroraField or RunnerMapTheme.MistDike;
            var wantSnowBanks = theme is RunnerMapTheme.IceLakeEcho or RunnerMapTheme.CedarRuins or RunnerMapTheme.MistDike;
            for (var i = 0; i < 8; i++)
            {
                var z = 10f + i * 15f;
                AddCustomEnvironmentProp(theme, EnvironmentPropId.Light, -8.8f, z, 0.92f);
                AddCustomEnvironmentProp(theme, EnvironmentPropId.Light, 8.8f, z + 7.5f, 0.88f);
                if (i % 2 == 1)
                {
                    AddCheckpointGate(z + 4f);
                }

                // 旗帜放护坡外（SceneryMinAbsX=8.5），不再侵入可视走廊被淡出。
                AddCustomEnvironmentProp(theme, EnvironmentPropId.Signpost, -8.6f, z + 3.2f, 0.9f);
                AddCustomEnvironmentProp(theme, EnvironmentPropId.Signpost, 8.6f, z + 10.2f, 0.9f);
                if (wantCrystals)
                {
                    AddCrystalCluster(-8.6f - Random.Range(0f, 1.1f), z + Random.Range(1f, 7f));
                    AddCrystalCluster(8.6f + Random.Range(0f, 1.1f), z + Random.Range(1f, 7f));
                }
                if (wantSnowBanks)
                {
                    AddSnowBankGroup(-9.6f - Random.Range(0f, 2f), z + Random.Range(-3f, 3f));
                    AddSnowBankGroup(9.6f + Random.Range(0f, 2f), z + Random.Range(-3f, 3f));
                }
                AddThemeSetPiece(theme, z, i);
            }
            ThemeRoadsideDressing.PlaceFarBelt(theme, AddOpenNatureProp);

            // 远山剪影配色随主题走：雪松暮紫、海洋礁蓝、天空云白，不再六图共用冰蓝。
            var (ridgeBody, ridgeCap) = theme switch
            {
                RunnerMapTheme.CedarRuins => (new Color(0.24f, 0.18f, 0.26f), new Color(0.68f, 0.48f, 0.52f)),
                RunnerMapTheme.AuroraField => (new Color(0.08f, 0.24f, 0.38f), new Color(0.36f, 0.82f, 0.76f)),
                RunnerMapTheme.MistDike => (new Color(0.24f, 0.3f, 0.38f), new Color(0.52f, 0.6f, 0.68f)),
                RunnerMapTheme.OceanReef => (new Color(0.04f, 0.26f, 0.38f), new Color(0.18f, 0.58f, 0.6f)),
                RunnerMapTheme.SkyFlight => (new Color(0.75f, 0.86f, 0.98f), new Color(1f, 1f, 1f)),
                _ => (new Color(0.16f, 0.36f, 0.62f), new Color(0.78f, 0.93f, 1f)),
            };
            for (var i = 0; i < 7; i++)
            {
                AddMountainGroup(-18f - Random.Range(0f, 4f), 36f + i * 20f, Random.Range(1.5f, 2.6f), ridgeBody, ridgeCap);
                AddMountainGroup(18f + Random.Range(0f, 4f), 44f + i * 20f, Random.Range(1.5f, 2.6f), ridgeBody, ridgeCap);
            }
        }

        /// <summary>
        /// 创建 32 块可循环弯道路面瓦片（每块 4 m，总循环周期 128 m）。
        /// 每块瓦片沿 <see cref="TrackTerrain.WorldCurveX"/> 定义的 S 形曲线放置并对齐切线方向。
        /// 在 <see cref="AnimateScenery"/> 中每帧更新位置与旋转，使瓦片无缝循环。
        /// </summary>
        private void BuildCurvedRoadTiles(RunnerMapTheme theme, Color roadBase, Color roadGloss, Color snowColor, Color edgeGlow)
        {
            var snowBerm = Color.Lerp(snowColor, Color.white, 0.32f);
            var roadTexture = LoadTexture(theme switch
            {
                RunnerMapTheme.CedarRuins => "tex_stone_surface",
                RunnerMapTheme.SkyFlight => "tex_fabric_surface",
                RunnerMapTheme.OceanReef => "tex_stone_surface",
                _ => "tex_ice_surface",
            });
            var snowTexture = LoadTexture("tex_ice_surface");
            const int count = 32;
            const float tileLen = TrackTerrain.RoadLoopLength / count; // = 4 m

            for (var i = 0; i < count; i++)
            {
                var tz = i * tileLen; // Z=0, 4, 8 … 124
                var tx = TrackTerrain.WorldCurveX(tz, 0f);
                var yaw = TrackTerrain.WorldCurveTangentYaw(tz, 0f);

                // tile 位于路面表面（所有物体以此为基础高度）
                var tile = new GameObject($"Road Tile {i}");
                var s0 = TrackTerrain.SurfaceY(tz, 0f, tuning.movement.groundY);
                tile.transform.position = new Vector3(tx, s0, tz);
                tile.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                // 路面主体 - 向下延伸形成厚度（子物体以 tile 为参考系，y<0表示在表面下方）
                var roadBaseGo = AddChildPrimitive(tile.transform, "Road Base", PrimitiveType.Cube,
                    new Vector3(0f, -0.06f, 0f), new Vector3(10.2f, 0.12f, tileLen + 0.12f), roadBase);
                ApplyTexture(roadBaseGo.GetComponent<Renderer>(), roadTexture, new Vector2(4f, 2.2f));
                var roadGlossGo = AddChildPrimitive(tile.transform, "Road Gloss", PrimitiveType.Cube,
                    new Vector3(0f, 0.005f, 0f), new Vector3(9.55f, 0.01f, tileLen), roadGloss, true);
                ApplyTexture(roadGlossGo.GetComponent<Renderer>(), roadTexture, new Vector2(4.4f, 2.6f));

                // 两侧雪地护坡 - 与路面表面平齐，略低一点
                var leftSnow = AddChildPrimitive(tile.transform, "Left Snow", PrimitiveType.Cube,
                    new Vector3(-8.2f, -0.02f, 0f), new Vector3(6.2f, 0.04f, tileLen + 0.12f), snowColor);
                var rightSnow = AddChildPrimitive(tile.transform, "Right Snow", PrimitiveType.Cube,
                    new Vector3(8.2f, -0.02f, 0f), new Vector3(6.2f, 0.04f, tileLen + 0.12f), snowColor);
                ApplyTexture(leftSnow.GetComponent<Renderer>(), snowTexture, new Vector2(2.8f, 1.6f));
                ApplyTexture(rightSnow.GetComponent<Renderer>(), snowTexture, new Vector2(2.8f, 1.6f));

                // 道路边缘发光条 - 略高于表面
                AddChildPrimitive(tile.transform, "Left Edge Glow", PrimitiveType.Cube,
                    new Vector3(-5.2f, 0.02f, 0f), new Vector3(0.18f, 0.04f, tileLen), edgeGlow, true);
                AddChildPrimitive(tile.transform, "Right Edge Glow", PrimitiveType.Cube,
                    new Vector3(5.2f, 0.02f, 0f), new Vector3(0.18f, 0.04f, tileLen), edgeGlow, true);

                // 道路边护坡 - 略高于两侧雪地
                AddChildPrimitive(tile.transform, "Left Berm", PrimitiveType.Cube,
                    new Vector3(-5.85f, 0.01f, 0f), new Vector3(0.75f, 0.14f, tileLen), snowBerm);
                AddChildPrimitive(tile.transform, "Right Berm", PrimitiveType.Cube,
                    new Vector3(5.85f, 0.01f, 0f), new Vector3(0.75f, 0.14f, tileLen), snowBerm);

                roadTiles.Add(tile);
            }
        }

        private void BuildIceDetails(Color edgeGlow)
        {
            // 冰裂作为路面瓦片的子物体：随路滚动、随瓦片销毁。
            // 此前是无主静态物体——不随路动（视觉滑移），切主题也不清理，会跨主题残留叠加。
            for (var i = 0; i < roadTiles.Count; i += 3)
            {
                var tile = roadTiles[i].transform;
                AddIceCrack(tile, -2.2f + Random.Range(-0.7f, 0.7f), Random.Range(-1.6f, 1.6f), 0.55f + Random.value * 0.7f);
                AddIceCrack(tile, 2.2f + Random.Range(-0.7f, 0.7f), Random.Range(-1.6f, 1.6f), 0.55f + Random.value * 0.7f);
            }
        }

        private void BuildOceanDetails(Color edgeGlow)
        {
            // 海洋主题：添加海草、贝壳、气泡等细节。
            // 全部落在护坡外（SceneryMinAbsX=8.5）：此前海草长在三车道上，既违反可视走廊
            // 约束，又会被玩家误读成障碍。
            for (var i = 0; i < 12; i++)
            {
                var z = i * 10f - 5f;
                AddSeaweedCluster(-(8.6f + Random.Range(0f, 1.6f)), z + Random.Range(-2f, 2f));
                AddSeaweedCluster(8.6f + Random.Range(0f, 1.6f), z + Random.Range(-2f, 2f));
                if (Random.value < 0.4f)
                    AddSeashell(-(8.7f + Random.Range(0f, 1.2f)), z + Random.Range(-1f, 1f));
                if (Random.value < 0.4f)
                    AddSeashell(8.7f + Random.Range(0f, 1.2f), z + Random.Range(-1f, 1f));
            }
            // 上升的气泡群（护坡外侧，不进可视走廊）
            for (var i = 0; i < 8; i++)
            {
                var z = i * 15f + 10f;
                AddBubbleStream(RandomScenerySideX(), z);
            }
        }

        private void BuildSkyDetails(Color edgeGlow)
        {
            // 天空主题：添加云朵、阳光射线、飞鸟等细节
            for (var i = 0; i < 12; i++)
            {
                var z = 22f + i * 11f;
                AddFloatingCloud(RandomScenerySideX(), z, Random.Range(0.5f, 0.9f));
            }
            // 阳光射线（仅远场侧线天顶光）
            for (var i = 0; i < 4; i++)
            {
                var z = i * 28f + 48f;
                AddSunlightBeam(RandomScenerySideX(), z);
            }
            // 远处飞鸟剪影
            for (var i = 0; i < 10; i++)
            {
                var z = Random.Range(20f, 150f);
                AddBirdSilhouette(Random.Range(-15f, 15f), z, Random.Range(8f, 12f));
            }
        }

        /// <summary>冰拱地标：半崖石块。此前回退成一条 2.8m 冰裂，作为路边地标完全不可读。</summary>
        private void AddIceArchLandmark(float x, float z)
        {
            AddOpenNatureProp(NaturePropCatalog.IceArch(), x, z, Vector3.one, new Color(0.55f, 0.85f, 1f));
        }

        private void AddIceCrack(Transform tile, float x, float z, float length)
        {
            var group = new GameObject("Hairline Ice Crack");
            group.transform.SetParent(tile, false);
            group.transform.localPosition = new Vector3(x, 0.055f, z);
            var main = AddChildPrimitive(group.transform, "Crack Main", PrimitiveType.Cube, Vector3.zero, new Vector3(0.035f, 0.02f, length), new Color(0.78f, 0.98f, 1f, 0.45f), true);
            main.transform.localRotation = Quaternion.Euler(0f, Random.Range(-28f, 28f), 0f);
            var branch = AddChildPrimitive(group.transform, "Crack Branch", PrimitiveType.Cube, new Vector3(0.13f, 0f, length * 0.12f), new Vector3(0.025f, 0.02f, length * 0.46f), new Color(0.78f, 0.98f, 1f, 0.32f), true);
            branch.transform.localRotation = Quaternion.Euler(0f, Random.Range(32f, 58f), 0f);
        }

        private void AddSeaweedCluster(float x, float z)
        {
            AddOpenNatureProp(NaturePropCatalog.OceanWeed(Mathf.RoundToInt(Mathf.Abs(x + z))),
                x, z, Vector3.one, new Color(0.22f, 0.72f, 0.48f));
        }

        private void AddSeashell(float x, float z)
        {
            AddOpenNatureProp(NaturePropCatalog.OceanShell(Mathf.RoundToInt(Mathf.Abs(z))),
                x, z, Vector3.one, new Color(0.95f, 0.78f, 0.62f));
        }

        private void AddBubbleStream(float x, float z)
        {
            var group = new GameObject("Bubble Stream");
            group.transform.position = new Vector3(x, 0.1f, z);
            for (var i = 0; i < 5; i++)
            {
                var size = 0.05f + Random.value * 0.08f;
                AddChildPrimitive(group.transform, "Bubble", PrimitiveType.Sphere,
                    new Vector3(Random.Range(-0.2f, 0.2f), i * 0.25f, Random.Range(-0.2f, 0.2f)),
                    Vector3.one * size,
                    new Color(0.7f, 0.95f, 1f, 0.4f), true);
            }
            AddSceneryProp(group);
        }

        private void AddFloatingCloud(float x, float z, float scale)
        {
            AddOpenNatureProp(NaturePropCatalog.SkyCloud(), ClampScenerySideX(x), z,
                Vector3.one * Mathf.Max(0.55f, scale), new Color(0.92f, 0.96f, 1f),
                7.6f + Random.value * 2.4f, false);
        }

        private void AddSunlightBeam(float x, float z)
        {
            var group = new GameObject("Sunlight Beam");
            x = ClampScenerySideX(x);
            group.transform.position = new Vector3(x, 7f + Random.value * 2f, z);
            AddChildPrimitive(group.transform, "Beam", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.35f, 2.8f, 0.35f), new Color(1f, 0.95f, 0.7f, 0.08f), true);
            group.transform.localRotation = Quaternion.Euler(8f, Random.Range(0f, 360f), 0f);
            RegisterSceneryProp(group, x, z, snapToGround: false);
        }

        private void AddBirdSilhouette(float x, float z, float y)
        {
            var bird = RunnerVisuals.CreatePrimitive("Bird", PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(0.25f, 0.05f, 0.08f), new Color(0.3f, 0.35f, 0.45f));
            bird.transform.localRotation = Quaternion.Euler(0f, Random.Range(-30f, 30f), Random.Range(-10f, 10f));
            RegisterSceneryProp(bird, x, z, snapToGround: false);
        }

        private void AddCrystalCluster(float x, float z)
        {
            AddOpenNatureProp(NaturePropCatalog.IceCrystal(Mathf.RoundToInt(Mathf.Abs(x + z))),
                x, z, Vector3.one, new Color(0.45f, 0.85f, 1f));
        }

        private void AddThemeSetPiece(RunnerMapTheme theme, float z, int index)
        {
            switch (theme)
            {
                case RunnerMapTheme.IceLakeEcho:
                    AddOpenNatureProp(NaturePropCatalog.IceLakeTree(index),
                        index % 2 == 0 ? -10.4f : 10.4f, z + 5.2f,
                        new Vector3(1.2f, 1.2f, 1.2f), new Color(0.35f, 0.72f, 0.92f));
                    AddOpenNatureProp(NaturePropCatalog.IceLakeAccent(index),
                        index % 2 == 0 ? 9.8f : -9.8f, z + 11f,
                        new Vector3(1.1f, 1.1f, 1.1f),
                        index % 2 == 0 ? new Color(0.55f, 0.78f, 1f) : new Color(1f, 0.62f, 0.38f));
                    break;
                case RunnerMapTheme.CedarRuins:
                    AddCedarRuin(-7.1f, z + 2.2f, index);
                    AddCedarRuin(7.1f, z + 8.4f, index + 1);
                    if (index % 2 == 0)
                        AddOpenNatureProp(NaturePropCatalog.CedarTree(), -10.6f, z + 5.4f, new Vector3(1.25f, 1.25f, 1.25f), new Color(0.52f, 0.72f, 0.58f));
                    else
                        AddOpenNatureProp("tent_detailedOpen", 10.2f, z + 4.8f, new Vector3(1.1f, 1.1f, 1.1f), new Color(0.72f, 0.55f, 0.4f));
                    AddOpenNatureProp(NaturePropCatalog.CedarAccent(index),
                        index % 2 == 0 ? -9.2f : 9.2f, z + 12f, Vector3.one, new Color(0.7f, 0.7f, 0.72f));
                    break;
                case RunnerMapTheme.AuroraField:
                    AddAuroraBeacon(-7.8f, z + 2.6f, index);
                    AddAuroraBeacon(7.8f, z + 9.1f, index + 1);
                    AddOpenNatureProp(NaturePropCatalog.AuroraTree(index),
                        index % 2 == 0 ? -10.8f : 10.8f, z + 5.1f,
                        new Vector3(1.05f, 1.05f, 1.05f), new Color(0.68f, 0.82f, 1f));
                    AddOpenNatureProp(NaturePropCatalog.AuroraAccent(index),
                        index % 2 == 0 ? 9.5f : -9.5f, z + 14f, Vector3.one * 1.4f, new Color(0.75f, 0.55f, 1f));
                    break;
                case RunnerMapTheme.MistDike:
                    AddMistVeil(-6.9f, z + 2.8f, index);
                    AddMistVeil(6.9f, z + 8.7f, index + 1);
                    AddOpenNatureProp(NaturePropCatalog.MistAccentA(index),
                        index % 2 == 0 ? -9.9f : 9.9f, z + 5.6f,
                        index % 2 == 0 ? new Vector3(0.9f, 0.9f, 0.9f) : new Vector3(1.3f, 1.3f, 1.3f),
                        new Color(0.74f, 0.86f, 0.94f));
                    AddOpenNatureProp(NaturePropCatalog.MistAccentB(index),
                        index % 2 == 0 ? 10.1f : -10.1f, z + 13f, Vector3.one, new Color(0.55f, 0.7f, 0.62f));
                    break;
                case RunnerMapTheme.OceanReef:
                    AddCoralReef(-7.5f, z + 2.5f, index);
                    AddCoralReef(7.5f, z + 9.0f, index + 1);
                    if (index % 3 == 0)
                        AddTreasureChest(-6.2f, z + 5.5f);
                    AddOpenNatureProp(index % 2 == 0 ? NaturePropCatalog.OceanTree() : "canoe",
                        index % 2 == 0 ? -10.5f : 10.5f, z + 6.2f,
                        new Vector3(1.15f, 1.15f, 1.15f), new Color(0.58f, 0.82f, 0.86f));
                    AddOpenNatureProp(index % 2 == 0 ? "CubePets/cubepet_fish" : "CubePets/cubepet_crab",
                        index % 2 == 0 ? 9.4f : -9.4f, z + 14f, Vector3.one, new Color(0.7f, 0.9f, 0.95f));
                    break;
                case RunnerMapTheme.SkyFlight:
                    AddSkyArch(-8.0f, z + 3.0f, index);
                    AddSkyArch(8.0f, z + 9.5f, index + 1);
                    if (index % 3 == 0)
                        AddRainbow(z + 26f);
                    AddOpenNatureProp(index % 2 == 0 ? "prop_flag" : NaturePropCatalog.SkyCloud(),
                        index % 2 == 0 ? -11.2f : 11.2f, z + 5.8f,
                        new Vector3(0.95f, 0.95f, 0.95f), new Color(0.82f, 0.9f, 1f));
                    AddOpenNatureProp(NaturePropCatalog.SkyAccent(index), index % 2 == 0 ? 9.6f : -9.6f, z + 15f, Vector3.one, new Color(0.86f, 0.7f, 0.5f));
                    break;
            }

            var profile = ThemePresentationProfile.For(theme);
            if (!AddCustomEnvironmentProp(theme, profile.Landmark, -10.8f, z + 13f, 1f))
                AddProfileLandmark(profile.PrimaryLandmark, -10.8f, z + 13f, index, profile.AccentColor);
            if (index % 2 == 0)
            {
                if (!AddCustomEnvironmentProp(theme, profile.Landmark, 10.8f, z + 17f, .82f))
                    AddProfileLandmark(profile.SecondaryLandmark, 10.8f, z + 17f, index + 1, profile.AccentColor);
            }

            AddCustomEnvironmentProp(theme, profile.Signpost, -9.2f, z + 7.5f, .92f);
            AddCustomEnvironmentProp(theme, profile.Light, 9.0f, z + 11.2f, .88f);
            AddCustomEnvironmentProp(theme, profile.EdgeProp, index % 2 == 0 ? -8.8f : 8.8f,
                z + 20.5f, 1f);
        }

        private bool AddCustomEnvironmentProp(RunnerMapTheme theme, EnvironmentPropId propId,
            float x, float z, float scale)
        {
            x = ClampScenerySideX(x);
            var root = new GameObject($"Custom {theme} {propId}");
            root.transform.position = new Vector3(x, 0f, z);
            root.transform.localScale = Vector3.one * Mathf.Max(.1f, scale);
            if (!visualFactory.TryAttachEnvironment(root.transform, theme, propId))
            {
                Object.Destroy(root);
                return false;
            }
            RegisterSceneryProp(root, x, z);
            return true;
        }

        private void AddProfileLandmark(ThemeLandmarkKind kind, float x, float z, int index, Color accent)
        {
            switch (kind)
            {
                case ThemeLandmarkKind.IceCrystal:
                    AddCrystalCluster(x, z);
                    break;
                case ThemeLandmarkKind.CedarRuin:
                    AddCedarRuin(x, z, index);
                    break;
                case ThemeLandmarkKind.SnowPine:
                    AddOpenNatureProp(NaturePropCatalog.SnowPineLandmark(), x, z, Vector3.one * 1.15f, accent);
                    break;
                case ThemeLandmarkKind.AuroraBeacon:
                case ThemeLandmarkKind.MagneticCrystal:
                    AddAuroraBeacon(x, z, index);
                    break;
                case ThemeLandmarkKind.MistVeil:
                    AddMistVeil(x, z, index);
                    break;
                case ThemeLandmarkKind.FogStone:
                    AddOpenNatureProp(NaturePropCatalog.FogStoneLandmark(), x, z, Vector3.one * 1.3f, accent);
                    break;
                case ThemeLandmarkKind.CoralReef:
                    AddCoralReef(x, z, index);
                    break;
                case ThemeLandmarkKind.ShellCluster:
                    AddTreasureChest(x, z);
                    break;
                case ThemeLandmarkKind.SkyArch:
                    AddSkyArch(x, z, index);
                    break;
                case ThemeLandmarkKind.CloudIsland:
                    AddFloatingCloud(x, z, 1.5f);
                    break;
                case ThemeLandmarkKind.IceArch:
                    AddIceArchLandmark(x, z);
                    break;
            }
        }

        private void AddCoralReef(float x, float z, int index)
        {
            AddOpenNatureProp(NaturePropCatalog.CoralReef(index), x, z, Vector3.one, new Color(1f, 0.5f, 0.58f));
        }

        private void AddTreasureChest(float x, float z)
        {
            AddOpenNatureProp(NaturePropCatalog.TreasureChest(), x, z, Vector3.one, new Color(0.72f, 0.5f, 0.28f));
        }

        private void AddSkyArch(float x, float z, int index)
        {
            AddOpenNatureProp(NaturePropCatalog.SkyArch(index), x, z, Vector3.one, new Color(0.65f, 0.7f, 0.8f));
        }

        private void AddRainbow(float z)
        {
            AddRainbowAccent(z, -10.8f);
            AddRainbowAccent(z, 10.8f);
        }

        private void AddRainbowAccent(float z, float lateralOffset)
        {
            var trackX = TrackTerrain.WorldCurveX(z, 0f);
            var worldX = trackX + lateralOffset;
            var group = new GameObject("Rainbow Accent");
            group.transform.position = new Vector3(worldX, 10.5f, z);
            var colors = new[]
            {
                new Color(1f, 0.45f, 0.45f, 0.2f),
                new Color(1f, 0.75f, 0.35f, 0.2f),
                new Color(0.5f, 1f, 0.55f, 0.2f),
                new Color(0.45f, 0.78f, 1f, 0.2f),
            };
            for (var i = 0; i < colors.Length; i++)
            {
                AddChildPrimitive(
                    group.transform,
                    $"Rainbow Stripe {i}",
                    PrimitiveType.Cube,
                    new Vector3(0f, i * 0.22f, 0f),
                    new Vector3(1.35f, 0.12f, 0.08f),
                    colors[i],
                    true);
            }

            RegisterSceneryProp(group, worldX, z, snapToGround: false);
        }

        private void AddCedarRuin(float x, float z, int index)
        {
            AddOpenNatureProp(NaturePropCatalog.CedarRuin(index), x, z, Vector3.one, new Color(0.48f, 0.34f, 0.22f));
        }

        private void AddAuroraBeacon(float x, float z, int index)
        {
            AddOpenNatureProp(NaturePropCatalog.AuroraBeacon(index), x, z, Vector3.one, new Color(0.46f, 0.92f, 1f));
        }

        private void AddMistVeil(float x, float z, int index)
        {
            AddOpenNatureProp(NaturePropCatalog.MistVeil(index), x, z, Vector3.one, new Color(0.72f, 0.84f, 0.9f));
        }

        private void AddCheckpointGate(float z)
        {
            var group = new GameObject("Checkpoint Gate");
            group.transform.position = new Vector3(0f, 0f, z);
            var left = AddChildPrimitive(group.transform, "Gate Left", PrimitiveType.Cube, new Vector3(-5.55f, 1.1f, 0f), new Vector3(0.18f, 2.2f, 0.22f), new Color(0.18f, 0.52f, 0.7f));
            var right = AddChildPrimitive(group.transform, "Gate Right", PrimitiveType.Cube, new Vector3(5.55f, 1.1f, 0f), new Vector3(0.18f, 2.2f, 0.22f), new Color(0.18f, 0.52f, 0.7f));
            var top = AddChildPrimitive(group.transform, "Gate Top", PrimitiveType.Cube, new Vector3(0f, 1.85f, 0f), new Vector3(11.2f, 0.14f, 0.18f), new Color(0.42f, 1f, 0.86f, 0.22f), true);
            left.transform.SetParent(group.transform, false);
            right.transform.SetParent(group.transform, false);
            top.transform.SetParent(group.transform, false);
            AddSceneryProp(group);
        }

        private static GameObject AddChildPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color, bool transparent = false)
        {
            var child = RunnerVisuals.CreatePrimitive(name, type, Vector3.zero, localScale, color, transparent);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            return child;
        }

        private Texture2D LoadTexture(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (textureCache.TryGetValue(name, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>($"PenguinRun/Textures/{name}");
            textureCache[name] = tex;
            return tex;
        }

        private void AddOpenNatureProp(string modelName, float x, float z, Vector3 scale, Color tint) =>
            AddOpenNatureProp(modelName, x, z, scale, tint, 0f, true);

        private void AddOpenNatureProp(string modelName, float x, float z, Vector3 scale, Color tint,
            float y, bool snapToGround)
        {
            var prefab = LoadModelPrefab(modelName);
            if (prefab == null)
            {
                return;
            }

            x = ClampScenerySideX(x);
            var go = Object.Instantiate(prefab);
            go.name = $"OpenModel_{modelName}";
            go.transform.position = new Vector3(x, y, z);
            go.transform.localScale = NaturePropCatalog.ApplyScale(modelName, scale);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                var mat = renderer.material;
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor"))
                {
                    var c = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", Color.Lerp(c, tint, 0.55f));
                }
                if (mat.HasProperty("_Color"))
                {
                    var c = mat.GetColor("_Color");
                    mat.SetColor("_Color", Color.Lerp(c, tint, 0.55f));
                }
            }

            RegisterSceneryProp(go, x, z, snapToGround);
        }

        private GameObject LoadModelPrefab(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return null;
            if (modelPrefabCache.TryGetValue(modelName, out var cached))
            {
                return cached;
            }

            var prefab = Resources.Load<GameObject>($"PenguinRun/Models/{modelName}");
            modelPrefabCache[modelName] = prefab;
            return prefab;
        }

        private static void ApplyTexture(Renderer renderer, Texture2D texture, Vector2 tiling)
        {
            if (renderer == null || texture == null) return;
            var material = renderer.material;
            if (material == null) return;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }
        }

        private void AddSnowBankGroup(float x, float z)
        {
            AddOpenNatureProp(NaturePropCatalog.SnowBank(Mathf.RoundToInt(Mathf.Abs(z))),
                x, z, Vector3.one, new Color(0.86f, 0.96f, 1f));
        }

        private void AddMountainGroup(float x, float z, float height, Color bodyColor, Color capColor)
        {
            RegisterSceneryProp(ThemeRidgeScenery.CreateRidgeGroup(x, z, height, bodyColor, capColor), x, z);
        }

        private void AnimateScenery(float dt, float effectiveSpeed)
        {
            if (game == null) return;

            var d = game.World.Distance;
            var gy = tuning.movement.groundY;
            var roadRecycleZ = GetBehindCameraCullZ(12f, -22f);
            var markerRecycleZ = GetBehindCameraCullZ(16f, -26f);
            var propRecycleZ = GetBehindCameraCullZ(24f, -32f);

            // ── 弯道路面瓦片：随世界向后滚动，超出后方阈值则循环跳回前方 ──────────
            foreach (var tile in roadTiles)
            {
                tile.transform.Translate(Vector3.back * (effectiveSpeed * dt), Space.World);
                if (tile.transform.position.z < roadRecycleZ)
                {
                    tile.transform.position += Vector3.forward * TrackTerrain.RoadLoopLength;
                }

                // 根据当前 Z 实时更新瓦片中心的弯道 X 偏移与切线朝向
                var tz = tile.transform.position.z;
                var surfaceY = TrackTerrain.SurfaceY(tz, d, gy);
                var tp = tile.transform.position;
                tp.x = TrackTerrain.WorldCurveX(tz, d);
                tp.y = surfaceY; // tile 位于路面表面
                tile.transform.position = tp;
                tile.transform.rotation = Quaternion.Euler(0f, TrackTerrain.WorldCurveTangentYaw(tz, d), 0f);
            }

            // ── 速度标线：贴在路面表面 ──────────────────────────────────────────
            foreach (var marker in trackMarkers)
            {
                marker.transform.Translate(Vector3.back * (effectiveSpeed * dt), Space.World);
                if (marker.transform.position.z < markerRecycleZ)
                {
                    marker.transform.position += Vector3.forward * 119f;
                }

                var mp = marker.transform.position;
                mp.x = TrackTerrain.WorldCurveX(mp.z, d);
                mp.y = TrackTerrain.SurfaceY(mp.z, d, gy) + 0.005f; // 略高避免 z-fighting
                marker.transform.position = mp;
            }

            var playerPos = player != null ? player.transform.position : Vector3.zero;
            var camPos = runnerCamera != null ? runnerCamera.transform.position : playerPos;

            for (var i = 0; i < sceneryPropInfos.Count; i++)
            {
                var info = sceneryPropInfos[i];
                var tr = info.Tr;
                if (tr == null) continue;

                if (info.IsAuroraRibbon)
                {
                    tr.localScale = Vector3.Lerp(tr.localScale, new Vector3(0.95f + Mathf.Sin(Time.time + i) * 0.22f, 8.5f, 0.08f), Mathf.Clamp01(dt * 2f));
                    continue;
                }

                var oldZ = tr.position.z;
                tr.Translate(Vector3.back * (effectiveSpeed * dt * 0.72f), Space.World);
                if (info.IsCheckpointGate && oldZ >= 0f && tr.position.z < 0f)
                {
                    runnerAudio.PlayCheckpoint();
                }

                if (tr.position.z < propRecycleZ)
                {
                    tr.position += Vector3.forward * 132f;
                }

                EnsureSceneryLateralOffset(info, d);
                AlignSceneryToTrack(info, d, gy);

                if (!info.SkipCorridorFade)
                {
                    ApplyViewCorridorFade(info, playerPos, camPos, d, dt);
                }
            }
        }

        /// <summary>
        /// 布景注册的唯一入口：按名字算一次逐帧动画所需的常量属性并缓存。
        /// 每帧循环只读 <see cref="SceneryPropInfo"/>，不再碰 <c>GameObject.name</c> 或 <c>GetComponent</c>。
        /// </summary>
        private SceneryPropInfo AddSceneryProp(GameObject prop)
        {
            if (prop == null) return null;
            var name = prop.name;
            var info = new SceneryPropInfo
            {
                Go = prop,
                Tr = prop.transform,
                Lateral = prop.GetComponent<SceneryLateralOffset>(),
                IsAuroraRibbon = name.StartsWith("Aurora Ribbon", System.StringComparison.Ordinal),
                IsCheckpointGate = name.StartsWith("Checkpoint Gate", System.StringComparison.Ordinal),
                SkipCorridorFade = ShouldSkipViewCorridorFadeName(name),
                ForceHideInCorridor = ShouldForceHideSceneryInCorridor(name),
                SnapToGround = IsGroundSceneryName(name),
            };
            sceneryProps.Add(prop);
            sceneryPropInfos.Add(info);
            return info;
        }

        private void RegisterSceneryProp(GameObject prop, float worldX, float worldZ, bool snapToGround = true)
        {
            var lateral = prop.GetComponent<SceneryLateralOffset>();
            if (lateral == null)
            {
                lateral = prop.AddComponent<SceneryLateralOffset>();
            }

            lateral.LateralOffset = worldX - TrackTerrain.WorldCurveX(worldZ, 0f);
            lateral.SnapToGround = snapToGround;
            var info = AddSceneryProp(prop);
            if (info != null) info.Lateral = lateral;
            sceneryPropRenderers[prop] = prop.GetComponentsInChildren<Renderer>(false);
        }

        private static void EnsureSceneryLateralOffset(SceneryPropInfo info, float runDistance)
        {
            if (info.Lateral != null || info.SkipCorridorFade) return;

            var pos = info.Tr.position;
            var lateral = info.Go.AddComponent<SceneryLateralOffset>();
            lateral.LateralOffset = pos.x - TrackTerrain.WorldCurveX(pos.z, runDistance);
            lateral.SnapToGround = info.SnapToGround;
            info.Lateral = lateral;
        }

        private static bool ShouldForceHideSceneryInCorridor(string name)
        {
            return name.Contains("Rainbow")
                || name.Contains("Distant Ice Ridge")
                || name.Contains("Sunlight Beam")
                || name.Contains("Floating Cloud")
                || name.Contains("sky_cloud")
                || name.Contains("Sky Arch");
        }

        private static bool IsGroundSceneryName(string name)
        {
            return !name.Contains("Cloud")
                && !name.Contains("Beam")
                && !name.Contains("Bird")
                && !name.Contains("Rainbow")
                && !name.Contains("Aurora Ribbon")
                && name != "Moon"
                && name != "Moon Glow";
        }

        private static void AlignSceneryToTrack(SceneryPropInfo info, float runDistance, float groundY)
        {
            var lateral = info.Lateral;
            if (lateral == null) return;

            var pos = info.Tr.position;
            pos.x = TrackTerrain.WorldCurveX(pos.z, runDistance) + lateral.LateralOffset;
            if (lateral.SnapToGround)
            {
                pos.y = TrackTerrain.SurfaceY(pos.z, runDistance, groundY);
            }

            info.Tr.position = pos;
        }

        private static float ClampScenerySideX(float x)
        {
            var sign = x < 0f ? -1f : 1f;
            if (Mathf.Approximately(x, 0f)) sign = Random.value < 0.5f ? -1f : 1f;
            return sign * Mathf.Max(Mathf.Abs(x), SceneryMinAbsX);
        }

        private static float RandomScenerySideX()
        {
            var side = Random.value < 0.5f ? -1f : 1f;
            return side * Random.Range(SceneryMinAbsX, 11.5f);
        }

        private static bool ShouldSkipViewCorridorFadeName(string n)
        {
            return n.StartsWith("Aurora Ribbon", System.StringComparison.Ordinal)
                || n == "Moon"
                || n == "Moon Glow";
        }

        private static bool IsInViewCorridor(Vector3 worldPos, Vector3 playerPos, Vector3 camPos, float runDistance)
        {
            if (worldPos.y > ViewCorridorMaxY) return false;
            var trackX = TrackTerrain.WorldCurveX(worldPos.z, runDistance);
            if (Mathf.Abs(worldPos.x - trackX) >= ViewCorridorHalfWidth) return false;
            var zMin = camPos.z + ViewCorridorNearZBehindCamera;
            var zMax = playerPos.z + ViewCorridorNearZAheadPlayer;
            return worldPos.z >= zMin && worldPos.z <= zMax;
        }

        private void ApplyViewCorridorFade(SceneryPropInfo info, Vector3 playerPos, Vector3 camPos, float runDistance, float dt)
        {
            var prop = info.Go;
            if (!sceneryPropRenderers.TryGetValue(prop, out var renderers))
            {
                renderers = prop.GetComponentsInChildren<Renderer>(false);
                if (renderers.Length == 0) return;
                sceneryPropRenderers[prop] = renderers;
            }

            if (renderers.Length == 0) return;

            var center = renderers[0].bounds.center;
            for (var i = 1; i < renderers.Length; i++)
            {
                center = Vector3.Lerp(center, renderers[i].bounds.center, 0.5f);
            }

            var inCorridor = IsInViewCorridor(center, playerPos, camPos, runDistance);
            var fadeRate = Mathf.Clamp01(dt * 12f);
            var forceHide = inCorridor && info.ForceHideInCorridor;

            foreach (var renderer in renderers)
            {
                var id = renderer.GetEntityId().GetHashCode();
                if (!sceneryRendererBaseEnabled.TryGetValue(id, out var baseEnabled))
                {
                    baseEnabled = renderer.enabled;
                    sceneryRendererBaseEnabled[id] = baseEnabled;
                }

                if (!sceneryRendererBaseAlpha.TryGetValue(id, out var baseAlpha))
                {
                    var shared = renderer.sharedMaterial;
                    baseAlpha = shared != null ? RunnerVisuals.GetColor(shared).a : 1f;
                    sceneryRendererBaseAlpha[id] = baseAlpha;
                }

                if (forceHide)
                {
                    renderer.enabled = false;
                    continue;
                }

                renderer.enabled = baseEnabled;

                var targetAlpha = inCorridor ? Mathf.Min(baseAlpha, ViewCorridorFadedAlpha) : baseAlpha;
                // 必须读逐渲染器的 MPB 覆盖值：写侧走 SetRendererAlpha（MPB），若这里读
                // sharedMaterial，读到的是全场同色物件共享、永不变化的缓存材质，插值每帧
                // 都从同一基准重来，走廊内物件永远淡不到目标 alpha。
                var currentAlpha = RunnerVisuals.GetRendererColor(renderer).a;
                RunnerVisuals.SetRendererAlpha(renderer, Mathf.Lerp(currentAlpha, targetAlpha, fadeRate));
            }
        }

        /// <summary>
        /// 计算对象回收阈值：优先基于相机位置，确保离开视野后再回收。
        /// </summary>
        private float GetBehindCameraCullZ(float extraBehindCamera, float fallbackZ)
        {
            if (runnerCamera == null) return fallbackZ;
            return runnerCamera.transform.position.z - extraBehindCamera;
        }

        private float landingBounceTimer;
        private float landingBounceOffset;
        private float slideHeightSmooth;

        private void UpdateCamera(float dt)
        {
            if (runnerCamera == null || player == null || game == null) return;

            var gy = tuning.movement.groundY;
            var d = game.World.Distance;
            var surface = TrackTerrain.SurfaceY(player.transform.position.z, d, gy);
            var yawTarget = TrackTerrain.CurveYawDegrees(d) * 0.72f;
            cameraYawSmoothed = Mathf.SmoothDampAngle(cameraYawSmoothed, yawTarget, ref cameraYawVel, 0.36f, 105f, dt);

            var cam = tuning.camera;
            var boss = game.Boss?.CurrentBoss;
            var bossFight = game.Boss != null && game.Boss.BossActive;
            var bossDanger = bossFight && boss != null && boss.IsPatternDangerous;

            // ── 速度动态参数 ─────────────────────────────────────────
            var speed01 = game.World.Speed01();
            var dynamicBack = Mathf.Lerp(0.35f, 1.15f, speed01);
            var bossBack = bossFight ? (bossDanger ? 2.8f : 1.9f) : 0f;
            var dynamicHeight = Mathf.Lerp(0.15f, 0.55f, speed01);
            var bossHeight = bossFight ? (bossDanger ? 1.1f : 0.65f) : 0f;
            var dynamicAimAhead = bossFight ? (bossDanger ? 3.4f : 2.2f)
                : Mathf.Lerp(0.8f, 2.0f, speed01);

            // 横向跟随权重：boss 战时减少贴身跟随，确保前方读图更稳定。
            var lateralCam = bossFight ? 0.14f : 0.2f;
            var lateralAim = bossFight ? 0.19f : 0.26f;
            var curveX = TrackTerrain.CurveCameraX(d) * 0.45f;
            var px = player.transform.position;

            var airborne = Mathf.Clamp(game.Motor.PlayerY - surface, 0f, 2.4f);
            // 跳跃时相机稍微抬起
            var jumpBoost = airborne * 0.22f;

            // ── 着陆弹跳 ──────────────────────────────────────────────
            if (game.Motor.Grounded && !wasGroundedLastFrame)
            {
                landingBounceTimer = 0.14f;
                landingBounceOffset = -0.22f;
            }
            if (landingBounceTimer > 0f)
            {
                landingBounceTimer -= dt;
                landingBounceOffset *= 0.8f; // 指数衰减
            }
            else
            {
                landingBounceOffset = 0f;
            }

            // ── 滑铲镜头下沉 ───────────────────────────────────────────
            var targetSlideHeight = game.Motor.SlideTimer > 0f ? -0.55f : 0f;
            slideHeightSmooth = Mathf.Lerp(slideHeightSmooth, targetSlideHeight, 1f - Mathf.Exp(-dt * 8f));

            var targetCam = new Vector3(
                px.x * lateralCam + curveX,
                surface + cam.heightAboveSurface + dynamicHeight + bossHeight + jumpBoost + landingBounceOffset + slideHeightSmooth,
                px.z - (cam.behindDistance + dynamicBack + bossBack));

            var aim = new Vector3(
                px.x * lateralAim + curveX * 0.4f,
                surface + cam.aimHeightAboveSurface + jumpBoost * 0.25f + landingBounceOffset * 0.3f,
                px.z + cam.aimAhead + dynamicAimAhead);

            if (cameraShakeTimer > 0f)
            {
                cameraShakeTimer -= dt;
                var shakeX = (Random.value - 0.5f) * cameraShakeIntensity;
                var shakeY = (Random.value - 0.5f) * cameraShakeIntensity;
                targetCam += new Vector3(shakeX, shakeY, 0f);
            }

            if (defeatFreezeTimer > 0f)
            {
                defeatFreezeTimer -= dt;
                dt *= 0.1f;
            }

            runnerCamera.transform.position = Vector3.SmoothDamp(runnerCamera.transform.position, targetCam, ref cameraVelocity, 0.18f, 52f, dt);

            // ── 速度动态 FOV：低速窄视野，高速宽视野 ───────────────────────
            var speedFov = Mathf.Lerp(58f, 78f, speed01);
            var dashFovBoost = game.World.DashTimer > 0f ? 7f : 0f;
            var bossFovBoost = bossFight ? (bossDanger ? 4f : 2f) : 0f;
            var targetFov = speedFov + dashFovBoost + bossFovBoost;
            runnerCamera.fieldOfView = Mathf.Lerp(runnerCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-dt * 10f));

            var dir = aim - runnerCamera.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                var e = look.eulerAngles;
                e.y += cameraYawSmoothed;
                e.z = TrackTerrain.BankAngleDegrees(d);
                var targetRot = Quaternion.Euler(e.x, e.y, e.z);
                runnerCamera.transform.rotation = Quaternion.Slerp(
                    runnerCamera.transform.rotation,
                    targetRot,
                    1f - Mathf.Exp(-dt * 8.5f));
            }
        }

        private void UpdateLaneGuides(float dt)
        {
            if (game == null) return;

            var lw = tuning.movement.laneWidth;
            var activeX = game.Motor.Lane.ToX(lw);
            var d = game.World.Distance;
            var gy = tuning.movement.groundY;
            for (var i = 0; i < laneGuides.Count; i++)
            {
                var guide = laneGuides[i];
                var p = guide.transform.position;
                p.y = TrackTerrain.SurfaceY(p.z, d, gy) + 0.02f;
                guide.transform.position = p;
                var rend = i < laneGuideRenderers.Count ? laneGuideRenderers[i] : null;
                if (rend == null) continue;
                // 两侧导条共享缓存材质：高亮必须走逐渲染器 MPB，写 sharedMaterial 会三条同变。
                var active = Mathf.Abs(p.x - activeX) < 0.1f;
                RunnerVisuals.SetRendererColor(
                    rend,
                    Color.Lerp(
                        RunnerVisuals.GetRendererColor(rend),
                        active ? new Color(0.57f, 1f, 0.86f, 0.92f) : new Color(0.82f, 0.93f, 1f, 0.35f),
                        Mathf.Clamp01(dt * 8f)));
            }
        }

        private static void ConfigureRuntimePerformance(bool active)
        {
            QualitySettings.vSyncCount = Application.isMobilePlatform ? 0 : (active ? 1 : 0);
            Application.targetFrameRate = active ? 60 : 15;
            Screen.sleepTimeout = active ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            Input.gyro.enabled = false;
        }

        private static void ApplyMobileQualityBudget()
        {
            // 原本是一套写死的移动端预算，所有 Android 设备共用；改为按设备能力选档。
            // 必须早于 RunnerPostFx.TryAttach——后处理档位读的就是这里写入的结果。
            //
            // 与旧版的另一处差别：旧版对非移动端直接 return，桌面/编辑器沿用项目默认画质档；
            // 现在桌面固定落在 High 档，使编辑器截图门禁看到的画面与高端机发布版一致。
            RunnerDeviceTier.Apply();
        }

        private void OnGUI()
        {
            if (game == null)
                return;

            // ── 倒计时覆盖层 ──────────────────────────────────────────
            DrawEntranceOverlay();

            // ── 死亡序列覆盖层 ────────────────────────────────────────
            DrawDeathOverlay();

            DrawThemeTransitionOverlay();
            DrawScreenFlashOverlay();
        }

        private void DrawEntranceOverlay()
        {
            if (game == null || !game.IsCountdown || game.EntrancePhase == EntrancePhase.None) return;

            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.4f;
            var entranceTimer = game.EntranceTimer;

            if (game.EntrancePhase == EntrancePhase.Numbers)
            {
                var step = Mathf.FloorToInt(entranceTimer / 0.7f);
                var tInStep = (entranceTimer - step * 0.7f) / 0.7f;
                var scale = 1f + (1f - tInStep) * 0.5f;
                var alpha = 1f - tInStep * 0.6f;

                // OnGUI 每帧至少调用两次（Layout + Repaint）。样式建一次后只改字段，
                // 数字用预置常量串——倒计时正是最容易被察觉到卡顿的时刻。
                entranceNumberStyle ??= new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                entranceNumberStyle.fontSize = Mathf.RoundToInt(120f * scale);
                entranceNumberStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(cx - 100f, cy - 80f, 200f, 160f),
                    CountdownLabel(game.CountdownNumber), entranceNumberStyle);
            }
            else if (game.EntrancePhase == EntrancePhase.GoText)
            {
                var t = entranceTimer / 0.6f;
                var scale = 1f + t * 0.3f;
                var alpha = 1f - t;

                entranceGoStyle ??= new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                entranceGoStyle.fontSize = Mathf.RoundToInt(80f * scale);
                entranceGoStyle.normal.textColor = new Color(0.34f, 1f, 0.82f, alpha);
                GUI.Label(new Rect(cx - 200f, cy - 60f, 400f, 120f), "出发！", entranceGoStyle);
            }
        }

        /// <summary>倒计时数字的预置串，避免每次 OnGUI 走 int.ToString()。</summary>
        private static string CountdownLabel(int n) => n switch
        {
            1 => "1",
            2 => "2",
            3 => "3",
            _ => n.ToString(),
        };

        private void DrawScreenFlashOverlay()
        {
            if (screenFlashTimer <= 0f && !bossSpawnFlashPending) return;

            if (screenFlashTimer > 0f)
            {
                screenFlashTimer -= Time.deltaTime;
                var t = Mathf.Clamp01(screenFlashTimer / Mathf.Max(0.01f, screenFlashDuration));
                var alpha = screenFlashColor.a * (1f - t) * (1f - t); // ease-out quad
                screenFlashCurrent = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, alpha);
            }

            // Boss 出场预警：屏幕边缘红光脉冲
            if (bossSpawnFlashPending)
            {
                var pulse = Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f;
                var warnAlpha = bossSpawnFlashIntensity * pulse * 0.22f;
                var warnColor = new Color(0.8f, 0.15f, 0.08f, warnAlpha);

                // 顶部和底部边缘条
                var edgeH = Screen.height * 0.12f;
                GUI.color = warnColor;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, edgeH), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, Screen.height - edgeH, Screen.width, edgeH), Texture2D.whiteTexture);
            }

            if (screenFlashTimer <= 0f && !bossSpawnFlashPending) return;

            GUI.color = screenFlashCurrent;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void TryStartThemeTransition()
        {
            if (game == null || game.IsThemeTransition || !game.Running) return;
            if (game.Boss != null && game.Boss.BossActive) return;
            if (game.World.RunTime < nextThemeSwitchTime) return;

            pendingTheme = GetNextTheme(currentTheme);
            if (pendingTheme == currentTheme)
            {
                nextThemeSwitchTime += ThemeSwitchIntervalSeconds;
                return;
            }

            if (!game.TryBeginThemeTransition()) return;

            themeAppliedInTransition = false;
            themeTransitionTimer = 0f;
            nextThemeSwitchTime += ThemeSwitchIntervalSeconds;
            runnerAudio.PlayCheckpoint();
        }

        private void TickThemeTransition(float dt)
        {
            themeTransitionTimer += dt;
            var applyAt = ThemeTransitionFadeOutSeconds + ThemeTransitionHoldSeconds * 0.5f;
            if (!themeAppliedInTransition && themeTransitionTimer >= applyAt)
            {
                ApplyRuntimeThemeChange(pendingTheme);
                themeAppliedInTransition = true;
            }

            var total = ThemeTransitionFadeOutSeconds + ThemeTransitionHoldSeconds + ThemeTransitionFadeInSeconds;
            if (themeTransitionTimer >= total)
            {
                game.CompleteThemeTransition();
                themeAppliedInTransition = false;
                themeTransitionTimer = 0f;
            }
        }

        private void ApplyRuntimeThemeChange(RunnerMapTheme nextTheme)
        {
            currentTheme = nextTheme;
            RunSession.NextRunTheme = nextTheme;
            TrackTerrain.SetThemeProfile(nextTheme);
            ApplyMapThemeVisuals(nextTheme);
            RebuildThemeScenery(nextTheme);
            game.Spawner.ApplyTheme(nextTheme, clearSpawnedEntities: true);
            game.Spawner.SeedInitialObstacles(game.World.Distance);
        }

        private void RebuildThemeScenery(RunnerMapTheme theme)
        {
            ClearObjects(roadTiles);
            ClearObjects(trackMarkers);
            ClearObjects(sceneryProps);
            sceneryPropInfos.Clear();
            BuildIceWorld(theme);
        }

        private void ClearObjects(List<GameObject> objects)
        {
            for (var i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj != null)
                {
                    sceneryPropRenderers.Remove(obj);
                    Destroy(obj);
                }
            }
            objects.Clear();
        }

        private void DrawThemeTransitionOverlay()
        {
            if (game == null || !game.IsThemeTransition) return;

            var total = ThemeTransitionFadeOutSeconds + ThemeTransitionHoldSeconds + ThemeTransitionFadeInSeconds;
            var t = Mathf.Clamp(themeTransitionTimer, 0f, total);

            float alpha;
            if (t < ThemeTransitionFadeOutSeconds)
            {
                alpha = Mathf.SmoothStep(0f, 0.82f, t / ThemeTransitionFadeOutSeconds);
            }
            else if (t < ThemeTransitionFadeOutSeconds + ThemeTransitionHoldSeconds)
            {
                alpha = 0.82f;
            }
            else
            {
                var fadeInT = (t - ThemeTransitionFadeOutSeconds - ThemeTransitionHoldSeconds) / ThemeTransitionFadeInSeconds;
                alpha = Mathf.SmoothStep(0.82f, 0f, fadeInT);
            }

            if (alpha <= 0f) return;

            GUI.color = new Color(0.02f, 0.06f, 0.12f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var labelAlpha = Mathf.Clamp01(alpha / 0.82f) * 0.92f;
            if (themeTransitionLabelStyle == null)
            {
                themeTransitionLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 42,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            themeTransitionLabelStyle.normal.textColor = new Color(0.78f, 0.97f, 1f, labelAlpha);
            GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 56f), $"场景切换 · {GetThemeDisplayName(pendingTheme)}", themeTransitionLabelStyle);
        }

        private static RunnerMapTheme GetNextTheme(RunnerMapTheme theme)
        {
            return theme switch
            {
                RunnerMapTheme.IceLakeEcho => RunnerMapTheme.CedarRuins,
                RunnerMapTheme.CedarRuins => RunnerMapTheme.AuroraField,
                RunnerMapTheme.AuroraField => RunnerMapTheme.MistDike,
                RunnerMapTheme.MistDike => RunnerMapTheme.OceanReef,
                RunnerMapTheme.OceanReef => RunnerMapTheme.SkyFlight,
                _ => RunnerMapTheme.IceLakeEcho,
            };
        }

        // 主题展示名集中在 RunnerMapThemeInfo：这里原本有一份独立副本，且已经和大厅那份
        // 漂移（此处是「雾堤」，其余地方是「北境雾堤」）。
        private static string GetThemeDisplayName(RunnerMapTheme theme) =>
            RunnerMapThemeInfo.DisplayName(theme);
    }
}
