using System.Collections.Generic;
using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 极光商店：4 Tab（特惠 / 道具 / 装扮 / 礼盒），统一卡片样式 + 购买确认弹窗 + 库存与解锁状态。
    /// 货币只有鱼干，购买流程走 <see cref="ShopStore"/>，全部本地存档。
    /// </summary>
    public sealed class ShopPanel : MonoBehaviour
    {
        private System.Action<MainMenuBootstrap.PanelEvent, string> dispatch;
        private RectTransform contentRoot;
        private RectTransform statusBar;
        private Text statusInventoryText;
        private Text statusBalanceText;
        private RectTransform countdownContainer;
        private Text countdownText;
        private float countdownTickAcc;

        private ShopCategory activeTab = ShopCategory.DailyDeals;
        private readonly List<(Button btn, Image bg, Text text, ShopCategory tab)> tabs = new();

        private GameObject confirmOverlay;

        private const float TabsHeight = 80f;
        private const float StatusHeight = 70f;
        private const float TopGap = 12f;

        public static GameObject Build(Transform parent, System.Action<MainMenuBootstrap.PanelEvent, string> dispatch)
        {
            var rootRt = UiBuilder.CreateRect(
                "ShopPanel", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                UiBuilder.MenuPanelSolidBg);
            var script = rootRt.gameObject.AddComponent<ShopPanel>();
            script.dispatch = dispatch;
            script.BuildLayout(rootRt);
            script.Refresh();
            return rootRt.gameObject;
        }

        private void BuildLayout(RectTransform parent)
        {
            PanelHeader.Create(parent, "极光商店", "用鱼干兑换装扮、道具与礼盒",
                () => dispatch(MainMenuBootstrap.PanelEvent.Close, null));

            BuildTabs(parent);
            BuildStatusBar(parent);

            var topInset = UiBuilder.PanelHeaderHeightPixels + TopGap + TabsHeight + TopGap + StatusHeight + TopGap;
            UiBuilder.CreatePanelScrollList(parent, out var content, topInset: topInset, bottomInset: 56f);
            contentRoot = content;

            // 给 content 子节点加点底部留白，让最后一行不顶到 dock
            var vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.padding = new RectOffset(20, 20, 14, 24);
                vlg.spacing = 14f;
            }
        }

        private void BuildTabs(RectTransform parent)
        {
            var top = UiBuilder.PanelHeaderHeightPixels + TopGap;
            var tabsContainer = UiBuilder.CreateRect("ShopTabs", parent,
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
            AddTab(tabsContainer, "特惠", ShopCategory.DailyDeals);
            AddTab(tabsContainer, "道具", ShopCategory.Boosters);
            AddTab(tabsContainer, "装扮", ShopCategory.Cosmetics);
            AddTab(tabsContainer, "礼盒", ShopCategory.Bundles);
            ApplyTabStyle();
        }

        private void AddTab(RectTransform parent, string label, ShopCategory tab)
        {
            var btn = UiBuilder.CreateButton("Tab_" + tab, parent.transform, label,
                Color.clear, UiBuilder.TextSecondary, 24, rounded: true);
            var img = btn.GetComponent<Image>();
            var text = btn.GetComponentInChildren<Text>();
            tabs.Add((btn, img, text, tab));
            btn.onClick.AddListener(() =>
            {
                activeTab = tab;
                ApplyTabStyle();
                Refresh();
            });
        }

        private void ApplyTabStyle()
        {
            foreach (var (btn, img, text, tab) in tabs)
            {
                if (btn == null) continue;
                var sel = tab == activeTab;
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

        private void BuildStatusBar(RectTransform parent)
        {
            var top = UiBuilder.PanelHeaderHeightPixels + TopGap + TabsHeight + TopGap;
            statusBar = UiBuilder.CreateRect("ShopStatus", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, StatusHeight), new Vector2(0f, -top),
                UiBuilder.Surface1, rounded: true);
            statusBar.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(statusBar.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            // 左侧：库存摘要
            var invIcon = UiBuilder.CreateRect("InvIcon", statusBar,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(36f, 36f), new Vector2(28f, 0f),
                new Color(0f, 0.85f, 0.95f, 0.22f), circle: true);
            UiBuilder.AddOutline(invIcon.gameObject, new Color(0f, 0.85f, 0.95f, 0.55f), new Vector2(1f, -1f));
            UiBuilder.CreateText("InvIconText", invIcon.transform, "\u25EB", 20, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentCyan);

            statusInventoryText = UiBuilder.CreateText("InvText", statusBar.transform,
                "持有道具：—", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            var srt = (RectTransform)statusInventoryText.transform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0.65f, 1f);
            srt.offsetMin = new Vector2(74f, 4f);
            srt.offsetMax = new Vector2(-12f, -4f);

            // 右侧：当前鱼干余额
            statusBalanceText = UiBuilder.CreateText("Balance", statusBar.transform,
                "—", 22, FontStyle.Bold, TextAnchor.MiddleRight, UiBuilder.WarmGold);
            var brt = (RectTransform)statusBalanceText.transform;
            brt.anchorMin = new Vector2(0.55f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(8f, 4f);
            brt.offsetMax = new Vector2(-24f, -4f);
        }

        private void Refresh()
        {
            // 顶栏鱼干刷新
            var fishText = transform.Find("Header/CurrencyPill/Value")?.GetComponent<Text>();
            if (fishText != null) fishText.text = $"{PlayerSave.TotalFishSnacks}";

            // 状态条
            if (statusInventoryText != null)
            {
                var df = ShopStore.InventoryCount(ShopItemKind.BoosterDoubleFish);
                var sb = ShopStore.InventoryCount(ShopItemKind.BoosterScoreBoost);
                var ls = ShopStore.InventoryCount(ShopItemKind.BoosterLuckyStart);
                if (df + sb + ls == 0)
                    statusInventoryText.text = "持有道具：暂无消耗券";
                else
                    statusInventoryText.text = $"持有道具：双倍 ×{df} · 加分 ×{sb} · 幸运 ×{ls}";
            }
            if (statusBalanceText != null)
                statusBalanceText.text = $"鱼干 {PlayerSave.TotalFishSnacks}";

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }

            switch (activeTab)
            {
                case ShopCategory.DailyDeals: BuildDailyDealsTab(); break;
                case ShopCategory.Boosters:   BuildBoostersTab(); break;
                case ShopCategory.Cosmetics:  BuildCosmeticsTab(); break;
                case ShopCategory.Bundles:    BuildBundlesTab(); break;
            }

            UiBuilder.RebuildScrollContent(contentRoot);
        }

        // ── 特惠 Tab ───────────────────────────────────────────────────────────

        private void BuildDailyDealsTab()
        {
            // 顶部说明 + 刷新倒计时
            BuildBanner("每日特惠", "每日凌晨刷新，限时折扣价。",
                new Color(1f, 0.78f, 0.32f, 1f),
                showCountdown: true);

            var deals = ShopStore.GetDailyDeals();
            foreach (var d in deals)
            {
                BuildDealCard(d);
            }

            BuildHint("打折商品可与营地强化叠加：例如鱼干收益 + 双倍券 = 1.3 × 2 倍鱼干。");
        }

        private void BuildDealCard(ShopStore.DailyDeal deal)
        {
            var def = deal.Item;
            var rarityColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;

            var row = UiBuilder.CreateScrollListRow("Deal_" + def.Id, contentRoot, 200f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.55f),
                new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(row.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -3f));

            // 左侧重音
            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rarityColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 图标徽章
            BuildIconBadge(row, def.IconGlyph, theme, new Vector2(54f, -56f));

            // 标题 + 折扣 chip
            var title = UiBuilder.CreateText("Title", row, def.Title,
                28, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.7f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 36f);
            trt.anchoredPosition = new Vector2(108f, -22f);

            var dealChip = UiBuilder.CreateRect("DealChip", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(86f, 30f), new Vector2(108f, -62f),
                new Color(1f, 0.45f, 0.55f, 0.22f), rounded: true);
            UiBuilder.AddOutline(dealChip.gameObject, new Color(1f, 0.45f, 0.55f, 0.55f),
                new Vector2(1f, -1f));
            UiBuilder.CreateText("DealText", dealChip.transform,
                $"-{deal.DiscountPercent}%", 18, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.6f, 1f));

            BuildRarityChip(row, def.Rarity, new Vector2(204f, -62f));

            // 描述
            var desc = UiBuilder.CreateText("Desc", row, def.Description,
                19, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.7f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 56f);
            drt.anchoredPosition = new Vector2(108f, -100f);

            // 价格区：原价（划线） + 折后价
            var origText = UiBuilder.CreateText("OrigPrice", row,
                $"<s>{deal.OriginalPrice}</s>", 18, FontStyle.Normal,
                TextAnchor.LowerRight, new Color(0.6f, 0.7f, 0.82f, 0.7f));
            origText.supportRichText = true;
            var ort = (RectTransform)origText.transform;
            ort.anchorMin = new Vector2(1f, 0.5f);
            ort.anchorMax = new Vector2(1f, 0.5f);
            ort.pivot = new Vector2(1f, 0f);
            ort.sizeDelta = new Vector2(220f, 24f);
            ort.anchoredPosition = new Vector2(-24f, 28f);

            var priceText = UiBuilder.CreateText("Price", row,
                $"{deal.DiscountedPrice} 鱼干", 26, FontStyle.Bold,
                TextAnchor.MiddleRight, UiBuilder.WarmGold);
            var prt = (RectTransform)priceText.transform;
            prt.anchorMin = new Vector2(1f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(1f, 0.5f);
            prt.sizeDelta = new Vector2(220f, 32f);
            prt.anchoredPosition = new Vector2(-24f, 4f);

            BuildBuyButton(row, "立即购买", theme, deal.DiscountedPrice, def, deal,
                new Vector2(220f, 60f), new Vector2(-24f, -34f));
        }

        // ── 道具 Tab ───────────────────────────────────────────────────────────

        private void BuildBoostersTab()
        {
            BuildBanner("局内消耗券", "购入后会在下一局自动消耗，效果仅本局生效。",
                new Color(1f, 0.82f, 0.35f, 1f));

            foreach (var def in ShopCatalog.InCategory(ShopCategory.Boosters))
            {
                if (def.BasePrice <= 0) continue; // 鱼干包仅在限时特惠中出现
                BuildBoosterCard(def);
            }
        }

        private void BuildBoosterCard(ShopItemDefinition def)
        {
            var rarityColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;
            var stock = ShopStore.InventoryCount(def.Kind);

            var row = UiBuilder.CreateScrollListRow("Item_" + def.Id, contentRoot, 188f, UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.45f),
                new Vector2(1f, -1f));

            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rarityColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            BuildIconBadge(row, def.IconGlyph, theme, new Vector2(54f, -56f));

            var title = UiBuilder.CreateText("Title", row, def.Title,
                26, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.7f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 32f);
            trt.anchoredPosition = new Vector2(108f, -20f);

            BuildRarityChip(row, def.Rarity, new Vector2(108f, -56f));

            // 库存 chip
            var stockChip = UiBuilder.CreateRect("StockChip", row,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(82f, 30f), new Vector2(204f, -56f),
                new Color(0f, 0.85f, 0.95f, 0.18f), rounded: true);
            UiBuilder.AddOutline(stockChip.gameObject, new Color(0f, 0.85f, 0.95f, 0.45f),
                new Vector2(1f, -1f));
            UiBuilder.CreateText("StockText", stockChip.transform,
                $"持有 ×{stock}", 17, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentCyan);

            var desc = UiBuilder.CreateText("Desc", row, def.Description,
                19, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.7f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 56f);
            drt.anchoredPosition = new Vector2(108f, -94f);

            var priceText = UiBuilder.CreateText("Price", row,
                $"{def.BasePrice} 鱼干", 24, FontStyle.Bold,
                TextAnchor.MiddleRight, UiBuilder.WarmGold);
            var prt = (RectTransform)priceText.transform;
            prt.anchorMin = new Vector2(1f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(1f, 0.5f);
            prt.sizeDelta = new Vector2(220f, 30f);
            prt.anchoredPosition = new Vector2(-24f, 18f);

            BuildBuyButton(row, "购买", theme, def.BasePrice, def, null,
                new Vector2(220f, 56f), new Vector2(-24f, -22f));
        }

        // ── 装扮 Tab ───────────────────────────────────────────────────────────

        private void BuildCosmeticsTab()
        {
            BuildBanner("装扮 · 围巾与帽子", "解锁后即可在大厅企鹅吉祥物上切换显示。",
                new Color(0.78f, 0.55f, 1f, 1f));

            BuildSubsectionHeader("围巾色调");
            foreach (var def in ShopCatalog.InCategory(ShopCategory.Cosmetics))
            {
                if (def.Kind != ShopItemKind.CosmeticScarf) continue;
                BuildCosmeticCard(def);
            }

            BuildSubsectionHeader("帽子样式");
            foreach (var def in ShopCatalog.InCategory(ShopCategory.Cosmetics))
            {
                if (def.Kind != ShopItemKind.CosmeticHat) continue;
                BuildCosmeticCard(def);
            }
        }

        private void BuildCosmeticCard(ShopItemDefinition def)
        {
            var rarityColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;
            var owned = ShopStore.IsOwned(def);
            var equipped = ShopStore.IsEquipped(def);

            var row = UiBuilder.CreateScrollListRow("Cos_" + def.Id, contentRoot, 156f,
                equipped ? new Color(0.07f, 0.22f, 0.32f, 1f) : UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject,
                equipped ? UiBuilder.BorderAccent : new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.4f),
                new Vector2(1f, -1f));

            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rarityColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 颜色样本（取代图标徽章）
            var swatch = UiBuilder.CreateRect("Swatch", row,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, 72f), new Vector2(54f, 0f),
                theme, circle: true);
            UiBuilder.AddOutline(swatch.gameObject, Color.white, new Vector2(2f, -2f));
            UiBuilder.AddShadow(swatch.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -3f));

            var title = UiBuilder.CreateText("Title", row, def.Title,
                26, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.65f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 32f);
            trt.anchoredPosition = new Vector2(108f, -22f);

            BuildRarityChip(row, def.Rarity, new Vector2(108f, -58f));

            if (equipped)
            {
                var eqChip = UiBuilder.CreateRect("EquippedChip", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(82f, 30f), new Vector2(204f, -58f),
                    new Color(0f, 0.85f, 0.95f, 0.22f), rounded: true);
                UiBuilder.AddOutline(eqChip.gameObject, new Color(0f, 0.85f, 0.95f, 0.6f), new Vector2(1f, -1f));
                UiBuilder.CreateText("EqText", eqChip.transform,
                    "已装备", 17, FontStyle.Bold,
                    TextAnchor.MiddleCenter, UiBuilder.AccentCyan);
            }
            else if (owned)
            {
                var ownedChip = UiBuilder.CreateRect("OwnedChip", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(82f, 30f), new Vector2(204f, -58f),
                    new Color(0.4f, 0.95f, 0.7f, 0.18f), rounded: true);
                UiBuilder.AddOutline(ownedChip.gameObject, new Color(0.4f, 0.95f, 0.7f, 0.55f), new Vector2(1f, -1f));
                UiBuilder.CreateText("OwnedText", ownedChip.transform,
                    "已解锁", 17, FontStyle.Bold,
                    TextAnchor.MiddleCenter, new Color(0.4f, 0.95f, 0.7f, 1f));
            }

            var desc = UiBuilder.CreateText("Desc", row, def.Description,
                18, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.65f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 48f);
            drt.anchoredPosition = new Vector2(108f, -94f);

            // 操作按钮：未拥有→购买；已拥有未装备→装备；已装备→灰
            string label;
            Color btnBg;
            Color btnFg;
            if (equipped)
            {
                label = "已装备";
                btnBg = new Color(0.12f, 0.22f, 0.32f, 0.85f);
                btnFg = new Color(1f, 1f, 1f, 0.55f);
            }
            else if (owned)
            {
                label = "装备";
                btnBg = UiBuilder.AccentCyan;
                btnFg = new Color(0.04f, 0.08f, 0.14f, 1f);
            }
            else if (def.BasePrice <= 0)
            {
                label = "默认";
                btnBg = new Color(0.12f, 0.22f, 0.32f, 0.85f);
                btnFg = new Color(1f, 1f, 1f, 0.55f);
            }
            else
            {
                label = $"{def.BasePrice} 鱼干";
                btnBg = theme;
                btnFg = new Color(0.04f, 0.08f, 0.14f, 1f);
            }

            var btn = UiBuilder.CreateButton("Action", row, label, btnBg, btnFg, 22, rounded: true);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.sizeDelta = new Vector2(180f, 64f);
            brt.anchoredPosition = new Vector2(-24f, 0f);

            if (equipped || (def.BasePrice <= 0 && !owned))
            {
                btn.interactable = false;
            }
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
                    btn.GetComponentInChildren<Text>().color = new Color(1f, 1f, 1f, 0.55f);
                }
                else
                {
                    UiBuilder.AddOutline(btn.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1.5f, -1.5f));
                    UiBuilder.AddShadow(btn.gameObject,
                        new Color(theme.r * 0.5f, theme.g * 0.5f, theme.b * 0.5f, 0.5f),
                        new Vector2(0f, -3f));
                }
                btn.onClick.AddListener(() => ShowConfirmDialog(def, def.BasePrice, null));
            }
        }

        // ── 礼盒 Tab ───────────────────────────────────────────────────────────

        private void BuildBundlesTab()
        {
            BuildBanner("珍藏礼盒", "每款仅可购买一次，附带额外鱼干与多种道具。",
                new Color(1f, 0.78f, 0.32f, 1f));

            foreach (var def in ShopCatalog.InCategory(ShopCategory.Bundles))
            {
                BuildBundleCard(def);
            }
        }

        private void BuildBundleCard(ShopItemDefinition def)
        {
            var rarityColor = ShopCatalog.RarityColor(def.Rarity);
            var theme = def.ThemeColor;
            var claimed = PlayerSave.IsBundleClaimed(def.Id);

            var row = UiBuilder.CreateScrollListRow("Bnd_" + def.Id, contentRoot, 200f,
                claimed ? new Color(0.06f, 0.14f, 0.22f, 0.95f) : UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject,
                claimed ? UiBuilder.BorderSubtle : new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.6f),
                new Vector2(1.5f, -1.5f));
            if (!claimed)
                UiBuilder.AddShadow(row.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -3f));

            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                rarityColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            BuildIconBadge(row, def.IconGlyph, theme, new Vector2(54f, -56f));

            var title = UiBuilder.CreateText("Title", row, def.Title,
                28, FontStyle.Bold, TextAnchor.UpperLeft,
                claimed ? UiBuilder.TextSecondary : Color.white);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.7f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 36f);
            trt.anchoredPosition = new Vector2(108f, -22f);

            BuildRarityChip(row, def.Rarity, new Vector2(108f, -62f));

            if (claimed)
            {
                var doneChip = UiBuilder.CreateRect("DoneChip", row,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(82f, 30f), new Vector2(204f, -62f),
                    new Color(0.4f, 0.95f, 0.7f, 0.18f), rounded: true);
                UiBuilder.AddOutline(doneChip.gameObject, new Color(0.4f, 0.95f, 0.7f, 0.55f),
                    new Vector2(1f, -1f));
                UiBuilder.CreateText("DoneText", doneChip.transform,
                    "已购入", 17, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Color(0.4f, 0.95f, 0.7f, 1f));
            }

            var desc = UiBuilder.CreateText("Desc", row, def.Description,
                19, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.7f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 64f);
            drt.anchoredPosition = new Vector2(108f, -100f);

            var priceText = UiBuilder.CreateText("Price", row,
                $"{def.BasePrice} 鱼干", 26, FontStyle.Bold,
                TextAnchor.MiddleRight, claimed ? UiBuilder.TextTertiary : UiBuilder.WarmGold);
            var prt = (RectTransform)priceText.transform;
            prt.anchorMin = new Vector2(1f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(1f, 0.5f);
            prt.sizeDelta = new Vector2(220f, 30f);
            prt.anchoredPosition = new Vector2(-24f, 22f);

            if (!claimed)
            {
                BuildBuyButton(row, "购买礼盒", theme, def.BasePrice, def, null,
                    new Vector2(220f, 60f), new Vector2(-24f, -32f));
            }
            else
            {
                var done = UiBuilder.CreateButton("Done", row, "已购入",
                    new Color(0.12f, 0.22f, 0.32f, 0.85f),
                    new Color(1f, 1f, 1f, 0.55f), 22, rounded: true);
                var brt = (RectTransform)done.transform;
                brt.anchorMin = new Vector2(1f, 0.5f);
                brt.anchorMax = new Vector2(1f, 0.5f);
                brt.pivot = new Vector2(1f, 0.5f);
                brt.sizeDelta = new Vector2(220f, 60f);
                brt.anchoredPosition = new Vector2(-24f, -32f);
                done.interactable = false;
            }
        }

        // ── 公共 UI ────────────────────────────────────────────────────────────

        private void BuildBanner(string title, string desc, Color themeColor, bool showCountdown = false)
        {
            var banner = UiBuilder.CreateScrollListRow("Banner", contentRoot, 110f, UiBuilder.Surface1);
            UiBuilder.AddOutline(banner.gameObject,
                new Color(themeColor.r, themeColor.g, themeColor.b, 0.45f), new Vector2(1f, -1f));

            // 左侧重音
            var accent = UiBuilder.CreateRect("Accent", banner,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                themeColor, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            var title3 = UiBuilder.CreateText("Title", banner, title,
                28, FontStyle.Bold, TextAnchor.UpperLeft, themeColor);
            var trt = (RectTransform)title3.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 36f);
            trt.anchoredPosition = new Vector2(20f, -16f);

            var desc2 = UiBuilder.CreateText("Desc", banner, desc,
                19, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)desc2.transform;
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(showCountdown ? 0.6f : 1f, 0.5f);
            drt.pivot = new Vector2(0f, 0f);
            drt.sizeDelta = new Vector2(0f, 38f);
            drt.anchoredPosition = new Vector2(20f, 14f);

            if (showCountdown)
            {
                countdownContainer = UiBuilder.CreateRect("Countdown", banner.transform,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(220f, 44f), new Vector2(-24f, -8f),
                    new Color(0.04f, 0.09f, 0.16f, 1f), rounded: true);
                UiBuilder.AddOutline(countdownContainer.gameObject,
                    new Color(themeColor.r, themeColor.g, themeColor.b, 0.5f), new Vector2(1f, -1f));

                var label = UiBuilder.CreateText("Label", countdownContainer.transform,
                    "刷新", 17, FontStyle.Normal, TextAnchor.MiddleLeft,
                    UiBuilder.TextSecondary);
                var lrt = (RectTransform)label.transform;
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(0.4f, 1f);
                lrt.offsetMin = new Vector2(14f, 0f);
                lrt.offsetMax = new Vector2(0f, 0f);

                countdownText = UiBuilder.CreateText("Value", countdownContainer.transform,
                    "—", 22, FontStyle.Bold, TextAnchor.MiddleRight, themeColor);
                var crt = (RectTransform)countdownText.transform;
                crt.anchorMin = new Vector2(0.4f, 0f);
                crt.anchorMax = new Vector2(1f, 1f);
                crt.offsetMin = new Vector2(8f, 0f);
                crt.offsetMax = new Vector2(-14f, 0f);

                countdownText.text = FormatCountdown(ShopStore.MillisUntilNextRefresh());
            }
        }

        private void BuildSubsectionHeader(string title)
        {
            var header = UiBuilder.CreateScrollListRow("Sub_" + title, contentRoot, 46f, null);
            var bar = UiBuilder.CreateRect("Bar", header,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 22f), new Vector2(20f, 0f),
                UiBuilder.AccentCyan, rounded: true);
            bar.GetComponent<Image>().raycastTarget = false;

            var t = UiBuilder.CreateText("Label", header, title,
                22, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.AccentCyan);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(36f, 0f);
            trt.offsetMax = new Vector2(-20f, 0f);
        }

        private void BuildHint(string text)
        {
            var hint = UiBuilder.CreateScrollListRow("Hint", contentRoot, 56f, null);
            var t = UiBuilder.CreateText("Text", hint, "\u2139  " + text,
                17, FontStyle.Normal, TextAnchor.MiddleLeft, UiBuilder.TextTertiary);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(20f, 0f);
            trt.offsetMax = new Vector2(-20f, 0f);
        }

        private static void BuildIconBadge(RectTransform parent, string glyph, Color theme, Vector2 anchorPos)
        {
            var iconBadge = UiBuilder.CreateRect("IconBadge", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(72f, 72f), anchorPos,
                new Color(theme.r, theme.g, theme.b, 0.18f), circle: true);
            UiBuilder.AddOutline(iconBadge.gameObject,
                new Color(theme.r, theme.g, theme.b, 0.6f), new Vector2(1.5f, -1.5f));
            UiBuilder.CreateText("Icon", iconBadge.transform, glyph, 32, FontStyle.Bold,
                TextAnchor.MiddleCenter, theme);
        }

        private static void BuildRarityChip(RectTransform parent, ShopRarity rarity, Vector2 anchorPos)
        {
            var color = ShopCatalog.RarityColor(rarity);
            var chip = UiBuilder.CreateRect("RarityChip", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(86f, 30f), anchorPos,
                new Color(color.r, color.g, color.b, 0.18f), rounded: true);
            UiBuilder.AddOutline(chip.gameObject, new Color(color.r, color.g, color.b, 0.55f),
                new Vector2(1f, -1f));
            UiBuilder.CreateText("Text", chip.transform, ShopCatalog.RarityLabel(rarity),
                17, FontStyle.Bold, TextAnchor.MiddleCenter, color);
        }

        private void BuildBuyButton(
            RectTransform row, string label, Color theme, int price,
            ShopItemDefinition def, ShopStore.DailyDeal deal,
            Vector2 size, Vector2 anchoredPos)
        {
            var canAfford = PlayerSave.TotalFishSnacks >= price;
            var bg = canAfford ? theme : new Color(0.18f, 0.28f, 0.4f, 0.9f);
            var fg = canAfford ? new Color(0.04f, 0.08f, 0.14f, 1f) : new Color(1f, 1f, 1f, 0.55f);

            var btn = UiBuilder.CreateButton("Buy", row, label, bg, fg, 22, rounded: true);
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
                UiBuilder.AddShadow(btn.gameObject,
                    new Color(theme.r * 0.5f, theme.g * 0.5f, theme.b * 0.5f, 0.5f),
                    new Vector2(0f, -3f));
            }

            btn.onClick.AddListener(() => ShowConfirmDialog(def, price, deal));
        }

        // ── 购买确认弹窗 ────────────────────────────────────────────────────

        private void ShowConfirmDialog(ShopItemDefinition def, int price, ShopStore.DailyDeal deal)
        {
            DismissConfirmDialog();
            var balance = PlayerSave.TotalFishSnacks;
            if (balance < price)
            {
                dispatch(MainMenuBootstrap.PanelEvent.Toast, $"鱼干不足，还差 {price - balance}");
                return;
            }

            var rt = (RectTransform)transform;
            confirmOverlay = UiBuilder.CreateRect("ShopConfirmOverlay", rt,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0.02f, 0.06f, 0.78f)).gameObject;
            confirmOverlay.GetComponent<Image>().raycastTarget = true;

            var dialog = UiBuilder.CreateRect("Dialog", confirmOverlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(720f, 580f), Vector2.zero,
                UiBuilder.MenuPanelInnerBg, rounded: true);
            UiBuilder.AddOutline(dialog.gameObject, UiBuilder.BorderAccent, new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(dialog.gameObject, new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, -8f));

            // 顶部
            var topBar = UiBuilder.CreateRect("Top", dialog,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 76f), new Vector2(0f, 0f),
                new Color(0.06f, 0.14f, 0.24f, 1f), rounded: true);
            topBar.pivot = new Vector2(0.5f, 1f);
            UiBuilder.CreateText("Title", topBar, "确认购买",
                32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            // 商品信息行
            var infoRow = UiBuilder.CreateRect("Info", dialog,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 130f), new Vector2(0f, -86f),
                new Color(0.05f, 0.1f, 0.18f, 0.8f), rounded: true);
            infoRow.pivot = new Vector2(0.5f, 1f);
            infoRow.offsetMin = new Vector2(28f, infoRow.offsetMin.y);
            infoRow.offsetMax = new Vector2(-28f, infoRow.offsetMax.y);

            BuildIconBadge(infoRow, def.IconGlyph, def.ThemeColor, new Vector2(54f, -54f));

            var infoTitle = UiBuilder.CreateText("ITitle", infoRow, def.Title,
                26, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var itrt = (RectTransform)infoTitle.transform;
            itrt.anchorMin = new Vector2(0f, 1f);
            itrt.anchorMax = new Vector2(1f, 1f);
            itrt.pivot = new Vector2(0f, 1f);
            itrt.sizeDelta = new Vector2(0f, 32f);
            itrt.anchoredPosition = new Vector2(108f, -16f);

            var infoDesc = UiBuilder.CreateText("IDesc", infoRow, def.Description,
                18, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var idrt = (RectTransform)infoDesc.transform;
            idrt.anchorMin = new Vector2(0f, 1f);
            idrt.anchorMax = new Vector2(1f, 1f);
            idrt.pivot = new Vector2(0f, 1f);
            idrt.sizeDelta = new Vector2(0f, 70f);
            idrt.anchoredPosition = new Vector2(108f, -54f);
            idrt.offsetMax = new Vector2(-20f, idrt.offsetMax.y);

            // 价格 + 余额
            var priceBlock = UiBuilder.CreateRect("PriceBlock", dialog,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 200f), new Vector2(0f, 130f),
                new Color(0.03f, 0.08f, 0.14f, 0.85f), rounded: true);
            priceBlock.pivot = new Vector2(0.5f, 0f);
            priceBlock.offsetMin = new Vector2(28f, priceBlock.offsetMin.y);
            priceBlock.offsetMax = new Vector2(-28f, priceBlock.offsetMax.y);
            UiBuilder.AddOutline(priceBlock.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            BuildPriceLine(priceBlock, "价格", $"{price} 鱼干", UiBuilder.WarmGold, new Vector2(0f, 1f), -16f);
            if (deal != null && deal.OriginalPrice > price)
            {
                BuildPriceLine(priceBlock, "原价", $"{deal.OriginalPrice} 鱼干", UiBuilder.TextTertiary,
                    new Vector2(0f, 1f), -56f);
            }
            BuildPriceLine(priceBlock, "当前持有", $"{balance} 鱼干", UiBuilder.TextSecondary, new Vector2(0f, 1f), -96f);
            BuildPriceLine(priceBlock, "购买后", $"{Mathf.Max(0, balance - price)} 鱼干",
                UiBuilder.AccentCyan, new Vector2(0f, 1f), -136f);

            // 操作按钮区
            var cancelBtn = UiBuilder.CreateButton("Cancel", dialog, "取消",
                UiBuilder.Surface2, UiBuilder.TextPrimary, 24, rounded: true);
            var crt = (RectTransform)cancelBtn.transform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0f, 0f);
            crt.sizeDelta = Vector2.zero;
            crt.offsetMin = new Vector2(28f, 28f);
            crt.offsetMax = new Vector2(-10f, 100f);
            UiBuilder.AddOutline(cancelBtn.gameObject, UiBuilder.BorderDefault, new Vector2(1f, -1f));
            cancelBtn.onClick.AddListener(DismissConfirmDialog);

            var confirmBtn = UiBuilder.CreateButton("Confirm", dialog, "确认购买",
                UiBuilder.AccentCyan, UiBuilder.TextOnAccent, 24, rounded: true);
            var conrt = (RectTransform)confirmBtn.transform;
            conrt.anchorMin = new Vector2(0.5f, 0f);
            conrt.anchorMax = new Vector2(1f, 0f);
            conrt.pivot = new Vector2(0f, 0f);
            conrt.sizeDelta = Vector2.zero;
            conrt.offsetMin = new Vector2(10f, 28f);
            conrt.offsetMax = new Vector2(-28f, 100f);
            UiBuilder.AddOutline(confirmBtn.gameObject, new Color(0.3f, 1f, 1f, 0.55f), new Vector2(2f, -2f));
            UiBuilder.AddShadow(confirmBtn.gameObject, new Color(0f, 0.5f, 0.6f, 0.45f), new Vector2(0f, -4f));
            confirmBtn.onClick.AddListener(() =>
            {
                if (ShopStore.TryPurchase(def, price, out var reason))
                {
                    DismissConfirmDialog();
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, $"已购入 {def.Title}");
                    Refresh();
                }
                else
                {
                    dispatch(MainMenuBootstrap.PanelEvent.Toast, reason ?? "购买失败");
                }
            });
        }

        private static void BuildPriceLine(RectTransform parent, string label, string value, Color valueColor,
            Vector2 anchorTop, float yOffset)
        {
            var lblText = UiBuilder.CreateText("Lbl_" + label, parent, label,
                20, FontStyle.Normal, TextAnchor.MiddleLeft, UiBuilder.TextSecondary);
            var lrt = (RectTransform)lblText.transform;
            lrt.anchorMin = anchorTop;
            lrt.anchorMax = new Vector2(0.5f, anchorTop.y);
            lrt.pivot = new Vector2(0f, 1f);
            lrt.sizeDelta = new Vector2(0f, 32f);
            lrt.anchoredPosition = new Vector2(20f, yOffset);

            var valText = UiBuilder.CreateText("Val_" + label, parent, value,
                22, FontStyle.Bold, TextAnchor.MiddleRight, valueColor);
            var vrt = (RectTransform)valText.transform;
            vrt.anchorMin = new Vector2(0.5f, anchorTop.y);
            vrt.anchorMax = new Vector2(1f, anchorTop.y);
            vrt.pivot = new Vector2(1f, 1f);
            vrt.sizeDelta = new Vector2(0f, 32f);
            vrt.anchoredPosition = new Vector2(-20f, yOffset);
        }

        private void DismissConfirmDialog()
        {
            if (confirmOverlay != null)
            {
                Destroy(confirmOverlay);
                confirmOverlay = null;
            }
        }

        private void Update()
        {
            if (activeTab != ShopCategory.DailyDeals || countdownText == null) return;
            countdownTickAcc += Time.unscaledDeltaTime;
            if (countdownTickAcc < 1f) return;
            countdownTickAcc = 0f;
            countdownText.text = FormatCountdown(ShopStore.MillisUntilNextRefresh());
        }

        private static string FormatCountdown(long ms)
        {
            var totalSec = ms <= 0L ? 0L : ms / 1000L;
            var h = totalSec / 3600L;
            var m = (totalSec % 3600L) / 60L;
            var s = totalSec % 60L;
            return h > 0 ? $"{h:D2}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
        }
    }
}
