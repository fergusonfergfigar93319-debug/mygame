using System;

namespace PenguinRun
{
    [Flags]
    internal enum RunnerAvatarBuffFlags
    {
        None = 0,
        Dash = 1 << 0,
        Magnet = 1 << 1,
        Shield = 1 << 2,
        Invulnerable = 1 << 3,
        Glide = 1 << 4,
        ScoreBoost = 1 << 5,
        SlowMo = 1 << 6,
        DoubleFish = 1 << 7,
        IceMirror = 1 << 8,
        AuroraChain = 1 << 9,
        FogLantern = 1 << 10,
        TreantArmor = 1 << 11,
        CoralBounce = 1 << 12,
        ThunderFeather = 1 << 13,
        FishBomb = 1 << 14,
    }

    internal readonly struct RunnerAvatarState
    {
        public RunnerAvatarState(
            bool running,
            bool grounded,
            bool sliding,
            bool gameOver,
            float speed01,
            float laneLean,
            float verticalVelocity,
            RunnerAvatarBuffFlags buffs)
        {
            Running = running;
            Grounded = grounded;
            Sliding = sliding;
            GameOver = gameOver;
            Speed01 = speed01;
            LaneLean = laneLean;
            VerticalVelocity = verticalVelocity;
            Buffs = buffs;
        }

        public bool Running { get; }
        public bool Grounded { get; }
        public bool Sliding { get; }
        public bool GameOver { get; }
        public float Speed01 { get; }
        public float LaneLean { get; }
        public float VerticalVelocity { get; }
        public RunnerAvatarBuffFlags Buffs { get; }

        public bool Has(RunnerAvatarBuffFlags flag) => (Buffs & flag) != 0;
    }
}
