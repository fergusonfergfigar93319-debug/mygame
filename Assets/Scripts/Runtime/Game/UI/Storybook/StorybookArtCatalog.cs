using UnityEngine;

namespace PenguinRun.Game.UI.Storybook
{
    /// <summary>Centralized Resources paths and deterministic visual fallbacks.</summary>
    public static class StorybookArtCatalog
    {
        public const string LobbyPanoramaPath = "PenguinRun/StorybookUI/lobby_panorama";
        public const string MascotPath = "PenguinRun/StorybookUI/mascot_gugugaga";

        private static Sprite lobbyPanorama;
        private static Sprite mascot;

        public static Sprite LobbyPanorama => lobbyPanorama ??= LoadSprite(LobbyPanoramaPath);

        public static Sprite Mascot => mascot ??= LoadSprite(MascotPath) ?? CreateFallbackMascot();

        private static Sprite LoadSprite(string resourcePath)
        {
            var importedSprite = Resources.Load<Sprite>(resourcePath);
            if (importedSprite != null) return importedSprite;

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name + "_RuntimeSprite";
            return sprite;
        }

        public static Sprite CreateFallbackMascot()
        {
            return UiBuilder.CircleSprite;
        }
    }
}
