using UnityEngine;

namespace PenguinRun
{
    internal sealed class SegmentWaveDirector
    {
        private static readonly SegmentPatternKind[] OnboardingRotation =
        {
            SegmentPatternKind.LaneWeave,
            SegmentPatternKind.JumpArc,
            SegmentPatternKind.SlideTunnel,
            SegmentPatternKind.RewardRun,
        };

        private RunnerSegmentDefinition[] pool;
        private RunnerSegmentDefinition current;
        private int wavesLeftInSegment;
        private readonly bool isOnboarding;
        private int onboardingRotationIndex;

        public SegmentWaveDirector(RunnerMapTheme theme, bool isOnboarding = false)
        {
            this.isOnboarding = isOnboarding;
            pool = RunnerSegmentCatalog.GetPool(theme);
            PickNewSegment();
        }

        public void ResetForRun()
        {
            onboardingRotationIndex = 0;
            PickNewSegment();
        }

        public void SetTheme(RunnerMapTheme theme)
        {
            pool = RunnerSegmentCatalog.GetPool(theme);
            PickNewSegment();
        }

        /// <summary>返回当前片段的调色板，供生成器在波次消费前查询（如开局种子障碍）。</summary>
        public SegmentObstaclePalette CurrentPalette => current.Palette;

        public SegmentPatternKind CurrentPatternKind => current.PatternKind;

        public bool LastConsumeWasFirstWave { get; private set; }

        public SegmentSpawnModifiers ConsumeWave()
        {
            LastConsumeWasFirstWave = wavesLeftInSegment == current.WaveCount;
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
            if (pool == null || pool.Length == 0)
            {
                current = RunnerSegmentCatalog.Fallback;
                wavesLeftInSegment = current.WaveCount;
                return;
            }

            if (isOnboarding)
            {
                current = PickOnboardingSegment();
                wavesLeftInSegment = current.WaveCount;
                return;
            }

            current = pool[Random.Range(0, pool.Length)];
            wavesLeftInSegment = current.WaveCount;
        }

        private RunnerSegmentDefinition PickOnboardingSegment()
        {
            for (var attempt = 0; attempt < OnboardingRotation.Length; attempt++)
            {
                var kind = OnboardingRotation[onboardingRotationIndex % OnboardingRotation.Length];
                onboardingRotationIndex++;
                if (TryFindByKind(kind, out var match))
                    return match;
            }

            // Prefer any non-excluded pattern over hard noise if rotation entries are missing.
            for (var i = 0; i < pool.Length; i++)
            {
                if (!IsExcludedOnboardingPattern(pool[i].PatternKind))
                    return pool[i];
            }

            return RunnerSegmentCatalog.Fallback;
        }

        private bool TryFindByKind(SegmentPatternKind kind, out RunnerSegmentDefinition match)
        {
            for (var i = 0; i < pool.Length; i++)
            {
                if (pool[i].PatternKind != kind) continue;
                match = pool[i];
                return true;
            }

            match = default;
            return false;
        }

        internal static bool IsExcludedOnboardingPattern(SegmentPatternKind kind)
        {
            return kind is SegmentPatternKind.HazardRhythm
                or SegmentPatternKind.PowerUpTrial
                or SegmentPatternKind.SplitGate
                or SegmentPatternKind.EnemyAmbush;
        }
    }
}
