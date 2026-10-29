# Theme Skill Expedition Implementation Plan

> **For agentic workers:** Use `executing-plans` for integration and `subagent-driven-development` for bounded implementation/review. Follow `test-driven-development`; checkbox steps record actual evidence.

**Goal:** Implement the approved ice-fracture jump route and cedar-rescue slide route without changing route rewards, onboarding, input, collision semantics, or the other themes.

**Architecture:** A deterministic, scene-independent catalog returns bounded obstacle/fish placements. A SegmentSpawner partial owns route entities and a distance reservation; the existing controller arbitrates Boss, mechanics, and cancellation. Existing collision/pickup/score systems remain authoritative.

**Tech Stack:** Unity 6 C#, PowerShell 7 standalone production-code regressions, bundled Roslyn compile gate.

**Workspace:** Continue in `codex/input-pause-stability` with the approved dirty baseline. Do not stash, discard, move to a clean worktree, or commit pre-existing edits. No push or PR. Prioritize implementation and verification; leave overlapping edits uncommitted for review.

## Task 1 — Deterministic route catalog

Files: create `Assets/Scripts/Runtime/PenguinRun/Systems/ThemeRouteEncounterCatalog.cs` and its `.meta`; create `tools/TestThemeRouteEncounters.ps1`, `tools/tests/ThemeRouteEncounterBoundary.cs`, `tools/tests/ThemeRouteEncounterRegression.cs`.

- [x] Add a test harness against the real catalog, route models/director/catalog and enum sources. Use minimal math-only Unity boundary stubs. A missing catalog must report a named failing requirement before compilation.
- [x] Run the standalone harness and require an explicit missing-feature FAIL before implementing.
- [x] Implement this API, with immutable read-only collections and no scene objects or reward writes:

```csharp
internal static bool TryCreate(RunnerMapTheme theme, string eventId, bool risky,
    RouteSegmentPattern pattern, float minimumBeatSpacing, out ThemeRouteEncounterPlan plan)
```

`ThemeRouteEncounterPlan` exposes `Obstacles`, `Fish`, `Length`; an obstacle placement exposes `OffsetZ`, `Lane`, `Slide`; a fish placement exposes `OffsetZ`, `Lane`, `BonusY`. Only the exact theme/event/pattern combination matches. Ice risk uses JumpArc, cedar risk uses SlideTunnel, both safe choices use CalmLane. Unknown combinations and invalid/non-finite spacing return false with null plan.

The safe plan is 24m long, contains no obstacles and six left-lane fish at `2f + i * 1.85f`, with zero bonus height. Risk plans have three right-lane obstacles at `12f + i * max(21f, minimumBeatSpacing)` and 12m exit clearance. Ice has six right-lane fish (two per jump, 2.4m before/after each obstacle with 1.15m bonus height); cedar risk has zero fish, preserving the existing physical pickup count. `Slide` is false for ice and true for cedar.

- [x] Test exact theme/event/pattern matching, six safe/ice fish versus zero cedar risk fish, three registered-spec-compatible opportunities, entry/exit clearance, two escape lanes, speed-dependent spacing, determinism, invalid inputs, once-only director resolution, reset and onboarding suppression. Example assertion:

```csharp
if (!ThemeRouteEncounterCatalog.TryCreate(RunnerMapTheme.IceLakeEcho,
    "ice-fracture", true, RouteSegmentPattern.JumpArc, 48f, out var plan)
    || plan.Obstacles.Count != 3 || plan.Fish.Count != 6)
    failures.Add("ice route must have three jump obstacles and six fish");
```

- [x] Re-run standalone tests and complete a local spec-compliance/code-quality review.
- [ ] Independent agent review: unavailable; the bounded agent failed before writing because this client rejected its context format. No independent review is claimed.

## Task 2 — Spawner ownership and world-authority integration

Files: create `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.RouteEncounters.cs` and `.meta`; modify `SegmentSpawner.cs`, `RunnerGameController.cs`; create controller route partial if needed to keep the ratchet; add `tools/VerifyThemeRouteEncounters.ps1`.

- [x] Write and run a failing source-wiring gate for the production integration before editing it. Require route spawning through the catalog, ownership cleanup, reservation-driven wave suppression and mechanic exclusion.
- [x] Extract existing route marker/outcome methods into the spawner partial; preserve the legacy-outcome helper for non-target events. Target markers use a bounded approach distance inside the existing decision window. Pass the full resolution and a timing budget from the controller:

```csharp
var maxSpeed = tuning.world.maxSpeed * Mathf.Max(1f, tuning.world.dashSpeedMultiplier);
var flight = 2f * tuning.movement.jumpVelocity / tuning.movement.gravity;
if (world.GlideTimer > 0f || spawner.HasPendingGlidePickup)
    flight *= tuning.powerUp.glideJumpMultiplier / tuning.powerUp.glideGravityMultiplier;
var beatSpacing = Mathf.Max(tuning.world.spawnSpacingMax, maxSpeed * (flight + 0.2f));
```

