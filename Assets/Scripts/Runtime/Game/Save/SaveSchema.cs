using System.Collections.Generic;

namespace PenguinRun.Game.Save
{
    /// <summary>Owns monotonic, non-destructive migration of long-term growth data.</summary>
    public static class SaveSchema
    {
        private const string VersionKey = "save_schema_version";
        private const string CampBonfireLevelKey = "camp_bonfire_level";
        private const string DiscoveredCsvKey = "codex_discovered_csv";
        private const string TuanBondXpKey = "bond_tuantuan_xp";
        private const string DrorBondXpKey = "bond_dror_xp";

        public const int CurrentVersion = 3;

        public static int StoredVersion => PlayerSave.ReadGrowthInt(VersionKey, 0);

        public static void EnsureMigrated()
        {
            var version = StoredVersion;
            if (version >= CurrentVersion) return;

            if (version < 1) MigrateLegacyCamp();
            if (version < 2) MigrateLegacyDiscoveries();
            if (version < 3) MigrateLegacyCompanions();

            PlayerSave.WriteGrowthInt(VersionKey, CurrentVersion);
            PlayerSave.Flush();
        }

        private static void MigrateLegacyCamp()
        {
            if (PlayerSave.VisitedCamp && PlayerSave.ReadGrowthInt(CampBonfireLevelKey, 0) <= 0)
                PlayerSave.WriteGrowthInt(CampBonfireLevelKey, 1);
        }

        private static void MigrateLegacyDiscoveries()
        {
            var discovered = ReadCsv(PlayerSave.ReadGrowthString(DiscoveredCsvKey));
            if (PlayerSave.RescuedTuanTuan) discovered.Add("story_rescue_tuantuan");
            if (PlayerSave.CompanionDrorUnlocked) discovered.Add("story_companion_dror");
            if (PlayerSave.TakamatsuDefeated) discovered.Add("story_takamatsu");
            if (PlayerSave.VisitedCamp) discovered.Add("story_first_camp");
            if (PlayerSave.Runner3DTutorialDone) discovered.Add("story_first_journey");
            foreach (var bossId in PlayerSave.GetDefeatedBossIds())
                discovered.Add("boss_" + bossId);
            PlayerSave.WriteGrowthString(DiscoveredCsvKey, string.Join(",", discovered));
        }

        private static void MigrateLegacyCompanions()
        {
            if (PlayerSave.RescuedTuanTuan && PlayerSave.ReadGrowthInt(TuanBondXpKey, 0) <= 0)
                PlayerSave.WriteGrowthInt(TuanBondXpKey, 20);
            if (PlayerSave.CompanionDrorUnlocked && PlayerSave.ReadGrowthInt(DrorBondXpKey, 0) <= 0)
                PlayerSave.WriteGrowthInt(DrorBondXpKey, 20);
        }

        private static HashSet<string> ReadCsv(string raw)
        {
            var values = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(raw)) return values;
            foreach (var token in raw.Split(','))
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrEmpty(trimmed)) values.Add(trimmed);
            }
            return values;
        }
    }
}
