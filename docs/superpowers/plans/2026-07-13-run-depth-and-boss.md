# Run Depth and Themed Bosses Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make repeated runs meaningfully different through readable route events, capped in-run upgrade choices, skill scoring, and six theme-specific Boss traits while preserving the current runner input and hit-detection authority.

**Architecture:** Add four independent session modules—`RouteEventDirector`, `RunUpgradeState`, `SkillScoreTracker`, and `BossThemeTraitCatalog`—owned and orchestrated by `RunnerGameController`. Modules expose read-only snapshots to HUD/result presentation; they never create collision geometry or write permanent save data.

**Tech Stack:** Unity 6, C#, existing programmatic segment spawning, retained uGUI/TMP, existing `BossPatternFxPool`, PowerShell contracts, Unity Roslyn compile fallback.

---

## File structure

**Create**

- `tools/VerifyRunDepthPhase2.ps1` — phase-two static gate.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventModels.cs` — event kind, risk, outcome, and immutable snapshot.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventCatalog.cs` — six-theme event pools.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs` — deterministic scheduling, choice, completion, and reward commands.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeModels.cs` — capped in-run upgrade definitions.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeCatalog.cs` — movement, collection, defense, and companion pools.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeState.cs` — offered/selected stacks and numeric query API.
- `Assets/Scripts/Runtime/Game/UI/RunUpgradeChoiceOverlay.cs` — three-choice retained uGUI overlay.
- `Assets/Scripts/Runtime/PenguinRun/Systems/SkillScoreTracker.cs` — perfect dodge, chain, air-collection, no-hit, and route bonuses.
- `Assets/Scripts/Runtime/PenguinRun/Systems/BossThemeTraitCatalog.cs` — theme-specific phase trait and reward data.

**Modify**

- `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs` — orchestrate modules and expose snapshots.
- `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs` — spawn route choice markers/reward lanes only.
- `Assets/Scripts/Runtime/PenguinRun/Systems/CollisionSystem.cs` — emit existing outcomes to skill tracker; no hit-rule change.
- `Assets/Scripts/Runtime/PenguinRun/Systems/PickupSystem.cs` — apply queried upgrade multipliers and skill events.
- `Assets/Scripts/Runtime/PenguinRun/Systems/WorldDirector.cs` — accept explicit skill/route score awards.
- `Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs` — consume theme traits without changing `CheckPatternHit`.
- `Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs` — visual phase cue hooks only.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs` — route, upgrade, and skill snapshot display.
- `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs` — show upgrade overlay and pass final metrics.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerRunResult.cs` and `Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs` — carry session metrics to results.
- `Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs` — show route/skill/build highlights.
- `docs/CURRENT_DESIGN.md` — record delivered phase and validation status.

---

### Task 1: Lock phase-two boundaries

**Files:**

- Create: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Create a contract helper identical in failure aggregation style to `VerifyStorybookUiPhase1.ps1`.
- [ ] Require the four module files and these APIs: `RouteEventDirector.Tick`, `RouteEventDirector.Choose`, `RunUpgradeState.BuildOffer`, `RunUpgradeState.Select`, `SkillScoreTracker.Record`, `BossThemeTraitCatalog.For`.
- [ ] Reject changes that remove `BossSystem.CheckPatternHit`, `RunnerHitResolver.IsBossPatternHit`, or `IsPatternDangerous`.
- [ ] Require `BossPatternFxPool` capacity 48 and `MaxShardBurstCount = 6`.
- [ ] Run the gate and observe missing-module failures.
- [ ] Commit only the failing gate with `git commit -m "test: lock run-depth phase boundaries"`.

### Task 2: Add deterministic route events

**Files:**

- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventModels.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventCatalog.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/RouteEventDirector.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add failing contracts for six themes and the event kinds `RiskFork`, `CompanionRescue`, `RelicCache`, `WeatherTrial`, `BossChase`, and `SafeExit`.
- [ ] Define the immutable snapshot:

```csharp
internal readonly struct RouteEventSnapshot
{
    public readonly string Id;
    public readonly RouteEventKind Kind;
    public readonly string Title;
    public readonly string SafeLabel;
    public readonly string RiskLabel;
    public readonly float Progress01;
    public readonly bool AwaitingChoice;
    public RouteEventSnapshot(string id, RouteEventKind kind, string title,
        string safeLabel, string riskLabel, float progress01, bool awaitingChoice)
    {
        Id = id ?? string.Empty;
        Kind = kind;
        Title = title ?? string.Empty;
        SafeLabel = safeLabel ?? string.Empty;
        RiskLabel = riskLabel ?? string.Empty;
        Progress01 = UnityEngine.Mathf.Clamp01(progress01);
        AwaitingChoice = awaitingChoice;
    }
}
```

- [ ] Seed the director from session theme, daily flag, and run count; schedule no event before 220 m and keep at least 280 m between events.
- [ ] Ensure each event has an explicit safe result, risky result, score/fish reward, and segment pattern.
- [ ] Run the phase gate and commit the three new files plus contract updates.

### Task 3: Spawn readable route choices

**Files:**

- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add a failing contract for `SpawnRouteEvent`, `TryConsumeRouteChoice`, and `ApplyRouteEventOutcome`.
- [ ] Let the controller call `RouteEventDirector.Tick(distance)` and ask the spawner for a route event only when no Boss/prelude/theme transition is active.
- [ ] Spawn safe/risky lane markers using existing collider-free marker primitives; only existing obstacles retain colliders.
- [ ] Resolve a choice from player lane after the readable decision line, then award the selected outcome through explicit world commands.
- [ ] Preserve normal segment scheduling after completion or expiry.
- [ ] Run both P0/storybook gates, the phase gate, and Roslyn compilation; commit.

