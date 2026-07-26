using System;
using System.Collections.Generic;
using System.Text;
using PenguinRun.Game;
using PenguinRun.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenguinRun
{
    /// <summary>Retained, value-cached runner HUD that never owns gameplay state.</summary>
    internal sealed class RunnerHudCanvas
    {
        private const int MaxVisibleUpgrades = 3;
        private const int MaxHeartSlots = 5;
        private readonly GameObject canvasObject;
        private readonly GameObject normalStatsRoot;
        private readonly GameObject bossRoot;
        private readonly GameObject pauseRoot;
        private readonly GameObject pauseActionRoot;
        private readonly GameObject feedbackRoot;
        private readonly TextMeshProUGUI modeText;
        private readonly TextMeshProUGUI scoreText;
        private readonly TextMeshProUGUI distanceText;
        private readonly TextMeshProUGUI comboText;
        private readonly TextMeshProUGUI fishText;
        private readonly Image[] heartIcons;
        private readonly GameObject buffRoot;
        private readonly TextMeshProUGUI buffText;
        private readonly TextMeshProUGUI feedbackText;
        private readonly TextMeshProUGUI bossNameText;
        private readonly TextMeshProUGUI bossPatternText;
        private readonly TextMeshProUGUI bossHintText;
        private readonly Image bossHealthFill;
        private readonly GameObject routeRoot;
        private readonly TextMeshProUGUI routeTitleText;
        private readonly TextMeshProUGUI routeChoiceText;
        private readonly GameObject upgradeRoot;
        private readonly TextMeshProUGUI upgradeText;
        private readonly GameObject skillRoot;
        private readonly TextMeshProUGUI skillText;
        private readonly GameObject mechanicRoot;
        private readonly TextMeshProUGUI mechanicTitleText;
        private readonly TextMeshProUGUI mechanicHintText;
        private readonly Image resonanceFill;
        private readonly TextMeshProUGUI contractSummaryText;

        private int cachedScore = int.MinValue;
        private int cachedDistance = int.MinValue;
        private int cachedCombo = int.MinValue;
        private int cachedFish = int.MinValue;
        private int cachedLives = int.MinValue;
        private int cachedMaxLives = int.MinValue;
        private string cachedBuffs;
        private readonly StringBuilder buffSb = new StringBuilder(128);
        private float overrideFeedbackUntil;
        private bool bossMode;
        private int cachedUpgradeSelectionCount = -1;
        private int lastSkillTier = -1;
        private int cachedContractProgress = -1;
        private int cachedSkillChain = int.MinValue;
        private int cachedSkillScore = int.MinValue;
        private float cachedSkillMultiplier = float.MinValue;
        private string cachedSkillLabel;
        private float skillTierFlashUntil;
        private string cachedRouteTitle;
        private string cachedRouteSafe;
        private string cachedRouteRisk;

        private RunnerHudCanvas(GameObject canvasObject, GameObject normalStatsRoot, GameObject bossRoot,
            GameObject pauseRoot, GameObject pauseActionRoot, GameObject feedbackRoot,
            TextMeshProUGUI modeText,
            TextMeshProUGUI scoreText, TextMeshProUGUI distanceText, TextMeshProUGUI comboText,
            TextMeshProUGUI fishText, Image[] heartIcons, GameObject buffRoot,
            TextMeshProUGUI buffText,
            TextMeshProUGUI feedbackText, TextMeshProUGUI bossNameText,
            TextMeshProUGUI bossPatternText, TextMeshProUGUI bossHintText, Image bossHealthFill,
            GameObject routeRoot, TextMeshProUGUI routeTitleText, TextMeshProUGUI routeChoiceText,
            GameObject upgradeRoot, TextMeshProUGUI upgradeText,
            GameObject skillRoot, TextMeshProUGUI skillText,
            GameObject mechanicRoot, TextMeshProUGUI mechanicTitleText,
            TextMeshProUGUI mechanicHintText, Image resonanceFill,
            TextMeshProUGUI contractSummaryText)
        {
            this.canvasObject = canvasObject;
            this.normalStatsRoot = normalStatsRoot;
            this.bossRoot = bossRoot;
            this.pauseRoot = pauseRoot;
            this.pauseActionRoot = pauseActionRoot;
            this.feedbackRoot = feedbackRoot;
            this.modeText = modeText;
            this.scoreText = scoreText;
            this.distanceText = distanceText;
            this.comboText = comboText;
            this.fishText = fishText;
            this.heartIcons = heartIcons;
            this.buffRoot = buffRoot;
            this.buffText = buffText;
            this.feedbackText = feedbackText;
            this.bossNameText = bossNameText;
            this.bossPatternText = bossPatternText;
            this.bossHintText = bossHintText;
            this.bossHealthFill = bossHealthFill;
            this.routeRoot = routeRoot;
            this.routeTitleText = routeTitleText;
            this.routeChoiceText = routeChoiceText;
            this.upgradeRoot = upgradeRoot;
            this.upgradeText = upgradeText;
            this.skillRoot = skillRoot;
            this.skillText = skillText;
            this.mechanicRoot = mechanicRoot;
            this.mechanicTitleText = mechanicTitleText;
            this.mechanicHintText = mechanicHintText;
            this.resonanceFill = resonanceFill;
            this.contractSummaryText = contractSummaryText;
        }

        public static RunnerHudCanvas Create(RunnerSessionConfig config, Action onPause,
            Action onResume, Action onReturnToCamp)
        {
            EnsureEventSystem();
            var canvasObject = UiBuilder.CreateCanvas("RunnerHudCanvas", null, 170);
            var root = UiBuilder.CreateRect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            ApplySafeArea(root);

            var normal = UiBuilder.CreateRect("NormalStats", root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));

            var left = CreateHudCard("RunStats", normal, new Vector2(0.018f, 0.79f), new Vector2(0.35f, 0.975f));
            var mode = CreateHudText("Mode", left, string.Empty, UiTheme.Micro, UiTheme.AccentCyan,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f), TextAlignmentOptions.Left);
            var score = CreateHudText("Score", left, "0", UiTheme.H1, Color.white,
                new Vector2(0.05f, 0.34f), new Vector2(0.58f, 0.76f), TextAlignmentOptions.Left);
            var distance = CreateHudText("Distance", left, "0 m", UiTheme.Body, UiTheme.TextSecondary,
                new Vector2(0.58f, 0.37f), new Vector2(0.95f, 0.70f), TextAlignmentOptions.Right);
            var combo = CreateHudText("Combo", left, string.Empty, UiTheme.Caption, UiTheme.WarmGold,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.33f), TextAlignmentOptions.Left);

            var right = CreateHudCard("Resources", normal, new Vector2(0.72f, 0.82f), new Vector2(0.982f, 0.975f));
            var fish = CreateHudText("Fish", right, "鱼干 0", UiTheme.Body, UiTheme.WarmGold,
                new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.92f), TextAlignmentOptions.Right);

            // 生命：心形图标行（Sprite 染色），替代 Unicode ♥♡ 文本
            var livesRow = UiBuilder.CreateRect("LivesRow", right,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.50f),
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            var livesLayout = livesRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            livesLayout.childAlignment = TextAnchor.MiddleRight;
            livesLayout.spacing = 5f;
            livesLayout.childControlWidth = false;
            livesLayout.childControlHeight = false;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;
            var hearts = new Image[MaxHeartSlots];
            for (var i = 0; i < hearts.Length; i++)
            {
                var heart = UiIcons.CreateImage($"Heart{i}", livesRow, UiIcons.HeartFull, UiTheme.Danger);
                ((RectTransform)heart.transform).sizeDelta = new Vector2(30f, 30f);
                heart.gameObject.SetActive(i < 3);
                hearts[i] = heart;
            }

            var buffCard = CreateHudCard("Buffs", normal, new Vector2(0.018f, 0.055f), new Vector2(0.31f, 0.17f));
            var buffs = CreateHudText("BuffSummary", buffCard, string.Empty, UiTheme.Caption, Color.white,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Left);
            buffCard.gameObject.SetActive(false);

            var upgradeRoot = CreateHudCard("RunBuild", normal, new Vector2(0.69f, 0.055f), new Vector2(0.982f, 0.17f));
            var upgradeText = CreateHudText("UpgradeSummary", upgradeRoot, string.Empty,
                UiTheme.Caption, UiTheme.PaperWarm,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Right);
            upgradeRoot.gameObject.SetActive(false);

            var skillRoot = CreateHudCard("SkillChain", normal, new Vector2(0.355f, 0.055f), new Vector2(0.645f, 0.15f));
            var skillText = CreateHudText("SkillSummary", skillRoot, string.Empty,
                UiTheme.Caption, UiTheme.WarmGold,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Center);
            skillRoot.gameObject.SetActive(false);

            var routeRoot = CreateHudCard("RouteChoice", normal, new Vector2(0.25f, 0.79f), new Vector2(0.75f, 0.90f));
            var routeTitle = CreateHudText("RouteTitle", routeRoot, string.Empty,
                UiTheme.Caption, UiTheme.PaperWarm,
                new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.94f), TextAlignmentOptions.Center);
            var routeChoice = CreateHudText("RouteLabels", routeRoot, string.Empty,
                UiTheme.Micro, UiTheme.WarmGold,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.52f), TextAlignmentOptions.Center);
            routeRoot.gameObject.SetActive(false);

            var mechanicRoot = CreateHudCard("MechanicBanner", root,
                new Vector2(0.28f, 0.76f), new Vector2(0.72f, 0.88f));
            var mechanicTitle = CreateHudText("MechanicTitle", mechanicRoot, string.Empty,
                UiTheme.Caption, UiTheme.AccentCyan,
                new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.94f), TextAlignmentOptions.Center);
            var mechanicHint = CreateHudText("MechanicHint", mechanicRoot, string.Empty,
                UiTheme.Micro, UiTheme.TextSecondary,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.5f), TextAlignmentOptions.Center);
            mechanicRoot.gameObject.SetActive(false);

            var resonanceTrack = UiBuilder.CreateProgressFill("Resonance", root,
                new Color(1f, 1f, 1f, 0.12f), UiTheme.AccentCyan, out var resonanceFill);
            resonanceFill.gameObject.name = "ResonanceFill";
            Place(resonanceTrack.rectTransform,
                new Vector2(0.36f, 0.185f), new Vector2(0.64f, 0.198f));

            var pause = UiBuilder.CreateButton("Pause", root, "暂停", new Color(0.05f, 0.10f, 0.16f, 0.88f),
                Color.white, UiTheme.Caption, rounded: true);
            Place((RectTransform)pause.transform, new Vector2(0.445f, 0.89f), new Vector2(0.555f, 0.975f));
            pause.onClick.AddListener(() => onPause?.Invoke());

            var feedbackRoot = UiBuilder.CreateRect("Feedback", root, new Vector2(0.24f, 0.67f),
                new Vector2(0.76f, 0.80f), Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.08f, 0.12f, 0.78f), rounded: true);
            var feedback = CreateHudText("Message", feedbackRoot, string.Empty, UiTheme.H2, Color.white,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Center);

            var bossRoot = CreateHudCard("BossFocus", root, new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.97f));
            var bossName = CreateHudText("BossName", bossRoot, string.Empty, UiTheme.H2, UiTheme.WarmGold,
                new Vector2(0.04f, 0.62f), new Vector2(0.55f, 0.95f), TextAlignmentOptions.Left);
            var pattern = CreateHudText("Pattern", bossRoot, string.Empty, UiTheme.Body, Color.white,
                new Vector2(0.55f, 0.62f), new Vector2(0.96f, 0.95f), TextAlignmentOptions.Right);
            var hint = CreateHudText("Hint", bossRoot, string.Empty, UiTheme.Caption, UiTheme.TextSecondary,
                new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.47f), TextAlignmentOptions.Center);
            var healthTrack = UiBuilder.CreateProgressFill("Health", bossRoot,
                new Color(1f, 1f, 1f, 0.14f), UiTheme.Danger, out var healthFill);
            Place(healthTrack.rectTransform, new Vector2(0.04f, 0.49f), new Vector2(0.96f, 0.59f));
            bossRoot.gameObject.SetActive(false);

            var pauseRoot = UiBuilder.CreateRect("Paused", root, new Vector2(0.25f, 0.18f),
                new Vector2(0.75f, 0.82f), Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.08f, 0.12f, 0.96f), rounded: true);
            UiBuilder.AddOutline(pauseRoot.gameObject,
                new Color(UiTheme.AccentCyan.r, UiTheme.AccentCyan.g, UiTheme.AccentCyan.b, 0.5f),
                new Vector2(2f, -2f));
            CreateHudText("PausedText", pauseRoot, "已暂停", UiTheme.H2, Color.white,
                new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.93f), TextAlignmentOptions.Center);
            var contractSummary = CreateHudText("ContractSummary", pauseRoot, string.Empty,
                UiTheme.Micro, UiTheme.PaperWarm,
                new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.70f), TextAlignmentOptions.Center);
            var resume = UiBuilder.CreateButton("Resume", pauseRoot, "继续奔跑", UiTheme.AccentCyan,
                new Color(0.02f, 0.10f, 0.14f, 1f), UiTheme.Body, rounded: true);
            Place((RectTransform)resume.transform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.43f));
            resume.onClick.AddListener(() => onResume?.Invoke());
            var returnToCamp = UiBuilder.CreateButton("ReturnToCamp", pauseRoot, "结算并返回营地",
                new Color(0.12f, 0.20f, 0.30f, 1f), Color.white, UiTheme.Caption, rounded: true);
            Place((RectTransform)returnToCamp.transform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.21f));
            returnToCamp.onClick.AddListener(() => onReturnToCamp?.Invoke());
            pauseRoot.gameObject.SetActive(false);

            var hud = new RunnerHudCanvas(canvasObject, normal.gameObject, bossRoot.gameObject,
                pauseRoot.gameObject, pause.gameObject, feedbackRoot.gameObject, mode, score, distance, combo,
                fish, hearts, buffCard.gameObject, buffs, feedback, bossName, pattern, hint, healthFill,
                routeRoot.gameObject, routeTitle, routeChoice,
                upgradeRoot.gameObject, upgradeText,
                skillRoot.gameObject, skillText,
                mechanicRoot.gameObject, mechanicTitle, mechanicHint, resonanceFill,
                contractSummary);
            hud.BindStatic(config);
            return hud;
        }

        public void BindStatic(RunnerSessionConfig config)
        {
            modeText.text = config.Mode switch
            {
                RunnerRunMode.Story => "章节旅程",
                RunnerRunMode.Daily => "每日挑战",
                _ => "极夜快跑",
            };
        }

        public void RefreshDynamic(WorldDirector world, int score, string feedback, float feedbackTimer)
        {
            if (world == null) return;
            var distance = Mathf.RoundToInt(world.Distance);
            if (score != cachedScore) { cachedScore = score; scoreText.text = score.ToString("N0"); }
            if (distance != cachedDistance) { cachedDistance = distance; distanceText.text = $"{distance:N0} m"; }
            if (world.CoinCombo != cachedCombo)
            {
                cachedCombo = world.CoinCombo;
                comboText.text = cachedCombo > 1 ? $"连续收集 ×{cachedCombo}" : string.Empty;
            }
            if (world.Coins != cachedFish) { cachedFish = world.Coins; fishText.text = $"鱼干 {cachedFish:N0}"; }
            if (world.Lives != cachedLives || world.MaxLives != cachedMaxLives)
            {
                cachedLives = world.Lives;
                cachedMaxLives = world.MaxLives;
                for (var i = 0; i < heartIcons.Length; i++)
                {
                    var visible = i < world.MaxLives;
                    heartIcons[i].gameObject.SetActive(visible);
                    if (!visible) continue;
                    var full = i < world.Lives;
                    heartIcons[i].sprite = UiIcons.Load(full ? UiIcons.HeartFull : UiIcons.HeartEmpty);
                    heartIcons[i].color = full
                        ? UiTheme.Danger
                        : new Color(UiTheme.Danger.r, UiTheme.Danger.g, UiTheme.Danger.b, 0.42f);
                }
            }

            // 道具倒计时显示粒度为 0.1s：复用 StringBuilder 并按内容比对，仅在显示值真正变化时才生成新串。
            buffSb.Clear();
            BuildBuffSummary(world, buffSb);
            if (!ContentEquals(buffSb, cachedBuffs))
            {
                cachedBuffs = buffSb.ToString();
                buffText.text = cachedBuffs;
                buffRoot.SetActive(!string.IsNullOrWhiteSpace(cachedBuffs));
            }

            if (Time.unscaledTime >= overrideFeedbackUntil)
            {
                feedbackRoot.SetActive(feedbackTimer > 0f && !string.IsNullOrWhiteSpace(feedback));
                if (feedbackRoot.activeSelf) feedbackText.text = feedback;
            }
        }

        public void SetRouteEvent(RouteEventSnapshot snapshot)
        {
            var visible = !bossMode && snapshot.AwaitingChoice;
            routeRoot.SetActive(visible);
            if (!visible) return;

            if (cachedRouteTitle == snapshot.Title
                && cachedRouteSafe == snapshot.SafeLabel
                && cachedRouteRisk == snapshot.RiskLabel) return;
            cachedRouteTitle = snapshot.Title;
            cachedRouteSafe = snapshot.SafeLabel;
            cachedRouteRisk = snapshot.RiskLabel;
            routeTitleText.text = snapshot.Title;
            routeChoiceText.text = $"← 安心：{snapshot.SafeLabel}    冒险：{snapshot.RiskLabel} →";
        }

        public void SetUpgradeSummary(RunUpgradeState upgrades)
        {
            if (upgrades == null || bossMode)
            {
                upgradeRoot.SetActive(false);
                return;
            }

            if (cachedUpgradeSelectionCount == upgrades.SelectionCount)
            {
                upgradeRoot.SetActive(cachedUpgradeSelectionCount > 0);
                return;
            }

            cachedUpgradeSelectionCount = upgrades.SelectionCount;
            upgradeRoot.SetActive(cachedUpgradeSelectionCount > 0);
            if (cachedUpgradeSelectionCount <= 0)
            {
                upgradeText.text = string.Empty;
                return;
            }

            var seen = new HashSet<RunUpgradeId>();
            var labels = new StringBuilder(80);
            var uniqueCount = 0;
            for (var i = 0; i < upgrades.SelectedIds.Count; i++)
            {
                var id = upgrades.SelectedIds[i];
                if (!seen.Add(id)) continue;
                uniqueCount++;
                if (uniqueCount > MaxVisibleUpgrades) continue;
                if (labels.Length > 0) labels.Append(" · ");
                labels.Append(RunUpgradeCatalog.For(id).Title)
                    .Append('×')
                    .Append(upgrades.GetStack(id));
            }

            var overflow = Mathf.Max(0, uniqueCount - MaxVisibleUpgrades);
            if (overflow > 0)
            {
                labels.Append("  +").Append(overflow);
            }
            upgradeText.text = labels.ToString();
        }

        public void SetSkillSnapshot(SkillScoreSnapshot snapshot)
        {
            var visible = !bossMode && snapshot.TotalScore > 0;
            skillRoot.SetActive(visible);
            if (!visible)
            {
                lastSkillTier = -1;
                cachedSkillChain = int.MinValue;
                return;
            }

            // 倍率升档的金色高亮保持 0.6s（单帧高亮肉眼不可见）；文本仅在数据变化帧重建。
            var tier = Mathf.Max(0, Mathf.RoundToInt((snapshot.Multiplier - 1f) / 0.25f));
            if (snapshot.Chain > 0 && lastSkillTier >= 0 && tier != lastSkillTier)
            {
                skillTierFlashUntil = Time.unscaledTime + 0.6f;
            }
            lastSkillTier = tier;

            if (snapshot.Chain != cachedSkillChain
                || snapshot.TotalScore != cachedSkillScore
                || snapshot.LastLabel != cachedSkillLabel
                || !Mathf.Approximately(snapshot.Multiplier, cachedSkillMultiplier))
            {
                cachedSkillChain = snapshot.Chain;
                cachedSkillScore = snapshot.TotalScore;
                cachedSkillMultiplier = snapshot.Multiplier;
                cachedSkillLabel = snapshot.LastLabel;
                skillText.text = snapshot.Chain > 0
                    ? $"{snapshot.LastLabel}  ×{snapshot.Chain}  {snapshot.Multiplier:0.00}倍  ·  技巧{snapshot.TotalScore:N0}"
                    : $"技巧分 {snapshot.TotalScore:N0}";
            }

            var expiring = snapshot.Chain > 0 && snapshot.RemainingSeconds < 0.6f;
            var highlight = expiring || Time.unscaledTime < skillTierFlashUntil;
            skillText.color = highlight ? UiTheme.WarmGold : UiTheme.TextSecondary;
            skillText.fontSize = highlight ? UiTheme.Body : UiTheme.Caption;
        }

        public void SetThemeMechanic(ThemeMechanicSnapshot snapshot)
        {
            var visible = !bossMode && snapshot.Visible
                && (snapshot.Phase == ThemeMechanicPhase.Telegraph
                    || snapshot.Phase == ThemeMechanicPhase.Active
                    || snapshot.Phase == ThemeMechanicPhase.Reward);
            mechanicRoot.SetActive(visible);
            if (!visible) return;
            mechanicTitleText.text = snapshot.Title;
            mechanicHintText.text = snapshot.Hint;
        }

        public void SetThemeMastery(ThemeMasteryHudSnapshot snapshot)
        {
            UiBuilder.SetProgress01(resonanceFill, snapshot.Resonance01);
            var progress = snapshot.Contract.Progress;
            if (progress == cachedContractProgress) return;
            cachedContractProgress = progress;
            contractSummaryText.text = $"旅途契约 · {snapshot.Contract.Title}  {progress}/{snapshot.Contract.Target}";
        }

        public void SetBossMode(BossSystem boss)
        {
            var active = boss != null && boss.BossActive && boss.CurrentBoss != null;
            bossMode = active;
            bossRoot.SetActive(active);
            normalStatsRoot.SetActive(!active);
            if (active) mechanicRoot.SetActive(false);
            if (!active) return;

            bossNameText.text = boss.CurrentBoss.Definition.DisplayName;
            bossPatternText.text = boss.CurrentBossPatternLabel;
            bossHintText.text = string.IsNullOrWhiteSpace(boss.CurrentBossCounterHint)
                ? boss.CurrentBossDodgeHint
                : boss.CurrentBossCounterHint;
            UiBuilder.SetProgress01(bossHealthFill,
                boss.CurrentBoss.HitsRemaining / (float)BossSystem.BossMaxHits);
        }

        public void SetPaused(bool paused)
        {
            pauseRoot.SetActive(paused);
            pauseActionRoot.SetActive(!paused);
        }

        public void SetFeedback(string message, Color color, float seconds)
        {
            feedbackText.text = message ?? string.Empty;
            feedbackText.color = color;
            feedbackRoot.SetActive(!string.IsNullOrWhiteSpace(message) && seconds > 0f);
            overrideFeedbackUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
        }

        public void Dispose()
        {
            if (canvasObject != null) UnityEngine.Object.Destroy(canvasObject);
        }

        private static RectTransform CreateHudCard(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var card = UiBuilder.CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.08f, 0.13f, 0.82f), rounded: true);
            UiBuilder.AddOutline(card.gameObject, new Color(UiTheme.AccentCyan.r,
                UiTheme.AccentCyan.g, UiTheme.AccentCyan.b, 0.30f), new Vector2(1f, -1f));
            return card;
        }

        private static TextMeshProUGUI CreateHudText(string name, Transform parent, string value,
            int size, Color color, Vector2 min, Vector2 max, TextAlignmentOptions alignment)
        {
            var text = UiBuilder.CreateTmpText(name, parent, value, size, FontStyle.Bold,
                TextAnchor.MiddleCenter, color);
            text.alignment = alignment;
            Place(text.rectTransform, min, max);
            return text;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplySafeArea(RectTransform root)
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var safe = Screen.safeArea;
            root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("RunnerEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystem);
        }

        private static bool ContentEquals(StringBuilder sb, string value)
        {
            if (value == null || sb.Length != value.Length) return false;
            for (var i = 0; i < value.Length; i++)
            {
                if (sb[i] != value[i]) return false;
            }
            return true;
        }

        private static void BuildBuffSummary(WorldDirector world, StringBuilder sb)
        {
            AppendBuff(sb, "冲刺", world.DashTimer);
            AppendBuff(sb, "磁力", world.MagnetTimer);
            AppendBuff(sb, "护盾", world.ShieldTimer);
            AppendBuff(sb, "滑翔", world.GlideTimer);
            AppendBuff(sb, "双倍鱼干", world.DoubleFishTimer);
            AppendBuff(sb, "星光", world.ScoreBoostTimer);
            AppendBuff(sb, "沙漏", world.SlowMoTimer);
            AppendBuff(sb, "冰镜", world.IceMirrorTimer);
            AppendBuff(sb, "极光连锁", world.AuroraChainTimer);
            AppendBuff(sb, "雾灯", world.FogLanternTimer);
            AppendBuff(sb, "雷羽", world.ThunderFeatherTimer);
            if (world.TreantArmorHits > 0)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append("树甲 ×").Append(world.TreantArmorHits);
            }
            if (world.CoralBounceReady)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append("珊瑚回弹");
            }
            if (world.FishBombs > 0)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append("鱼弹 ×").Append(world.FishBombs);
            }
        }

        private static void AppendBuff(StringBuilder sb, string label, float seconds)
        {
            if (seconds <= 0.05f) return;
            if (sb.Length > 0) sb.Append(" · ");
            sb.Append(label).Append(' ').Append(seconds.ToString("0.0")).Append('s');
        }
    }
}
