using System;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Game.Growth
{
    public static class CampRebuildStore
    {
        public static int GetLevel(CampBuildingId id) =>
            Mathf.Clamp(PlayerSave.ReadGrowthInt(Key(id), 0), 0, CampBuildingCatalog.MaxLevel);

        public static CampBuildingSnapshot Snapshot(CampBuildingId id)
        {
            var definition = CampBuildingCatalog.For(id);
            var level = GetLevel(id);
            var cost = CampBuildingCatalog.CostForNextLevel(id, level);
            return new CampBuildingSnapshot(definition, level, cost, ResolveVisibleState(definition, level, cost));
        }

        public static CampRebuildSnapshot Snapshot()
        {
            var nodes = new CampBuildingSnapshot[CampBuildingCatalog.All.Count];
            var totalLevels = 0;
            var affordable = 0;
            for (var i = 0; i < CampBuildingCatalog.All.Count; i++)
            {
                nodes[i] = Snapshot(CampBuildingCatalog.All[i].Id);
                totalLevels += nodes[i].Level;
                if (nodes[i].VisibleState == CampBuildingVisibleState.Affordable) affordable++;
            }
            return new CampRebuildSnapshot(Array.AsReadOnly(nodes), totalLevels, affordable);
        }

        public static bool CanPurchase(CampBuildingId id)
        {
            var snapshot = Snapshot(id);
            return snapshot.VisibleState == CampBuildingVisibleState.Affordable && snapshot.NextCost > 0;
        }

        public static bool TryPurchase(CampBuildingId id)
        {
            var before = Snapshot(id);
            if (before.VisibleState != CampBuildingVisibleState.Affordable || before.NextCost <= 0)
                return false;
            if (!PlayerSave.TrySpendFishSnacks(before.NextCost)) return false;

            PlayerSave.WriteGrowthInt(Key(id), before.Level + 1);
            PlayerSave.AddCampInvested(before.NextCost);
            PlayerSave.VisitedCamp = true;
            PlayerSave.Flush();
            return true;
        }

        public static int CountAffordableNodes() => Snapshot().AffordableNodes;

        public static CampBuildingSnapshot? NextAffordableNode()
        {
            foreach (var definition in CampBuildingCatalog.All)
            {
                var node = Snapshot(definition.Id);
                if (node.VisibleState == CampBuildingVisibleState.Affordable) return node;
            }
            return null;
        }

        private static CampBuildingVisibleState ResolveVisibleState(CampBuildingDefinition definition,
            int level, int nextCost)
        {
            if (!PrerequisiteMet(definition)) return CampBuildingVisibleState.Locked;
            if (level >= CampBuildingCatalog.MaxLevel) return CampBuildingVisibleState.Rebuilt;
            if (nextCost > 0 && PlayerSave.TotalFishSnacks >= nextCost)
                return CampBuildingVisibleState.Affordable;
            return level > 0 ? CampBuildingVisibleState.Rebuilt : CampBuildingVisibleState.GatheringMaterials;
        }

        private static bool PrerequisiteMet(CampBuildingDefinition definition)
        {
            if (!definition.Prerequisite.HasValue) return true;
            return GetLevel(definition.Prerequisite.Value) >= definition.PrerequisiteLevel;
        }

        private static string Key(CampBuildingId id) => "camp_" + id.ToString().ToLowerInvariant() + "_level";
    }
}
