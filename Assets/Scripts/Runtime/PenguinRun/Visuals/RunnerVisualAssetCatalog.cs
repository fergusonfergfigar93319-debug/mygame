using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    public readonly struct RunnerVisualDescriptor
    {
        public readonly string ResourcePath;
        public readonly Vector3 LocalPosition;
        public readonly Vector3 LocalEuler;
        public readonly Vector3 LocalScale;
        public readonly string FallbackResourcePath;

        public bool IsEmpty => string.IsNullOrWhiteSpace(ResourcePath);

        public RunnerVisualDescriptor(string resourcePath, Vector3 position, Vector3 euler,
            Vector3 scale, string fallbackResourcePath = "")
        {
            ResourcePath = resourcePath ?? string.Empty;
            LocalPosition = position;
            LocalEuler = euler;
            LocalScale = scale;
            FallbackResourcePath = fallbackResourcePath ?? string.Empty;
        }
    }

    /// <summary>Stable runtime resource keys for independently-authored runner visuals.</summary>
    public static class RunnerVisualAssetCatalog
    {
        private static readonly RunnerMapTheme[] BatchThemes =
        {
            RunnerMapTheme.IceLakeEcho,
            RunnerMapTheme.CedarRuins,
            RunnerMapTheme.AuroraField,
            RunnerMapTheme.MistDike,
            RunnerMapTheme.OceanReef,
            RunnerMapTheme.SkyFlight,
        };

        private static readonly ObstacleKind[] BatchObstacleKinds =
        {
            ObstacleKind.StandardHigh,
            ObstacleKind.SlideGate,
            ObstacleKind.WideWall,
        };

        private static readonly PickupVisualId[] BatchPickupKinds =
        {
            PickupVisualId.FishSnack,
            PickupVisualId.Dash,
            PickupVisualId.Magnet,
            PickupVisualId.Shield,
            PickupVisualId.ScoreStar,
            PickupVisualId.GlideFeather,
            PickupVisualId.DoubleFishSnack,
            PickupVisualId.TimeHourglass,
            PickupVisualId.FishBomb,
            PickupVisualId.SecondHeart,
            PickupVisualId.BubbleShield,
            PickupVisualId.SeahorseBoost,
            PickupVisualId.CloudWalk,
            PickupVisualId.WindRider,
            PickupVisualId.IceMirror,
            PickupVisualId.AuroraChain,
            PickupVisualId.FogLantern,
            PickupVisualId.TreantArmor,
            PickupVisualId.CoralBounce,
            PickupVisualId.ThunderFeather,
        };

        private static readonly EnvironmentPropId[] BatchEnvironmentKinds =
        {
            EnvironmentPropId.Landmark,
            EnvironmentPropId.Signpost,
            EnvironmentPropId.Light,
            EnvironmentPropId.EdgeProp,
        };

        public static RunnerVisualDescriptor ForObstacle(RunnerMapTheme theme, ObstacleKind kind)
        {
            var slug = kind switch
            {
                ObstacleKind.StandardHigh => "standard_high",
                ObstacleKind.SlideGate => "slide_gate",
                ObstacleKind.WideWall => "wide_wall",
                _ => string.Empty,
            };
            if (string.IsNullOrEmpty(slug)) return default;
            var position = kind == ObstacleKind.SlideGate
                ? new Vector3(0f, -0.24f, 0f)
                : new Vector3(0f, -0.55f, 0f);
            return new RunnerVisualDescriptor(
                $"PenguinRun/Prefabs/Obstacles/{ThemeSlug(theme)}/{slug}",
                position, Vector3.zero, Vector3.one);
        }

        /// <summary>道具视觉放大：跑酷镜头下 1× 雕塑过小，易被读成光点；1.85× 保持可读且不挡道。</summary>
        public static RunnerVisualDescriptor ForPickup(PickupVisualId id) =>
            new($"PenguinRun/Prefabs/Pickups/{PickupSlug(id)}", Vector3.zero, Vector3.zero, Vector3.one * 1.85f);

        public static RunnerVisualDescriptor ForEnvironment(RunnerMapTheme theme, EnvironmentPropId id) =>
            new($"PenguinRun/Prefabs/Environment/{ThemeSlug(theme)}/{EnvironmentSlug(id)}",
                Vector3.zero, Vector3.zero, Vector3.one);

        public static IEnumerable<(RunnerMapTheme theme, ObstacleKind kind)> AllBatchOneObstacleKeys()
        {
            foreach (var theme in BatchThemes)
                foreach (var kind in BatchObstacleKinds)
                    yield return (theme, kind);
        }

        public static IEnumerable<PickupVisualId> AllBatchOnePickupKeys()
        {
            foreach (var id in BatchPickupKinds) yield return id;
        }

        public static IEnumerable<(RunnerMapTheme theme, EnvironmentPropId id)> AllBatchOneEnvironmentKeys()
        {
            foreach (var theme in BatchThemes)
                foreach (var id in BatchEnvironmentKinds)
                    yield return (theme, id);
        }

        public static bool TryGetPickupId(PowerUpKind kind, out PickupVisualId id)
        {
            switch (kind)
            {
                case PowerUpKind.Dash: id = PickupVisualId.Dash; return true;
                case PowerUpKind.Magnet: id = PickupVisualId.Magnet; return true;
                case PowerUpKind.Shield: id = PickupVisualId.Shield; return true;
                case PowerUpKind.ScoreStar: id = PickupVisualId.ScoreStar; return true;
                case PowerUpKind.GlideFeather: id = PickupVisualId.GlideFeather; return true;
                case PowerUpKind.DoubleFishSnack: id = PickupVisualId.DoubleFishSnack; return true;
                case PowerUpKind.TimeHourglass: id = PickupVisualId.TimeHourglass; return true;
                case PowerUpKind.FishBomb: id = PickupVisualId.FishBomb; return true;
                case PowerUpKind.SecondHeart: id = PickupVisualId.SecondHeart; return true;
                case PowerUpKind.BubbleShield: id = PickupVisualId.BubbleShield; return true;
                case PowerUpKind.SeahorseBoost: id = PickupVisualId.SeahorseBoost; return true;
                case PowerUpKind.CloudWalk: id = PickupVisualId.CloudWalk; return true;
                case PowerUpKind.WindRider: id = PickupVisualId.WindRider; return true;
                case PowerUpKind.IceMirror: id = PickupVisualId.IceMirror; return true;
                case PowerUpKind.AuroraChain: id = PickupVisualId.AuroraChain; return true;
                case PowerUpKind.FogLantern: id = PickupVisualId.FogLantern; return true;
                case PowerUpKind.TreantArmor: id = PickupVisualId.TreantArmor; return true;
                case PowerUpKind.CoralBounce: id = PickupVisualId.CoralBounce; return true;
                case PowerUpKind.ThunderFeather: id = PickupVisualId.ThunderFeather; return true;
                default: id = PickupVisualId.FishSnack; return false;
            }
        }

        public static string ThemeSlug(RunnerMapTheme theme) => theme switch
        {
            RunnerMapTheme.IceLakeEcho => "ice_lake",
            RunnerMapTheme.CedarRuins => "cedar",
            RunnerMapTheme.AuroraField => "aurora",
            RunnerMapTheme.MistDike => "mist",
            RunnerMapTheme.OceanReef => "ocean",
            RunnerMapTheme.SkyFlight => "sky",
            _ => "ice_lake",
        };

        public static string ObstacleSlug(ObstacleKind kind) => kind switch
        {
            ObstacleKind.StandardHigh => "standard_high",
            ObstacleKind.SlideGate => "slide_gate",
            ObstacleKind.WideWall => "wide_wall",
            _ => string.Empty,
        };

        public static string PickupSlug(PickupVisualId id) => id switch
        {
            PickupVisualId.FishSnack => "fish_snack",
            PickupVisualId.Dash => "dash",
            PickupVisualId.Magnet => "magnet",
            PickupVisualId.Shield => "shield",
            PickupVisualId.ScoreStar => "score_star",
            PickupVisualId.DoubleFishSnack => "double_fish_snack",
            PickupVisualId.TimeHourglass => "time_hourglass",
            PickupVisualId.FishBomb => "fish_bomb",
            PickupVisualId.SecondHeart => "second_heart",
            PickupVisualId.BubbleShield => "bubble_shield",
            PickupVisualId.SeahorseBoost => "seahorse_boost",
            PickupVisualId.CloudWalk => "cloud_walk",
            PickupVisualId.WindRider => "wind_rider",
            PickupVisualId.IceMirror => "ice_mirror",
            PickupVisualId.AuroraChain => "aurora_chain",
            PickupVisualId.FogLantern => "fog_lantern",
            PickupVisualId.TreantArmor => "treant_armor",
            PickupVisualId.CoralBounce => "coral_bounce",
            PickupVisualId.ThunderFeather => "thunder_feather",
            _ => "glide_feather",
        };

        public static string EnvironmentSlug(EnvironmentPropId id) => id switch
        {
            EnvironmentPropId.Landmark => "landmark",
            EnvironmentPropId.Signpost => "signpost",
            EnvironmentPropId.Light => "light",
            _ => "edge_prop",
        };
    }
}
