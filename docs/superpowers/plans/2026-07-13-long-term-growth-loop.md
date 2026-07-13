# Long-Term Growth Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Turn completed journeys into a coherent long-term loop spanning five camp rebuild nodes, companion bonds, themed codex discoveries, unified goals, and one contextual lobby recommendation.

**Architecture:** Keep the existing `CampUpgrades`, `MissionStore`, and legacy story/boss codex intact. Add narrow stores over `PlayerPrefs`, front them with immutable snapshots/catalogs, and update all growth systems through one `GrowthTransaction` invoked by `RunOutcomeRouter`. Presentation reads snapshots only: journey results explain what advanced, the lobby selects one useful next action, and existing camp/codex surfaces gain rebuild and discovery sections.

**Tech Stack:** Unity 6, C# 9, retained uGUI/TMP, `PlayerPrefs`, PowerShell static verification, Unity Roslyn compiler fallback.

---

## File Structure

- Create `tools/VerifyLongTermGrowthPhase3.ps1` — structural acceptance gate for the full phase.
- Create `Assets/Scripts/Runtime/Game/Save/SaveSchema.cs` — explicit save version and non-destructive migrations.
- Create `Assets/Scripts/Runtime/Game/Growth/CampBuildingModels.cs` — five node identifiers and immutable state/definition models.
- Create `Assets/Scripts/Runtime/Game/Growth/CampBuildingCatalog.cs` — costs, prerequisites, story copy, and unlock effects.
- Create `Assets/Scripts/Runtime/Game/Growth/CampRebuildStore.cs` — persistence, affordability, purchases, and camp completion snapshots.
- Create `Assets/Scripts/Runtime/Game/Growth/CompanionBondModels.cs` — companion identifiers and bond snapshots.
- Create `Assets/Scripts/Runtime/Game/Growth/CompanionBondStore.cs` — bond XP/levels and modest helper unlock queries.
- Create `Assets/Scripts/Runtime/Game/Growth/CodexDiscoveryCatalog.cs` — route, relic, and growth discovery definitions with unknown clues.
- Create `Assets/Scripts/Runtime/Game/Growth/CodexDiscoveryStore.cs` — discovery persistence and new-item state.
- Create `Assets/Scripts/Runtime/Game/Growth/GrowthTransaction.cs` — one post-run input, one aggregate result, one flush boundary.
- Create `Assets/Scripts/Runtime/Game/Growth/GrowthRecommendation.cs` — one prioritized lobby action from claimable/affordable/new state.
- Modify `Assets/Scripts/Runtime/Game/Save/PlayerSave.cs` — expose safe persistence helpers needed by the new stores.
- Modify `Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs` — invoke the transaction and carry its result.
- Modify `Assets/Scripts/Runtime/Game/UI/CampPanel.cs` — add the five-node rebuilding section without replacing numeric upgrades.
- Modify `Assets/Scripts/Runtime/Game/UI/CodexPanel.cs` — group legacy story/boss and new route/relic discoveries.
- Modify `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs` — carry recommendation kind and bond/camp summary.
- Modify `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyBackground.cs` — show rebuilt camp silhouettes from a snapshot.
- Modify `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs` — surface bond level/progress and helper unlock.
- Modify `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs` — run migration, bind snapshots, and route one primary action.
- Modify `Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs` — explain bond/discovery/camp progress earned by the run.
- Modify `docs/CURRENT_DESIGN.md` and `docs/GAME_OPTIMIZATION_PLAN.md` — record the delivered loop and validation limits.

### Task 1: Lock the phase-three acceptance contract

**Files:**
- Create: `tools/VerifyLongTermGrowthPhase3.ps1`

**Step 1: Write the failing verification script**

Require exact source artifacts and integration tokens for: save schema versioning; five camp nodes; bond levels; route/relic discovery groups; `GrowthTransaction.Apply`; one `PlayerSave.Flush`; result feedback; lobby recommendation; camp/codex UI sections.

**Step 2: Run the gate to verify RED**

Run: `powershell -ExecutionPolicy Bypass -File tools/VerifyLongTermGrowthPhase3.ps1`

Expected: FAIL because phase-three source files do not exist.

**Step 3: Commit the RED contract**

```powershell
git add tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "test: lock long-term growth phase boundaries"
```

