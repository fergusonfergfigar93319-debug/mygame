using System.Collections.Generic;
using PenguinRun.Game.Save;

namespace PenguinRun.Game.Growth
{
    public static class CodexDiscoveryStore
    {
        private const string DiscoveredKey = "codex_discovered_csv";
        private const string SeenKey = "codex_seen_csv";

        public static bool Discover(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || CodexDiscoveryCatalog.For(id) == null) return false;
            var discovered = ReadSet(DiscoveredKey);
            if (!discovered.Add(id)) return false;
            WriteSet(DiscoveredKey, discovered);
            return true;
        }

        public static bool IsDiscovered(string id) => ReadSet(DiscoveredKey).Contains(id);

        public static bool IsNew(string id) => IsDiscovered(id) && !ReadSet(SeenKey).Contains(id);

        public static int CountDiscovered()
        {
            var count = 0;
            foreach (var definition in CodexDiscoveryCatalog.All)
                if (IsDiscovered(definition.Id)) count++;
            return count;
        }

        public static int CountNew()
        {
            var count = 0;
            foreach (var definition in CodexDiscoveryCatalog.All)
                if (IsNew(definition.Id)) count++;
            return count;
        }

        public static void MarkAllSeen()
        {
            var seen = ReadSet(SeenKey);
            foreach (var definition in CodexDiscoveryCatalog.All)
                if (IsDiscovered(definition.Id)) seen.Add(definition.Id);
            WriteSet(SeenKey, seen);
        }

        private static HashSet<string> ReadSet(string key)
        {
            var values = new HashSet<string>();
            var raw = PlayerSave.ReadGrowthString(key);
            if (string.IsNullOrWhiteSpace(raw)) return values;
            foreach (var token in raw.Split(','))
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrEmpty(trimmed)) values.Add(trimmed);
            }
            return values;
        }

        private static void WriteSet(string key, HashSet<string> values) =>
            PlayerSave.WriteGrowthString(key, string.Join(",", values));
    }
}
