using System;
using System.Collections.Generic;

namespace PenguinRun
{
    internal readonly struct ThemeRouteObstacle
    {
        public readonly float OffsetZ;
        public readonly RunnerLane Lane;
        public readonly bool Slide;
        public ThemeRouteObstacle(float z, RunnerLane lane, bool slide)
        { OffsetZ = z; Lane = lane; Slide = slide; }
    }

    internal readonly struct ThemeRouteFish
    {
        public readonly float OffsetZ;
        public readonly RunnerLane Lane;
        public readonly float BonusY;
        public ThemeRouteFish(float z, RunnerLane lane, float bonusY)
        { OffsetZ = z; Lane = lane; BonusY = bonusY; }
    }

    internal sealed class ThemeRouteEncounterPlan
    {
        public IReadOnlyList<ThemeRouteObstacle> Obstacles { get; }
        public IReadOnlyList<ThemeRouteFish> Fish { get; }
        public float Length { get; }
        internal ThemeRouteEncounterPlan(ThemeRouteObstacle[] obstacles, ThemeRouteFish[] fish, float length)
        {
            Obstacles = Array.AsReadOnly(obstacles);
            Fish = Array.AsReadOnly(fish);
            Length = length;
        }
    }

    /// <summary>Deterministic content only; selection rewards remain in the controller.</summary>
    internal static class ThemeRouteEncounterCatalog
    {
        public static bool TryCreate(RunnerMapTheme theme, string eventId, bool risky,
            RouteSegmentPattern pattern, float minimumBeatSpacing, out ThemeRouteEncounterPlan plan)
        {
            plan = null;
            if (float.IsNaN(minimumBeatSpacing) || float.IsInfinity(minimumBeatSpacing)
                || minimumBeatSpacing <= 0f) return false;
            var ice = theme == RunnerMapTheme.IceLakeEcho && eventId == "ice-fracture";
            var cedar = theme == RunnerMapTheme.CedarRuins && eventId == "cedar-rescue";
            if (!ice && !cedar) return false;
            var expected = !risky ? RouteSegmentPattern.CalmLane
                : ice ? RouteSegmentPattern.JumpArc : RouteSegmentPattern.SlideTunnel;
            if (pattern != expected) return false;
            var spacing = Math.Max(21f, minimumBeatSpacing);
            var length = 24f + 2f * spacing;
            if (float.IsInfinity(length)) return false;
            if (!risky)
            {
                var safeFish = new ThemeRouteFish[6];
                for (var i = 0; i < safeFish.Length; i++)
                    safeFish[i] = new ThemeRouteFish(2f + i * 1.85f, RunnerLane.Left, 0f);
                plan = new ThemeRouteEncounterPlan(Array.Empty<ThemeRouteObstacle>(), safeFish, 24f);
                return true;
            }
            var obstacles = new ThemeRouteObstacle[3];
            var fish = new ThemeRouteFish[ice ? 6 : 0];
            for (var i = 0; i < obstacles.Length; i++)
            {
                var z = 12f + i * spacing;
                obstacles[i] = new ThemeRouteObstacle(z, RunnerLane.Right, cedar);
                if (!ice) continue;
                fish[i * 2] = new ThemeRouteFish(z - 2.4f, RunnerLane.Right, 1.15f);
                fish[i * 2 + 1] = new ThemeRouteFish(z + 2.4f, RunnerLane.Right, 1.15f);
            }
            plan = new ThemeRouteEncounterPlan(obstacles, fish, length);
            return true;
        }
    }
}
