using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 通用面板顶部条：「< 返回 + 标题 + 副标题 + 右侧鱼干计数」。
    /// 重构后使用 UiBuilder 设计系统，提供更精致的视觉与一致的间距。
    /// </summary>
    public static class PanelHeader
    {
        public static RectTransform Create(Transform parent, string title, string subtitle, System.Action onBack)
        {
            // 顶部主条：渐变深色，带圆角与底部渐变发光线
            var bar = UiBuilder.CreateRect(
                "Header",
                parent,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                sizeDelta: new Vector2(0f, UiBuilder.PanelHeaderHeightPixels),
                anchoredPosition: new Vector2(0f, 0f),
                bg: new Color(0.06f, 0.13f, 0.24f, 1f));
            bar.pivot = new Vector2(0.5f, 1f);

            // 顶部高光：从顶部向下淡化的薄层，增加立体感
            var topGlow = UiBuilder.CreateRect("TopGlow", bar,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 6f), new Vector2(0f, -3f),
                new Color(0.4f, 0.85f, 1f, 0.18f), rounded: true);
            topGlow.GetComponent<Image>().raycastTarget = false;

            // 底部分隔线：青色发光，与下方内容区域分离
            var bottomLine = UiBuilder.CreateRect("HeaderLine", bar,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 2f), new Vector2(0f, 1f),
                new Color(0.0f, 0.85f, 0.95f, 0.45f), rounded: true);
            bottomLine.GetComponent<Image>().raycastTarget = false;

            // 底部辉光（更宽更弱）
            var bottomGlow = UiBuilder.CreateRect("HeaderLineGlow", bar,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 12f), new Vector2(0f, 8f),
                new Color(0.0f, 0.85f, 0.95f, 0.08f), rounded: true);
            bottomGlow.GetComponent<Image>().raycastTarget = false;

            // 返回按钮：圆形带箭头，hover 高亮
            var backBtn = UiBuilder.CreateButton("Back", bar, "\u2190",
                new Color(0.1f, 0.22f, 0.38f, 1f), Color.white, 36, rounded: true);
            var brt = (RectTransform)backBtn.transform;
            brt.anchorMin = new Vector2(0f, 0.5f);
            brt.anchorMax = new Vector2(0f, 0.5f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.sizeDelta = new Vector2(82f, 82f);
            brt.anchoredPosition = new Vector2(28f, -10f);

            var btnColors = backBtn.colors;
            btnColors.normalColor = new Color(0.1f, 0.22f, 0.38f, 1f);
            btnColors.highlightedColor = new Color(0.18f, 0.38f, 0.58f, 1f);
            btnColors.pressedColor = new Color(0.06f, 0.16f, 0.28f, 1f);
            btnColors.selectedColor = new Color(0.16f, 0.34f, 0.52f, 1f);
            backBtn.colors = btnColors;

            backBtn.onClick.AddListener(() => {
                Debug.Log($"[PanelHeader] 返回按钮被点击 - {title}");
                onBack?.Invoke();
            });

            UiBuilder.AddOutline(backBtn.gameObject, UiBuilder.BorderAccent, new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(backBtn.gameObject, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -3f));

            // 标题：左侧重音条 + 大标题
            var titleAccent = UiBuilder.CreateRect("TitleAccent", bar,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 56f), new Vector2(124f, 14f),
                UiBuilder.AccentCyan, rounded: true);
            titleAccent.GetComponent<Image>().raycastTarget = false;

            var titleText = UiBuilder.CreateText("Title", bar, title, 42, FontStyle.Bold,
                TextAnchor.LowerLeft, Color.white);
            var trt = (RectTransform)titleText.transform;
            trt.anchorMin = new Vector2(0f, 0.52f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 0.3f);
            trt.offsetMin = new Vector2(140f, 4f);
            trt.offsetMax = new Vector2(-220f, -10f);
            UiBuilder.AddShadow(titleText.gameObject, new Color(0f, 0.05f, 0.15f, 0.55f), new Vector2(0f, -2f));

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subText = UiBuilder.CreateText("Subtitle", bar, subtitle, 22, FontStyle.Normal,
                    TextAnchor.UpperLeft, UiBuilder.TextSecondary);
                var srt = (RectTransform)subText.transform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(1f, 0.5f);
                srt.pivot = new Vector2(0f, 0.85f);
                srt.offsetMin = new Vector2(140f, 12f);
                srt.offsetMax = new Vector2(-220f, -4f);
            }

            // 右侧货币胶囊：使用统一组件
            var (currencyPill, _) = UiBuilder.CreateCurrencyPill(
                "CurrencyPill", bar,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(186f, 62f), new Vector2(-110f, -10f),
                "鱼", UiBuilder.WarmGold,
                $"{Save.PlayerSave.TotalFishSnacks}");

            // 让胶囊有微弱发光以强调
            UiBuilder.AddShadow(currencyPill.gameObject, new Color(1f, 0.6f, 0.2f, 0.18f), new Vector2(0f, -2f));

            return bar;
        }
    }
}
