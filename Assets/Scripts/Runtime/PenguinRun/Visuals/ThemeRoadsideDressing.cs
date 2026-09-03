using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Mid-ground CC0 belt between near set pieces (~9–11 m) and distant ridges (~18 m+).
    /// Placement stays outside <c>SceneryMinAbsX</c>; the spawn callback must strip colliders.
    /// </summary>
    internal static class ThemeRoadsideDressing
    {
        public delegate void SpawnOpenProp(string modelName, float x, float z, Vector3 scale, Color tint);

        public const float MinAbsX = 13.2f;
        public const float MaxAbsX = 15.2f;

        public static void PlaceFarBelt(RunnerMapTheme theme, SpawnOpenProp spawn)
        {
            if (spawn == null) return;

            for (var i = 0; i < 8; i++)
            {
                var z = 10f + i * 15f;
                var (left, right, scale, tint) = Pick(theme, i);
                spawn(left, i % 2 == 0 ? -14.4f : -13.2f, z + 8.2f, scale, tint);
                spawn(right, i % 2 == 0 ? 13.6f : 15.0f, z + 17.4f, scale * 0.86f, tint);
            }
        }

        public static (string left, string right, Vector3 scale, Color tint) Pick(
            RunnerMapTheme theme, int index)
        {
            return theme switch
            {
                RunnerMapTheme.CedarRuins => (
                    NaturePropCatalog.CedarFar(index),
                    NaturePropCatalog.CedarFar(index + 1),
                    new Vector3(1.15f, 1.15f, 1.15f),
                    new Color(0.52f, 0.68f, 0.48f)),
                RunnerMapTheme.AuroraField => (
                    NaturePropCatalog.AuroraFar(index),
                    NaturePropCatalog.AuroraFar(index + 1),
                    new Vector3(1.05f, 1.05f, 1.05f),
                    new Color(0.62f, 0.48f, 0.92f)),
                RunnerMapTheme.MistDike => (
                    NaturePropCatalog.MistFar(index),
                    NaturePropCatalog.MistFar(index + 1),
                    Vector3.one,
                    new Color(0.62f, 0.74f, 0.78f)),
                RunnerMapTheme.OceanReef => (
                    NaturePropCatalog.OceanFar(index),
                    NaturePropCatalog.OceanFar(index + 1),
                    new Vector3(1.1f, 1.1f, 1.1f),
                    new Color(0.28f, 0.72f, 0.68f)),
                RunnerMapTheme.SkyFlight => (
                    NaturePropCatalog.SkyFar(index),
                    NaturePropCatalog.SkyFar(index + 1),
                    Vector3.one,
                    new Color(0.86f, 0.78f, 0.58f)),
                _ => (
                    NaturePropCatalog.IceLakeFar(index),
                    NaturePropCatalog.IceLakeFar(index + 1),
                    new Vector3(1.15f, 1.15f, 1.15f),
                    new Color(0.42f, 0.72f, 0.92f)),
            };
        }
    }
}
