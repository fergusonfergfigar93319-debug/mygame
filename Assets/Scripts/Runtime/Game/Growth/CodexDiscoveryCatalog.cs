using System;
using System.Collections.Generic;

namespace PenguinRun.Game.Growth
{
    public enum CodexDiscoveryGroup { Route, Relic }

    public sealed class CodexDiscoveryDefinition
    {
        public CodexDiscoveryDefinition(string id, CodexDiscoveryGroup group, string theme,
            string title, string description, string unknownClue)
        {
            Id = id ?? string.Empty;
            Group = group;
            Theme = theme ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            UnknownClue = unknownClue ?? string.Empty;
        }
        public string Id { get; }
        public CodexDiscoveryGroup Group { get; }
        public string Theme { get; }
        public string Title { get; }
        public string Description { get; }
        public string UnknownClue { get; }
    }

    public static class CodexDiscoveryCatalog
    {
        private static readonly CodexDiscoveryDefinition[] Definitions =
        {
            Route("ice-fracture", "冰湖回声", "冰镜分岔", "裂冰会映出两条都真实的路。", "在冰湖路线事件中作出一次选择。"),
            Route("ice-cache", "冰湖回声", "冰封储藏", "晶核里封着迁徙者留下的鱼干。", "在冰湖寻找封冻的储藏点。"),
            Route("cedar-rescue", "雪松遗迹", "雪松呼救", "根廊深处的铃声属于迷路的伙伴。", "深入雪松遗迹的救援岔路。"),
            Route("cedar-exit", "雪松遗迹", "遗迹岔口", "石灯仍在指向被风雪掩埋的出口。", "在雪松遗迹寻找石灯。"),
            Route("aurora-trial", "极光原野", "极光磁暴", "蓝极与赤极会改变安全的方向。", "完成一次极光天气试炼。"),
            Route("aurora-fork", "极光原野", "光带交叉", "强光尽头藏着更冒险的落点。", "追逐极光原野的交叉光带。"),
            Route("mist-chase", "迷雾长堤", "雾中追影", "堤灯之外的影子并不都是真身。", "在迷雾长堤追入浓雾。"),
            Route("mist-rescue", "迷雾长堤", "迷雾铃声", "绳索和铃声能带回走散的朋友。", "循铃完成一次迷雾救援。"),
            Route("reef-cache", "珊瑚海湾", "珊瑚宝湾", "回流把远方的贝壳送到同一处。", "潜入珊瑚海湾的回流。"),
            Route("reef-trial", "珊瑚海湾", "潮汐试炼", "逆浪的节奏比速度更重要。", "踏过一次珊瑚逆浪。"),
            Route("sky-chase", "云端航线", "风暴追逐", "风眼会短暂打开一条云上近路。", "乘风追入云端风暴。"),
            Route("sky-exit", "云端航线", "浮岛出口", "断桥之后仍有能接住旅人的风。", "从浮岛断桥成功离开。"),
            Relic("relic_ice_fracture", "冰裂遗物", "一道不会融化的冰纹。", "击败带有冰裂特性的首领。"),
            Relic("relic_cedar_root", "雪松根徽", "年轮记录着遗迹守卫的誓言。", "击败带有根系守卫特性的首领。"),
            Relic("relic_aurora_polarity", "极光极芯", "微光会在掌心改变极性。", "击败带有极光极性特性的首领。"),
            Relic("relic_mist_decoy", "雾影铃", "只有真身靠近时它才会发声。", "识破迷雾首领的幻影。"),
            Relic("relic_coral_rebound", "回弹珊瑚", "潮水离开后仍保留着弹性。", "击败带有珊瑚回弹特性的首领。"),
            Relic("relic_storm_draft", "风暴羽片", "羽片指向风暴最安静的中心。", "击败带有风暴气流特性的首领。"),
        };

        public static IReadOnlyList<CodexDiscoveryDefinition> All { get; } = Array.AsReadOnly(Definitions);

        public static CodexDiscoveryDefinition For(string id)
        {
            foreach (var definition in Definitions)
                if (definition.Id == id) return definition;
            return null;
        }

        public static IEnumerable<CodexDiscoveryDefinition> ForGroup(CodexDiscoveryGroup group)
        {
            foreach (var definition in Definitions)
                if (definition.Group == group) yield return definition;
        }

        private static CodexDiscoveryDefinition Route(string id, string theme, string title,
            string description, string clue) =>
            new(id, CodexDiscoveryGroup.Route, theme, title, description, clue);

        private static CodexDiscoveryDefinition Relic(string id, string title, string description, string clue) =>
            new(id, CodexDiscoveryGroup.Relic, "首领遗物", title, description, clue);
    }
}
