using UnityEngine;

namespace PenguinRun
{
    internal sealed class WorldDirector
    {
        private readonly RunnerWorldTuning worldTuning;
        private readonly RunnerPickupTuning pickupTuning;

        private int coinPickupScoreAccumulated;

        public WorldDirector(RunnerWorldTuning worldTuning, RunnerPickupTuning pickupTuning)
        {
            this.worldTuning = worldTuning;
            this.pickupTuning = pickupTuning;
            Speed = worldTuning.startSpeed;
        }

        public float Distance { get; private set; }
        public float RunTime { get; private set; }
        public float Speed { get; private set; }
        public int Score { get; private set; }

        public int Coins { get; private set; }
        public int ComboBonusScore { get; private set; }
        public int CoinCombo { get; private set; }

        public float DashTimer { get; set; }
        public float MagnetTimer { get; set; }
        public float ShieldTimer { get; set; }
        public float ScoreBoostTimer { get; set; }
        public float GlideTimer { get; set; }
        public float DoubleFishTimer { get; private set; }
        public float SlowMoTimer { get; private set; }

        // ── 新道具状态 ────────────────────────────────────────
        /// <summary>冰镜反射：激活时碰撞障碍被弹开而非受伤（类似临时护盾但无声）。</summary>
        public float IceMirrorTimer { get; set; }
        /// <summary>极光连锁：激活时金币连击计时器不归零，combo 持续累积。</summary>
        public float AuroraChainTimer { get; set; }
        /// <summary>雾灯：激活时同时享有延长磁吸和慢动作。</summary>
        public float FogLanternTimer { get; set; }
        /// <summary>树人护甲：可吸收的连续伤害次数（&gt;0 时视为护盾叠层）。</summary>
        public int TreantArmorHits { get; private set; }
        /// <summary>珊瑚回弹：是否就绪（下次受击时弹出+短暂冲刺）。</summary>
        public bool CoralBounceReady { get; private set; }
        /// <summary>雷羽：激活时冲刺+磁吸同时生效，冲刺期间可摧毁小障碍。</summary>
        public float ThunderFeatherTimer { get; set; }

        /// <summary>当前剩余生命（心）。归零时本局结束。</summary>
        public int Lives { get; private set; }

        public int MaxLives { get; private set; } = 3;

        /// <summary>受伤后的无敌时间（秒）。</summary>
        public float HitInvulnerabilityTimer { get; private set; }

        /// <summary>Multiplier applied to the last fish snack pickup score increment.</summary>
        public float LastFishScoreMultiplier { get; private set; } = 1f;

        public int PeakCoinCombo { get; private set; }

        /// <summary>已击败 boss 数量（HUD 显示用）。</summary>
        public int BossesDefeated { get; set; }

        /// <summary>完美闪避次数（在风险事件命中前 0.25s 内做出正确动作）。</summary>
        public int PerfectDodges { get; private set; }

        /// <summary>FishBomb 持有数量：受伤时自动消耗 1 个抵消伤害。</summary>
        public int FishBombs { get; private set; }
        public string LastBossSpeedTier { get; private set; } = "C";
        public int LastBossSpeedFishBonus { get; private set; }
        public int LastBossSpeedScoreBonus { get; private set; }

        /// <summary>暂停状态（不写到 Time.timeScale，由游戏循环判断）。</summary>
        public bool Paused { get; set; }

        private float comboTimer;

        public void ResetRun()
        {
            Distance = 0f;
            RunTime = 0f;
            Speed = worldTuning.startSpeed;
            Score = 0;
            Coins = 0;
            ComboBonusScore = 0;
            CoinCombo = 0;
            comboTimer = 0f;
            DashTimer = 0f;
            MagnetTimer = 0f;
            ShieldTimer = 0f;
            ScoreBoostTimer = 0f;
            GlideTimer = 0f;
            DoubleFishTimer = 0f;
            SlowMoTimer = 0f;
            IceMirrorTimer = 0f;
            AuroraChainTimer = 0f;
            FogLanternTimer = 0f;
            TreantArmorHits = 0;
            CoralBounceReady = false;
            ThunderFeatherTimer = 0f;
            Lives = MaxLives;
            HitInvulnerabilityTimer = 0f;
            LastFishScoreMultiplier = 1f;
            PeakCoinCombo = 0;
            coinPickupScoreAccumulated = 0;
            BossesDefeated = 0;
            PerfectDodges = 0;
            FishBombs = 0;
            LastBossSpeedTier = "C";
            LastBossSpeedFishBonus = 0;
            LastBossSpeedScoreBonus = 0;
            Paused = false;
        }

        public void AddTreantArmorHits(int hits) => TreantArmorHits = Mathf.Min(3, TreantArmorHits + hits);

        public bool TryConsumeTreantArmor()
        {
            if (TreantArmorHits <= 0) return false;
            TreantArmorHits -= 1;
            return true;
        }

        public void SetCoralBounce() => CoralBounceReady = true;

        public bool TryConsumeCoralBounce()
        {
            if (!CoralBounceReady) return false;
            CoralBounceReady = false;
            DashTimer = Mathf.Max(DashTimer, 1.2f);
            SetHitInvulnerability(1.8f);
            return true;
        }

