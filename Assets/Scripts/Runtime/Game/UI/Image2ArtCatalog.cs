using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun.Game.UI
{
    /// <summary>Image 2 illustration paths shared by secondary UI surfaces.</summary>
    public static class Image2ArtCatalog
    {
        public const string ResultCelebrationPath = "PenguinRun/StorybookUI/Image2/result_celebration_image2";
        public const string CodexGalleryPath = "PenguinRun/StorybookUI/Image2/codex_boss_gallery_image2";
        public const string ShopSupplyPath = "PenguinRun/StorybookUI/Image2/shop_supply_board_image2";
        public const string JournalMapPath = "PenguinRun/StorybookUI/Image2/journal_map_image2";

        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite ResultCelebration => Load(ResultCelebrationPath);
        public static Sprite CodexGallery => Load(CodexGalleryPath);
        public static Sprite ShopSupply => Load(ShopSupplyPath);
        public static Sprite JournalMap => Load(JournalMapPath);

        private static Sprite Load(string resourcePath)
        {
            if (Cache.TryGetValue(resourcePath, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.name = texture.name + "_RuntimeSprite";
                }
            }

            Cache[resourcePath] = sprite;
            return sprite;
        }
    }
}
