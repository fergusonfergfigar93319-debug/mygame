using System;
using PenguinRun.Game.UI.Storybook;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Mascot portrait and one lightweight relationship interaction.</summary>
    public sealed class LobbyCompanionCorner
    {
        public RectTransform Root { get; }
        private readonly TextMeshProUGUI hint;
        private readonly Button interaction;
        private readonly TextMeshProUGUI bondText;
        private readonly Image bondFill;

        private LobbyCompanionCorner(RectTransform root, TextMeshProUGUI hint, Button interaction, TextMeshProUGUI bondText, Image bondFill)
        {
            Root = root;
            this.hint = hint;
            this.interaction = interaction;
            this.bondText = bondText;
            this.bondFill = bondFill;
        }

        public static LobbyCompanionCorner Build(Transform parent)
        {
            var root = StorybookUi.CreatePaperCard("LobbyCompanionCorner", parent,
                new Color(UiTheme.PaperWarm.r, UiTheme.PaperWarm.g, UiTheme.PaperWarm.b, 0.90f));
            root.anchorMin = new Vector2(0.58f, 0.18f);
            root.anchorMax = new Vector2(0.93f, 0.84f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var portraitRect = UiBuilder.CreateRect("MascotPortrait", root, new Vector2(0.08f, 0.23f),
                new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            var portrait = portraitRect.GetComponent<Image>();
            portrait.sprite = StorybookArtCatalog.Mascot;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portraitRect.PlayFloat(8f, 3.2f);

            var hintCard = UiBuilder.CreateRect("CompanionHint", root, new Vector2(0.07f, 0.05f),
                new Vector2(0.93f, 0.25f), Vector2.zero, Vector2.zero,
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.60f), rounded: true);
            var hint = UiBuilder.CreateTmpText("Hint", hintCard, string.Empty, UiTheme.Caption,
                FontStyle.Bold, TextAnchor.MiddleCenter, UiTheme.InkPrimary);
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.one;
            hint.rectTransform.offsetMin = new Vector2(16f, 8f);
            hint.rectTransform.offsetMax = new Vector2(-16f, -8f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();

            var bondCard = UiBuilder.CreateRect("BondProgress", root, new Vector2(0.08f, 0.82f),
                new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero,
                new Color(UiTheme.PaperBase.r, UiTheme.PaperBase.g, UiTheme.PaperBase.b, 0.88f), rounded: true);
            var bondText = UiBuilder.CreateTmpText("BondLevel", bondCard, string.Empty, UiTheme.Micro,
                FontStyle.Bold, TextAnchor.UpperCenter, UiTheme.InkPrimary);
            bondText.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            bondText.rectTransform.anchorMax = Vector2.one;
            bondText.rectTransform.offsetMin = new Vector2(8f, 0f);
            bondText.rectTransform.offsetMax = new Vector2(-8f, -2f);
            var track = UiBuilder.CreateRect("Track", bondCard, new Vector2(0.08f, 0.16f),
                new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero, UiTheme.MistBlue, rounded: true);
            var fill = UiBuilder.CreateRect("Fill", track, Vector2.zero, new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero, UiTheme.CoralAction, rounded: true).GetComponent<Image>();
            track.GetComponent<Image>().raycastTarget = false;
            fill.raycastTarget = false;
            return new LobbyCompanionCorner(root, hint, button, bondText, fill);
        }

        public void Bind(LobbyViewModel model, Action onInteract)
        {
            hint.text = model.CompanionHint;
            bondText.text = model.BondLevel > 0
                ? $"羁绊 Lv {model.BondLevel} · {model.BondHelperLabel}"
                : "完成旅程，与伙伴建立羁绊";
            var fillRect = (RectTransform)bondFill.transform;
            fillRect.anchorMax = new Vector2(model.BondProgress, 1f);
            interaction.onClick.RemoveAllListeners();
            if (onInteract != null) interaction.onClick.AddListener(() => onInteract());
        }
    }
}
