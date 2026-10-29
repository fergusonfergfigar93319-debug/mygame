using System;
using System.Collections.Generic;
using PenguinRun;
using PenguinRun.Game;
using PenguinRun.Game.Save;
using PenguinRun.Game.Growth;
using PenguinRun.Game.Mission;
using PenguinRun.Game.UI;

namespace PenguinRun.Tests
{
    public static class ThemeMasterySessionContracts
    {
        public static string[] Run()
        {
            var failures = new List<string>();
            Check("revisit and departure", Revisit, failures);
            Check("per-theme skill attribution", SkillAttribution, failures);
            Check("late first visit and deterministic selection", LateVisit, failures);
            Check("immutable distinct snapshots and legacy", Snapshots, failures);
            Check("reset and invalid requests", Reset, failures);
            Check("multi-theme outcome settlement and legacy", Settlement, failures);
            Check("six result rows and actual contract progress", ResultRows, failures);
            return failures.ToArray();
        }
        private static void Check(string name, Action test, List<string> failures)
        {
            try { test(); }
            catch (Exception ex) { failures.Add(name + ": " + ex.Message); }
        }
        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
        private static ThemeMasterySessionLedger Ledger(int seed = 42) =>
            new(RunnerMapTheme.IceLakeEcho, ThemeMechanicMode.Endless, seed, _ => false);
        private static void Complete(ThemeMasteryTracker tracker)
        {
            tracker.BeginEvent(1, false);
            tracker.RecordKeypoint(true);
            tracker.ResolveEvent(false);
        }
        private static void Revisit()
        {
            var ledger = Ledger();
            var first = ledger.Current;
            Complete(first); Complete(first); Complete(first);
            Require(first.IsResonating, "three flawless events must resonate");
            var contract = first.HudSnapshot.Contract;
            first.BeginEvent(2, true);
            first.RecordKeypoint(true);
            ledger.SetTheme(RunnerMapTheme.CedarRuins, 100f);
            Require(first.ResolveEvent(true) == ThemeMechanicGrade.None, "departure must cancel unfinished event");
            Require(!first.IsResonating && first.Resonance01 == 0f, "departure clears resonance");
            ledger.SetTheme(RunnerMapTheme.IceLakeEcho, 200f);
            Require(ReferenceEquals(first, ledger.Current), "A-B-A must reuse tracker");
            Require(first.RunSnapshot.EventsCompleted == 3, "completed progress survives revisit");
            Require(first.HudSnapshot.Contract.Kind == contract.Kind && first.HudSnapshot.Contract.Progress == contract.Progress,
                "contract survives revisit unchanged");
            Complete(first);
            Require(first.RunSnapshot.BestPerfectStreak == 3, "current streak must not cross departure");
            Require(!first.HudSnapshot.ShowTutorial, "tutorial stays seen on revisit");
        }
        private static int SeedWithSkillContract(RunnerMapTheme theme)
        {
            for (var seed = 0; seed < 1000; seed++)
                if (new ThemeMasteryTracker(theme, ThemeMechanicMode.Endless, seed, false).HudSnapshot.Contract.Kind
                    == JourneyContractKind.SkillBeforeDistance) return seed;
            throw new Exception("no deterministic skill contract seed found");
        }
        private static void SkillAttribution()
        {
            var seed = SeedWithSkillContract(RunnerMapTheme.CedarRuins);
            var ledger = Ledger(seed);
            ledger.RecordSkillAward(1100, 100f);
            ledger.SetTheme(RunnerMapTheme.CedarRuins, 200f);
            ledger.RecordSkillAward(100, 300f);
            Require(ledger.Current.HudSnapshot.Contract.Progress == 100, "opening theme skill leaked into new theme");
            ledger.SetTheme(RunnerMapTheme.IceLakeEcho, 350f);
            ledger.RecordSkillAward(1000, 400f);
            ledger.SetTheme(RunnerMapTheme.CedarRuins, 450f);
            ledger.RecordSkillAward(300, 500f);
            Require(ledger.Current.HudSnapshot.Contract.Progress == 400, "revisit must retain only its own skill");
            ledger.RecordSkillAward(800, 800f);
            Require(ledger.Current.HudSnapshot.Contract.Completed, "800m boundary should count");
            var late = Ledger(seed);
            late.SetTheme(RunnerMapTheme.CedarRuins, 500f);
            late.RecordSkillAward(1200, 800.01f);
            Require(late.Current.HudSnapshot.Contract.Progress == 0, "skill after deadline must not count");
        }
        private static void LateVisit()
        {
            for (var seed = 0; seed < 40; seed++)
            for (var index = 1; index < 6; index++)
            {
                var a = Ledger(seed);
                var b = Ledger(seed);
                a.SetTheme((RunnerMapTheme)index, 801f);
                b.SetTheme((RunnerMapTheme)index, 9000f);
                Require(a.Current.HudSnapshot.Contract.Kind != JourneyContractKind.SkillBeforeDistance,
                    "late first visit must exclude expired contract");
                Require(a.Current.HudSnapshot.Contract.Kind == b.Current.HudSnapshot.Contract.Kind,
                    "same seed and theme must choose deterministically");
            }
            var early = Ledger(SeedWithSkillContract(RunnerMapTheme.CedarRuins));
            early.SetTheme(RunnerMapTheme.CedarRuins, 800f);
            early.SetTheme(RunnerMapTheme.IceLakeEcho, 801f);
            early.SetTheme(RunnerMapTheme.CedarRuins, 900f);
            Require(early.Current.HudSnapshot.Contract.Kind == JourneyContractKind.SkillBeforeDistance,
                "revisit must not reroll expired contract");
        }
        private static void Snapshots()
        {
            var ledger = Ledger();
            Complete(ledger.Current);
            ledger.SetTheme(RunnerMapTheme.CedarRuins, 100f);
            var frozen = ledger.Snapshots;
            Complete(ledger.Current);
            Require(frozen.Count == 2 && frozen[1].EventsCompleted == 0, "snapshot cannot reflect later writes");
            try { ((IList<ThemeMasteryRunSnapshot>)frozen).Add(default); throw new Exception("snapshot mutable"); }
            catch (NotSupportedException) { }
            var source = new List<ThemeMasteryRunSnapshot> { frozen[0], frozen[0], frozen[1] };
            var copied = ThemeMasterySnapshots.CopyDistinct(source, frozen[0]);
            source.Clear();
            Require(copied.Count == 2, "deduplication/copy must isolate source and ignore legacy when collection supplied");
            Require(ThemeMasterySnapshots.CopyDistinct(null, frozen[0]).Count == 1, "legacy adapts to one snapshot");
            Require(ThemeMasterySnapshots.CopyDistinct(Array.Empty<ThemeMasteryRunSnapshot>(), frozen[0]).Count == 0,
                "explicit empty collection must not replay legacy");
        }
        private static void Reset()
        {
            var ledger = Ledger();
            Complete(ledger.Current);
            var before = ledger.Current;
            Require(!ledger.SetTheme((RunnerMapTheme)99, 10f) && !ledger.SetTheme(RunnerMapTheme.IceLakeEcho, 10f),
                "invalid/duplicate switch accepted");
            Require(ledger.Current.RunSnapshot.EventsCompleted == 1, "invalid/duplicate request mutated progress");
            ledger.SetTheme(RunnerMapTheme.SkyFlight, 100f);
            ledger.Reset();
            Require(ledger.Snapshots.Count == 1 && ledger.Current.RunSnapshot.Theme == RunnerMapTheme.IceLakeEcho,
                "reset must restore only opening theme");
            Require(ledger.Current.RunSnapshot.EventsCompleted == 0 && !ReferenceEquals(before, ledger.Current),
                "reset must clear run-local progress");
        }

