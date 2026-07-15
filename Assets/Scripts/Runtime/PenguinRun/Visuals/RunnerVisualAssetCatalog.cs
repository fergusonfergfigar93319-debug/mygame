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

        public static RunnerVisualDescriptor ForPickup(PickupVisualId id) =>
            new($"PenguinRun/Prefabs/Pickups/{PickupSlug(id)}", Vector3.zero, Vector3.zero, Vector3.one);

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
            id = kind switch
            {
                PowerUpKind.Dash => PickupVisualId.Dash,
                PowerUpKind.Magnet => PickupVisualId.Magnet,
                PowerUpKind.Shield => PickupVisualId.Shield,
                PowerUpKind.ScoreStar => PickupVisualId.ScoreStar,
                PowerUpKind.GlideFeather => PickupVisualId.GlideFeather,
                _ => PickupVisualId.FishSnack,
            };
            return kind is PowerUpKind.Dash or PowerUpKind.Magnet or PowerUpKind.Shield or
                PowerUpKind.ScoreStar or PowerUpKind.GlideFeather;
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
