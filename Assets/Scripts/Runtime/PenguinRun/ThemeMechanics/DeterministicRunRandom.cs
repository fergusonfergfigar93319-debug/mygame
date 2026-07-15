namespace PenguinRun
{
    internal interface IRunRandom
    {
        uint NextUInt();
        float Next01();
        int Range(int minInclusive, int maxExclusive);
    }

    internal sealed class XorShiftRunRandom : IRunRandom
    {
        private const uint NonZeroSeed = 0x6D2B79F5u;
        private uint state;

        public XorShiftRunRandom(int seed)
        {
            state = seed == 0 ? NonZeroSeed : unchecked((uint)seed);
        }

        public uint NextUInt()
        {
            var x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x == 0 ? NonZeroSeed : x;
            return state;
        }

        public float Next01()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return maxExclusive <= minInclusive
                ? minInclusive
                : minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }
    }
}
