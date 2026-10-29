using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    internal sealed partial class SegmentSpawner
    {
        private readonly RouteEncounterReservation routeReservation = new();
        private RunnerMapTheme routeTheme;
        private readonly List<GameObject> routeObstacles = new();
        private readonly List<GameObject> routeFish = new();
        public bool RouteEncounterActive => routeReservation.Active;
        public bool HasRouteChoiceMarker => routeEventRoot != null;

        public bool HasPendingGlidePickup
        {
            get
            {
                foreach (var pickup in powerUps)
                    if (pickup.Value == PowerUpKind.GlideFeather && pickup.Key != null
                        && pickup.Key.activeSelf && pickup.Key.transform.position.z >= 0f) return true;
                return false;
            }
        }

        public void SpawnRouteEventOutcome(RouteEventResolution resolution, float runDistance, float beatSpacing)
        {
            if (!resolution.IsValid || config.IsOnboarding || RouteEncounterActive) return;
            if (!ThemeRouteEncounterCatalog.TryCreate(routeTheme, resolution.EventId,
                resolution.Risky, resolution.Outcome.SegmentPattern, beatSpacing, out var plan))
            {
                SpawnLegacyRouteOutcome(resolution.Outcome, resolution.Risky, runDistance);
                return;
            }

            // Never erase existing hazards: place the complete encounter beyond their far edge.
            var spawnZ = Mathf.Max(worldTuning.spawnDistance + 18f, FarthestRouteContentZ() + 12f);
            if (!routeReservation.TryReserve(spawnZ, plan.Length)) return;
            var palette = waveDirector.CurrentPalette;
            foreach (var obstacle in plan.Obstacles)
            {
                var before = obstacles.Count;
                SpawnStandardObstacle(spawnZ + obstacle.OffsetZ, obstacle.Lane, obstacle.Slide, palette, runDistance);
                for (var i = before; i < obstacles.Count; i++) routeObstacles.Add(obstacles[i]);
            }
            foreach (var fish in plan.Fish)
            {
                var spawned = SpawnCoin(spawnZ + fish.OffsetZ, fish.Lane, runDistance, fish.BonusY);
                if (spawned != null) routeFish.Add(spawned);
            }
            SpawnRouteEncounterPresentation(plan, resolution, spawnZ, runDistance);
        }

        private float FarthestRouteContentZ()
        {
            var farthest = 0f;
            foreach (var obstacle in obstacles)
            {
                if (obstacle == null) continue;
                var extent = obstacleSpecs.TryGetValue(obstacle, out var spec) ? spec.HalfDz : 2f;
                farthest = Mathf.Max(farthest, obstacle.transform.position.z + extent);
            }
            foreach (var fish in coins)
                if (fish != null) farthest = Mathf.Max(farthest, fish.transform.position.z + 1f);
            foreach (var power in powerUps.Keys)
                if (power != null) farthest = Mathf.Max(farthest, power.transform.position.z + 1f);
            foreach (var gate in speedGates)
                if (gate != null) farthest = Mathf.Max(farthest, gate.transform.position.z + 2f);
            return farthest;
        }

        public void CancelRouteEncounter()
        {
            if (routeEventRoot != null) Object.Destroy(routeEventRoot);
            routeEventRoot = null;
            while (routeObstacles.Count > 0)
            {
                var obstacle = routeObstacles[routeObstacles.Count - 1];
                routeObstacles.RemoveAt(routeObstacles.Count - 1);
                TryRemoveObstacle(obstacle);
            }
            foreach (var fish in routeFish)
            {
                if (fish == null) continue;
                coins.Remove(fish);
                coinYDelta.Remove(fish);
                coinPool.Return(fish);
            }
            routeFish.Clear();
            foreach (var root in routeOutcomeRoots)
                if (root != null) Object.Destroy(root);
            routeOutcomeRoots.Clear();
            ClearRoutePresentation();
            routeReservation.Clear();
        }

        private void TickRouteReservation(float dt, float effectiveSpeed, float runDistance)
        {
            // PickupSystem removes collected fish from the live registry; return owned inactive
            // fish here before a later Rent can recycle them into unrelated content.
            for (var i = routeFish.Count - 1; i >= 0; i--)
            {
                var fish = routeFish[i];
                if (fish != null && fish.activeSelf) continue;
                if (fish != null)
                {
                    coins.Remove(fish);
                    coinYDelta.Remove(fish);
                    coinPool.Return(fish);
                }
                routeFish.RemoveAt(i);
            }
            var wasActive = RouteEncounterActive;
            routeReservation.Advance(dt, effectiveSpeed, worldTuning.despawnZ);
            TickRoutePresentation(dt, effectiveSpeed, runDistance);
            if (wasActive && !RouteEncounterActive) CancelRouteEncounter();
        }

        public bool SpawnRouteEvent(RouteEventSnapshot routeEvent, float runDistance)
        {
            if (config.IsOnboarding || RouteEncounterActive || !routeEvent.AwaitingChoice || routeEventRoot != null)
            {
                return false;
            }

            var spawnZ = Mathf.Max(
                worldTuning.spawnDistance + 34f,
                nextSpawnZ + worldTuning.spawnDistance + 24f);
            // Targeted previews must reach the player inside the existing 72m decision window.
            // These collider-free markers do not displace or clear the current track content.
            if (IsThemeRouteEvent(routeEvent.Id)) spawnZ = Mathf.Min(spawnZ, 64f);
            var surfaceY = TrackTerrain.SurfaceY(spawnZ, runDistance, baseGroundY);
            var root = new GameObject($"Route Event {routeEvent.Id}");
            root.transform.position = new Vector3(0f, surfaceY, spawnZ);

            // Collider-free route markers: RunnerVisuals creates mesh renderers without physics components.
            var safeColor = new Color(0.34f, 0.82f, 0.68f, 0.88f);
            var riskColor = new Color(1f, 0.42f, 0.32f, 0.92f);
            for (var i = 0; i < 5; i++)
            {
                var z = i * 5.5f;
                AddRouteLaneMarker(root.transform, RunnerLane.Left, z, safeColor, false);
                AddRouteLaneMarker(root.transform, RunnerLane.Right, z, riskColor, true);
            }

            AddChildPrimitive(root.transform, "Safe Beacon", PrimitiveType.Cylinder,
                new Vector3(RunnerLane.Left.ToX(laneWidth), 1.1f, 20f),
                new Vector3(0.5f, 2.2f, 0.5f), safeColor, true);
            AddChildPrimitive(root.transform, "Risk Beacon", PrimitiveType.Cylinder,
                new Vector3(RunnerLane.Right.ToX(laneWidth), 1.35f, 20f),
                new Vector3(0.62f, 2.7f, 0.62f), riskColor, true);
            AddChildPrimitive(root.transform, "Decision Line", PrimitiveType.Cube,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(laneWidth * 3.15f, 0.07f, 0.26f),
                new Color(1f, 0.9f, 0.55f, 0.8f), true);

            routeEventRoot = root;
            nextSpawnZ = Mathf.Max(nextSpawnZ, spawnZ + 30f - worldTuning.spawnDistance);
            return true;
        }

        public bool TryConsumeRouteChoice(RunnerLane playerLane, out bool risky)
        {
            risky = false;
            if (routeEventRoot == null || routeEventRoot.transform.position.z > 2.5f)
            {
                return false;
            }

            risky = playerLane == RunnerLane.Right;
            Object.Destroy(routeEventRoot);
            routeEventRoot = null;
            return true;
        }

        private void SpawnLegacyRouteOutcome(
            RouteEventOutcome outcome,
            bool risky,
            float runDistance)
        {
            var spawnZ = worldTuning.spawnDistance + 18f;
            // Legacy reward trails also own their finite tail until it passes the recycle line.
            if (!routeReservation.TryReserve(spawnZ, 28f)) return;
            var surfaceY = TrackTerrain.SurfaceY(spawnZ, runDistance, baseGroundY);
            var lane = risky ? RunnerLane.Right : RunnerLane.Left;
            var laneX = lane.ToX(laneWidth);
            var color = risky
                ? new Color(1f, 0.55f, 0.28f, 0.72f)
                : new Color(0.48f, 0.9f, 0.7f, 0.72f);
            var root = new GameObject($"Route Outcome {outcome.SegmentPattern}");
            root.transform.position = new Vector3(0f, surfaceY, spawnZ);

            for (var i = 0; i < 8; i++)
            {
                var sideOffset = outcome.SegmentPattern switch
                {
                    RouteSegmentPattern.Slalom => (i % 2 == 0 ? -0.38f : 0.38f) * laneWidth,
                    RouteSegmentPattern.FishRibbon => Mathf.Sin(i * 0.8f) * laneWidth * 0.28f,
                    RouteSegmentPattern.ChaseLine => Mathf.Sin(i * 0.45f) * laneWidth * 0.18f,
                    _ => 0f,
                };
                var height = outcome.SegmentPattern == RouteSegmentPattern.JumpArc
                    ? Mathf.Sin(i / 7f * Mathf.PI) * 1.15f
                    : 0.12f;
                AddChildPrimitive(root.transform, $"Reward Trail {i}", PrimitiveType.Cube,
                    new Vector3(laneX + sideOffset, height, i * 2.4f),
                    new Vector3(0.34f, 0.08f, 0.52f), color, true);
            }

            // 「散鱼」等奖励段：除引导条外真正刷出可拾取金币。
            if (outcome.SegmentPattern == RouteSegmentPattern.FishRibbon)
                SpawnCoinLine(spawnZ + 1.2f, lane, 12, runDistance);
            else if (outcome.SegmentPattern is RouteSegmentPattern.CalmLane or RouteSegmentPattern.JumpArc)
                SpawnCoinLine(spawnZ + 2f, lane, 6, runDistance);

            routeOutcomeRoots.Add(root);
        }

        private void TickRouteMarkers(float dt, float effectiveSpeed, float runDistance)
        {
            TickRouteReservation(dt, effectiveSpeed, runDistance);
            if (routeEventRoot != null)
            {
                MoveRouteMarkerRoot(routeEventRoot, dt, effectiveSpeed, runDistance);
                if (routeEventRoot.transform.position.z < worldTuning.despawnZ)
                {
                    Object.Destroy(routeEventRoot);
                    routeEventRoot = null;
                }
            }

            for (var i = routeOutcomeRoots.Count - 1; i >= 0; i--)
            {
                var root = routeOutcomeRoots[i];
                if (root == null)
                {
                    routeOutcomeRoots.RemoveAt(i);
                    continue;
                }

                MoveRouteMarkerRoot(root, dt, effectiveSpeed, runDistance);
                if (root.transform.position.z + 28f < worldTuning.despawnZ)
                {
                    Object.Destroy(root);
                    routeOutcomeRoots.RemoveAt(i);
                }
            }
        }

        private void MoveRouteMarkerRoot(GameObject root, float dt, float effectiveSpeed, float runDistance)
        {
            var position = root.transform.position;
            position.z -= effectiveSpeed * dt;
            position.y = TrackTerrain.SurfaceY(position.z, runDistance, baseGroundY);
            root.transform.position = position;
        }

        private void AddRouteLaneMarker(
            Transform root,
            RunnerLane lane,
            float localZ,
            Color color,
            bool diamond)
        {
            var marker = AddChildPrimitive(root, diamond ? "Risk Diamond" : "Safe Tile",
                PrimitiveType.Cube,
                new Vector3(lane.ToX(laneWidth), 0.08f, localZ),
                new Vector3(0.72f, 0.08f, 0.72f), color, true);
            if (diamond)
            {
                marker.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
        }
    }
}
