using System;
using UnityEngine;

namespace PenguinRun
{
    [Serializable]
    public sealed class RunnerTuningConfig
    {
        [Header("Movement")]
        public float LaneWidth = 3f;
        public float LaneLerp = 29f;
        public float Gravity = 22f;
        public float JumpVelocity = 12f;
        public float SlideDuration = 0.56f;
        public float GroundY = 0.9f;
        public float GroundEpsilon = 0.025f;
        public float CoyoteTime = 0.16f;
        public float JumpBufferTime = 0.18f;
        public float SlideBufferTime = 0.16f;

        [Header("Rhythm")]
        public float StartSpeed = 10f;
        public float MaxSpeed = 32f;
        public float SpawnDistance = 90f;
        public float InitialSpawnZ = 22f;
        public float DespawnZ = -8f;
        public float SpawnSpacingMin = 15f;
        public float SpawnSpacingMax = 21f;
        public float SpawnLeadMin = 8f;
        public float SpawnLeadMax = 15f;
        public float SpeedRampPerMeter = 0.014f;
        public float DashSpeedMultiplier = 1.22f;

        [Header("Scoring")]
        public float ScoreDistanceFactor = 1.8f;
        public float ScoreBoostMultiplier = 1.35f;
        public int CoinScore = 10;
        public float CoinComboTimeout = 1.15f;
        public int ComboBonusPerCoin = 2;
        public int ComboBonusMax = 36;

        [Tooltip("Every N fish snacks in the current streak counts as one tier for score multiplier.")]
        public int CoinComboTierEvery = 3;

        public float CoinComboMultiplierPerTier = 0.08f;

        public int CoinComboTierCap = 5;

        public float SlowMoSpeedMultiplier = 0.84f;

        [Header("Spawns")]
        public float PowerUpBaseChance = 0.11f;
        public float PowerUpChancePerPolarLevel = 0.025f;
        public float CoinLineChance = 0.24f;
        public float SingleObstacleChance = 0.28f;

        [Tooltip("Random roll breakpoints for power-up kind when spawning a pickup (values on [0,1]).")]
        public float PowerPickDashMax = 0.24f;
        public float PowerPickMagnetMax = 0.46f;
        public float PowerPickShieldMax = 0.67f;
        public float PowerPickScoreStarMax = 0.80f;

        [Tooltip("Width on [0,1] after ScoreStar threshold reserved for Double Fish pickup.")]
        public float PowerPickDoubleFishBand = 0.055f;

        [Tooltip("Width reserved for Hourglass after DoubleFish band.")]
        public float PowerPickHourglassBand = 0.045f;

        [Header("Pickups")]
        public float CoinCollectX = 1f;
        public float CoinCollectZ = 0.75f;
        public float CoinCollectY = 1.4f;
        public float MagnetCollectXBase = 3.25f;
        public float MagnetCollectXPerLevel = 0.35f;
        public float MagnetCollectZ = 2.4f;

        [Header("Camera (跟随第三人称)")]
        [Tooltip("必须与 FramingReference / RunnerCameraTuning 一致：behind 10.5 / height 6.5 / FOV 72。")]
        public float CameraBehindDistance = 10.5f;

        public float CameraHeightAboveSurface = 6.5f;

        [Tooltip("LookAt 瞄准点在角色前方的距离。")]
        public float CameraAimAhead = 19f;

        public float CameraAimHeightAboveSurface = 0.25f;
        public float CameraFieldOfView = 72f;

        [Range(0.72f, 1f)]
        [Tooltip("企鹅模型整体缩放。")]
        public float PlayerVisualScale = 0.86f;

        [Header("PowerUp Durations (base + level * perLevel)")]
        public float DashBaseSeconds = 5.5f;
        public float DashPerLevelSeconds = 0.9f;
        public float MagnetBaseSeconds = 7f;
        public float MagnetPerLevelSeconds = 1.4f;
        public float ShieldBaseSeconds = 4.5f;
        public float ShieldPerTuanLevelSeconds = 0.8f;
        public float ScoreStarBaseSeconds = 7.5f;
        public float ScoreStarPerPolarLevelSeconds = 0.8f;
        public float GlideBaseSeconds = 6f;
        public float GlidePerTuanLevelSeconds = 0.7f;

        public float DoubleFishBaseSeconds = 6f;
        public float DoubleFishPerPolarLevelSeconds = 0.65f;

        public float HourglassBaseSeconds = 4.2f;
        public float HourglassPerPolarLevelSeconds = 0.55f;

        public float GlideGravityMultiplier = 0.56f;
        public float GlideJumpMultiplier = 1.12f;

        [Header("Combat / Invulnerability")]
        public float InvulnAfterObstacleHit = 2.0f;
        public float InvulnAfterFishBombObstacle = 1.2f;
        public float InvulnAfterFishBombBoss = 1.2f;
        public float InvulnAfterBossHit = 2.0f;
        public float InvulnAfterShieldBreak = 1.0f;
        public float InvulnAfterIceMirror = 1.0f;
        public float InvulnAfterTreantArmor = 1.1f;
        public float InvulnAfterCoralBounce = 1.8f;

        [Header("Perfect Action Window")]
        public float PerfectWindowJumpSeconds = 0.32f;
        // 与 SlideDuration(0.56s) 对齐：滑铲过门的大部分时段都可判完美，消除“早滑安全却不记完美”的错配
        public float PerfectWindowSlideSeconds = 0.50f;
        public float PerfectWindowAirSlideSeconds = 0.28f;
        public float PerfectWindowDoubleJumpSeconds = 0.28f;

        [Header("Boss Dodge")]
        public float BossAirDodgeHeight = 0.55f;
        public float BossSafeLaneHalfFactor = 0.72f;
        public float BossOffLaneFactor = 0.38f;
        public float BossSalvoLaneFactor = 0.38f;
        public float BossCenterBeamLaneFactor = 0.32f;

        public static RunnerTuningConfig Default() => new();
    }
}