### Task 2: Add explicit save schema and migration

**Files:**
- Create: `Assets/Scripts/Runtime/Game/Save/SaveSchema.cs`
- Modify: `Assets/Scripts/Runtime/Game/Save/PlayerSave.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`

**Step 1: Tighten the gate**

Require `CurrentVersion`, `EnsureMigrated`, monotonic version writes, initialization before lobby reads, and no delete/reset calls.

**Step 2: Run RED**

Expected: missing schema/version tokens.

**Step 3: Implement the minimum migration**

Introduce version 3. Migrate existing players non-destructively: preserve legacy upgrades, derive initial bonfire progress from `VisitedCamp`, seed discovery state from legacy story/boss flags, then write the new version. Add public typed key/value helpers in `PlayerSave` so stores do not scatter raw preference access.

**Step 4: Run GREEN and compile**

Run the phase gate and the Unity Roslyn fallback compile. Expected: both pass.

**Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/Game/Save Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "feat: version long-term growth saves"
```

### Task 3: Build the five-node camp reconstruction model

**Files:**
- Create: `Assets/Scripts/Runtime/Game/Growth/CampBuildingModels.cs`
- Create: `Assets/Scripts/Runtime/Game/Growth/CampBuildingCatalog.cs`
- Create: `Assets/Scripts/Runtime/Game/Growth/CampRebuildStore.cs`

**Step 1: Tighten the gate**

Require `Bonfire`, `Workshop`, `CompanionHut`, `LibraryTent`, and `Lookout`; three visible states per node; max level 3; prerequisites; fish costs; affordability and purchase APIs.

**Step 2: Run RED**

Expected: missing camp growth model.

**Step 3: Implement deterministic catalog/store behavior**

Bonfire starts the chain; Workshop and Companion Hut branch from it; Library Tent follows the hut; Lookout closes the chain. Purchases subtract fish once, persist levels, increment lifetime investment, and expose the next best affordable node. Structural levels unlock story/bond/codex affordances and do not duplicate raw stat multipliers in `CampUpgrades`.

**Step 4: Run GREEN and compile**

Expected: gate and compiler pass.

**Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/Game/Growth tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "feat: add five-node camp rebuilding"
```

### Task 4: Present rebuilding in camp and lobby scenery

**Files:**
- Modify: `Assets/Scripts/Runtime/Game/UI/CampPanel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyBackground.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`

**Step 1: Tighten the gate**

Require a rebuild summary/header, node cards with locked/affordable/rebuilt states, purchases through `CampRebuildStore`, and five named non-interactive background layers whose visibility reflects node levels.

**Step 2: Run RED**

Expected: models exist but no presentation integration.

**Step 3: Implement focused UI changes**

Place reconstruction cards before the existing stat upgrade groups. Keep cards compact and storybook-styled; locked nodes explain prerequisites. Lobby background layers use existing primitive silhouettes and never intercept input.

**Step 4: Run GREEN and compile**

Expected: gate and compiler pass.

**Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/Game/UI tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "feat: visualize camp reconstruction"
```

### Task 5: Add companion bond progression

**Files:**
- Create: `Assets/Scripts/Runtime/Game/Growth/CompanionBondModels.cs`
- Create: `Assets/Scripts/Runtime/Game/Growth/CompanionBondStore.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs`

**Step 1: Tighten the gate**

Require TuanTuan and Dror, levels 1–5, XP thresholds, unlock-aware earning, a passive helper query, a conditional helper query, and lobby progress copy.

**Step 2: Run RED**

Expected: bond artifacts absent.

**Step 3: Implement bounded bond rewards**

Award XP from completed routes, risky choices, skill score, daily runs, and themed relics. Bond levels primarily unlock dialogue and helper labels. Passive/conditional helpers expose capped query values for later run integration; they must not create a second damage or reward authority.

**Step 4: Run GREEN and compile**

Expected: gate and compiler pass.

**Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/Game/Growth Assets/Scripts/Runtime/Game/UI/Lobby tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "feat: add companion bond progression"
```

### Task 6: Expand the codex into themed discoveries

**Files:**
- Create: `Assets/Scripts/Runtime/Game/Growth/CodexDiscoveryCatalog.cs`
- Create: `Assets/Scripts/Runtime/Game/Growth/CodexDiscoveryStore.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/CodexPanel.cs`