### Task 4: Add capped in-run upgrades

**Files:**

- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeModels.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeCatalog.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeState.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add failing contracts for categories `Movement`, `Collection`, `Defense`, `Companion`, maximum six selections, and maximum three stacks per upgrade.
- [ ] Define upgrade IDs and effects as data; do not store delegates or mutate save data.
- [ ] Implement deterministic three-choice offers that exclude maxed upgrades and avoid duplicate IDs.
- [ ] Expose numeric queries such as `DashDurationMultiplier`, `MagnetRadiusMultiplier`, `PerfectDodgeScoreMultiplier`, and `ShieldGraceSeconds`.
- [ ] Reset the entire state on `RunnerGameController.ResetRun`.
- [ ] Run the phase gate and Roslyn compile; commit.

### Task 5: Add the three-choice overlay

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/RunUpgradeChoiceOverlay.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add failing contracts for exactly three cards, explicit category/description/stack text, and one selection callback.
- [ ] Trigger offers after the first completed route event and then every second completed event, up to the six-selection cap.
- [ ] Freeze simulation through the existing pause/FSM route while the overlay is active; keep UI time unscaled.
- [ ] On selection, call `RunUpgradeState.Select`, close the overlay, restore the prior state, and show one short HUD feedback.
- [ ] Add a safe auto-select fallback after 12 seconds for accessibility and unattended test runs.
- [ ] Run gates and Roslyn compile; commit.

### Task 6: Add skill scoring

**Files:**

- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/SkillScoreTracker.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/CollisionSystem.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/PickupSystem.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/WorldDirector.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add failing contracts for `PerfectDodge`, `LaneChain`, `AirCollection`, `NoHitSegment`, `RiskRoute`, and `BossCounter`.
- [ ] Implement `Record(SkillAction action, int baseScore)` with a 2.5-second chain window, multiplier cap 3.0, and explicit `BreakChain` on damage.
- [ ] Feed real existing collision/pickup/segment events into the tracker; do not infer events by polling transforms.
- [ ] Award score through `WorldDirector.AddSkillScore(int)` and expose a snapshot containing total, chain, multiplier, and last label.
- [ ] Run gates and Roslyn compile; commit.

### Task 7: Surface route/build/skill state in HUD

**Files:**

- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add failing contracts for `SetRouteEvent`, `SetUpgradeSummary`, and `SetSkillSnapshot`.
- [ ] Show route choices only above the playable corridor and hide them immediately after resolution.
- [ ] Show at most three compact upgrade icons/labels plus a `+N` overflow count.
- [ ] Emphasize skill chain only on tier change or when less than 0.6 seconds remain.
- [ ] Boss mode remains dominant and suppresses route/build detail until the fight ends.
- [ ] Run gates and Roslyn compile; commit.

### Task 8: Add six theme-specific Boss traits

**Files:**

- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/BossThemeTraitCatalog.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/BossPatternVfxPlayer.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add failing contracts for one trait per theme: ice fracture, cedar root guard, aurora polarity, mist decoy, coral rebound, storm draft.
- [ ] Apply traits only at existing phase transitions and strike callbacks.
- [ ] Keep the only damage path as `CheckPatternHit -> IsBossPatternHit -> IsPatternDangerous`.
- [ ] Keep mist decoys visual-only, all strike VFX pool-rented, pool capacity 48, and shard burst cap six.
- [ ] Award one explicit themed relic/result flag after defeat.
- [ ] Run all gates and Roslyn compile; commit.

### Task 9: Carry run-depth metrics into results

**Files:**

- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerRunResult.cs`
- Modify: `Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Add failing contracts for route events completed, risky routes completed, skill score, peak skill chain, selected upgrade IDs, and themed relic.
- [ ] Populate fields from module snapshots in `FinishRun`; keep all collections copied/immutable after finish.
- [ ] Include the strongest skill/build/route highlight in the journey result review text.
- [ ] Do not add permanent progression behavior in phase two; phase three consumes the result fields later.
- [ ] Run gates and Roslyn compile; commit.

### Task 10: Verify and document phase two

**Files:**

- Modify: `docs/CURRENT_DESIGN.md`
- Modify: `docs/GAME_OPTIMIZATION_PLAN.md`
- Modify: `tools/VerifyRunDepthPhase2.ps1`

- [ ] Run `VerifyUiArtP0.ps1`, `VerifyStorybookUiPhase1.ps1`, and `VerifyRunDepthPhase2.ps1`.
- [ ] Parse `Packages/manifest.json`, run `git diff --check`, validate all runtime asset `.meta` files, and compile `Assembly-CSharp` with Unity Roslyn.
- [ ] Attempt Unity batch compile; if license return code 198 repeats, record it as runtime-blocked rather than passed.
- [ ] When licensed, smoke-test safe/risk routes, two upgrade offers, skill-chain break, all six Boss traits, death/retry, and result highlights.
- [ ] Update docs with actual evidence and commit `feat: complete run-depth phase`.

## Completion criteria

- A run contains readable, spaced route decisions without disrupting Boss/theme transitions.
- Upgrade selection is deterministic, capped, reset per run, and never writes save data.
- Skill score comes from real gameplay events and breaks on damage.
- Six Bosses have distinct traits without altering the established damage authority.
- HUD and result pages expose the new decisions without blocking the playable corridor.
- All three static gates and Unity Roslyn compilation pass; Editor/runtime status is reported separately.
