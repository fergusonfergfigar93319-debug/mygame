using System;
using PenguinRun.Game.UI.Storybook;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>The lobby's single answer to “what should I do next?”.</summary>
    public sealed class LobbyStoryCard
    {
        private readonly TextMeshProUGUI title;
        private readonly TextMeshProUGUI body;
        private readonly TextMeshProUGUI actionLabel;
        private readonly TextMeshProUGUI fishLabel;
        private readonly Image progressFill;
        private readonly Button primaryAction;

        private LobbyStoryCard(TextMeshProUGUI title, TextMeshProUGUI body,
            TextMeshProUGUI actionLabel, TextMeshProUGUI fishLabel, Image progressFill,
            Button primaryAction)
        {
            this.title = title;
            this.body = body;
            this.actionLabel = actionLabel;
            this.fishLabel = fishLabel;
            this.progressFill = progressFill;
            this.primaryAction = primaryAction;
        }

        public static LobbyStoryCard Build(Transform parent)
        {
            var card = StorybookUi.CreatePaperCard("LobbyStoryCard", parent);
            card.anchorMin = new Vector2(0.055f, 0.22f);
            card.anchorMax = new Vector2(0.53f, 0.78f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var eyebrow = StorybookUi.CreateStoryLabel("ChapterLabel", card, "今日故事 · 极夜营地", UiTheme.Caption);
            Place(eyebrow.rectTransform, new Vector2(0.07f, 0.80f), new Vector2(0.93f, 0.94f));
            eyebrow.color = UiTheme.CoralAction;

            var title = StorybookUi.CreateStoryLabel("StoryTitle", card, string.Empty, UiTheme.H1);
            Place(title.rectTransform, new Vector2(0.07f, 0.57f), new Vector2(0.93f, 0.82f));

            var body = UiBuilder.CreateTmpText("StoryBody", card, string.Empty, UiTheme.Body,
                FontStyle.Normal, TextAnchor.UpperLeft, UiTheme.InkSecondary);
            body.textWrappingMode = TextWrappingModes.Normal;
            Place(body.rectTransform, new Vector2(0.07f, 0.35f), new Vector2(0.93f, 0.59f));

            var progressTrack = UiBuilder.CreateRect("StoryProgress", card, new Vector2(0.07f, 0.28f),
                new Vector2(0.72f, 0.32f), Vector2.zero, Vector2.zero,
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.45f), rounded: true);
            var fill = UiBuilder.CreateRect("Fill", progressTrack, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, UiTheme.PineGreen, rounded: true).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;

            var fish = UiBuilder.CreateTmpText("Fish", card, string.Empty, UiTheme.Caption,
                FontStyle.Bold, TextAnchor.MiddleRight, UiTheme.InkSecondary);
            Place(fish.rectTransform, new Vector2(0.73f, 0.25f), new Vector2(0.93f, 0.35f));

            var button = StorybookUi.CreatePrimaryAction("PrimaryStoryAction", card, "出发");
            var buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = new Vector2(0.07f, 0.06f);
            buttonRect.anchorMax = new Vector2(0.93f, 0.22f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            var actionLabel = button.GetComponentInChildren<TextMeshProUGUI>();

            StorybookUi.PlayPageTurnIn(card);
            return new LobbyStoryCard(title, body, actionLabel, fish, fill, button);
        }

        public void Bind(LobbyViewModel model, Action onPrimaryAction)
        {
            title.text = model.CurrentStoryTitle;
            body.text = model.CurrentStoryBody;
            actionLabel.text = model.PrimaryActionLabel;
            fishLabel.text = $"鱼干 {model.Fish:N0}";
            progressFill.fillAmount = model.StoryProgress01;
            primaryAction.onClick.RemoveAllListeners();
            if (onPrimaryAction != null) primaryAction.onClick.AddListener(() => onPrimaryAction());
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
