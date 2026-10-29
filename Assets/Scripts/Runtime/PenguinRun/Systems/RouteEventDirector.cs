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

        private IReadOnlyList<RouteEventDefinition> pool;
        private readonly int seed;
        private readonly RunnerMapTheme openingTheme;
        private readonly Dictionary<RunnerMapTheme, int> eventOrdinals = new();
        private RunnerMapTheme theme;
        private readonly bool suppressed;
        private float nextEventDistance = FirstEventDistance;
        private float eventStartDistance;
        private RouteEventDefinition activeDefinition;
        private RouteEventSnapshot snapshot = RouteEventSnapshot.Empty;
        private readonly List<string> completedEventIds = new();

        public RouteEventDirector(RunnerMapTheme theme, bool daily, int runCount, bool suppressed = false)
        {
            this.theme = theme;
            openingTheme = theme;
            pool = RouteEventCatalog.For(theme);
            seed = BuildSeed(theme, daily, runCount);
            this.suppressed = suppressed;
        }

        public RouteEventSnapshot Snapshot => snapshot;

        public bool AwaitingChoice => snapshot.AwaitingChoice;

        public int CompletedCount { get; private set; }

        public int RiskyCompletedCount { get; private set; }
        public IReadOnlyList<string> CompletedEventIds => completedEventIds;

        public RunnerMapTheme CurrentTheme => theme;

        /// <summary>Swap only future event definitions; progress and spacing remain run-global.</summary>
        public void SetTheme(RunnerMapTheme nextTheme, float currentDistance)
        {
            if (!RunThemeSessionState.IsValid(nextTheme)) return;
            if (theme == nextTheme) return;
            if (snapshot.AwaitingChoice) Cancel(currentDistance);
            theme = nextTheme;
            pool = RouteEventCatalog.For(nextTheme);
        }

        public bool Tick(float distanceMeters)
        {
            if (suppressed) return false;

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

            eventOrdinals.TryGetValue(theme, out var ordinal);
            activeDefinition = pool[DeterministicIndex(theme, ordinal++, pool.Count)];
            eventOrdinals[theme] = ordinal;
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

        /// <summary>Higher-priority world content cancels without awarding a choice.</summary>
        public void Cancel(float distanceMeters)
        {
            if (!snapshot.AwaitingChoice) return;
            nextEventDistance = Mathf.Max(eventStartDistance + MinimumEventSpacing,
                Mathf.Max(0f, distanceMeters) + MinimumEventSpacing);
            activeDefinition = default;
            snapshot = RouteEventSnapshot.Empty;
        }

        public void Reset()
        {
            eventOrdinals.Clear();
            nextEventDistance = FirstEventDistance;
            eventStartDistance = 0f;
            activeDefinition = default;
            snapshot = RouteEventSnapshot.Empty;
            CompletedCount = 0;
            RiskyCompletedCount = 0;
            completedEventIds.Clear();
            theme = openingTheme;
            pool = RouteEventCatalog.For(openingTheme);
        }

        private int DeterministicIndex(RunnerMapTheme eventTheme, int ordinal, int count)
        {
            unchecked
            {
                // Remove the opening theme salt, then apply this theme's original salt.
                var themeSeed = seed ^ ((int)openingTheme + 1) * 73856093
                    ^ ((int)eventTheme + 1) * 73856093;
                var value = themeSeed + ordinal * 1103515245 + 12345;
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
