using System;
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
        private readonly GameObject canvasObject;
        private readonly GameObject normalStatsRoot;
        private readonly GameObject bossRoot;
        private readonly GameObject pauseRoot;
        private readonly GameObject feedbackRoot;
        private readonly TextMeshProUGUI modeText;
        private readonly TextMeshProUGUI scoreText;
        private readonly TextMeshProUGUI distanceText;
        private readonly TextMeshProUGUI comboText;
        private readonly TextMeshProUGUI fishText;
        private readonly TextMeshProUGUI livesText;
        private readonly TextMeshProUGUI buffText;
        private readonly TextMeshProUGUI feedbackText;
        private readonly TextMeshProUGUI bossNameText;
        private readonly TextMeshProUGUI bossPatternText;
        private readonly TextMeshProUGUI bossHintText;
        private readonly Image bossHealthFill;

        private int cachedScore = int.MinValue;
        private int cachedDistance = int.MinValue;
        private int cachedCombo = int.MinValue;
        private int cachedFish = int.MinValue;
        private int cachedLives = int.MinValue;
        private string cachedBuffs;
        private float overrideFeedbackUntil;

        private RunnerHudCanvas(GameObject canvasObject, GameObject normalStatsRoot, GameObject bossRoot,
            GameObject pauseRoot, GameObject feedbackRoot, TextMeshProUGUI modeText,
            TextMeshProUGUI scoreText, TextMeshProUGUI distanceText, TextMeshProUGUI comboText,
            TextMeshProUGUI fishText, TextMeshProUGUI livesText, TextMeshProUGUI buffText,
            TextMeshProUGUI feedbackText, TextMeshProUGUI bossNameText,
            TextMeshProUGUI bossPatternText, TextMeshProUGUI bossHintText, Image bossHealthFill)
        {
            this.canvasObject = canvasObject;
            this.normalStatsRoot = normalStatsRoot;
            this.bossRoot = bossRoot;
            this.pauseRoot = pauseRoot;
            this.feedbackRoot = feedbackRoot;
            this.modeText = modeText;
            this.scoreText = scoreText;
            this.distanceText = distanceText;
            this.comboText = comboText;
            this.fishText = fishText;
            this.livesText = livesText;
            this.buffText = buffText;
            this.feedbackText = feedbackText;
            this.bossNameText = bossNameText;
            this.bossPatternText = bossPatternText;
            this.bossHintText = bossHintText;
            this.bossHealthFill = bossHealthFill;
        }

        public static RunnerHudCanvas Create(RunnerSessionConfig config, Action onExit)
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
            var lives = CreateHudText("Lives", right, "♥♥♥", UiTheme.Body, UiTheme.Danger,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.50f), TextAlignmentOptions.Right);

            var buffCard = CreateHudCard("Buffs", normal, new Vector2(0.018f, 0.055f), new Vector2(0.31f, 0.17f));
            var buffs = CreateHudText("BuffSummary", buffCard, string.Empty, UiTheme.Caption, Color.white,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Left);

            var exit = UiBuilder.CreateButton("Exit", root, "返回营地", new Color(0.05f, 0.10f, 0.16f, 0.76f),
                Color.white, UiTheme.Caption, rounded: true);
            Place((RectTransform)exit.transform, new Vector2(0.445f, 0.91f), new Vector2(0.555f, 0.975f));
            exit.onClick.AddListener(() => onExit?.Invoke());

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

            var pauseRoot = UiBuilder.CreateRect("Paused", root, new Vector2(0.39f, 0.44f),
                new Vector2(0.61f, 0.57f), Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.08f, 0.12f, 0.88f), rounded: true);
            CreateHudText("PausedText", pauseRoot, "已暂停", UiTheme.H2, Color.white,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            pauseRoot.gameObject.SetActive(false);

            var hud = new RunnerHudCanvas(canvasObject, normal.gameObject, bossRoot.gameObject,
                pauseRoot.gameObject, feedbackRoot.gameObject, mode, score, distance, combo,
                fish, lives, buffs, feedback, bossName, pattern, hint, healthFill);
            hud.BindStatic(config);
            return hud;
        }

        public void BindStatic(RunnerSessionConfig config)
        {
            modeText.text = config.Daily ? "每日挑战" : "极夜快跑";
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
            if (world.Lives != cachedLives)
            {
                cachedLives = world.Lives;
                livesText.text = new string('♥', Mathf.Max(0, world.Lives)) +
                    new string('♡', Mathf.Max(0, world.MaxLives - world.Lives));
            }

            var buffs = BuildBuffSummary(world);
            if (buffs != cachedBuffs) { cachedBuffs = buffs; buffText.text = buffs; }

            if (Time.unscaledTime >= overrideFeedbackUntil)
            {
                feedbackRoot.SetActive(feedbackTimer > 0f && !string.IsNullOrWhiteSpace(feedback));
                if (feedbackRoot.activeSelf) feedbackText.text = feedback;
            }
        }

        public void SetBossMode(BossSystem boss)
        {
            var active = boss != null && boss.BossActive && boss.CurrentBoss != null;
            bossRoot.SetActive(active);
            normalStatsRoot.SetActive(!active);
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

        private static string BuildBuffSummary(WorldDirector world)
        {
            var sb = new StringBuilder(96);
            AppendBuff(sb, "冲刺", world.DashTimer);
            AppendBuff(sb, "磁力", world.MagnetTimer);
            AppendBuff(sb, "护盾", world.ShieldTimer);
            AppendBuff(sb, "滑翔", world.GlideTimer);
            AppendBuff(sb, "双倍鱼干", world.DoubleFishTimer);
            if (world.TreantArmorHits > 0)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append("树甲 ×").Append(world.TreantArmorHits);
            }
            return sb.ToString();
        }

        private static void AppendBuff(StringBuilder sb, string label, float seconds)
        {
            if (seconds <= 0.05f) return;
            if (sb.Length > 0) sb.Append(" · ");
            sb.Append(label).Append(' ').Append(seconds.ToString("0.0")).Append('s');
        }
    }
}
