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
        private readonly TextMeshProUGUI hint;
        private readonly Button interaction;

        private LobbyCompanionCorner(TextMeshProUGUI hint, Button interaction)
        {
            this.hint = hint;
            this.interaction = interaction;
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
            return new LobbyCompanionCorner(hint, button);
        }

        public void Bind(LobbyViewModel model, Action onInteract)
        {
            hint.text = model.CompanionHint;
            interaction.onClick.RemoveAllListeners();
            if (onInteract != null) interaction.onClick.AddListener(() => onInteract());
        }
    }
}
