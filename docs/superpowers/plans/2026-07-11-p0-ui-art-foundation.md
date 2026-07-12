# P0 UI and Art Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the P0 visual foundation without changing runner gameplay rules or the 16:9 playable-view corridor.

**Architecture:** Add `UiTheme` as the single source of visual tokens and let `UiBuilder` consume it. Add a procedural gradient skybox with theme-driven colors, and move primitive material creation to a shared cache while applying per-renderer color/alpha via `MaterialPropertyBlock`.

**Tech Stack:** Unity 6 Built-in Render Pipeline, uGUI, C#, ShaderLab, PowerShell static contract gate.

---

### Task 1: Lock P0 contracts

**Files:**
- Create: `tools/VerifyUiArtP0.ps1`

- [ ] Write the static contract gate for design tokens, skybox registration, and material caching.
- [ ] Run `powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1`; it must initially fail because P0 types do not exist.

### Task 2: Centralize design tokens

**Files:**
- Create: `Assets/Scripts/Runtime/Game/UI/UiTheme.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/UiBuilder.cs`

- [ ] Add the specified color, typography, spacing, radius, and duration tokens to `UiTheme`.
- [ ] Add the stable `com.unity.textmeshpro@3.0.6` package dependency before migrating individual UI surfaces.
- [ ] Replace `UiBuilder` design literals with `UiTheme` references while retaining its public API.
- [ ] Run the P0 contract gate and fix all token-related failures.

### Task 3: Upgrade sky and distant silhouettes

**Files:**
- Create: `Assets/Shaders/GradientSkybox.shader`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`

- [ ] Add a horizon-to-zenith procedural skybox shader.
- [ ] Select colors by `RunnerMapTheme` and apply them without touching camera framing or corridor bounds.
- [ ] Replace rotated-cube ridges with low-poly silhouette meshes outside the corridor.

### Task 4: Cache primitive materials

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerVisuals.cs`

- [ ] Cache materials by shader/color/transparency and return shared instances from `CreateMaterial`.
- [ ] Use `MaterialPropertyBlock` for renderer color/alpha updates; never mutate a cached material to fade an individual renderer.
- [ ] Run the P0 contract gate and inspect call sites that apply alpha.

### Task 5: Verify

**Files:**
- Modify: `tools/VerifyUiArtP0.ps1` only if a new stable contract is needed.

- [ ] Run the P0 contract gate.
- [ ] Run Unity batch-mode compilation if the Unity editor executable is available; otherwise report that editor compilation needs to be run locally.
- [ ] Review `git diff --check` and the scoped diff before handoff.
