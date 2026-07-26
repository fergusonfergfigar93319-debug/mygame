using System;
using System.Collections.Generic;
using PenguinRun.Game.Growth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    public readonly struct LobbyCampActions
    {
        public readonly Action OpenCamp;
        public readonly Action OpenCodex;

        public LobbyCampActions(Action openCamp, Action openCodex)
        {
            OpenCamp = openCamp;
            OpenCodex = openCodex;
        }
    }

    /// <summary>Interactive, layered camp panorama. It owns presentation and emits navigation callbacks only.</summary>
    public sealed class LobbyCampScene
    {
        private sealed class BuildingView
        {
            public Image Image;
            public Button Button;
            public GameObject CaptionRoot;
            public TextMeshProUGUI Caption;
        }

        public RectTransform Root { get; private set; }

        private readonly Dictionary<CampBuildingId, BuildingView> buildings = new();
        private readonly List<Button> sceneButtons = new();
        private CanvasGroup hotspotGroup;
        private Image mascot;
        private Image companion;
        private TextMeshProUGUI companionBondLabel;
        private Image companionBondFill;
        private RectTransform companionBondPlate;
        private LobbyCampActions actions;
        private bool inputEnabled = true;
        private bool panelOpen;

        public static LobbyCampScene Build(Transform parent, LobbyCampActions actions)
        {
            var scene = new LobbyCampScene { actions = actions };
            scene.Root = UiBuilder.CreateRect("PremiumCampScene", parent, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, UiTheme.PaperBase);
            scene.Root.SetAsFirstSibling();

            var panorama = UiBuilder.CreateRect("Panorama", scene.Root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.white);
            var panoramaImage = panorama.GetComponent<Image>();
            panoramaImage.sprite = CampVisualAssetCatalog.Panorama;
            panoramaImage.preserveAspect = false;
            panoramaImage.raycastTarget = false;
            if (panoramaImage.sprite == null)
                panoramaImage.color = Color.Lerp(UiTheme.PaperBase, UiTheme.MistBlue, 0.5f);

            var wash = UiBuilder.CreateRect("AmbientWash", scene.Root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.08f, 0.17f, 0.22f, 0.08f));
            wash.GetComponent<Image>().raycastTarget = false;

            var hotspotLayer = UiBuilder.CreateRect("HotspotLayer", scene.Root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.clear);
            hotspotLayer.GetComponent<Image>().raycastTarget = false;
            scene.hotspotGroup = hotspotLayer.gameObject.AddComponent<CanvasGroup>();

            var buildingLayer = UiBuilder.CreateRect("BuildingLayer", scene.Root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.clear);
            buildingLayer.GetComponent<Image>().raycastTarget = false;
            scene.CreateEffect(buildingLayer, "BonfireGlow", new Vector2(0.34f, 0.31f),
                new Vector2(0.17f, 0.28f), CampVisualAssetCatalog.Glow, 0.58f, pulse: true);
            scene.CreateEffect(buildingLayer, "BonfireSmoke", new Vector2(0.34f, 0.43f),
                new Vector2(0.08f, 0.22f), CampVisualAssetCatalog.Smoke, 0.54f, smokeDrift: true);
            scene.CreateBuilding(buildingLayer, hotspotLayer, CampBuildingId.Bonfire, new Vector2(0.34f, 0.27f), new Vector2(0.14f, 0.20f));
            scene.CreateBuilding(buildingLayer, hotspotLayer, CampBuildingId.Workshop, new Vector2(0.18f, 0.40f), new Vector2(0.18f, 0.28f));
            scene.CreateBuilding(buildingLayer, hotspotLayer, CampBuildingId.CompanionHut, new Vector2(0.82f, 0.35f), new Vector2(0.18f, 0.27f));
            scene.CreateBuilding(buildingLayer, hotspotLayer, CampBuildingId.LibraryTent, new Vector2(0.68f, 0.29f), new Vector2(0.17f, 0.25f));
            scene.CreateBuilding(buildingLayer, hotspotLayer, CampBuildingId.Lookout, new Vector2(0.87f, 0.54f), new Vector2(0.15f, 0.34f));

            var characterLayer = UiBuilder.CreateRect("CharacterLayer", scene.Root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.clear);
            characterLayer.GetComponent<Image>().raycastTarget = false;
            scene.mascot = scene.CreateCharacter(characterLayer, "Mascot", new Vector2(0.39f, 0.12f),
                new Vector2(0.58f, 0.69f), CampVisualAssetCatalog.Mascot, null);
            scene.companion = scene.CreateCharacter(characterLayer, "Companion", new Vector2(0.55f, 0.16f),
                new Vector2(0.68f, 0.56f), CampVisualAssetCatalog.Companion(false), actions.OpenCodex);
            scene.CreateCompanionBondPlate(characterLayer);
            // 伙伴美术缺席时整组隐藏，与前景道具用的是同一条约定（sprite 为空即不显示）。
            // 羁绊进度不会因此丢失：顶栏 Ticker 与战绩 Sheet 都仍然展示。
            scene.ApplyCompanionVisibility();

            var foreground = UiBuilder.CreateRect("ForegroundProps", scene.Root,
                new Vector2(0f, 0f), new Vector2(1f, 0.33f), Vector2.zero, Vector2.zero, Color.white);
            var foregroundImage = foreground.GetComponent<Image>();
            foregroundImage.sprite = CampVisualAssetCatalog.ForegroundProps;
            foregroundImage.preserveAspect = false;
            foregroundImage.raycastTarget = false;
            foreground.gameObject.SetActive(foregroundImage.sprite != null);

            hotspotLayer.SetAsLastSibling();

            scene.CreateSnowMotes(scene.Root);
            scene.ApplyInputState();
            return scene;
        }

        public void Bind(LobbyViewModel model, CampRebuildSnapshot camp, bool drorUnlocked)
        {
            mascot.sprite = CampVisualAssetCatalog.Mascot;
            companion.sprite = CampVisualAssetCatalog.Companion(drorUnlocked);
            ApplyCompanionVisibility();
            companionBondLabel.text = $"羁绊 Lv.{model.BondLevel} · {model.CompanionHint}";
            companionBondFill.fillAmount = model.BondProgress;
            foreach (var node in camp.Nodes)
            {
                if (!buildings.TryGetValue(node.Id, out var view)) continue;
                view.Image.sprite = CampVisualAssetCatalog.Building(node.Id, node.Level);
                var isFallback = view.Image.sprite == UiBuilder.CircleSprite;
                view.Image.color = isFallback ? FallbackColor(node.Id, node.Level) : Color.white;
                view.Caption.text = Caption(node);
                view.CaptionRoot.SetActive(node.Level == 0 || node.VisibleState == CampBuildingVisibleState.Affordable);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            ApplyInputState();
        }

        public void SetPanelOpen(bool open)
        {
            panelOpen = open;
            ApplyInputState();
        }

        private void ApplyInputState()
        {
            var active = inputEnabled && !panelOpen;
            foreach (var button in sceneButtons)
                if (button != null) button.interactable = active;
            if (hotspotGroup == null) return;
            hotspotGroup.alpha = panelOpen ? 0f : 1f;
            hotspotGroup.interactable = active;
            hotspotGroup.blocksRaycasts = active;
        }

        private void CreateBuilding(Transform parent, Transform hotspotParent, CampBuildingId id, Vector2 anchor, Vector2 size)
        {
            var imageRect = UiBuilder.CreateRect(id + "Visual", parent, anchor - size * 0.5f,
                anchor + size * 0.5f, Vector2.zero, Vector2.zero, Color.white);
            var image = imageRect.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var buttonRect = UiBuilder.CreateRect(id + "Hotspot", hotspotParent, anchor, anchor,
                new Vector2(56f, 56f), Vector2.zero, new Color(0f, 0f, 0f, 0f), circle: true);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonRect.GetComponent<Image>();
            if (actions.OpenCamp != null) button.onClick.AddListener(() => actions.OpenCamp());
            sceneButtons.Add(button);

            var ring = UiBuilder.CreateRect("Glow", buttonRect, new Vector2(0.14f, 0.14f),
                new Vector2(0.86f, 0.86f), Vector2.zero, Vector2.zero,
                new Color(UiTheme.WarmGold.r, UiTheme.WarmGold.g, UiTheme.WarmGold.b, 0.62f), circle: true);
            ring.GetComponent<Image>().raycastTarget = false;

            var captionRoot = UiBuilder.CreateRect("Caption", buttonRect,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(180f, 38f),
                new Vector2(0f, 26f), new Color(UiTheme.InkPrimary.r, UiTheme.InkPrimary.g, UiTheme.InkPrimary.b, 0.82f), rounded: true);
            captionRoot.GetComponent<Image>().raycastTarget = false;
            var caption = UiBuilder.CreateTmpText("Label", captionRoot, string.Empty, UiTheme.Micro,
                FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(caption.rectTransform, 6f);
            buildings[id] = new BuildingView { Image = image, Button = button, CaptionRoot = captionRoot.gameObject, Caption = caption };
        }

        private void CreateEffect(Transform parent, string name, Vector2 anchor, Vector2 size,
            Sprite sprite, float alpha, bool pulse = false, bool smokeDrift = false)
        {
            var rect = UiBuilder.CreateRect(name, parent, anchor - size * 0.5f, anchor + size * 0.5f,
                Vector2.zero, Vector2.zero, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            rect.gameObject.SetActive(sprite != null);
            if (pulse)
            {
                var motion = rect.gameObject.AddComponent<LobbyCampPulse>();
                motion.Amplitude = 0.045f;
            }
            if (smokeDrift)
                rect.gameObject.AddComponent<LobbyCampSmokeDrift>();
        }

        private void CreateCompanionBondPlate(Transform parent)
        {
            companionBondPlate = UiBuilder.CreateRect("CompanionBondPlate", parent,
                new Vector2(0.52f, 0.12f), new Vector2(0.72f, 0.19f), Vector2.zero, Vector2.zero,
                new Color(UiTheme.InkPrimary.r, UiTheme.InkPrimary.g, UiTheme.InkPrimary.b, 0.78f), rounded: true);
            var plate = companionBondPlate;
            UiBuilder.AddOutline(plate.gameObject,
                new Color(UiTheme.WarmGold.r, UiTheme.WarmGold.g, UiTheme.WarmGold.b, 0.62f), new Vector2(1f, -1f));
            plate.GetComponent<Image>().raycastTarget = false;

            companionBondLabel = UiBuilder.CreateTmpText("BondLabel", plate, string.Empty, UiTheme.Micro,
                FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            companionBondLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.34f);
            companionBondLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.96f);
            companionBondLabel.rectTransform.offsetMin = Vector2.zero;
            companionBondLabel.rectTransform.offsetMax = Vector2.zero;

            var track = UiBuilder.CreateRect("BondProgress", plate,
                new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.30f), Vector2.zero, Vector2.zero,
                new Color(1f, 1f, 1f, 0.24f), rounded: true);
            track.GetComponent<Image>().raycastTarget = false;
            companionBondFill = UiBuilder.CreateRect("Fill", track, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, UiTheme.WarmGold, rounded: true).GetComponent<Image>();
            companionBondFill.type = Image.Type.Filled;
            companionBondFill.fillMethod = Image.FillMethod.Horizontal;
            companionBondFill.raycastTarget = false;
        }

        /// <summary>伙伴立绘缺席时隐藏角色与羁绊铭牌，避免出现一个没有主体的浮空标签。</summary>
        private void ApplyCompanionVisibility()
        {
            var hasArt = companion != null && companion.sprite != null;
            if (companion != null) companion.gameObject.SetActive(hasArt);
            if (companionBondPlate != null) companionBondPlate.gameObject.SetActive(hasArt);
        }

        private Image CreateCharacter(Transform parent, string name, Vector2 min, Vector2 max,
            Sprite sprite, Action onClick)
        {
            var rect = UiBuilder.CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = onClick != null;
            var motion = rect.gameObject.AddComponent<LobbyCharacterBreath>();
            motion.Amplitude = name == "Mascot" ? 0.018f : 0.012f;
            if (onClick != null)
            {
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => onClick());
                sceneButtons.Add(button);
            }
            return image;
        }

        private void CreateSnowMotes(Transform parent)
        {
            // 雪花每帧改 anchoredPosition，会把所在 Canvas 标脏。放进独立的嵌套 Canvas，
            // 让重建圈在装饰层内，不波及营地建筑与热点按钮的合批。
            var layer = new GameObject("SnowLayer", typeof(RectTransform), typeof(Canvas));
            layer.transform.SetParent(parent, false);
            var layerRt = (RectTransform)layer.transform;
            layerRt.anchorMin = Vector2.zero;
            layerRt.anchorMax = Vector2.one;
            layerRt.sizeDelta = Vector2.zero;
            layerRt.anchoredPosition = Vector2.zero;
            var snowCanvas = layer.GetComponent<Canvas>();
            snowCanvas.overrideSorting = true;
            snowCanvas.sortingOrder = 1;

            for (var i = 0; i < 18; i++)
            {
                var x = Mathf.Repeat(i * 0.6180339f, 1f);
                var y = Mathf.Repeat(i * 0.381966f + 0.17f, 1f);
                var size = 3f + i % 4 * 1.4f;
                var mote = UiBuilder.CreateRect($"SnowMote_{i}", layer.transform, new Vector2(x, y), new Vector2(x, y),
                    new Vector2(size, size), Vector2.zero, new Color(1f, 1f, 1f, 0.24f + i % 3 * 0.07f), circle: true);
                mote.GetComponent<Image>().raycastTarget = false;
                var drift = mote.gameObject.AddComponent<LobbyCampSnowMote>();
                drift.Speed = 3.5f + i % 5;
                drift.Phase = i * 0.71f;
            }
        }

        private static string Caption(CampBuildingSnapshot node) => node.VisibleState switch
        {
            CampBuildingVisibleState.Affordable => $"{node.Definition.Title} · 可重建",
            CampBuildingVisibleState.Locked => $"{node.Definition.Title} · 未解锁",
            _ => $"{node.Definition.Title} · Lv.{node.Level}",
        };

        private static Color FallbackColor(CampBuildingId id, int level)
        {
            var baseColor = id switch
            {
                CampBuildingId.Bonfire => UiTheme.CoralAction,
                CampBuildingId.Workshop => UiTheme.PineGreen,
                CampBuildingId.CompanionHut => UiTheme.BerryDanger,
                CampBuildingId.LibraryTent => UiTheme.MistBlue,
                _ => UiTheme.WarmGold,
            };
            return Color.Lerp(new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f), baseColor,
                Mathf.Clamp01(level / 3f));
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;
        }
    }

    internal sealed class LobbyCharacterBreath : MonoBehaviour
    {
        public float Amplitude = 0.015f;
        private Vector3 origin;

        private void Awake() => origin = transform.localScale;
        private void Update() => transform.localScale = origin * (1f + Mathf.Sin(Time.unscaledTime * 1.7f) * Amplitude);
    }

    internal sealed class LobbyCampSnowMote : MonoBehaviour
    {
        public float Speed;
        public float Phase;
        private Vector2 origin;

        private void Awake() => origin = ((RectTransform)transform).anchoredPosition;
        private void Update()
        {
            var t = Time.unscaledTime + Phase;
            ((RectTransform)transform).anchoredPosition = origin +
                new Vector2(Mathf.Sin(t * 0.65f) * 5f, Mathf.Repeat(t * Speed, 46f) - 23f);
        }
    }

    internal sealed class LobbyCampPulse : MonoBehaviour
    {
        public float Amplitude = 0.04f;
        private Vector3 origin;

        private void Awake() => origin = transform.localScale;
        private void Update() => transform.localScale = origin *
            (1f + Mathf.Sin(Time.unscaledTime * 1.35f) * Amplitude);
    }

    internal sealed class LobbyCampSmokeDrift : MonoBehaviour
    {
        private Vector2 origin;

        private void Awake() => origin = ((RectTransform)transform).anchoredPosition;
        private void Update()
        {
            var t = Time.unscaledTime;
            ((RectTransform)transform).anchoredPosition = origin +
                new Vector2(Mathf.Sin(t * 0.72f) * 5f, Mathf.Sin(t * 0.41f) * 3f);
        }
    }
}
