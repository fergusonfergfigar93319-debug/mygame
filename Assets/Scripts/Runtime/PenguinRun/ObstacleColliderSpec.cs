namespace PenguinRun
{
    /// <summary>
    /// Axis-aligned hit extents in world X/Z around obstacle transform position (player uses similar thresholds).
    /// Plus vertical clearance for jump-over logic.
    /// </summary>
    public readonly struct ObstacleColliderSpec
    {
        /// <summary>低障碍下方可通过的最大判定身高（配合 PlayerMotor.BodyHeight 蹲姿盒）。</summary>
        public const float DefaultPassUnderMaxHeight = 1.05f;

        public ObstacleColliderSpec(
            bool isLow,
            float halfDx,
            float halfDz,
            float feetClearanceAboveRoot = 999f,
            bool isThunderFeatherDestructible = false,
            float passUnderMaxHeight = DefaultPassUnderMaxHeight)
        {
            IsLow = isLow;
            HalfDx = halfDx;
            HalfDz = halfDz;
            FeetClearanceAboveRoot = feetClearanceAboveRoot;
            IsThunderFeatherDestructible = isThunderFeatherDestructible;
            PassUnderMaxHeight = passUnderMaxHeight;
        }

        public bool IsLow { get; }
        public float HalfDx { get; }
        public float HalfDz { get; }
        public float FeetClearanceAboveRoot { get; }

        /// <summary>雷羽冲刺态下可粉碎的小型可跳障碍。</summary>
        public bool IsThunderFeatherDestructible { get; }

        /// <summary>IsLow 障碍允许通过的判定身高上限；判定身高低于该值即视为从下方穿过。</summary>
        public float PassUnderMaxHeight { get; }

        public static ObstacleColliderSpec SmallJumpable => new(false, 0.50f, 0.44f, 0.78f, true);

        public static ObstacleColliderSpec LowSlideGate => new(true, 0.45f, 0.32f, 0.48f);

        public static ObstacleColliderSpec WideWall => new(false, 1.05f, 0.60f, 999f);

        public static ObstacleColliderSpec Enemy => new(false, 0.55f, 0.55f, 999f);

        public static ObstacleColliderSpec PatrolEnemy => new(false, 0.50f, 0.55f, 999f);

        public static ObstacleColliderSpec SwoopEnemy => new(false, 0.48f, 0.52f, 999f);

        public static ObstacleColliderSpec Rolling => new(false, 0.68f, 0.58f, 999f);

        public static ObstacleColliderSpec MediumBarrier => new(true, 0.55f, 0.42f, 0.92f);

        public static ObstacleColliderSpec WideSlideOnly => new(true, 1.02f, 0.50f, 0.42f);

        public static ObstacleColliderSpec TallSpike => new(false, 0.38f, 0.38f, 999f);

        public static ObstacleColliderSpec SmallFlying => new(false, 0.45f, 0.45f, 1.2f);

        public static ObstacleColliderSpec DefaultHigh => SmallJumpable;
        public static ObstacleColliderSpec DefaultLow => LowSlideGate;
    }
}
