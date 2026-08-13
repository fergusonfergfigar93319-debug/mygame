using UnityEngine;

namespace PenguinRun
{
    internal enum HitOutcomeKind
    {
        None,
        PerfectDodge,
        BlockedByDefense,
        Damage,
        GameOver,
    }

    internal readonly struct HitOutcome
    {
        public HitOutcomeKind Kind { get; }
        public string FeedbackText { get; }
        public float FeedbackDuration { get; }
        public float InvulnerabilitySeconds { get; }
        public bool RemoveObstacle { get; }
        public bool ConsumeShield { get; }
        public bool PlayCrashSound { get; }

        public HitOutcome(
            HitOutcomeKind kind,
            string feedbackText = "",
            float feedbackDuration = 0f,
            float invulnerabilitySeconds = 0f,
            bool removeObstacle = false,
            bool consumeShield = false,
            bool playCrashSound = false)
        {
            Kind = kind;
            FeedbackText = feedbackText;
            FeedbackDuration = feedbackDuration;
            InvulnerabilitySeconds = invulnerabilitySeconds;
            RemoveObstacle = removeObstacle;
            ConsumeShield = consumeShield;
            PlayCrashSound = playCrashSound;
        }

        public static HitOutcome None => new(HitOutcomeKind.None);
    }

    /// <summary>
    /// 统一障碍/Boss 命中后的防御与扣血判定。
    /// </summary>
    internal static class RunnerHitResolver
    {
        public static HitOutcome TryResolveObstacleDefense(
            WorldDirector world,
            in ObstacleColliderSpec spec,
            RunnerCombatTuning combat)
        {
            if (world.ShieldTimer > 0f)
            {
                // 护盾为消耗制并附碎裂无敌（与 Boss 路径同一条款），否则挡下障碍后下一帧可立刻再受伤。
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "护盾抵消",
                    0.8f,
                    combat.invulnAfterShieldBreak,
                    removeObstacle: true,
                    consumeShield: true);
            }

            if (world.TryConsumeFishBomb())
            {
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "鱼弹反弹！",
                    0.85f,
                    combat.invulnAfterFishBombObstacle,
                    removeObstacle: true);
            }

            if (world.IceMirrorTimer > 0f)
            {
                world.IceMirrorTimer = 0f;
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "冰镜折射！",
                    0.82f,
                    combat.invulnAfterIceMirror,
                    removeObstacle: true);
            }

            if (world.TryConsumeTreantArmor())
            {
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    $"树皮挡下！护甲×{world.TreantArmorHits}",
                    0.82f,
                    combat.invulnAfterTreantArmor,
                    removeObstacle: true);
            }

            if (world.TryConsumeCoralBounce())
            {
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "珊瑚回弹！",
                    0.9f,
                    combat.invulnAfterCoralBounce,
                    removeObstacle: true);
            }

            if (world.ThunderFeatherTimer > 0f && spec.IsThunderFeatherDestructible)
            {
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "雷羽击破！",
                    0.65f,
                    removeObstacle: true);
            }

            var gameOver = world.LoseOneLife();
            if (gameOver)
            {
                return new HitOutcome(
                    HitOutcomeKind.GameOver,
                    "生命耗尽",
                    1.4f,
                    combat.invulnAfterObstacleHit,
                    removeObstacle: true,
                    playCrashSound: true);
            }

            return new HitOutcome(
                HitOutcomeKind.Damage,
                $"受伤！剩余 {world.Lives} 心（2秒无敌）",
                0.95f,
                combat.invulnAfterObstacleHit,
                removeObstacle: true);
        }

        public static HitOutcome TryResolveBossHit(
            WorldDirector world,
            RunnerCombatTuning combat,
            bool hadShield)
        {
            if (hadShield)
            {
                world.ShieldTimer = 0f;
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "护盾碎裂，挡下 BOSS 攻击！等破绽出现后再冲刺反击",
                    1f,
                    combat.invulnAfterShieldBreak,
                    consumeShield: true);
            }

            if (world.TryConsumeFishBomb())
            {
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "鱼弹反弹 BOSS 攻击！",
                    1f,
                    combat.invulnAfterFishBombBoss);
            }

            if (world.IceMirrorTimer > 0f)
            {
                world.IceMirrorTimer = 0f;
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "冰镜折射 BOSS 攻击！",
                    1f,
                    combat.invulnAfterIceMirror);
            }

            if (world.TryConsumeTreantArmor())
            {
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    $"树皮挡下 BOSS 攻击！护甲×{world.TreantArmorHits}",
                    1f,
                    combat.invulnAfterTreantArmor);
            }

            if (world.TryConsumeCoralBounce())
            {
                return new HitOutcome(
                    HitOutcomeKind.BlockedByDefense,
                    "珊瑚回弹 BOSS 攻击！",
                    1f,
                    combat.invulnAfterCoralBounce);
            }

            world.SetHitInvulnerability(combat.invulnAfterBossHit);
            var gameOver = world.LoseOneLife();
            if (gameOver)
            {
                return new HitOutcome(
                    HitOutcomeKind.GameOver,
                    "被 BOSS 击倒",
                    1.4f,
                    playCrashSound: true);
            }

            return new HitOutcome(
                HitOutcomeKind.Damage,
                "",
                1f);
        }

        public static bool IsBossPatternHit(
            BossPattern pattern,
            bool isPatternDangerous,
            float playerX,
            float playerY,
            float groundY,
            bool playerSliding,
            float playerDashTimer,
            float laneWidth,
            RunnerCombatTuning combat,
            BossEncounter boss)
        {
            if (!isPatternDangerous) return false;

            var playerAirborne = playerY > groundY + combat.bossAirDodgeHeight;
            var halfLane = laneWidth * combat.bossSafeLaneHalfFactor;
            var offLane = laneWidth * combat.bossOffLaneFactor;

            switch (pattern)
            {
                case BossPattern.SweepLow:
                    if (playerSliding || playerAirborne) return false;
                    if (Mathf.Abs(playerX - boss.SafeLaneX) < halfLane) return false;
                    break;

                case BossPattern.DiveHigh:
                    // 用开战时锁定的俯冲车道判定：Boss 根节点 x 每帧向中线回插，
                    // 读 WorldX 会让命中带漂到中道、与预警圈车道相反。
                    if (playerAirborne) return false;
                    if (Mathf.Abs(playerX - boss.DiveLaneX) > offLane) return false;
                    break;

                case BossPattern.ChargeAcross:
                    if (playerAirborne) return false;
                    if (Mathf.Abs(playerX - boss.SafeLaneX) < halfLane) return false;
                    break;

                case BossPattern.RangedSalvo:
                    // 齐射：冲刺 / 跳跃可躲；只在弹道落地附近判伤，避免“球还没到就扣血”。
                    if (playerDashTimer > 0f || playerAirborne) return false;
                    if (!IsInSalvoLane(boss, playerX, laneWidth * combat.bossSalvoLaneFactor)) return false;
                    if (!boss.IsSalvoImpactActive) return false;
                    break;

                case BossPattern.CenterBeam:
                    if (playerAirborne) return false;
                    if (Mathf.Abs(playerX) > laneWidth * combat.bossCenterBeamLaneFactor) return false;
                    break;

                case BossPattern.QuakePulse:
                    if (playerSliding || playerAirborne) return false;
                    break;

                default:
                    return false;
            }

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
    }
}
