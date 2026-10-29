using System;
using System.Collections.Generic;

namespace PenguinRun
{
    public static class ThemeMasterySnapshots
    {
        // A supplied collection is authoritative; never also settle the legacy entry.
        // Duplicate themes are the same run snapshot, not additive awards. Keep the first.
        public static IReadOnlyList<ThemeMasteryRunSnapshot> CopyDistinct(
            IReadOnlyList<ThemeMasteryRunSnapshot> snapshots, ThemeMasteryRunSnapshot legacy = default)
        {
            snapshots ??= new[] { legacy };
            var copy = new List<ThemeMasteryRunSnapshot>(6);
            var seen = new bool[6];
            for (var i = 0; i < snapshots.Count; i++)
            {
                var entry = snapshots[i];
                var index = (int)entry.Theme;
                if (index < 0 || index >= seen.Length || seen[index]) continue;
                seen[index] = true;
                copy.Add(entry);
            }
            return Array.AsReadOnly(copy.ToArray());
        }
    }
}
