using System;
using System.Collections.Generic;

namespace PenguinRun.Game.Growth
{
    public enum CampBuildingId
    {
        Bonfire,
        Workshop,
        CompanionHut,
        LibraryTent,
        Lookout,
    }

    public enum CampBuildingVisibleState
    {
        Locked,
        GatheringMaterials,
        Affordable,
        Rebuilt,
    }

    public sealed class CampBuildingDefinition
    {
        public CampBuildingDefinition(CampBuildingId id, string title, string story, string unlockEffect,
            int[] fishCosts, CampBuildingId? prerequisite = null, int prerequisiteLevel = 0)
        {
            Id = id;
            Title = title ?? string.Empty;
            Story = story ?? string.Empty;
            UnlockEffect = unlockEffect ?? string.Empty;
            FishCosts = fishCosts ?? Array.Empty<int>();
            Prerequisite = prerequisite;
            PrerequisiteLevel = Math.Max(0, prerequisiteLevel);
        }

        public CampBuildingId Id { get; }
        public string Title { get; }
        public string Story { get; }
        public string UnlockEffect { get; }
        public IReadOnlyList<int> FishCosts { get; }
        public CampBuildingId? Prerequisite { get; }
        public int PrerequisiteLevel { get; }
    }

    public readonly struct CampBuildingSnapshot
    {
        public CampBuildingSnapshot(CampBuildingDefinition definition, int level, int nextCost,
            CampBuildingVisibleState visibleState)
        {
            Definition = definition;
            Level = Math.Max(0, level);
            NextCost = Math.Max(0, nextCost);
            VisibleState = visibleState;
        }

        public CampBuildingDefinition Definition { get; }
        public CampBuildingId Id => Definition.Id;
        public int Level { get; }
        public int NextCost { get; }
        public CampBuildingVisibleState VisibleState { get; }
        public bool IsMaxLevel => Level >= CampBuildingCatalog.MaxLevel;
    }

    public readonly struct CampRebuildSnapshot
    {
        public CampRebuildSnapshot(IReadOnlyList<CampBuildingSnapshot> nodes, int totalLevels,
            int affordableNodes)
        {
            Nodes = nodes ?? Array.AsReadOnly(Array.Empty<CampBuildingSnapshot>());
            TotalLevels = Math.Max(0, totalLevels);
            AffordableNodes = Math.Max(0, affordableNodes);
        }

        public IReadOnlyList<CampBuildingSnapshot> Nodes { get; }
        public int TotalLevels { get; }
        public int AffordableNodes { get; }
        public int MaxTotalLevels => CampBuildingCatalog.All.Count * CampBuildingCatalog.MaxLevel;
        public float Progress01 => MaxTotalLevels <= 0 ? 0f : Math.Min(1f, TotalLevels / (float)MaxTotalLevels);
    }
}
