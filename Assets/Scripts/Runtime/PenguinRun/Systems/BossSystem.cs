using System.Collections.Generic;
using PenguinRun.Game;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Boss 攻击模式：决定 boss 当前回合的危险区域与玩家应有反应。
    /// </summary>
    internal enum BossPattern
    {
        None,
        // 横扫地面：左/中/右 三车道随机一个安全区域，其余车道有低障碍 → 滑铲或换道避开
        SweepLow,
        // 高空俯冲：boss 飞高然后落到某车道造成冲击 → 跳跃或换道
        DiveHigh,
        // 全场冲锋：boss 横向移动覆盖全部车道 → 玩家必须跳起
        ChargeAcross,
        // 投射物：boss 投出多枚抛物线投射物，落到随机车道 → 换道躲避
        RangedSalvo,
        // 中央光束：中央车道危险，需切边道或起跳
        CenterBeam,
        // 震地脉冲：全场地面冲击，需滑铲规避
        QuakePulse,
        // 短暂破绽：boss 显露弱点，玩家可冲刺通过造成伤害
        Vulnerable,
    }

    /// <summary>
    /// Boss 阶段：影响行动节奏与攻击模式选择。
    /// </summary>
    internal enum BossPhase
    {
        Spawning,    // boss 浮现，禁用攻击
        Active,      // 正常战斗
        Vulnerable,  // 露出弱点
        Defeated,    // 已击败，准备退场
        Retreating,  // 退场中
    }

    /// <summary>
    /// Boss 系统：每隔 N 米触发一场 boss 战，玩家在跑酷过程中遇到 boss、躲攻击、找机会反击。
    /// 通过冲刺穿过破绽窗口可造成伤害；3 次伤害后 boss 战结束并发放奖励。
    /// </summary>
    internal sealed class BossSystem
    {
        // ── 战斗参数（固定） ──────────────────────────────────
        public const int BossMaxHits = 3;
        public const float BossSpawnAheadZ = 30f;
        public const float BossActiveZ = 14f;
        public const float VulnerableSeconds = 1.6f;
        public const float HitInvulnerabilitySeconds = 0.6f;
        public const float DefeatRetreatSeconds = 1.4f;

        // ── 难度相关（运行时从 DifficultyPreset 注入） ────────
        private float firstBossDistance;
        private float bossInterval;
        private float bossPatternMinSeconds;
        private float bossPatternMaxSeconds;
        private float bossDangerWindowRatio;
        private float bossTelegraphLifetimeRatio;
        private float bossVulnerableChance;

        // ── 奖励 ─────────────────────────────────────────────
        public const int BossDefeatBonusFishSnacks = 25;
        public const int BossDefeatBonusScore = 800;
        public const int BossPhaseDamageScore = 150;

        // ── 状态 ─────────────────────────────────────────────
        public BossEncounter CurrentBoss { get; private set; }
        public bool BossActive => CurrentBoss != null && CurrentBoss.Phase != BossPhase.Defeated;
        public int BossesDefeated { get; private set; }
        private float nextBossDistance;
        private readonly HashSet<BossPattern> explainedPatterns = new();
        private bool explainedCounterWindow;
        private bool guidanceEnabledForCurrentBoss;
        private BossPattern lastKnownPattern = BossPattern.None;
        private BossPhase lastKnownPhase = BossPhase.Spawning;
        private string difficultyHintTag = "NORMAL";
        private bool guidancePauseRequest;
        private float currentBossElapsed;
        private float speedRewardWindowSeconds = 13f;
        private int speedRewardMaxFish = 14;
        private int speedRewardMaxScore = 420;

        // ── 反馈 ─────────────────────────────────────────────
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
            }
        }

        /// <summary>
        /// 每帧调用：根据玩家距离触发 boss、推进战斗、处理逻辑。
        /// 返回值：玩家此帧是否应该被 boss 攻击命中（外部按受伤逻辑处理）。
        /// </summary>
        public bool Tick(float dt, float runDistance, float playerX, float playerY, float groundY,
            bool playerSliding, float playerDashTimer, float playerShieldTimer,
            ref string feedbackText, ref float feedbackTimer)
        {
            // 触发 boss
            if (CurrentBoss == null && runDistance >= nextBossDistance)
            {
                SpawnBoss(runDistance);
                if (guidanceEnabledForCurrentBoss)
                {
                    feedbackText = $"\u26A0 BOSS 来袭：{CurrentBoss.Definition.DisplayName}  [{difficultyHintTag}]\n{GetDashHowToHint()}，再在破绽期冲刺反击";
                    feedbackTimer = 1.9f;
                }
                else
                {
                    feedbackText = $"\u26A0 BOSS 再临：{CurrentBoss.Definition.DisplayName}";
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
        /// 当 boss 战激活时，调用该方法决定是否应暂停常规障碍生成。
        /// </summary>
        public bool ShouldPauseSpawning => config.Daily || BossActive;

        private void SpawnBoss(float runDistance)
        {
            var def = BossDefinitions.PickFor(config.MapTheme, BossesDefeated);
            CurrentBoss = new BossEncounter(def, baseGroundY, laneWidth, runDistance,
                bossPatternMinSeconds, bossPatternMaxSeconds,
                bossDangerWindowRatio, bossTelegraphLifetimeRatio, bossVulnerableChance);
            CurrentBoss.Root.transform.SetParent(sceneRoot, false);
            explainedPatterns.Clear();
            explainedCounterWindow = false;
            guidanceEnabledForCurrentBoss = !PlayerSave.HasDefeatedBoss(def.BossId);
            lastKnownPattern = BossPattern.None;
            lastKnownPhase = BossPhase.Spawning;
            guidancePauseRequest = false;
            currentBossElapsed = 0f;
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

            // 玩家在破绽期 + 冲刺通过 = 命中
            if (boss.Phase == BossPhase.Vulnerable && playerDashTimer > 0f)
            {
                if (CanCounterBossInVulnerable(boss, playerX, laneWidth * 1.2f))
                {
                    DamageBoss(boss, ref feedbackText, ref feedbackTimer);
                    return false;
                }
            }

            // 护盾 ramming：护盾期间在 boss 旁也可造成伤害
            if (boss.Phase == BossPhase.Vulnerable && playerShieldTimer > 0f)
            {
                if (CanCounterBossInVulnerable(boss, playerX, laneWidth * 1.5f))
                {
                    DamageBoss(boss, ref feedbackText, ref feedbackTimer);
                    return false;
                }
            }

            // 检测 boss 攻击命中玩家
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
            // 攻击在击发阶段（patternTimer 处于危险窗口）才有伤害
            if (!boss.IsPatternDangerous) return false;

            switch (boss.Pattern)
            {
                case BossPattern.SweepLow:
                    // 低扫：滑铲 / 在安全车道（boss.SafeLaneIndex）可避免
                    if (playerSliding) return false;
                    if (Mathf.Abs(playerX - boss.SafeLaneX) < laneWidth * 0.5f) return false;
                    break;

                case BossPattern.DiveHigh:
                    // 高扑：跳跃 / 不在 boss 所在车道可避免
                    var inAir = playerY > groundY + 0.6f;
                    if (inAir) return false;
                    if (Mathf.Abs(playerX - boss.WorldX) > laneWidth * 0.55f) return false;
                    break;

                case BossPattern.ChargeAcross:
                    // 横冲：必须跳跃
                    if (playerY > groundY + 0.6f) return false;
                    break;

                case BossPattern.RangedSalvo:
                    // 投射物：在投射物落点车道才会被击中
                    if (playerDashTimer > 0f) return false;
                    if (!IsInSalvoLane(boss, playerX, laneWidth * 0.42f)) return false;
                    break;

                case BossPattern.CenterBeam:
                    // 中央光束：中心车道危险；起跳也可规避
                    if (playerY > groundY + 0.6f) return false;
                    if (Mathf.Abs(playerX) > laneWidth * 0.45f) return false;
                    break;

                case BossPattern.QuakePulse:
                    // 震地脉冲：需滑铲躲过低位冲击
                    if (playerSliding) return false;
                    break;
            }

            // 命中：护盾抵消，否则触发受伤
            if (playerShieldTimer > 0f)
            {
                feedbackText = "护盾抵消 BOSS 攻击！等待破绽后冲刺反击";
                feedbackTimer = 1f;
                boss.HitFlashTimer = 0.4f;
                return false;
            }

            feedbackText = guidanceEnabledForCurrentBoss
                ? $"{boss.Definition.DisplayName} 命中！{GetPatternDodgeHint(boss.Pattern)}"
                : $"{boss.Definition.DisplayName} 命中！";
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
            // 跑酷里玩家通常在 z≈0，boss 在前方固定 z。此前直接用 |bossZ|<4 导致反击窗口几乎永远失败。
            // 这里改为：boss 处在可交战前场区间 + 玩家车道对齐，即可判定冲刺/护盾反击成立。
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
                ComputePendingDefeatReward(currentBossElapsed, out var fishReward, out var scoreReward,
                    out var speedTier, out var speedFish, out var speedScore);
                feedbackText = speedScore > 0 || speedFish > 0
                    ? $"\u2605 击败 {boss.Definition.DisplayName}！速杀 {speedTier}  +{speedFish}鱼干 +{speedScore}分"
                    : $"\u2605 击败 {boss.Definition.DisplayName}！+{fishReward} 鱼干";
                feedbackTimer = 2.2f;
                audio.PlayBossDefeat();
                MarkDefeatRewardPending(fishReward, scoreReward, speedTier, speedFish, speedScore);
            }
            else
            {
                feedbackText = $"BOSS 受伤！剩余 HP {boss.HitsRemaining}/{BossMaxHits}";
                feedbackTimer = 1.2f;
            }
        }

        /// <summary>
        /// 当外部检测到 boss 战刚结束（boss 完全消失）时，调用这个方法领取奖励。
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

        /// <summary>当 boss 在 ProcessCombat 内被击杀，标记奖励等待领取。</summary>
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

        public string CurrentBossPatternLabel
        {
            get
            {
                if (CurrentBoss == null) return string.Empty;
                if (CurrentBoss.Phase == BossPhase.Vulnerable) return "弱点暴露";
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
                    return $"趁现在冲刺穿过 Boss；若没冲刺，先{GetDashHowToHint()}或开盾贴脸撞击";
                }
                return "先躲技能，等绿色破绽圈出现后再反击";
            }
        }

        private void ApplyDifficultyBossTuning(DifficultyKind kind, DifficultyPreset preset, bool bossRushMode)
        {
            var cadenceMultiplier = 1f;
            var intervalMultiplier = 1f;
            bossDangerWindowRatio = 0.55f;
            bossTelegraphLifetimeRatio = 0.6f;
            bossVulnerableChance = 0.5f;

            switch (kind)
            {
                case DifficultyKind.Easy:
                    cadenceMultiplier = 1.15f;
                    intervalMultiplier = 1.2f;
                    bossDangerWindowRatio = 0.45f;
                    bossTelegraphLifetimeRatio = 0.72f;
                    bossVulnerableChance = 0.62f;
                    difficultyHintTag = "EASY";
                    break;
                case DifficultyKind.Hard:
                    cadenceMultiplier = 0.86f;
                    intervalMultiplier = 0.82f;
                    bossDangerWindowRatio = 0.62f;
                    bossTelegraphLifetimeRatio = 0.53f;
                    bossVulnerableChance = 0.42f;
                    difficultyHintTag = "HARD";
                    break;
                case DifficultyKind.Expert:
                    cadenceMultiplier = 0.74f;
                    intervalMultiplier = 0.68f;
                    bossDangerWindowRatio = 0.68f;
                    bossTelegraphLifetimeRatio = 0.46f;
                    bossVulnerableChance = 0.34f;
                    difficultyHintTag = "EXPERT";
                    break;
                default:
                    cadenceMultiplier = 1f;
                    intervalMultiplier = 1f;
                    bossDangerWindowRatio = 0.55f;
                    bossTelegraphLifetimeRatio = 0.6f;
                    bossVulnerableChance = 0.5f;
                    difficultyHintTag = "NORMAL";
                    break;
            }

            if (bossRushMode)
            {
                firstBossDistance = kind switch
                {
                    DifficultyKind.Easy => 120f,
                    DifficultyKind.Hard => 70f,
                    DifficultyKind.Expert => 50f,
                    _ => 90f,
                };
                bossInterval = kind switch
                {
                    DifficultyKind.Easy => 240f,
                    DifficultyKind.Hard => 170f,
                    DifficultyKind.Expert => 130f,
                    _ => 200f,
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

            bossPatternMinSeconds = Mathf.Max(0.65f, preset.BossPatternMinSeconds * cadenceMultiplier);
            bossPatternMaxSeconds = Mathf.Max(bossPatternMinSeconds + 0.16f, preset.BossPatternMaxSeconds * cadenceMultiplier);
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
                    ? $"破绽窗口出现！现在冲刺反击（{GetDashHowToHint()}）"
                    : $"绿色破绽圈出现：冲刺穿过 Boss 可造成伤害\n{GetDashHowToHint()}，没有冲刺可用护盾撞击";
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
                BossPattern.SweepLow => "低位横扫",
                BossPattern.DiveHigh => "俯冲砸击",
                BossPattern.ChargeAcross => "全场冲锋",
                BossPattern.RangedSalvo => "远程齐射",
                BossPattern.CenterBeam => "中央光束",
                BossPattern.QuakePulse => "震地脉冲",
                BossPattern.Vulnerable => "破绽窗口",
                _ => "准备攻击",
            };
        }

        private static string GetPatternDodgeHint(BossPattern pattern)
        {
            return pattern switch
            {
                BossPattern.SweepLow => "滑铲或切到安全车道",
                BossPattern.DiveHigh => "提前换道，或起跳躲冲击",
                BossPattern.ChargeAcross => "立刻起跳越过冲锋",
                BossPattern.RangedSalvo => "看预警圈换道，冲刺可穿弹",
                BossPattern.CenterBeam => "离开中路，或起跳穿过光束",
                BossPattern.QuakePulse => "立刻滑铲，躲低位震波",
                _ => "保持移动，观察预警圈",
            };
        }

        private static string GetDashHowToHint()
        {
            return "双击屏幕冲刺（也可拾取冲刺道具）";
        }
    }
}
