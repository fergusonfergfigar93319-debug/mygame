using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>In-run uGUI result card. It keeps retry in the runner scene and only returns to the lobby on request.</summary>
    public static class RunnerDeathOverlay
    {
        public static GameObject Show(int score, int distanceMeters, int coins, Action onRetry, Action onReturnToLobby)
        {
            var model = JourneyResultViewModel.FromDeath(score, distanceMeters, coins);
            var canvas = UiBuilder.CreateCanvas("RunnerDeathOverlayCanvas", null, 240);
            var root = UiBuilder.CreateRect("RunnerDeathOverlay", canvas.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.01f, 0.025f, 0.07f, 0.82f));

            var card = UiBuilder.CreateCard("ResultCard", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(760f, 700f), Vector2.zero, UiTheme.PaperWarm, addBorder: true, addShadow: true);
            UiBuilder.AddOutline(card.gameObject, UiTheme.StitchBorder, new Vector2(2f, -2f));
            UiBuilder.AttachArtVignette("ResultCelebrationArt", card, Image2ArtCatalog.ResultCelebration, 0.16f);
            card.PlayPanelPopIn();

            // These labels are center-anchored, so a zero X size collapses them to a
            // one-character-wide column. Keep an explicit width at every resolution.
            AddText("Title", card, model.Headline, UiTheme.H1, FontStyle.Bold, new Vector2(0f, 224f), new Vector2(620f, 70f), UiTheme.InkPrimary);
            var scoreText = AddText("Score", card, "0", UiTheme.Display, FontStyle.Bold, new Vector2(0f, 126f), new Vector2(620f, 90f), UiTheme.CoralAction);
            // 分数滚动呈现：只在整数变化帧写 text。
            TweenExtensions.PlayCountUp(model.Score, 0.7f, v => scoreText.text = v.ToString("N0"));
            AddText("ScoreLabel", card, model.BestComparison, UiTheme.Caption, FontStyle.Normal, new Vector2(0f, 72f), new Vector2(620f, 34f), UiTheme.InkSecondary);

            AddStat(card, "距离", $"{model.DistanceMeters}m", new Vector2(-188f, -32f), UiTheme.MistBlue);
            AddStat(card, "鱼干", model.Fish.ToString(), new Vector2(0f, -32f), UiTheme.CoralAction);
            AddStat(card, "下一步", model.ReviewHint, new Vector2(188f, -32f), UiTheme.PineGreen, smallerValue: true);
            AddText("ProgressionPreview", card, model.ProgressionPreview, UiTheme.Micro, FontStyle.Normal,
                new Vector2(0f, -126f), new Vector2(620f, 42f), UiTheme.InkSecondary);

            var retry = UiBuilder.CreatePrimaryButton("Retry", card, "再来一局", UiTheme.H2, null);
            PlaceButton(retry, new Vector2(0f, -208f), new Vector2(560f, 84f));
            retry.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(canvas);
                onRetry?.Invoke();
            });

            var lobby = UiBuilder.CreateSecondaryButton("ReturnToLobby", card, "回到营地", UiTheme.Body, null);
            PlaceButton(lobby, new Vector2(0f, -310f), new Vector2(560f, 66f));
            lobby.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(canvas);
                onReturnToLobby?.Invoke();
            });

            return canvas;
        }

        public static string GetDeathAnalysisHint(int distanceMeters)
        {
            if (distanceMeters < 180) return "试着更早观察前方三条车道";
            if (distanceMeters < 600) return "连续闪避能让节奏更稳定";
            return "你已进入高压区，留意 Boss 预警";
        }

        private static void AddStat(RectTransform parent, string label, string value, Vector2 position, Color accent, bool smallerValue = false)
        {
            var cell = UiBuilder.CreateRect("Stat_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(168f, 124f), position, UiTheme.PaperBase, rounded: true);
            UiBuilder.AddOutline(cell.gameObject, new Color(accent.r, accent.g, accent.b, 0.4f), new Vector2(1f, -1f));
            AddText("Value", cell, value, smallerValue ? UiTheme.Micro : UiTheme.H2, FontStyle.Bold,
                new Vector2(0f, 18f), new Vector2(142f, smallerValue ? 54f : 48f), accent);
            AddText("Label", cell, label, UiTheme.Micro, FontStyle.Normal, new Vector2(0f, -39f), new Vector2(142f, 28f), UiTheme.InkSecondary);
        }

        private static TextMeshProUGUI AddText(string name, RectTransform parent, string content, int size, FontStyle style, Vector2 position, Vector2 dimensions, Color color)
        {
            var text = UiBuilder.CreateTmpText(name, parent, content, size, style, TextAnchor.MiddleCenter, color);
            var rt = (RectTransform)text.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dimensions;
            rt.anchoredPosition = position;
            return text;
        }

        private static void PlaceButton(Button button, Vector2 position, Vector2 dimensions)
        {
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dimensions;
            rt.anchoredPosition = position;
        }
    }
}
