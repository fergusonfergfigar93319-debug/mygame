using PenguinRun.Game.Save;
using PenguinRun.Game.UI.Storybook;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>Shared storybook chapter header for every full-screen lobby panel.</summary>
    public static class PanelHeader
    {
        public static RectTransform Create(Transform parent, string title, string subtitle, System.Action onBack)
        {
            var bar = UiBuilder.CreateRect("Header", parent,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, UiBuilder.PanelHeaderHeightPixels), Vector2.zero,
                UiTheme.PaperWarm, rounded: true);
            bar.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddShadow(bar.gameObject, UiTheme.ShadowWarm, new Vector2(0f, -6f));

            var back = UiBuilder.CreateButton("Back", bar, string.Empty,
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.75f),
                UiTheme.InkPrimary, 34, rounded: true);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = new Vector2(0f, 0.5f);
            backRect.anchorMax = new Vector2(0f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.sizeDelta = new Vector2(82f, 82f);
            backRect.anchoredPosition = new Vector2(28f, -8f);
            back.onClick.AddListener(() => onBack?.Invoke());
            StorybookUi.AddStitchedOutline(back.gameObject);
            var backIcon = UiIcons.CreateImage("BackIcon", back.transform, UiIcons.Back, UiTheme.InkPrimary);
            var backIconRect = (RectTransform)backIcon.transform;
            backIconRect.anchorMin = new Vector2(0.26f, 0.26f);
            backIconRect.anchorMax = new Vector2(0.74f, 0.74f);
            backIconRect.offsetMin = Vector2.zero;
            backIconRect.offsetMax = Vector2.zero;

            var eyebrow = UiBuilder.CreateText("Eyebrow", bar, "营地手记 · 新的一页",
                UiTheme.Micro, FontStyle.Bold, TextAnchor.LowerLeft, UiTheme.CoralAction);
            Place(eyebrow.rectTransform, new Vector2(0f, 0.61f), new Vector2(1f, 0.92f),
                new Vector2(132f, 0f), new Vector2(-220f, 0f));

            var titleText = UiBuilder.CreateText("Title", bar, title, UiTheme.H1,
                FontStyle.Bold, TextAnchor.MiddleLeft, UiTheme.InkPrimary);
            Place(titleText.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 0.68f),
                new Vector2(132f, 0f), new Vector2(-220f, 0f));

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subText = UiBuilder.CreateText("Subtitle", bar, subtitle, UiTheme.Caption,
                    FontStyle.Normal, TextAnchor.UpperLeft, UiTheme.InkSecondary);
                Place(subText.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.31f),
                    new Vector2(132f, 0f), new Vector2(-220f, 0f));
            }

            var (currencyPill, _) = UiBuilder.CreateCurrencyPill("CurrencyPill", bar,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(186f, 62f), new Vector2(-110f, -8f),
                "鱼", UiTheme.CoralAction, $"{PlayerSave.TotalFishSnacks}");
            StorybookUi.AddStitchedOutline(currencyPill.gameObject);

            var divider = StorybookUi.CreateSectionDivider("HeaderStitch", bar);
            divider.anchorMin = new Vector2(0.03f, 0f);
            divider.anchorMax = new Vector2(0.97f, 0f);
            divider.anchoredPosition = new Vector2(0f, 2f);
            divider.GetComponent<Image>().raycastTarget = false;
            return bar;
        }

        public static void RefreshCurrency(RectTransform headerBar)
        {
            if (headerBar == null) return;
            var value = headerBar.Find("CurrencyPill/Value");
            var text = value != null ? value.GetComponent<TextMeshProUGUI>() : null;
            if (text != null) text.text = $"{PlayerSave.TotalFishSnacks}";
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
