using UnityEngine;

namespace PenguinRun
{
    internal enum RunUpgradeCategory
    {
        Movement,
        Collection,
        Defense,
        Companion,
    }

    public enum RunUpgradeId
    {
        LongDash,
        QuickSteps,
        FeatherJump,
        WideMagnet,
        FishSatchel,
        LastingSupplies,
        ShieldPadding,
        IceGuard,
        RescueWhistle,
        DodgeCheers,
        TrailScout,
        BossSignal,
    }

    internal enum RunUpgradeEffect
    {
        DashDuration,
        LaneResponse,
        AirControl,
        MagnetRadius,
        FishValue,
        PowerUpDuration,
        ShieldGrace,
        HitInvulnerability,
        CompanionFind,
        PerfectDodgeScore,
        RouteReward,
        BossCounterScore,
    }

    internal readonly struct RunUpgradeDefinition
    {
        public readonly RunUpgradeId Id;
        public readonly RunUpgradeCategory Category;
        public readonly string Title;
        public readonly string Description;
        public readonly RunUpgradeEffect Effect;
        public readonly float AmountPerStack;
        public readonly int MaxStacks;

        public RunUpgradeDefinition(
            RunUpgradeId id,
            RunUpgradeCategory category,
            string title,
            string description,
            RunUpgradeEffect effect,
            float amountPerStack,
            int maxStacks = 3)
        {
            Id = id;
            Category = category;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Effect = effect;
            AmountPerStack = Mathf.Max(0f, amountPerStack);
            MaxStacks = Mathf.Clamp(maxStacks, 1, 3);
        }
    }

    internal readonly struct RunUpgradeOffer
    {
        public readonly RunUpgradeDefinition First;
        public readonly RunUpgradeDefinition Second;
        public readonly RunUpgradeDefinition Third;
        public readonly int Count;

        public RunUpgradeOffer(
            RunUpgradeDefinition first,
            RunUpgradeDefinition second,
            RunUpgradeDefinition third)
        {
            First = first;
            Second = second;
            Third = third;
            Count = 3;
        }

        public RunUpgradeDefinition this[int index] => index switch
        {
            0 when Count > 0 => First,
            1 when Count > 1 => Second,
            2 when Count > 2 => Third,
            _ => default,
        };

        public bool Contains(RunUpgradeId id) =>
            Count == 3 && (First.Id == id || Second.Id == id || Third.Id == id);

        public static RunUpgradeOffer Empty => default;
    }
}
