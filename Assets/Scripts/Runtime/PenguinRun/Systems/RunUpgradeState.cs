using System.Collections.Generic;

namespace PenguinRun
{
    internal sealed class RunUpgradeState
    {
        internal const int MaxSelections = 6;
        internal const int MaxStacksPerUpgrade = 3;

        private readonly int seed;
        private readonly Dictionary<RunUpgradeId, int> stacks = new();
        private readonly List<RunUpgradeId> selectedIds = new(MaxSelections);
        private RunUpgradeOffer currentOffer;
        private int offerOrdinal;

        public RunUpgradeState(RunnerMapTheme theme, bool daily, int runCount)
        {
            unchecked
            {
                seed = ((int)theme + 11) * 92821
                    ^ (daily ? 68917 : 0)
                    ^ System.Math.Max(0, runCount) * 31337;
            }
        }

        public int SelectionCount => selectedIds.Count;

        public bool CanSelect => SelectionCount < MaxSelections;

        public IReadOnlyList<RunUpgradeId> SelectedIds => selectedIds.AsReadOnly();

        public float DashDurationMultiplier => 1f + EffectValue(RunUpgradeEffect.DashDuration);

        public float MagnetRadiusMultiplier => 1f + EffectValue(RunUpgradeEffect.MagnetRadius);

        public float PerfectDodgeScoreMultiplier => 1f + EffectValue(RunUpgradeEffect.PerfectDodgeScore);

        public float ShieldGraceSeconds => EffectValue(RunUpgradeEffect.ShieldGrace);

        public float PowerUpDurationMultiplier => 1f + EffectValue(RunUpgradeEffect.PowerUpDuration);

        public float LaneResponseMultiplier => 1f + EffectValue(RunUpgradeEffect.LaneResponse);

        public float RouteRewardMultiplier => 1f + EffectValue(RunUpgradeEffect.RouteReward);

        public float FishRewardMultiplier => 1f + EffectValue(RunUpgradeEffect.FishValue);

        public RunUpgradeOffer BuildOffer()
        {
            if (!CanSelect)
            {
                currentOffer = RunUpgradeOffer.Empty;
                return currentOffer;
            }

            if (currentOffer.Count == 3)
            {
                return currentOffer;
            }

            var candidates = new List<RunUpgradeDefinition>(RunUpgradeCatalog.All.Count);
            for (var i = 0; i < RunUpgradeCatalog.All.Count; i++)
            {
                var definition = RunUpgradeCatalog.All[i];
                if (GetStack(definition.Id) < System.Math.Min(MaxStacksPerUpgrade, definition.MaxStacks))
                {
                    candidates.Add(definition);
                }
            }

            if (candidates.Count < 3)
            {
                currentOffer = RunUpgradeOffer.Empty;
                return currentOffer;
            }

            var ordinal = offerOrdinal++;
            candidates.Sort((left, right) =>
                StableRank(left.Id, ordinal).CompareTo(StableRank(right.Id, ordinal)));
            currentOffer = new RunUpgradeOffer(candidates[0], candidates[1], candidates[2]);
            return currentOffer;
        }

        public bool Select(RunUpgradeId id)
        {
            if (!CanSelect || !currentOffer.Contains(id))
            {
                return false;
            }

            var definition = RunUpgradeCatalog.For(id);
            var current = GetStack(id);
            if (current >= System.Math.Min(MaxStacksPerUpgrade, definition.MaxStacks))
            {
                return false;
            }

            stacks[id] = current + 1;
            selectedIds.Add(id);
            currentOffer = RunUpgradeOffer.Empty;
            return true;
        }

        public int GetStack(RunUpgradeId id) => stacks.TryGetValue(id, out var count) ? count : 0;

        public void Reset()
        {
            stacks.Clear();
            selectedIds.Clear();
            currentOffer = RunUpgradeOffer.Empty;
            offerOrdinal = 0;
        }

        private float EffectValue(RunUpgradeEffect effect)
        {
            var value = 0f;
            foreach (var pair in stacks)
            {
                var definition = RunUpgradeCatalog.For(pair.Key);
                if (definition.Effect == effect)
                {
                    value += definition.AmountPerStack * pair.Value;
                }
            }
            return value;
        }

        private uint StableRank(RunUpgradeId id, int ordinal)
        {
            unchecked
            {
                var value = (uint)(seed ^ ((int)id + 1) * 1597334677 ^ (ordinal + 1) * 381201581);
                value ^= value >> 16;
                value *= 2246822519u;
                value ^= value >> 13;
                return value;
            }
        }
    }
}
