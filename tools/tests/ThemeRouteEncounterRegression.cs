using System;
using System.Collections.Generic;
namespace PenguinRun.Tests
{
    public static class ThemeRouteEncounterRegression
    {
        public static int CaseCount { get; private set; }
        public static List<string> Run()
        {
            var failures = new List<string>();
            CaseCount = 0;
            void Check(bool ok, string label) { CaseCount++; if (!ok) failures.Add(label); }
            foreach (var theme in new[] { RunnerMapTheme.IceLakeEcho, RunnerMapTheme.CedarRuins })
            foreach (var risky in new[] { false, true })
            {
                var ice = theme == RunnerMapTheme.IceLakeEcho;
                var id = ice ? "ice-fracture" : "cedar-rescue";
                var pattern = !risky ? RouteSegmentPattern.CalmLane : ice ? RouteSegmentPattern.JumpArc : RouteSegmentPattern.SlideTunnel;
                var created = ThemeRouteEncounterCatalog.TryCreate(theme, id, risky, pattern, 48f, out var plan);
                Check(created && plan != null, id + " creates target choice " + risky);
                if (!created || plan == null) continue;
                Check(plan.Obstacles.Count == (risky ? 3 : 0), id + " obstacle count");
                Check(plan.Fish.Count == (risky && !ice ? 0 : 6), id + " preserves physical fish budget");
                for (var i = 0; i < plan.Obstacles.Count; i++)
                {
                    var o = plan.Obstacles[i];
                    Check(o.Lane == RunnerLane.Right && o.Slide == !ice, id + " leaves escape lanes and uses matching obstacle kind");
                    Check(o.OffsetZ >= 12f && plan.Length - o.OffsetZ >= 12f, id + " entry/exit clearance");
                    if (i > 0) Check(o.OffsetZ - plan.Obstacles[i-1].OffsetZ >= 48f, id + " recovery spacing");
                }
                foreach (var fish in plan.Fish)
                    Check(fish.OffsetZ > 0 && fish.OffsetZ < plan.Length && fish.Lane == (risky ? RunnerLane.Right : RunnerLane.Left)
                        && fish.BonusY == (risky ? 1.15f : 0f), id + " fish in bounded reachable lane");
                ThemeRouteEncounterCatalog.TryCreate(theme, id, risky, pattern, 48f, out var repeat);
                Check(repeat.Length == plan.Length && repeat.Obstacles.Count == plan.Obstacles.Count && repeat.Fish.Count == plan.Fish.Count, id + " deterministic");
            }
            foreach (RunnerMapTheme theme in Enum.GetValues(typeof(RunnerMapTheme)))
                Check(!ThemeRouteEncounterCatalog.TryCreate(theme, "unrecognized", true, RouteSegmentPattern.JumpArc, 40, out _), "unknown event fallback " + theme);
            Check(!ThemeRouteEncounterCatalog.TryCreate(RunnerMapTheme.CedarRuins, "ice-fracture", true, RouteSegmentPattern.JumpArc, 40, out _), "theme/event mismatch fallback");
            Check(!ThemeRouteEncounterCatalog.TryCreate(RunnerMapTheme.IceLakeEcho, "ice-fracture", true, RouteSegmentPattern.CalmLane, 40, out _), "wrong pattern fallback");
            foreach (var invalid in new[] { -1f, 0f, float.NaN, float.PositiveInfinity, float.MaxValue })
                Check(!ThemeRouteEncounterCatalog.TryCreate(RunnerMapTheme.IceLakeEcho, "ice-fracture", true, RouteSegmentPattern.JumpArc, invalid, out var p) && p == null, "reject invalid/unbounded spacing");
            foreach (var speed in new[] { 10f, 20f, 32f * 1.22f })
            foreach (var flight in new[] { 24f / 22f, 24f * 1.12f / (22f * 0.56f) })
            {
                var spacing = speed * (flight + 0.2f);
                ThemeRouteEncounterCatalog.TryCreate(RunnerMapTheme.IceLakeEcho, "ice-fracture", true, RouteSegmentPattern.JumpArc, spacing, out var p);
                Check((p.Obstacles[1].OffsetZ-p.Obstacles[0].OffsetZ) / speed >= flight + 0.19f, "jump recovery at speed/glide");
            }
            var reservation = new RouteEncounterReservation();
            var icePreview = new RouteEventSnapshot("ice-fracture", RouteEventKind.RiskFork, "冰镜分岔", "沿岸稳行", "穿越裂冰", 0, true);
            var cedarPreview = new RouteEventSnapshot("cedar-rescue", RouteEventKind.CompanionRescue, "雪松呼救", "清理近路", "深入根廊", 0, true);
            Check(ThemeRoutePresentation.ChoiceLabels(icePreview).Contains("跳跃线") && ThemeRoutePresentation.ChoiceLabels(icePreview).Contains("稳健补给"), "ice action expectations");
            Check(ThemeRoutePresentation.ChoiceLabels(cedarPreview).Contains("滑铲线"), "cedar action expectations");
            Check(ThemeRoutePresentation.ChoiceLabels(RouteEventSnapshot.Empty) == string.Empty, "hide resolved choice labels");
            var legacy = new RouteEventSnapshot("other", RouteEventKind.RiskFork, "title", "safe", "risk", 0, true);
            Check(ThemeRoutePresentation.ChoiceLabels(legacy) == "左道 安心：safe    右道 冒险：risk", "unrelated event wording unchanged");
            Check(!reservation.Active && reservation.TryReserve(100, 120), "reserve initially idle window");
            Check(!reservation.TryReserve(20, 30), "cannot overwrite live reservation");
            reservation.Advance(0, 30, -8);
            Check(reservation.EndZ == 220 && reservation.Active, "pause does not move reservation");
            reservation.Advance(1, 20, -8);
            Check(reservation.EndZ == 200, "move reservation once by simulation distance");
            reservation.Advance(7, 30, -8);
            Check(!reservation.Active, "reservation releases after entire exit passes despawn");
            Check(reservation.TryReserve(110, 24), "scheduling resumes after completion");
            reservation.Clear(); reservation.Clear();
            Check(!reservation.Active && reservation.EndZ == 0, "cancellation/reset idempotent");
            Check(!reservation.TryReserve(float.NaN, 20) && !reservation.TryReserve(10, -1), "invalid reservation rejected");
            var director = new RouteEventDirector(RunnerMapTheme.IceLakeEcho, false, 10);
            Check(director.Tick(220), "existing first event distance");
            var first = director.Choose(true, 300);
            Check(first.IsValid && !director.Choose(true, 300).IsValid && director.CompletedCount == 1 && director.RiskyCompletedCount == 1, "selection reward authority once only");
            Check(!director.Tick(579) && director.Tick(580), "original event spacing retained");
            director.Cancel(600); director.Cancel(600);
            Check(!director.AwaitingChoice && !director.Choose(false, 600).IsValid && director.CompletedCount == 1, "cancel does not award/re-resolve");
            Check(!director.Tick(879) && director.Tick(880), "cancel defers retry by original spacing");
            director.Reset();
            Check(director.CompletedCount == 0 && director.RiskyCompletedCount == 0 && director.CompletedEventIds.Count == 0 && !director.AwaitingChoice, "reset clears route progress");
            var onboarding = new RouteEventDirector(RunnerMapTheme.CedarRuins, false, 0, true);
            Check(!onboarding.Tick(10000) && !onboarding.Choose(true, 10000).IsValid, "onboarding suppresses routes");
            return failures;
        }
    }
}