        private static ThemeMasteryRunSnapshot Progress(RunnerMapTheme theme, int events = 3) =>
            new(theme, events, 2, 1, 2, false, false, false,
                JourneyContractKind.PerfectStreak, 1, true, 2, 3);

        private static void Settlement()
        {
            PlayerSave.Values.Clear(); PlayerSave.Writes.Clear();
            MissionStore.Calls = 0; CodexUnlocks.Calls = 0; CompanionBondStore.Calls = 0;
            var ice = Progress(RunnerMapTheme.IceLakeEcho);
            var cedar = Progress(RunnerMapTheme.CedarRuins, 4);
            var source = new List<ThemeMasteryRunSnapshot> { ice, cedar, cedar };
            var capture = new RunnerRunResult(1000, 800, 10, 70f, themeMastery: ice, themeMasteries: source);
            source.Clear();
            var settled = RunOutcomeRouter.Apply(1000, 800, 10, 70f, false, DifficultyKind.Normal,
                themeMastery: ice, themeMasteries: capture.ThemeMasteries);
            Require(capture.ThemeMasteries.Count == 2 && settled.ThemeMasteries.Count == 2, "capture/result must copy and deduplicate");
            Require(ThemeMasteryStore.For(RunnerMapTheme.IceLakeEcho).EventsCompleted == 3
                && ThemeMasteryStore.For(RunnerMapTheme.CedarRuins).EventsCompleted == 4,
                "each theme must settle once without replaying legacy input");
            Require(PlayerSave.Writes["theme_mastery_v1_0_events"] == 1
                && PlayerSave.Writes["theme_mastery_v1_1_events"] == 1, "v1 mastery writes must be once per theme");
            Require(MissionStore.Calls == 1 && CodexUnlocks.Calls == 1 && CompanionBondStore.Calls == 1,
                "whole-run rewards must not multiply by themes");
            Require(settled.Growth.ThemeMasteryStarGained, "earned star must reach growth summary");
            var legacy = RunOutcomeRouter.Apply(100, 100, 1, 10f, false, DifficultyKind.Normal, themeMastery: cedar);
            Require(legacy.ThemeMasteries.Count == 1 && ThemeMasteryStore.For(RunnerMapTheme.CedarRuins).EventsCompleted == 8,
                "legacy single-theme outcome must still settle exactly once");
            var empty = RunOutcomeRouter.Apply(0, 0, 0, 0f, false, DifficultyKind.Normal,
                themeMastery: ice, themeMasteries: Array.Empty<ThemeMasteryRunSnapshot>());
            Require(empty.ThemeMasteries.Count == 0 && ThemeMasteryStore.For(RunnerMapTheme.IceLakeEcho).EventsCompleted == 3,
                "empty explicit collection must not write legacy mastery");
        }

        private static void ResultRows()
        {
            var rows = new ThemeMasteryRunSnapshot[6];
            for (var i = 0; i < rows.Length; i++) rows[i] = Progress((RunnerMapTheme)i);
            var result = new RunOutcomeRouter.Result { ThemeMasteries = rows };
            var model = JourneyResultViewModel.FromRunResult(result);
            Require(model.ThemeMasteryBody.Split('\n').Length == 6, "all six progressed themes must have a row");
            Require(model.ThemeMasteryBody.Contains("2/3"), "incomplete contract must show actual progress");
            result.ThemeMasteries = new[] { default(ThemeMasteryRunSnapshot), Progress(RunnerMapTheme.CedarRuins) };
            Require(JourneyResultViewModel.FromRunResult(result).ThemeMasteryBody.Split('\n').Length == 1,
                "visited theme without effective progress must not add an empty row");
        }
    }
}
