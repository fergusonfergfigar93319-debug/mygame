using System.Collections.Generic;
using PenguinRun.Game.Mission;
using PenguinRun.Game.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 任务面板：每日 / 成就 Tab、领取按钮、Snackbar、空状态、每日倒计时、成就分组。
    /// 重构优化版：改进视觉层次、卡片设计和交互体验。
    /// </summary>
    public sealed class MissionPanel : MonoBehaviour
    {
        private System.Action<MainMenuBootstrap.PanelEvent, string> dispatch;
        private MissionTrack track = MissionTrack.Daily;
        private RectTransform contentRoot;
        private RectTransform headerBar;
        private TextMeshProUGUI countdownText;
        private GameObject countdownRow;
        /// <summary>上次写入倒计时文本时的整秒值；秒数不变时跳过 Format 与 .text 写入。</summary>
        private long lastCountdownSeconds = -1;
        private Image tabDailyBg;
        private Image tabAchBg;
        private Button tabDailyBtn;
        private Button tabAchBtn;
        
        // 现代化配色方案
        private static readonly Color TabInactive = new(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.55f);
        // Coral 只留给主 CTA「领取奖励」；Tab 选中态走统一的 PineGreen 激活色
        private static readonly Color TabActive = UiBuilder.AccentActive;
        private static readonly Color CardBg = UiTheme.PaperWarm;
        private static readonly Color CardBgCompleted = new(0.82f, 0.92f, 0.82f, 1f);
        private static readonly Color CardBgClaimed = new(UiTheme.PaperBase.r, UiTheme.PaperBase.g, UiTheme.PaperBase.b, 0.72f);
        private static readonly Color ProgressBarBg = new(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.5f);
        private static readonly Color ProgressBarFill = UiTheme.CoralAction;
        private static readonly Color ProgressBarFillCompleted = UiTheme.PineGreen;
        private static readonly Color RewardGold = UiTheme.CoralAction;
        private static readonly Color StatusTextActive = UiTheme.CoralAction;
        private static readonly Color StatusTextCompleted = UiTheme.PineGreen;
        private static readonly Color StatusTextClaimed = UiTheme.InkSecondary;

        public static GameObject Build(Transform parent, System.Action<MainMenuBootstrap.PanelEvent, string> dispatch)
        {
            var rootRt = UiBuilder.CreateRect(
                "MissionPanel", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                UiTheme.PaperBase);
            var root = rootRt.gameObject;

            var script = root.AddComponent<MissionPanel>();
            script.dispatch = dispatch;
            script.BuildLayout(rootRt);
            script.ApplyTabStyle();
            script.Refresh();
            return root;
        }

        private void BuildLayout(RectTransform parent)
        {
            headerBar = PanelHeader.Create(parent, "每日任务", "完成目标领取鱼干", () => dispatch(MainMenuBootstrap.PanelEvent.Close, null));

            const float tabsHeight = 88f;
            const float tabsGap = 16f;
            const float countdownHeight = 72f;
            const float countdownGap = 16f;

            var tabsTop = UiBuilder.PanelHeaderHeightPixels + tabsGap;
            
            // Tab 容器：纸面+雾蓝的浅色底，与营地商店主 Tab 一致
            var tabsContainer = UiBuilder.CreateRect("TabsContainer", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, tabsHeight + 8f), new Vector2(0f, -tabsTop + 4f),
                Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.22f), rounded: true);
            tabsContainer.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(tabsContainer.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var hlg = tabsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 8, 8);
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            
            // Tab按钮 - 现代药丸设计
            tabDailyBtn = CreateModernTab(tabsContainer, "每日", true);
            tabDailyBg = tabDailyBtn.GetComponent<Image>();
            tabAchBtn = CreateModernTab(tabsContainer, "成就", false);
            tabAchBg = tabAchBtn.GetComponent<Image>();
            
            tabDailyBtn.onClick.AddListener(() => SwitchTrack(MissionTrack.Daily));
            tabAchBtn.onClick.AddListener(() => SwitchTrack(MissionTrack.Achievement));

            // 倒计时条：雾蓝纸面底 + 墨色/松绿文字
            var countdownTop = tabsTop + tabsHeight + countdownGap;
            countdownRow = UiBuilder.CreateRect("CountdownRow", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, countdownHeight), new Vector2(0f, -countdownTop),
                Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.28f), rounded: true).gameObject;
            ((RectTransform)countdownRow.transform).pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(countdownRow, UiTheme.StitchBorder, new Vector2(1f, -1f));

            // 倒计时图标
            var iconRect = UiBuilder.CreateRect("TimerIcon", countdownRow.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(36f, 36f), new Vector2(20f, 0f),
                UiTheme.PineGreen, circle: true);
            var timerIcon = UiIcons.CreateImage("Icon", iconRect, UiIcons.Timer, Color.white);
            var timerIconRect = (RectTransform)timerIcon.transform;
            timerIconRect.anchorMin = new Vector2(0.15f, 0.15f);
            timerIconRect.anchorMax = new Vector2(0.85f, 0.85f);
            timerIconRect.offsetMin = Vector2.zero;
            timerIconRect.offsetMax = Vector2.zero;

            var clabel = UiBuilder.CreateText("Label", countdownRow.transform, "今日任务刷新", 24, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.TextPrimary);
            var crt = (RectTransform)clabel.transform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.sizeDelta = Vector2.zero;
            crt.anchoredPosition = Vector2.zero;
            crt.offsetMin = new Vector2(64f, 8f);
            crt.offsetMax = new Vector2(-20f, -8f);

            countdownText = UiBuilder.CreateText("Value", countdownRow.transform, "—", 26, FontStyle.Bold, TextAnchor.MiddleRight, UiBuilder.AccentActive);
            var crt2 = (RectTransform)countdownText.transform;
            crt2.anchorMin = new Vector2(0.5f, 0f);
            crt2.anchorMax = new Vector2(1f, 1f);
            crt2.sizeDelta = Vector2.zero;
            crt2.anchoredPosition = Vector2.zero;
            crt2.offsetMin = new Vector2(20f, 8f);
            crt2.offsetMax = new Vector2(-20f, -8f);

            UiBuilder.CreatePanelScrollList(parent, out var content,
                topInset: countdownTop + countdownHeight + 16f,
                bottomInset: 80f);
            contentRoot = content;
        }

        private Button CreateModernTab(RectTransform parent, string label, bool isActive)
        {
            var btn = UiBuilder.CreateButton($"Tab_{label}", parent, label, 
                isActive ? TabActive : TabInactive, 
                isActive ? Color.white : UiBuilder.TextPrimary, 
                28, rounded: true);
            var btnRt = (RectTransform)btn.transform;
            
            // 添加微妙阴影效果
            if (isActive)
            {
                var shadow = UiBuilder.CreateRect("Shadow", btnRt,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(btnRt.sizeDelta.x + 4f, btnRt.sizeDelta.y + 4f), new Vector2(0f, -3f),
                    UiTheme.ShadowWarm, rounded: true);
                shadow.SetSiblingIndex(0);
            }
            
            return btn;
        }

        private void SwitchTrack(MissionTrack t)
        {
            track = t;
            ApplyTabStyle();
            Refresh();
        }

        private void ApplyTabStyle()
        {
            if (tabDailyBg == null || tabAchBg == null) return;
            var dailySel = track == MissionTrack.Daily;
            
            // 更新背景色
            tabDailyBg.color = dailySel ? TabActive : TabInactive;
            tabAchBg.color = dailySel ? TabInactive : TabActive;
            
            // 更新文字颜色：选中 PineGreen 上用白字，未选雾蓝上用墨色
            var activeTextColor = Color.white;
            var inactiveTextColor = UiBuilder.TextPrimary;
            tabDailyBg.GetComponentInChildren<TextMeshProUGUI>().color = dailySel ? activeTextColor : inactiveTextColor;
            tabAchBg.GetComponentInChildren<TextMeshProUGUI>().color = dailySel ? inactiveTextColor : activeTextColor;
            
            // 添加切换动画效果
            var dailyRt = tabDailyBg.transform as RectTransform;
            var achRt = tabAchBg.transform as RectTransform;
            if (dailyRt != null) dailyRt.localScale = dailySel ? Vector3.one * 1.02f : Vector3.one;
            if (achRt != null) achRt.localScale = dailySel ? Vector3.one : Vector3.one * 1.02f;
        }

        private void Refresh()
        {
            PanelHeader.RefreshCurrency(headerBar);
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }

            if (track == MissionTrack.Daily) MissionStore.EnsureDailyBucket();
            countdownRow.SetActive(track == MissionTrack.Daily);
            if (track == MissionTrack.Daily && countdownText != null)
            {
                var ms = MissionStore.MillisUntilNextDailyReset();
                lastCountdownSeconds = ms <= 0L ? 0L : ms / 1000L;
                countdownText.text = FormatCountdown(ms);
            }

            var states = MissionStore.MissionStates(track);
            if (states.Count == 0)
            {
                BuildEmpty(track);
                UiBuilder.RebuildScrollContent(contentRoot);
                return;
            }

            if (track == MissionTrack.Achievement)
            {
                BuildGroupedAchievements(states);
            }
            else
            {
                foreach (var s in states) BuildCard(s);
            }

            UiBuilder.RebuildScrollContent(contentRoot);
        }

        private void BuildEmpty(MissionTrack t)
        {
            var empty = UiBuilder.CreateScrollListRow("Empty", contentRoot, 400f,
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.35f));
            UiBuilder.AddOutline(empty.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            
            var (title, body, icon) = t == MissionTrack.Daily
                ? ("暂无每日任务", "当前没有可展示的每日目标，请稍后再试或检查配置。", UiIcons.Timer)
                : ("暂无成就任务", "成就列表为空，完成任务后将在此汇总进度与奖励。", UiIcons.Star);

            // 中央图标
            var iconRect = UiBuilder.CreateRect("EmptyIcon", empty.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(80f, 80f), new Vector2(0f, 30f),
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.6f), circle: true);
            var emptyIcon = UiIcons.CreateImage("IconImage", iconRect.transform, icon,
                UiTheme.PineGreen);
            var emptyIconRect = (RectTransform)emptyIcon.transform;
            emptyIconRect.anchorMin = new Vector2(0.2f, 0.2f);
            emptyIconRect.anchorMax = new Vector2(0.8f, 0.8f);
            emptyIconRect.offsetMin = Vector2.zero;
            emptyIconRect.offsetMax = Vector2.zero;

            var titleText = UiBuilder.CreateText("Title", empty.transform, title, 32, FontStyle.Bold, TextAnchor.MiddleCenter, UiBuilder.TextPrimary);
            var trt = (RectTransform)titleText.transform;
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(0f, 48f);
            trt.anchoredPosition = new Vector2(0f, -50f);

            var bodyText = UiBuilder.CreateText("Body", empty.transform, body, 22, FontStyle.Normal, TextAnchor.MiddleCenter, 
                UiBuilder.TextSecondary);
            var brt = (RectTransform)bodyText.transform;
            brt.anchorMin = new Vector2(0.1f, 0f);
            brt.anchorMax = new Vector2(0.9f, 0.5f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 80f);
            brt.anchoredPosition = new Vector2(0f, -60f);
        }

        private void BuildGroupedAchievements(List<MissionUiState> all)
        {
            foreach (AchievementGroup g in System.Enum.GetValues(typeof(AchievementGroup)))
            {
                var inGroup = new List<MissionUiState>();
                foreach (var s in all)
                    if (s.Definition.AchievementGroup.HasValue && s.Definition.AchievementGroup.Value == g)
                        inGroup.Add(s);
                if (inGroup.Count == 0) continue;
                BuildSectionHeader(g.SectionTitle());
                foreach (var s in inGroup) BuildCard(s);
            }
            var ungrouped = new List<MissionUiState>();
            foreach (var s in all)
                if (!s.Definition.AchievementGroup.HasValue)
                    ungrouped.Add(s);
            if (ungrouped.Count > 0)
            {
                BuildSectionHeader("其他成就");
                foreach (var s in ungrouped) BuildCard(s);
            }
        }

        private void BuildSectionHeader(string title)
        {
            var header = UiBuilder.CreateRect("SectionHeader", contentRoot,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 56f), Vector2.zero, null);
            
            // 添加左侧装饰线
            var accentLine = UiBuilder.CreateRect("AccentLine", header,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 28f), new Vector2(12f, 0f),
                UiBuilder.AccentActive, rounded: true);
            
            var t = UiBuilder.CreateText("Text", header, title, 26, FontStyle.Bold, TextAnchor.MiddleLeft, 
                UiBuilder.AccentActive);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = new Vector2(24f, 0f);
            rt.offsetMax = new Vector2(-8f, 0f);
            UiBuilder.SetScrollItemHeight(header, 56f);
        }

        private void BuildCard(MissionUiState state)
        {
            var def = state.Definition;
            
            // 根据状态选择卡片背景色
            Color cardBgColor;
            if (state.Claimed)
                cardBgColor = CardBgClaimed;
            else if (state.Completed)
                cardBgColor = CardBgCompleted;
            else
                cardBgColor = CardBg;
            
            // 创建现代化卡片
            var card = UiBuilder.CreateScrollListRow("Card_" + def.Id, contentRoot, 260f, cardBgColor);
            var cardRt = card.GetComponent<RectTransform>();
            UiBuilder.AddOutline(card.gameObject, UiTheme.StitchBorder, new Vector2(1f, -1f));
            
            // 左侧装饰条 - 根据状态显示不同颜色（进行中珊瑚、已完成松绿、已领取灰）
            var statusBarColor = state.Claimed ? new Color(0.5f, 0.6f, 0.7f, 0.5f) :
                                state.Completed ? UiTheme.PineGreen :
                                UiTheme.CoralAction;
            var statusBar = UiBuilder.CreateRect("StatusBar", card.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(6f, 0f), new Vector2(12f, -32f),
                statusBarColor, rounded: true);
            statusBar.anchorMin = new Vector2(0f, 0f);
            statusBar.anchorMax = new Vector2(0f, 1f);
            statusBar.offsetMin = new Vector2(12f, 20f);
            statusBar.offsetMax = new Vector2(18f, -20f);

            // 标题 - 米黄卡面上用墨色
            var titleText = UiBuilder.CreateText("Title", card.transform, def.Title, 32, FontStyle.Bold, TextAnchor.UpperLeft, UiBuilder.TextPrimary);
            var trt = (RectTransform)titleText.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.65f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 42f);
            trt.anchoredPosition = new Vector2(28f, -20f);

            // 描述文字 - 墨色次级
            var descText = UiBuilder.CreateText("Desc", card.transform, def.Description, 22, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)descText.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.7f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 64f);
            drt.anchoredPosition = new Vector2(28f, -66f);

            // 奖励区域 - 右上角金币图标+数字
            var rewardContainer = UiBuilder.CreateRect("RewardContainer", card.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(120f, 56f), new Vector2(-20f, -16f),
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.35f), rounded: true);
            
            var coinIcon = UiBuilder.CreateRect("CoinIcon", rewardContainer,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(28f, 28f), new Vector2(12f, 0f),
                new Color(1f, 0.85f, 0.35f, 1f), circle: true);
            UiBuilder.CreateText("CoinText", coinIcon.transform, "鱼", 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.05f, 1f));
            
            var rewardText = UiBuilder.CreateText("Reward", rewardContainer.transform, $"+{def.RewardFishSnacks}", 26, FontStyle.Bold, TextAnchor.MiddleCenter, RewardGold);
            var rrt = (RectTransform)rewardText.transform;
            rrt.anchorMin = new Vector2(0f, 0f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.offsetMin = new Vector2(44f, 0f);
            rrt.offsetMax = new Vector2(-8f, 0f);

            // 进度条区域
            var barTrack = UiBuilder.CreateRect("BarTrack", card.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 16f), Vector2.zero, ProgressBarBg);
            barTrack.anchoredPosition = new Vector2(0f, 88f);
            barTrack.offsetMin = new Vector2(28f, 80f);
            barTrack.offsetMax = new Vector2(-28f, 96f);
            
            // 进度条填充颜色根据状态变化
            var fillColor = state.Completed ? ProgressBarFillCompleted : ProgressBarFill;
            UiBuilder.CreateProgressFill("BarFill", barTrack, new Color(0, 0, 0, 0), fillColor, out var fill);
            var fillRt = (RectTransform)fill.transform.parent;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            UiBuilder.SetProgress01(fill, Mathf.Min(1f, state.Progress / (float)def.Target));
            
            // 进度数字 - 左下角
            var progressColor = state.Completed ? StatusTextCompleted : StatusTextActive;
            var progressText = UiBuilder.CreateText("Progress", card.transform, $"{state.Progress} / {def.Target}", 24, FontStyle.Bold, TextAnchor.MiddleLeft, progressColor);
            var prt = (RectTransform)progressText.transform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0.5f, 0f);
            prt.pivot = new Vector2(0f, 0f);
            prt.sizeDelta = new Vector2(0f, 36f);
            prt.anchoredPosition = new Vector2(28f, 100f);

            // 状态标签 - 右下角
            var (statusLabel, statusColor) = state.Claimed ? ("已领取", StatusTextClaimed) :
                                              state.Completed ? ("可领取", StatusTextCompleted) :
                                              ("进行中", UiBuilder.TextSecondary);
            var statusText = UiBuilder.CreateText("Status", card.transform, statusLabel, 24, FontStyle.Bold, TextAnchor.MiddleRight, statusColor);
            var srt = (RectTransform)statusText.transform;
            srt.anchorMin = new Vector2(0.5f, 0f);
            srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(1f, 0f);
            srt.sizeDelta = new Vector2(0f, 36f);
            srt.anchoredPosition = new Vector2(-28f, 100f);

            // 领取按钮 - 现代化设计
            if (state.Claimable)
            {
                var btnContainer = UiBuilder.CreateRect("BtnContainer", card.transform,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(280f, 72f), Vector2.zero,
                    new Color(0f, 0f, 0f, 0f));
                btnContainer.anchorMin = new Vector2(0f, 0f);
                btnContainer.anchorMax = new Vector2(1f, 0f);
                btnContainer.offsetMin = new Vector2(28f, 12f);
                btnContainer.offsetMax = new Vector2(-28f, 84f);
                
                // 主 CTA：珊瑚色 + 白字 + 缝线描边
                var btn = UiBuilder.CreateButton("Claim", btnContainer, "领取奖励", UiTheme.CoralAction, Color.white, 28, rounded: true);
                var btnRt = (RectTransform)btn.transform;
                btnRt.anchorMin = new Vector2(0f, 0f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.sizeDelta = Vector2.zero;
                
                UiBuilder.AddOutline(btn.gameObject, UiTheme.StitchBorder, new Vector2(2f, -2f));
                
                btn.onClick.AddListener(() =>
                {
                    var ok = MissionStore.ClaimMission(def.Id);
                    dispatch(MainMenuBootstrap.PanelEvent.Toast,
                        ok ? $"已领取 {def.RewardFishSnacks} 鱼干" : "暂时无法领取");
                    if (ok) Refresh();
                });
            }
            else if (state.Claimed || state.Completed)
            {
                // 已完成或已领取状态，显示一个装饰性的完成标记
                var checkmark = UiBuilder.CreateRect("Checkmark", card.transform,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(48f, 48f), new Vector2(-32f, 20f),
                    new Color(UiTheme.PineGreen.r, UiTheme.PineGreen.g, UiTheme.PineGreen.b, 0.15f), circle: true);
                UiBuilder.CreateText("CheckText", checkmark.transform, "✓", 28, FontStyle.Bold, TextAnchor.MiddleCenter, 
                    UiTheme.PineGreen);
            }
        }

        private void Update()
        {
            if (track != MissionTrack.Daily) return;
            if (countdownText == null) return;
            var ms = MissionStore.MillisUntilNextDailyReset();
            var sec = ms <= 0L ? 0L : ms / 1000L;
            if (sec == lastCountdownSeconds) return;
            lastCountdownSeconds = sec;
            countdownText.text = FormatCountdown(ms);
        }

        private static string FormatCountdown(long ms)
        {
            // 勿用 Mathf.Max(long,long)：会转成 float，导致小时/分钟出现长小数
            var totalSec = ms <= 0L ? 0L : ms / 1000L;
            var h = totalSec / 3600L;
            var m = (totalSec % 3600L) / 60L;
            var s = totalSec % 60L;
            
            // 现代化时间显示格式
            if (h > 0) 
                return $"{h:D2}:{m:D2}:{s:D2}";
            return $"{m:D2}:{s:D2}";
        }
    }
}
