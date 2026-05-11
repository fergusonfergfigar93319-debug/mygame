using System.Collections.Generic;
using PenguinRun.Game.Save;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 营地强化面板：六个升级条目，分类 Tab + 综合总览 + 效果预览。
    /// 重构后采用现代化卡片设计：分类筛选、等级圆点、当前/下一级效果对比、推荐标签。
    /// </summary>
    public sealed class CampPanel : MonoBehaviour
    {
        private System.Action<MainMenuBootstrap.PanelEvent, string> dispatch;
        private RectTransform contentRoot;
        private RectTransform overviewRoot;
        private Text overviewLevelsText;
        private Text overviewMaxedText;
        private Text overviewBoostText;
        private Image overviewBarFill;

        private CategoryFilter activeFilter = CategoryFilter.All;
        private readonly List<(Button btn, Image bg, Text text, CategoryFilter filter)> tabs = new();

        private const float OverviewHeight = 130f;
        private const float TabsHeight = 72f;
        private const float OverviewGap = 14f;
        private const float TabsGap = 12f;

        // 各 kind 主题色，与图标搭配
        private static readonly Dictionary<CampUpgradeKind, Color> KindColors = new()
        {
            { CampUpgradeKind.Dash,       new Color(1f, 0.55f, 0.25f, 1f) },
            { CampUpgradeKind.Tuan,       new Color(0.55f, 0.85f, 1f, 1f) },
            { CampUpgradeKind.Polar,      new Color(0.4f, 0.95f, 0.7f, 1f) },
            { CampUpgradeKind.Magnet,     new Color(0.85f, 0.55f, 1f, 1f) },
            { CampUpgradeKind.FishGain,   new Color(1f, 0.82f, 0.35f, 1f) },
            { CampUpgradeKind.ScoreBonus, new Color(1f, 0.45f, 0.55f, 1f) },
        };

        private static readonly Dictionary<CampUpgradeKind, string> KindIcons = new()
        {
            { CampUpgradeKind.Dash,       "\u279E" },
            { CampUpgradeKind.Tuan,       "\u2665" },
            { CampUpgradeKind.Polar,      "\u2745" },
            { CampUpgradeKind.Magnet,     "\u269B" },
            { CampUpgradeKind.FishGain,   "\u2615" },
            { CampUpgradeKind.ScoreBonus, "\u2605" },
        };

        // 推荐购买的 kind 顺序（用于新手提示）
        private static readonly CampUpgradeKind[] RecommendedOrder =
        {
            CampUpgradeKind.Dash,
            CampUpgradeKind.Magnet,
            CampUpgradeKind.FishGain,
        };

        public enum CategoryFilter
        {
            All,
            Action,
            Survival,
            Reward,
        }

        public static GameObject Build(Transform parent, System.Action<MainMenuBootstrap.PanelEvent, string> dispatch)
        {
            var rootRt = UiBuilder.CreateRect(
                "CampPanel", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                UiBuilder.MenuPanelSolidBg);
            var script = rootRt.gameObject.AddComponent<CampPanel>();
            script.dispatch = dispatch;
            script.BuildLayout(rootRt);
            script.Refresh();
            return rootRt.gameObject;
        }

        private void BuildLayout(RectTransform parent)
        {
            PanelHeader.Create(parent, "营地强化", "升级局外能力，让每一局更高效",
                () => dispatch(MainMenuBootstrap.PanelEvent.Close, null));

            BuildOverview(parent);
            BuildCategoryTabs(parent);

            var topInset = UiBuilder.PanelHeaderHeightPixels + OverviewGap + OverviewHeight + TabsGap + TabsHeight + 12f;
            UiBuilder.CreatePanelScrollList(parent, out var content, topInset: topInset);
            contentRoot = content;
        }

        private void BuildOverview(RectTransform parent)
        {
            var top = UiBuilder.PanelHeaderHeightPixels + OverviewGap;
            overviewRoot = UiBuilder.CreateRect("OverviewBanner", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, OverviewHeight), new Vector2(0f, -top),
                UiBuilder.Surface1, rounded: true);
            overviewRoot.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(overviewRoot.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            UiBuilder.AddShadow(overviewRoot.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -3f));

            // 左侧重音条
            var accent = UiBuilder.CreateRect("Accent", overviewRoot,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                UiBuilder.AccentCyan, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;
            accent.offsetMin = new Vector2(0f, 14f);
            accent.offsetMax = new Vector2(5f, -14f);

            // 一行三个统计 chip：总等级 / 满级数 / 综合加成
            BuildStatChip(overviewRoot, "总等级", "0/0", new Color(0.4f, 0.85f, 1f, 1f),
                new Vector2(0.02f, 0.5f), out overviewLevelsText);
            BuildStatChip(overviewRoot, "已满级", "0", new Color(0.4f, 0.95f, 0.7f, 1f),
                new Vector2(0.34f, 0.5f), out overviewMaxedText);
            BuildStatChip(overviewRoot, "综合加成", "+0%", new Color(1f, 0.82f, 0.35f, 1f),
                new Vector2(0.66f, 0.5f), out overviewBoostText);

            // 底部进度细条：总体强化进度（总等级/最大总等级）
            var trackRt = UiBuilder.CreateRect("ProgressTrack", overviewRoot,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 8f), new Vector2(0f, 14f),
                new Color(0.05f, 0.1f, 0.18f, 0.95f), rounded: true);
            trackRt.offsetMin = new Vector2(20f, trackRt.offsetMin.y);
            trackRt.offsetMax = new Vector2(-20f, trackRt.offsetMax.y);
            UiBuilder.AddOutline(trackRt.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            overviewBarFill = UiBuilder.CreateRect("Fill", trackRt,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero,
                UiBuilder.AccentCyan, rounded: true).GetComponent<Image>();
            overviewBarFill.raycastTarget = false;
        }

        private void BuildStatChip(RectTransform parent, string title, string initialValue,
            Color valueColor, Vector2 anchor, out Text valueRef)
        {
            var chip = UiBuilder.CreateRect("Stat_" + title, parent,
                anchor, new Vector2(anchor.x + 0.32f, anchor.y),
                Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0f));
            chip.pivot = new Vector2(0f, 0.5f);
            chip.sizeDelta = new Vector2(0f, 70f);
            chip.offsetMin = new Vector2(20f, -22f);
            chip.offsetMax = new Vector2(-20f, 48f);

            var label = UiBuilder.CreateText("Label", chip, title,
                18, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0f, 1f);
            lrt.sizeDelta = new Vector2(0f, 22f);
            lrt.anchoredPosition = new Vector2(0f, 0f);

            valueRef = UiBuilder.CreateText("Value", chip, initialValue,
                30, FontStyle.Bold, TextAnchor.UpperLeft, valueColor);
            var vrt = (RectTransform)valueRef.transform;
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0f, 0.5f);
            vrt.sizeDelta = new Vector2(0f, 36f);
            vrt.anchoredPosition = new Vector2(0f, -8f);
        }

        private void BuildCategoryTabs(RectTransform parent)
        {
            var top = UiBuilder.PanelHeaderHeightPixels + OverviewGap + OverviewHeight + TabsGap;
            var tabsContainer = UiBuilder.CreateRect("CampTabs", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, TabsHeight), new Vector2(0f, -top),
                new Color(0.05f, 0.11f, 0.2f, 0.92f), rounded: true);
            tabsContainer.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(tabsContainer.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var hlg = tabsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            tabs.Clear();
            AddTabButton(tabsContainer, "全部", CategoryFilter.All);
            AddTabButton(tabsContainer, "跑酷", CategoryFilter.Action);
            AddTabButton(tabsContainer, "防守", CategoryFilter.Survival);
            AddTabButton(tabsContainer, "收益", CategoryFilter.Reward);
            ApplyTabStyle();
        }

        private void AddTabButton(RectTransform parent, string label, CategoryFilter f)
        {
            var btn = UiBuilder.CreateButton("Tab_" + f, parent.transform, label,
                Color.clear, UiBuilder.TextSecondary, 22, rounded: true);
            var img = btn.GetComponent<Image>();
            var text = btn.GetComponentInChildren<Text>();
            tabs.Add((btn, img, text, f));
            btn.onClick.AddListener(() =>
            {
                activeFilter = f;
                ApplyTabStyle();
                Refresh();
            });
        }

        private void ApplyTabStyle()
        {
            foreach (var (btn, img, text, filter) in tabs)
            {
                if (btn == null) continue;
                var sel = filter == activeFilter;
                img.color = sel ? UiBuilder.AccentCyan : new Color(0.08f, 0.16f, 0.28f, 0.85f);
                if (text != null)
                {
                    text.color = sel ? new Color(0.04f, 0.08f, 0.14f, 1f) : UiBuilder.TextSecondary;
                    text.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
                }
                var outline = btn.gameObject.GetComponent<Outline>();
                if (sel)
                {
                    if (outline == null)
                        UiBuilder.AddOutline(btn.gameObject, new Color(0.3f, 1f, 1f, 0.55f), new Vector2(1.5f, -1.5f));
                    else
                        outline.effectColor = new Color(0.3f, 1f, 1f, 0.55f);
                }
                else
                {
                    if (outline != null) Object.Destroy(outline);
                }
            }
        }

        private void Refresh()
        {
            // 顶栏鱼干刷新
            var fishText = transform.Find("Header/CurrencyPill/Value")?.GetComponent<Text>();
            if (fishText != null) fishText.text = $"{PlayerSave.TotalFishSnacks}";

            // 总览条
            var totalLevels = CampUpgrades.TotalLevelsCount();
            var maxTotal = CampUpgrades.MaxTotalLevels;
            if (overviewLevelsText != null) overviewLevelsText.text = $"{totalLevels}/{maxTotal}";
            if (overviewMaxedText != null) overviewMaxedText.text = $"{CampUpgrades.MaxedKindCount()}/{CampUpgrades.AllKinds.Length}";
            if (overviewBoostText != null)
            {
                var fish = (CampUpgrades.GetFishGainMultiplier() - 1f) * 100f;
                var sc = (CampUpgrades.GetScoreMultiplier() - 1f) * 100f;
                overviewBoostText.text = $"鱼+{fish:0}%  分+{sc:0}%";
            }
            if (overviewBarFill != null)
            {
                var ratio = maxTotal > 0 ? totalLevels / (float)maxTotal : 0f;
                var rt = (RectTransform)overviewBarFill.transform;
                rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            }

            // 列表区域：按 filter 过滤
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }

            // 顺序：先 Action / Survival / Reward；同类内按 enum 顺序保留
            CampUpgradeCategory[] order = { CampUpgradeCategory.Action, CampUpgradeCategory.Survival, CampUpgradeCategory.Reward };
            var anyShown = false;
            foreach (var cat in order)
            {
                if (activeFilter != CategoryFilter.All && !MatchesFilter(cat)) continue;

                // 如果是「全部」过滤，给每个类别加一个分组标题
                var shownAny = false;
                foreach (var k in CampUpgrades.AllKinds)
                {
                    if (CampUpgrades.GetCategory(k) != cat) continue;
                    if (!shownAny && activeFilter == CategoryFilter.All)
                    {
                        BuildSectionHeader(cat);
                        shownAny = true;
                    }
                    BuildRow(k);
                    anyShown = true;
                }
            }

            if (!anyShown)
            {
                // 理论不会触发，留作兜底
                BuildEmptyRow();
            }

            UiBuilder.RebuildScrollContent(contentRoot);
        }

        private bool MatchesFilter(CampUpgradeCategory cat) =>
            activeFilter switch
            {
                CategoryFilter.Action   => cat == CampUpgradeCategory.Action,
                CategoryFilter.Survival => cat == CampUpgradeCategory.Survival,
                CategoryFilter.Reward   => cat == CampUpgradeCategory.Reward,
                _ => true,
            };

        private void BuildSectionHeader(CampUpgradeCategory cat)
        {
            var header = UiBuilder.CreateScrollListRow("Section_" + cat, contentRoot, 46f, null);
            var color = CampUpgrades.GetCategoryColor(cat);

            var bar = UiBuilder.CreateRect("Bar", header,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 22f), new Vector2(20f, 0f),
                color, rounded: true);
            bar.GetComponent<Image>().raycastTarget = false;

            var label = UiBuilder.CreateText("Label", header,
                $"{CampUpgrades.GetCategoryLabel(cat)}类强化",
                22, FontStyle.Bold, TextAnchor.MiddleLeft, color);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = new Vector2(36f, 0f);
            lrt.offsetMax = new Vector2(-20f, 0f);
        }

        private void BuildEmptyRow()
        {
            var row = UiBuilder.CreateScrollListRow("Empty", contentRoot, 200f,
                new Color(0.08f, 0.18f, 0.32f, 0.6f));
            UiBuilder.CreateText("Title", row.transform, "暂无可展示的强化",
                26, FontStyle.Bold, TextAnchor.MiddleCenter, UiBuilder.TextSecondary);
        }

        private void BuildRow(CampUpgradeKind kind)
        {
            var level = CampUpgrades.GetLevel(kind);
            var maxLevel = PlayerSave.CampMaxLevel;
            var cost = CampUpgrades.GetUpgradeCost(kind, level);
            var maxed = !cost.HasValue;
            var kindColor = KindColors.GetValueOrDefault(kind, UiBuilder.AccentCyan);
            var icon = KindIcons.GetValueOrDefault(kind, "\u2605");
            var category = CampUpgrades.GetCategory(kind);

            var row = UiBuilder.CreateScrollListRow("Row_" + kind, contentRoot, 268f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject,
                maxed ? new Color(1f, 0.85f, 0.35f, 0.55f) : UiBuilder.BorderSubtle,
                new Vector2(1f, -1f));

            // 左侧重音条
            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                kindColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 图标徽章
            var iconBadge = UiBuilder.CreateRect("IconBadge", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(72f, 72f), new Vector2(54f, -56f),
                new Color(kindColor.r, kindColor.g, kindColor.b, 0.18f), circle: true);
            UiBuilder.AddOutline(iconBadge.gameObject,
                new Color(kindColor.r, kindColor.g, kindColor.b, 0.6f), new Vector2(1.5f, -1.5f));
            UiBuilder.CreateText("Icon", iconBadge.transform, icon, 36, FontStyle.Bold,
                TextAnchor.MiddleCenter, kindColor);

            // 标题
            var title = UiBuilder.CreateText("Title", row,
                CampUpgrades.GetTitle(kind),
                30, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.65f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 40f);
            trt.anchoredPosition = new Vector2(108f, -22f);

            // 等级 chip + 分类 chip 行
            var lvChip = UiBuilder.CreateRect("LevelChip", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(108f, 32f), new Vector2(108f, -68f),
                new Color(kindColor.r, kindColor.g, kindColor.b, 0.22f), rounded: true);
            UiBuilder.AddOutline(lvChip.gameObject,
                new Color(kindColor.r, kindColor.g, kindColor.b, 0.5f), new Vector2(1f, -1f));
            UiBuilder.CreateText("LvText", lvChip.transform,
                $"Lv {level}/{maxLevel}",
                18, FontStyle.Bold, TextAnchor.MiddleCenter, kindColor);

            var catColor = CampUpgrades.GetCategoryColor(category);
            var catChip = UiBuilder.CreateRect("CatChip", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(82f, 32f), new Vector2(220f, -68f),
                new Color(catColor.r, catColor.g, catColor.b, 0.18f), rounded: true);
            UiBuilder.AddOutline(catChip.gameObject,
                new Color(catColor.r, catColor.g, catColor.b, 0.45f), new Vector2(1f, -1f));
            UiBuilder.CreateText("CatText", catChip.transform,
                CampUpgrades.GetCategoryLabel(category),
                17, FontStyle.Bold, TextAnchor.MiddleCenter, catColor);

            // 推荐 chip（仅在等级 0 且属于推荐列表时出现）
            if (level == 0 && System.Array.IndexOf(RecommendedOrder, kind) >= 0)
            {
                var recChip = UiBuilder.CreateRect("RecChip", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(88f, 32f), new Vector2(310f, -68f),
                    new Color(1f, 0.85f, 0.35f, 0.22f), rounded: true);
                UiBuilder.AddOutline(recChip.gameObject,
                    new Color(1f, 0.85f, 0.35f, 0.55f), new Vector2(1f, -1f));
                UiBuilder.CreateText("RecText", recChip.transform, "推荐",
                    17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f, 1f));
            }

            // 描述
            var desc = UiBuilder.CreateText("Desc", row, CampUpgrades.GetDescription(kind),
                20, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.65f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 56f);
            drt.anchoredPosition = new Vector2(108f, -106f);

            // 效果预览：当前 → 下一级
            var previewBg = UiBuilder.CreateRect("Preview", row,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 50f), new Vector2(108f, 70f),
                new Color(0.05f, 0.1f, 0.18f, 0.6f), rounded: true);
            previewBg.anchorMax = new Vector2(0.65f, 0f);
            previewBg.offsetMax = new Vector2(-12f, previewBg.offsetMax.y);
            UiBuilder.AddOutline(previewBg.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var currentText = UiBuilder.CreateText("Current", previewBg,
                $"当前 · {CampUpgrades.FormatEffectAt(kind, level)}",
                18, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.TextSecondary);
            var crt = (RectTransform)currentText.transform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(14f, 0f);
            crt.offsetMax = new Vector2(-6f, 0f);

            var arrow = UiBuilder.CreateText("Arrow", previewBg, "\u2192",
                22, FontStyle.Bold, TextAnchor.MiddleCenter,
                maxed ? UiBuilder.WarmGold : kindColor);
            var art = (RectTransform)arrow.transform;
            art.anchorMin = new Vector2(0.46f, 0f);
            art.anchorMax = new Vector2(0.54f, 1f);
            art.sizeDelta = Vector2.zero;
            art.offsetMin = Vector2.zero;
            art.offsetMax = Vector2.zero;

            var nextValue = maxed ? "已满级" : CampUpgrades.FormatEffectAt(kind, level + 1);
            var nextText = UiBuilder.CreateText("Next", previewBg,
                $"下一级 · {nextValue}",
                18, FontStyle.Bold, TextAnchor.MiddleLeft,
                maxed ? UiBuilder.WarmGold : kindColor);
            var nrt = (RectTransform)nextText.transform;
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.offsetMin = new Vector2(8f, 0f);
            nrt.offsetMax = new Vector2(-14f, 0f);

            // 等级圆点（5 个），位于卡片左下
            BuildLevelDots(row, level, maxLevel, kindColor);

            // 按钮区
            var btnLabel = maxed ? "已满级" : $"消耗 {cost.Value} 鱼干";
            var enabled = !maxed && PlayerSave.TotalFishSnacks >= (cost ?? int.MaxValue);

            Color btnBg, btnFg;
            if (maxed)
            {
                btnBg = new Color(0.12f, 0.22f, 0.32f, 0.85f);
                btnFg = new Color(1f, 1f, 1f, 0.5f);
            }
            else if (enabled)
            {
                btnBg = kindColor;
                btnFg = new Color(0.05f, 0.08f, 0.12f, 1f);
            }
            else
            {
                btnBg = new Color(0.18f, 0.28f, 0.4f, 0.9f);
                btnFg = new Color(1f, 1f, 1f, 0.55f);
            }

            var btn = UiBuilder.CreateButton("Buy", row, btnLabel, btnBg, btnFg, 22, rounded: true);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.sizeDelta = new Vector2(220f, 84f);
            brt.anchoredPosition = new Vector2(-24f, 12f);

            if (enabled)
            {
                UiBuilder.AddOutline(btn.gameObject,
                    new Color(1f, 1f, 1f, 0.4f), new Vector2(1.5f, -1.5f));
                UiBuilder.AddShadow(btn.gameObject,
                    new Color(kindColor.r * 0.5f, kindColor.g * 0.5f, kindColor.b * 0.5f, 0.5f),
                    new Vector2(0f, -4f));
            }
            else if (!maxed)
            {
                UiBuilder.AddOutline(btn.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            }

            // 副标签：差距提示（鱼干不足时显示还差多少）
            var btnSub = UiBuilder.CreateText("BuyHint", row,
                maxed ? "" : (enabled ? "" : $"还差 {Mathf.Max(0, cost.Value - PlayerSave.TotalFishSnacks)} 鱼干"),
                17, FontStyle.Normal, TextAnchor.MiddleRight,
                new Color(1f, 0.55f, 0.6f, 0.85f));
            var bsrt = (RectTransform)btnSub.transform;
            bsrt.anchorMin = new Vector2(1f, 0.5f);
            bsrt.anchorMax = new Vector2(1f, 0.5f);
            bsrt.pivot = new Vector2(1f, 0.5f);
            bsrt.sizeDelta = new Vector2(220f, 24f);
            bsrt.anchoredPosition = new Vector2(-24f, -38f);

            if (maxed)
            {
                var maxBadge = UiBuilder.CreateRect("MaxBadge", btn.transform,
                    new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(46f, 22f), new Vector2(-6f, -4f),
                    UiBuilder.WarmGold, rounded: true);
                UiBuilder.CreateText("MaxText", maxBadge.transform, "MAX", 14, FontStyle.Bold,
                    TextAnchor.MiddleCenter, new Color(0.2f, 0.1f, 0.02f, 1f));
            }

            btn.interactable = enabled;
            btn.onClick.AddListener(() =>
            {
                var ok = CampUpgrades.TryPurchase(kind);
                if (ok)
                {
                    var newLv = CampUpgrades.GetLevel(kind);
                    var msg = newLv >= maxLevel
                        ? $"{CampUpgrades.GetTitle(kind)} 升至满级 Lv {newLv}！"
                        : $"{CampUpgrades.GetTitle(kind)} 升级到 Lv {newLv}";
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, msg);
                    Refresh();
                }
                else
                {
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, "鱼干不足或已满级");
                }
            });
        }

        private static void BuildLevelDots(RectTransform row, int level, int maxLevel, Color kindColor)
        {
            const float spacing = 14f;
            const float size = 14f;
            var totalWidth = maxLevel * size + (maxLevel - 1) * spacing;
            var startX = -totalWidth * 0.5f;

            var anchor = UiBuilder.CreateRect("LevelDots", row,
                new Vector2(0.32f, 0f), new Vector2(0.32f, 0f),
                Vector2.zero, new Vector2(0f, 22f),
                null);
            anchor.pivot = new Vector2(0.5f, 0.5f);

            for (var i = 0; i < maxLevel; i++)
            {
                var filled = i < level;
                var dot = UiBuilder.CreateRect("Dot" + i, anchor,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(size, size),
                    new Vector2(startX + i * (size + spacing) + size * 0.5f, 0f),
                    filled ? kindColor : new Color(0.16f, 0.26f, 0.4f, 1f),
                    circle: true);
                dot.GetComponent<Image>().raycastTarget = false;
                if (filled)
                {
                    UiBuilder.AddOutline(dot.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1f, -1f));
                }
            }
        }
    }
}
