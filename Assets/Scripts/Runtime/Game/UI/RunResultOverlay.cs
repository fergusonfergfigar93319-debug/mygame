using PenguinRun.Game;
using PenguinRun.Game.Save;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 跑酷一局结束后回到主菜单时弹出的结算面板。
    /// 重构：分层设计、统计卡片网格、动画化得分展示、新纪录庆祝特效。
    /// </summary>
    public static class RunResultOverlay
    {
        public static void Show(Transform parent, RunOutcomeRouter.Result r, System.Action onDismiss)
        {
            // 全屏背景遮罩（新纪录时稍微降低透明度让金色更突出）
            var bgAlpha = r.NewBest ? 0.82f : 0.88f;
            var root = UiBuilder.CreateRect(
                "RunResultOverlay", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, bgAlpha));

            // 新纪录：背景金色粒子光晕
            if (r.NewBest)
            {
                var celebrationBg = UiBuilder.CreateRect("CelebrationBg", root,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(1600f, 1600f), Vector2.zero,
                    new Color(1f, 0.75f, 0.2f, 0.08f), glow: true);
                celebrationBg.GetComponent<Image>().raycastTarget = false;
                celebrationBg.gameObject.AddComponent<CelebrationPulse>();
            }

            // 中央卡片：圆角、阴影、发光边（新纪录用金色）
            var panel = UiBuilder.CreateRect(
                "Panel", root,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(920f, 920f), Vector2.zero,  // 加高一点给破纪录徽章留空间
                UiBuilder.Surface1, rounded: true);
            UiBuilder.AddOutline(panel.gameObject,
                r.NewBest ? new Color(1f, 0.84f, 0.32f, 0.75f) : new Color(0f, 0.85f, 0.95f, 0.55f),
                new Vector2(3f, -3f));
            UiBuilder.AddShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.7f), new Vector2(0f, -10f));

            // 顶部弧形辉光（新纪录用更强金色光效）
            var topGlowHeight = r.NewBest ? 320f : 240f;
            var topGlowAlpha = r.NewBest ? 0.28f : 0.16f;
            var topGlow = UiBuilder.CreateRect(
                "TopGlow", panel,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, topGlowHeight), new Vector2(0f, -topGlowHeight * 0.5f),
                r.NewBest
                    ? new Color(1f, 0.7f, 0.2f, topGlowAlpha)
                    : new Color(0f, 0.85f, 0.95f, 0.16f),
                rounded: true);
            topGlow.GetComponent<Image>().raycastTarget = false;

            // 顶部高光线（新纪录用更宽的金色条）
            var topLineHeight = r.NewBest ? 6f : 4f;
            var topAccent = UiBuilder.CreateRect(
                "TopAccent", panel,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, topLineHeight), new Vector2(0f, -2f),
                r.NewBest ? UiBuilder.WarmGold : UiBuilder.AccentCyan, rounded: true);
            topAccent.GetComponent<Image>().raycastTarget = false;

            // 新纪录：顶部闪烁星星装饰
            if (r.NewBest)
            {
                CreateCelebrationStars(panel, 8);
            }

            // 标题徽章（圆形）- 新纪录时更大且有脉冲动画
            var badgeSize = r.NewBest ? 140f : 110f;
            var badgeY = r.NewBest ? -45f : -50f;
            var badgeColor = r.NewBest
                ? new Color(1f, 0.84f, 0.32f, 0.35f)
                : new Color(0f, 0.85f, 0.95f, 0.2f);
            var headlineBadge = UiBuilder.CreateRect("HeadlineBadge", panel,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(badgeSize, badgeSize), new Vector2(0f, badgeY),
                badgeColor, circle: true);
            UiBuilder.AddOutline(headlineBadge.gameObject,
                r.NewBest ? new Color(1f, 0.9f, 0.5f, 0.9f) : UiBuilder.AccentCyan,
                new Vector2(3f, -3f));
            UiBuilder.CreateText("BadgeIcon", headlineBadge.transform,
                r.NewBest ? "\u2605" : "\u2713",
                r.NewBest ? 72 : 56, FontStyle.Bold, TextAnchor.MiddleCenter,
                r.NewBest ? UiBuilder.WarmGold : UiBuilder.AccentCyan);

            // 新纪录时添加脉冲动画
            if (r.NewBest)
            {
                headlineBadge.gameObject.AddComponent<CelebrationPulse>();
            }

            // 主标题
            var headlineSize = r.NewBest ? 62 : 52;
            var headline = UiBuilder.CreateText("Headline", panel,
                r.NewBest ? "\u2605 新纪录！ \u2605" : "本局完成",
                headlineSize, FontStyle.Bold, TextAnchor.MiddleCenter,
                r.NewBest ? UiBuilder.WarmGold : Color.white);
            var hrt = (RectTransform)headline.transform;
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.sizeDelta = new Vector2(0f, 90f);
            hrt.anchoredPosition = new Vector2(0f, r.NewBest ? -195f : -180f);
            UiBuilder.AddShadow(headline.gameObject, new Color(0f, 0.05f, 0.15f, 0.65f), new Vector2(0f, -4f));

            // 新纪录时标题也加脉冲
            if (r.NewBest)
            {
                headline.gameObject.AddComponent<CelebrationPulse>().amplitude = 0.03f;
            }

            // 副标题
            var sublineText = r.NewBest
                ? $"超越前纪录 {PlayerSave.BestScore - r.BonusScore:N0} 分！"
                : "再来一局，挑战新纪录";
            var subline = UiBuilder.CreateText("Subline", panel,
                sublineText,
                22, FontStyle.Bold, TextAnchor.MiddleCenter,
                r.NewBest ? new Color(1f, 0.85f, 0.4f, 0.9f) : UiBuilder.TextSecondary);
            var sublRt = (RectTransform)subline.transform;
            sublRt.anchorMin = new Vector2(0f, 1f);
            sublRt.anchorMax = new Vector2(1f, 1f);
            sublRt.pivot = new Vector2(0.5f, 1f);
            sublRt.sizeDelta = new Vector2(0f, 32f);
            sublRt.anchoredPosition = new Vector2(0f, -280f);

            // 得分大字（中央亮点）
            var scoreCard = UiBuilder.CreateRect("ScoreCard", panel,
                new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f),
                new Vector2(0f, 220f), new Vector2(0f, 60f),
                new Color(0.04f, 0.09f, 0.16f, 1f), rounded: true);
            UiBuilder.AddOutline(scoreCard.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var scoreLabel = UiBuilder.CreateText("ScoreLabel", scoreCard.transform,
                "总得分", 22, FontStyle.Bold, TextAnchor.UpperCenter, UiBuilder.TextSecondary);
            var slrt = (RectTransform)scoreLabel.transform;
            slrt.anchorMin = new Vector2(0f, 1f);
            slrt.anchorMax = new Vector2(1f, 1f);
            slrt.pivot = new Vector2(0.5f, 1f);
            slrt.sizeDelta = new Vector2(0f, 40f);
            slrt.anchoredPosition = new Vector2(0f, -20f);

            var score = UiBuilder.CreateText("Score", scoreCard.transform,
                r.Score.ToString("N0"), 110, FontStyle.Bold, TextAnchor.MiddleCenter,
                r.NewBest ? UiBuilder.WarmGold : Color.white);
            var srt = (RectTransform)score.transform;
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.sizeDelta = Vector2.zero;
            srt.offsetMin = new Vector2(0f, 50f);
            srt.offsetMax = new Vector2(0f, -42f);
            UiBuilder.AddShadow(score.gameObject, new Color(0f, 0.05f, 0.15f, 0.55f), new Vector2(0f, -4f));

            // 排名信息（在得分下方）
            var rank = UiBuilder.CreateText("Rank", scoreCard.transform,
                BuildRankLabel(r), 22, FontStyle.Bold,
                TextAnchor.LowerCenter,
                r.NewBest ? UiBuilder.WarmGold : UiBuilder.AccentCyan);
            var rrt = (RectTransform)rank.transform;
            rrt.anchorMin = new Vector2(0f, 0f);
            rrt.anchorMax = new Vector2(1f, 0f);
            rrt.pivot = new Vector2(0.5f, 0f);
            rrt.sizeDelta = new Vector2(0f, 36f);
            rrt.anchoredPosition = new Vector2(0f, 14f);

            // 三栏统计网格（距离 / 鱼干 / 存活）
            var gridY = -240f;
            var gridStartX = 0.08f;
            var gridEndX = 0.92f;
            var cellWidth = (gridEndX - gridStartX) / 3f;
            var spacing = 0.01f;

            BuildStatCell(panel, gridStartX + spacing, gridStartX + cellWidth - spacing, gridY,
                "\u279E", "距离", $"{r.DistanceMeters}m", new Color(0.55f, 0.85f, 1f, 1f));
            BuildStatCell(panel, gridStartX + cellWidth + spacing, gridStartX + 2 * cellWidth - spacing, gridY,
                "\u2740", "鱼干", $"{r.Coins}", UiBuilder.WarmGold);
            BuildStatCell(panel, gridStartX + 2 * cellWidth + spacing, gridEndX - spacing, gridY,
                "\u231B", "存活", $"{Mathf.RoundToInt(r.SurvivalSeconds)}s", new Color(0.4f, 0.95f, 0.7f, 1f));

            BuildBonusBanner(panel, r);

            // 主操作按钮：返回主菜单（青色 CTA）
            var dismiss = UiBuilder.CreatePrimaryButton(
                "Dismiss", panel.transform, "返回主菜单", 32, null);
            var brt = (RectTransform)dismiss.transform;
            brt.anchorMin = new Vector2(0.1f, 0f);
            brt.anchorMax = new Vector2(0.9f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(0f, 96f);
            brt.anchoredPosition = new Vector2(0f, 56f);
            dismiss.onClick.AddListener(() =>
            {
                Object.Destroy(root.gameObject);
                onDismiss?.Invoke();
            });
        }

        private static void BuildStatCell(RectTransform parent,
            float anchorXMin, float anchorXMax, float anchoredY,
            string icon, string label, string value, Color color)
        {
            var cell = UiBuilder.CreateRect("Stat_" + label, parent,
                new Vector2(anchorXMin, 0.5f), new Vector2(anchorXMax, 0.5f),
                new Vector2(0f, 130f), new Vector2(0f, anchoredY),
                new Color(0.04f, 0.09f, 0.16f, 1f), rounded: true);
            UiBuilder.AddOutline(cell.gameObject,
                new Color(color.r, color.g, color.b, 0.35f), new Vector2(1f, -1f));

            // 图标
            var iconText = UiBuilder.CreateText("Icon", cell.transform, icon,
                36, FontStyle.Bold, TextAnchor.UpperCenter, color);
            var irt = (RectTransform)iconText.transform;
            irt.anchorMin = new Vector2(0f, 1f);
            irt.anchorMax = new Vector2(1f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.sizeDelta = new Vector2(0f, 44f);
            irt.anchoredPosition = new Vector2(0f, -10f);

            // 数值
            var valueText = UiBuilder.CreateText("Value", cell.transform, value,
                28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var vrt = (RectTransform)valueText.transform;
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.sizeDelta = Vector2.zero;
            vrt.offsetMin = new Vector2(0f, 28f);
            vrt.offsetMax = new Vector2(0f, -52f);

            // 标签
            var labelText = UiBuilder.CreateText("Label", cell.transform, label,
                18, FontStyle.Normal, TextAnchor.LowerCenter, UiBuilder.TextSecondary);
            var lrt = (RectTransform)labelText.transform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.sizeDelta = new Vector2(0f, 28f);
            lrt.anchoredPosition = new Vector2(0f, 8f);
        }

        /// <summary>显示本局来自营地强化与消耗券的额外奖励，让玩家直观看到养成的价值。</summary>
        private static void BuildBonusBanner(RectTransform panel, RunOutcomeRouter.Result r)
        {
            var hasBossSpeed = r.LastBossSpeedFishBonus > 0 || r.LastBossSpeedScoreBonus > 0;
            if (r.BonusCoins <= 0 && r.BonusScore <= 0
                && !r.ConsumedDoubleFishTicket && !r.ConsumedScoreBoostTicket
                && !hasBossSpeed) return;

            var sb = new System.Text.StringBuilder("\u2728  ");
            var parts = new System.Collections.Generic.List<string>();
            if (r.BonusCoins > 0) parts.Add($"鱼干 +{r.BonusCoins}");
            if (r.BonusScore > 0) parts.Add($"得分 +{r.BonusScore}");
            if (r.ConsumedDoubleFishTicket) parts.Add("双倍鱼干券");
            if (r.ConsumedScoreBoostTicket) parts.Add("得分加成券");
            if (hasBossSpeed) parts.Add($"Boss速杀 {r.LastBossSpeedTier} (+{r.LastBossSpeedFishBonus}鱼干/+{r.LastBossSpeedScoreBonus}分)");
            sb.Append(string.Join(" · ", parts));

            var banner = UiBuilder.CreateRect("BonusBanner", panel,
                new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f),
                new Vector2(0f, 56f), new Vector2(0f, -382f),
                new Color(1f, 0.78f, 0.32f, 0.18f), rounded: true);
            UiBuilder.AddOutline(banner.gameObject, new Color(1f, 0.78f, 0.32f, 0.55f),
                new Vector2(1f, -1f));

            var t = UiBuilder.CreateText("Text", banner, sb.ToString(),
                20, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.4f, 1f));
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            trt.offsetMin = new Vector2(16f, 0f);
            trt.offsetMax = new Vector2(-16f, 0f);
        }

        private static string BuildRankLabel(RunOutcomeRouter.Result r)
        {
            if (r.Daily && r.RankInChallengeBucket.HasValue)
            {
                return $"\u2605 今日榜第 {r.RankInChallengeBucket.Value}   \u2605 总榜第 {r.RankByScore}";
            }
            return $"\u2605 总榜第 {r.RankByScore}";
        }

        /// <summary>创建庆祝星星装饰（围绕在卡片周围）。</summary>
        private static void CreateCelebrationStars(RectTransform parent, int count)
        {
            var colors = new[]
            {
                new Color(1f, 0.85f, 0.35f, 0.9f),
                new Color(1f, 0.7f, 0.2f, 0.85f),
                new Color(1f, 0.95f, 0.6f, 0.8f),
            };

            for (var i = 0; i < count; i++)
            {
                var angle = (i / (float)count) * Mathf.PI * 2f;
                var radius = 500f + Random.Range(-60f, 60f);
                var pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.6f);
                var size = Random.Range(8f, 18f);
                var color = colors[Random.Range(0, colors.Length)];

                var star = UiBuilder.CreateRect($"Star_{i}", parent,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(size, size), pos,
                    color, glow: true);
                star.GetComponent<Image>().raycastTarget = false;

                var twinkle = star.gameObject.AddComponent<StarTwinkle>();
                twinkle.baseAlpha = color.a;
                twinkle.speed = Random.Range(2f, 4f);
                twinkle.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        /// <summary>庆祝脉冲动画：缩放的正弦波动。</summary>
        private class CelebrationPulse : MonoBehaviour
        {
            public float amplitude = 0.05f;
            public float speed = 3f;
            private Vector3 baseScale;
            private float startTime;

            private void Start()
            {
                baseScale = transform.localScale;
                startTime = Time.time;
            }

            private void Update()
            {
                var t = Time.time - startTime;
                var s = 1f + Mathf.Sin(t * speed) * amplitude;
                transform.localScale = baseScale * s;
            }
        }

        /// <summary>星星闪烁动画。</summary>
        private class StarTwinkle : MonoBehaviour
        {
            public float baseAlpha = 0.9f;
            public float speed = 2.5f;
            public float phaseOffset = 0f;
            private Image img;

            private void Start()
            {
                img = GetComponent<Image>();
            }

            private void Update()
            {
                if (img == null) return;
                var t = Time.time * speed + phaseOffset;
                var alpha = baseAlpha * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t)));
                var c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }
    }
}
