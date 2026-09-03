using PenguinRun;
using PenguinRun.Game.Save;
using PenguinRun.Game.Growth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 图鉴面板：基于 PlayerSave 的解锁标记展示进度（团团、Dror、雪松线、击败高松鹅等）。
    /// 重构：进度条 banner、图标徽章、解锁/未解锁状态对比强烈。
    /// </summary>
    public sealed class CodexPanel : MonoBehaviour
    {
        private System.Action<MainMenuBootstrap.PanelEvent, string> dispatch;
        private RectTransform contentRoot;
        private TextMeshProUGUI progressText;
        private Image progressFill;
        private RectTransform progressContainer;
        private readonly System.Collections.Generic.Dictionary<string, Sprite> iconCache = new();

        private const float BannerHeight = 96f;
        private const float BannerGap = 14f;

        public static GameObject Build(Transform parent, System.Action<MainMenuBootstrap.PanelEvent, string> dispatch)
        {
            var rootRt = UiBuilder.CreateRect(
                "CodexPanel", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                UiTheme.PaperBase);
            var script = rootRt.gameObject.AddComponent<CodexPanel>();
            script.dispatch = dispatch;
            script.BuildLayout(rootRt);
            script.Refresh();
            return rootRt.gameObject;
        }

        private void BuildLayout(RectTransform parent)
        {
            PanelHeader.Create(parent, "企鹅图鉴", "伙伴、敌人和关键旅程",
                () => dispatch(MainMenuBootstrap.PanelEvent.Close, null));

            // 进度横幅：卡片化设计，带文字 + 进度条
            var bannerTop = UiBuilder.PanelHeaderHeightPixels + BannerGap;
            progressContainer = UiBuilder.CreateRect("ProgressBanner", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, BannerHeight), new Vector2(0f, -bannerTop),
                UiBuilder.Surface1, rounded: true);
            progressContainer.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(progressContainer.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            UiBuilder.AttachArtVignette("CodexGalleryArt", progressContainer, Image2ArtCatalog.CodexGallery, 0.14f);

            // 左侧：进度文本
            progressText = UiBuilder.CreateText("ProgressText", progressContainer.transform,
                string.Empty, 22, FontStyle.Bold,
                TextAnchor.UpperLeft, UiTheme.InkPrimary);
            var ptrt = (RectTransform)progressText.transform;
            ptrt.anchorMin = new Vector2(0f, 0.5f);
            ptrt.anchorMax = new Vector2(1f, 1f);
            ptrt.pivot = new Vector2(0f, 0.5f);
            ptrt.offsetMin = new Vector2(24f, 0f);
            ptrt.offsetMax = new Vector2(-24f, -8f);

            // 进度条
            var trackRt = UiBuilder.CreateRect("Track", progressContainer.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 12f), new Vector2(0f, 18f),
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.48f), rounded: true);
            trackRt.offsetMin = new Vector2(24f, trackRt.offsetMin.y);
            trackRt.offsetMax = new Vector2(-24f, trackRt.offsetMax.y);
            UiBuilder.AddOutline(trackRt.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            progressFill = UiBuilder.CreateRect("Fill", trackRt,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero,
                UiTheme.CoralAction, rounded: true).GetComponent<Image>();
            progressFill.raycastTarget = false;

            UiBuilder.CreatePanelScrollList(parent, out var content,
                topInset: bannerTop + BannerHeight + BannerGap);
            contentRoot = content;
        }

        private void Refresh()
        {
            if (progressText != null)
            {
                var u = CodexUnlocks.UnlockedCount();
                var total = CodexUnlocks.StoryEntryTotal;
                var bu = CodexUnlocks.BossesDefeatedCount();
                var bt = CodexUnlocks.BossEntryTotal;
                // #3C7873 = UiTheme.PineGreen、#466366 = 墨色次级；米黄横幅上禁用旧霓虹 #00DAF0 / 灰蓝 #A8B6C8
                progressText.text = $"<color=#3C7873><b>{u}</b></color>/{total}<size=18><color=#466366> 故事</color></size>"
                    + $"  ·  <color=#EC6341><b>{bu}</b></color>/{bt}<size=18><color=#466366> Boss</color></size>"
                    + $"  <size=18><color=#466366>· 无尽 / 今日挑战</color></size>";
                progressText.richText = true;

                if (progressFill != null)
                {
                    var ratio = total > 0 ? (float)u / total : 0f;
                    var fillRt = (RectTransform)progressFill.transform;
                    fillRt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
                }
            }

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }

            BuildEntry("\u2665", "icon_companion_star", "救回团团",
                $"在雪松村废墟救回第一位伙伴。提示：单局约 {CodexUnlocks.RescueTuanTuanDistanceMeters}m 或得分 {CodexUnlocks.RescueTuanTuanMinScore}+。",
                PlayerSave.RescuedTuanTuan, new Color(1f, 0.55f, 0.7f, 1f));
            BuildEntry("\u2606", "icon_companion_dror", "Dror 同行",
                $"抵达冰湖后与 Dror 结伴。提示：完成一次今日挑战、单局 {CodexUnlocks.CompanionDrorMinDistanceMeters}m+ 或累计 {CodexUnlocks.CompanionDrorMinLifetimeRuns} 局。",
                PlayerSave.CompanionDrorUnlocked, new Color(0.55f, 0.85f, 1f, 1f));
            BuildEntry("\u2694", "icon_boss_defeat", "击败高松鹅",
                $"在终章击退入侵雪原的高松鹅。提示：单局得分 {CodexUnlocks.TakamatsuMinScore}+ 或存活 {CodexUnlocks.TakamatsuMinSurvivalSeconds:0.#} 秒。",
                PlayerSave.TakamatsuDefeated, new Color(1f, 0.45f, 0.45f, 1f));
            BuildEntry("\u2302", "icon_camp", "营地之旅", "首次进入营地，开启局外强化。",
                PlayerSave.VisitedCamp, new Color(0.4f, 0.95f, 0.7f, 1f));
            BuildEntry("\u2605", "icon_tutorial", "3D 跑酷新手教程", "完成至少一局完整结算。",
                PlayerSave.Runner3DTutorialDone, new Color(1f, 0.85f, 0.4f, 1f));

            BuildSectionHeader($"Boss 战绩 · {CodexUnlocks.BossesDefeatedCount()} / {CodexUnlocks.BossEntryTotal}");
            foreach (var boss in BossDefinitions.GetAllBossesOrdered())
                BuildBossEntry(boss);

            BuildDiscoverySection(CodexDiscoveryGroup.Route, "路线手记");
            BuildDiscoverySection(CodexDiscoveryGroup.Relic, "主题遗物");
            CodexDiscoveryStore.MarkAllSeen();

            UiBuilder.RebuildScrollContent(contentRoot);
        }

        private void BuildDiscoverySection(CodexDiscoveryGroup group, string label)
        {
            var unlockedCount = 0;
            var total = 0;
            foreach (var definition in CodexDiscoveryCatalog.ForGroup(group))
            {
                total++;
                if (CodexDiscoveryStore.IsDiscovered(definition.Id)) unlockedCount++;
            }
            BuildSectionHeader($"{label} · {unlockedCount}/{total}");
            foreach (var definition in CodexDiscoveryCatalog.ForGroup(group))
            {
                var unlocked = CodexDiscoveryStore.IsDiscovered(definition.Id);
                var isNew = CodexDiscoveryStore.IsNew(definition.Id);
                var title = unlocked && isNew ? $"{definition.Title}  新" : definition.Title;
                var color = group == CodexDiscoveryGroup.Route ? UiTheme.PineGreen : UiTheme.CoralAction;
                BuildEntry(group == CodexDiscoveryGroup.Route ? "路" : "◆", "icon_locked",
                    title, $"{definition.Theme} · {definition.Description}", unlocked, color,
                    $"线索：{definition.UnknownClue}");
            }
        }

        private void BuildEntry(string fallbackIcon, string iconSpriteName, string title, string desc,
            bool unlocked, Color themeColor, string lockedDescription = null)
        {
            // 未解锁行也留在纸面色系：旧深色底会让墨色标题（TextSecondary）不可读
            var rowBg = unlocked ? UiBuilder.Surface1 : Color.Lerp(UiTheme.PaperBase, UiTheme.MistBlue, 0.35f);
            var row = UiBuilder.CreateScrollListRow("Entry_" + title, contentRoot, 160f, rowBg);
            UiBuilder.AddOutline(row.gameObject,
                unlocked ? new Color(themeColor.r, themeColor.g, themeColor.b, 0.35f) : UiBuilder.BorderSubtle,
                new Vector2(1f, -1f));

            // 左侧重音条
            var accent = UiBuilder.CreateRect("Accent", row,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(5f, 0f), Vector2.zero,
                unlocked ? themeColor : new Color(0.25f, 0.3f, 0.4f, 0.6f), rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            // 图标徽章
            var iconBadge = UiBuilder.CreateRect("IconBadge", row,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, 72f), new Vector2(54f, 0f),
                unlocked
                    ? new Color(themeColor.r, themeColor.g, themeColor.b, 0.2f)
                    : new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.55f),
                circle: true);
            UiBuilder.AddOutline(iconBadge.gameObject,
                unlocked
                    ? new Color(themeColor.r, themeColor.g, themeColor.b, 0.55f)
                    : UiBuilder.BorderSubtle,
                new Vector2(1.5f, -1.5f));
            var iconSprite = LoadIconSprite(unlocked ? iconSpriteName : "icon_locked");
            if (iconSprite != null)
            {
                var iconImageRt = UiBuilder.CreateRect("IconImage", iconBadge.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(44f, 44f), Vector2.zero, Color.white);
                var iconImage = iconImageRt.GetComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                iconImage.color = unlocked ? themeColor : UiBuilder.TextSecondary;
            }
            else
            {
                UiBuilder.CreateText("Icon", iconBadge.transform, unlocked ? fallbackIcon : "?",
                    36, FontStyle.Bold, TextAnchor.MiddleCenter,
                    unlocked ? themeColor : UiBuilder.TextSecondary);
            }

            // 标题
            var titleText = UiBuilder.CreateText("Title", row, title,
                28, FontStyle.Bold, TextAnchor.UpperLeft,
                unlocked ? UiBuilder.TextPrimary : UiBuilder.TextSecondary);
            var trt = (RectTransform)titleText.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.7f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 42f);
            trt.anchoredPosition = new Vector2(108f, -22f);

            // 描述
            var descText = UiBuilder.CreateText("Desc", row,
                unlocked ? desc : (lockedDescription ?? "继续游戏以解锁此条目。"),
                20, FontStyle.Normal, TextAnchor.UpperLeft,
                unlocked ? UiBuilder.TextSecondary : UiBuilder.TextTertiary);
            var drt = (RectTransform)descText.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.7f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 70f);
            drt.anchoredPosition = new Vector2(108f, -68f);

            // 状态徽章（chip）
            var statusChip = UiBuilder.CreateRect("StatusChip", row,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(120f, 38f), new Vector2(-24f, 0f),
                unlocked
                    ? new Color(themeColor.r, themeColor.g, themeColor.b, 0.22f)
                    : new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.5f),
                rounded: true);
            UiBuilder.AddOutline(statusChip.gameObject,
                unlocked
                    ? new Color(themeColor.r, themeColor.g, themeColor.b, 0.55f)
                    : UiBuilder.BorderSubtle,
                new Vector2(1f, -1f));
            // \u26AC 在 CJK 字体里是豆腐块，未解锁态直接用文字
            UiBuilder.CreateText("StatusText", statusChip.transform,
                unlocked ? "\u2713 已解锁" : "未解锁",
                18, FontStyle.Bold, TextAnchor.MiddleCenter,
                unlocked ? themeColor : UiBuilder.TextSecondary);
        }

        private void BuildSectionHeader(string label)
        {
            var row = UiBuilder.CreateScrollListRow("BossSectionHeader", contentRoot, 52f,
                UiBuilder.Surface2);
            var rowBgImage = row.GetComponentInChildren<Image>(true);
            if (rowBgImage != null)
                rowBgImage.raycastTarget = false;
            UiBuilder.CreateText("Label", row.transform, label, 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentActive);
            var lt = row.GetComponentInChildren<TextMeshProUGUI>();
            if (lt != null)
            {
                var lrt = (RectTransform)lt.transform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(16f, 0f);
                lrt.offsetMax = new Vector2(-16f, 0f);
            }
        }

        private void BuildBossEntry(BossDefinition boss)
        {
            if (boss == null) return;
            var unlocked = PlayerSave.HasDefeatedBoss(boss.BossId);
            var theme = boss.GlowColor;
            theme.a = 1f;

            BuildEntry("\u2694", "icon_boss_defeat",
                unlocked ? boss.DefeatCodexLabel : boss.DisplayName,
                unlocked ? $"{boss.SignatureReward} · {boss.SignatureMechanic}"
                    : "在无尽或今日挑战中击败该首领以解锁图鉴条目。",
                unlocked, theme);
        }

        private Sprite LoadIconSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            if (iconCache.TryGetValue(spriteName, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>($"PenguinRun/CodexIcons/{spriteName}");
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>($"PenguinRun/CodexIcons/{spriteName}");
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            iconCache[spriteName] = sprite;
            return sprite;
        }
    }
}
