using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class RouteEventDirector
    {
        internal const float FirstEventDistance = 220f;
        internal const float MinimumEventSpacing = 280f;
        private const float DecisionDistance = 72f;

        private readonly IReadOnlyList<RouteEventDefinition> pool;
        private readonly int seed;
        private int eventIndex;
        private float nextEventDistance = FirstEventDistance;
        private float eventStartDistance;
        private RouteEventDefinition activeDefinition;
        private RouteEventSnapshot snapshot = RouteEventSnapshot.Empty;
        private readonly List<string> completedEventIds = new();

        public RouteEventDirector(RunnerMapTheme theme, bool daily, int runCount)
        {
            pool = RouteEventCatalog.For(theme);
            seed = BuildSeed(theme, daily, runCount);
        }

        public RouteEventSnapshot Snapshot => snapshot;

        public bool AwaitingChoice => snapshot.AwaitingChoice;

        public int CompletedCount { get; private set; }

        public int RiskyCompletedCount { get; private set; }
        public IReadOnlyList<string> CompletedEventIds => completedEventIds;

        public bool Tick(float distanceMeters)
        {
            distanceMeters = Mathf.Max(0f, distanceMeters);
            if (snapshot.AwaitingChoice)
            {
                var progress = Mathf.InverseLerp(eventStartDistance, eventStartDistance + DecisionDistance, distanceMeters);
                snapshot = BuildSnapshot(activeDefinition, progress, true);
                return false;
            }

            if (pool.Count == 0 || distanceMeters < nextEventDistance)
            {
                return false;
            }

            activeDefinition = pool[DeterministicIndex(eventIndex++, pool.Count)];
            eventStartDistance = distanceMeters;
            snapshot = BuildSnapshot(activeDefinition, 0f, true);
            return true;
        }

        public RouteEventResolution Choose(bool risky, float distanceMeters)
        {
            if (!snapshot.AwaitingChoice)
            {
                return RouteEventResolution.None;
            }

            var outcome = risky ? activeDefinition.RiskOutcome : activeDefinition.SafeOutcome;
            var resolution = new RouteEventResolution(
                true,
                activeDefinition.Id,
                activeDefinition.Kind,
                risky,
                outcome);

            CompletedCount++;
            if (!string.IsNullOrEmpty(resolution.EventId) && !completedEventIds.Contains(resolution.EventId))
                completedEventIds.Add(resolution.EventId);
            if (risky)
            {
                RiskyCompletedCount++;
            }

            nextEventDistance = Mathf.Max(
                eventStartDistance + MinimumEventSpacing,
                Mathf.Max(0f, distanceMeters) + MinimumEventSpacing);
            activeDefinition = default;
            snapshot = RouteEventSnapshot.Empty;
            return resolution;
        }

        public RouteEventResolution Expire(float distanceMeters) => Choose(false, distanceMeters);

        public void Reset()
        {
            eventIndex = 0;
            nextEventDistance = FirstEventDistance;
            eventStartDistance = 0f;
            activeDefinition = default;
            snapshot = RouteEventSnapshot.Empty;
            CompletedCount = 0;
            RiskyCompletedCount = 0;
            completedEventIds.Clear();
        }

        private int DeterministicIndex(int ordinal, int count)
        {
            unchecked
            {
                var value = seed + ordinal * 1103515245 + 12345;
                value ^= value >> 16;
                return (int)((uint)value % (uint)count);
            }
        }

        private static int BuildSeed(RunnerMapTheme theme, bool daily, int runCount)
        {
            unchecked
            {
                return ((int)theme + 1) * 73856093
                    ^ (daily ? 19349663 : 0)
                    ^ Mathf.Max(0, runCount) * 83492791;
            }
        }

        private static RouteEventSnapshot BuildSnapshot(
            RouteEventDefinition definition,
            float progress01,
            bool awaitingChoice) => new(
                definition.Id,
                definition.Kind,
                definition.Title,
                definition.SafeLabel,
                definition.RiskLabel,
                progress01,
                awaitingChoice);
    }
}
