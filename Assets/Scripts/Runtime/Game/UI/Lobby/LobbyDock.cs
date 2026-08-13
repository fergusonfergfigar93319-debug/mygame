using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    public readonly struct LobbyDockActions
    {
        public readonly Action Camp;
        public readonly Action Missions;
        public readonly Action Run;
        public readonly Action Codex;
        public readonly Action Settings;

        public LobbyDockActions(Action camp, Action missions, Action run, Action codex, Action settings)
        {
            Camp = camp;
            Missions = missions;
            Run = run;
            Codex = codex;
            Settings = settings;
        }
    }

    /// <summary>Stable five-destination dock. It emits callbacks and owns no navigation rules.</summary>
    public sealed class LobbyDock
    {
        private sealed class Item
        {
            public string Name;
            public Image Background;
            public TextMeshProUGUI Label;
            public GameObject Badge;
            public TextMeshProUGUI BadgeText;
        }

        private readonly List<Item> items = new();
        public RectTransform Root { get; private set; }

        public static LobbyDock Build(Transform parent, LobbyDockActions actions)
        {
            var dock = new LobbyDock();
            var root = UiBuilder.CreateRect("StorybookLobbyDock", parent, new Vector2(0.27f, 0.025f),
                new Vector2(0.73f, 0.145f), Vector2.zero, Vector2.zero,
                new Color(UiTheme.PaperWarm.r, UiTheme.PaperWarm.g, UiTheme.PaperWarm.b, 0.94f), rounded: true);
            UiBuilder.AddOutline(root.gameObject, UiTheme.StitchBorder, new Vector2(2f, -2f));
            UiBuilder.AddShadow(root.gameObject, UiTheme.ShadowWarm, new Vector2(0f, -6f));
            dock.Root = root;

            dock.AddItem(root, 0, "CampShop", "营地", actions.Camp);
            dock.AddItem(root, 1, "Missions", "任务", actions.Missions);
            // Home already has Coral primary CTA「翻开旅程」; dock Run is nav, not a second primary.
            dock.AddItem(root, 2, "Run", "出发", actions.Run, primary: false);
            dock.AddItem(root, 3, "Codex", "图鉴", actions.Codex);
            dock.AddItem(root, 4, "Settings", "设置", actions.Settings);
            return dock;
        }

        public void SetSelected(string panelName)
        {
            foreach (var item in items)
            {
                // Selected tab uses the shared PineGreen active accent; Coral stays reserved for
                // the single primary CTA. Unselected keeps a faint paper fill for press feedback.
                var selected = !string.IsNullOrEmpty(panelName) && item.Name == panelName;
                item.Background.color = selected ? UiBuilder.AccentActive :
                    new Color(UiTheme.PaperWarm.r, UiTheme.PaperWarm.g, UiTheme.PaperWarm.b, 0.35f);
                item.Label.color = selected ? Color.white : UiTheme.InkPrimary;
            }
        }

        public void SetBadges(int missionCount, int campCount)
        {
            SetBadge("Missions", missionCount);
            SetBadge("CampShop", campCount);
        }

        private void AddItem(RectTransform parent, int index, string name, string label,
            Action callback, bool primary = false)
        {
            var width = 1f / 5f;
            var rect = UiBuilder.CreateRect(name, parent, new Vector2(index * width + 0.01f, 0.10f),
                new Vector2((index + 1) * width - 0.01f, 0.90f), Vector2.zero, Vector2.zero,
                primary ? UiTheme.CoralAction
                    : new Color(UiTheme.PaperWarm.r, UiTheme.PaperWarm.g, UiTheme.PaperWarm.b, 0.35f),
                rounded: true);
            var image = rect.GetComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            UiBuilder.WireClickFeedback(button);
            if (callback != null) button.onClick.AddListener(() => callback());

            var text = UiBuilder.CreateTmpText("Label", rect, label, UiTheme.Caption, FontStyle.Bold,
                TextAnchor.MiddleCenter, primary ? Color.white : UiTheme.InkPrimary);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            var badge = UiBuilder.CreateRect("Badge", rect, new Vector2(0.72f, 0.70f),
                new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero, UiTheme.BerryDanger, circle: true);
            var badgeText = UiBuilder.CreateTmpText("Count", badge, string.Empty, UiTheme.Micro,
                FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            badgeText.rectTransform.anchorMin = Vector2.zero;
            badgeText.rectTransform.anchorMax = Vector2.one;
            badgeText.rectTransform.offsetMin = Vector2.zero;
            badgeText.rectTransform.offsetMax = Vector2.zero;
            badge.gameObject.SetActive(false);

            items.Add(new Item { Name = name, Background = image, Label = text, Badge = badge.gameObject, BadgeText = badgeText });
        }

        private void SetBadge(string name, int count)
        {
            var item = items.Find(candidate => candidate.Name == name);
            if (item == null) return;
            item.Badge.SetActive(count > 0);
            item.BadgeText.text = count > 99 ? "99+" : Mathf.Max(0, count).ToString();
        }
    }
}
