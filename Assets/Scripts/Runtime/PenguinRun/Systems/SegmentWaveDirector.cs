using UnityEngine;

namespace PenguinRun
{
    internal sealed class SegmentWaveDirector
    {
        private RunnerSegmentDefinition[] pool;
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

        public void SetTheme(RunnerMapTheme theme)
        {
            pool = RunnerSegmentCatalog.GetPool(theme);
            PickNewSegment();
        }

        /// <summary>返回当前片段的调色板，供生成器在波次消费前查询（如开局种子障碍）。</summary>
        public SegmentObstaclePalette CurrentPalette => current.Palette;

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
