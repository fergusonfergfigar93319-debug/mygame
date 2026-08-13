using PenguinRun.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun
{
    /// <summary>
    /// Boss 战专属 HUD 面板。把 Boss 相关部件从 RunnerHudCanvas 的构造参数里分出来，
    /// 并把 BossSystem 已经算好、但此前没有出口的两项节奏数据接上：
    /// 距下次破绽还有几招（节拍点），以及速杀奖励窗口剩余时间。
    ///
    /// 表现层约定：本面板只读 BossSystem 的公开快照，不参与任何命中判定。
    /// </summary>
    internal sealed class BossHudPanel
    {
        /// <summary>血条分段数与 BossSystem.BossMaxHits 对齐；跑动中离散段位比连续长度好读。</summary>
        private const int MaxHealthSegments = BossSystem.BossMaxHits;

        /// <summary>破绽节拍点上限：招式数超过此值时退化为文字，避免挤成一排噪点。</summary>
        private const int MaxRhythmPips = 4;

        private static readonly Color HealthLost = new(1f, 0.35f, 0.45f, 0.16f);
        private static readonly Color RhythmPending = new(0.78f, 0.86f, 0.94f, 0.30f);

        public readonly GameObject Root;
        private readonly TextMeshProUGUI nameText;
        private readonly TextMeshProUGUI patternText;
        private readonly TextMeshProUGUI hintText;
        private readonly Image[] healthSegments;
        private readonly Image[] rhythmPips;
        private readonly GameObject rhythmRoot;
        private readonly TextMeshProUGUI rhythmLabel;
        private readonly GameObject speedRunRoot;
        private readonly Image speedRunFill;

        private int cachedHits = int.MinValue;
        private int cachedPatternsLeft = int.MinValue;
        /// <summary>速杀进度条按 1% 粒度门控的缓存值（RefreshSpeedRun）。</summary>
        private int cachedSpeedRunCents = int.MinValue;
        private string cachedName;
        private string cachedPattern;
        private string cachedHint;
        private bool cachedVulnerable;

        private BossHudPanel(GameObject root, TextMeshProUGUI nameText, TextMeshProUGUI patternText,
            TextMeshProUGUI hintText, Image[] healthSegments, GameObject rhythmRoot, Image[] rhythmPips,
            TextMeshProUGUI rhythmLabel, GameObject speedRunRoot, Image speedRunFill)
        {
            Root = root;
            this.nameText = nameText;
            this.patternText = patternText;
            this.hintText = hintText;
            this.healthSegments = healthSegments;
            this.rhythmRoot = rhythmRoot;
            this.rhythmPips = rhythmPips;
            this.rhythmLabel = rhythmLabel;
            this.speedRunRoot = speedRunRoot;
            this.speedRunFill = speedRunFill;
        }

        public static BossHudPanel Create(Transform parent)
        {
            var card = UiBuilder.CreateRect("BossFocus", parent,
                new Vector2(0.25f, 0.735f), new Vector2(0.75f, 0.975f),
                Vector2.zero, Vector2.zero, new Color(0.03f, 0.08f, 0.13f, 0.86f), rounded: true);
            UiBuilder.AddOutline(card.gameObject,
                new Color(UiTheme.Danger.r, UiTheme.Danger.g, UiTheme.Danger.b, 0.34f), new Vector2(1f, -1f));

            var name = Text("BossName", card, UiTheme.H2, UiTheme.WarmGold,
                new Vector2(0.04f, 0.70f), new Vector2(0.56f, 0.96f), TextAlignmentOptions.Left);
            var pattern = Text("Pattern", card, UiTheme.Body, Color.white,
                new Vector2(0.56f, 0.70f), new Vector2(0.96f, 0.96f), TextAlignmentOptions.Right);

            // 血条：等宽分段，段间留空。玩家只用余光就能数出「还剩几下」。
            var healthRow = UiBuilder.CreateRect("HealthRow", card,
                new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.66f),
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            var segments = new Image[MaxHealthSegments];
            var segStep = 1f / MaxHealthSegments;
            for (var i = 0; i < MaxHealthSegments; i++)
            {
                var seg = UiBuilder.CreateRect($"HealthSeg{i}", healthRow,
                    new Vector2(i * segStep + 0.006f, 0f), new Vector2((i + 1) * segStep - 0.006f, 1f),
                    Vector2.zero, Vector2.zero, UiTheme.Danger, rounded: true);
                segments[i] = seg.GetComponent<Image>();
            }

            // 破绽节拍：躲过一招点亮一颗，全亮即进入反击窗口。把「挨打等机会」变成可预期的节拍。
            var rhythmRow = UiBuilder.CreateRect("VulnerableRhythm", card,
                new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.52f),
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            var rhythmLabel = Text("RhythmLabel", rhythmRow, UiTheme.Micro, UiTheme.TextSecondary,
                new Vector2(0f, 0f), new Vector2(0.46f, 1f), TextAlignmentOptions.Left);
            var pips = new Image[MaxRhythmPips];
            for (var i = 0; i < MaxRhythmPips; i++)
            {
                var pip = UiBuilder.CreateRect($"Pip{i}", rhythmRow,
                    new Vector2(0.50f + i * 0.125f, 0.18f), new Vector2(0.50f + i * 0.125f + 0.085f, 0.82f),
                    Vector2.zero, Vector2.zero, RhythmPending, circle: true);
                pips[i] = pip.GetComponent<Image>();
            }

            var hint = Text("Hint", card, UiTheme.Caption, UiTheme.TextSecondary,
                new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.37f), TextAlignmentOptions.Center);

            // 速杀窗口：只在窗口内出现，过期即隐藏——不给玩家留已经失效的信息。
            var speedTrack = UiBuilder.CreateProgressFill("SpeedRun", card,
                new Color(1f, 1f, 1f, 0.10f), UiTheme.WarmGold, out var speedFill);
            Place(speedTrack.rectTransform, new Vector2(0.04f, 0.055f), new Vector2(0.96f, 0.115f));

            card.gameObject.SetActive(false);
            return new BossHudPanel(card.gameObject, name, pattern, hint, segments,
                rhythmRow.gameObject, pips, rhythmLabel, speedTrack.gameObject, speedFill);
        }

        public void Refresh(BossSystem boss)
        {
            var encounter = boss.CurrentBoss;
            if (encounter == null) return;

            var displayName = encounter.Definition.DisplayName;
            if (displayName != cachedName)
            {
                cachedName = displayName;
                nameText.text = displayName;
            }

            var patternLabel = boss.CurrentBossPatternLabel;
            if (patternLabel != cachedPattern)
            {
                cachedPattern = patternLabel;
                patternText.text = patternLabel;
            }

            var vulnerable = encounter.Phase == BossPhase.Vulnerable;
            if (vulnerable != cachedVulnerable)
            {
                cachedVulnerable = vulnerable;
                // 破绽期整块面板转绿：这是全场唯一的进攻窗口，值得压过其它配色。
                patternText.color = vulnerable ? UiTheme.Success : Color.white;
            }

            var hint = string.IsNullOrWhiteSpace(boss.CurrentBossCounterHint)
                ? boss.CurrentBossDodgeHint
                : boss.CurrentBossCounterHint;
            if (hint != cachedHint)
            {
                cachedHint = hint;
                hintText.text = hint;
            }

            if (encounter.HitsRemaining != cachedHits)
            {
                cachedHits = encounter.HitsRemaining;
                for (var i = 0; i < healthSegments.Length; i++)
                {
                    healthSegments[i].color = i < cachedHits ? UiTheme.Danger : HealthLost;
                }
            }

            RefreshRhythm(boss, vulnerable);
            RefreshSpeedRun(boss);
        }

        private void RefreshRhythm(BossSystem boss, bool vulnerable)
        {
            var remaining = boss.CurrentBossPatternsUntilVulnerable;
            // 破绽期本身不需要节拍提示：此刻窗口已经开了。
            var visible = !vulnerable && remaining > 0 && remaining <= MaxRhythmPips;
            if (rhythmRoot.activeSelf != visible) rhythmRoot.SetActive(visible);
            if (!visible || remaining == cachedPatternsLeft) return;

            cachedPatternsLeft = remaining;
            rhythmLabel.text = "破绽倒数";
            for (var i = 0; i < rhythmPips.Length; i++)
            {
                if (i >= MaxRhythmPips) break;
                var pending = i < remaining;
                rhythmPips[i].gameObject.SetActive(i < MaxRhythmPips);
                rhythmPips[i].color = pending ? RhythmPending : UiTheme.Success;
            }
        }

        private void RefreshSpeedRun(BossSystem boss)
        {
            var window = boss.CurrentBossSpeedRewardWindow;
            var left = boss.CurrentBossSpeedRewardTimeLeft;
            var visible = window > 0.01f && left > 0f;
            if (speedRunRoot.activeSelf != visible) speedRunRoot.SetActive(visible);
            if (!visible) return;
            // 进度条按 1% 粒度门控：值不变时跳过 sizeDelta 写入，避免逐帧标脏。
            var cents = Mathf.RoundToInt(Mathf.Clamp01(left / window) * 100f);
            if (cents == cachedSpeedRunCents) return;
            cachedSpeedRunCents = cents;
            UiBuilder.SetProgress01(speedRunFill, cents / 100f);
        }

        public void Hide()
        {
            if (Root.activeSelf) Root.SetActive(false);
            cachedHits = int.MinValue;
            cachedPatternsLeft = int.MinValue;
            cachedName = null;
            cachedPattern = null;
            cachedHint = null;
            cachedSpeedRunCents = int.MinValue;
        }

        public void Show()
        {
            if (!Root.activeSelf) Root.SetActive(true);
        }

        private static TextMeshProUGUI Text(string name, Transform parent, int size, Color color,
            Vector2 min, Vector2 max, TextAlignmentOptions alignment)
        {
            var text = UiBuilder.CreateTmpText(name, parent, string.Empty, size, FontStyle.Bold,
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
    }
}
