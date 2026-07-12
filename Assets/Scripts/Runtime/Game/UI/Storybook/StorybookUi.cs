using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Storybook
{
    /// <summary>Reusable storybook presentation primitives with no gameplay dependencies.</summary>
    public static class StorybookUi
    {
        public static RectTransform CreatePaperCard(string name, Transform parent, Color? color = null)
        {
            var card = UiBuilder.CreateRect(name, parent, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, color ?? UiTheme.PaperWarm, rounded: true);
            UiBuilder.AddShadow(card.gameObject, UiTheme.ShadowWarm, new Vector2(7f, -8f));
            AddStitchedOutline(card.gameObject);
            return card;
        }

        public static TextMeshProUGUI CreateStoryLabel(string name, Transform parent, string text, int size)
        {
            return UiBuilder.CreateTmpText(name, parent, text, size, FontStyle.Bold,
                TextAnchor.MiddleLeft, UiTheme.InkPrimary);
        }

        public static Button CreatePrimaryAction(string name, Transform parent, string text)
        {
            var button = UiBuilder.CreateButton(name, parent, text, UiTheme.CoralAction,
                Color.white, UiTheme.Body, rounded: true);
            UiBuilder.AddOutline(button.gameObject, UiTheme.StitchBorder, new Vector2(2f, -2f));
            UiBuilder.AddShadow(button.gameObject, UiTheme.ShadowWarm, new Vector2(0f, -5f));
            return button;
        }

        public static RectTransform CreateSectionDivider(string name, Transform parent)
        {
            return UiBuilder.CreateRect(name, parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 2f), Vector2.zero, UiTheme.StitchBorder);
        }

        public static void AddStitchedOutline(GameObject target)
        {
            UiBuilder.AddOutline(target, UiTheme.StitchBorder, new Vector2(2f, -2f));
        }

        public static void PlayPageTurnIn(RectTransform target)
        {
            if (target == null) return;
            target.PlayPanelPopIn(UiTheme.StorybookPageTurnDuration);
        }
    }
}
