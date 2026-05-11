using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun.Game.Shop
{
    public enum ShopCategory
    {
        DailyDeals,
        Boosters,
        Cosmetics,
        Bundles,
    }

    public enum ShopRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
    }

    /// <summary>
    /// 商店条目类型：决定 <see cref="ShopStore"/> 在购买后如何发放。
    /// </summary>
    public enum ShopItemKind
    {
        BoosterDoubleFish,   // 局内：下一局结算鱼干 ×2
        BoosterScoreBoost,   // 局内：下一局结算得分 ×1.5
        BoosterLuckyStart,   // 库存：占位"幸运起手"令牌（暂未消耗，留作将来扩展）
        FishPackSmall,       // 一次性：发放鱼干包（小）
        FishPackMedium,      // 一次性：发放鱼干包（中）
        CosmeticScarf,       // 装扮：解锁/装备围巾
        CosmeticHat,         // 装扮：解锁/装备帽子
        BundleStarter,       // 礼盒：启程（仅一次）
        BundlePolar,         // 礼盒：极地（仅一次）
        BundleAurora,        // 礼盒：极光（仅一次）
    }

    /// <summary>
    /// 商店条目静态定义。Param 字段对装扮表示其 cosmetic id；对鱼干包/礼盒则编码内容数量。
    /// </summary>
    public sealed class ShopItemDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public string IconGlyph;
        public ShopCategory Category;
        public ShopRarity Rarity;
        public ShopItemKind Kind;
        public string Param;
        public int BasePrice;
        public Color ThemeColor;
    }

    /// <summary>
    /// 装扮配色表：装扮 id ↔ 主体色 / 描边色 / 显示名。
    /// </summary>
    public static class CosmeticPalette
    {
        public sealed class ScarfColor
        {
            public string Id;
            public string Title;
            public Color Primary;
            public Color Secondary;
            public ShopRarity Rarity;
            public int Price;
        }

        public sealed class HatColor
        {
            public string Id;
            public string Title;
            public Color Crown;
            public Color Band;
            public ShopRarity Rarity;
            public int Price;
        }

        public static readonly List<ScarfColor> Scarves = new()
        {
            new() { Id = "scarf_cyan",   Title = "极地青",   Primary = new(0.15f, 0.75f, 1f),     Secondary = new(0.12f, 0.65f, 0.95f), Rarity = ShopRarity.Common,    Price = 0   },
            new() { Id = "scarf_pink",   Title = "樱花粉",   Primary = new(1f, 0.6f, 0.78f),      Secondary = new(0.95f, 0.5f, 0.7f),   Rarity = ShopRarity.Common,    Price = 180 },
            new() { Id = "scarf_red",    Title = "烈焰红",   Primary = new(0.95f, 0.35f, 0.3f),   Secondary = new(0.85f, 0.25f, 0.2f),  Rarity = ShopRarity.Rare,      Price = 280 },
            new() { Id = "scarf_violet", Title = "紫晶",     Primary = new(0.7f, 0.45f, 1f),      Secondary = new(0.55f, 0.32f, 0.9f),  Rarity = ShopRarity.Rare,      Price = 320 },
            new() { Id = "scarf_aurora", Title = "极光绿",   Primary = new(0.4f, 0.95f, 0.7f),    Secondary = new(0.25f, 0.85f, 0.55f), Rarity = ShopRarity.Epic,      Price = 460 },
            new() { Id = "scarf_gold",   Title = "鱼干金",   Primary = new(1f, 0.82f, 0.32f),     Secondary = new(0.85f, 0.65f, 0.18f), Rarity = ShopRarity.Epic,      Price = 540 },
            new() { Id = "scarf_pearl",  Title = "雪原珍珠", Primary = new(0.95f, 0.96f, 1f),     Secondary = new(0.78f, 0.85f, 0.95f), Rarity = ShopRarity.Legendary, Price = 820 },
        };

        public static readonly List<HatColor> Hats = new()
        {
            new() { Id = "hat_blue",   Title = "极夜蓝",   Crown = new(0.15f, 0.5f, 0.9f),    Band = new(0.08f, 0.35f, 0.75f),  Rarity = ShopRarity.Common, Price = 0   },
            new() { Id = "hat_pine",   Title = "雪松绿",   Crown = new(0.2f, 0.55f, 0.4f),    Band = new(0.12f, 0.4f, 0.28f),   Rarity = ShopRarity.Common, Price = 220 },
            new() { Id = "hat_amber",  Title = "暮色橙",   Crown = new(0.95f, 0.55f, 0.25f),  Band = new(0.78f, 0.4f, 0.15f),   Rarity = ShopRarity.Rare,   Price = 360 },
            new() { Id = "hat_violet", Title = "夜空紫",   Crown = new(0.45f, 0.32f, 0.78f),  Band = new(0.32f, 0.22f, 0.6f),   Rarity = ShopRarity.Rare,   Price = 380 },
            new() { Id = "hat_aurora", Title = "极光绒帽", Crown = new(0.4f, 0.95f, 0.85f),   Band = new(0.25f, 0.8f, 0.7f),    Rarity = ShopRarity.Epic,   Price = 580 },
        };

        public static ScarfColor FindScarf(string id)
        {
            foreach (var s in Scarves) if (s.Id == id) return s;
            return Scarves[0];
        }

        public static HatColor FindHat(string id)
        {
            foreach (var h in Hats) if (h.Id == id) return h;
            return Hats[0];
        }
    }

    /// <summary>
    /// 商店静态目录：所有可购入条目集中维护，UI 与 Store 都从这里读。
    /// </summary>
    public static class ShopCatalog
    {
        public static readonly List<ShopItemDefinition> All = BuildAll();

        public static IEnumerable<ShopItemDefinition> InCategory(ShopCategory cat)
        {
            foreach (var d in All)
                if (d.Category == cat) yield return d;
        }

        public static ShopItemDefinition Find(string id)
        {
            foreach (var d in All) if (d.Id == id) return d;
            return null;
        }

        public static string RarityLabel(ShopRarity r) =>
            r switch
            {
                ShopRarity.Common    => "常规",
                ShopRarity.Rare      => "稀有",
                ShopRarity.Epic      => "史诗",
                ShopRarity.Legendary => "传说",
                _ => "—",
            };

        public static Color RarityColor(ShopRarity r) =>
            r switch
            {
                ShopRarity.Common    => new(0.6f, 0.7f, 0.82f, 1f),
                ShopRarity.Rare      => new(0.4f, 0.85f, 1f, 1f),
                ShopRarity.Epic      => new(0.78f, 0.55f, 1f, 1f),
                ShopRarity.Legendary => new(1f, 0.78f, 0.32f, 1f),
                _ => Color.white,
            };

        private static List<ShopItemDefinition> BuildAll()
        {
            var list = new List<ShopItemDefinition>
            {
                // ── 道具：消耗券 ─────────────────────────────────────────────
                new()
                {
                    Id = "boost_double_fish",
                    Title = "双倍鱼干券",
                    Description = "下一局结算鱼干 ×2，可与营地「鱼干收益」叠加。",
                    IconGlyph = "\u2738",
                    Category = ShopCategory.Boosters,
                    Rarity = ShopRarity.Rare,
                    Kind = ShopItemKind.BoosterDoubleFish,
                    BasePrice = 220,
                    ThemeColor = new(1f, 0.82f, 0.35f, 1f),
                },
                new()
                {
                    Id = "boost_score_boost",
                    Title = "得分加成券",
                    Description = "下一局结算得分 ×1.5，冲新纪录利器。",
                    IconGlyph = "\u2605",
                    Category = ShopCategory.Boosters,
                    Rarity = ShopRarity.Rare,
                    Kind = ShopItemKind.BoosterScoreBoost,
                    BasePrice = 280,
                    ThemeColor = new(1f, 0.55f, 0.6f, 1f),
                },
                new()
                {
                    Id = "boost_lucky_start",
                    Title = "幸运起手令",
                    Description = "下一局生效：开局 8 秒额外 1 次幸运掉率（占位道具，后续版本启用）。",
                    IconGlyph = "\u2726",
                    Category = ShopCategory.Boosters,
                    Rarity = ShopRarity.Epic,
                    Kind = ShopItemKind.BoosterLuckyStart,
                    BasePrice = 460,
                    ThemeColor = new(0.55f, 0.85f, 1f, 1f),
                },
                new()
                {
                    Id = "pack_fish_small",
                    Title = "鱼干包 · 小",
                    Description = "立即获得 200 鱼干，节省一段攒鱼干的时光。",
                    IconGlyph = "\u2615",
                    Category = ShopCategory.Boosters,
                    Rarity = ShopRarity.Common,
                    Kind = ShopItemKind.FishPackSmall,
                    BasePrice = 0, // 仅在限时特惠出现，不在常驻列表中显示购买按钮
                    ThemeColor = new(0.95f, 0.78f, 0.45f, 1f),
                },
                new()
                {
                    Id = "pack_fish_medium",
                    Title = "鱼干包 · 中",
                    Description = "立即获得 600 鱼干。",
                    IconGlyph = "\u2615",
                    Category = ShopCategory.Boosters,
                    Rarity = ShopRarity.Rare,
                    Kind = ShopItemKind.FishPackMedium,
                    BasePrice = 0,
                    ThemeColor = new(0.95f, 0.78f, 0.45f, 1f),
                },
            };

            // ── 装扮：围巾色 / 帽子色 ─────────────────────────────────────
            foreach (var s in CosmeticPalette.Scarves)
            {
                list.Add(new ShopItemDefinition
                {
                    Id = s.Id,
                    Title = $"围巾 · {s.Title}",
                    Description = "解锁后可在装扮中切换，应用到大厅的企鹅吉祥物。",
                    IconGlyph = "\u2766",
                    Category = ShopCategory.Cosmetics,
                    Rarity = s.Rarity,
                    Kind = ShopItemKind.CosmeticScarf,
                    Param = s.Id,
                    BasePrice = s.Price,
                    ThemeColor = s.Primary,
                });
            }
            foreach (var h in CosmeticPalette.Hats)
            {
                list.Add(new ShopItemDefinition
                {
                    Id = h.Id,
                    Title = $"帽子 · {h.Title}",
                    Description = "解锁后即可在装扮中切换戴在企鹅头上。",
                    IconGlyph = "\u26C4",
                    Category = ShopCategory.Cosmetics,
                    Rarity = h.Rarity,
                    Kind = ShopItemKind.CosmeticHat,
                    Param = h.Id,
                    BasePrice = h.Price,
                    ThemeColor = h.Crown,
                });
            }

            // ── 礼盒：限购一次 ────────────────────────────────────────────
            list.Add(new ShopItemDefinition
            {
                Id = "bundle_starter",
                Title = "启程礼盒",
                Description = "200 鱼干 · 双倍鱼干券 ×1 · 得分加成券 ×1（一次性）。",
                IconGlyph = "\u2728",
                Category = ShopCategory.Bundles,
                Rarity = ShopRarity.Rare,
                Kind = ShopItemKind.BundleStarter,
                BasePrice = 380,
                ThemeColor = new(0.55f, 0.85f, 1f, 1f),
            });
            list.Add(new ShopItemDefinition
            {
                Id = "bundle_polar",
                Title = "极地大礼盒",
                Description = "600 鱼干 · 各类券 ×2 · 解锁极地青围巾（一次性）。",
                IconGlyph = "\u2746",
                Category = ShopCategory.Bundles,
                Rarity = ShopRarity.Epic,
                Kind = ShopItemKind.BundlePolar,
                BasePrice = 980,
                ThemeColor = new(0.78f, 0.55f, 1f, 1f),
            });
            list.Add(new ShopItemDefinition
            {
                Id = "bundle_aurora",
                Title = "极光豪华礼盒",
                Description = "1500 鱼干 · 全套消耗券 ×3 · 解锁雪原珍珠围巾（一次性）。",
                IconGlyph = "\u2604",
                Category = ShopCategory.Bundles,
                Rarity = ShopRarity.Legendary,
                Kind = ShopItemKind.BundleAurora,
                BasePrice = 1980,
                ThemeColor = new(1f, 0.78f, 0.32f, 1f),
            });

            // ── 鱼干包补回 BasePrice，便于在限时特惠中显示原价 ──────────
            foreach (var d in list)
            {
                if (d.Kind == ShopItemKind.FishPackSmall) d.BasePrice = 280;
                else if (d.Kind == ShopItemKind.FishPackMedium) d.BasePrice = 760;
            }

            return list;
        }
    }
}
