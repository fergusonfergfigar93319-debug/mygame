using System;
using System.Collections.Generic;
namespace PenguinRun.Tests
{
    public static class ThemeContinuityRegression
    {
        private static readonly List<string> Failures = new();
        public static int CaseCount { get; private set; }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Case(string name, Action body)
        {
            CaseCount++;
            try { body(); }
            catch (Exception error) { Failures.Add(name + ": " + error.GetBaseException().Message); }
        }

        public static string[] Run()
        {
            Failures.Clear();
            CaseCount = 0;
            Case("all opening themes and Daily lock", () =>
            {
                foreach (RunnerMapTheme theme in Enum.GetValues(typeof(RunnerMapTheme)))
                {
                    var state = new RunThemeSessionState(theme, true);
                    state.Tick(120f, true);
                    Check(state.OpeningTheme == theme && state.CurrentTheme == theme, "opening changed");
                    Check(!state.RequestSwitch() && !state.SwitchPending, "Daily requested rotation");
                    Check(!state.ApplyTheme(RunThemeSessionState.NextTheme(theme)), "Daily applied rotation");
                }
            });
            Case("52 seconds produces exactly one request", () =>
            {
                var state = new RunThemeSessionState(RunnerMapTheme.IceLakeEcho, false);
                state.Tick(51.9f, true); Check(!state.SwitchPending, "early request");
                state.Tick(.2f, true); Check(state.SwitchPending, "missing request");
                Check(!state.RequestSwitch(), "duplicate request accepted");
                state.Tick(500f, true);
                Check(state.ConsumeSwitch() == RunnerMapTheme.CedarRuins, "wrong next theme");
                Check(!state.SwitchPending && state.ElapsedSeconds == 0f, "catch-up debt survived");
                Check(state.ConsumeSwitch() == RunnerMapTheme.CedarRuins, "consumed twice");
            });
            Case("pause and invalid deltas freeze clock", () =>
            {
                var state = new RunThemeSessionState(RunnerMapTheme.IceLakeEcho, false);
                state.Tick(30f, true); state.Tick(500f, false); state.Tick(-1f, true);
                state.Tick(float.NaN, true); state.Tick(float.PositiveInfinity, true);
                Check(state.ElapsedSeconds == 30f && !state.SwitchPending, "clock advanced during freeze");
            });
            Case("duplicate or invalid theme has no side effects", () =>
            {
                var state = new RunThemeSessionState(RunnerMapTheme.CedarRuins, false); state.Tick(10f, true);
                Check(!state.ApplyTheme(state.CurrentTheme) && !state.ApplyTheme((RunnerMapTheme)99), "invalid apply accepted");
                Check(state.ElapsedSeconds == 10f, "no-op reset clock");
                state.RequestSwitch(); state.ConsumeSwitch(); state.Reset();
                Check(state.CurrentTheme == state.OpeningTheme && !state.SwitchPending, "reset failed");
                Check(RunThemeSessionState.NextTheme(RunnerMapTheme.SkyFlight) == RunnerMapTheme.IceLakeEcho, "wrap failed");
            });
            Case("theme-local route sequence preserves original seeds", () =>
            {
                foreach (var daily in new[] { false, true })
                for (var run = 0; run < 24; run++)
                {
                    var route = new RouteEventDirector(RunnerMapTheme.IceLakeEcho, daily, run);
                    var ordinals = new int[6];
                    float distance = 220;
                    for (var visit = 0; visit < 18; visit++)
                    {
                        var theme = (RunnerMapTheme)(visit % 6);
                        route.SetTheme(theme, distance);
                        Check(route.Tick(distance), "route did not start");
                        var pool = RouteEventCatalog.For(theme);
                        int value;
                        unchecked {
                            var seed = ((int)theme + 1) * 73856093 ^ (daily ? 19349663 : 0) ^ run * 83492791;
                            value = seed + ordinals[(int)theme]++ * 1103515245 + 12345;
                            value ^= value >> 16;
                        }
                        Check(route.Snapshot.Id == pool[(int)((uint)value % (uint)pool.Count)].Id, "changed original theme seed/ordinal");
                        route.Choose(visit % 2 == 0, distance);
                        distance += 280;
                    }
                    Check(route.CompletedCount == 18 && route.RiskyCompletedCount == 9, "lost global counters");
                }
            });
            Case("route cancellation spacing, invalid apply and reset", () =>
            {
                var route = new RouteEventDirector(RunnerMapTheme.CedarRuins, false, 7);
                route.Tick(220);
                var id = route.Snapshot.Id;
                route.SetTheme((RunnerMapTheme)99, 260);
                route.SetTheme(RunnerMapTheme.CedarRuins, 260);
                Check(route.AwaitingChoice && route.Snapshot.Id == id, "no-op cancelled route");
                route.SetTheme(RunnerMapTheme.AuroraField, 260);
                Check(!route.AwaitingChoice && route.CompletedCount == 0 && !route.Choose(false, 260).IsValid, "cancel rewarded");
                Check(!route.Tick(539) && route.Tick(540), "spacing not based on actual cancellation distance");
                route.Reset();
                Check(route.CurrentTheme == RunnerMapTheme.CedarRuins && route.Tick(220) && route.Snapshot.Id == id, "reset lost opening sequence");
            });
            Case("pending world event drains without scheduling; pause freezes", () =>
            {
                var events = new WorldEventScheduler();
                Check(!events.Tick(500, 0) && !events.Tick(500, .02f, false), "started during freeze/pending");
                Check(events.Tick(500, .02f) && events.EventActive, "milestone not scheduled");
                var remaining = events.EventTimer;
                events.Tick(1000, 0, false);
                events.Tick(1000, float.NaN, false);
                Check(events.EventTimer == remaining, "pause consumed active event");
                events.Tick(1000, remaining + .1f, false);
                Check(!events.EventActive && !events.Tick(1000, .02f, false), "pending added content");
                Check(events.Tick(1000, .02f), "resumed scheduling lost milestone");
                events.Reset();
                Check(!events.EventActive && !events.Tick(499, .02f), "reset lost initial milestone");
                Check(!new WorldEventScheduler(true).Tick(10000, 100), "onboarding event leaked");
            });
            return Failures.ToArray();
        }
    }
}
