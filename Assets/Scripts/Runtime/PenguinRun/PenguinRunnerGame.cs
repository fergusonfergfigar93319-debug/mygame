using System.Collections.Generic;
using PenguinRun.Game;
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
        private readonly List<GameObject> trackMarkers = new();
        private readonly List<GameObject> sceneryProps = new();
        private readonly List<GameObject> roadTiles = new();
        private readonly Dictionary<string, Texture2D> textureCache = new();
        private readonly Dictionary<string, GameObject> modelPrefabCache = new();

        private RunnerSessionConfig config;
        private GameObject player;
        private PenguinCharacter character;
        private RunnerAudio runnerAudio;
        private Camera runnerCamera;
        private Vector3 cameraVelocity;
        private float cameraYawSmoothed;
        private float cameraYawVel;
        private RunnerHud hud;
        private ParticleEffectPool particlePool;
        private WorldEventScheduler worldEvents;
        private bool exiting;
        private bool warnedGameMissing;
        private RunnerMapTheme currentTheme;
        private RunnerMapTheme pendingTheme;
        private float nextThemeSwitchTime;
        private bool themeTransitionActive;
        private bool themeAppliedInTransition;
        private float themeTransitionTimer;
        private GUIStyle themeTransitionLabelStyle;

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

        // ── 开局倒计时 ──────────────────────────────────────────
        private enum EntranceState { None, Countdown, GoText }
        private EntranceState entranceState;
        private float entranceTimer;
        private int countdownNumber; // 3, 2, 1
        private bool entranceComplete;

        // ── 死亡序列 ────────────────────────────────────────────
        private enum DeathState { None, Freeze, FadeRed, ShowResult, WaitingTap }
        private DeathState deathState;
        private float deathStateTimer;
        private bool deathSequenceActive;
        private GUIStyle deathScoreStyle;
        private GUIStyle deathTitleStyle;

        private const float ThemeSwitchIntervalSeconds = 52f;
        private const float ThemeTransitionFadeOutSeconds = 0.45f;
        private const float ThemeTransitionHoldSeconds = 0.22f;
        private const float ThemeTransitionFadeInSeconds = 0.68f;

        private const float RunnerHudReferenceHeight = 1080f;

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

        /// <summary>IMGUI HUD：按屏幕高度对齐到 1080p 基准，供边缘触控阈值使用。</summary>
        private static float RunnerHudLayoutScale => Mathf.Max(Screen.height / RunnerHudReferenceHeight, 1e-4f);

        private static RunnerHudLayout ApplyRunnerHudGuiMatrix()
        {
            var safe = Screen.safeArea;
            var s = RunnerHudLayoutScale;
            var guiTop = Screen.height - safe.y - safe.height;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, guiTop, 0f), Quaternion.identity, new Vector3(s, s, 1f));
            return new RunnerHudLayout(safe.width / s, safe.height / s);
        }

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
            hud = new RunnerHud();
            BuildScene();
            particlePool = new ParticleEffectPool(transform, tuning.movement.groundY);
            worldEvents = new WorldEventScheduler();
            ApplyMapThemeVisuals(currentTheme);
            game = new RunnerGameController(tuning, config, runnerAudio, player.transform, character, transform, PulseHaptic, TriggerScreenFlash, OnRunFinished);
            game.ResetRun(false);
            lastLives = game.World.Lives;
        }

        private void OnRunFinished(RunnerRunResult result)
        {
            if (exiting) return;
            exiting = true;
            var applied = RunOutcomeRouter.Apply(
                result.Score, result.DistanceMeters, result.Coins, result.SurvivalSeconds, config.Daily,
                config.Difficulty,
                result.LastBossSpeedTier, result.LastBossSpeedFishBonus, result.LastBossSpeedScoreBonus);
            RunSession.LastResult = applied;
            SceneTransitionManager.Instance.TransitionTo("MainMenu");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            ConfigureRuntimePerformance(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            ConfigureRuntimePerformance(!pauseStatus);
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

            // ── 死亡序列：接管所有输入和模拟 ──────────────────────────
            if (deathSequenceActive)
            {
                TickDeathSequence(Time.deltaTime);
                return;
            }

            if (!entranceComplete && (Input.GetKeyDown(KeyCode.Escape) || IsReturnButtonTouch()))
            {
                // 倒计时中允许跳过
                if (entranceState != EntranceState.None)
                {
                    entranceComplete = true;
                    entranceState = EntranceState.None;
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || IsReturnButtonTouch())
            {
                game.FinishRun();
                return;
            }

            if (!game.Running)
            {
                TickEntranceOrIdle(Time.deltaTime);
                var idleDt = Mathf.Min(Time.deltaTime, 0.05f);
                UpdateCamera(idleDt);
                UpdateLaneGuides(idleDt);
                particlePool.Tick(idleDt, game.World.Distance);
                return;
            }

            // ── 检测游戏结束 → 启动死亡序列 ──────────────────────────
            if (game.GameOver && !deathSequenceActive)
            {
                StartDeathSequence();
                return;
            }

            if (themeTransitionActive)
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
            if (!game.IsSimulationFrozen)
            {
                AnimateScenery(dt, effectiveSpeed);
                game.TickRunningWorld(dt, effectiveSpeed);
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

        // ── 开局倒计时 / 空闲 ────────────────────────────────────
        private void TickEntranceOrIdle(float rawDt)
        {
            var dt = Mathf.Min(rawDt, 0.05f);

            if (!entranceComplete)
            {
                if (entranceState == EntranceState.None)
                {
                    entranceState = EntranceState.Countdown;
                    entranceTimer = 0f;
                    countdownNumber = 3;
                }

                entranceTimer += dt;
                if (entranceState == EntranceState.Countdown)
                {
                    // 每 0.7s 倒数一个数字
                    var step = Mathf.FloorToInt(entranceTimer / 0.7f);
                    var num = 3 - step;
                    if (num <= 0)
                    {
                        entranceState = EntranceState.GoText;
                        entranceTimer = 0f;
                        countdownNumber = 0;
                    }
                    else
                    {
                        countdownNumber = num;
                    }
                }
                else if (entranceState == EntranceState.GoText)
                {
                    if (entranceTimer >= 0.6f)
                    {
                        entranceComplete = true;
                        entranceState = EntranceState.None;
                        game.ResetRun(true);
                        game.FeedbackText = "出发！";
                        game.FeedbackTimer = 0.8f;
                    }
                }
            }

            game.TickIdle(rawDt);
        }

        // ── 死亡序列 ──────────────────────────────────────────────
        private void StartDeathSequence()
        {
            deathSequenceActive = true;
            deathState = DeathState.Freeze;
            deathStateTimer = 0f;
            TriggerScreenFlash(new Color(0.85f, 0.15f, 0.08f, 0.5f), 0.5f);
            TriggerCameraShake(0.35f, 0.08f);
        }

        private void TickDeathSequence(float rawDt)
        {
            var dt = Mathf.Min(rawDt, 0.05f);
            deathStateTimer += dt;

            switch (deathState)
            {
                case DeathState.Freeze:
                    // 冻结 0.4s，角色倒地动画
                    if (deathStateTimer >= 0.4f)
                    {
                        deathState = DeathState.FadeRed;
                        deathStateTimer = 0f;
                    }
                    // 期间驱动角色动画（gameOver 状态）
                    character.Animate(dt, false, game.Motor.Grounded, false, true, 0f, 0f, 0f, 0f, 0f, 0f);
                    game.RunnerAudio.UpdateRunState(0f, false, false, false, false);
                    UpdateCamera(dt);
                    break;

                case DeathState.FadeRed:
                    // 红屏渐显 0.6s
                    var redAlpha = Mathf.Clamp01(deathStateTimer / 0.6f) * 0.55f;
                    screenFlashColor = new Color(0.75f, 0.08f, 0.02f, redAlpha);
                    screenFlashCurrent = screenFlashColor;
                    screenFlashTimer = 0.02f; // 持续渲染
                    screenFlashDuration = 0.02f;

                    character.Animate(dt, false, game.Motor.Grounded, false, true, 0f, 0f, 0f, 0f, 0f, 0f);
                    UpdateCamera(dt);

                    if (deathStateTimer >= 0.6f)
                    {
                        deathState = DeathState.ShowResult;
                        deathStateTimer = 0f;
                    }
                    break;

                case DeathState.ShowResult:
                    // 显示结算 1.5s
                    character.Animate(dt, false, game.Motor.Grounded, false, true, 0f, 0f, 0f, 0f, 0f, 0f);
                    UpdateCamera(dt);

                    if (deathStateTimer >= 1.5f)
                    {
                        deathState = DeathState.WaitingTap;
                        deathStateTimer = 0f;
                    }
                    break;

                case DeathState.WaitingTap:
                    // 等待点击
                    character.Animate(dt, false, game.Motor.Grounded, false, true, 0f, 0f, 0f, 0f, 0f, 0f);
                    UpdateCamera(dt);

                    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        FinishDeathSequence();
                    }
                    break;
            }
        }

        private void FinishDeathSequence()
        {
            deathSequenceActive = false;
            deathState = DeathState.None;

            if (!exiting)
            {
                game.FinishRun();
            }
        }

        private void DrawDeathOverlay()
        {
            if (!deathSequenceActive) return;

            if (deathState == DeathState.ShowResult || deathState == DeathState.WaitingTap)
            {
                // 结算摘要
                var world = game.World;
                var cx = Screen.width * 0.5f;
                var cy = Screen.height * 0.38f;

                if (deathTitleStyle == null)
                {
                    deathTitleStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 56,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white },
                    };
                    deathScoreStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 36,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(1f, 0.92f, 0.55f) },
                    };
                }

                GUI.Label(new Rect(cx - 200f, cy, 400f, 70f), "游戏结束", deathTitleStyle);
                GUI.Label(new Rect(cx - 200f, cy + 72f, 400f, 50f), $"得分 {world.Score}", deathScoreStyle);
                GUI.Label(new Rect(cx - 200f, cy + 122f, 400f, 36f), $"距离 {Mathf.RoundToInt(world.Distance)}m · 鱼干 {world.Coins}", new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.8f, 0.9f, 1f) },
                });

                if (deathState == DeathState.WaitingTap)
                {
                    var pulse = Mathf.Sin(Time.time * 3f) * 0.3f + 0.7f;
                    var tapColor = new Color(1f, 1f, 1f, pulse);
                    var tapStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 26,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = tapColor },
                    };
                    GUI.Label(new Rect(cx - 200f, cy + 190f, 400f, 40f), "点击返回", tapStyle);
                }
            }

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

            // 收集金币粒子
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

            // 受伤粒子
            if (world.Lives < lastLives)
            {
                particlePool.EmitBurst(playerPos, new Color(1f, 0.2f, 0.1f), 12, 4f, 0.7f);
                lastLives = world.Lives;
            }
            else if (world.Lives > lastLives)
            {
                // 生命恢复粒子
                particlePool.EmitRing(playerPos, new Color(0.3f, 1f, 0.5f), 10, 1f, 2.5f, 0.5f);
                lastLives = world.Lives;
            }

            // 完美闪避粒子
            if (world.PerfectDodges > lastPerfectDodges)
            {
                particlePool.EmitBurst(playerPos, new Color(0.4f, 0.85f, 1f), 8, 3f, 0.6f);
                lastPerfectDodges = world.PerfectDodges;
            }

            // 着陆粒子
            if (motor.Grounded && !wasGroundedLastFrame)
            {
                var groundPos = new Vector3(playerPos.x, tuning.movement.groundY + 0.02f, playerPos.z);
                particlePool.Emit(groundPos, new Color(0.8f, 0.95f, 1f), 5, 1.2f, 0.35f, false);
            }
            wasGroundedLastFrame = motor.Grounded;

            // 滑铲火花
            if (motor.SlideTimer > 0f && !wasDodgingLastFrame)
            {
                var slidePos = new Vector3(playerPos.x, tuning.movement.groundY + 0.02f, playerPos.z - 0.5f);
                particlePool.Emit(slidePos, new Color(0.6f, 0.9f, 1f), 3, 0.8f, 0.3f, false);
            }
            wasDodgingLastFrame = motor.SlideTimer > 0f;
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
                // Boss 首次被击败：定格 + 震动 + 金色闪屏
                defeatFreezeTimer = 0.25f;
                cameraShakeTimer = 0.5f;
                cameraShakeIntensity = 0.15f;
                TriggerScreenFlash(new Color(1f, 0.85f, 0.2f, 0.45f), 0.55f);
                wasBossDefeated = true;

                // 金色粒子爆发
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
                runnerCamera.transform.DOShakePosition(duration, intensity, 25, 90, false, true);
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
            switch (theme)
            {
                case RunnerMapTheme.CedarRuins:
                    RenderSettings.fogColor = new Color(0.55f, 0.42f, 0.55f);
                    RenderSettings.ambientLight = new Color(0.48f, 0.38f, 0.42f);
                    if (runnerCamera != null)
                        runnerCamera.backgroundColor = new Color(0.42f, 0.52f, 0.72f);
                    break;
                case RunnerMapTheme.AuroraField:
                    RenderSettings.fogColor = new Color(0.35f, 0.52f, 0.62f);
                    RenderSettings.ambientLight = new Color(0.36f, 0.44f, 0.58f);
                    if (runnerCamera != null)
                        runnerCamera.backgroundColor = new Color(0.28f, 0.38f, 0.58f);
                    break;
                case RunnerMapTheme.MistDike:
                    RenderSettings.fogColor = new Color(0.42f, 0.46f, 0.5f);
                    RenderSettings.ambientLight = new Color(0.32f, 0.38f, 0.44f);
                    if (runnerCamera != null)
                        runnerCamera.backgroundColor = new Color(0.38f, 0.44f, 0.5f);
                    break;
                case RunnerMapTheme.OceanReef:
                    // 海洋主题：蓝绿色调，仿佛在水下
                    RenderSettings.fogColor = new Color(0.2f, 0.55f, 0.65f);
                    RenderSettings.ambientLight = new Color(0.25f, 0.55f, 0.6f);
                    if (runnerCamera != null)
                        runnerCamera.backgroundColor = new Color(0.15f, 0.45f, 0.55f);
                    break;
                case RunnerMapTheme.SkyFlight:
                    // 天空主题：明亮的蓝天白云
                    RenderSettings.fogColor = new Color(0.7f, 0.85f, 0.98f);
                    RenderSettings.ambientLight = new Color(0.65f, 0.75f, 0.9f);
                    if (runnerCamera != null)
                        runnerCamera.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
                    break;
                default:
                    RenderSettings.fogColor = new Color(0.66f, 0.82f, 0.9f);
                    RenderSettings.ambientLight = new Color(0.42f, 0.58f, 0.68f);
                    if (runnerCamera != null)
                        runnerCamera.backgroundColor = new Color(0.54f, 0.77f, 0.95f);
                    break;
            }
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
            ApplyRunnerCameraViewport169();

            RenderSettings.ambientLight = new Color(0.42f, 0.58f, 0.68f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.66f, 0.82f, 0.9f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 55f;   // 雾气起始距离后移
            RenderSettings.fogEndDistance = 180f;  // 雾气结束距离增加，后方跑道可见更远

            var light = new GameObject("Moon Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            player = new GameObject("Penguin Character");
            // 使用程序化 Mesh 生成的可爱卡通企鹅，替代 Primitive 拼接版本
            character = PenguinCharacter.CreateCute(player.transform);
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
                _ => new Color(0.08f, 0.43f, 0.72f),
            };
            var roadGloss = theme switch
            {
                RunnerMapTheme.CedarRuins => new Color(0.58f, 0.88f, 0.86f, 0.18f),
                RunnerMapTheme.AuroraField => new Color(0.5f, 0.9f, 1f, 0.28f),
                RunnerMapTheme.MistDike => new Color(0.78f, 0.9f, 0.96f, 0.16f),
                RunnerMapTheme.OceanReef => new Color(0.25f, 0.65f, 0.8f, 0.35f), // 海洋：青蓝色光泽
                RunnerMapTheme.SkyFlight => new Color(0.75f, 0.88f, 1f, 0.3f),    // 天空：白云光泽
                _ => new Color(0.36f, 0.82f, 0.98f, 0.22f),
            };
            var snowColor = theme switch
            {
                RunnerMapTheme.CedarRuins => new Color(0.74f, 0.82f, 0.82f),
                RunnerMapTheme.AuroraField => new Color(0.74f, 0.9f, 1f),
                RunnerMapTheme.MistDike => new Color(0.68f, 0.76f, 0.8f),
                RunnerMapTheme.OceanReef => new Color(0.8f, 0.92f, 0.95f),   // 海洋：浅蓝白色
                RunnerMapTheme.SkyFlight => new Color(0.98f, 0.98f, 1f),      // 天空：纯白
                _ => new Color(0.78f, 0.93f, 0.98f),
            };
            var edgeGlow = theme switch
            {
                RunnerMapTheme.CedarRuins => new Color(0.7f, 0.95f, 0.72f, 0.58f),
                RunnerMapTheme.AuroraField => new Color(0.66f, 0.54f, 1f, 0.78f),
                RunnerMapTheme.MistDike => new Color(0.8f, 0.94f, 1f, 0.42f),
                RunnerMapTheme.OceanReef => new Color(0.3f, 0.9f, 0.95f, 0.75f), // 海洋：亮青色
                RunnerMapTheme.SkyFlight => new Color(1f, 1f, 0.6f, 0.6f),        // 天空：阳光黄
                _ => new Color(0.66f, 0.95f, 1f, 0.82f),
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

            sceneryProps.Add(RunnerVisuals.CreatePrimitive("Moon", PrimitiveType.Sphere, new Vector3(8.6f, 13f, 58f), Vector3.one * 3.2f, new Color(0.9f, 0.95f, 0.96f)));
            sceneryProps.Add(RunnerVisuals.CreatePrimitive("Moon Glow", PrimitiveType.Sphere, new Vector3(8.6f, 13f, 58f), Vector3.one * 4.3f, new Color(0.74f, 0.92f, 1f, 0.14f), true));
            for (var i = 0; i < 5; i++)
            {
                var aurora = RunnerVisuals.CreatePrimitive(
                    $"Aurora Ribbon {i}",
                    PrimitiveType.Cube,
                    new Vector3(-7f + i * 3.6f, 9.2f + i * 0.42f, 50f + i * 7f),
                    new Vector3(0.9f, 8.5f, 0.08f),
                    i % 2 == 0 ? new Color(0.25f, 1f, 0.78f, 0.22f) : new Color(0.58f, 0.46f, 1f, 0.2f),
                    true);
                aurora.transform.rotation = Quaternion.Euler(0f, 0f, -18f + i * 13f);
                sceneryProps.Add(aurora);
            }

            for (var i = 0; i < 8; i++)
            {
                var z = 10f + i * 15f;
                AddLampGroup(-8.8f, z);
                AddLampGroup(8.8f, z + 7.5f);
                if (i % 2 == 1)
                {
                    AddCheckpointGate(z + 4f);
                }

                AddBannerFlag(-5.65f, z + 3.2f, i % 2 == 0);
                AddBannerFlag(5.65f, z + 10.2f, i % 2 != 0);
                AddCrystalCluster(-6.8f - Random.Range(0f, 1.1f), z + Random.Range(1f, 7f));
                AddCrystalCluster(6.8f + Random.Range(0f, 1.1f), z + Random.Range(1f, 7f));
                AddSnowBankGroup(-9.6f - Random.Range(0f, 2f), z + Random.Range(-3f, 3f));
                AddSnowBankGroup(9.6f + Random.Range(0f, 2f), z + Random.Range(-3f, 3f));
                AddThemeSetPiece(theme, z, i);
            }

            for (var i = 0; i < 7; i++)
            {
                AddMountainGroup(-18f - Random.Range(0f, 4f), 36f + i * 20f, Random.Range(1.5f, 2.6f));
                AddMountainGroup(18f + Random.Range(0f, 4f), 44f + i * 20f, Random.Range(1.5f, 2.6f));
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
            for (var i = 0; i < 10; i++)
            {
                var z = i * 12.4f - 4f;
                AddIceCrack(-2.2f + Random.Range(-0.7f, 0.7f), z + Random.Range(-1.6f, 1.6f), 0.55f + Random.value * 0.7f);
                AddIceCrack(2.2f + Random.Range(-0.7f, 0.7f), z + Random.Range(-1.6f, 1.6f), 0.55f + Random.value * 0.7f);
            }
        }

        private void BuildOceanDetails(Color edgeGlow)
        {
            // 海洋主题：添加海草、贝壳、气泡等细节
            for (var i = 0; i < 12; i++)
            {
                var z = i * 10f - 5f;
                // 海草群（远离三车道中心）
                AddSeaweedCluster(-3.2f + Random.Range(-0.9f, 0.9f), z + Random.Range(-2f, 2f));
                AddSeaweedCluster(3.2f + Random.Range(-0.9f, 0.9f), z + Random.Range(-2f, 2f));
                // 贝壳点缀
                if (Random.value < 0.4f)
                    AddSeashell(-3.5f + Random.Range(-0.6f, 0.6f), z + Random.Range(-1f, 1f));
                if (Random.value < 0.4f)
                    AddSeashell(3.5f + Random.Range(-0.6f, 0.6f), z + Random.Range(-1f, 1f));
            }
            // 上升的气泡群
            for (var i = 0; i < 8; i++)
            {
                var z = i * 15f + 10f;
                AddBubbleStream(Random.Range(-2f, 2f), z);
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

        private void AddIceCrack(float x, float z, float length)
        {
            var group = new GameObject("Hairline Ice Crack");
            group.transform.position = new Vector3(x, 0.055f, z);
            var main = AddChildPrimitive(group.transform, "Crack Main", PrimitiveType.Cube, Vector3.zero, new Vector3(0.035f, 0.02f, length), new Color(0.78f, 0.98f, 1f, 0.45f), true);
            main.transform.localRotation = Quaternion.Euler(0f, Random.Range(-28f, 28f), 0f);
            var branch = AddChildPrimitive(group.transform, "Crack Branch", PrimitiveType.Cube, new Vector3(0.13f, 0f, length * 0.12f), new Vector3(0.025f, 0.02f, length * 0.46f), new Color(0.78f, 0.98f, 1f, 0.32f), true);
            branch.transform.localRotation = Quaternion.Euler(0f, Random.Range(32f, 58f), 0f);
        }

        private void AddSeaweedCluster(float x, float z)
        {
            var group = new GameObject("Seaweed Cluster");
            group.transform.position = new Vector3(x, 0f, z);
            for (var i = 0; i < 3; i++)
            {
                var height = 0.4f + Random.value * 0.6f;
                var seaweed = AddChildPrimitive(group.transform, "Seaweed", PrimitiveType.Cube,
                    new Vector3((i - 1) * 0.15f + Random.Range(-0.05f, 0.05f), height * 0.5f, Random.Range(-0.1f, 0.1f)),
                    new Vector3(0.08f, height, 0.08f),
                    new Color(0.15f, 0.65f + Random.value * 0.2f, 0.35f));
                seaweed.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f));
            }
            sceneryProps.Add(group);
        }

        private void AddSeashell(float x, float z)
        {
            var group = new GameObject("Seashell");
            group.transform.position = new Vector3(x, 0.02f, z);
            AddChildPrimitive(group.transform, "Shell Base", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.18f, 0.08f, 0.15f), new Color(0.95f, 0.85f, 0.78f));
            AddChildPrimitive(group.transform, "Shell Glow", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.02f), new Vector3(0.06f, 0.02f, 0.08f), new Color(0.3f, 0.9f, 1f, 0.6f), true);
            sceneryProps.Add(group);
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
            sceneryProps.Add(group);
        }

        private void AddFloatingCloud(float x, float z, float scale)
        {
            var group = new GameObject("Floating Cloud");
            x = ClampScenerySideX(x);
            var y = 7.5f + Random.value * 2.5f;
            group.transform.position = new Vector3(x, y, z);
            AddChildPrimitive(group.transform, "Cloud Main", PrimitiveType.Sphere, Vector3.zero, new Vector3(1.2f, 0.6f, 0.8f) * scale, new Color(1f, 1f, 1f, 0.45f), true);
            AddChildPrimitive(group.transform, "Cloud Puff L", PrimitiveType.Sphere, new Vector3(-0.42f, 0.1f, 0f) * scale, new Vector3(0.55f, 0.4f, 0.5f) * scale, new Color(1f, 1f, 1f, 0.38f), true);
            AddChildPrimitive(group.transform, "Cloud Puff R", PrimitiveType.Sphere, new Vector3(0.42f, 0.12f, 0.08f) * scale, new Vector3(0.5f, 0.38f, 0.48f) * scale, new Color(1f, 1f, 1f, 0.4f), true);
            RegisterSceneryProp(group, x, z, snapToGround: false);
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

        private void AddLampGroup(float x, float z)
        {
            var group = new GameObject("Warm Track Lamp");
            group.transform.position = new Vector3(x, 0f, z);
            var pole = RunnerVisuals.CreatePrimitive("Lamp Pole", PrimitiveType.Cylinder, group.transform.position + new Vector3(0f, 0.65f, 0f), new Vector3(0.08f, 0.65f, 0.08f), new Color(0.12f, 0.24f, 0.32f));
            var lightBall = RunnerVisuals.CreatePrimitive("Lamp Glow", PrimitiveType.Sphere, group.transform.position + new Vector3(0f, 1.42f, 0f), Vector3.one * 0.28f, new Color(1f, 0.82f, 0.45f, 0.42f), true);
            var lightHalo = RunnerVisuals.CreatePrimitive("Lamp Soft Halo", PrimitiveType.Sphere, group.transform.position + new Vector3(0f, 1.42f, 0f), Vector3.one * 0.34f, new Color(1f, 0.7f, 0.32f, 0.08f), true);
            pole.transform.SetParent(group.transform, true);
            lightBall.transform.SetParent(group.transform, true);
            lightHalo.transform.SetParent(group.transform, true);
            RegisterSceneryProp(group, x, z);
        }

        private void AddBannerFlag(float x, float z, bool leftSide)
        {
            var group = new GameObject("Penguin Run Banner");
            group.transform.position = new Vector3(x, 0f, z);
            AddChildPrimitive(group.transform, "Banner Pole", PrimitiveType.Cylinder, new Vector3(0f, 0.76f, 0f), new Vector3(0.06f, 0.76f, 0.06f), new Color(0.16f, 0.28f, 0.36f));
            var flag = AddChildPrimitive(group.transform, "Blue Snow Flag", PrimitiveType.Cube, new Vector3(leftSide ? -0.28f : 0.28f, 1.36f, 0f), new Vector3(0.56f, 0.28f, 0.035f), new Color(0.05f, 0.48f, 0.9f));
            flag.transform.localRotation = Quaternion.Euler(0f, 0f, leftSide ? -5f : 5f);
            AddChildPrimitive(flag.transform, "Flag Highlight", PrimitiveType.Cube, new Vector3(0f, 0.16f, -0.03f), new Vector3(0.92f, 0.16f, 0.5f), new Color(0.48f, 0.88f, 1f, 0.38f), true);
            sceneryProps.Add(group);
        }

        private void AddCrystalCluster(float x, float z)
        {
            var group = new GameObject("Aurora Crystal Cluster");
            group.transform.position = new Vector3(x, 0f, z);
            for (var i = 0; i < 3; i++)
            {
                var height = 0.65f + Random.value * 0.75f;
                var crystal = AddChildPrimitive(
                    group.transform,
                    "Blue Ice Crystal",
                    PrimitiveType.Cube,
                    new Vector3((i - 1) * 0.26f, height * 0.42f, Random.Range(-0.12f, 0.12f)),
                    new Vector3(0.18f, height, 0.18f),
                    i == 1 ? new Color(0.34f, 0.92f, 1f, 0.72f) : new Color(0.54f, 0.7f, 1f, 0.64f),
                    true);
                crystal.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-16f, 16f));
            }

            sceneryProps.Add(group);
        }

        private void AddThemeSetPiece(RunnerMapTheme theme, float z, int index)
        {
            switch (theme)
            {
                case RunnerMapTheme.CedarRuins:
                    AddCedarRuin(-7.1f, z + 2.2f, index);
                    AddCedarRuin(7.1f, z + 8.4f, index + 1);
                    if (index % 2 == 0)
                    {
                        AddOpenNatureProp("tree_pineTallA", -10.6f, z + 5.4f, new Vector3(1.25f, 1.25f, 1.25f), new Color(0.52f, 0.72f, 0.58f));
                    }
                    else
                    {
                        AddOpenNatureProp("rock_largeA", 10.2f, z + 4.8f, new Vector3(1.4f, 1.2f, 1.4f), new Color(0.62f, 0.7f, 0.78f));
                    }
                    break;
                case RunnerMapTheme.AuroraField:
                    AddAuroraBeacon(-7.8f, z + 2.6f, index);
                    AddAuroraBeacon(7.8f, z + 9.1f, index + 1);
                    AddOpenNatureProp(index % 2 == 0 ? "leaftree" : "tree_simple",
                        index % 2 == 0 ? -10.8f : 10.8f,
                        z + 5.1f,
                        new Vector3(1.05f, 1.05f, 1.05f),
                        new Color(0.68f, 0.82f, 1f));
                    break;
                case RunnerMapTheme.MistDike:
                    AddMistVeil(-6.9f, z + 2.8f, index);
                    AddMistVeil(6.9f, z + 8.7f, index + 1);
                    AddOpenNatureProp(index % 2 == 0 ? "mushroom_redGroup" : "stone_2",
                        index % 2 == 0 ? -9.9f : 9.9f,
                        z + 5.6f,
                        index % 2 == 0 ? new Vector3(0.9f, 0.9f, 0.9f) : new Vector3(1.3f, 1.3f, 1.3f),
                        new Color(0.74f, 0.86f, 0.94f));
                    break;
                case RunnerMapTheme.OceanReef:
                    AddCoralReef(-7.5f, z + 2.5f, index);
                    AddCoralReef(7.5f, z + 9.0f, index + 1);
                    if (index % 3 == 0)
                        AddTreasureChest(-6.2f, z + 5.5f);
                    AddOpenNatureProp(index % 2 == 0 ? "stone_1" : "rock_smallA",
                        index % 2 == 0 ? -10.5f : 10.5f,
                        z + 6.2f,
                        new Vector3(1.15f, 1.15f, 1.15f),
                        new Color(0.58f, 0.82f, 0.86f));
                    break;
                case RunnerMapTheme.SkyFlight:
                    AddSkyArch(-8.0f, z + 3.0f, index);
                    AddSkyArch(8.0f, z + 9.5f, index + 1);
                    if (index % 3 == 0)
                        AddRainbow(z + 26f);
                    AddOpenNatureProp(index % 2 == 0 ? "tree_pineSmallA" : "tree_simple",
                        index % 2 == 0 ? -11.2f : 11.2f,
                        z + 5.8f,
                        new Vector3(0.95f, 0.95f, 0.95f),
                        new Color(0.82f, 0.9f, 1f));
                    break;
            }
        }

        private void AddCoralReef(float x, float z, int index)
        {
            var group = new GameObject("Coral Reef Set");
            group.transform.position = new Vector3(x, 0f, z);
            var side = x < 0f ? -1f : 1f;
            // 主珊瑚柱
            AddChildPrimitive(group.transform, "Coral Stalk", PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(0.55f, 1.0f, 0.55f), new Color(1f, 0.45f, 0.55f));
            // 珊瑚分支
            for (var i = 0; i < 3; i++)
            {
                var branch = AddChildPrimitive(group.transform, $"Coral Branch {i}", PrimitiveType.Cylinder,
                    new Vector3(side * (0.3f + i * 0.15f), 1.5f + i * 0.25f, Mathf.Sin(i * 1.7f) * 0.2f),
                    new Vector3(0.32f, 0.4f, 0.32f),
                    Color.Lerp(new Color(1f, 0.4f, 0.5f), new Color(1f, 0.7f, 0.75f), i / 3f));
                branch.transform.localRotation = Quaternion.Euler(20f * side, 0f, side * 35f);
            }
            // 顶部珊瑚簇
            AddChildPrimitive(group.transform, "Coral Bloom", PrimitiveType.Sphere, new Vector3(0f, 2.1f, 0f), new Vector3(0.55f, 0.4f, 0.55f), new Color(1f, 0.6f, 0.7f));
            // 海星点缀
            if (index % 2 == 0)
            {
                var starfish = AddChildPrimitive(group.transform, "Starfish", PrimitiveType.Cube, new Vector3(side * 0.6f, 0.1f, 0.4f), new Vector3(0.35f, 0.04f, 0.35f), new Color(1f, 0.65f, 0.3f));
                starfish.transform.localRotation = Quaternion.Euler(0f, 30f * index, 0f);
            }
            // 海葵触手
            for (var i = 0; i < 5; i++)
            {
                var ang = i * 72f * Mathf.Deg2Rad;
                var tentacle = AddChildPrimitive(group.transform, $"Anemone {i}", PrimitiveType.Capsule,
                    new Vector3(Mathf.Cos(ang) * 0.18f, 0.4f, Mathf.Sin(ang) * 0.18f),
                    new Vector3(0.08f, 0.3f, 0.08f),
                    new Color(0.65f, 0.85f, 1f, 0.85f), true);
                tentacle.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            }
            sceneryProps.Add(group);
        }

        private void AddTreasureChest(float x, float z)
        {
            var group = new GameObject("Treasure Chest");
            group.transform.position = new Vector3(x, 0f, z);
            AddChildPrimitive(group.transform, "Chest Base", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0f), new Vector3(0.55f, 0.36f, 0.4f), new Color(0.45f, 0.28f, 0.16f));
            AddChildPrimitive(group.transform, "Chest Lid", PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.15f), new Vector3(0.55f, 0.18f, 0.18f), new Color(0.55f, 0.35f, 0.2f));
            AddChildPrimitive(group.transform, "Chest Lock", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0.22f), new Vector3(0.1f, 0.12f, 0.05f), new Color(1f, 0.85f, 0.3f));
            AddChildPrimitive(group.transform, "Chest Glow", PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0f), Vector3.one * 0.3f, new Color(1f, 0.85f, 0.3f, 0.55f), true);
            sceneryProps.Add(group);
        }

        private void AddSkyArch(float x, float z, int index)
        {
            var group = new GameObject("Sky Arch Set");
            group.transform.position = new Vector3(x, 0f, z);
            // 浮空云石平台
            AddChildPrimitive(group.transform, "Floating Stone", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f), new Vector3(1.4f, 0.45f, 1.2f), new Color(0.65f, 0.7f, 0.8f));
            AddChildPrimitive(group.transform, "Stone Cloud Halo", PrimitiveType.Sphere, new Vector3(0f, 1.3f, 0f), new Vector3(1.2f, 0.35f, 1f), new Color(1f, 1f, 1f, 0.28f), true);
            // 拱门
            for (var i = 0; i < 5; i++)
            {
                var ang = (i / 4f) * Mathf.PI;
                var arch = AddChildPrimitive(group.transform, $"Arch {i}", PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(ang) * 0.85f, 1.85f + Mathf.Cos(ang) * 0.85f, 0f),
                    new Vector3(0.18f, 0.4f, 0.18f),
                    new Color(0.95f, 0.78f, 0.45f));
                arch.transform.localRotation = Quaternion.Euler(0f, 0f, -ang * Mathf.Rad2Deg);
            }
            // 顶部光球
            AddChildPrimitive(group.transform, "Arch Beacon", PrimitiveType.Sphere, new Vector3(0f, 2.7f, 0f), Vector3.one * 0.32f, new Color(1f, 0.95f, 0.55f, 0.85f), true);
            RegisterSceneryProp(group, x, z);
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
            var group = new GameObject("Cedar Ruin Set");
            group.transform.position = new Vector3(x, 0f, z);
            var side = x < 0f ? -1f : 1f;
            AddChildPrimitive(group.transform, "Broken Cedar Post", PrimitiveType.Cube, new Vector3(0f, 0.82f, 0f), new Vector3(0.34f, 1.64f, 0.34f), new Color(0.36f, 0.24f, 0.16f));
            var beam = AddChildPrimitive(group.transform, "Fallen Cedar Beam", PrimitiveType.Cube, new Vector3(-side * 0.42f, 0.42f, 0.48f), new Vector3(1.55f, 0.22f, 0.24f), new Color(0.48f, 0.34f, 0.22f));
            beam.transform.localRotation = Quaternion.Euler(0f, side * 16f, side * (index % 2 == 0 ? -12f : 8f));
            AddChildPrimitive(group.transform, "Snow On Beam", PrimitiveType.Cube, new Vector3(-side * 0.42f, 0.58f, 0.48f), new Vector3(1.38f, 0.08f, 0.2f), new Color(0.82f, 0.9f, 0.9f));
            sceneryProps.Add(group);
        }

        private void AddAuroraBeacon(float x, float z, int index)
        {
            var group = new GameObject("Aurora Beacon Set");
            group.transform.position = new Vector3(x, 0f, z);
            var core = AddChildPrimitive(group.transform, "Beacon Crystal", PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(0.42f, 1.8f, 0.42f), new Color(0.46f, 0.92f, 1f, 0.7f), true);
            core.transform.localRotation = Quaternion.Euler(0f, 28f + index * 11f, 0f);
            AddChildPrimitive(group.transform, "Beacon Halo", PrimitiveType.Cylinder, new Vector3(0f, 1.15f, 0f), new Vector3(1.8f, 0.04f, 1.8f), new Color(0.46f, 0.28f, 1f, 0.18f), true);
            AddChildPrimitive(group.transform, "Beacon Foot Glow", PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0f), new Vector3(1.6f, 0.34f, 1.6f), new Color(0.18f, 1f, 0.78f, 0.14f), true);
            sceneryProps.Add(group);
        }

        private void AddMistVeil(float x, float z, int index)
        {
            var group = new GameObject("Mist Veil Set");
            group.transform.position = new Vector3(x, 0f, z);
            for (var i = 0; i < 3; i++)
            {
                var veil = AddChildPrimitive(
                    group.transform,
                    "Low Fog Veil",
                    PrimitiveType.Cube,
                    new Vector3((i - 1) * 0.52f, 0.55f + i * 0.08f, i * 0.34f),
                    new Vector3(1.2f, 0.42f, 0.08f),
                    new Color(0.72f, 0.84f, 0.9f, 0.16f),
                    true);
                veil.transform.localRotation = Quaternion.Euler(0f, index * 9f + i * 14f, 0f);
            }

            AddChildPrimitive(group.transform, "Dike Marker", PrimitiveType.Cylinder, new Vector3(0f, 0.48f, -0.52f), new Vector3(0.16f, 0.48f, 0.16f), new Color(0.28f, 0.36f, 0.42f));
            sceneryProps.Add(group);
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
            sceneryProps.Add(group);
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

        private void AddOpenNatureProp(string modelName, float x, float z, Vector3 scale, Color tint)
        {
            var prefab = LoadModelPrefab(modelName);
            if (prefab == null)
            {
                return;
            }

            var go = Object.Instantiate(prefab);
            go.name = $"OpenModel_{modelName}";
            go.transform.position = new Vector3(x, 0f, z);
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                var mat = renderer.material;
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor"))
                {
                    var c = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", Color.Lerp(c, tint, 0.35f));
                }
                if (mat.HasProperty("_Color"))
                {
                    var c = mat.GetColor("_Color");
                    mat.SetColor("_Color", Color.Lerp(c, tint, 0.35f));
                }
            }

            sceneryProps.Add(go);
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
            var group = new GameObject("Snow Bank");
            group.transform.position = new Vector3(x, 0f, z);
            var baseSize = Random.Range(0.8f, 1.7f);
            for (var i = 0; i < 3; i++)
            {
                var snow = RunnerVisuals.CreatePrimitive(
                    "Snow Lump",
                    PrimitiveType.Sphere,
                    group.transform.position + new Vector3((i - 1) * baseSize * 0.42f, 0.08f + i * 0.03f, Random.Range(-0.25f, 0.25f)),
                    new Vector3(baseSize, 0.34f + i * 0.05f, baseSize * 0.8f),
                    new Color(0.86f, 0.96f, 1f));
                snow.transform.SetParent(group.transform, true);
            }

            var sparkle = AddChildPrimitive(group.transform, "Snow Sparkle", PrimitiveType.Cube, new Vector3(0.15f, 0.38f, -0.18f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.84f, 1f, 1f, 0.62f), true);
            sparkle.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            sceneryProps.Add(group);
        }

        private void AddMountainGroup(float x, float z, float height)
        {
            var group = new GameObject("Distant Ice Ridge");
            group.transform.position = new Vector3(x, 0f, z);
            var ridge = RunnerVisuals.CreatePrimitive("Blue Ridge", PrimitiveType.Cube, group.transform.position + new Vector3(0f, height * 0.42f, 0f), new Vector3(height * 1.45f, height, height * 0.8f), new Color(0.18f, 0.42f, 0.68f));
            ridge.transform.rotation = Quaternion.Euler(0f, 0f, x < 0f ? -18f : 18f);
            ridge.transform.SetParent(group.transform, true);
            var cap = RunnerVisuals.CreatePrimitive("Ridge Snow Cap", PrimitiveType.Cube, group.transform.position + new Vector3(0f, height * 0.84f, -0.06f), new Vector3(height * 0.76f, height * 0.2f, height * 0.66f), new Color(0.84f, 0.96f, 1f));
            cap.transform.rotation = ridge.transform.rotation;
            cap.transform.SetParent(group.transform, true);
            var shadow = RunnerVisuals.CreatePrimitive("Ridge Cool Shadow", PrimitiveType.Cube, group.transform.position + new Vector3(x < 0f ? 0.46f : -0.46f, height * 0.4f, -0.2f), new Vector3(height * 0.42f, height * 0.72f, height * 0.64f), new Color(0.1f, 0.25f, 0.46f, 0.35f), true);
            shadow.transform.rotation = ridge.transform.rotation;
            shadow.transform.SetParent(group.transform, true);
            RegisterSceneryProp(group, x, z);
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

            for (var i = 0; i < sceneryProps.Count; i++)
            {
                var prop = sceneryProps[i];
                if (prop.name.StartsWith("Aurora Ribbon", System.StringComparison.Ordinal))
                {
                    prop.transform.localScale = Vector3.Lerp(prop.transform.localScale, new Vector3(0.95f + Mathf.Sin(Time.time + i) * 0.22f, 8.5f, 0.08f), Mathf.Clamp01(dt * 2f));
                    continue;
                }

                var oldZ = prop.transform.position.z;
                prop.transform.Translate(Vector3.back * (effectiveSpeed * dt * 0.72f), Space.World);
                if (prop.name.StartsWith("Checkpoint Gate", System.StringComparison.Ordinal) && oldZ >= 0f && prop.transform.position.z < 0f)
                {
                    runnerAudio.PlayCheckpoint();
                }

                if (prop.transform.position.z < propRecycleZ)
                {
                    prop.transform.position += Vector3.forward * 132f;
                }

                EnsureSceneryLateralOffset(prop, d);
                AlignSceneryToTrack(prop, d, gy);

                if (!ShouldSkipViewCorridorFade(prop))
                {
                    ApplyViewCorridorFade(prop, playerPos, camPos, d, dt);
                }
            }
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
            sceneryProps.Add(prop);
            sceneryPropRenderers[prop] = prop.GetComponentsInChildren<Renderer>(false);
        }

        private void EnsureSceneryLateralOffset(GameObject prop, float runDistance)
        {
            if (prop.GetComponent<SceneryLateralOffset>() != null) return;
            if (ShouldSkipViewCorridorFade(prop)) return;

            var pos = prop.transform.position;
            var lateral = prop.AddComponent<SceneryLateralOffset>();
            lateral.LateralOffset = pos.x - TrackTerrain.WorldCurveX(pos.z, runDistance);
            lateral.SnapToGround = IsGroundSceneryName(prop.name);
        }

        private static bool ShouldForceHideSceneryInCorridor(string name)
        {
            return name.Contains("Rainbow")
                || name.Contains("Distant Ice Ridge")
                || name.Contains("Sunlight Beam")
                || name.Contains("Floating Cloud")
                || name.Contains("Warm Track Lamp")
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

        private static void AlignSceneryToTrack(GameObject prop, float runDistance, float groundY)
        {
            var lateral = prop.GetComponent<SceneryLateralOffset>();
            if (lateral == null) return;

            var pos = prop.transform.position;
            pos.x = TrackTerrain.WorldCurveX(pos.z, runDistance) + lateral.LateralOffset;
            if (lateral.SnapToGround)
            {
                pos.y = TrackTerrain.SurfaceY(pos.z, runDistance, groundY);
            }

            prop.transform.position = pos;
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

        private static bool ShouldSkipViewCorridorFade(GameObject prop)
        {
            if (prop == null) return true;
            var n = prop.name;
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

        private void ApplyViewCorridorFade(GameObject prop, Vector3 playerPos, Vector3 camPos, float runDistance, float dt)
        {
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
            var forceHide = inCorridor && ShouldForceHideSceneryInCorridor(prop.name);

            foreach (var renderer in renderers)
            {
                var id = renderer.GetInstanceID();
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
                var currentAlpha = RunnerVisuals.GetColor(renderer.sharedMaterial).a;
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
            foreach (var guide in laneGuides)
            {
                var p = guide.transform.position;
                p.y = TrackTerrain.SurfaceY(p.z, d, gy) + 0.02f;
                guide.transform.position = p;
                var rend = guide.GetComponent<Renderer>();
                if (rend == null) continue;
                var mat = rend.sharedMaterial;
                var active = Mathf.Abs(guide.transform.position.x - activeX) < 0.1f;
                RunnerVisuals.SetColor(
                    mat,
                    Color.Lerp(
                        RunnerVisuals.GetColor(mat),
                        active ? new Color(0.57f, 1f, 0.86f, 0.92f) : new Color(0.82f, 0.93f, 1f, 0.35f),
                        Mathf.Clamp01(dt * 8f)));
            }
        }

        private static bool IsReturnButtonTouch()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return false;
            }

            var s = RunnerHudLayoutScale;
            var edgeH = 260f * s;
            var edgeV = 180f * s;

            var pointer = Input.mousePosition;
            var yFromTop = Screen.height - pointer.y;
            var nearHorizontalEdge = pointer.x <= edgeH || pointer.x >= Screen.width - edgeH;
            var nearVerticalEdge =
                pointer.y <= edgeV ||
                pointer.y >= Screen.height - edgeV ||
                yFromTop <= edgeV ||
                yFromTop >= Screen.height - edgeV;
            return nearHorizontalEdge && nearVerticalEdge;
        }

        private static void ConfigureRuntimePerformance(bool active)
        {
            QualitySettings.vSyncCount = active ? 1 : 0;
            Application.targetFrameRate = active ? 60 : 15;
            Screen.sleepTimeout = active ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            Input.gyro.enabled = false;
        }

        private void OnGUI()
        {
            if (game == null || hud == null)
                return;

            // ── 倒计时覆盖层 ──────────────────────────────────────────
            DrawEntranceOverlay();

            // ── 死亡序列覆盖层 ────────────────────────────────────────
            DrawDeathOverlay();

            var prevMatrix = GUI.matrix;
            try
            {
                var layout = ApplyRunnerHudGuiMatrix();
                var world = game.World;
                if (hud.Draw(layout, config, world, game.Running, game.GameOver, world.Score, game.FeedbackText, game.FeedbackTimer,
                        currentTheme,
                        game.Boss, game.IsPaused))
                {
                    game.FinishRun();
                }
            }
            finally
            {
                GUI.matrix = prevMatrix;
            }

            DrawThemeTransitionOverlay();
            DrawScreenFlashOverlay();
        }

        private void DrawEntranceOverlay()
        {
            if (entranceComplete || entranceState == EntranceState.None) return;

            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.4f;

            if (entranceState == EntranceState.Countdown)
            {
                // 3-2-1 大字动画
                var step = Mathf.FloorToInt(entranceTimer / 0.7f);
                var tInStep = (entranceTimer - step * 0.7f) / 0.7f; // 0..1
                var scale = 1f + (1f - tInStep) * 0.5f; // 开始大，逐渐缩小
                var alpha = 1f - tInStep * 0.6f; // 开始亮，逐渐淡

                var numStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(120f * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 1f, 1f, alpha) },
                };
                GUI.Label(new Rect(cx - 100f, cy - 80f, 200f, 160f), countdownNumber.ToString(), numStyle);
            }
            else if (entranceState == EntranceState.GoText)
            {
                var t = entranceTimer / 0.6f;
                var scale = 1f + t * 0.3f;
                var alpha = 1f - t;

                var goStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(80f * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.34f, 1f, 0.82f, alpha) },
                };
                GUI.Label(new Rect(cx - 200f, cy - 60f, 400f, 120f), "出发！", goStyle);
            }
        }

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
            if (themeTransitionActive || game == null || !game.Running) return;
            if (game.Boss != null && game.Boss.BossActive) return;
            if (game.World.RunTime < nextThemeSwitchTime) return;

            pendingTheme = GetNextTheme(currentTheme);
            if (pendingTheme == currentTheme)
            {
                nextThemeSwitchTime += ThemeSwitchIntervalSeconds;
                return;
            }

            themeTransitionActive = true;
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
                themeTransitionActive = false;
                themeAppliedInTransition = false;
                themeTransitionTimer = 0f;
            }
        }

        private void ApplyRuntimeThemeChange(RunnerMapTheme nextTheme)
        {
            currentTheme = nextTheme;
            config.MapTheme = nextTheme;
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
            if (!themeTransitionActive) return;

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

        private static string GetThemeDisplayName(RunnerMapTheme theme)
        {
            return theme switch
            {
                RunnerMapTheme.CedarRuins => "雪松废墟",
                RunnerMapTheme.AuroraField => "极光磁场",
                RunnerMapTheme.MistDike => "雾堤",
                RunnerMapTheme.OceanReef => "海洋珊瑚礁",
                RunnerMapTheme.SkyFlight => "天空飞翔",
                _ => "冰湖回音谷",
            };
        }
    }
}
