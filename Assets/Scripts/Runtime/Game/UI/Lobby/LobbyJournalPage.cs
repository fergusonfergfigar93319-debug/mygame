using System;
using PenguinRun.Game.UI.Storybook;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Left story page: one prioritized recommendation, progress, and reward.</summary>
    public sealed class LobbyJournalPage
    {
        public RectTransform Root { get; private set; }

        private TextMeshProUGUI title;
        private TextMeshProUGUI body;
        private TextMeshProUGUI reward;
        private TextMeshProUGUI actionLabel;
        private Image progressFill;
        private Button action;

        public static LobbyJournalPage Build(Transform parent)
        {
            var page = new LobbyJournalPage();
            page.Root = StorybookUi.CreatePaperCard("LobbyJournalPage", parent,
                WithAlpha(UiTheme.PaperWarm, 0.96f));
            Place(page.Root, new Vector2(0.03f, 0.20f), new Vector2(0.27f, 0.82f));

            var eyebrow = StorybookUi.CreateStoryLabel("JournalEyebrow", page.Root, "今日手记", UiTheme.Caption);
            eyebrow.color = UiTheme.CoralAction;
            Place(eyebrow.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f));

            page.title = StorybookUi.CreateStoryLabel("JournalTitle", page.Root, string.Empty, UiTheme.H2);
            page.title.enableAutoSizing = true;
            page.title.fontSizeMin = UiTheme.Body;
            page.title.fontSizeMax = UiTheme.H2;
            Place(page.title.rectTransform, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.84f));

            page.body = UiBuilder.CreateTmpText("JournalBody", page.Root, string.Empty, UiTheme.Caption,
                FontStyle.Normal, TextAnchor.UpperLeft, UiTheme.InkSecondary);
            page.body.maxVisibleLines = 3;
            page.body.overflowMode = TextOverflowModes.Ellipsis;
            Place(page.body.rectTransform, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.64f));

            var track = UiBuilder.CreateRect("JournalProgress", page.Root,
                new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.40f), Vector2.zero, Vector2.zero,
                WithAlpha(UiTheme.MistBlue, 0.52f), rounded: true);
            page.progressFill = UiBuilder.CreateRect("Fill", track, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, UiTheme.PineGreen, rounded: true).GetComponent<Image>();
            page.progressFill.type = Image.Type.Filled;
            page.progressFill.fillMethod = Image.FillMethod.Horizontal;

            var rewardCard = UiBuilder.CreateRect("JournalReward", page.Root,
                new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero,
                WithAlpha(UiTheme.MistBlue, 0.34f), rounded: true);
            page.reward = UiBuilder.CreateTmpText("RewardLabel", rewardCard, string.Empty, UiTheme.Micro,
                FontStyle.Bold, TextAnchor.MiddleCenter, UiTheme.InkPrimary);
            Stretch(page.reward.rectTransform, 12f);

            // 手记主操作是本页主 CTA：珊瑚色 + 白字（PineGreen 留给选中/激活态）
            page.action = UiBuilder.CreateButton("JournalAction", page.Root, string.Empty,
                UiTheme.CoralAction, UiBuilder.TextOnAccent, UiTheme.Caption, rounded: true);
            Place((RectTransform)page.action.transform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.18f));
            var legacy = page.action.GetComponentInChildren<TextMeshProUGUI>();
            if (legacy != null) StorybookUi.DestroyGeneratedObject(legacy.gameObject);
            page.actionLabel = UiBuilder.CreateTmpText("Label", page.action.transform, string.Empty,
                UiTheme.Caption, FontStyle.Bold, TextAnchor.MiddleCenter, UiBuilder.TextOnAccent);
            Stretch(page.actionLabel.rectTransform, 4f);
            return page;
        }

        public void Bind(LobbyViewModel model, Action onOpenRecommendation)
        {
            title.text = model.CurrentStoryTitle;
            body.text = model.CurrentStoryBody;
            reward.text = model.JournalRewardLabel;
            progressFill.fillAmount = model.StoryProgress01;
            actionLabel.text = model.PrimaryActionLabel;
            action.onClick.RemoveAllListeners();
            if (onOpenRecommendation != null)
                action.onClick.AddListener(() => onOpenRecommendation());
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }
}
