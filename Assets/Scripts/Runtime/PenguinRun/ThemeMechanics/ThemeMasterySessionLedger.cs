using System;
using System.Collections.Generic;

namespace PenguinRun
{
    // Run-local ownership only. Persistent writes remain at the terminal outcome boundary.
    internal sealed class ThemeMasterySessionLedger
    {
        private readonly RunnerMapTheme openingTheme;
        private readonly ThemeMechanicMode mode;
        private readonly int seed;
        private readonly bool[] tutorialSeen = new bool[6];
        private readonly ThemeMasteryTracker[] trackers = new ThemeMasteryTracker[6];
        private readonly int[] skillEarned = new int[6];
        private readonly List<RunnerMapTheme> visited = new();
        private RunnerMapTheme currentTheme;

        public ThemeMasterySessionLedger(RunnerMapTheme openingTheme, ThemeMechanicMode mode,
            int seed, Func<RunnerMapTheme, bool> tutorialSeen)
        {
            if (!IsValid(openingTheme)) throw new ArgumentOutOfRangeException(nameof(openingTheme));
            this.openingTheme = openingTheme;
            this.mode = mode;
            this.seed = seed;
            for (var i = 0; i < this.tutorialSeen.Length; i++)
                this.tutorialSeen[i] = tutorialSeen?.Invoke((RunnerMapTheme)i) ?? false;
            Reset();
        }

        public ThemeMasteryTracker Current => trackers[(int)currentTheme];
        public IReadOnlyList<ThemeMasteryRunSnapshot> Snapshots
        {
            get
            {
                var snapshots = new ThemeMasteryRunSnapshot[visited.Count];
                for (var i = 0; i < visited.Count; i++) snapshots[i] = trackers[(int)visited[i]].RunSnapshot;
                return Array.AsReadOnly(snapshots);
            }
        }

        public bool SetTheme(RunnerMapTheme theme, float distance)
        {
            if (!IsValid(theme) || theme == currentTheme) return false;
            Current.LeaveTheme();
            currentTheme = theme;
            Visit(theme, distance);
            return true;
        }

        public void RecordSkillAward(int delta, float globalDistance)
        {
            if (delta <= 0) return;
            var index = (int)currentTheme;
            skillEarned[index] = (int)Math.Min(int.MaxValue, (long)skillEarned[index] + delta);
            Current.RecordSkill(skillEarned[index], globalDistance);
        }

        public void Reset()
        {
            Array.Clear(trackers, 0, trackers.Length);
            Array.Clear(skillEarned, 0, skillEarned.Length);
            visited.Clear();
            currentTheme = openingTheme;
            Visit(openingTheme, 0f);
        }

        private void Visit(RunnerMapTheme theme, float distance)
        {
            var index = (int)theme;
            if (trackers[index] != null) return;
            trackers[index] = new ThemeMasteryTracker(theme, mode, seed, tutorialSeen[index], distance);
            visited.Add(theme);
        }

        private static bool IsValid(RunnerMapTheme theme) => (int)theme >= 0 && (int)theme < 6;
    }
}