**Step 1: Tighten the gate**

Require grouped route and relic entries, stable identifiers, unknown clue text, discovered/new flags, and a mark-seen operation.

**Step 2: Run RED**

Expected: discovery catalog/store absent.

**Step 3: Implement and present discoveries**

Catalog all route event definitions by theme and all six boss relics. The UI keeps legacy story and boss entries, then adds route notes and relic pages. Unknown items show actionable clues rather than generic locks; opening the codex marks visible new entries seen.

**Step 4: Run GREEN and compile**

Expected: gate and compiler pass.

**Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/Game/Growth Assets/Scripts/Runtime/Game/UI/CodexPanel.cs tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "feat: add themed codex discoveries"
```

### Task 7: Unify post-run growth updates

**Files:**
- Create: `Assets/Scripts/Runtime/Game/Growth/GrowthTransaction.cs`
- Modify: `Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs`
- Modify: `Assets/Scripts/Runtime/Game/Mission/MissionStore.cs`

**Step 1: Tighten the gate**

Require immutable input/result snapshots, mission/codex/bond updates inside one transaction, no internal `PlayerPrefs.Save` from the run update path, and exactly one `PlayerSave.Flush` after records are written.

**Step 2: Run RED**

Expected: `RunOutcomeRouter` still updates stores independently.

**Step 3: Implement the transaction boundary**

Move mission, legacy codex, new discovery, and bond progression behind `GrowthTransaction.Apply`. Return gained bond XP, new discovery IDs, newly claimable goal count, and the strongest progression summary. Keep scoring, leaderboard submission, and run records unchanged.

**Step 4: Run GREEN and compile**

Expected: gate and compiler pass.

**Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/Game/Growth Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs Assets/Scripts/Runtime/Game/Mission/MissionStore.cs tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "feat: unify post-run growth transaction"
```

### Task 8: Surface one useful next action

**Files:**
- Create: `Assets/Scripts/Runtime/Game/Growth/GrowthRecommendation.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`

**Step 1: Tighten the gate**

Require priority `claimable goal > new discovery > affordable rebuild > affordable upgrade > next journey`, typed action routing, and journey result use of transaction summary.

**Step 2: Run RED**

Expected: lobby still selects only missions/upgrades/run and result has generic sync copy.

**Step 3: Implement recommendation and feedback**

Build one immutable recommendation. Route its action without string comparisons. Bind bond/camp state into the companion corner and background. Journey results explain the strongest permanent progression change from that run.

**Step 4: Run GREEN and compile**

Expected: gate and compiler pass.

**Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/Game/Growth Assets/Scripts/Runtime/Game/UI tools/VerifyLongTermGrowthPhase3.ps1
git commit -m "feat: connect growth feedback and recommendations"
```

### Task 9: Complete verification and project documentation

**Files:**
- Modify: `docs/CURRENT_DESIGN.md`
- Modify: `docs/GAME_OPTIMIZATION_PLAN.md`

**Step 1: Run the complete static suite**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyRunDepthPhase2.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyLongTermGrowthPhase3.ps1
```

Expected: all PASS.

**Step 2: Validate assets and source**

Parse `Packages/manifest.json`, run runtime `.meta` validation, `git diff --check`, and the Unity Roslyn fallback compile including all phase-two and phase-three source files.

Expected: all pass.

**Step 3: Attempt Unity batch verification**

Run Unity in batch mode against the project and capture the log. Expected when licensed: import/runtime checks pass. If the environment still has no valid editor license, document exit code 198 and do not claim runtime/Android validation.

**Step 4: Update documentation**

Record permanent/session data boundaries, transaction order, delivered camp/bond/codex/goal behavior, validation evidence, and the remaining Unity-device QA checklist.

**Step 5: Review against the approved design**

Search for placeholders (`TODO`, `NotImplementedException`), confirm every approved phase-three system has a visible surface and a persistence path, and confirm no new system bypasses the unified transaction.

**Step 6: Commit**

```powershell
git add docs tools Assets/Scripts/Runtime/Game Assets/Scripts/Runtime/PenguinRun
git commit -m "feat: complete long-term growth loop"
```

