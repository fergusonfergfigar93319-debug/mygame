namespace PenguinRun
{
    internal enum RunFeedbackKind
    {
        PerfectDodge,
        ObstacleDamage,
        ObstacleDeath,
        BossWarning,
        BossHit,
        BossDeath,
        DefenseBlock,
        SegmentHazard,
        SegmentReward,
        /// <summary>擦身而过：未做跳/铲、仅靠车道位置以极小间距掠过障碍。</summary>
        NearMiss,
        /// <summary>手动冲刺起手（双击 / Shift）。</summary>
        DashStart,
    }
}
