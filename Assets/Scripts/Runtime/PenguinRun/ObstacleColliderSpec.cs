namespace PenguinRun
{
    /// <summary>
    /// Axis-aligned hit extents in world X/Z around obstacle transform position (player uses similar thresholds).
    /// </summary>
    public readonly struct ObstacleColliderSpec
    {
        public ObstacleColliderSpec(bool isLow, float halfDx, float halfDz)
        {
            IsLow = isLow;
            HalfDx = halfDx;
            HalfDz = halfDz;
        }

        public bool IsLow { get; }
        public float HalfDx { get; }
        public float HalfDz { get; }

        // Keep default hitboxes close to visible mesh silhouette to avoid "air hits".
        public static ObstacleColliderSpec DefaultHigh => new(false, 0.62f, 0.62f);

        public static ObstacleColliderSpec DefaultLow => new(true, 0.74f, 0.68f);
    }
}
