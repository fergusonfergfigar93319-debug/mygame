# Camp Upgrade Duration Alignment Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task in the current chat. Preserve the existing dirty feature checkout. Do not create branches, commit unrelated files, or dispatch subagents.

**Goal:** Align camp upgrade previews with default gameplay durations without changing gameplay or save data.

**Architecture:** `RunnerTuningConfig.Default()` remains the default parameter source. `CampUpgrades` reads it for previews and existing duration getters. `CampShopPanel` gives multi-effect previews sufficient vertical space. Independent tests compile the real config, runtime mapping, camp upgrade and save classes with only Unity/storage and unrelated history boundaries replaced.

**Tech Stack:** Unity 6000.4.5f1, C#, PowerShell 7, bundled Roslyn.

---

## Files and responsibilities

- `Assets/Scripts/Runtime/Game/Save/CampUpgrades.cs`: preview formulas, descriptions and three existing duration getters; no persistence or balance changes.
- `Assets/Scripts/Runtime/Game/UI/CampShopPanel.cs`: increase multi-line preview height and row height together; keep bottom spacing and purchase controls.
- `tools/tests/CampUpgradePreviewBoundary.cs`: in-memory PlayerPrefs, Unity attributes/math/color, fail-fast unrelated RunRecords boundary.
- `tools/tests/CampUpgradePreviewRegression.cs`: level 0–5, clamping, descriptions, getters, no save writes, and unchanged price/reward assertions.
- `tools/TestCampUpgradePreview.ps1`: compile and execute the independent tests in a fresh process; optional alternate config constants in memory.
- `tools/VerifyCampUpgradePreview.ps1`: fail if a shipped scene overrides the previewed config fields, or the UI loses its multi-line layout hooks.
- `docs/CURRENT_DESIGN.md`, `docs/GDD.md`, `docs/VERIFICATION.md`: record the new default-config preview contract and verification boundary.

### Task 1: Reproduce the mismatch

- [x] Add the independent boundary, regression suite and runner. Assert preview numbers against `RunnerTuning.FromConfig(RunnerTuningConfig.Default()).powerUp`, not a copied gameplay formula constant.

```csharp
var runtime = RunnerTuning.FromConfig(RunnerTuningConfig.Default()).powerUp;
var actual = CampUpgrades.FormatEffectAt(CampUpgradeKind.Dash, level);
var expected = runtime.dashBaseSeconds + level * runtime.dashPerLevelSeconds;
Check(actual.Contains(expected.ToString("0.##") + " 秒"), "dash preview drift");
```

- [x] Run `pwsh -NoProfile -File tools/TestCampUpgradePreview.ps1`. Before the fix, 54 behavior assertions failed; the suite compiled and failed for the intended formula/text mismatches.

### Task 2: Align previews and getters

- [x] Read one cached, private default config in `CampUpgrades`. Replace the three stale getter formulas with the same source used by the preview.

```csharp
private static readonly RunnerTuningConfig PreviewTuning = RunnerTuningConfig.Default();
public static float GetFishDashDurationSeconds() => PreviewTuning.DashBaseSeconds + GetLevel(CampUpgradeKind.Dash) * PreviewTuning.DashPerLevelSeconds;
public static float GetTuanAssistDurationSeconds() => PreviewTuning.ShieldBaseSeconds + GetLevel(CampUpgradeKind.Tuan) * PreviewTuning.ShieldPerTuanLevelSeconds;
public static float GetMagnetDurationSeconds() => PreviewTuning.MagnetBaseSeconds + GetLevel(CampUpgradeKind.Magnet) * PreviewTuning.MagnetPerLevelSeconds;
```

- [x] In `FormatEffectAt`, retain the existing 0–5 clamp; format base + level × increment using the matching fields. Show dash/magnet as `基础 {seconds:0.##} 秒`; show Tuan as two lines `基础护盾 {seconds:0.##} 秒` / `滑翔 {seconds:0.##} 秒`; show Polar as three lines `道具率 +{points:0.#} 点` / `基础星光 {seconds:0.##} 秒` / `双倍 {seconds:0.##} / 沙漏 {seconds:0.##} 秒`. Keep fish/score expressions unchanged.
- [x] Describe shield expiry vs one-hit break correctly; name glide as Tuan's second effect. Clarify that preview excludes run-local combo/upgrade multipliers. Name Polar's three affected durations.
- [x] Grow multi-line rows while preserving bottom clearance:

```csharp
var previewHeight = kind == CampUpgradeKind.Polar ? 90f : kind == CampUpgradeKind.Tuan ? 64f : 46f;
// row height: 240f + previewHeight - 46f
// preview size: new Vector2(0f, previewHeight)
// preview position: new Vector2(98f, 58f + (previewHeight - 46f) * 0.5f)
```

- [x] Run the independent suite for both normal and in-memory alternate defaults. All 74 scenarios pass in each run; alternate values are changed in memory only.

### Task 3: Guard, document and verify

- [x] Add a static gate that strips comments before checking source contracts, verifies UI layout hooks and rejects duration/probability overrides in shipped runner scene instances. This gate prevents future Inspector changes from silently invalidating the default-config preview.
- [x] Update the design baseline and GDD with actual default formulas and label the UI values as base durations, not maximum protection guarantees.
- [x] Run `pwsh -NoProfile -File tools/CompileFullRoslyn.ps1`, five existing `Test*.ps1` suites, the new suite, and every `Verify*.ps1` script. Roslyn and test suites passed; 24/25 static scripts passed with the inherited BossEncounter line-count failure.
- [x] Run `git diff --check` and inspect this batch's diffs. Record RED/GREEN evidence and unavailable Unity/Android validation in `docs/VERIFICATION.md`.
- [x] Leave all changes uncommitted in the existing feature checkout for review; no merge, push or cleanup is part of this request.
