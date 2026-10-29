// In-memory save and unrelated external systems. Mastery, outcome routing, growth and result view are production.
using System.Collections.Generic;
namespace PenguinRun.Game.Save
{
    public static class PlayerSave
    {
        public static readonly Dictionary<string, int> Values = new();
        public static readonly Dictionary<string, int> Writes = new();
        public static int BestScore, TotalRuns, Fish;
        public static bool RescuedTuanTuan => true;
        public static string PlayerNickname => "test";
        public static int ReadGrowthInt(string key, int fallback = 0) => Values.TryGetValue(key, out var value) ? value : fallback;
        public static void WriteGrowthInt(string key, int value)
        {
            Values[key] = value;
            Writes[key] = Writes.TryGetValue(key, out var count) ? count + 1 : 1;
        }
        public static void AddFishSnacks(int amount) { Fish += amount; }
        public static string GetOrCreatePlayerId() => "test";
        public static void IncrementTotalRuns() { TotalRuns++; }
        public static void Flush() { }
    }
    public static class CampUpgrades
    {
        public static float GetFishGainMultiplier() => 1f;
        public static float GetScoreMultiplier() => 1f;
    }
    public static class RunRecords
    {
        public static void AddRecord(int s, int d, int c, float t, bool best, string mode, int rank) { }
    }
    public static class CodexUnlocks
    {
        public static int Calls;
        public static void ApplyAfterRun(int s, int d, float t, bool daily) { Calls++; }
    }
}
namespace PenguinRun.Game.Mission
{
    public static class MissionStore
    {
        public static int Calls;
        public static int CountClaimableMissions(bool daily) => 0;
        public static void RecordAfterRun(int s, int d, int c, bool daily) { Calls++; }
    }
}
namespace PenguinRun.Game.Shop
{
    public static class ShopStore
    {
        public static (float, float) ConsumePendingForOutcome() => (1f, 1f);
    }
}
namespace PenguinRun.Game.Leaderboard
{
    public sealed class LeaderboardEntry
    {
        public string id, playerId, nickname, mode, challengeBucket;
        public int totalScore, distanceScoreUnits, fishSnacks, beaconCount, lorePageCount;
        public float survivalSeconds;
        public long timestampMillis;
        public bool rescuedTuanTuan;
    }
    public static class LeaderboardStore
    {
        public static string NewEntryId() => "test";
        public static (int RankByScore, int? RankInChallengeBucket) Submit(LeaderboardEntry entry) => (1, null);
    }
}
namespace PenguinRun.Game.Growth
{
    public static class CompanionBondStore
    {
        public static int Calls;
        public static float GetPassiveHelperBonus(CompanionId id) => 0f;
        public static float GetConditionalHelperChance(CompanionId id) => 0f;
        public static CompanionBondAward AwardAfterRun(int r, int risky, int skill, bool daily, bool relic)
        { Calls++; return new(8, 8, false, false); }
    }
    public static class CodexDiscoveryStore
    {
        public static bool Discover(string id) => false;
    }
    public sealed class DiscoveryDefinition { public string Title => "test"; }
    public static class CodexDiscoveryCatalog
    {
        public static DiscoveryDefinition For(string id) => new();
    }
}
namespace PenguinRun.Game
{
    public enum DifficultyKind { Normal }
    public sealed class DifficultyPreset
    {
        public float ScoreMultiplier => 1f;
        public static DifficultyPreset ForKind(DifficultyKind kind) => new();
    }
}
namespace PenguinRun.Game.UI
{
    public static class RunnerDeathOverlay
    {
        public static string GetDeathAnalysisHint(int distance) => string.Empty;
    }
}
