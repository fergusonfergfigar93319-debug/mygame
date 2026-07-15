using System;
using UnityEngine;

namespace PenguinRun
{
    internal readonly struct ThemeMechanicModePolicy
    {
        public ThemeMechanicModePolicy(
            float firstDistance,
            int maxPerRun,
            int cooldownBeats,
            float baseChance,
            bool allowSignature)
        {
            FirstDistance = Mathf.Max(0f, firstDistance);
            MaxPerRun = Mathf.Max(0, maxPerRun);
            CooldownBeats = Mathf.Max(0, cooldownBeats);
            BaseChance = Mathf.Clamp01(baseChance);
            AllowSignature = allowSignature;
        }

        public float FirstDistance { get; }
        public int MaxPerRun { get; }
        public int CooldownBeats { get; }
        public float BaseChance { get; }
        public bool AllowSignature { get; }
    }

    internal static class ThemeMechanicCatalog
    {
        private const int WarningCapacity = 32;

        private static readonly bool[] MissingThemeWarnings = new bool[WarningCapacity];
        private static bool warnedOutOfRangeTheme;

        private static readonly ThemeMechanicDefinition[] All =
        {
            new(
                ThemeMechanicId.IceInertia,
                RunnerMapTheme.IceLakeEcho,
                "裂冰回响",
                "稳住换道，跃过扩散裂纹",
                "ice/cracked_plate",
                "ice/crystal_rebound_gate",
                "ice/floe_bridge",
                2.4f,
                8f,
                4f,
                1f),
            new(
                ThemeMechanicId.CedarCollapse,
                RunnerMapTheme.CedarRuins,
                "雪松坍塌",
                "跟紧缺口，穿过摆木",
                "cedar/swing_log",
                "cedar/root_gallery",
                "cedar/ruin_escape",
                2.6f,
                9f,
                4f,
                1f),
            new(
                ThemeMechanicId.AuroraPolarity,
                RunnerMapTheme.AuroraField,
                "极光换极",
                "识别蓝红极性，避开排斥线",
                "aurora/polarity_gate",
                "aurora/prism_beam",
                "aurora/magnetic_rebuild",
                2.2f,
                8f,
                4f,
                1f),
            new(
                ThemeMechanicId.MistBeacon,
                RunnerMapTheme.MistDike,
                "雾堤追光",
                "跟随真灯束，不追逐雾影",
                "mist/decoy_shadow",
                "mist/rotating_beacon",
                "mist/floodgate_chase",
                2.8f,
                10f,
                4f,
                1f),
            new(
                ThemeMechanicId.OceanTide,
                RunnerMapTheme.OceanReef,
                "潮汐回廊",
                "观察潮位，借气泡越过珊瑚",
                "ocean/coral_lift",
                "ocean/bubble_vault",
                "ocean/wave_tunnel",
                2.5f,
                9f,
                4f,
                1f),
            new(
                ThemeMechanicId.SkyWind,
                RunnerMapTheme.SkyFlight,
                "风场断岛",
                "借上升气流，修正横风偏移",
                "sky/wind_gate",
                "sky/cloud_platform",
                "sky/island_glide_chain",
                2.4f,
                10f,
                4f,
                1f),
        };

        public static ReadOnlySpan<ThemeMechanicDefinition> Definitions => All;

        public static ThemeMechanicDefinition ForTheme(RunnerMapTheme theme)
        {
            return TryForTheme(theme, out var definition) ? definition : All[0];
        }

        public static bool TryForTheme(RunnerMapTheme theme, out ThemeMechanicDefinition definition)
        {
            foreach (var candidate in All)
            {
                if (candidate.Theme != theme)
                {
                    continue;
                }

                definition = candidate;
                return true;
            }

            WarnMissingThemeOnce(theme);
            definition = default;
            return false;
        }

        public static ThemeMechanicModePolicy PolicyFor(ThemeMechanicMode mode)
        {
            return mode switch
            {
                ThemeMechanicMode.Story => new ThemeMechanicModePolicy(150f, 3, 3, 0.42f, true),
                ThemeMechanicMode.Daily => new ThemeMechanicModePolicy(120f, 8, 3, 0.58f, true),
                _ => new ThemeMechanicModePolicy(100f, int.MaxValue, 3, 0.52f, true),
            };
        }

        public static void ResetWarningState()
        {
            Array.Clear(MissingThemeWarnings, 0, MissingThemeWarnings.Length);
            warnedOutOfRangeTheme = false;
        }

        private static void WarnMissingThemeOnce(RunnerMapTheme theme)
        {
            var index = (int)theme;
            if (index >= 0 && index < MissingThemeWarnings.Length)
            {
                if (MissingThemeWarnings[index])
                {
                    return;
                }

                MissingThemeWarnings[index] = true;
            }
            else
            {
                if (warnedOutOfRangeTheme)
                {
                    return;
                }

                warnedOutOfRangeTheme = true;
            }

            Debug.LogWarning($"[ThemeMechanicCatalog] Missing definition for map theme '{theme}'. Normal segments will continue.");
        }
    }
}