For slide routes use at least `slideDuration + 0.2f` rather than the jump flight budget. Respect actual tuning field spellings found in `Systems/RunnerTuning.cs`. Never change tuning to make a test pass.

- [x] Compute spawn start beyond both the existing safe spawn distance and the farthest registered obstacle/pickup/gate plus an entrance margin. Do not delete normal content to fit a route. Spawn real obstacles through SpawnStandardObstacle and fish through SpawnCoin; record only newly registered entity references for route cancellation.
- [x] Maintain RouteEncounterActive until the last reserved point has passed the despawn line. During reservation, do not consume ordinary waves or schedule theme mechanics. Keep route presentation until its reserved tail exits. Move distance once per active simulation tick; pause/tutorial/zero-dt must not spawn or advance.
- [x] Cancel route markers, route-owned obstacles/fish and reservation on Boss approach/prelude/active, theme change, death, reset, disposal/finish. Add director Cancel(distance) without rewards/counters. Never use Expire for cancellation. Preserve original choice rewards/upgrade timing and unchanged event spacing/seed.
- [x] Test pure cancellation/reservation behavior and production lifecycle source hooks; run standalone tests and full Roslyn compile. Add a real-spawner Play Mode validator, with execution pending the Unity license.

## Task 3 — Presentation and bounded scenery

Files: modify `Systems/RunnerHudCanvas.cs`, route presentation partial and existing theme scenery builder only as necessary.

- [x] Extend the source-wiring gate to fail unless targeted prompts say 跳跃线 / 滑铲线 / 稳健补给 while preserving event title, directional lane selection, and unrelated event wording.
- [x] Add a pure presentation helper and keep the selection window authoritative. Separate life from the routine-stat root so Boss mode retains life, pause, DashReady and warnings. No new input gestures or central dash countdown. Visual acceptance remains pending.
- [x] Use existing theme assets for one optional route-side landmark, with its complete rotating footprint outside the 8.5m corridor, following curvature/terrain. Missing assets skip decoration; no downloads, generation, colliders or per-frame material clones.
- [x] Add source regressions for boundaries and check unchanged camera/FOV, first-run protection, physical rewards, feedback authority and quality-tier hazard visibility.

## Task 4 — Verification and delivery

- [x] Run each test in a fresh pwsh process (Add-Type harnesses intentionally reuse production type names). Run every static gate and record the inherited file-size failure below:

```powershell
pwsh -NoProfile -File tools/TestThemeRouteEncounters.ps1
pwsh -NoProfile -File tools/TestGameplayFairness.ps1
pwsh -NoProfile -File tools/TestRunnerInput.ps1
pwsh -NoProfile -File tools/CompileFullRoslyn.ps1
Get-ChildItem tools/Verify*.ps1 | ForEach-Object { & pwsh -NoProfile -File $_.FullName; if ($LASTEXITCODE) { throw $_.Name } }
git diff --check
```

- [x] Lower the spawner/controller file-size ceilings after extraction, pair new Unity sources with .meta, and update docs/VERIFICATION.md with measured results and limitations.
- [x] Check Unity availability without changing licensing or saves. The attempted batch validation exited 198 before tests loaded.
- [ ] Execute the new real-spawner Play Mode validator and UI/Android acceptance after the Unity license/environment is available. Require fresh visual/device evidence; no runtime pass is claimed.
- [x] Review the scoped diff against the approved spec; fix actionable findings and rerun affected tests. Leave the dirty workspace uncommitted and unstaged.

## Progress / evidence

- Baseline gameplay fairness: 21 scenarios passed. Input test must use a fresh process because both existing harnesses declare the same C# boundary types.
- RED evidence: the missing catalog failed before implementation; the initial integration gate reported 12 missing hookups. Later presentation, pending-glide/disposal, Boss-prelude and landmark-footprint assertions also failed before their fixes.
- Final local run: 88 theme-route assertions, 21 gameplay-fairness scenarios and 18 input scenarios passed; full Roslyn compilation passed for 199 source files. Static gates: 21/22 passed.
- The only static failure is unchanged, committed BossEncounter.cs: 1,947 lines against its existing 1,900 ceiling (47 over). This task neither edits that file nor raises its ceiling. Extracted SegmentSpawner.cs is 2,259/2,260; RunnerGameController.cs is 1,150/1,150.
- Unity batch validation returned 198: no valid Editor license. Log: _tmp/theme-route-unity.log. The new editor validator compiles but has not run; no new screenshots or Android performance evidence exist.
- Local review fixed the controller namespace import, extraction-aware legacy source gate, pending-glide timing, disposal cleanup, Boss-prelude mechanic exclusion and landmark footprint clearance. Independent-agent review could not run; no commit, push or PR was created.
