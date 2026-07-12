# Map and Item Visual Mobile Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make each runner theme and power-up visually distinct while preserving gameplay collision rules and mobile rendering budgets.

**Architecture:** A theme presentation profile will centralize colors, ground texture selection, landmark recipes, and pickup accents. `PenguinRunnerGame` consumes it only when rebuilding scenery; `SegmentSpawner` keeps all collision and `PowerUpKind` behavior unchanged.

**Tech Stack:** Unity 6, C#, Built-in Render Pipeline, low-poly procedural meshes, pooled `ParticleSystem`, static PowerShell contracts.

---

### Task 1: Theme presentation profile

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemePresentationProfile.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Test: `tools/VerifyUiArtP0.ps1`

- [ ] Add a failing static contract requiring profiles for all six `RunnerMapTheme` values and two landmark identifiers per profile.
- [ ] Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1`; expect failure for `ThemePresentationProfile`.
- [ ] Implement `ThemePresentationProfile.For(RunnerMapTheme)` returning ground tint, horizon/zenith/accent colors, and two landmark kinds.
- [ ] Replace theme-specific scenery switches with profile consumption while leaving `ObstacleColliderSpec` and `ApplyViewCorridorFade` untouched.
- [ ] Re-run the static contract; expect pass.

### Task 2: Low-poly landmark recipes

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Test: `tools/VerifyUiArtP0.ps1`

- [ ] Add a failing contract requiring ice crystal, cedar ruins, magnetic crystal, fog stone, coral, and cloud-island landmark builders.
- [ ] Implement each landmark as a low-poly mesh or grouped primitive outside `SceneryMinAbsX`.
- [ ] Register every landmark through `RegisterSceneryProp` so corridor fading applies.
- [ ] Re-run the contract and inspect a SkyFlight 10-second three-lane capture in Unity.

### Task 3: Power-up visual layer

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/PowerUpVisualProfile.cs`
- Test: `tools/VerifyUiArtP0.ps1`

- [ ] Add a failing contract requiring every `PowerUpKind` to resolve a core color, halo color, and pulse scale.
- [ ] Implement `PowerUpVisualProfile.For(PowerUpKind, RunnerMapTheme)` without changing pickup colliders or effects.
- [ ] Apply the profile only to the existing visual child renderers; use shared materials and `MaterialPropertyBlock` for tinting.
- [ ] Re-run the contract; manually verify dash, shield, magnet, and each theme-exclusive pickup are readable from all three lanes.

### Task 4: Mobile quality gates

**Files:**
- Modify: `ProjectSettings/QualitySettings.asset`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Test: `tools/VerifyUiArtP0.ps1`

- [ ] Add a failing contract requiring Android Medium to cap particle budgets and avoid high-intensity Bloom.
- [ ] Add a runtime quality helper that applies the Android Medium particle limit without changing desktop quality levels.
- [ ] Keep target frame rate at 60 while focused and 15 while paused.
- [ ] Run static contracts, `git diff --check`, then Unity batch compilation and Android-device profiling once the local Unity license is activated.
