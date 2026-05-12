using System.Collections.Generic;
using PenguinRun.Game;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Boss attack patterns and the matching dodge/counter windows.
    /// </summary>
    internal enum BossPattern
    {
        None,
        SweepLow,
        DiveHigh,
        ChargeAcross,
        RangedSalvo,
        CenterBeam,
        QuakePulse,
        Vulnerable,
    }

    /// <summary>
    /// Boss encounter phases.
    /// </summary>
    internal enum BossPhase
    {
        Spawning,
        Active,
        Vulnerable,
        Defeated,
        Retreating,
    }

    /// <summary>
    /// Runs timed boss encounters during the endless runner.
    /// </summary>
    internal sealed class BossSystem
    {
        // 战斗参数（固定）
        public const int BossMaxHits = 3;
        public const float BossSpawnAheadZ = 30f;
        public const float BossActiveZ = 14f;
        public const float VulnerableSeconds = 1.6f;
        public const float HitInvulnerabilitySeconds = 0.6f;
        public const float DefeatRetreatSeconds = 1.4f;

        // 难度相关（运行时从 DifficultyPreset 注入）
        private float firstBossDistance;
        private float bossInterval;
        private float bossPatternMinSeconds;
        private float bossPatternMaxSeconds;
        private float bossDangerWindowRatio;
        private float bossTelegraphLifetimeRatio;
        private float bossVulnerableChance;

        // 奖励
        public const int BossDefeatBonusFishSnacks = 25;
        public const int BossDefeatBonusScore = 800;
        public const int BossPhaseDamageScore = 150;

        // 状态
        public BossEncounter CurrentBoss { get; private set; }
        public bool BossActive => CurrentBoss != null && CurrentBoss.Phase != BossPhase.Defeated;
        public int BossesDefeated { get; private set; }
        private float nextBossDistance;
        private readonly HashSet<BossPattern> explainedPatterns = new();
        private bool explainedCounterWindow;
        private bool guidanceEnabledForCurrentBoss;
        private BossPattern lastKnownPattern = BossPattern.None;
        private BossPhase lastKnownPhase = BossPhase.Spawning;
        private string difficultyHintTag = "普通";
        private bool guidancePauseRequest;
        private float currentBossElapsed;
        private float speedRewardWindowSeconds = 13f;
        private int speedRewardMaxFish = 14;
        private int speedRewardMaxScore = 420;

        // Boss 前奏 / 奖励段控制
        private bool preludeSpawned = false;
        private BossDefinition currentBossDef;
        private BossDefinition lastDefeatedBoss;
        private BossDefinition nextBossDef; // 预计算的下一个 Boss

        // 反馈（音频等）
        private readonly RunnerAudio audio;
        private readonly RunnerSessionConfig config;
        private readonly Transform sceneRoot;
        private readonly float baseGroundY;
        private readonly float laneWidth;

        public BossSystem(RunnerAudio audio, RunnerSessionConfig config, Transform sceneRoot, float baseGroundY, float laneWidth)
        {
            this.audio = audio;
            this.config = config;
            this.sceneRoot = sceneRoot;
            this.baseGroundY = baseGroundY;
            this.laneWidth = laneWidth;

            var preset = config.DifficultyPreset;
            ApplyDifficultyBossTuning(config.Difficulty, preset, config.Daily);
            nextBossDistance = firstBossDistance;
        }

        public void Reset()
        {
            CleanupBoss();
            BossesDefeated = 0;
            nextBossDistance = firstBossDistance;
            explainedPatterns.Clear();
            explainedCounterWindow = false;
            guidanceEnabledForCurrentBoss = true;
            lastKnownPattern = BossPattern.None;
            lastKnownPhase = BossPhase.Spawning;
            guidancePauseRequest = false;
            currentBossElapsed = 0f;
        }

        public void CleanupBoss()
        {
            if (CurrentBoss != null)
            {
                CurrentBoss.Dispose();
                CurrentBoss = null;
                audio.ExitBossMusic();
            }
        }

        /// <summary>
        /// 每帧调用：根据跑动距离触发 Boss、推进战斗、处理逻辑。
        /// 返回值：玩家本帧是否应被 Boss 攻击命中（由外部按受伤逻辑处理）。
        /// </summary>
        public bool Tick(float dt, float runDistance, float playerX, float playerY, float groundY,
            bool playerSliding, float playerDashTimer, float playerShieldTimer,
            ref string feedbackText, ref float feedbackTimer)
        {
            // 触发 Boss
            if (CurrentBoss == null && runDistance >= nextBossDistance)
            {
                SpawnBoss(runDistance);
                if (guidanceEnabledForCurrentBoss)
                {
                    feedbackText = $"⚠ BOSS 来袭：{CurrentBoss.Definition.DisplayName}【{difficultyHintTag}】\n{GetDashHowToHint()}；破绽圈变绿时冲刺穿身反击";
                    feedbackTimer = 1.9f;
                }
                else
                {
                    feedbackText = $"⚠ BOSS 再临：{CurrentBoss.Definition.DisplayName}";
                    feedbackTimer = 1.1f;
                }
                audio.PlayBossLand();
            }

            if (CurrentBoss == null) return false;

            currentBossElapsed += dt;
            CurrentBoss.Tick(dt, runDistance, baseGroundY);
            UpdateBossGuidance(ref feedbackText, ref feedbackTimer);

            // 战斗逻辑
            return ProcessCombat(dt, runDistance, playerX, playerY, groundY,
                playerSliding, playerDashTimer, playerShieldTimer,
                ref feedbackText, ref feedbackTimer);
        }

        /// <summary>
        /// Boss 战进行中时，用该属性决定是否暂停常规障碍生成。
        /// </summary>
        public bool ShouldPauseSpawning => config.Daily || BossActive;

        private void SpawnBoss(float runDistance)
        {
            var ctx = new BossDefinitions.BossSpawnContext
            {
                MapTheme = config.MapTheme,
                Difficulty = config.Difficulty,
                IsDaily = config.Daily,
                RunDistance = runDistance,
                BossesDefeated = BossesDefeated,
                DefeatedBossIds = PlayerSave.GetDefeatedBossIds() ?? new HashSet<string>(),
            };

            var def = BossDefinitions.PickFor(ctx);
            currentBossDef = def;
            nextBossDef = null; // 当前 Boss 已生成，清空预计算
            preludeSpawned = false; // 重置前奏标记

            CurrentBoss = new BossEncounter(def, baseGroundY, laneWidth, runDistance,
                bossPatternMinSeconds, bossPatternMaxSeconds,
                bossDangerWindowRatio, bossTelegraphLifetimeRatio, bossVulnerableChance);
            CurrentBoss.Root.transform.SetParent(sceneRoot, false);
            audio.EnterBossMusic(def);
            explainedPatterns.Clear();
            explainedCounterWindow = false;
            guidanceEnabledForCurrentBoss = !PlayerSave.HasDefeatedBoss(def.BossId);
            lastKnownPattern = BossPattern.None;
            lastKnownPhase = BossPhase.Spawning;
            guidancePauseRequest = false;
            currentBossElapsed = 0f;
            ResetFightStats(); // 重置本战统计

            // 预计算下一场可能出场的 Boss
            PrecomputeNextBoss(ctx);
        }

        /// <summary>预计算下一个可能出现的 Boss。</summary>
        private void PrecomputeNextBoss(BossDefinitions.BossSpawnContext ctx)
        {
            ctx.BossesDefeated = BossesDefeated + 1; // 假设当前 Boss 将被击败
            nextBossDef = BossDefinitions.PickFor(ctx);
        }

        private bool ProcessCombat(float dt, float runDistance, float playerX, float playerY, float groundY,
            bool playerSliding, float playerDashTimer, float playerShieldTimer,
            ref string feedbackText, ref float feedbackTimer)
        {
            var boss = CurrentBoss;
            if (boss.Phase == BossPhase.Defeated || boss.Phase == BossPhase.Retreating)
            {
                if (boss.Phase == BossPhase.Retreating && boss.RetreatTimer <= 0f)
                {
                    CleanupBoss();
                    nextBossDistance = runDistance + bossInterval;
                }
                return false;
            }

            // 破绽期 + 冲刺穿过 = 命中
            if (boss.Phase == BossPhase.Vulnerable && playerDashTimer > 0f)
            {
                if (CanCounterBossInVulnerable(boss, playerX, laneWidth * 1.2f))
                {
                    DamageBoss(boss, ref feedbackText, ref feedbackTimer);
                    return false;
                }
            }

            // 护盾冲撞：破绽期间贴近 Boss 也可造成伤害
            if (boss.Phase == BossPhase.Vulnerable && playerShieldTimer > 0f)
            {
                if (CanCounterBossInVulnerable(boss, playerX, laneWidth * 1.5f))
                {
                    DamageBoss(boss, ref feedbackText, ref feedbackTimer);
                    return false;
                }
            }

            // 检测 Boss 攻击是否命中玩家
            if (boss.HitFlashTimer > 0f) return false;
            if (boss.Phase != BossPhase.Active) return false;
            if (boss.Pattern == BossPattern.None) return false;

            return CheckPatternHit(boss, playerX, playerY, groundY, playerSliding,
                playerDashTimer, playerShieldTimer, ref feedbackText, ref feedbackTimer);
        }

        private bool CheckPatternHit(BossEncounter boss, float playerX, float playerY, float groundY,
            bool playerSliding, float playerDashTimer, float playerShieldTimer,
            ref string feedbackText, ref float feedbackTimer)
        {
            if (!boss.IsPatternDangerous) return false;

            switch (boss.Pattern)
            {
                case BossPattern.SweepLow:
                    // 低扫：滑铲 / 待在安全车道可回避
                    if (playerSliding) return false;
                    if (Mathf.Abs(playerX - boss.SafeLaneX) < laneWidth * 0.5f) return false;
                    break;

                case BossPattern.DiveHigh:
                    // 俯冲：起跳 / 不在 Boss 所在车道可回避
                    var inAir = playerY > groundY + 0.6f;
                    if (inAir) return false;
                    if (Mathf.Abs(playerX - boss.WorldX) > laneWidth * 0.55f) return false;
                    break;

                case BossPattern.ChargeAcross:
                    if (playerY > groundY + 0.6f) return false;
                    break;

                case BossPattern.RangedSalvo:
                    if (playerDashTimer > 0f) return false;
                    if (!IsInSalvoLane(boss, playerX, laneWidth * 0.42f)) return false;
                    break;

                case BossPattern.CenterBeam:
                    // 中央光束：中央车道危险；起跳也可规避
                    if (playerY > groundY + 0.6f) return false;
                    if (Mathf.Abs(playerX) > laneWidth * 0.45f) return false;
                    break;

                case BossPattern.QuakePulse:
                    // 震地脉冲：需滑铲躲过地面冲击
                    if (playerSliding) return false;
                    break;
            }

            // 命中：有护盾则抵消，否则扣命
            if (playerShieldTimer > 0f)
            {
                feedbackText = "护盾抵挡 BOSS 攻击！等破绽出现后再冲刺反击";
                feedbackTimer = 1f;
                boss.HitFlashTimer = 0.4f;
                return false;
            }

            // 记录被何种招式击中，用于死亡复盘
            RecordPlayerHit(boss.Pattern);

            feedbackText = guidanceEnabledForCurrentBoss
                ? $"{boss.Definition.DisplayName} 击中了你！{GetPatternDodgeHint(boss.Pattern)}"
                : $"{boss.Definition.DisplayName} 击中了你！";
            feedbackTimer = 1f;
            boss.HitFlashTimer = HitInvulnerabilitySeconds;
            audio.PlayPlayerHurt();
            return true;
        }

        private static bool IsInSalvoLane(BossEncounter boss, float playerX, float laneTolerance)
        {
            foreach (var px in boss.SalvoLaneX)
            {
                if (Mathf.Abs(playerX - px) < laneTolerance) return true;
            }
            return false;
        }

        private static bool CanCounterBossInVulnerable(BossEncounter boss, float playerX, float laneTolerance)
        {
            var bossInEngageBand = boss.WorldZ >= BossActiveZ - 5.5f && boss.WorldZ <= BossActiveZ + 3f;
            var laneAligned = Mathf.Abs(playerX - boss.WorldX) < laneTolerance;
            return bossInEngageBand && laneAligned;
        }

        private void DamageBoss(BossEncounter boss, ref string feedbackText, ref float feedbackTimer)
        {
            boss.HitsRemaining = Mathf.Max(0, boss.HitsRemaining - 1);
            boss.EnterAfterHit();
            audio.PlayBossShieldBreak();

            if (boss.HitsRemaining <= 0)
            {
                boss.RetreatTimer = DefeatRetreatSeconds;
                boss.Phase = BossPhase.Retreating;
                BossesDefeated += 1;
                PlayerSave.MarkBossDefeated(boss.Definition.BossId);
                lastDefeatedBoss = boss.Definition; // 记录本场击败的 Boss
                ComputePendingDefeatReward(currentBossElapsed, out var fishReward, out var scoreReward,
                    out var speedTier, out var speedFish, out var speedScore);
                feedbackText = speedScore > 0 || speedFish > 0
                    ? $"★ 已击败 {boss.Definition.DisplayName}！速杀 {speedTier} 档 +{speedFish} 鱼干 +{speedScore} 分"
                    : $"★ 已击败 {boss.Definition.DisplayName}！+{fishReward} 鱼干";
                feedbackTimer = 2.2f;
                audio.PlayBossDefeat();
                MarkDefeatRewardPending(fishReward, scoreReward, speedTier, speedFish, speedScore);
            }
            else
            {
                feedbackText = $"BOSS 受伤！剩余血量 {boss.HitsRemaining}/{BossMaxHits}";
                feedbackTimer = 1.2f;
            }
        }

        /// <summary>
        /// 当外部检测到 Boss 战已结束（Boss 完全退场）时，调用该方法领取击败奖励。
        /// </summary>
        public bool TryConsumeDefeatReward(out int fishBonus, out int scoreBonus, out string speedTier, out int speedFishBonus, out int speedScoreBonus)
        {
            if (defeatRewardPending)
            {
                defeatRewardPending = false;
                fishBonus = pendingFishBonus;
                scoreBonus = pendingScoreBonus;
                speedTier = pendingSpeedTier;
                speedFishBonus = pendingSpeedFishBonus;
                speedScoreBonus = pendingSpeedScoreBonus;
                pendingFishBonus = 0;
                pendingScoreBonus = 0;
                pendingSpeedTier = "C";
                pendingSpeedFishBonus = 0;
                pendingSpeedScoreBonus = 0;
                return true;
            }
            fishBonus = 0;
            scoreBonus = 0;
            speedTier = "C";
            speedFishBonus = 0;
            speedScoreBonus = 0;
            return false;
        }

        private bool defeatRewardPending;
        private int pendingFishBonus;
        private int pendingScoreBonus;
        private string pendingSpeedTier = "C";
        private int pendingSpeedFishBonus;
        private int pendingSpeedScoreBonus;

        /// <summary>当 Boss 在 ProcessCombat 内被击杀时，标记奖励待领取。</summary>
        public void MarkDefeatRewardPending(int fishReward, int scoreReward, string speedTier, int speedFishBonus, int speedScoreBonus)
        {
            pendingFishBonus = Mathf.Max(0, fishReward);
            pendingScoreBonus = Mathf.Max(0, scoreReward);
            pendingSpeedTier = string.IsNullOrEmpty(speedTier) ? "C" : speedTier;
            pendingSpeedFishBonus = Mathf.Max(0, speedFishBonus);
            pendingSpeedScoreBonus = Mathf.Max(0, speedScoreBonus);
            defeatRewardPending = true;
        }

        public bool TryConsumeGuidancePauseRequest()
        {
            if (guidancePauseRequest)
            {
                guidancePauseRequest = false;
                return true;
            }
            return false;
        }

        // Boss 前奏 / 奖励段对外 API

        /// <summary>上一场被击败的 Boss（用于生成奖励段）。</summary>
        public BossDefinition LastDefeatedBoss => lastDefeatedBoss;

        /// <summary>下一场即将出现的 Boss（用于生成前奏段）。</summary>
        public BossDefinition NextBossDefinition => nextBossDef;

        /// <summary>是否已生成前奏段。</summary>
        public bool HasSpawnedPrelude => preludeSpawned;

        /// <summary>标记前奏段已生成。</summary>
        public void MarkPreludeSpawned() => preludeSpawned = true;

        /// <summary>距下一场 Boss 是否在指定距离内。</summary>
        public bool IsBossApproaching(float runDistance, float withinMeters)
        {
            if (CurrentBoss != null) return false;
            var distToBoss = nextBossDistance - runDistance;
            return distToBoss > 0 && distToBoss <= withinMeters;
        }

        public bool GuidanceEnabledForCurrentBoss => guidanceEnabledForCurrentBoss;
        public int CurrentBossRound => CurrentBoss == null ? 0 : BossesDefeated + 1;
        public bool IsBossRushMode => config.Daily;
        public float CurrentBossSpeedRewardTimeLeft => Mathf.Max(0f, speedRewardWindowSeconds - currentBossElapsed);
        public float CurrentBossSpeedRewardWindow => speedRewardWindowSeconds;
        public int CurrentBossMaxSpeedFishReward => speedRewardMaxFish;
        public int CurrentBossMaxSpeedScoreReward => speedRewardMaxScore;
        public bool CurrentBossInAttackInterval => CurrentBoss != null && CurrentBoss.IsInAttackInterval;
        public float CurrentBossAttackIntervalLeft => CurrentBoss == null ? 0f : CurrentBoss.AttackIntervalTimeLeft;
        public float CurrentBossAttackIntervalDuration => CurrentBoss == null ? 0.01f : CurrentBoss.AttackIntervalDuration;

        /// <summary>当前 Boss 的攻击-破绽循环轮次（供 HUD 显示节奏）。</summary>
        public int CurrentBossPhaseCycle => CurrentBoss?.CurrentPhaseCycle ?? 0;

        /// <summary>距离下次破绽还有几招。</summary>
        public int CurrentBossPatternsUntilVulnerable => CurrentBoss?.PatternsUntilVulnerable ?? 0;

        /// <summary>当前阶段已持续的时间。</summary>
        public float CurrentBossPhaseElapsed => CurrentBoss?.ElapsedInCurrentPhase ?? 0f;

        public string CurrentBossPatternLabel
        {
            get
            {
                if (CurrentBoss == null) return string.Empty;
                if (CurrentBoss.Phase == BossPhase.Vulnerable) return "破绽暴露";
                if (CurrentBoss.Phase != BossPhase.Active) return string.Empty;
                return GetPatternDisplayName(CurrentBoss.Pattern);
            }
        }

        public string CurrentBossDodgeHint
        {
            get
            {
                if (!guidanceEnabledForCurrentBoss) return string.Empty;
                if (CurrentBoss == null || CurrentBoss.Phase != BossPhase.Active) return string.Empty;
                return GetPatternDodgeHint(CurrentBoss.Pattern);
            }
        }

        public string CurrentBossCounterHint
        {
            get
            {
                if (!guidanceEnabledForCurrentBoss) return string.Empty;
                if (CurrentBoss == null) return string.Empty;
                if (CurrentBoss.Phase == BossPhase.Vulnerable)
                {
                    return $"破绽窗口：立刻冲刺贯穿 BOSS。{GetDashHowToHint()}；也可用护盾贴身撞击。";
                }
                return "先躲连招，等绿色破绽圈出现再反击";
            }
        }

        private void ApplyDifficultyBossTuning(DifficultyKind kind, DifficultyPreset preset, bool bossRushMode)
        {
            var cadenceMultiplier = 1f;
            var intervalMultiplier = 1f;
            bossDangerWindowRatio = 0.48f;
            bossTelegraphLifetimeRatio = 0.75f;
            bossVulnerableChance = 0.5f;

            switch (kind)
            {
                case DifficultyKind.Easy:
                    cadenceMultiplier = 1.2f;
                    intervalMultiplier = 1.3f;
                    bossDangerWindowRatio = 0.35f;
                    bossTelegraphLifetimeRatio = 0.88f;
                    bossVulnerableChance = 0.72f;
                    difficultyHintTag = "轻松";
                    break;
                case DifficultyKind.Hard:
                    cadenceMultiplier = 0.92f;
                    intervalMultiplier = 1.28f;
                    bossDangerWindowRatio = 0.52f;   // 标准预警
                    bossTelegraphLifetimeRatio = 0.65f;
                    bossVulnerableChance = 0.42f;    // 破绽偏少
                    difficultyHintTag = "困难";
                    break;
                case DifficultyKind.Expert:
                    cadenceMultiplier = 0.85f;
                    intervalMultiplier = 1.35f;
                    bossDangerWindowRatio = 0.58f;   // 更短预警
                    bossTelegraphLifetimeRatio = 0.55f;
                    bossVulnerableChance = 0.35f;
                    difficultyHintTag = "专家";
                    break;
                default:
                    cadenceMultiplier = 1f;
                    intervalMultiplier = 1f;
                    bossDangerWindowRatio = 0.45f;
                    bossTelegraphLifetimeRatio = 0.72f;
                    bossVulnerableChance = 0.55f;
                    difficultyHintTag = "普通";
                    break;
            }

            if (bossRushMode)
            {
                firstBossDistance = kind switch
                {
                    DifficultyKind.Easy => 155f,
                    DifficultyKind.Hard => 125f,
                    DifficultyKind.Expert => 95f,
                    _ => 115f,
                };
                bossInterval = kind switch
                {
                    DifficultyKind.Easy => 375f,
                    DifficultyKind.Hard => 360f,
                    DifficultyKind.Expert => 300f,
                    _ => 320f,
                };
                speedRewardWindowSeconds = kind switch
                {
                    DifficultyKind.Easy => 15f,
                    DifficultyKind.Hard => 11.5f,
                    DifficultyKind.Expert => 10f,
                    _ => 13f,
                };
            }
            else
            {
                firstBossDistance = preset.FirstBossDistance;
                bossInterval = Mathf.Max(120f, preset.BossInterval * intervalMultiplier);
                speedRewardWindowSeconds = kind switch
                {
                    DifficultyKind.Easy => 16f,
                    DifficultyKind.Hard => 13f,
                    DifficultyKind.Expert => 11.5f,
                    _ => 14.5f,
                };
            }

            speedRewardMaxFish = kind switch
            {
                DifficultyKind.Easy => 10,
                DifficultyKind.Hard => 16,
                DifficultyKind.Expert => 20,
                _ => 14,
            };
            speedRewardMaxScore = kind switch
            {
                DifficultyKind.Easy => 320,
                DifficultyKind.Hard => 520,
                DifficultyKind.Expert => 680,
                _ => 420,
            };

            bossPatternMinSeconds = Mathf.Max(1.3f, preset.BossPatternMinSeconds * cadenceMultiplier);
            bossPatternMaxSeconds = Mathf.Max(bossPatternMinSeconds + 0.4f, preset.BossPatternMaxSeconds * cadenceMultiplier);
        }

        private void ComputePendingDefeatReward(float elapsedSeconds, out int fishReward, out int scoreReward,
            out string speedTier, out int speedFishBonus, out int speedScoreBonus)
        {
            var ratio = elapsedSeconds / Mathf.Max(0.1f, speedRewardWindowSeconds);
            var tierFactor = 0.15f;
            speedTier = "C";
            if (ratio <= 0.36f)
            {
                speedTier = "S";
                tierFactor = 1f;
            }
            else if (ratio <= 0.6f)
            {
                speedTier = "A";
                tierFactor = 0.72f;
            }
            else if (ratio <= 0.84f)
            {
                speedTier = "B";
                tierFactor = 0.45f;
            }

            speedFishBonus = Mathf.RoundToInt(speedRewardMaxFish * tierFactor);
            speedScoreBonus = Mathf.RoundToInt(speedRewardMaxScore * tierFactor);
            fishReward = BossDefeatBonusFishSnacks + speedFishBonus;
            scoreReward = BossDefeatBonusScore + speedScoreBonus;
        }

        private void UpdateBossGuidance(ref string feedbackText, ref float feedbackTimer)
        {
            if (!guidanceEnabledForCurrentBoss) return;
            var boss = CurrentBoss;
            if (boss == null) return;

            var phaseChanged = boss.Phase != lastKnownPhase;
            var patternChanged = boss.Pattern != lastKnownPattern;
            if (!phaseChanged && !patternChanged) return;

            string nextTip = null;
            var forceShow = false;

            if (boss.Phase == BossPhase.Active && boss.Pattern != BossPattern.None)
            {
                var firstSeen = explainedPatterns.Add(boss.Pattern);
                nextTip = $"{GetPatternDisplayName(boss.Pattern)}：{GetPatternDodgeHint(boss.Pattern)}";
                forceShow = firstSeen;
            }
            else if (boss.Phase == BossPhase.Vulnerable)
            {
                nextTip = explainedCounterWindow
                    ? $"破绽持续中：冲刺反击。（{GetDashHowToHint()}）"
                    : $"绿色破绽圈出现：冲刺贯穿 BOSS 造成伤害。\n{GetDashHowToHint()}；也可用护盾贴身撞击。";
                forceShow = !explainedCounterWindow;
                explainedCounterWindow = true;
            }

            if (!string.IsNullOrEmpty(nextTip) && (forceShow || feedbackTimer <= 0.2f))
            {
                feedbackText = nextTip;
                feedbackTimer = forceShow ? 1.8f : 1.1f;
                if (forceShow)
                {
                    guidancePauseRequest = true;
                }
            }

            lastKnownPhase = boss.Phase;
            lastKnownPattern = boss.Pattern;
        }

        private static string GetPatternDisplayName(BossPattern pattern)
        {
            return pattern switch
            {
                BossPattern.SweepLow => "低空横扫",
                BossPattern.DiveHigh => "高空俯冲",
                BossPattern.ChargeAcross => "全场冲锋",
                BossPattern.RangedSalvo => "远程齐射",
                BossPattern.CenterBeam => "中央光束",
                BossPattern.QuakePulse => "震地脉冲",
                BossPattern.Vulnerable => "破绽",
                _ => "蓄力待发",
            };
        }

        private static string GetPatternDodgeHint(BossPattern pattern)
        {
            return pattern switch
            {
                BossPattern.SweepLow => "滑铲或换到预警圈标示的安全车道",
                BossPattern.DiveHigh => "提早换道，或在落地前起跳躲过",
                BossPattern.ChargeAcross => "三路同时预警时立刻起跳",
                BossPattern.RangedSalvo => "看准地面预警圈换位躲弹；冲刺可小段无敌穿过",
                BossPattern.CenterBeam => "离开正中车道，或起跳越过光束",
                BossPattern.QuakePulse => "黄圈出现立刻滑铲贴地躲过",
                _ => "保持移动，紧盯地面预警圈",
            };
        }

        private static string GetDashHowToHint()
        {
            return "双击屏幕冲刺；或拾取冲刺道具触发";
        }

        // 死亡复盘

        private BossPattern lastHitPattern = BossPattern.None;
        private int hitCountInCurrentFight = 0;

        /// <summary>获取死亡复盘提示：分析被哪一招命中并给出建议。</summary>
        public string GetDeathAnalysisHint()
        {
            if (lastHitPattern == BossPattern.None) return "被 BOSS 击败";

            var hint = lastHitPattern switch
            {
                BossPattern.SweepLow => "败于低空横扫：红圈出现时滑铲或换到安全车道",
                BossPattern.DiveHigh => "败于高空俯冲：BOSS 抬身时提早换道或起跳躲过",
                BossPattern.ChargeAcross => "败于全场冲锋：三路同时亮预警时立刻起跳",
                BossPattern.RangedSalvo => "败于远程齐射：看准落点圈换位；也可用冲刺穿过",
                BossPattern.CenterBeam => "败于中央光束：离开正中车道或起跳越过",
                BossPattern.QuakePulse => "败于震地脉冲：黄圈出现立刻滑铲，不要硬跳",
                _ => "被 BOSS 击败：观察预警圈，选择对应躲法"
            };

            if (hitCountInCurrentFight >= 2)
            {
                hint += "\n提示：先保住血量，绿色破绽时再冲刺反击。";
            }

            return hint;
        }

        /// <summary>记录玩家被击中（用于复盘）。</summary>
        private void RecordPlayerHit(BossPattern pattern)
        {
            lastHitPattern = pattern;
            hitCountInCurrentFight++;
        }

        /// <summary>重置本战命中统计。</summary>
        private void ResetFightStats()
        {
            lastHitPattern = BossPattern.None;
            hitCountInCurrentFight = 0;
        }
    }
}
