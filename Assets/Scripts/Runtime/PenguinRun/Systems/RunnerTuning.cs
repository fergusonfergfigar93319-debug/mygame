using UnityEngine;

namespace PenguinRun
{
    [System.Serializable]
    public sealed class RunnerMovementTuning
    {
        [Header("Lanes")]
        public float laneWidth = 3f;
        public float laneLerp = 23f;

        [Header("Vertical")]
        public float gravity = 22f;
        public float jumpVelocity = 12f;
        public float slideDuration = 0.56f;

        [Header("Feel")]
        public float coyoteTime = 0.16f;
        public float jumpBufferTime = 0.18f;
        public float groundY = 0.9f;
        public float groundEpsilon = 0.025f;
    }

    [System.Serializable]
    public sealed class RunnerWorldTuning
    {
        [Header("Speed")]
        public float startSpeed = 10f;
        public float maxSpeed = 32f;
        public float speedPerDistance = 0.018f;
        public float dashSpeedMultiplier = 1.22f;

        [Header("Scoring")]
        public float scorePerDistance = 1.8f;
        public int scorePerCoin = 10;
        public float scoreBoostMultiplier = 1.35f;
        public float slowMoSpeedMultiplier = 0.84f;

        [Header("Spawning")]
        public float spawnDistance = 90f;
        public float initialSpawnZ = 22f;
        public float despawnZ = -8f;
        public float spawnLeadMin = 8f;
        public float spawnLeadMax = 15f;
        public float spawnSpacingMin = 14f;
        public float spawnSpacingMax = 21f;
    }

    [System.Serializable]
    public sealed class RunnerCameraTuning
    {
        public float behindDistance = 10.5f;
        public float heightAboveSurface = 5.5f;
        public float aimAhead = 15f;
        public float aimHeightAboveSurface = 1.0f;
        public float fieldOfView = 72f;
        public float playerVisualScale = 0.86f;
    }

    [System.Serializable]
    public sealed class RunnerPickupTuning
    {
        [Header("Coins")]
        public float coinCollectX = 1f;
        public float coinCollectZ = 0.75f;
        public float coinCollectY = 1.4f;

        [Header("Magnet")]
        public float magnetCollectXBase = 3.25f;
        public float magnetCollectXPerLevel = 0.35f;
        public float magnetCollectZ = 2.4f;

        [Header("Combo")]
        public float comboWindowSeconds = 1.15f;
        public int comboBonusPerCoin = 2;
        public int comboBonusMax = 36;
        public int coinComboTierEvery = 3;
        public float coinComboMultiplierPerTier = 0.08f;
        public int coinComboTierCap = 5;
    }

    [System.Serializable]
    public sealed class RunnerPowerUpTuning
    {
        [Header("Spawn chances")]
        public float basePowerUpChance = 0.08f;
        public float powerUpChancePerPolarLevel = 0.025f;

        [Header("Segment rolls")]
        public float coinLineSpawnChance = 0.24f;
        public float singleObstacleSpawnChance = 0.28f;

        [Header("Power-up kind roll (cumulative thresholds on [0,1])")]
        public float powerPickDashMax = 0.24f;
        public float powerPickMagnetMax = 0.46f;
        public float powerPickShieldMax = 0.67f;
        public float powerPickScoreStarMax = 0.84f;
        public float powerPickDoubleFishBand = 0.055f;
        public float powerPickHourglassBand = 0.045f;

        [Header("Durations (base + level * perLevel)")]
        public float dashBaseSeconds = 5.5f;
        public float dashPerLevelSeconds = 0.9f;
        public float magnetBaseSeconds = 7f;
        public float magnetPerLevelSeconds = 1.4f;
        public float shieldBaseSeconds = 4.5f;
        public float shieldPerLevelSeconds = 0.8f;
        public float scoreStarBaseSeconds = 7.5f;
        public float scoreStarPerLevelSeconds = 0.8f;
        public float glideBaseSeconds = 6f;
        public float glidePerLevelSeconds = 0.7f;
        public float doubleFishBaseSeconds = 6f;
        public float doubleFishPerPolarLevelSeconds = 0.65f;
        public float hourglassBaseSeconds = 4.2f;
        public float hourglassPerPolarLevelSeconds = 0.55f;

        [Header("Jump modifiers")]
        public float glideGravityMultiplier = 0.56f;
        public float glideJumpMultiplier = 1.12f;
    }

    [System.Serializable]
    public sealed class RunnerTuning
    {
        public RunnerMovementTuning movement = new();
        public RunnerWorldTuning world = new();
        public RunnerPickupTuning pickup = new();
        public RunnerPowerUpTuning powerUp = new();
        public RunnerCameraTuning camera = new();

