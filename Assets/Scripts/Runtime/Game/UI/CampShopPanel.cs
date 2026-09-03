using System.Collections.Generic;
using PenguinRun.Game.Save;
using PenguinRun.Game.Growth;
using PenguinRun.Game.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 营地/商店/记录三合一面板：统一入口、统一货币显示、Tab 切换。
    /// 整合自 CampPanel + ShopPanel 功能，新增 RunRecords 战绩页。
    /// </summary>
    public sealed class CampShopPanel : MonoBehaviour
    {
        private System.Action<MainMenuBootstrap.PanelEvent, string> dispatch;
        private RectTransform contentRoot;

        // Tab 状态
        /// <summary>营地强化分类过滤（原 CampPanel.CategoryFilter，旧面板删除后收编于此）。</summary>
        public enum CampCategoryFilter { All, Action, Survival, Reward }

        public enum MainTab { Camp, Shop, Records }
        private MainTab activeTab = MainTab.Camp;
        private readonly List<(Button btn, Image bg, TextMeshProUGUI text, MainTab tab)> tabButtons = new();

        // 子 Tab（商店内部）
        private ShopCategory activeShopCategory = ShopCategory.DailyDeals;
        private readonly List<(Button btn, Image bg, TextMeshProUGUI text, ShopCategory cat)> shopTabs = new();

        // 营地子状态
        private CampCategoryFilter activeCampFilter = CampCategoryFilter.All;

        // 常量
        private const float TabsHeight = 84f;
        private const float SubTabsHeight = 72f;
        private const float TopGap = 12f;

        // Shop 确认弹窗
        private GameObject confirmOverlay;
        private RectTransform countdownContainer;
        private TextMeshProUGUI countdownText;
        /// <summary>上次写入倒计时文本时的整秒值；秒数不变时跳过 Format 与 .text 写入。</summary>
        private long lastCountdownSeconds = -1;

        public static GameObject Build(Transform parent, System.Action<MainMenuBootstrap.PanelEvent, string> dispatch)
        {
            var rootRt = UiBuilder.CreateRect(
                "CampShopPanel", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                UiTheme.PaperBase);
            var script = rootRt.gameObject.AddComponent<CampShopPanel>();
            script.dispatch = dispatch;
            script.BuildLayout(rootRt);
            script.Refresh();
            return rootRt.gameObject;
        }

        private void BuildLayout(RectTransform parent)
        {
            PanelHeader.Create(parent, "营地与商店", "强化能力、兑换道具、查看战绩",
                () => dispatch(MainMenuBootstrap.PanelEvent.Close, null));

            BuildMainTabs(parent);

            var topInset = UiBuilder.PanelHeaderHeightPixels + TopGap + TabsHeight + TopGap;
            UiBuilder.CreatePanelScrollList(parent, out var content, topInset: topInset, bottomInset: 270f);
            contentRoot = content;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 顶部主 Tab（营地 / 商店 / 记录）
        // ─────────────────────────────────────────────────────────────────────────
        private void BuildMainTabs(RectTransform parent)
        {
            var top = UiBuilder.PanelHeaderHeightPixels + TopGap;
            var tabsContainer = UiBuilder.CreateRect("MainTabs", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, TabsHeight), new Vector2(0f, -top),
                Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.22f), rounded: true);
            tabsContainer.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(tabsContainer.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            UiBuilder.AddShadow(tabsContainer.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -4f));

            // 顶部松绿装饰条
            var topLine = UiBuilder.CreateRect("TopLine", tabsContainer,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 3f), new Vector2(0f, -2f),
                new Color(UiTheme.PineGreen.r, UiTheme.PineGreen.g, UiTheme.PineGreen.b, 0.45f), rounded: true);
            topLine.GetComponent<Image>().raycastTarget = false;

            var hlg = tabsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 10, 10);
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            tabButtons.Clear();
            // 装饰符仅用已核实可渲染的 ★（\u2726/\u2615 在 CJK 字体里会渲染成豆腐块）
            AddMainTab(tabsContainer, "营地强化", MainTab.Camp, "\u2605");
            AddMainTab(tabsContainer, "极光商店", MainTab.Shop, "\u2605");
            AddMainTab(tabsContainer, "跑酷记录", MainTab.Records, "\u2605");
            ApplyMainTabStyle();
        }

        private void AddMainTab(RectTransform parent, string label, MainTab tab, string icon)
        {
            // 复合按钮：图标 + 文字
            var btnGo = new GameObject("Tab_" + tab, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.sizeDelta = new Vector2(0f, 64f);

            var img = btnGo.GetComponent<Image>();
            img.sprite = UiBuilder.RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = UiTheme.MistBlue;

            var btn = btnGo.GetComponent<Button>();
            tabButtons.Add((btn, img, null, tab));

            // 内容 Stack
            var stack = new GameObject("Stack", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stack.transform.SetParent(btnGo.transform, false);
            var srt = (RectTransform)stack.transform;
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.sizeDelta = Vector2.zero;
            srt.offsetMin = new Vector2(12f, 0f);
            srt.offsetMax = new Vector2(-12f, 0f);
            var hlg = stack.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 图标
            var iconText = UiBuilder.CreateText("Icon", stack.transform, icon, 26, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentActive);
            var irt = (RectTransform)iconText.transform;
            irt.sizeDelta = new Vector2(32f, 32f);

            // 文字
            var text = UiBuilder.CreateText("Label", stack.transform, label, 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.TextPrimary);
            var trt = (RectTransform)text.transform;
            trt.sizeDelta = new Vector2(0f, 28f);
            trt.SetAsLastSibling();

            // 保存引用用于样式切换
            for (var i = 0; i < tabButtons.Count; i++)
            {
                if (tabButtons[i].tab == tab)
                {
                    tabButtons[i] = (btn, img, text, tab);
                    break;
                }
            }

            btn.onClick.AddListener(() =>
            {
                activeTab = tab;
                ApplyMainTabStyle();
                Refresh();
            });
        }

        private void ApplyMainTabStyle()
        {
            foreach (var (btn, img, text, tab) in tabButtons)
            {
                if (btn == null) continue;
                var sel = tab == activeTab;
                img.color = sel ? UiBuilder.AccentActive : UiTheme.MistBlue;
                if (text != null)
                {
                    text.color = sel ? Color.white : UiBuilder.TextPrimary;
                    text.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
                }
                // 选中底是 PineGreen，图标必须翻白，否则同色不可见
                var iconLabel = btn.transform.Find("Stack/Icon")?.GetComponent<TextMeshProUGUI>();
                if (iconLabel != null)
                    iconLabel.color = sel ? Color.white : UiBuilder.AccentActive;
                var outline = btn.gameObject.GetComponent<Outline>();
                if (sel)
                {
                    if (outline == null)
                        UiBuilder.AddOutline(btn.gameObject, UiTheme.StitchBorder, new Vector2(1.5f, -1.5f));
                    else
                        outline.effectColor = UiTheme.StitchBorder;
                }
                else
                {
                    if (outline != null) Destroy(outline);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 刷新逻辑：根据当前 Tab 分发
        // ─────────────────────────────────────────────────────────────────────────
        private void Refresh()
        {
            // 更新顶栏货币
            var fishText = transform.Find("Header/CurrencyPill/Value")?.GetComponent<TextMeshProUGUI>();
            if (fishText != null) fishText.text = $"{PlayerSave.TotalFishSnacks}";

            // 清空内容区
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            switch (activeTab)
            {
                case MainTab.Camp:
                    BuildCampTab();
                    break;
                case MainTab.Shop:
                    BuildShopTab();
                    break;
                case MainTab.Records:
                    BuildRecordsTab();
                    break;
            }

            UiBuilder.RebuildScrollContent(contentRoot);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 营地强化 Tab
        // ─────────────────────────────────────────────────────────────────────────
        private void BuildCampTab()
        {
            BuildReconstructionSection();

            // 子 Tab（分类筛选）
            BuildCampSubTabs();

            // 总览卡片
            BuildCampOverview();

            // 升级条目列表
            var filter = activeCampFilter;
            CampUpgradeCategory[] order = { CampUpgradeCategory.Action, CampUpgradeCategory.Survival, CampUpgradeCategory.Reward };

            foreach (var cat in order)
            {
                if (filter != CampCategoryFilter.All && !MatchesCampFilter(cat)) continue;

                var shownAny = false;
                foreach (var k in CampUpgrades.AllKinds)
                {
                    if (CampUpgrades.GetCategory(k) != cat) continue;
                    if (!shownAny && filter == CampCategoryFilter.All)
                    {
                        BuildCampSectionHeader(cat);
                        shownAny = true;
                    }
                    BuildCampRow(k);
                }
            }
        }

        private void BuildReconstructionSection()
        {
            var snapshot = CampRebuildStore.Snapshot();
            var summary = UiBuilder.CreateScrollListRow("ReconstructionSummary", contentRoot, 82f,
                Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.12f));
            UiBuilder.AddOutline(summary.gameObject, new Color(UiTheme.PineGreen.r, UiTheme.PineGreen.g,
                UiTheme.PineGreen.b, 0.4f), new Vector2(1f, -1f));
            var summaryText = UiBuilder.CreateText("Summary", summary,
                $"营地重建  {snapshot.TotalLevels}/{snapshot.MaxTotalLevels}    可修复 {snapshot.AffordableNodes}",
                24, FontStyle.Bold, TextAnchor.MiddleLeft, UiTheme.InkPrimary);
            var summaryRt = (RectTransform)summaryText.transform;
            summaryRt.anchorMin = Vector2.zero;
            summaryRt.anchorMax = Vector2.one;
            summaryRt.offsetMin = new Vector2(24f, 0f);
            summaryRt.offsetMax = new Vector2(-24f, 0f);

            foreach (var definition in CampBuildingCatalog.All)
                BuildReconstructionNode(CampRebuildStore.Snapshot(definition.Id));
        }

        private void BuildReconstructionNode(CampBuildingSnapshot node)
        {
            var locked = node.VisibleState == CampBuildingVisibleState.Locked;
            var affordable = node.VisibleState == CampBuildingVisibleState.Affordable;
            var color = node.Id switch
            {
                CampBuildingId.Bonfire => UiTheme.CoralAction,
                CampBuildingId.Workshop => UiTheme.PineGreen,
                CampBuildingId.CompanionHut => UiTheme.BerryDanger,
                CampBuildingId.LibraryTent => UiTheme.MistBlue,
                _ => UiTheme.WarmGold,
            };
            var row = UiBuilder.CreateScrollListRow("Rebuild_" + node.Id, contentRoot, 154f,
                locked ? Color.Lerp(UiTheme.PaperBase, UiTheme.MistBlue, 0.35f) : UiTheme.PaperWarm);
            UiBuilder.AddOutline(row.gameObject, new Color(color.r, color.g, color.b, locked ? 0.18f : 0.45f),
                new Vector2(1f, -1f));

            var title = UiBuilder.CreateText("Title", row,
                $"{node.Definition.Title}  Lv {node.Level}/{CampBuildingCatalog.MaxLevel}",
                25, FontStyle.Bold, TextAnchor.UpperLeft, UiTheme.InkPrimary);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(0.72f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 36f);
            titleRt.anchoredPosition = new Vector2(26f, -18f);

            var description = locked && node.Definition.Prerequisite.HasValue
                ? $"先将{CampBuildingCatalog.For(node.Definition.Prerequisite.Value).Title}修复到 Lv {node.Definition.PrerequisiteLevel}"
                : $"{node.Definition.Story}  ·  {node.Definition.UnlockEffect}";
            var body = UiBuilder.CreateText("Description", row, description, 18, FontStyle.Normal,
                TextAnchor.UpperLeft, locked ? UiBuilder.TextTertiary : UiTheme.InkSecondary);
            var bodyRt = (RectTransform)body.transform;
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(0.72f, 1f);
            bodyRt.offsetMin = new Vector2(26f, 16f);
            bodyRt.offsetMax = new Vector2(-10f, -58f);

            var label = node.IsMaxLevel ? "重建完成" : affordable ? $"修复 {node.NextCost} 鱼干" :
                locked ? "前置未完成" : $"还差 {Mathf.Max(0, node.NextCost - PlayerSave.TotalFishSnacks)} 鱼干";
            var button = UiBuilder.CreateButton("Rebuild", row, label,
                affordable ? color : Color.Lerp(UiTheme.PaperBase, UiTheme.MistBlue, 0.45f),
                affordable ? UiTheme.InkPrimary : UiBuilder.TextTertiary, 19, rounded: true);
            var buttonRt = (RectTransform)button.transform;
            buttonRt.anchorMin = new Vector2(1f, 0.5f);
            buttonRt.anchorMax = new Vector2(1f, 0.5f);
            buttonRt.pivot = new Vector2(1f, 0.5f);
            buttonRt.sizeDelta = new Vector2(205f, 66f);
            buttonRt.anchoredPosition = new Vector2(-22f, 0f);
            button.interactable = affordable;
            button.onClick.AddListener(() =>
            {
                if (!CampRebuildStore.TryPurchase(node.Id)) return;
                dispatch(MainMenuBootstrap.PanelEvent.Toast, $"{node.Definition.Title}完成一阶段重建");
                Refresh();
            });
        }

        private void BuildCampSubTabs()
        {
            var tabsRow = UiBuilder.CreateScrollListRow("CampSubTabs", contentRoot, 64f, null);
            var tabsBg = UiBuilder.CreateRect("TabsBg", tabsRow,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 56f), Vector2.zero,
                Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.22f), rounded: true);
            tabsBg.pivot = new Vector2(0.5f, 0.5f);
            tabsBg.offsetMin = new Vector2(0f, -28f);
            tabsBg.offsetMax = new Vector2(0f, 28f);
            UiBuilder.AddOutline(tabsBg.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var hlg = tabsBg.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var campTabs = new List<(Button btn, Image bg, TextMeshProUGUI text, CampCategoryFilter filter)>();
            System.Action<RectTransform, string, CampCategoryFilter> addTab = (parent, lbl, f) =>
            {
                var btn = UiBuilder.CreateButton("SubTab_" + f, parent.transform, lbl,
                    UiTheme.MistBlue, UiBuilder.TextPrimary, 20, rounded: true);
                var img = btn.GetComponent<Image>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                campTabs.Add((btn, img, txt, f));
                btn.onClick.AddListener(() =>
                {
                    activeCampFilter = f;
                    // 重新应用样式并刷新
                    foreach (var t in campTabs)
                    {
                        var sel = t.filter == activeCampFilter;
                        t.bg.color = sel ? UiBuilder.AccentActive : UiTheme.MistBlue;
                        if (t.text != null)
                        {
                            t.text.color = sel ? Color.white : UiBuilder.TextPrimary;
                            t.text.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
                        }
                        var ol = t.btn.gameObject.GetComponent<Outline>();
                        if (sel)
                        {
                            if (ol == null) UiBuilder.AddOutline(t.btn.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f));
                        }
                        else
                        {
                            if (ol != null) Destroy(ol);
                        }
                    }
                    Refresh();
                });
            };

            addTab(tabsBg, "全部", CampCategoryFilter.All);
            addTab(tabsBg, "跑酷", CampCategoryFilter.Action);
            addTab(tabsBg, "防守", CampCategoryFilter.Survival);
            addTab(tabsBg, "收益", CampCategoryFilter.Reward);

            // 应用初始样式
            foreach (var t in campTabs)
            {
                var sel = t.filter == activeCampFilter;
                t.bg.color = sel ? UiBuilder.AccentActive : UiTheme.MistBlue;
                if (t.text != null)
                {
                    t.text.color = sel ? Color.white : UiBuilder.TextPrimary;
                    t.text.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
                }
                if (sel) UiBuilder.AddOutline(t.btn.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f));
            }
        }

        private bool MatchesCampFilter(CampUpgradeCategory cat) =>
            activeCampFilter switch
            {
                CampCategoryFilter.Action => cat == CampUpgradeCategory.Action,
                CampCategoryFilter.Survival => cat == CampUpgradeCategory.Survival,
                CampCategoryFilter.Reward => cat == CampUpgradeCategory.Reward,
                _ => true,
            };

        private void BuildCampOverview()
        {
            var totalLevels = CampUpgrades.TotalLevelsCount();
            var maxTotal = CampUpgrades.MaxTotalLevels;
            var ratio = maxTotal > 0 ? totalLevels / (float)maxTotal : 0f;
            var fishPct = (CampUpgrades.GetFishGainMultiplier() - 1f) * 100f;
            var scorePct = (CampUpgrades.GetScoreMultiplier() - 1f) * 100f;

            var row = UiBuilder.CreateScrollListRow("CampOverview", contentRoot, 120f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            UiBuilder.AddShadow(row.gameObject, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, -3f));

            // 左侧重音条
            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                UiBuilder.AccentActive, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 标题
            var title = UiBuilder.CreateText("Title", row, "强化总览",
                24, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.TextPrimary);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 32f);
            trt.anchoredPosition = new Vector2(20f, -14f);

            // 三个统计 chip
            BuildStatChip(row, "总等级", $"{totalLevels}/{maxTotal}", new Color(0.4f, 0.85f, 1f, 1f), 0.02f);
            BuildStatChip(row, "已满级", $"{CampUpgrades.MaxedKindCount()}/{CampUpgrades.AllKinds.Length}", new Color(0.4f, 0.95f, 0.7f, 1f), 0.35f);
            BuildStatChip(row, "综合加成", $"鱼+{fishPct:0}%  分+{scorePct:0}%", new Color(1f, 0.82f, 0.35f, 1f), 0.68f);

            // 底部进度条
            var track = UiBuilder.CreateRect("Track", row,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 6f), new Vector2(20f, 12f),
                new Color(0.05f, 0.1f, 0.18f, 0.95f), rounded: true);
            track.offsetMax = new Vector2(-20f, track.offsetMax.y);
            UiBuilder.AddOutline(track.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var fill = UiBuilder.CreateRect("Fill", track,
                new Vector2(0f, 0f), new Vector2(Mathf.Clamp01(ratio), 1f),
                Vector2.zero, Vector2.zero,
                UiBuilder.AccentActive, rounded: true);
            fill.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildStatChip(RectTransform parent, string title, string value, Color color, float anchorX)
        {
            var chip = UiBuilder.CreateRect("Chip_" + title, parent,
                new Vector2(anchorX, 0.5f), new Vector2(anchorX + 0.3f, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color(0.04f, 0.08f, 0.14f, 0.6f));
            chip.pivot = new Vector2(0f, 0.5f);
            chip.sizeDelta = new Vector2(-16f, 56f);
            chip.offsetMin = new Vector2(8f, -28f);
            chip.offsetMax = new Vector2(-8f, 28f);
            UiBuilder.AddOutline(chip.gameObject, new Color(color.r, color.g, color.b, 0.35f), new Vector2(1f, -1f));

            UiBuilder.CreateText("Label", chip, title, 16, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var lrt = (RectTransform)chip.transform.Find("Label");
            if (lrt != null)
            {
                lrt.anchorMin = new Vector2(0f, 1f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.sizeDelta = new Vector2(-12f, 20f);
                lrt.anchoredPosition = new Vector2(8f, -4f);
            }

            var vt = UiBuilder.CreateText("Value", chip, value, 24, FontStyle.Bold, TextAnchor.LowerLeft, color);
            var vrt = (RectTransform)vt.transform;
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0f, 0f);
            vrt.sizeDelta = new Vector2(-12f, 26f);
            vrt.anchoredPosition = new Vector2(8f, 4f);
        }

        private void BuildCampSectionHeader(CampUpgradeCategory cat)
        {
            var header = UiBuilder.CreateScrollListRow("CampSection", contentRoot, 40f, null);
            var color = CampUpgrades.GetCategoryColor(cat);

            var bar = UiBuilder.CreateRect("Bar", header,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 20f), new Vector2(12f, 0f),
                color, rounded: true);
            bar.GetComponent<Image>().raycastTarget = false;

            UiBuilder.CreateText("Label", header, $"{CampUpgrades.GetCategoryLabel(cat)}类强化",
                20, FontStyle.Bold, TextAnchor.MiddleLeft, color);
            var lrt = (RectTransform)header.transform.Find("Label");
            if (lrt != null)
            {
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(28f, 0f);
                lrt.offsetMax = new Vector2(-12f, 0f);
            }
        }

        private void BuildCampRow(CampUpgradeKind kind)
        {
            var level = CampUpgrades.GetLevel(kind);
            var maxLevel = PlayerSave.CampMaxLevel;
            var cost = CampUpgrades.GetUpgradeCost(kind, level);
            var maxed = !cost.HasValue;
            var kindColor = GetKindColor(kind);
            var icon = GetKindIcon(kind);

            var row = UiBuilder.CreateScrollListRow("CampRow_" + kind, contentRoot, 240f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject,
                maxed ? new Color(1f, 0.85f, 0.35f, 0.55f) : UiBuilder.BorderSubtle,
                new Vector2(1f, -1f));

            // 重音条
            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                kindColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 图标徽章
            var badge = UiBuilder.CreateRect("Badge", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(68f, 68f), new Vector2(50f, -52f),
                new Color(kindColor.r, kindColor.g, kindColor.b, 0.18f), circle: true);
            UiBuilder.AddOutline(badge.gameObject, new Color(kindColor.r, kindColor.g, kindColor.b, 0.55f), new Vector2(1.5f, -1.5f));
            UiBuilder.CreateText("Icon", badge.transform, icon, 34, FontStyle.Bold, TextAnchor.MiddleCenter, kindColor);

            // 标题
            var title = UiBuilder.CreateText("Title", row, CampUpgrades.GetTitle(kind),
                28, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.TextPrimary);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.6f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 36f);
            trt.anchoredPosition = new Vector2(98f, -18f);

            // 等级 chip
            var lvChip = UiBuilder.CreateRect("LvChip", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(100f, 30f), new Vector2(98f, -62f),
                new Color(kindColor.r, kindColor.g, kindColor.b, 0.2f), rounded: true);
            UiBuilder.AddOutline(lvChip.gameObject, new Color(kindColor.r, kindColor.g, kindColor.b, 0.5f), new Vector2(1f, -1f));
            UiBuilder.CreateText("Lv", lvChip.transform, $"Lv {level}/{maxLevel}", 17, FontStyle.Bold, TextAnchor.MiddleCenter, kindColor);

            // 描述
            var desc = UiBuilder.CreateText("Desc", row, CampUpgrades.GetDescription(kind),
                19, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.6f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 48f);
            drt.anchoredPosition = new Vector2(98f, -100f);

            // 效果预览
            var previewBg = UiBuilder.CreateRect("Preview", row,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 46f), new Vector2(98f, 58f),
                new Color(0.05f, 0.1f, 0.18f, 0.6f), rounded: true);
            previewBg.anchorMax = new Vector2(0.6f, 0f);
            previewBg.offsetMax = new Vector2(-12f, previewBg.offsetMax.y);
            UiBuilder.AddOutline(previewBg.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var curText = UiBuilder.CreateText("Cur", previewBg,
                $"当前 {CampUpgrades.FormatEffectAt(kind, level)}",
                17, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.TextSecondary);
            var crt = (RectTransform)curText.transform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(12f, 0f);
            crt.offsetMax = new Vector2(-4f, 0f);

            var arrow = UiBuilder.CreateText("Arrow", previewBg, "\u2192",
                20, FontStyle.Bold, TextAnchor.MiddleCenter, maxed ? UiBuilder.WarmGold : kindColor);
            var art = (RectTransform)arrow.transform;
            art.anchorMin = new Vector2(0.46f, 0f);
            art.anchorMax = new Vector2(0.54f, 1f);
            art.sizeDelta = Vector2.zero;

            var nextVal = maxed ? "已满级" : CampUpgrades.FormatEffectAt(kind, level + 1);
            var nextText = UiBuilder.CreateText("Next", previewBg,
                $"下一级 · {nextVal}", 17, FontStyle.Bold, TextAnchor.MiddleLeft,
                maxed ? UiBuilder.WarmGold : kindColor);
            var nrt = (RectTransform)nextText.transform;
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.offsetMin = new Vector2(4f, 0f);
            nrt.offsetMax = new Vector2(-12f, 0f);

            // 等级圆点
            BuildCampDots(row, level, maxLevel, kindColor);

            // 按钮
            var btnLabel = maxed ? "已满级" : $"消耗 {cost.Value} 鱼干";
            var canAfford = !maxed && PlayerSave.TotalFishSnacks >= (cost ?? int.MaxValue);
            Color btnBg, btnFg;
            if (maxed)
            {
                btnBg = new Color(0.12f, 0.22f, 0.32f, 0.85f);
                btnFg = new Color(1f, 1f, 1f, 0.5f);
            }
            else if (canAfford)
            {
                btnBg = kindColor;
                btnFg = new Color(0.05f, 0.08f, 0.12f, 1f);
            }
            else
            {
                btnBg = new Color(0.18f, 0.28f, 0.4f, 0.9f);
                btnFg = new Color(1f, 1f, 1f, 0.55f);
            }

            var btn = UiBuilder.CreateButton("Buy", row, btnLabel, btnBg, btnFg, 21, rounded: true);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.sizeDelta = new Vector2(200f, 76f);
            brt.anchoredPosition = new Vector2(-20f, 10f);
            btn.interactable = canAfford;

            if (canAfford)
            {
                UiBuilder.AddOutline(btn.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1.5f, -1.5f));
                UiBuilder.AddShadow(btn.gameObject,
                    new Color(kindColor.r * 0.5f, kindColor.g * 0.5f, kindColor.b * 0.5f, 0.5f),
                    new Vector2(0f, -3f));
            }

            if (!maxed && !canAfford)
            {
                var hint = UiBuilder.CreateText("Hint", row,
                    $"还差 {Mathf.Max(0, cost.Value - PlayerSave.TotalFishSnacks)} 鱼干",
                    16, FontStyle.Normal, TextAnchor.MiddleRight, new Color(1f, 0.55f, 0.6f, 0.85f));
                var hrt = (RectTransform)hint.transform;
                hrt.anchorMin = new Vector2(1f, 0.5f);
                hrt.anchorMax = new Vector2(1f, 0.5f);
                hrt.pivot = new Vector2(1f, 0.5f);
                hrt.sizeDelta = new Vector2(200f, 22f);
                hrt.anchoredPosition = new Vector2(-20f, -36f);
            }

            btn.onClick.AddListener(() =>
            {
                if (CampUpgrades.TryPurchase(kind))
                {
                    var newLv = CampUpgrades.GetLevel(kind);
                    var msg = newLv >= maxLevel ? $"{CampUpgrades.GetTitle(kind)} 升至满级！" : $"升级至 Lv {newLv}";
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, msg);
                    Refresh();
                }
                else
                {
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, "鱼干不足或已满级");
                }
            });
        }

        private void BuildCampDots(RectTransform row, int level, int maxLevel, Color color)
        {
            const float spacing = 12f;
            const float size = 12f;
            var totalWidth = maxLevel * size + (maxLevel - 1) * spacing;
            var startX = -totalWidth * 0.5f;

            var anchor = UiBuilder.CreateRect("Dots", row,
                new Vector2(0.3f, 0f), new Vector2(0.3f, 0f),
                Vector2.zero, new Vector2(0f, 18f), null);
            anchor.pivot = new Vector2(0.5f, 0.5f);

            for (var i = 0; i < maxLevel; i++)
            {
                var filled = i < level;
                var dot = UiBuilder.CreateRect("Dot" + i, anchor,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(size, size),
                    new Vector2(startX + i * (size + spacing) + size * 0.5f, 0f),
                    filled ? color : new Color(0.16f, 0.26f, 0.4f, 1f),
                    circle: true);
                dot.GetComponent<Image>().raycastTarget = false;
                if (filled) UiBuilder.AddOutline(dot.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1f, -1f));
            }
        }

        private Color GetKindColor(CampUpgradeKind kind) => kind switch
        {
            CampUpgradeKind.Dash => new Color(1f, 0.55f, 0.25f, 1f),
            CampUpgradeKind.Tuan => new Color(0.55f, 0.85f, 1f, 1f),
            CampUpgradeKind.Polar => new Color(0.4f, 0.95f, 0.7f, 1f),
            CampUpgradeKind.Magnet => new Color(0.85f, 0.55f, 1f, 1f),
            CampUpgradeKind.FishGain => new Color(1f, 0.82f, 0.35f, 1f),
            CampUpgradeKind.ScoreBonus => new Color(1f, 0.45f, 0.55f, 1f),
            _ => UiBuilder.AccentActive,
        };

        // 徽章符号只保留已核实可渲染的 ★（\u279E/\u2745/\u269B/\u2615 在 CJK 字体里是豆腐块），
        // 各类目靠徽章配色（GetKindColor）区分。
        private string GetKindIcon(CampUpgradeKind kind) => kind switch
        {
            CampUpgradeKind.Tuan => "\u2665",
            _ => "\u2605",
        };

        // ─────────────────────────────────────────────────────────────────────────
        // 商店 Tab（简化版，整合自 ShopPanel）
        // ─────────────────────────────────────────────────────────────────────────
        private void BuildShopTab()
        {
            // 子 Tab
            BuildShopSubTabs();

            // 库存状态条
            BuildShopStatusBar();

            // 内容
            switch (activeShopCategory)
            {
                case ShopCategory.DailyDeals:
                    BuildShopDailyDeals();
                    break;
                case ShopCategory.Boosters:
                    BuildShopBoosters();
                    break;
                case ShopCategory.Cosmetics:
                    BuildShopCosmetics();
                    break;
                case ShopCategory.Bundles:
                    BuildShopBundles();
                    break;
            }
        }

        private void BuildShopSubTabs()
        {
            var tabsRow = UiBuilder.CreateScrollListRow("ShopSubTabs", contentRoot, 64f, null);
            var tabsBg = UiBuilder.CreateRect("TabsBg", tabsRow,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 56f), Vector2.zero,
                Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.22f), rounded: true);
            tabsBg.pivot = new Vector2(0.5f, 0.5f);
            tabsBg.offsetMin = new Vector2(0f, -28f);
            tabsBg.offsetMax = new Vector2(0f, 28f);
            UiBuilder.AddOutline(tabsBg.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var hlg = tabsBg.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            shopTabs.Clear();
            System.Action<string, ShopCategory> addTab = (lbl, cat) =>
            {
                var btn = UiBuilder.CreateButton("ShopTab_" + cat, tabsBg.transform, lbl,
                    UiTheme.MistBlue, UiBuilder.TextPrimary, 20, rounded: true);
                var img = btn.GetComponent<Image>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                shopTabs.Add((btn, img, txt, cat));
                btn.onClick.AddListener(() =>
                {
                    activeShopCategory = cat;
                    foreach (var t in shopTabs)
                    {
                        var sel = t.cat == activeShopCategory;
                        t.bg.color = sel ? UiBuilder.AccentActive : UiTheme.MistBlue;
                        if (t.text != null)
                        {
                            t.text.color = sel ? Color.white : UiBuilder.TextPrimary;
                            t.text.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
                        }
                        var ol = t.btn.gameObject.GetComponent<Outline>();
                        if (sel) { if (ol == null) UiBuilder.AddOutline(t.btn.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f)); }
                        else { if (ol != null) Destroy(ol); }
                    }
                    Refresh();
                });
            };

            addTab("特惠", ShopCategory.DailyDeals);
            addTab("道具", ShopCategory.Boosters);
            addTab("装扮", ShopCategory.Cosmetics);
            addTab("礼盒", ShopCategory.Bundles);

            foreach (var t in shopTabs)
            {
                var sel = t.cat == activeShopCategory;
                t.bg.color = sel ? UiBuilder.AccentActive : UiTheme.MistBlue;
                if (t.text != null)
                {
                    t.text.color = sel ? Color.white : UiBuilder.TextPrimary;
                    t.text.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
                }
                if (sel) UiBuilder.AddOutline(t.btn.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f));
            }
        }

        private void BuildShopStatusBar()
        {
            var row = UiBuilder.CreateScrollListRow("ShopStatus", contentRoot, 70f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            // 左侧库存
            var invIcon = UiBuilder.CreateRect("InvIcon", row,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(36f, 36f), new Vector2(24f, 0f),
                new Color(UiTheme.PineGreen.r, UiTheme.PineGreen.g, UiTheme.PineGreen.b, 0.22f), circle: true);
            UiBuilder.AddOutline(invIcon.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f));
            UiBuilder.CreateText("InvIconText", invIcon.transform, "\u25EB", 20, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentActive);

            var df = ShopStore.InventoryCount(ShopItemKind.BoosterDoubleFish);
            var sb = ShopStore.InventoryCount(ShopItemKind.BoosterScoreBoost);
            var ls = ShopStore.InventoryCount(ShopItemKind.BoosterLuckyStart);
            var invText = df + sb + ls == 0 ? "持有道具：暂无消耗券" : $"双倍 ×{df} · 加分 ×{sb} · 幸运 ×{ls}";

            var invLabel = UiBuilder.CreateText("InvText", row, invText,
                21, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.TextPrimary);
            var irt = (RectTransform)invLabel.transform;
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(0.6f, 1f);
            irt.offsetMin = new Vector2(68f, 4f);
            irt.offsetMax = new Vector2(-8f, -4f);

            // 右侧余额
            var balText = UiBuilder.CreateText("Balance", row, $"鱼干 {PlayerSave.TotalFishSnacks}",
                22, FontStyle.Bold, TextAnchor.MiddleRight, UiBuilder.WarmGold);
            var brt = (RectTransform)balText.transform;
            brt.anchorMin = new Vector2(0.6f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(8f, 4f);
            brt.offsetMax = new Vector2(-24f, -4f);
        }

        private void BuildShopDailyDeals()
        {
            BuildShopBanner("每日特惠", "每日凌晨刷新，限时折扣价。", new Color(1f, 0.78f, 0.32f, 1f), true);
            var deals = ShopStore.GetDailyDeals();
            foreach (var d in deals) BuildShopDealCard(d);
            BuildShopHint("打折商品可与营地强化叠加");
        }

        private void BuildShopBoosters()
        {
            BuildShopBanner("局内消耗券", "购入后下一局自动消耗，效果仅本局生效。", new Color(1f, 0.82f, 0.35f, 1f), false);
            foreach (var def in ShopCatalog.InCategory(ShopCategory.Boosters))
            {
                if (def.BasePrice <= 0) continue;
                BuildShopBoosterCard(def);
            }
        }

        private void BuildShopCosmetics()
        {
            BuildShopBanner("装扮", "解锁后在大厅企鹅吉祥物上切换显示。", new Color(0.78f, 0.55f, 1f, 1f), false);
            BuildShopSubsectionHeader("围巾色调");
            foreach (var def in ShopCatalog.InCategory(ShopCategory.Cosmetics))
                if (def.Kind == ShopItemKind.CosmeticScarf) BuildShopCosmeticCard(def);
            BuildShopSubsectionHeader("帽子样式");
            foreach (var def in ShopCatalog.InCategory(ShopCategory.Cosmetics))
                if (def.Kind == ShopItemKind.CosmeticHat) BuildShopCosmeticCard(def);
        }

        private void BuildShopBundles()
        {
            BuildShopBanner("珍藏礼盒", "每款仅可购买一次。", new Color(1f, 0.78f, 0.32f, 1f), false);
            foreach (var def in ShopCatalog.InCategory(ShopCategory.Bundles))
                BuildShopBundleCard(def);
        }

        private void BuildShopBanner(string title, string desc, Color color, bool showCountdown)
        {
            var banner = UiBuilder.CreateScrollListRow("ShopBanner", contentRoot, 100f, UiBuilder.Surface1);
            UiBuilder.AddOutline(banner.gameObject, new Color(color.r, color.g, color.b, 0.45f), new Vector2(1f, -1f));

            UiBuilder.AttachArtVignette("SupplyBoardArt", banner, Image2ArtCatalog.ShopSupply, 0.13f);

            var accent = UiBuilder.CreateRect("Accent", banner,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                color, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            var t = UiBuilder.CreateText("Title", banner, title, 26, FontStyle.Bold, TextAnchor.UpperLeft, color);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 32f);
            trt.anchoredPosition = new Vector2(18f, -14f);

            var d = UiBuilder.CreateText("Desc", banner, desc, 18, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)d.transform;
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(showCountdown ? 0.6f : 1f, 0.5f);
            drt.pivot = new Vector2(0f, 0f);
            drt.sizeDelta = new Vector2(0f, 32f);
            drt.anchoredPosition = new Vector2(18f, 10f);

            if (showCountdown)
            {
                countdownContainer = UiBuilder.CreateRect("Countdown", banner.transform,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(200f, 40f), new Vector2(-16f, -6f),
                    new Color(0.04f, 0.09f, 0.16f, 1f), rounded: true);
                UiBuilder.AddOutline(countdownContainer.gameObject,
                    new Color(color.r, color.g, color.b, 0.5f), new Vector2(1f, -1f));

                UiBuilder.CreateText("Label", countdownContainer.transform, "刷新",
                    16, FontStyle.Normal, TextAnchor.MiddleLeft, UiBuilder.TextSecondary);
                var lrt = (RectTransform)countdownContainer.transform.Find("Label");
                if (lrt != null)
                {
                    lrt.anchorMin = new Vector2(0f, 0f);
                    lrt.anchorMax = new Vector2(0.35f, 1f);
                    lrt.offsetMin = new Vector2(12f, 0f);
                    lrt.offsetMax = new Vector2(0f, 0f);
                }

                countdownText = UiBuilder.CreateText("Value", countdownContainer.transform, "—",
                    20, FontStyle.Bold, TextAnchor.MiddleRight, color);
                var crt = (RectTransform)countdownText.transform;
                crt.anchorMin = new Vector2(0.35f, 0f);
                crt.anchorMax = new Vector2(1f, 1f);
                crt.offsetMin = new Vector2(6f, 0f);
                crt.offsetMax = new Vector2(-12f, 0f);
                var initialMs = ShopStore.MillisUntilNextRefresh();
                lastCountdownSeconds = initialMs <= 0 ? 0L : initialMs / 1000L;
                countdownText.text = FormatCountdown(initialMs);
            }
        }

        private void BuildShopDealCard(ShopStore.DailyDeal deal)
        {
            var def = deal.Item;
            var rColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;

            var row = UiBuilder.CreateScrollListRow("Deal_" + def.Id, contentRoot, 180f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, new Color(rColor.r, rColor.g, rColor.b, 0.55f), new Vector2(1.5f, -1.5f));

            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            BuildShopIconBadge(row, def.IconGlyph, theme, new Vector2(50f, -50f));

            var title = UiBuilder.CreateText("Title", row, def.Title, 26, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.TextPrimary);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.65f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 32f);
            trt.anchoredPosition = new Vector2(100f, -18f);

            // 折扣 chip
            var dealChip = UiBuilder.CreateRect("DealChip", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(80f, 28f), new Vector2(100f, -56f),
                new Color(1f, 0.45f, 0.55f, 0.22f), rounded: true);
            UiBuilder.AddOutline(dealChip.gameObject, new Color(1f, 0.45f, 0.55f, 0.55f), new Vector2(1f, -1f));
            UiBuilder.CreateText("DealText", dealChip.transform, $"-{deal.DiscountPercent}%",
                17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.6f, 1f));

            BuildShopRarityChip(row, def.Rarity, new Vector2(190f, -56f));

            var desc = UiBuilder.CreateText("Desc", row, def.Description, 18, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.65f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 48f);
            drt.anchoredPosition = new Vector2(100f, -92f);

            // 价格
            var orig = UiBuilder.CreateText("Orig", row, $"<s>{deal.OriginalPrice}</s>", 17, FontStyle.Normal,
                TextAnchor.LowerRight, new Color(0.6f, 0.7f, 0.82f, 0.7f));
            orig.richText = true;
            var ort = (RectTransform)orig.transform;
            ort.anchorMin = new Vector2(1f, 0.5f);
            ort.anchorMax = new Vector2(1f, 0.5f);
            ort.pivot = new Vector2(1f, 0f);
            ort.sizeDelta = new Vector2(180f, 22f);
            ort.anchoredPosition = new Vector2(-20f, 26f);

            var price = UiBuilder.CreateText("Price", row, $"{deal.DiscountedPrice} 鱼干", 24, FontStyle.Bold,
                TextAnchor.MiddleRight, UiBuilder.WarmGold);
            var prt = (RectTransform)price.transform;
            prt.anchorMin = new Vector2(1f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(1f, 0.5f);
            prt.sizeDelta = new Vector2(180f, 28f);
            prt.anchoredPosition = new Vector2(-20f, 2f);

            BuildShopBuyButton(row, "立即购买", theme, deal.DiscountedPrice, def, deal,
                new Vector2(180f, 56f), new Vector2(-20f, -30f));
        }

        private void BuildShopBoosterCard(ShopItemDefinition def)
        {
            var rColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;
            var stock = ShopStore.InventoryCount(def.Kind);

            var row = UiBuilder.CreateScrollListRow("Booster_" + def.Id, contentRoot, 170f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, new Color(rColor.r, rColor.g, rColor.b, 0.45f), new Vector2(1f, -1f));

            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            BuildShopIconBadge(row, def.IconGlyph, theme, new Vector2(50f, -50f));

            var title = UiBuilder.CreateText("Title", row, def.Title, 25, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.TextPrimary);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.65f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 30f);
            trt.anchoredPosition = new Vector2(100f, -16f);

            BuildShopRarityChip(row, def.Rarity, new Vector2(100f, -52f));

            // 库存
            var stockChip = UiBuilder.CreateRect("StockChip", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(76f, 28f), new Vector2(190f, -52f),
                new Color(UiTheme.PineGreen.r, UiTheme.PineGreen.g, UiTheme.PineGreen.b, 0.18f), rounded: true);
            UiBuilder.AddOutline(stockChip.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f));
            UiBuilder.CreateText("StockText", stockChip.transform, $"持有 ×{stock}",
                16, FontStyle.Bold, TextAnchor.MiddleCenter, UiBuilder.AccentActive);

            var desc = UiBuilder.CreateText("Desc", row, def.Description, 18, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.65f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 48f);
            drt.anchoredPosition = new Vector2(100f, -88f);

            var price = UiBuilder.CreateText("Price", row, $"{def.BasePrice} 鱼干", 23, FontStyle.Bold,
                TextAnchor.MiddleRight, UiBuilder.WarmGold);
            var prt = (RectTransform)price.transform;
            prt.anchorMin = new Vector2(1f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(1f, 0.5f);
            prt.sizeDelta = new Vector2(180f, 26f);
            prt.anchoredPosition = new Vector2(-20f, 16f);

            BuildShopBuyButton(row, "购买", theme, def.BasePrice, def, null,
                new Vector2(180f, 54f), new Vector2(-20f, -20f));
        }

        private void BuildShopCosmeticCard(ShopItemDefinition def)
        {
            var rColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;
            var owned = ShopStore.IsOwned(def);
            var equipped = ShopStore.IsEquipped(def);

            // 已装备行：淡雾蓝纸面 + 松绿描边（旧的深青底会让墨色文字不可读）
            var row = UiBuilder.CreateScrollListRow("Cos_" + def.Id, contentRoot, 140f,
                equipped ? Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.45f) : UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject,
                equipped ? UiBuilder.AccentActive : new Color(rColor.r, rColor.g, rColor.b, 0.4f),
                new Vector2(1f, -1f));

            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 颜色样本
            var swatch = UiBuilder.CreateRect("Swatch", row,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(68f, 68f), new Vector2(50f, 0f),
                theme, circle: true);
            UiBuilder.AddOutline(swatch.gameObject, Color.white, new Vector2(2f, -2f));

            var title = UiBuilder.CreateText("Title", row, def.Title, 25, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.TextPrimary);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.6f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 30f);
            trt.anchoredPosition = new Vector2(98f, -18f);

            BuildShopRarityChip(row, def.Rarity, new Vector2(98f, -54f));

            if (equipped)
            {
                var eqChip = UiBuilder.CreateRect("EqChip", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(76f, 28f), new Vector2(190f, -54f),
                    new Color(UiTheme.PineGreen.r, UiTheme.PineGreen.g, UiTheme.PineGreen.b, 0.22f), rounded: true);
                UiBuilder.AddOutline(eqChip.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f));
                UiBuilder.CreateText("EqText", eqChip.transform, "已装备", 16, FontStyle.Bold,
                    TextAnchor.MiddleCenter, UiBuilder.AccentActive);
            }
            else if (owned)
            {
                var ownChip = UiBuilder.CreateRect("OwnChip", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(76f, 28f), new Vector2(190f, -54f),
                    new Color(0.4f, 0.95f, 0.7f, 0.18f), rounded: true);
                UiBuilder.AddOutline(ownChip.gameObject, new Color(0.4f, 0.95f, 0.7f, 0.55f), new Vector2(1f, -1f));
                UiBuilder.CreateText("OwnText", ownChip.transform, "已解锁", 16, FontStyle.Bold,
                    TextAnchor.MiddleCenter, new Color(0.4f, 0.95f, 0.7f, 1f));
            }

            var desc = UiBuilder.CreateText("Desc", row, def.Description, 17, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.6f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 40f);
            drt.anchoredPosition = new Vector2(98f, -88f);

            // 操作按钮
            string label; Color btnBg, btnFg;
            if (equipped) { label = "已装备"; btnBg = new Color(0.12f, 0.22f, 0.32f, 0.85f); btnFg = new Color(1f, 1f, 1f, 0.55f); }
            else if (owned) { label = "装备"; btnBg = UiBuilder.AccentActive; btnFg = new Color(0.04f, 0.08f, 0.14f, 1f); }
            else if (def.BasePrice <= 0) { label = "默认"; btnBg = new Color(0.12f, 0.22f, 0.32f, 0.85f); btnFg = new Color(1f, 1f, 1f, 0.55f); }
            else { label = $"{def.BasePrice} 鱼干"; btnBg = theme; btnFg = new Color(0.04f, 0.08f, 0.14f, 1f); }

            var btn = UiBuilder.CreateButton("Action", row, label, btnBg, btnFg, 21, rounded: true);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.sizeDelta = new Vector2(160f, 60f);
            brt.anchoredPosition = new Vector2(-20f, 0f);

            if (equipped || (def.BasePrice <= 0 && !owned)) btn.interactable = false;
            else if (owned)
            {
                btn.onClick.AddListener(() =>
                {
                    if (ShopStore.TryEquip(def))
                    {
                        dispatch(MainMenuBootstrap.PanelEvent.Toast, $"{def.Title} 已装备");
                        Refresh();
                    }
                });
                UiBuilder.AddOutline(btn.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1.5f, -1.5f));
            }
            else
            {
                var canAfford = PlayerSave.TotalFishSnacks >= def.BasePrice;
                btn.interactable = canAfford;
                if (!canAfford)
                {
                    var bImg = btn.GetComponent<Image>();
                    bImg.color = new Color(0.18f, 0.28f, 0.4f, 0.9f);
                    btn.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.55f);
                }
                else
                {
                    UiBuilder.AddOutline(btn.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1.5f, -1.5f));
                    UiBuilder.AddShadow(btn.gameObject, new Color(theme.r * 0.5f, theme.g * 0.5f, theme.b * 0.5f, 0.5f), new Vector2(0f, -3f));
                }
                btn.onClick.AddListener(() => ShowShopConfirm(def, def.BasePrice, null));
            }
        }

        private void BuildShopBundleCard(ShopItemDefinition def)
        {
            var rColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;
            var claimed = PlayerSave.IsBundleClaimed(def.Id);

            // 已购买行降为偏灰的纸面色（旧的深海军蓝会让墨色副标题不可读）
            var row = UiBuilder.CreateScrollListRow("Bnd_" + def.Id, contentRoot, 180f,
                claimed ? Color.Lerp(UiTheme.PaperBase, UiTheme.MistBlue, 0.35f) : UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject,
                claimed ? UiBuilder.BorderSubtle : new Color(rColor.r, rColor.g, rColor.b, 0.6f),
                new Vector2(1.5f, -1.5f));
            if (!claimed) UiBuilder.AddShadow(row.gameObject, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, -3f));

            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            BuildShopIconBadge(row, def.IconGlyph, theme, new Vector2(50f, -50f));

            var title = UiBuilder.CreateText("Title", row, def.Title, 27, FontStyle.Bold, TextAnchor.UpperLeft,
                claimed ? UiBuilder.TextSecondary : UiBuilder.TextPrimary);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.65f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 32f);
            trt.anchoredPosition = new Vector2(100f, -18f);

            BuildShopRarityChip(row, def.Rarity, new Vector2(100f, -56f));

            if (claimed)
            {
                var doneChip = UiBuilder.CreateRect("DoneChip", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(76f, 28f), new Vector2(190f, -56f),
                    new Color(0.4f, 0.95f, 0.7f, 0.18f), rounded: true);
                UiBuilder.AddOutline(doneChip.gameObject, new Color(0.4f, 0.95f, 0.7f, 0.55f), new Vector2(1f, -1f));
                UiBuilder.CreateText("DoneText", doneChip.transform, "已购买", 16, FontStyle.Bold,
                    TextAnchor.MiddleCenter, new Color(0.4f, 0.95f, 0.7f, 1f));
            }

            var desc = UiBuilder.CreateText("Desc", row, def.Description, 18, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.65f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 48f);
            drt.anchoredPosition = new Vector2(100f, -92f);

            var price = UiBuilder.CreateText("Price", row, $"{def.BasePrice} 鱼干", 25, FontStyle.Bold,
                TextAnchor.MiddleRight, claimed ? UiBuilder.TextTertiary : UiBuilder.WarmGold);
            var prt = (RectTransform)price.transform;
            prt.anchorMin = new Vector2(1f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(1f, 0.5f);
            prt.sizeDelta = new Vector2(180f, 28f);
            prt.anchoredPosition = new Vector2(-20f, 20f);

            if (!claimed)
            {
                BuildShopBuyButton(row, "购买礼盒", theme, def.BasePrice, def, null,
                    new Vector2(180f, 58f), new Vector2(-20f, -28f));
            }
            else
            {
                var doneBtn = UiBuilder.CreateButton("Done", row, "已购买",
                    new Color(0.12f, 0.22f, 0.32f, 0.85f),
                    new Color(1f, 1f, 1f, 0.55f), 21, rounded: true);
                var brt = (RectTransform)doneBtn.transform;
                brt.anchorMin = new Vector2(1f, 0.5f);
                brt.anchorMax = new Vector2(1f, 0.5f);
                brt.pivot = new Vector2(1f, 0.5f);
                brt.sizeDelta = new Vector2(180f, 58f);
                brt.anchoredPosition = new Vector2(-20f, -28f);
                doneBtn.interactable = false;
            }
        }

        private void BuildShopIconBadge(RectTransform parent, string glyph, Color theme, Vector2 anchorPos)
        {
            var badge = UiBuilder.CreateRect("Icon", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(68f, 68f), anchorPos,
                new Color(theme.r, theme.g, theme.b, 0.18f), circle: true);
            UiBuilder.AddOutline(badge.gameObject, new Color(theme.r, theme.g, theme.b, 0.55f), new Vector2(1.5f, -1.5f));
            UiBuilder.CreateText("Glyph", badge.transform, glyph, 32, FontStyle.Bold, TextAnchor.MiddleCenter, theme);
        }

        private void BuildShopRarityChip(RectTransform parent, ShopRarity rarity, Vector2 anchorPos)
        {
            var color = ShopCatalog.RarityColor(rarity);
            var chip = UiBuilder.CreateRect("Rarity", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(80f, 28f), anchorPos,
                new Color(color.r, color.g, color.b, 0.18f), rounded: true);
            UiBuilder.AddOutline(chip.gameObject, new Color(color.r, color.g, color.b, 0.55f), new Vector2(1f, -1f));
            UiBuilder.CreateText("Text", chip.transform, ShopCatalog.RarityLabel(rarity), 16, FontStyle.Bold,
                TextAnchor.MiddleCenter, color);
        }

        private void BuildShopSubsectionHeader(string title)
        {
            var header = UiBuilder.CreateScrollListRow("SubHdr", contentRoot, 44f, null);
            var bar = UiBuilder.CreateRect("Bar", header,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 20f), new Vector2(16f, 0f),
                UiBuilder.AccentActive, rounded: true);
            bar.GetComponent<Image>().raycastTarget = false;
            UiBuilder.CreateText("Label", header, title, 20, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.AccentActive);
            var lrt = (RectTransform)header.transform.Find("Label");
            if (lrt != null)
            {
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(32f, 0f);
                lrt.offsetMax = new Vector2(-12f, 0f);
            }
        }

        private void BuildShopHint(string text)
        {
            var hint = UiBuilder.CreateScrollListRow("Hint", contentRoot, 52f, null);
            var t = UiBuilder.CreateText("Text", hint, "\u2139  " + text, 16, FontStyle.Normal,
                TextAnchor.MiddleLeft, UiBuilder.TextTertiary);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(16f, 0f);
            trt.offsetMax = new Vector2(-16f, 0f);
        }

        private void BuildShopBuyButton(RectTransform row, string label, Color theme, int price,
            ShopItemDefinition def, ShopStore.DailyDeal deal, Vector2 size, Vector2 anchoredPos)
        {
            var canAfford = PlayerSave.TotalFishSnacks >= price;
            var bg = canAfford ? theme : new Color(0.18f, 0.28f, 0.4f, 0.9f);
            var fg = canAfford ? new Color(0.04f, 0.08f, 0.14f, 1f) : new Color(1f, 1f, 1f, 0.55f);

            var btn = UiBuilder.CreateButton("Buy", row, label, bg, fg, 21, rounded: true);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.sizeDelta = size;
            brt.anchoredPosition = anchoredPos;
            btn.interactable = canAfford;

            if (canAfford)
            {
                UiBuilder.AddOutline(btn.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1.5f, -1.5f));
                UiBuilder.AddShadow(btn.gameObject, new Color(theme.r * 0.5f, theme.g * 0.5f, theme.b * 0.5f, 0.5f),
                    new Vector2(0f, -3f));
            }
            btn.onClick.AddListener(() => ShowShopConfirm(def, price, deal));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 商店确认弹窗
        // ─────────────────────────────────────────────────────────────────────────
        private void ShowShopConfirm(ShopItemDefinition def, int price, ShopStore.DailyDeal deal)
        {
            DismissShopConfirm();
            var balance = PlayerSave.TotalFishSnacks;
            if (balance < price)
            {
                dispatch(MainMenuBootstrap.PanelEvent.Toast, $"鱼干不足，还差 {price - balance}");
                return;
            }

            var rt = (RectTransform)transform;
            confirmOverlay = UiBuilder.CreateRect("ConfirmOverlay", rt,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0.02f, 0.06f, 0.78f)).gameObject;
            confirmOverlay.GetComponent<Image>().raycastTarget = true;

            var dialog = UiBuilder.CreateRect("Dialog", confirmOverlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(680f, 520f), Vector2.zero,
                UiBuilder.Surface1, rounded: true);
            UiBuilder.AddOutline(dialog.gameObject, UiTheme.StitchBorder, new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(dialog.gameObject, new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, -8f));

            // 顶部
            var topBar = UiBuilder.CreateRect("Top", dialog,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 70f), new Vector2(0f, 0f),
                Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.22f), rounded: true);
            topBar.pivot = new Vector2(0.5f, 1f);
            UiBuilder.CreateText("Title", topBar, "确认购买", 30, FontStyle.Bold, TextAnchor.MiddleCenter, UiTheme.InkPrimary);

            // 商品信息
            var infoRow = UiBuilder.CreateRect("Info", dialog,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 120f), new Vector2(0f, -80f),
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.34f), rounded: true);
            infoRow.pivot = new Vector2(0.5f, 1f);
            infoRow.offsetMin = new Vector2(24f, infoRow.offsetMin.y);
            infoRow.offsetMax = new Vector2(-24f, infoRow.offsetMax.y);

            BuildShopIconBadge(infoRow, def.IconGlyph, def.ThemeColor, new Vector2(50f, -50f));

            var iTitle = UiBuilder.CreateText("ITitle", infoRow, def.Title, 24, FontStyle.Bold, TextAnchor.UpperLeft, UiTheme.InkPrimary);
            var itrt = (RectTransform)iTitle.transform;
            itrt.anchorMin = new Vector2(0f, 1f);
            itrt.anchorMax = new Vector2(1f, 1f);
            itrt.pivot = new Vector2(0f, 1f);
            itrt.sizeDelta = new Vector2(0f, 30f);
            itrt.anchoredPosition = new Vector2(98f, -14f);

            var iDesc = UiBuilder.CreateText("IDesc", infoRow, def.Description, 17, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var idrt = (RectTransform)iDesc.transform;
            idrt.anchorMin = new Vector2(0f, 1f);
            idrt.anchorMax = new Vector2(1f, 1f);
            idrt.pivot = new Vector2(0f, 1f);
            idrt.sizeDelta = new Vector2(-20f, 60f);
            idrt.anchoredPosition = new Vector2(98f, -48f);

            // 价格区
            var priceBlock = UiBuilder.CreateRect("PriceBlock", dialog,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 170f), new Vector2(0f, 120f),
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.28f), rounded: true);
            priceBlock.pivot = new Vector2(0.5f, 0f);
            priceBlock.offsetMin = new Vector2(24f, priceBlock.offsetMin.y);
            priceBlock.offsetMax = new Vector2(-24f, priceBlock.offsetMax.y);
            UiBuilder.AddOutline(priceBlock.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            BuildPriceLine(priceBlock, "价格", $"{price} 鱼干", UiBuilder.WarmGold, 0f, -14f);
            if (deal != null && deal.OriginalPrice > price)
                BuildPriceLine(priceBlock, "原价", $"{deal.OriginalPrice} 鱼干", UiBuilder.TextTertiary, 0f, -50f);
            BuildPriceLine(priceBlock, "当前持有", $"{balance} 鱼干", UiBuilder.TextSecondary, 0f, -86f);
            BuildPriceLine(priceBlock, "购买后", $"{Mathf.Max(0, balance - price)} 鱼干", UiBuilder.AccentActive, 0f, -122f);

            // 按钮
            var cancelBtn = UiBuilder.CreateButton("Cancel", dialog, "取消",
                UiBuilder.Surface2, UiBuilder.TextPrimary, 22, rounded: true);
            var crt = (RectTransform)cancelBtn.transform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0f, 0f);
            crt.sizeDelta = Vector2.zero;
            crt.offsetMin = new Vector2(24f, 24f);
            crt.offsetMax = new Vector2(-10f, 90f);
            UiBuilder.AddOutline(cancelBtn.gameObject, UiBuilder.BorderDefault, new Vector2(1f, -1f));
            cancelBtn.onClick.AddListener(DismissShopConfirm);

            // 主 CTA：与大厅一致的珊瑚色 + 白字 + 缝线描边
            var confirmBtn = UiBuilder.CreateButton("Confirm", dialog, "确认购买",
                UiTheme.CoralAction, UiBuilder.TextOnAccent, 22, rounded: true);
            var conrt = (RectTransform)confirmBtn.transform;
            conrt.anchorMin = new Vector2(0.5f, 0f);
            conrt.anchorMax = new Vector2(1f, 0f);
            conrt.pivot = new Vector2(0f, 0f);
            conrt.sizeDelta = Vector2.zero;
            conrt.offsetMin = new Vector2(10f, 24f);
            conrt.offsetMax = new Vector2(-24f, 90f);
            UiBuilder.AddOutline(confirmBtn.gameObject, UiTheme.StitchBorder, new Vector2(2f, -2f));
            UiBuilder.AddShadow(confirmBtn.gameObject, UiTheme.ShadowWarm, new Vector2(0f, -4f));
            confirmBtn.onClick.AddListener(() =>
            {
                if (ShopStore.TryPurchase(def, price, out var reason))
                {
                    DismissShopConfirm();
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, $"已购入 {def.Title}");
                    Refresh();
                }
                else
                {
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, reason ?? "购买失败");
                }
            });
        }

        private void BuildPriceLine(RectTransform parent, string label, string value, Color valueColor, float x, float yOffset)
        {
            var lbl = UiBuilder.CreateText("Lbl_" + label, parent, label, 19, FontStyle.Normal, TextAnchor.MiddleLeft, UiBuilder.TextSecondary);
            var lrt = (RectTransform)lbl.transform;
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0f, 1f);
            lrt.sizeDelta = new Vector2(0f, 28f);
            lrt.anchoredPosition = new Vector2(18f, yOffset);

            var val = UiBuilder.CreateText("Val_" + label, parent, value, 21, FontStyle.Bold, TextAnchor.MiddleRight, valueColor);
            var vrt = (RectTransform)val.transform;
            vrt.anchorMin = new Vector2(0.5f, 1f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(1f, 1f);
            vrt.sizeDelta = new Vector2(0f, 28f);
            vrt.anchoredPosition = new Vector2(-18f, yOffset);
        }

        private void DismissShopConfirm()
        {
            if (confirmOverlay != null)
            {
                Destroy(confirmOverlay);
                confirmOverlay = null;
            }
        }

        private static string FormatCountdown(long ms)
        {
            var sec = ms <= 0 ? 0 : ms / 1000;
            var h = sec / 3600;
            var m = (sec % 3600) / 60;
            var s = sec % 60;
            return h > 0 ? $"{h:D2}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 跑酷记录 Tab
        // ─────────────────────────────────────────────────────────────────────────
        private void BuildRecordsTab()
        {
            var stats = RunRecords.GetStats();
            var records = RunRecords.GetRecent(20);
            var personalBest = RunRecords.GetPersonalBest();

            // 统计总览卡片
            BuildRecordsOverview(stats, personalBest);

            // 历史记录列表
            if (records.Count == 0)
            {
                var empty = UiBuilder.CreateScrollListRow("Empty", contentRoot, 200f, null);
                var t = UiBuilder.CreateText("Text", empty, "暂无跑酷记录\n开始一局无尽模式来创建你的首个记录！",
                    22, FontStyle.Bold, TextAnchor.MiddleCenter, UiBuilder.TextSecondary);
                var trt = (RectTransform)t.transform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(20f, 0f);
                trt.offsetMax = new Vector2(-20f, 0f);
            }
            else
            {
                // 列表标题
                var listHeader = UiBuilder.CreateScrollListRow("ListHeader", contentRoot, 40f, null);
                var bar = UiBuilder.CreateRect("Bar", listHeader,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(4f, 20f), new Vector2(12f, 0f),
                    UiBuilder.WarmGold, rounded: true);
                bar.GetComponent<Image>().raycastTarget = false;
                UiBuilder.CreateText("Label", listHeader, "最近战绩",
                    20, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.WarmGold);
                var lrt = (RectTransform)listHeader.transform.Find("Label");
                if (lrt != null)
                {
                    lrt.anchorMin = new Vector2(0f, 0f);
                    lrt.anchorMax = new Vector2(1f, 1f);
                    lrt.offsetMin = new Vector2(28f, 0f);
                    lrt.offsetMax = new Vector2(-12f, 0f);
                }

                foreach (var r in records)
                    BuildRecordRow(r);
            }
        }

        private void BuildRecordsOverview(RunRecords.StatsSummary stats, RunRecords.Record best)
        {
            var row = UiBuilder.CreateScrollListRow("RecOverview", contentRoot, 280f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            UiBuilder.AddShadow(row.gameObject, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, -3f));

            // 重音条
            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                UiBuilder.WarmGold, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 标题
            var title = UiBuilder.CreateText("Title", row, "战绩统计", 26, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.TextPrimary);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 34f);
            trt.anchoredPosition = new Vector2(20f, -14f);

            // 六格统计（3 × 2行）；图标统一用可渲染的 ★（\u279E/\u2726/\u2615 是豆腐块），靠配色区分
            BuildRecordStatCell(row, 0.02f, 0.65f, "总场次", $"{stats.TotalRuns}", "\u2605", new Color(0.55f, 0.85f, 1f, 1f));
            BuildRecordStatCell(row, 0.34f, 0.65f, "最高得分", $"{stats.BestScore:N0}", "\u2605", UiBuilder.WarmGold);
            BuildRecordStatCell(row, 0.66f, 0.65f, "最远距离", $"{stats.BestDistance}m", "\u2605", new Color(1f, 0.55f, 0.35f, 1f));
            BuildRecordStatCell(row, 0.02f, 0.18f, "平均得分", $"{stats.AverageScore:N0}", "\u2605", new Color(0.4f, 0.95f, 0.7f, 1f));

            // 个人巅峰
            if (best != null)
            {
                var bestBg = UiBuilder.CreateRect("BestBg", row,
                    new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0f, 70f), new Vector2(20f, 78f),
                    new Color(0.04f, 0.08f, 0.14f, 0.7f), rounded: true);
                bestBg.offsetMax = new Vector2(-20f, bestBg.offsetMax.y);
                UiBuilder.AddOutline(bestBg.gameObject, new Color(1f, 0.84f, 0.32f, 0.5f), new Vector2(1f, -1f));

                var crown = UiBuilder.CreateRect("Crown", bestBg,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(44f, 44f), new Vector2(16f, 0f),
                    new Color(1f, 0.84f, 0.32f, 0.25f), circle: true);
                UiBuilder.AddOutline(crown.gameObject, UiBuilder.WarmGold, new Vector2(1.5f, -1.5f));
                UiBuilder.CreateText("CrownText", crown.transform, "\u2605", 24, FontStyle.Bold,
                    TextAnchor.MiddleCenter, UiBuilder.WarmGold);

                var bestTitle = UiBuilder.CreateText("BestTitle", bestBg, "个人巅峰",
                    18, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.WarmGold);
                var btrt = (RectTransform)bestTitle.transform;
                btrt.anchorMin = new Vector2(0f, 1f);
                btrt.anchorMax = new Vector2(1f, 1f);
                btrt.pivot = new Vector2(0f, 1f);
                btrt.sizeDelta = new Vector2(0f, 24f);
                btrt.anchoredPosition = new Vector2(68f, -10f);

                var bestValue = UiBuilder.CreateText("BestValue", bestBg,
                    $"{best.score:N0} 分 · {best.distanceMeters}m · {Mathf.RoundToInt(best.survivalSeconds)}秒",
                    22, FontStyle.Bold, TextAnchor.LowerLeft, Color.white);
                var bvrt = (RectTransform)bestValue.transform;
                bvrt.anchorMin = new Vector2(0f, 0f);
                bvrt.anchorMax = new Vector2(1f, 1f);
                bvrt.pivot = new Vector2(0f, 0f);
                bvrt.sizeDelta = new Vector2(-80f, 28f);
                bvrt.anchoredPosition = new Vector2(68f, 8f);

                var bestDate = UiBuilder.CreateText("BestDate", bestBg,
                    best.Timestamp.ToString("MM-dd HH:mm"),
                    15, FontStyle.Normal, TextAnchor.LowerRight, UiBuilder.TextTertiary);
                var bdrt = (RectTransform)bestDate.transform;
                bdrt.anchorMin = new Vector2(1f, 0f);
                bdrt.anchorMax = new Vector2(1f, 1f);
                bdrt.pivot = new Vector2(1f, 0f);
                bdrt.sizeDelta = new Vector2(100f, 20f);
                bdrt.anchoredPosition = new Vector2(-14f, 8f);
            }
            else
            {
                var emptyBest = UiBuilder.CreateText("EmptyBest", row,
                    "完成一局跑酷来创建你的首个记录！",
                    18, FontStyle.Normal, TextAnchor.MiddleCenter, UiBuilder.TextSecondary);
                var ebrt = (RectTransform)emptyBest.transform;
                ebrt.anchorMin = new Vector2(0f, 0f);
                ebrt.anchorMax = new Vector2(1f, 0f);
                ebrt.pivot = new Vector2(0.5f, 0f);
                ebrt.sizeDelta = new Vector2(0f, 70f);
                ebrt.anchoredPosition = new Vector2(0f, 78f);
            }
        }

        private void BuildRecordStatCell(RectTransform parent, float anchorX, float anchorY, string label, string value, string icon, Color color)
        {
            var cell = UiBuilder.CreateRect("Stat_" + label, parent,
                new Vector2(anchorX, anchorY), new Vector2(anchorX + 0.30f, anchorY + 0.42f),
                Vector2.zero, new Vector2(0f, 10f),
                new Color(0.04f, 0.08f, 0.14f, 0.5f));
            cell.pivot = new Vector2(0f, 0.5f);
            cell.sizeDelta = new Vector2(-12f, 52f);
            UiBuilder.AddOutline(cell.gameObject, new Color(color.r, color.g, color.b, 0.35f), new Vector2(1f, -1f));

            // 图标
            var iconBg = UiBuilder.CreateRect("IconBg", cell,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(30f, 30f), new Vector2(8f, 0f),
                new Color(color.r, color.g, color.b, 0.2f), circle: true);
            UiBuilder.CreateText("Icon", iconBg.transform, icon, 15, FontStyle.Bold, TextAnchor.MiddleCenter, color);

            var lbl = UiBuilder.CreateText("Label", cell, label, 13, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var lrt = (RectTransform)lbl.transform;
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0f, 1f);
            lrt.sizeDelta = new Vector2(-8f, 16f);
            lrt.anchoredPosition = new Vector2(44f, -4f);

            var val = UiBuilder.CreateText("Value", cell, value, 18, FontStyle.Bold, TextAnchor.LowerLeft, color);
            var vrt = (RectTransform)val.transform;
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0f, 0f);
            vrt.sizeDelta = new Vector2(-8f, 22f);
            vrt.anchoredPosition = new Vector2(44f, 2f);
        }

        private void BuildRecordRow(RunRecords.Record r)
        {
            var isBest = r.wasNewBest;
            var row = UiBuilder.CreateScrollListRow("Rec_" + r.timestampMillis, contentRoot, 110f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject,
                isBest ? new Color(1f, 0.84f, 0.32f, 0.55f) : UiBuilder.BorderSubtle,
                new Vector2(1f, -1f));
            if (isBest) UiBuilder.AddShadow(row.gameObject, new Color(1f, 0.7f, 0.2f, 0.15f), new Vector2(0f, -2f));

            // 左侧重音（破纪录用金色）
            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                isBest ? UiBuilder.WarmGold : UiBuilder.AccentActive, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 日期
            var date = UiBuilder.CreateText("Date", row, r.Timestamp.ToString("MM-dd HH:mm"),
                17, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)date.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.4f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 24f);
            drt.anchoredPosition = new Vector2(18f, -12f);

            // 模式标签
            var modeText = r.mode == "daily" ? "每日" : "无尽";
            var modeColor = r.mode == "daily" ? new Color(0.85f, 0.55f, 1f, 1f) : new Color(0.55f, 0.85f, 1f, 1f);
            var modeChip = UiBuilder.CreateRect("Mode", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(56f, 24f), new Vector2(18f, -42f),
                new Color(modeColor.r, modeColor.g, modeColor.b, 0.2f), rounded: true);
            UiBuilder.AddOutline(modeChip.gameObject, new Color(modeColor.r, modeColor.g, modeColor.b, 0.5f), new Vector2(1f, -1f));
            UiBuilder.CreateText("ModeText", modeChip.transform, modeText, 14, FontStyle.Bold, TextAnchor.MiddleCenter, modeColor);

            // 破纪录徽章
            if (isBest)
            {
                var bestBadge = UiBuilder.CreateRect("BestBadge", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(70f, 26f), new Vector2(82f, -40f),
                    new Color(1f, 0.84f, 0.32f, 0.22f), rounded: true);
                UiBuilder.AddOutline(bestBadge.gameObject, UiBuilder.WarmGold, new Vector2(1f, -1f));
                UiBuilder.CreateText("BestText", bestBadge.transform, "\u2605 新纪录", 14, FontStyle.Bold,
                    TextAnchor.MiddleCenter, UiBuilder.WarmGold);
            }

            // 三列数据：得分 / 距离 / 鱼干
            BuildRecordValueCell(row, 0.38f, "得分", $"{r.score:N0}", isBest ? UiBuilder.WarmGold : UiBuilder.TextPrimary);
            BuildRecordValueCell(row, 0.58f, "距离", $"{r.distanceMeters}m", UiBuilder.TextSecondary);
            BuildRecordValueCell(row, 0.78f, "鱼干", $"{r.fishSnacks}", UiBuilder.WarmGold);
        }

        private void BuildRecordValueCell(RectTransform parent, float anchorX, string label, string value, Color valueColor)
        {
            var cell = UiBuilder.CreateRect("Cell_" + label, parent,
                new Vector2(anchorX, 0.5f), new Vector2(anchorX + 0.18f, 0.5f),
                Vector2.zero, Vector2.zero, null);
            cell.pivot = new Vector2(0f, 0.5f);

            var val = UiBuilder.CreateText("Value", cell, value, 24, FontStyle.Bold, TextAnchor.UpperLeft, valueColor);
            var vrt = (RectTransform)val.transform;
            vrt.anchorMin = new Vector2(0f, 0.5f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0f, 0.5f);
            vrt.sizeDelta = new Vector2(0f, 28f);
            vrt.anchoredPosition = new Vector2(0f, 0f);

            var lbl = UiBuilder.CreateText("Label", cell, label, 14, FontStyle.Normal, TextAnchor.LowerLeft, UiBuilder.TextSecondary);
            var lrt = (RectTransform)lbl.transform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(0f, 18f);
            lrt.anchoredPosition = new Vector2(0f, 0f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Update（商店倒计时）
        // ─────────────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (activeTab != MainTab.Shop || activeShopCategory != ShopCategory.DailyDeals) return;
            if (countdownText == null) return;
            var ms = ShopStore.MillisUntilNextRefresh();
            var sec = ms <= 0 ? 0L : ms / 1000L;
            if (sec == lastCountdownSeconds) return;
            lastCountdownSeconds = sec;
            countdownText.text = FormatCountdown(ms);
        }
    }
}
