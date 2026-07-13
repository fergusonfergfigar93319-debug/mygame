using PenguinRun.Game.UI.Storybook;
using PenguinRun.Game.Growth;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Text-free storybook panorama with cheap depth and snow accents.</summary>
    public static class LobbyBackground
    {
        public static RectTransform Build(Transform parent, CampRebuildSnapshot camp)
        {
            var root = UiBuilder.CreateRect("StorybookLobbyBackground", parent, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, UiTheme.PaperBase);
            root.SetAsFirstSibling();

            var panorama = UiBuilder.CreateRect("Panorama", root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.white);
            var panoramaImage = panorama.GetComponent<Image>();
            panoramaImage.sprite = StorybookArtCatalog.LobbyPanorama;
            panoramaImage.preserveAspect = false;
            panoramaImage.raycastTarget = false;
            if (panoramaImage.sprite == null)
                panoramaImage.color = Color.Lerp(UiTheme.PaperBase, UiTheme.MistBlue, 0.55f);

            var paperWash = UiBuilder.CreateRect("PaperWash", root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(UiTheme.PaperBase.r, UiTheme.PaperBase.g,
                    UiTheme.PaperBase.b, 0.12f));
            paperWash.GetComponent<Image>().raycastTarget = false;

            CreateSilhouette(root, "LeftPines", new Vector2(0f, 0f), new Vector2(0.32f, 0.38f),
                new Color(UiTheme.PineGreen.r, UiTheme.PineGreen.g, UiTheme.PineGreen.b, 0.16f), -8f);
            CreateSilhouette(root, "RightCamp", new Vector2(0.68f, 0f), new Vector2(1f, 0.30f),
                new Color(UiTheme.BerryDanger.r, UiTheme.BerryDanger.g, UiTheme.BerryDanger.b, 0.10f), 6f);

            CreateCampLayer(root, "BonfireLayer", new Vector2(0.72f, 0.08f), new Vector2(0.77f, 0.17f),
                UiTheme.CoralAction);
            CreateCampLayer(root, "WorkshopLayer", new Vector2(0.79f, 0.08f), new Vector2(0.87f, 0.20f),
                UiTheme.PineGreen);
            CreateCampLayer(root, "CompanionHutLayer", new Vector2(0.88f, 0.08f), new Vector2(0.96f, 0.19f),
                UiTheme.BerryDanger);
            CreateCampLayer(root, "LibraryTentLayer", new Vector2(0.61f, 0.08f), new Vector2(0.69f, 0.18f),
                UiTheme.MistBlue);
            CreateCampLayer(root, "LookoutLayer", new Vector2(0.53f, 0.08f), new Vector2(0.59f, 0.25f),
                UiTheme.WarmGold);
            Refresh(root, camp);

            for (var i = 0; i < 24; i++)
            {
                var x = Mathf.Repeat(i * 0.6180339f, 1f);
                var y = Mathf.Repeat(i * 0.381966f + 0.17f, 1f);
                var size = 3f + i % 4 * 1.5f;
                var mote = UiBuilder.CreateRect($"SnowMote_{i}", root,
                    new Vector2(x, y), new Vector2(x, y), new Vector2(size, size), Vector2.zero,
                    new Color(1f, 1f, 1f, 0.25f + (i % 3) * 0.08f), circle: true);
                mote.GetComponent<Image>().raycastTarget = false;
                var drift = mote.gameObject.AddComponent<LobbySnowMote>();
                drift.Speed = 4f + i % 5;
                drift.Phase = i * 0.73f;
            }

            return root;
        }

        public static void Refresh(RectTransform root, CampRebuildSnapshot camp)
        {
            if (root == null) return;
            foreach (var node in camp.Nodes)
            {
                var layer = root.Find(LayerName(node.Id));
                if (layer == null) continue;
                layer.gameObject.SetActive(node.Level > 0);
                var image = layer.GetComponent<Image>();
                if (image != null) image.color = WithAlpha(image.color, 0.25f + node.Level * 0.2f);
            }
        }

        private static void CreateCampLayer(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var layer = UiBuilder.CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero,
                WithAlpha(color, 0.45f), rounded: true);
            layer.GetComponent<Image>().raycastTarget = false;
        }

        private static string LayerName(CampBuildingId id) => id switch
        {
            CampBuildingId.Bonfire => "BonfireLayer",
            CampBuildingId.Workshop => "WorkshopLayer",
            CampBuildingId.CompanionHut => "CompanionHutLayer",
            CampBuildingId.LibraryTent => "LibraryTentLayer",
            _ => "LookoutLayer",
        };

        private static Color WithAlpha(Color color, float alpha) =>
            new(color.r, color.g, color.b, Mathf.Clamp01(alpha));

        private static void CreateSilhouette(Transform parent, string name, Vector2 min, Vector2 max,
            Color color, float drift)
        {
            var layer = UiBuilder.CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero, color,
                rounded: true);
            layer.GetComponent<Image>().raycastTarget = false;
            var motion = layer.gameObject.AddComponent<LobbyParallaxDrift>();
            motion.Distance = drift;
        }
    }

    internal sealed class LobbyParallaxDrift : MonoBehaviour
    {
        public float Distance;
        private Vector2 origin;

        private void Awake() => origin = ((RectTransform)transform).anchoredPosition;

        private void Update()
        {
            ((RectTransform)transform).anchoredPosition = origin + Vector2.right *
                (Mathf.Sin(Time.unscaledTime * 0.18f) * Distance);
        }
    }

    internal sealed class LobbySnowMote : MonoBehaviour
    {
        public float Speed;
        public float Phase;
        private Vector2 origin;

        private void Awake() => origin = ((RectTransform)transform).anchoredPosition;

        private void Update()
        {
            var t = Time.unscaledTime + Phase;
            ((RectTransform)transform).anchoredPosition = origin +
                new Vector2(Mathf.Sin(t * 0.7f) * 5f, Mathf.Repeat(t * Speed, 44f) - 22f);
        }
    }
}