        public void AddPerfectDodge()
        {
            PerfectDodges += 1;
            ComboBonusScore += 25;
        }

        public void AddFishBomb() => FishBombs += 1;

        public bool TryConsumeFishBomb()
        {
            if (FishBombs <= 0) return false;
            FishBombs -= 1;
            return true;
        }

        public void AddExtraLife()
        {
            MaxLives = Mathf.Min(5, MaxLives + 1);
            Lives = Mathf.Min(MaxLives, Lives + 1);
        }

        public void AddBossReward(int fishBonus, int scoreBonus)
        {
            Coins += fishBonus;
            ComboBonusScore += scoreBonus;
            BossesDefeated += 1;
        }

        public void SetBossSpeedResult(string tier, int fishBonus, int scoreBonus)
        {
            LastBossSpeedTier = string.IsNullOrEmpty(tier) ? "C" : tier;
            LastBossSpeedFishBonus = Mathf.Max(0, fishBonus);
            LastBossSpeedScoreBonus = Mathf.Max(0, scoreBonus);
        }

        public float Tick(float dt)
        {
            Speed = Mathf.Min(worldTuning.maxSpeed, worldTuning.startSpeed + Distance * worldTuning.speedPerDistance);
            var dashMult = DashTimer > 0f ? worldTuning.dashSpeedMultiplier : 1f;
            var slowMult = SlowMoTimer > 0f ? worldTuning.slowMoSpeedMultiplier : 1f;
            var effectiveSpeed = Speed * dashMult * slowMult;
            Distance += effectiveSpeed * dt;
            RunTime += dt;

            DashTimer = Mathf.Max(0f, DashTimer - dt);
            MagnetTimer = Mathf.Max(0f, MagnetTimer - dt);
            ShieldTimer = Mathf.Max(0f, ShieldTimer - dt);
            ScoreBoostTimer = Mathf.Max(0f, ScoreBoostTimer - dt);
            GlideTimer = Mathf.Max(0f, GlideTimer - dt);
            DoubleFishTimer = Mathf.Max(0f, DoubleFishTimer - dt);
            SlowMoTimer = Mathf.Max(0f, SlowMoTimer - dt);
            IceMirrorTimer = Mathf.Max(0f, IceMirrorTimer - dt);
            AuroraChainTimer = Mathf.Max(0f, AuroraChainTimer - dt);
            FogLanternTimer = Mathf.Max(0f, FogLanternTimer - dt);
            ThunderFeatherTimer = Mathf.Max(0f, ThunderFeatherTimer - dt);
            comboTimer = Mathf.Max(0f, AuroraChainTimer > 0f ? comboTimer + dt * 0.5f : comboTimer - dt);
            HitInvulnerabilityTimer = Mathf.Max(0f, HitInvulnerabilityTimer - dt);
            if (comboTimer <= 0f)
            {
                CoinCombo = 0;
            }

            var scoreBoost = ScoreBoostTimer > 0f ? worldTuning.scoreBoostMultiplier : 1f;
            Score =
                Mathf.RoundToInt(Distance * worldTuning.scorePerDistance * scoreBoost) +
                coinPickupScoreAccumulated +
                ComboBonusScore;
            return effectiveSpeed;
        }

        public void AddCoin(int coinsDelta, int comboBonusDelta, float comboWindowSeconds)
        {
            Coins += coinsDelta;
            CoinCombo += 1;
            comboTimer = comboWindowSeconds;
            ComboBonusScore += comboBonusDelta;

            var every = Mathf.Max(1, pickupTuning.coinComboTierEvery);
            var tier = Mathf.Min(
                pickupTuning.coinComboTierCap,
                Mathf.Max(0, CoinCombo - 1) / every);
            LastFishScoreMultiplier = 1f + tier * pickupTuning.coinComboMultiplierPerTier;
            PeakCoinCombo = Mathf.Max(PeakCoinCombo, CoinCombo);

            var doubleMult = DoubleFishTimer > 0f ? 2f : 1f;
            coinPickupScoreAccumulated +=
                Mathf.RoundToInt(worldTuning.scorePerCoin * LastFishScoreMultiplier * doubleMult);
        }

        public void ExtendDoubleFish(float seconds)
        {
            DoubleFishTimer = Mathf.Max(DoubleFishTimer, seconds);
        }

        public void ExtendSlowMo(float seconds)
        {
            SlowMoTimer = Mathf.Max(SlowMoTimer, seconds);
        }

        /// <summary>触发受伤无敌，防止同一帧多段判伤。</summary>
        public void SetHitInvulnerability(float seconds) =>
            HitInvulnerabilityTimer = Mathf.Max(HitInvulnerabilityTimer, seconds);

        /// <summary>扣一颗心；返回 true 表示生命耗尽。</summary>
        public bool LoseOneLife()
        {
            Lives = Mathf.Max(0, Lives - 1);
            return Lives <= 0;
        }

        public float Speed01() => Mathf.InverseLerp(worldTuning.startSpeed, worldTuning.maxSpeed, Speed);
    }
}