        public static RunnerTuning FromConfig(RunnerTuningConfig c)
        {
            return new RunnerTuning
            {
                movement = new RunnerMovementTuning
                {
                    laneWidth = c.LaneWidth,
                    laneLerp = c.LaneLerp,
                    gravity = c.Gravity,
                    jumpVelocity = c.JumpVelocity,
                    slideDuration = c.SlideDuration,
                    coyoteTime = c.CoyoteTime,
                    jumpBufferTime = c.JumpBufferTime,
                    groundY = c.GroundY,
                    groundEpsilon = c.GroundEpsilon,
                },
                world = new RunnerWorldTuning
                {
                    startSpeed = c.StartSpeed,
                    maxSpeed = c.MaxSpeed,
                    speedPerDistance = c.SpeedRampPerMeter,
                    dashSpeedMultiplier = c.DashSpeedMultiplier,
                    scorePerDistance = c.ScoreDistanceFactor,
                    scorePerCoin = c.CoinScore,
                    scoreBoostMultiplier = c.ScoreBoostMultiplier,
                    slowMoSpeedMultiplier = c.SlowMoSpeedMultiplier,
                    spawnDistance = c.SpawnDistance,
                    initialSpawnZ = c.InitialSpawnZ,
                    despawnZ = c.DespawnZ,
                    spawnLeadMin = c.SpawnLeadMin,
                    spawnLeadMax = c.SpawnLeadMax,
                    spawnSpacingMin = c.SpawnSpacingMin,
                    spawnSpacingMax = c.SpawnSpacingMax,
                },
                pickup = new RunnerPickupTuning
                {
                    coinCollectX = c.CoinCollectX,
                    coinCollectZ = c.CoinCollectZ,
                    coinCollectY = c.CoinCollectY,
                    magnetCollectXBase = c.MagnetCollectXBase,
                    magnetCollectXPerLevel = c.MagnetCollectXPerLevel,
                    magnetCollectZ = c.MagnetCollectZ,
                    comboWindowSeconds = c.CoinComboTimeout,
                    comboBonusPerCoin = c.ComboBonusPerCoin,
                    comboBonusMax = c.ComboBonusMax,
                    coinComboTierEvery = c.CoinComboTierEvery,
                    coinComboMultiplierPerTier = c.CoinComboMultiplierPerTier,
                    coinComboTierCap = c.CoinComboTierCap,
                },
                powerUp = new RunnerPowerUpTuning
                {
                    basePowerUpChance = c.PowerUpBaseChance,
                    powerUpChancePerPolarLevel = c.PowerUpChancePerPolarLevel,
                    coinLineSpawnChance = c.CoinLineChance,
                    singleObstacleSpawnChance = c.SingleObstacleChance,
                    powerPickDashMax = c.PowerPickDashMax,
                    powerPickMagnetMax = c.PowerPickMagnetMax,
                    powerPickShieldMax = c.PowerPickShieldMax,
                    powerPickScoreStarMax = c.PowerPickScoreStarMax,
                    powerPickDoubleFishBand = c.PowerPickDoubleFishBand,
                    powerPickHourglassBand = c.PowerPickHourglassBand,
                    dashBaseSeconds = c.DashBaseSeconds,
                    dashPerLevelSeconds = c.DashPerLevelSeconds,
                    magnetBaseSeconds = c.MagnetBaseSeconds,
                    magnetPerLevelSeconds = c.MagnetPerLevelSeconds,
                    shieldBaseSeconds = c.ShieldBaseSeconds,
                    shieldPerLevelSeconds = c.ShieldPerTuanLevelSeconds,
                    scoreStarBaseSeconds = c.ScoreStarBaseSeconds,
                    scoreStarPerLevelSeconds = c.ScoreStarPerPolarLevelSeconds,
                    glideBaseSeconds = c.GlideBaseSeconds,
                    glidePerLevelSeconds = c.GlidePerTuanLevelSeconds,
                    doubleFishBaseSeconds = c.DoubleFishBaseSeconds,
                    doubleFishPerPolarLevelSeconds = c.DoubleFishPerPolarLevelSeconds,
                    hourglassBaseSeconds = c.HourglassBaseSeconds,
                    hourglassPerPolarLevelSeconds = c.HourglassPerPolarLevelSeconds,
                    glideGravityMultiplier = c.GlideGravityMultiplier,
                    glideJumpMultiplier = c.GlideJumpMultiplier,
                },
                camera = new RunnerCameraTuning
                {
                    behindDistance = c.CameraBehindDistance,
                    heightAboveSurface = c.CameraHeightAboveSurface,
                    aimAhead = c.CameraAimAhead,
                    aimHeightAboveSurface = c.CameraAimHeightAboveSurface,
                    fieldOfView = c.CameraFieldOfView,
                    playerVisualScale = c.PlayerVisualScale,
                },
            };
        }
    }
}

