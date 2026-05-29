namespace PenguinRun
{
    /// <summary>
    /// 单次 Boss 招式的视觉节拍（不改变命中判定，仅驱动动画与 FX）。
    /// </summary>
    internal enum BossAttackPhase
    {
        None,
        Windup,
        Telegraph,
        Strike,
        Recovery,
    }
}
