using System;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Game.Growth
{
    public readonly struct ThemeMasteryRecord
    {
        public ThemeMasteryRecord(RunnerMapTheme theme, int eventsCompleted, int perfectCount,
            int flawlessCount, int bestPerfectStreak, bool signatureCompleted,
            bool flawlessSignatureCompleted, int contractsCompleted, int stars, bool tutorialSeen)
        {
            Theme = theme;
            EventsCompleted = Math.Max(0, eventsCompleted);
            PerfectCount = Math.Max(0, perfectCount);
            FlawlessCount = Math.Max(0, flawlessCount);
            BestPerfectStreak = Math.Max(0, bestPerfectStreak);
            SignatureCompleted = signatureCompleted;
            FlawlessSignatureCompleted = flawlessSignatureCompleted;
            ContractsCompleted = Math.Max(0, contractsCompleted);
            Stars = Mathf.Clamp(stars, 0, 3);
            TutorialSeen = tutorialSeen;
        }

        public RunnerMapTheme Theme { get; }
        public int EventsCompleted { get; }
        public int PerfectCount { get; }
        public int FlawlessCount { get; }
        public int BestPerfectStreak { get; }
        public bool SignatureCompleted { get; }
        public bool FlawlessSignatureCompleted { get; }
        public int ContractsCompleted { get; }
        public int Stars { get; }
        public bool TutorialSeen { get; }
    }

    public static class ThemeMasteryStore
    {
        private const string SchemaKey = "theme_mastery_schema";

        public static void EnsureMigrated()
        {
            if (PlayerSave.ReadGrowthInt(SchemaKey, 0) <= 0)
                PlayerSave.WriteGrowthInt(SchemaKey, 1);
        }

        public static ThemeMasteryRecord For(RunnerMapTheme theme)
        {
            EnsureMigrated();
            var signature = Read(theme, "signature");
            return new ThemeMasteryRecord(theme, Read(theme, "events"), Read(theme, "perfect"),
                Read(theme, "flawless"), Read(theme, "best_streak"), signature >= 1,
                signature >= 2, Read(theme, "contracts"), Read(theme, "stars"),
                Read(theme, "tutorial") != 0);
        }

        public static ThemeMasteryRecord ApplyRun(ThemeMasteryRunSnapshot run)
        {
            var previous = For(run.Theme);
            var events = previous.EventsCompleted + run.EventsCompleted;
            var perfect = previous.PerfectCount + run.PerfectCount;
            var flawless = previous.FlawlessCount + run.FlawlessCount;
            var contracts = previous.ContractsCompleted + (run.ContractCompleted ? 1 : 0);
            var bestStreak = Math.Max(previous.BestPerfectStreak, run.BestPerfectStreak);
            var signature = previous.FlawlessSignatureCompleted || run.FlawlessSignatureCompleted
                ? 2 : previous.SignatureCompleted || run.SignatureCompleted ? 1 : 0;
            var stars = signature >= 2 ? 3 : perfect >= 5 && contracts >= 1 ? 2 : events >= 3 ? 1 : 0;

            Write(run.Theme, "events", events);
            Write(run.Theme, "perfect", perfect);
            Write(run.Theme, "flawless", flawless);
            Write(run.Theme, "best_streak", bestStreak);
            Write(run.Theme, "signature", signature);
            Write(run.Theme, "contracts", contracts);
            Write(run.Theme, "stars", Math.Max(previous.Stars, stars));
            Write(run.Theme, "tutorial", previous.TutorialSeen || run.TutorialSeen ? 1 : 0);
            return For(run.Theme);
        }

        private static int Read(RunnerMapTheme theme, string suffix) =>
            Math.Max(0, PlayerSave.ReadGrowthInt(Key(theme, suffix), 0));

        private static void Write(RunnerMapTheme theme, string suffix, int value) =>
            PlayerSave.WriteGrowthInt(Key(theme, suffix), Math.Max(0, value));

        private static string Key(RunnerMapTheme theme, string suffix) =>
            $"theme_mastery_v1_{(int)theme}_{suffix}";
    }
}
