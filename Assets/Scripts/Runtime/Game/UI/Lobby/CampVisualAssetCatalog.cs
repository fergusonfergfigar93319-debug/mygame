using System.Collections.Generic;
using PenguinRun.Game.Growth;
using PenguinRun.Game.UI.Storybook;
using UnityEngine;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Stable premium-home resource paths with deterministic legacy fallbacks.</summary>
    public static class CampVisualAssetCatalog
    {
        public const string PanoramaPath = "PenguinRun/StorybookUI/PremiumHome/camp_panorama_premium";
        public const string MascotPath = "PenguinRun/StorybookUI/PremiumHome/mascot_gugugaga_premium";
        public const string TuanTuanPath = "PenguinRun/StorybookUI/PremiumHome/companion_tuantuan";
        public const string DrorPath = "PenguinRun/StorybookUI/PremiumHome/companion_dror";
        public const string BonfireAtlasPath = "PenguinRun/StorybookUI/PremiumHome/camp_bonfire_atlas";
        public const string WorkshopAtlasPath = "PenguinRun/StorybookUI/PremiumHome/camp_workshop_atlas";
        public const string CompanionHutAtlasPath = "PenguinRun/StorybookUI/PremiumHome/camp_companion_hut_atlas";
        public const string LibraryAtlasPath = "PenguinRun/StorybookUI/PremiumHome/camp_library_atlas";
        public const string LookoutAtlasPath = "PenguinRun/StorybookUI/PremiumHome/camp_lookout_atlas";
        public const string ForegroundPropsPath = "PenguinRun/StorybookUI/PremiumHome/camp_foreground_props";
        public const string SmokePath = "PenguinRun/StorybookUI/PremiumHome/camp_smoke_soft";
        public const string GlowPath = "PenguinRun/StorybookUI/PremiumHome/camp_glow_soft";

        private static readonly Dictionary<string, Sprite> Cache = new();
        private static readonly Dictionary<string, Sprite> AtlasSlices = new();

        public static Sprite Panorama => Load(PanoramaPath) ?? StorybookArtCatalog.LobbyPanorama;
        public static Sprite Mascot => Load(MascotPath) ?? StorybookArtCatalog.Mascot;
        /// <summary>
        /// 伙伴立绘。**没有**美术时返回 null，由调用方隐藏节点。
        ///
        /// 此前回退是 <c>?? Mascot</c>：伙伴美术从未产出过（`companion_tuantuan` /
        /// `companion_dror` 只声明了路径，生成器没有生成），于是大厅会在吉祥物旁边再画
        /// 一只一模一样的吉祥物。少一个角色好过多一只重复的角色。
        /// </summary>
        public static Sprite Companion(bool drorUnlocked) => Load(drorUnlocked ? DrorPath : TuanTuanPath);
        public static Sprite ForegroundProps => Load(ForegroundPropsPath);
        public static Sprite Smoke => Load(SmokePath);
        public static Sprite Glow => Load(GlowPath);

        public static Sprite Building(CampBuildingId id, int level)
        {
            var path = id switch
            {
                CampBuildingId.Bonfire => BonfireAtlasPath,
                CampBuildingId.Workshop => WorkshopAtlasPath,
                CampBuildingId.CompanionHut => CompanionHutAtlasPath,
                CampBuildingId.LibraryTent => LibraryAtlasPath,
                _ => LookoutAtlasPath,
            };
            var atlas = Load(path);
            if (atlas == null || atlas.texture == null) return UiBuilder.CircleSprite;

            var state = Mathf.Clamp(level <= 0 ? 1 : level, 1, 3) - 1;
            var key = path + "#" + state;
            if (AtlasSlices.TryGetValue(key, out var cached) && cached != null) return cached;

            var texture = atlas.texture;
            var width = texture.width / 3f;
            var rect = new Rect(width * state, 0f, width, texture.height);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = $"{texture.name}_State{state + 1}";
            AtlasSlices[key] = sprite;
            return sprite;
        }

        private static Sprite Load(string resourcePath)
        {
            if (Cache.TryGetValue(resourcePath, out var cached)) return cached;
            var imported = Resources.Load<Sprite>(resourcePath);
            if (imported == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    imported = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    imported.name = texture.name + "_RuntimeSprite";
                }
            }
            Cache[resourcePath] = imported;
            return imported;
        }
    }
}
