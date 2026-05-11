using UnityEngine;

namespace PenguinRun
{
    internal sealed class SegmentWaveDirector
    {
        private readonly RunnerSegmentDefinition[] pool;
        private RunnerSegmentDefinition current;
        private int wavesLeftInSegment;

        public SegmentWaveDirector(RunnerMapTheme theme)
        {
            pool = RunnerSegmentCatalog.GetPool(theme);
            PickNewSegment();
        }

        public void ResetForRun()
        {
            PickNewSegment();
        }

        public SegmentSpawnModifiers ConsumeWave()
        {
            var modifiers = SegmentSpawnModifiers.FromDefinition(current);
            wavesLeftInSegment--;
            if (wavesLeftInSegment <= 0)
            {
                PickNewSegment();
            }

            return modifiers;
        }

        private void PickNewSegment()
        {
            current = pool[Random.Range(0, pool.Length)];
            wavesLeftInSegment = current.WaveCount;
        }
    }
}
