using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// UI 图标注册表：白色剪影 Sprite（Kenney CC0 + 自绘补充），随 Image.color 染色。
    /// 替代早期 Unicode 字符图标（♥ ★ ◷ ← 等），保证跨字体/跨平台渲染一致。
    /// </summary>
    public static class UiIcons
    {
        public const string Star = "icon_star";
        public const string Back = "icon_back";
        public const string Pause = "icon_pause";
        public const string Gear = "icon_gear";
        public const string Trophy = "icon_trophy";
        public const string Check = "icon_check";
        public const string Cross = "icon_cross";
        public const string Locked = "icon_locked";
        public const string HeartFull = "icon_heart_full";
        public const string HeartEmpty = "icon_heart_empty";
        public const string Timer = "icon_timer";
        /// <summary>路程标记（旗帜）。由 tools/generate_ui_icons.py 生成。</summary>
        public const string Distance = "icon_distance";
        /// <summary>鱼干（局内货币）。由 tools/generate_ui_icons.py 生成。</summary>
        public const string Fish = "icon_fish";

        private static readonly Dictionary<string, Sprite> cache = new();

        /// <summary>加载图标 Sprite；导入器未按 Sprite 配置时回退 Texture2D 动态创建。</summary>
        public static Sprite Load(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;
            if (cache.TryGetValue(iconName, out var cached) && cached != null) return cached;

            var sprite = Resources.Load<Sprite>($"PenguinRun/UiIcons/{iconName}");
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>($"PenguinRun/UiIcons/{iconName}");
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = iconName;
                }
            }

            cache[iconName] = sprite;
            return sprite;
        }

        /// <summary>创建一个染色图标 Image；加载失败时保持空白（调用方可退回文字）。</summary>
        public static Image CreateImage(string name, Transform parent, string iconName, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            var sprite = Load(iconName);
            img.sprite = sprite != null ? sprite : UiBuilder.CircleSprite;
            img.color = tint;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }
    }
}
