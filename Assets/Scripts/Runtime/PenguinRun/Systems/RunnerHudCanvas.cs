using System;
using System.Collections.Generic;
using System.Text;
using PenguinRun.Game;
using PenguinRun.Game.UI;
using PenguinRun.Game.UI.Lobby;
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

        // 局内 HUD 画在深色 3D 场景上，与纸张大厅是两套合法色板（见 CONTEXT「纸张 UI 层」）。
        // 但此前同一张卡上会同时出现霓虹青描边、纸暖白正文与暖金强调——三套语言混用。
        // 这里收敛成一组「霜面」令牌：冰雾色描边 / 雪白主文 / 霜蓝辅文 / 暖金奖励 / 珊瑚危险。
        private static readonly Color CardFill = new(0.03f, 0.08f, 0.13f, 0.78f);
        private static readonly Color CardStroke = new(0.72f, 0.88f, 0.94f, 0.22f);
        private static readonly Color TextSnow = new(0.97f, 0.98f, 1f, 1f);
        private static readonly Color TextFrost = new(0.72f, 0.86f, 0.94f, 0.85f);
        private static readonly Color Reward = UiTheme.WarmGold;
        private static readonly Color Danger = UiTheme.CoralAction;
        private readonly GameObject canvasObject;
        private readonly GameObject normalStatsRoot;
        private readonly BossHudPanel bossPanel;
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
        private int cachedBuffFingerprint = int.MinValue;
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
        private string cachedMechanicTitle;
        private string cachedMechanicHint;
        private int cachedResonanceCents = -1;
        private bool lastSkillHighlight;
        private string cachedFeedback;

        private RunnerHudCanvas(GameObject canvasObject, GameObject normalStatsRoot, BossHudPanel bossPanel,
            GameObject pauseRoot, GameObject pauseActionRoot, GameObject feedbackRoot,
            TextMeshProUGUI modeText,
            TextMeshProUGUI scoreText, TextMeshProUGUI distanceText, TextMeshProUGUI comboText,
            TextMeshProUGUI fishText, Image[] heartIcons, GameObject buffRoot,
            TextMeshProUGUI buffText,
            TextMeshProUGUI feedbackText,
            GameObject routeRoot, TextMeshProUGUI routeTitleText, TextMeshProUGUI routeChoiceText,
            GameObject upgradeRoot, TextMeshProUGUI upgradeText,
            GameObject skillRoot, TextMeshProUGUI skillText,
            GameObject mechanicRoot, TextMeshProUGUI mechanicTitleText,
            TextMeshProUGUI mechanicHintText, Image resonanceFill,
            TextMeshProUGUI contractSummaryText)
        {
            this.canvasObject = canvasObject;
            this.normalStatsRoot = normalStatsRoot;
            this.bossPanel = bossPanel;
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
            var mode = CreateHudText("Mode", left, string.Empty, UiTheme.Micro, TextFrost,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f), TextAlignmentOptions.Left);
            var score = CreateHudText("Score", left, "0", UiTheme.H1, TextSnow,
                new Vector2(0.05f, 0.34f), new Vector2(0.58f, 0.76f), TextAlignmentOptions.Left);
            var distance = CreateHudText("Distance", left, "0 m", UiTheme.Body, TextFrost,
                new Vector2(0.58f, 0.37f), new Vector2(0.95f, 0.70f), TextAlignmentOptions.Right);
            var combo = CreateHudText("Combo", left, string.Empty, UiTheme.Caption, Reward,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.33f), TextAlignmentOptions.Left);

            var right = CreateHudCard("Resources", normal, new Vector2(0.72f, 0.82f), new Vector2(0.982f, 0.975f));
            var fish = CreateHudText("Fish", right, "鱼干 0", UiTheme.Body, Reward,
                new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.92f), TextAlignmentOptions.Right);

            // 生命：心形图标行（Sprite 染色），替代 Unicode ♥♡ 文本
            var livesRow = UiBuilder.CreateRect("LivesRow", right,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.50f),
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            var livesLayout = livesRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            livesLayout.childAlignment = TextAnchor.MiddleRight;
            livesLayout.spacing = 6f;
            livesLayout.childControlWidth = false;
            livesLayout.childControlHeight = false;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;
            var hearts = new Image[MaxHeartSlots];
            for (var i = 0; i < hearts.Length; i++)
            {
                var heart = UiIcons.CreateImage($"Heart{i}", livesRow, UiIcons.HeartFull, Danger);
                // 生命是余光里最重要的读数：30px 在手机上约 13dp，放大到 40px。
                ((RectTransform)heart.transform).sizeDelta = new Vector2(40f, 40f);
                heart.gameObject.SetActive(i < 3);
                hearts[i] = heart;
            }

            var buffCard = CreateHudCard("Buffs", normal, new Vector2(0.018f, 0.055f), new Vector2(0.31f, 0.17f));
            var buffs = CreateHudText("BuffSummary", buffCard, string.Empty, UiTheme.Caption, TextSnow,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Left);
            buffCard.gameObject.SetActive(false);

            var upgradeRoot = CreateHudCard("RunBuild", normal, new Vector2(0.69f, 0.055f), new Vector2(0.982f, 0.17f));
            var upgradeText = CreateHudText("UpgradeSummary", upgradeRoot, string.Empty,
                UiTheme.Caption, TextSnow,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Right);
            upgradeRoot.gameObject.SetActive(false);

            var skillRoot = CreateHudCard("SkillChain", normal, new Vector2(0.355f, 0.055f), new Vector2(0.645f, 0.15f));
            var skillText = CreateHudText("SkillSummary", skillRoot, string.Empty,
                UiTheme.Caption, Reward,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Center);
            skillRoot.gameObject.SetActive(false);

            var routeRoot = CreateHudCard("RouteChoice", normal, new Vector2(0.25f, 0.79f), new Vector2(0.75f, 0.90f));
            var routeTitle = CreateHudText("RouteTitle", routeRoot, string.Empty,
                UiTheme.Caption, TextSnow,
                new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.94f), TextAlignmentOptions.Center);
            var routeChoice = CreateHudText("RouteLabels", routeRoot, string.Empty,
                UiTheme.Micro, Reward,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.52f), TextAlignmentOptions.Center);
            routeRoot.gameObject.SetActive(false);

            var mechanicRoot = CreateHudCard("MechanicBanner", root,
                new Vector2(0.28f, 0.76f), new Vector2(0.72f, 0.88f));
            var mechanicTitle = CreateHudText("MechanicTitle", mechanicRoot, string.Empty,
                UiTheme.Caption, TextSnow,
                new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.94f), TextAlignmentOptions.Center);
            var mechanicHint = CreateHudText("MechanicHint", mechanicRoot, string.Empty,
                UiTheme.Micro, TextFrost,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.5f), TextAlignmentOptions.Center);
            mechanicRoot.gameObject.SetActive(false);

            var resonanceTrack = UiBuilder.CreateProgressFill("Resonance", root,
                new Color(1f, 1f, 1f, 0.12f), TextFrost, out var resonanceFill);
            resonanceFill.gameObject.name = "ResonanceFill";
            Place(resonanceTrack.rectTransform,
                new Vector2(0.36f, 0.185f), new Vector2(0.64f, 0.198f));

            var pause = UiBuilder.CreateButton("Pause", root, "暂停", CardFill, TextSnow, UiTheme.Caption, rounded: true);
            UiBuilder.AddOutline(pause.gameObject, CardStroke, new Vector2(1f, -1f));
            Place((RectTransform)pause.transform, new Vector2(0.445f, 0.89f), new Vector2(0.555f, 0.975f));
            pause.onClick.AddListener(() => onPause?.Invoke());

            var feedbackRoot = UiBuilder.CreateRect("Feedback", root, new Vector2(0.24f, 0.67f),
                new Vector2(0.76f, 0.80f), Vector2.zero, Vector2.zero, CardFill, rounded: true);
            UiBuilder.AddOutline(feedbackRoot.gameObject, CardStroke, new Vector2(1f, -1f));
            var feedback = CreateHudText("Message", feedbackRoot, string.Empty, UiTheme.H2, TextSnow,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Center);

            var bossPanel = BossHudPanel.Create(root);

            var pauseRoot = UiBuilder.CreateRect("Paused", root, new Vector2(0.25f, 0.18f),
                new Vector2(0.75f, 0.82f), Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.08f, 0.12f, 0.96f), rounded: true);
            UiBuilder.AttachArtVignette("RunBackdrop", pauseRoot, CampVisualAssetCatalog.RunBackdrop, 0.14f);
            UiBuilder.AddOutline(pauseRoot.gameObject, CardStroke, new Vector2(2f, -2f));
            CreateHudText("PausedText", pauseRoot, "已暂停", UiTheme.H2, TextSnow,
                new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.93f), TextAlignmentOptions.Center);
            var contractSummary = CreateHudText("ContractSummary", pauseRoot, string.Empty,
                UiTheme.Micro, TextFrost,
                new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.70f), TextAlignmentOptions.Center);
            // 主操作与大厅底坞同一语言：珊瑚底 + 白字；次级为霜蓝底。
            var resume = UiBuilder.CreateButton("Resume", pauseRoot, "继续奔跑", UiTheme.CoralAction,
                Color.white, UiTheme.Body, rounded: true);
            Place((RectTransform)resume.transform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.43f));
            resume.onClick.AddListener(() => onResume?.Invoke());
            var returnToCamp = UiBuilder.CreateButton("ReturnToCamp", pauseRoot, "结算并返回营地",
                new Color(0.16f, 0.26f, 0.34f, 1f), TextSnow, UiTheme.Caption, rounded: true);
            Place((RectTransform)returnToCamp.transform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.21f));
            returnToCamp.onClick.AddListener(() => onReturnToCamp?.Invoke());
            pauseRoot.gameObject.SetActive(false);

            var hud = new RunnerHudCanvas(canvasObject, normal.gameObject, bossPanel,
                pauseRoot.gameObject, pause.gameObject, feedbackRoot.gameObject, mode, score, distance, combo,
                fish, hearts, buffCard.gameObject, buffs, feedback,
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
                // 离散事件才跳动：连击推进时给一次 punch（分数每帧变，不做跳动）。
                var comboAdvanced = cachedCombo != int.MinValue && world.CoinCombo > cachedCombo && world.CoinCombo > 1;
                cachedCombo = world.CoinCombo;
                comboText.text = cachedCombo > 1 ? $"连续收集 ×{cachedCombo}" : string.Empty;
                if (comboAdvanced) comboText.transform.PlayClickBounce();
            }
            if (world.Coins != cachedFish)
            {
                var fishGained = cachedFish != int.MinValue && world.Coins > cachedFish;
                cachedFish = world.Coins;
                fishText.text = $"鱼干 {cachedFish:N0}";
                if (fishGained) fishText.transform.PlayClickBounce();
            }
            if (world.Lives != cachedLives || world.MaxLives != cachedMaxLives)
            {
                var previousLives = cachedLives;
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
                        ? Danger
                        : new Color(Danger.r, Danger.g, Danger.b, 0.38f);
                }
                // 掉血/回血的那颗心 punch 一次，让余光也能捕捉到生命变化。
                if (previousLives != int.MinValue)
                {
                    var changedIndex = Mathf.Clamp(Mathf.Min(previousLives, world.Lives), 0, heartIcons.Length - 1);
                    if (heartIcons[changedIndex].gameObject.activeSelf)
                        heartIcons[changedIndex].transform.PlayClickBounce();
                }
            }

            // 道具倒计时显示粒度为 0.1s。先用「量化后的显示值指纹」判断这一帧的显示内容
            // 是否会变；不变就整段跳过——否则即便最终不改 text，StringBuilder 的构建和其中
            // 每个 buff 的 ToString("0.0") 仍会每帧分配。
            var buffFingerprint = ComputeBuffFingerprint(world);
            if (buffFingerprint != cachedBuffFingerprint)
            {
                cachedBuffFingerprint = buffFingerprint;
                buffSb.Clear();
                BuildBuffSummary(world, buffSb);
                if (!ContentEquals(buffSb, cachedBuffs))
                {
                    cachedBuffs = buffSb.ToString();
                    buffText.text = cachedBuffs;
                    buffRoot.SetActive(!string.IsNullOrWhiteSpace(cachedBuffs));
                }
            }

            if (Time.unscaledTime >= overrideFeedbackUntil)
            {
                feedbackRoot.SetActive(feedbackTimer > 0f && !string.IsNullOrWhiteSpace(feedback));
                // 提示显示期间每帧进入：同一条消息不重复写 .text；换新消息时弹一次。
                if (feedbackRoot.activeSelf && !ReferenceEquals(feedback, cachedFeedback) && feedback != cachedFeedback)
                {
                    cachedFeedback = feedback;
                    feedbackText.text = feedback;
                    feedbackRoot.transform.PlayClickBounce();
                }
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
            routeChoiceText.text = $"左道 安心：{snapshot.SafeLabel}    右道 冒险：{snapshot.RiskLabel}";
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
            // 颜色/字号只在高亮状态翻转的那一帧写入。
            if (highlight != lastSkillHighlight)
            {
                lastSkillHighlight = highlight;
                skillText.color = highlight ? Reward : TextFrost;
                skillText.fontSize = highlight ? UiTheme.Body : UiTheme.Caption;
            }
        }

        public void SetThemeMechanic(ThemeMechanicSnapshot snapshot)
        {
            var visible = !bossMode && snapshot.Visible
                && (snapshot.Phase == ThemeMechanicPhase.Telegraph
                    || snapshot.Phase == ThemeMechanicPhase.Active
                    || snapshot.Phase == ThemeMechanicPhase.Reward);
            mechanicRoot.SetActive(visible);
            if (!visible) return;
            // 机关可见期间每帧进入：文本按值变更门控，避免逐帧写脏 Canvas。
            if (cachedMechanicTitle != snapshot.Title)
            {
                cachedMechanicTitle = snapshot.Title;
                mechanicTitleText.text = snapshot.Title;
            }
            if (cachedMechanicHint != snapshot.Hint)
            {
                cachedMechanicHint = snapshot.Hint;
                mechanicHintText.text = snapshot.Hint;
            }
        }

        public void SetThemeMastery(ThemeMasteryHudSnapshot snapshot)
        {
            // 共鸣条按 1% 粒度门控：SetProgress01 改 sizeDelta 即使值相同也会标脏。
            var resonanceCents = Mathf.RoundToInt(Mathf.Clamp01(snapshot.Resonance01) * 100f);
            if (resonanceCents != cachedResonanceCents)
            {
                cachedResonanceCents = resonanceCents;
                UiBuilder.SetProgress01(resonanceFill, resonanceCents / 100f);
            }
            var progress = snapshot.Contract.Progress;
            if (progress == cachedContractProgress) return;
            cachedContractProgress = progress;
            contractSummaryText.text = $"旅途契约 · {snapshot.Contract.Title}  {progress}/{snapshot.Contract.Target}";
        }

        public void SetBossMode(BossSystem boss)
        {
            var active = boss != null && boss.BossActive && boss.CurrentBoss != null;
            bossMode = active;
            normalStatsRoot.SetActive(!active);
            if (!active)
            {
                bossPanel.Hide();
                return;
            }

            mechanicRoot.SetActive(false);
            bossPanel.Show();
            bossPanel.Refresh(boss);
        }

        public void SetPaused(bool paused)
        {
            var wasActive = pauseRoot.activeSelf;
            pauseRoot.SetActive(paused);
            pauseActionRoot.SetActive(!paused);
            // 暂停面板从瞬切改为弹出（SetUpdate(true)，timeScale=0 时照常播放）。
            if (paused && !wasActive)
                ((RectTransform)pauseRoot.transform).PlayPanelPopIn(0.22f);
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
            var card = UiBuilder.CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero, CardFill, rounded: true);
            UiBuilder.AddOutline(card.gameObject, CardStroke, new Vector2(1f, -1f));
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

        /// <summary>
        /// 把所有 buff 的**显示值**（0.1s 量化后的秒数与计数）折成一个整数指纹。
        /// 指纹相同即代表这一帧渲染出来的摘要与上一帧逐字符相同，可以整段跳过重建。
        /// </summary>
        private static int ComputeBuffFingerprint(WorldDirector world)
        {
            var h = 17;
            h = h * 31 + Tenths(world.DashTimer);
            h = h * 31 + Tenths(world.MagnetTimer);
            h = h * 31 + Tenths(world.ShieldTimer);
            h = h * 31 + Tenths(world.GlideTimer);
            h = h * 31 + Tenths(world.DoubleFishTimer);
            h = h * 31 + Tenths(world.ScoreBoostTimer);
            h = h * 31 + Tenths(world.SlowMoTimer);
            h = h * 31 + Tenths(world.IceMirrorTimer);
            h = h * 31 + Tenths(world.AuroraChainTimer);
            h = h * 31 + Tenths(world.FogLanternTimer);
            h = h * 31 + Tenths(world.ThunderFeatherTimer);
            h = h * 31 + world.TreantArmorHits;
            h = h * 31 + (world.CoralBounceReady ? 1 : 0);
            h = h * 31 + world.FishBombs;
            return h;
        }

        /// <summary>秒数量化到 0.1s，与 AppendBuff 的 "0.0" 显示格式对齐。</summary>
        private static int Tenths(float seconds)
        {
            return seconds <= 0.05f ? -1 : Mathf.RoundToInt(seconds * 10f);
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
