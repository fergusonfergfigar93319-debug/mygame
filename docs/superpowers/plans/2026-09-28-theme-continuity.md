# Theme Continuity and Safe Switching Implementation Plan

Approved scope: M2a + M2b from [the design specification](../specs/2026-09-28-theme-continuity-design.md).
Status updated 2026-10-01: implementation and independent code checks complete; Unity and Android runtime acceptance pending.

## Authority and preserved state

`RunnerSessionConfig.MapTheme` stays the opening snapshot. The controller owns `RunThemeSessionState` and exposes `CurrentTheme`; presentation follows a successful commit under the existing transition mask. The controller, rather than the presentation object, owns the effective-time clock and single Pending bit.

Route randomness retains the original theme/daily/opening run-count seed formula with a separate ordinal for each theme. Global route totals, next permitted distance, upgrade stacks, mechanic budgets/debt/random state, Boss eligibility and rewards stay intact across themes. Cancelling a shown choice uses the actual cancellation distance and grants no reward.

## Implementation checklist

- [x] Session state: 52 effective Running seconds, one Pending request, Daily lock for all six themes, duplicate/invalid no-op, reset and wrap.
- [x] Route director: swap pool, preserve run-global counters/spacing, resume theme-local ordinals, restore opening theme on retry.
- [x] Controller theme partial: current mechanic requests, Boss context, spawner and mastery commit; terrain profile precedes new spawn heights.
- [x] Boss: invalidate only unannounced future preview, retain prelude/active definition, restore opening context on reset.
- [x] Safe boundary: pause/tutorial/upgrade wait, 180m Boss guard, prelude/active, route choice/finite tail, nonterminal mechanic, owned reward entities.
- [x] Pending: stop new routes/mechanics/world events; existing content drains naturally. World events receive explicit simulation delta and freeze on pause.
- [x] Rewards: register only actual generated Boss reward entities; clear stale ownership on pool rental; release on collection/despawn; legacy route reward trails also reserve their tail.
- [x] Per-theme mastery ledger: A→B→A reuse, leave clears only temporary event/resonance/current streak, theme-local skill awards, late (>800m) first visit excludes expired contract.
- [x] Immutable result collection and single-snapshot compatibility; deduplicate themes and settle via existing outcome/growth boundary, with other growth once per run.
- [x] Compact actual-progress rows with scrollable result summary.
- [x] Cohesive partial extraction and lower ratchet ceilings (controller 1060, spawner 2210); do not raise BossEncounter ceiling.
- [x] Static wiring gates updated for the real partials/current-theme consumers.
- [x] Design, GDD, domain rules and verification documentation updated.

## Verification evidence

Commands run in separate fresh PowerShell 7 processes:

| Check | Result |
| --- | --- |
| TestThemeContinuity | 7 groups; state, original route seed/ordinal progression, cancel/reset, pending world events and pause |
| TestThemeMasterySession | 7 groups, including 200 late-visit seed/theme cases and production settlement with persistence boundary stubs |
| TestThemeRouteEncounters | 88 assertions |
| TestGameplayFairness | 21 groups |
| TestRunnerInput | 18 groups |
| CompileFullRoslyn | 205 sources compiled |
| Verify scripts | 23/24 pass; inherited BossEncounter 1947/1900 line failure |
| Unity batch startup | Exit 198, no valid Editor license; validator did not execute |

The route sequence regression was observed failing before correcting theme seed derivation. The mastery harness was observed failing on the missing ledger source before implementation. Other integration checks must not be described as executed Unity tests.

## Remaining runtime acceptance

- [ ] Execute `ProjectThemeContinuityValidation` in Play Mode after licensing is available. The validator uses an isolated scene and requires an already initialized mastery schema to avoid performing a save migration.
- [ ] Confirm real Boss prelude/spawn identity, reward pool recycling, legacy and custom route tails, mechanic lifecycle, retry, pause/foreground recovery and Daily across six opening themes.
- [ ] Capture transition/HUD/audio synchronization and one/six-theme result scrolling at 16:9 and ultrawide.
- [ ] Measure Android long-session performance and object recycling at all quality tiers.

The current Unity log is `_tmp/theme-continuity-unity-2026-10-01.log`. Do not infer runtime success from independent tests or Roslyn. M2c Boss distance changes and additional theme content remain separate.
