using UnityEngine;

namespace PenguinRun
{
    internal sealed class MapMechanicDirector
    {
        private IRunRandom random;
        private readonly ThemeMechanicMode mode;
        private readonly int seed;
        private readonly bool suppressed;

        private int beatsSinceMechanic = 99;
        private int scheduledCount;
        private bool signatureScheduled;

        public MapMechanicDirector(ThemeMechanicMode mode, int seed, bool suppressed = false)
        {
            this.mode = mode;
            this.seed = seed;
            this.suppressed = suppressed;
            random = new XorShiftRunRandom(seed);
        }

        public float RewardDebt { get; private set; }
        public float PressureDebt { get; private set; }

        public void Reset()
        {
            beatsSinceMechanic = 99;
            scheduledCount = 0;
            signatureScheduled = false;
            RewardDebt = 0f;
            PressureDebt = 0f;
            random = new XorShiftRunRandom(seed);
            ThemeMechanicCatalog.ResetWarningState();
        }

        public void RecordBeat(bool dangerous, bool rewarding)
        {
            beatsSinceMechanic++;
            RewardDebt = Mathf.Clamp(
                RewardDebt + (dangerous ? 0.34f : -0.12f) - (rewarding ? 0.55f : 0f),
                0f,
                1.5f);
            PressureDebt = Mathf.Clamp(
                PressureDebt + (!dangerous && !rewarding ? 0.18f : -0.28f),
                0f,
                1.5f);
        }

        public bool TrySchedule(in ThemeMechanicContext context, out ThemeMechanicRequest request)
        {
            request = ThemeMechanicRequest.None;
            if (suppressed) return false;

            var policy = ThemeMechanicCatalog.PolicyFor(mode);
            if (context.Mode != mode
                || context.BossActive
                || context.RouteEventActive
                || context.ThemeTransition
                || context.Distance < policy.FirstDistance
                || scheduledCount >= policy.MaxPerRun
                || beatsSinceMechanic <= policy.CooldownBeats
                || context.Lives <= 0)
            {
                return false;
            }

            var recentHitAdjustment = context.SecondsSinceHit < 8f ? -0.18f : 0f;
            var chance = Mathf.Clamp01(
                policy.BaseChance
                + PressureDebt * 0.16f
                - RewardDebt * 0.12f
                + recentHitAdjustment);
            if (random.Next01() >= chance)
            {
                return false;
            }

            if (!ThemeMechanicCatalog.TryForTheme(context.Theme, out var definition))
            {
                return false;
            }

            var firstStoryMechanic = mode == ThemeMechanicMode.Story && scheduledCount == 0;
            var signature = !firstStoryMechanic
                && policy.AllowSignature
                && !signatureScheduled
                && context.Lives >= 2
                && context.SecondsSinceHit >= 8f
                && context.Distance >= 650f
                && random.Next01() < 0.12f;
            var intensity = signature
                ? ThemeMechanicIntensity.Signature
                : firstStoryMechanic
                    ? ThemeMechanicIntensity.Low
                    : context.SecondsSinceHit < 8f
                        ? ThemeMechanicIntensity.Low
                        : context.Difficulty01 > 0.72f || PressureDebt > 0.9f
                            ? ThemeMechanicIntensity.High
                            : ThemeMechanicIntensity.Medium;

            request = new ThemeMechanicRequest(true, definition, intensity, signature);
            signatureScheduled |= signature;
            scheduledCount++;
            beatsSinceMechanic = 0;
            RewardDebt = Mathf.Clamp(RewardDebt + (signature ? 0.6f : 0.35f), 0f, 1.5f);
            PressureDebt = 0f;
            return true;
        }
    }
}
