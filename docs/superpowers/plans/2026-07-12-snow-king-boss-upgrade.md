# Snow King Boss Upgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade Snow King into the reference Boss with a stronger modular silhouette, readable intro and strike feedback, without changing its collision geometry.

**Architecture:** `BossEncounter` remains the only authority over attack phase and `BossSystem.CheckPatternHit` continues to use `RunnerHitResolver`. New Snow King visual modules attach only below the existing boss body; strike VFX remains rented from `BossPatternFxPool`.

**Tech Stack:** Unity 6, C#, procedural low-poly primitives, `BossPatternFxPool`, PowerShell static contracts.

---

### Task 1: Lock strike-to-hit alignment

**Files:**
- Modify: `tools/VerifyUiArtP0.ps1`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs`
- Test: `tools/VerifyUiArtP0.ps1`

- [ ] Add a failing contract requiring Snow King strike feedback to originate from `OnPatternStrikeMoment` and preserve `IsPatternDangerous` as the hit gate.
- [ ] Run the contract and observe the expected failure.
- [ ] Add an explicit Snow King strike pulse through the existing callback; do not add colliders or modify `RunnerHitResolver`.
- [ ] Re-run the contract and verify it passes.

### Task 2: Upgrade Snow King silhouette and intro

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs`
- Test: `tools/VerifyUiArtP0.ps1`

- [ ] Add a failing contract for crown, ice-core and shoulder-ice Snow King modules.
- [ ] Build the modules in `BuildSnowKing` using shared materials and only existing primitive helpers.
- [ ] Use the existing spawning transform to animate the assembled body from distant silhouette to active position.
- [ ] Re-run contracts and verify no new object is created outside the boss root.

### Task 3: Verify pool and mobile budgets

**Files:**
- Modify: `tools/VerifyUiArtP0.ps1`
- Test: `tools/VerifyUiArtP0.ps1`

- [ ] Add contracts that preserve `BossPatternFxPool` capacity 48 and require the Snow King strike to rent, not destroy, FX.
- [ ] Run static verification and `git diff --check`.
- [ ] Run Unity compilation plus a Snow King Android Medium playthrough when the local Unity license is active.
