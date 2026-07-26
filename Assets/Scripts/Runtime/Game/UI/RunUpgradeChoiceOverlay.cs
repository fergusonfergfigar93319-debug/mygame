using System;
using PenguinRun.Game.UI.Storybook;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    internal sealed class RunUpgradeChoiceOverlay : MonoBehaviour
    {
        private const int CardCount = 3;
        private const float AutoSelectSeconds = 12f;

        private Action<RunUpgradeId> onSelected;
        private RunUpgradeOffer offer;
        private TextMeshProUGUI countdownText;
        private float remainingSeconds;
        private int shownSeconds = -1;
        private bool resolved;

        public static RunUpgradeChoiceOverlay Show(
            RunUpgradeOffer offer,
            Func<RunUpgradeId, int> getStack,
            Action<RunUpgradeId> onSelected)
        {
            if (offer.Count != CardCount) return null;

            var canvas = UiBuilder.CreateCanvas("RunUpgradeChoiceCanvas", null, 260);
            var overlay = canvas.AddComponent<RunUpgradeChoiceOverlay>();
            overlay.offer = offer;
            overlay.onSelected = onSelected;
            overlay.remainingSeconds = AutoSelectSeconds;
            overlay.Build(canvas.transform, getStack);
            return overlay;
        }

        private void Update()
        {
            if (resolved) return;

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.unscaledDeltaTime);
            // 倒计时按整秒显示：只在秒数真正变化时改写文本，避免每帧构造字符串并把 Graphic 标脏。
            var wholeSeconds = Mathf.CeilToInt(remainingSeconds);
            if (countdownText != null && wholeSeconds != shownSeconds)
            {
                shownSeconds = wholeSeconds;
                countdownText.text = $"{wholeSeconds} 秒后自动选择";
            }

            if (remainingSeconds <= 0f)
            {
                Resolve(offer.First.Id);
            }
        }

        private void Build(Transform canvas, Func<RunUpgradeId, int> getStack)
        {
            var shade = UiBuilder.CreateRect("UpgradeShade", canvas, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.015f, 0.035f, 0.08f, 0.86f));
            var book = UiBuilder.CreateCard("UpgradeBook", shade,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1120f, 670f), Vector2.zero, UiTheme.PaperWarm,
                addBorder: true, addShadow: true);
            StorybookUi.AddStitchedOutline(book.gameObject);

            AddText("Title", book, "旅途中学会了什么？", UiTheme.H1, FontStyle.Bold,
                new Vector2(0f, 270f), new Vector2(900f, 64f), UiTheme.InkPrimary);
            AddText("Hint", book, "选择一张旅途札记，本局立即生效", UiTheme.Body, FontStyle.Normal,
                new Vector2(0f, 224f), new Vector2(900f, 38f), UiTheme.InkSecondary);

            for (var i = 0; i < CardCount; i++)
            {
                BuildCard(book, offer[i], getStack?.Invoke(offer[i].Id) ?? 0, i);
            }

            countdownText = UiBuilder.CreateTmpText("AutoSelectCountdown", book,
                "12 秒后自动选择", UiTheme.Caption, FontStyle.Normal,
                TextAnchor.MiddleCenter, UiTheme.InkSecondary);
            countdownText.rectTransform.anchorMin = countdownText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            countdownText.rectTransform.sizeDelta = new Vector2(600f, 34f);
            countdownText.rectTransform.anchoredPosition = new Vector2(0f, -286f);
            StorybookUi.PlayPageTurnIn(book);
        }

        private void BuildCard(RectTransform parent, RunUpgradeDefinition definition, int currentStack, int index)
        {
            var x = (index - 1) * 340f;
            var accent = CategoryColor(definition.Category);
            var card = UiBuilder.CreateRect($"UpgradeCard{index + 1}", parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(306f, 404f), new Vector2(x, -20f), UiTheme.PaperBase, rounded: true);
            UiBuilder.AddOutline(card.gameObject, new Color(accent.r, accent.g, accent.b, 0.72f), new Vector2(2f, -2f));
            UiBuilder.AddShadow(card.gameObject, UiTheme.ShadowWarm, new Vector2(0f, -5f));

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            var selectedId = definition.Id;
            button.onClick.AddListener(() => Resolve(selectedId));

            AddText("Category", card, CategoryLabel(definition.Category), UiTheme.Caption, FontStyle.Bold,
                new Vector2(0f, 154f), new Vector2(250f, 34f), accent);
            AddText("Title", card, definition.Title, UiTheme.H2, FontStyle.Bold,
                new Vector2(0f, 96f), new Vector2(264f, 62f), UiTheme.InkPrimary);
            AddText("Description", card, definition.Description, UiTheme.Body, FontStyle.Normal,
                new Vector2(0f, 12f), new Vector2(250f, 100f), UiTheme.InkSecondary);
            AddText("Stack", card, $"当前 {currentStack}/{definition.MaxStacks} 层",
                UiTheme.Caption, FontStyle.Bold, new Vector2(0f, -100f), new Vector2(240f, 34f), accent);
            AddText("Choose", card, "选择", UiTheme.Body, FontStyle.Bold,
                new Vector2(0f, -158f), new Vector2(220f, 44f), UiTheme.CoralAction);
        }

        private void Resolve(RunUpgradeId id)
        {
            if (resolved) return;
            resolved = true;
            var callback = onSelected;
            onSelected = null;
            callback?.Invoke(id);
            Destroy(gameObject);
        }

        private static string CategoryLabel(RunUpgradeCategory category) => category switch
        {
            RunUpgradeCategory.Movement => "行动",
            RunUpgradeCategory.Collection => "收集",
            RunUpgradeCategory.Defense => "守护",
            RunUpgradeCategory.Companion => "伙伴",
            _ => "旅途",
        };

        private static Color CategoryColor(RunUpgradeCategory category) => category switch
        {
            RunUpgradeCategory.Movement => UiTheme.MistBlue,
            RunUpgradeCategory.Collection => UiTheme.CoralAction,
            RunUpgradeCategory.Defense => UiTheme.PineGreen,
            RunUpgradeCategory.Companion => new Color(0.72f, 0.42f, 0.68f, 1f),
            _ => UiTheme.InkSecondary,
        };

        private static void AddText(
            string name,
            RectTransform parent,
            string content,
            int size,
            FontStyle style,
            Vector2 position,
            Vector2 dimensions,
            Color color)
        {
            var text = UiBuilder.CreateTmpText(name, parent, content, size, style,
                TextAnchor.MiddleCenter, color);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.sizeDelta = dimensions;
            text.rectTransform.anchoredPosition = position;
        }
    }
}
