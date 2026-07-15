using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Compact home status: identity and resources without covering the camp scene.</summary>
    public sealed class LobbyTopRibbon
    {
        public RectTransform Root { get; private set; }

        private TextMeshProUGUI profileLabel;
        private TextMeshProUGUI fishLabel;
        private TextMeshProUGUI rareLabel;
        private TextMeshProUGUI seasonLabel;

        public static LobbyTopRibbon Build(Transform parent)
        {
            var ribbon = new LobbyTopRibbon();
            ribbon.Root = UiBuilder.CreateRect("LobbyTopRibbon", parent,
                new Vector2(0.03f, 0.90f), new Vector2(0.97f, 0.975f),
                Vector2.zero, Vector2.zero, Color.clear);
            ribbon.Root.GetComponent<Image>().raycastTarget = false;

            ribbon.profileLabel = CreatePill(ribbon.Root, "Profile", new Vector2(0f, 0f),
                new Vector2(0.25f, 1f), UiTheme.PaperWarm, UiTheme.InkPrimary, TextAnchor.MiddleLeft);
            ribbon.fishLabel = CreatePill(ribbon.Root, "Fish", new Vector2(0.265f, 0f),
                new Vector2(0.42f, 1f), WithAlpha(UiTheme.InkPrimary, 0.78f), Color.white);
            ribbon.rareLabel = CreatePill(ribbon.Root, "RareResource", new Vector2(0.435f, 0f),
                new Vector2(0.57f, 1f), WithAlpha(UiTheme.InkPrimary, 0.78f), Color.white);
            ribbon.seasonLabel = CreatePill(ribbon.Root, "Season", new Vector2(0.73f, 0f),
                new Vector2(1f, 1f), UiTheme.CoralAction, Color.white, TextAnchor.MiddleRight);
            return ribbon;
        }

        public void Bind(LobbyViewModel model)
        {
            profileLabel.text = $"咕咕嘎嘎 · Lv.{model.PlayerLevel}";
            fishLabel.text = $"鱼干 {model.Fish:N0}";
            rareLabel.text = $"星页 {model.RareResource:N0}";
            seasonLabel.text = model.SeasonLabel;
        }

        private static TextMeshProUGUI CreatePill(Transform parent, string name, Vector2 min,
            Vector2 max, Color background, Color foreground, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var pill = UiBuilder.CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero,
                background, rounded: true);
            UiBuilder.AddOutline(pill.gameObject, WithAlpha(UiTheme.StitchBorder, 0.55f), new Vector2(1f, -1f));
            UiBuilder.AddShadow(pill.gameObject, UiTheme.ShadowWarm, new Vector2(0f, -3f));
            var text = UiBuilder.CreateTmpText("Label", pill, string.Empty, UiTheme.Caption,
                FontStyle.Bold, alignment, foreground);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(16f, 2f);
            text.rectTransform.offsetMax = new Vector2(-16f, -2f);
            return text;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }
}
