using System;
using System.Collections.Generic;

namespace PenguinRun.Game.Growth
{
    public static class CampBuildingCatalog
    {
        public const int MaxLevel = 3;

        private static readonly CampBuildingDefinition[] Definitions =
        {
            new(CampBuildingId.Bonfire, "星火篝火", "让风雪中的归途重新有光。", "开启营地重建与旅程来信",
                new[] { 50, 120, 240 }),
            new(CampBuildingId.Workshop, "雪橇工坊", "把沿途拾到的零件做成可靠装备。", "扩展局外强化与构筑提示",
                new[] { 80, 170, 320 }, CampBuildingId.Bonfire, 1),
            new(CampBuildingId.CompanionHut, "伙伴小屋", "给并肩旅行的朋友留一盏灯。", "开启羁绊互动与同行帮助",
                new[] { 90, 190, 350 }, CampBuildingId.Bonfire, 1),
            new(CampBuildingId.LibraryTent, "图书帐篷", "把路线、遗物和传说装订成册。", "显示未知发现线索与主题页",
                new[] { 120, 230, 420 }, CampBuildingId.CompanionHut, 2),
            new(CampBuildingId.Lookout, "云端瞭望台", "从高处寻找下一段最值得启程的故事。", "强化大厅目标推荐与挑战记录",
                new[] { 160, 300, 520 }, CampBuildingId.Workshop, 2),
        };

        public static IReadOnlyList<CampBuildingDefinition> All { get; } = Array.AsReadOnly(Definitions);

        public static CampBuildingDefinition For(CampBuildingId id)
        {
            foreach (var definition in Definitions)
                if (definition.Id == id) return definition;
            throw new ArgumentOutOfRangeException(nameof(id), id, null);
        }

        public static int CostForNextLevel(CampBuildingId id, int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= MaxLevel) return 0;
            return For(id).FishCosts[currentLevel];
        }
    }
}
