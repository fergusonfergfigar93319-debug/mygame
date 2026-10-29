using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    internal sealed partial class SegmentSpawner
    {
        private const float LandmarkLateralOffset = 10.5f;
        private readonly List<RouteGuidePoint> routeGuidePoints = new();
        private GameObject routeGuideRoot;
        private GameObject routeLandmark;
        private float routePresentationZ;
        private float routeLandmarkOffsetZ;
        private float routeLandmarkLateralOffset;

        private readonly struct RouteGuidePoint
        {
            public readonly Transform Transform;
            public readonly float X, OffsetZ, Height;
            public RouteGuidePoint(Transform transform, float x, float offsetZ, float height)
            { Transform = transform; X = x; OffsetZ = offsetZ; Height = height; }
        }

        private bool IsThemeRouteEvent(string id) =>
            routeTheme == RunnerMapTheme.IceLakeEcho && id == "ice-fracture"
            || routeTheme == RunnerMapTheme.CedarRuins && id == "cedar-rescue";

        private void SpawnRouteEncounterPresentation(ThemeRouteEncounterPlan plan,
            RouteEventResolution resolution, float spawnZ, float runDistance)
        {
            routePresentationZ = spawnZ;
            routeGuideRoot = new GameObject($"Theme Route {resolution.EventId}");
            var color = resolution.Risky ? new Color(1f, 0.78f, 0.36f, 0.8f)
                : new Color(0.66f, 0.88f, 0.92f, 0.8f);
            foreach (var obstacle in plan.Obstacles)
            {
                AddRouteGuidePoint(obstacle.OffsetZ - 4f, obstacle.Lane, 0.12f, color, false);
                AddRouteGuidePoint(obstacle.OffsetZ, obstacle.Lane,
                    obstacle.Slide ? 0.12f : 1.15f, color, obstacle.Slide);
                AddRouteGuidePoint(obstacle.OffsetZ + 4f, obstacle.Lane, 0.12f, color, false);
            }
            if (plan.Obstacles.Count == 0)
                foreach (var fish in plan.Fish)
                    AddRouteGuidePoint(fish.OffsetZ, fish.Lane, 0.12f, color, false);

            // Optional, already-authored theme resource. Missing assets never add a primitive fallback.
            routeLandmark = new GameObject("Theme Route Landmark");
            if (!visualFactory.TryAttachEnvironment(routeLandmark.transform, routeTheme, EnvironmentPropId.Landmark))
            {
                Object.Destroy(routeLandmark);
                routeLandmark = null;
            }
            else
            {
                foreach (var collider in routeLandmark.GetComponentsInChildren<Collider>(true))
                { collider.enabled = false; Object.Destroy(collider); }
                // Keep the entire rotating footprint outside the lane corridor, not only its pivot.
                var footprint = 0f;
                foreach (var renderer in routeLandmark.GetComponentsInChildren<Renderer>(true))
                {
                    var bounds = renderer.bounds;
                    var x = Mathf.Abs(bounds.center.x) + bounds.extents.x;
                    var z = Mathf.Abs(bounds.center.z) + bounds.extents.z;
                    footprint = Mathf.Max(footprint, new Vector2(x, z).magnitude);
                }
                routeLandmarkLateralOffset = Mathf.Max(LandmarkLateralOffset, 8.5f + footprint + 0.4f);
                if (float.IsNaN(routeLandmarkLateralOffset) || float.IsInfinity(routeLandmarkLateralOffset))
                { Object.Destroy(routeLandmark); routeLandmark = null; }
            }
            routeLandmarkOffsetZ = Mathf.Min(32f, plan.Length * 0.5f);
            TickRoutePresentation(0f, 0f, runDistance);
        }

        private void AddRouteGuidePoint(float offsetZ, RunnerLane lane, float height, Color color, bool diamond)
        {
            var point = AddChildPrimitive(routeGuideRoot.transform, diamond ? "Slide Guide" : "Route Guide",
                PrimitiveType.Cube, Vector3.zero, new Vector3(0.34f, 0.06f, 0.52f), color, true);
            if (diamond) point.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            routeGuidePoints.Add(new RouteGuidePoint(point.transform, lane.ToX(laneWidth), offsetZ, height));
        }

        private void TickRoutePresentation(float dt, float effectiveSpeed, float runDistance)
        {
            if (routeGuideRoot == null) return;
            routePresentationZ -= effectiveSpeed * dt;
            foreach (var point in routeGuidePoints)
            {
                if (point.Transform == null) continue;
                var z = routePresentationZ + point.OffsetZ;
                point.Transform.position = new Vector3(point.X,
                    TrackTerrain.SurfaceY(z, runDistance, baseGroundY) + point.Height, z);
            }
            if (routeLandmark == null) return;
            var landmarkZ = routePresentationZ + routeLandmarkOffsetZ;
            routeLandmark.transform.position = new Vector3(
                routeLandmarkLateralOffset + TrackTerrain.WorldCurveX(landmarkZ, runDistance),
                TrackTerrain.SurfaceY(landmarkZ, runDistance, baseGroundY), landmarkZ);
            routeLandmark.transform.rotation = Quaternion.Euler(0f,
                TrackTerrain.WorldCurveTangentYaw(landmarkZ, runDistance), 0f);
        }

        private void ClearRoutePresentation()
        {
            if (routeGuideRoot != null) Object.Destroy(routeGuideRoot);
            if (routeLandmark != null) Object.Destroy(routeLandmark);
            routeGuideRoot = routeLandmark = null;
            routeGuidePoints.Clear();
            routePresentationZ = routeLandmarkOffsetZ = 0f;
            routeLandmarkLateralOffset = 0f;
        }
    }
}
