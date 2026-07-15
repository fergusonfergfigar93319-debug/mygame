using System;
using PenguinRun.Game.UI.Storybook;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Right story page: the next route and the home's only primary journey action.</summary>
    public sealed class LobbyChapterPage
    {
        public RectTransform Root { get; private set; }

        private TextMeshProUGUI title;
        private TextMeshProUGUI body;
        private TextMeshProUGUI difficulty;
        private TextMeshProUGUI reward;
        private TextMeshProUGUI actionLabel;
        private Button primaryAction;

        public static LobbyChapterPage Build(Transform parent)
        {
            var page = new LobbyChapterPage();
            page.Root = StorybookUi.CreatePaperCard("LobbyChapterPage", parent,
                WithAlpha(UiTheme.PaperWarm, 0.96f));
            Place(page.Root, new Vector2(0.73f, 0.20f), new Vector2(0.97f, 0.82f));

            var eyebrow = StorybookUi.CreateStoryLabel("ChapterEyebrow", page.Root, "下一章", UiTheme.Caption);
            eyebrow.color = UiTheme.PineGreen;
            Place(eyebrow.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f));

            page.title = StorybookUi.CreateStoryLabel("ChapterTitle", page.Root, string.Empty, UiTheme.H2);
            page.title.enableAutoSizing = true;
            page.title.fontSizeMin = UiTheme.Body;
            page.title.fontSizeMax = UiTheme.H2;
            Place(page.title.rectTransform, new Vector2(0.08f, 0.65f), new Vector2(0.92f, 0.84f));

            page.body = UiBuilder.CreateTmpText("ChapterBody", page.Root, string.Empty, UiTheme.Caption,
                FontStyle.Normal, TextAnchor.UpperLeft, UiTheme.InkSecondary);
            page.body.maxVisibleLines = 3;
            page.body.overflowMode = TextOverflowModes.Ellipsis;
            Place(page.body.rectTransform, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.65f));

            var route = UiBuilder.CreateRect("ChapterRoute", page.Root,
                new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.43f), Vector2.zero, Vector2.zero,
                Color.clear);
            route.GetComponent<Image>().raycastTarget = false;
            for (var i = 0; i < 7; i++)
            {
                var x = i / 6f;
                var dot = UiBuilder.CreateRect($"RouteDot_{i}", route,
                    new Vector2(x, 0.5f), new Vector2(x, 0.5f), new Vector2(i is 0 or 6 ? 12f : 6f, i is 0 or 6 ? 12f : 6f),
                    Vector2.zero, i == 6 ? UiTheme.CoralAction : UiTheme.PineGreen, circle: true);
                dot.GetComponent<Image>().raycastTarget = false;
            }

            page.difficulty = UiBuilder.CreateTmpText("DifficultyLabel", page.Root, string.Empty,
                UiTheme.Micro, FontStyle.Bold, TextAnchor.MiddleLeft, UiTheme.BerryDanger);
            Place(page.difficulty.rectTransform, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.36f));

            page.reward = UiBuilder.CreateTmpText("RouteRewardLabel", page.Root, string.Empty,
                UiTheme.Micro, FontStyle.Bold, TextAnchor.MiddleLeft, UiTheme.InkSecondary);
            Place(page.reward.rectTransform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.28f));

            page.primaryAction = StorybookUi.CreatePrimaryAction("PrimaryChapterAction", page.Root, "翻开旅程");
            Place((RectTransform)page.primaryAction.transform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.17f));
            page.actionLabel = page.primaryAction.GetComponentInChildren<TextMeshProUGUI>();
            return page;
        }

        public void Bind(LobbyViewModel model, Action onStartJourney)
        {
            title.text = model.NextChapterTitle;
            body.text = model.NextChapterBody;
            difficulty.text = model.DifficultyLabel;
            reward.text = model.RouteRewardLabel;
            actionLabel.text = "翻开旅程";
            primaryAction.onClick.RemoveAllListeners();
            if (onStartJourney != null)
                primaryAction.onClick.AddListener(() => onStartJourney());
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }
}
