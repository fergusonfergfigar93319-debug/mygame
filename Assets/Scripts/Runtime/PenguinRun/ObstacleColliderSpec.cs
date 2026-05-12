namespace PenguinRun
{
    /// <summary>
    /// Axis-aligned hit extents in world X/Z around obstacle transform position (player uses similar thresholds).
    /// Plus vertical clearance for jump-over logic.
    /// </summary>
    public readonly struct ObstacleColliderSpec
    {
        /// <param name="isLow">Whether this is a low/slide gate (legacy semantic, kept for compatibility).</param>
        /// <param name="halfDx">Half width (lane X) of hitbox.</param>
        /// <param name="halfDz">Half depth (run Z) of hitbox.</param>
        /// <param name="feetClearanceAboveRoot">
        /// Minimum player foot Y required to jump over this obstacle (relative to obstacle root Y).
        /// Set to a large value (e.g., 999f) to make the obstacle un-jumpable.
        /// </param>
        public ObstacleColliderSpec(bool isLow, float halfDx, float halfDz, float feetClearanceAboveRoot = 999f)
        {
            IsLow = isLow;
            HalfDx = halfDx;
            HalfDz = halfDz;
            FeetClearanceAboveRoot = feetClearanceAboveRoot;
        }

        public bool IsLow { get; }
        public float HalfDx { get; }
        public float HalfDz { get; }
        public float FeetClearanceAboveRoot { get; }

        /// <summary>Small rock/tree obstacles that can be jumped over.</summary>
        public static ObstacleColliderSpec SmallJumpable => new(false, 0.55f, 0.48f, 0.72f);

        /// <summary>Low slide gates: slide under or jump over.</summary>
        public static ObstacleColliderSpec LowSlideGate => new(true, 0.45f, 0.26f, 0.22f);

        /// <summary>Wide walls: cannot jump over, must slide under or switch lane.</summary>
        public static ObstacleColliderSpec WideWall => new(false, 1.28f, 0.66f, 999f);

        /// <summary>Enemies: cannot jump over (tall or flying).</summary>
        public static ObstacleColliderSpec Enemy => new(false, 0.55f, 0.55f, 999f);

        /// <summary>Rolling snowballs: cannot jump over (rolling hazard).</summary>
        public static ObstacleColliderSpec Rolling => new(false, 0.68f, 0.68f, 999f);

        /// <summary>Mid-height barrier: can be jumped over with a well-timed jump, OR slid under.</summary>
        public static ObstacleColliderSpec MediumBarrier => new(true, 0.55f, 0.42f, 1.05f);

        /// <summary>Wide low barrier: spans 2+ lanes but low enough to slide under.</summary>
        public static ObstacleColliderSpec WideSlideOnly => new(true, 1.15f, 0.50f, 0.28f);

        /// <summary>Tall spike: cannot jump over, must switch lane.</summary>
        public static ObstacleColliderSpec TallSpike => new(false, 0.38f, 0.38f, 999f);

        /// <summary>Small flying enemy: can be jumped over.</summary>
        public static ObstacleColliderSpec SmallFlying => new(false, 0.45f, 0.45f, 1.2f);

        // Legacy names kept for compatibility during transition.
        public static ObstacleColliderSpec DefaultHigh => SmallJumpable;
        public static ObstacleColliderSpec DefaultLow => LowSlideGate;
    }
}
