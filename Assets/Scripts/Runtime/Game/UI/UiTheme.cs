using TMPro;
using UnityEngine;

namespace PenguinRun.Game.UI
{
    /// <summary>Shared visual tokens for every uGUI surface in PenguinRun.</summary>
    public static class UiTheme
    {
        public static readonly Color AccentCyan = new(0.0f, 0.85f, 0.95f, 1f);
        public static readonly Color WarmGold = new(1f, 0.85f, 0.4f, 1f);
        public static readonly Color Success = new(0.35f, 0.95f, 0.6f, 1f);
        public static readonly Color Warning = new(1f, 0.55f, 0.35f, 1f);
        public static readonly Color Danger = new(1f, 0.35f, 0.45f, 1f);
        public static readonly Color SurfaceBase = new(0.04f, 0.09f, 0.16f, 1f);
        public static readonly Color Surface1 = new(0.07f, 0.14f, 0.24f, 1f);
        public static readonly Color Surface2 = new(0.1f, 0.18f, 0.3f, 1f);
        public static readonly Color SurfaceElevated = new(0.13f, 0.22f, 0.36f, 1f);
        public static readonly Color TextPrimary = new(1f, 1f, 1f, 0.96f);
        public static readonly Color TextSecondary = new(0.78f, 0.86f, 0.94f, 0.78f);
        public static readonly Color TextTertiary = new(0.6f, 0.7f, 0.82f, 0.55f);
        public static readonly Color TextOnAccent = new(0.04f, 0.08f, 0.14f, 1f);
        public static readonly Color BorderSubtle = new(0.25f, 0.5f, 0.7f, 0.2f);
        public static readonly Color BorderDefault = new(0.3f, 0.6f, 0.8f, 0.35f);
        public static readonly Color BorderAccent = new(0.0f, 0.85f, 0.95f, 0.55f);

        // Storybook camp semantic palette. Menu and progression surfaces use these
        // names so the runner/Boss palette can remain independently readable.
        public static readonly Color PaperBase = new(0.965f, 0.918f, 0.792f, 1f);
        public static readonly Color PaperWarm = new(1f, 0.968f, 0.89f, 1f);
        public static readonly Color InkPrimary = new(0.145f, 0.235f, 0.255f, 1f);
        public static readonly Color InkSecondary = new(0.275f, 0.39f, 0.40f, 0.82f);
        public static readonly Color PineGreen = new(0.235f, 0.47f, 0.45f, 1f);
        public static readonly Color MistBlue = new(0.64f, 0.82f, 0.82f, 1f);
        public static readonly Color CoralAction = new(0.925f, 0.39f, 0.255f, 1f);
        public static readonly Color BerryDanger = new(0.45f, 0.24f, 0.35f, 1f);
        public static readonly Color StitchBorder = new(0.19f, 0.32f, 0.33f, 0.72f);
        public static readonly Color ShadowWarm = new(0.20f, 0.16f, 0.13f, 0.24f);

        public const int Display = 72;
        public const int H1 = 48;
        public const int H2 = 36;
        public const int Body = 28;
        public const int Caption = 22;
        public const int Micro = 18;

        public const float Spacing1 = 4f;
        public const float Spacing2 = 8f;
        public const float Spacing3 = 12f;
        public const float Spacing4 = 16f;
        public const float Spacing5 = 24f;
        public const float Spacing6 = 32f;
        public const float RadiusS = 8f;
        public const float RadiusM = 14f;
        public const float RadiusL = 22f;
        public const float StorybookCardRadius = 24f;
        public const float DurFast = 0.15f;
        public const float DurBase = 0.25f;
        public const float DurSlow = 0.4f;
        public const float StorybookPageTurnDuration = 0.34f;
        public const float StorybookPunchDuration = 0.18f;

        private static Font fallbackFont;
        private static TMP_FontAsset tmpFont;

        /// <summary>Bundled Simplified-Chinese UI fallback; loaded once for both uGUI and IMGUI surfaces.</summary>
        public static Font FallbackFont => fallbackFont ??= Resources.Load<Font>("PenguinRun/Fonts/NotoSansCJKsc-Regular")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        /// <summary>Runtime-generated dynamic SDF font so bundled CJK glyphs can populate on demand.</summary>
        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (tmpFont != null) return tmpFont;
                tmpFont = TMP_FontAsset.CreateFontAsset(FallbackFont);
                tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                tmpFont.enableMultiAtlasSupport = true;
                return tmpFont;
            }
        }
    }
}
