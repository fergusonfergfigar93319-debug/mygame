# Theme Map Mechanics and Mastery Gameplay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build six readable, deterministic theme-map mechanics with controlled pacing, mastery/resonance gameplay, automatic journey contracts, Story/Endless/Daily mode policies, detailed dual-LOD apparatus assets, and verified Android delivery.

**Architecture:** Add a focused `ThemeMechanics` runtime package beside the existing segment, route, and Boss systems. A pure deterministic director schedules immutable catalog definitions; a lifecycle runtime exposes bounded movement modifiers and world commands; a dedicated spawner owns pooled visual apparatus; a mastery tracker produces session snapshots that the existing controller, HUD, result route, and growth transaction consume. Existing `ObstacleColliderSpec`, `RunnerHitResolver`, Boss danger windows, and three-lane authority stay unchanged.

**Tech Stack:** Unity 6.0.4, C# 9, built-in render pipeline, retained uGUI/TMP, `PlayerPrefs` through `PlayerSave` growth helpers, code-authored Unity prefabs with LODGroup, PowerShell contract tests, Unity batch-mode editor validation, Android Gradle build.

---

## Scope and file map

Create these focused runtime files under `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/`:

- `ThemeMechanicModels.cs`: enums and immutable definition/request/snapshot/modifier values.
- `DeterministicRunRandom.cs`: injected xorshift random source used by director and contracts.
- `ThemeMechanicCatalog.cs`: six theme definitions and three mode policies.
- `MapMechanicDirector.cs`: cooldown, history, pressure debt, reward debt, intensity, and fixed-seed selection.
- `ThemeMechanicRuntime.cs`: lifecycle state machine, cancellation, keypoint progress, and modifier cleanup.
- `ThemeMechanicPatternCatalog.cs`: concrete apparatus beats for all six normal and six signature events.
- `ThemeMechanicSpawner.cs`: pooled visual roots, telegraphs, apparatus beats, and signature setpieces.
- `ThemeMasteryModels.cs`: mastery, resonance, journey contract, and run-result snapshots.
- `JourneyContractCatalog.cs`: five automatic contract definitions and deterministic selection.
- `ThemeMasteryTracker.cs`: event result grading, resonance, contract progress, and result snapshot.

Create these persistence/UI/editor files:

- `Assets/Scripts/Runtime/Game/Growth/ThemeMasteryStore.cs`: versioned permanent per-theme records.
- `Assets/Scripts/Editor/PenguinRun/ThemeMechanicSculptureBuilder.cs`: twelve apparatus and six setpiece model recipes.
- `Assets/Scripts/Editor/PenguinRun/ThemeMechanicPrefabBuilder.cs`: dual-LOD prefab generation.
- `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`: deterministic, lifecycle, asset, and pool validation.
- `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicPreviewCapture.cs`: six visual preview captures with blank-image rejection.
- `tools/VerifyThemeMechanicsPhase4.ps1`: static phase-four contract gate.

Modify these integration files only at their existing responsibilities:

- `Assets/Scripts/Runtime/Game/RunSession.cs` and `RunnerSessionConfig.cs`: explicit Story/Endless/Daily mode and run seed.
- `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`: chapter CTA starts Story; dock run CTA remains Endless.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyChapterPage.cs`: Story callback naming only.
- `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`: construct, tick, cancel, and snapshot the new systems.
- `Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs`: consume bounded modifiers; retain lane authority.
- `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs`: expose narrow apparatus/obstacle hooks; no new state machine.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs`: event banner, resonance bar, and contract summaries.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerRunResult.cs`: immutable mastery snapshot.
- `Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs`, `Growth/GrowthTransaction.cs`, `UI/JourneyResultViewModel.cs`, and `UI/RunResultOverlay.cs`: propagate and display permanent progress.
- `Assets/Scripts/Editor/PenguinRun/RunnerSculptureBuilder.cs` and `PenguinVisualPrefabBuilder.cs`: expose shared primitive/material helpers without changing existing assets.
- `Assets/Editor/ProjectUiLayoutRegressionTest.cs`: add theme HUD assertions at three resolutions.
- `tools/BuildLatestAndroidApk.ps1`: include the new static gate before build.

### Task 1: Lock the phase-four models and deterministic random contract

**Files:**
- Create: `tools/VerifyThemeMechanicsPhase4.ps1`
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicModels.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/DeterministicRunRandom.cs`

- [ ] **Step 1: Write the failing static contract**

Create `tools/VerifyThemeMechanicsPhase4.ps1` with this initial content:

```powershell
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-Required([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }
    Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

$models = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicModels.cs'
$random = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/DeterministicRunRandom.cs'
foreach ($name in 'ThemeMechanicId','ThemeMechanicPhase','ThemeMechanicMode','ThemeMechanicIntensity','ThemeMechanicDefinition','ThemeMechanicModifiers','ThemeMechanicSnapshot') {
    Require $models "\b$name\b" $name
}
Require $random '\bIRunRandom\b' 'injected run random interface'
Require $random '\bXorShiftRunRandom\b' 'deterministic xorshift implementation'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}
Write-Output 'Theme mechanics phase-four static contracts passed.'
```

- [ ] **Step 2: Run the contract and verify it fails**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyThemeMechanicsPhase4.ps1
```

Expected: exit 1 with both runtime files reported missing.

- [ ] **Step 3: Add the complete shared model vocabulary**

Create `ThemeMechanicModels.cs` with these public/internal types and exact defaults:

```csharp
using UnityEngine;

namespace PenguinRun
{
    internal enum ThemeMechanicId { IceInertia, CedarCollapse, AuroraPolarity, MistBeacon, OceanTide, SkyWind }
    internal enum ThemeMechanicPhase { Idle, Telegraph, Active, Resolve, Reward, Cooldown, Cancelled }
    internal enum ThemeMechanicMode { Story, Endless, Daily }
    internal enum ThemeMechanicIntensity { Low, Medium, High, Signature }
    internal enum ThemeMechanicGrade { None, Completed, Perfect, Flawless }
    internal enum ThemeMechanicCancelReason { None, Boss, RouteEvent, ThemeTransition, Timeout, MissingCriticalAsset }

    internal readonly struct ThemeMechanicDefinition
    {
        public ThemeMechanicDefinition(ThemeMechanicId id, RunnerMapTheme theme, string displayName,
            string actionHint, string apparatusA, string apparatusB, string signature,
            float telegraphSeconds, float activeSeconds, float rewardSeconds, float baseWeight)
        {
            Id = id; Theme = theme; DisplayName = displayName ?? string.Empty;
            ActionHint = actionHint ?? string.Empty; ApparatusA = apparatusA ?? string.Empty;
            ApparatusB = apparatusB ?? string.Empty; Signature = signature ?? string.Empty;
            TelegraphSeconds = Mathf.Max(2f, telegraphSeconds);
            ActiveSeconds = Mathf.Clamp(activeSeconds, 6f, 12f);
            RewardSeconds = Mathf.Clamp(rewardSeconds, 3f, 6f);
            BaseWeight = Mathf.Max(0.01f, baseWeight);
        }
        public ThemeMechanicId Id { get; }
        public RunnerMapTheme Theme { get; }
        public string DisplayName { get; }
        public string ActionHint { get; }
        public string ApparatusA { get; }
        public string ApparatusB { get; }
        public string Signature { get; }
        public float TelegraphSeconds { get; }
        public float ActiveSeconds { get; }
        public float RewardSeconds { get; }
        public float BaseWeight { get; }
    }

    internal readonly struct ThemeMechanicModifiers
    {
        public ThemeMechanicModifiers(float laneResponse, float laneDrift, float gravity,
            float glideDuration, float visibility)
        {
            LaneResponseMultiplier = Mathf.Clamp(laneResponse, .55f, 1.35f);
            LaneDriftLanesPerSecond = Mathf.Clamp(laneDrift, -.32f, .32f);
            GravityMultiplier = Mathf.Clamp(gravity, .65f, 1.35f);
            GlideDurationMultiplier = Mathf.Clamp(glideDuration, 1f, 1.8f);
            Visibility01 = Mathf.Clamp(visibility, .35f, 1f);
        }
        public float LaneResponseMultiplier { get; }
        public float LaneDriftLanesPerSecond { get; }
        public float GravityMultiplier { get; }
        public float GlideDurationMultiplier { get; }
        public float Visibility01 { get; }
        public static ThemeMechanicModifiers Identity => new(1f, 0f, 1f, 1f, 1f);
    }

    internal readonly struct ThemeMechanicSnapshot
    {
        public ThemeMechanicSnapshot(ThemeMechanicId id, ThemeMechanicPhase phase,
            ThemeMechanicIntensity intensity, string title, string hint, float progress01,
            ThemeMechanicModifiers modifiers, bool visible)
        {
            Id = id; Phase = phase; Intensity = intensity; Title = title ?? string.Empty;
            Hint = hint ?? string.Empty; Progress01 = Mathf.Clamp01(progress01);
            Modifiers = modifiers; Visible = visible;
        }
        public ThemeMechanicId Id { get; }
        public ThemeMechanicPhase Phase { get; }
        public ThemeMechanicIntensity Intensity { get; }
        public string Title { get; }
        public string Hint { get; }
        public float Progress01 { get; }
        public ThemeMechanicModifiers Modifiers { get; }
        public bool Visible { get; }
        public static ThemeMechanicSnapshot Empty => new(default, ThemeMechanicPhase.Idle,
            ThemeMechanicIntensity.Low, string.Empty, string.Empty, 0f,
            ThemeMechanicModifiers.Identity, false);
    }
}
```

- [ ] **Step 4: Add deterministic random and verify the contract passes**

Create `DeterministicRunRandom.cs`:

```csharp
namespace PenguinRun
{
    internal interface IRunRandom { uint NextUInt(); float Next01(); int Range(int minInclusive, int maxExclusive); }

    internal sealed class XorShiftRunRandom : IRunRandom
    {
        private uint state;
        public XorShiftRunRandom(int seed) { state = (uint)(seed == 0 ? 0x6D2B79F5 : seed); }
        public uint NextUInt()
        {
            var x = state;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state = x == 0 ? 0x6D2B79F5u : x;
            return state;
        }
        public float Next01() => (NextUInt() & 0x00FFFFFFu) / 16777216f;
        public int Range(int minInclusive, int maxExclusive) => maxExclusive <= minInclusive
            ? minInclusive
            : minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
    }
}
```

Run the PowerShell gate again. Expected: `Theme mechanics phase-four static contracts passed.`

- [ ] **Step 5: Commit**

```powershell
git add tools/VerifyThemeMechanicsPhase4.ps1 Assets/Scripts/Runtime/PenguinRun/ThemeMechanics
git commit -m "test: lock theme mechanic model contracts"
```

### Task 2: Define the six theme mechanics and mode policies

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicCatalog.cs`
- Modify: `tools/VerifyThemeMechanicsPhase4.ps1`

- [ ] **Step 1: Extend the static gate first**

Add these assertions after the model checks:

```powershell
$catalog = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicCatalog.cs'
foreach ($id in 'IceInertia','CedarCollapse','AuroraPolarity','MistBeacon','OceanTide','SkyWind') {
    Require $catalog "ThemeMechanicId\.$id" "catalog definition $id"
}
Require $catalog '\bForTheme\s*\(' 'theme lookup'
Require $catalog '\bPolicyFor\s*\(' 'mode policy lookup'
```

Run the gate. Expected: fail because `ThemeMechanicCatalog.cs` is missing.

- [ ] **Step 2: Create exact catalog definitions**

Create `ThemeMechanicCatalog.cs` with one immutable definition per theme:

```csharp
using System;

namespace PenguinRun
{
    internal readonly struct ThemeMechanicModePolicy
    {
        public ThemeMechanicModePolicy(float firstDistance, int maxPerRun, int cooldownBeats,
            float baseChance, bool allowSignature)
        { FirstDistance = firstDistance; MaxPerRun = maxPerRun; CooldownBeats = cooldownBeats;
          BaseChance = baseChance; AllowSignature = allowSignature; }
        public float FirstDistance { get; }
        public int MaxPerRun { get; }
        public int CooldownBeats { get; }
        public float BaseChance { get; }
        public bool AllowSignature { get; }
    }

    internal static class ThemeMechanicCatalog
    {
        private static readonly ThemeMechanicDefinition[] All =
        {
            new(ThemeMechanicId.IceInertia, RunnerMapTheme.IceLakeEcho, "裂冰回响", "稳住换道，跃过扩散裂纹",
                "ice/cracked_plate", "ice/crystal_rebound_gate", "ice/floe_bridge", 2.4f, 8f, 4f, 1f),
            new(ThemeMechanicId.CedarCollapse, RunnerMapTheme.CedarRuins, "雪松坍塌", "跟紧缺口，穿过摆木",
                "cedar/swing_log", "cedar/root_gallery", "cedar/ruin_escape", 2.6f, 9f, 4f, 1f),
            new(ThemeMechanicId.AuroraPolarity, RunnerMapTheme.AuroraField, "极光换极", "识别蓝红极性，避开排斥线",
                "aurora/polarity_gate", "aurora/prism_beam", "aurora/magnetic_rebuild", 2.2f, 8f, 4f, 1f),
            new(ThemeMechanicId.MistBeacon, RunnerMapTheme.MistDike, "雾堤追光", "跟随真灯束，不追逐雾影",
                "mist/decoy_shadow", "mist/rotating_beacon", "mist/floodgate_chase", 2.8f, 10f, 4f, 1f),
            new(ThemeMechanicId.OceanTide, RunnerMapTheme.OceanReef, "潮汐回廊", "观察潮位，借气泡越过珊瑚",
                "ocean/coral_lift", "ocean/bubble_vault", "ocean/wave_tunnel", 2.5f, 9f, 4f, 1f),
            new(ThemeMechanicId.SkyWind, RunnerMapTheme.SkyFlight, "风场断岛", "借上升气流，修正横风偏移",
                "sky/wind_gate", "sky/cloud_platform", "sky/island_glide_chain", 2.4f, 10f, 4f, 1f),
        };

        public static ReadOnlySpan<ThemeMechanicDefinition> Definitions => All;
        public static ThemeMechanicDefinition ForTheme(RunnerMapTheme theme)
        {
            foreach (var definition in All) if (definition.Theme == theme) return definition;
            WarnMissingThemeOnce(theme);
            return All[0];
        }
        public static ThemeMechanicModePolicy PolicyFor(ThemeMechanicMode mode) => mode switch
        {
            ThemeMechanicMode.Story => new(150f, 3, 3, .42f, true),
            ThemeMechanicMode.Daily => new(120f, 8, 3, .58f, true),
            _ => new(100f, int.MaxValue, 3, .52f, true),
        };
    }
}
```

Back `WarnMissingThemeOnce` with a fixed-size set keyed by `RunnerMapTheme`; it logs at most once per missing theme per run. Add `TryForTheme(RunnerMapTheme, out ThemeMechanicDefinition)` for the director so an invalid catalog lookup skips the mechanic and leaves the normal segment active instead of silently scheduling Ice Lake.

- [ ] **Step 3: Add catalog validation to the Unity editor validator skeleton**

Create `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs` with `ValidateCatalog` checking exactly six unique themes, non-empty apparatus paths, telegraph >= 2, active in [6,12], and reward in [3,6]. Expose `RunNow(out string report)` and `ValidateFromCommandLine()`; return report `PASS: six theme mechanic definitions are valid.` when no failures exist.

- [ ] **Step 4: Run compilation and catalog validation**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -nographics -quit `
  -projectPath 'C:\Users\dxh53\Desktop\mygame' `
  -executeMethod PenguinRun.Editor.ProjectThemeMechanicValidation.ValidateFromCommandLine `
  -logFile 'C:\Users\dxh53\Desktop\mygame\_tmp\theme-mechanic-catalog.log'
```

Expected log: `PASS: six theme mechanic definitions are valid.`

- [ ] **Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicCatalog.cs Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: define six theme map mechanics"
```

### Task 3: Implement the deterministic controlled director

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/MapMechanicDirector.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`
- Modify: `tools/VerifyThemeMechanicsPhase4.ps1`

- [ ] **Step 1: Add failing director assertions and simulations**

Require `MapMechanicDirector`, `RecordBeat`, `TrySchedule`, `RewardDebt`, and `PressureDebt` in the PowerShell gate. In editor validation, add `ValidateDirectorDeterminism`, `ValidateDirectorCooldown`, and `ValidateDirectorExclusions` calls before building the report.

The deterministic test constructs two directors with seed `20260716`, feeds 500 identical standard beats, and asserts every returned request ID/intensity pair matches. The cooldown test runs 10,000 beats and fails when two scheduled requests have fewer than three intervening beats or reward debt remains starved for more than the policy's validated bound. Assert the first Story request is always low intensity, never signature, and cannot occur before 150 meters. The exclusion test sets `bossActive`, `routeEventActive`, and `themeTransition` separately and requires `TrySchedule` to return false.

- [ ] **Step 2: Run validation and verify compilation fails**

Expected: `MapMechanicDirector` and its request/context types are missing.

- [ ] **Step 3: Add request/context values to `ThemeMechanicModels.cs`**

Add immutable `ThemeMechanicContext` with theme, mode, distance, lives, secondsSinceHit, bossActive, routeEventActive, themeTransition, difficulty01, and `ThemeMechanicRequest` with definition, intensity, signature. Add `ThemeMechanicRequest.None` with `IsValid == false`.

- [ ] **Step 4: Implement the director**

Create `MapMechanicDirector.cs` with these exact rules:

```csharp
namespace PenguinRun
{
    internal sealed class MapMechanicDirector
    {
        private readonly IRunRandom random;
        private readonly ThemeMechanicMode mode;
        private int beatsSinceMechanic = 99;
        private int scheduledCount;
        private bool signatureScheduled;
        public float RewardDebt { get; private set; }
        public float PressureDebt { get; private set; }

        public MapMechanicDirector(ThemeMechanicMode mode, int seed)
        { this.mode = mode; random = new XorShiftRunRandom(seed); }

        public void Reset()
        { beatsSinceMechanic = 99; scheduledCount = 0; signatureScheduled = false; RewardDebt = 0f; PressureDebt = 0f; }

        public void RecordBeat(bool dangerous, bool rewarding)
        {
            beatsSinceMechanic++;
            RewardDebt = UnityEngine.Mathf.Clamp(RewardDebt + (dangerous ? .34f : -.12f) - (rewarding ? .55f : 0f), 0f, 1.5f);
            PressureDebt = UnityEngine.Mathf.Clamp(PressureDebt + (!dangerous && !rewarding ? .18f : -.28f), 0f, 1.5f);
        }

        public bool TrySchedule(in ThemeMechanicContext context, out ThemeMechanicRequest request)
        {
            request = ThemeMechanicRequest.None;
            var policy = ThemeMechanicCatalog.PolicyFor(mode);
            if (context.BossActive || context.RouteEventActive || context.ThemeTransition ||
                context.Distance < policy.FirstDistance || scheduledCount >= policy.MaxPerRun ||
                beatsSinceMechanic < policy.CooldownBeats || context.Lives <= 0) return false;

            var safety = context.SecondsSinceHit < 8f ? -.18f : 0f;
            var chance = UnityEngine.Mathf.Clamp01(policy.BaseChance + PressureDebt * .16f - RewardDebt * .12f + safety);
            if (random.Next01() >= chance) return false;

            if (!ThemeMechanicCatalog.TryForTheme(context.Theme, out var definition)) return false;
            var firstStoryMechanic = mode == ThemeMechanicMode.Story && scheduledCount == 0;
            var signature = !firstStoryMechanic && policy.AllowSignature && !signatureScheduled && context.Lives >= 2 &&
                context.SecondsSinceHit >= 8f && context.Distance >= 650f && random.Next01() < .12f;
            var intensity = signature ? ThemeMechanicIntensity.Signature :
                firstStoryMechanic ? ThemeMechanicIntensity.Low :
                context.SecondsSinceHit < 8f ? ThemeMechanicIntensity.Low :
                context.Difficulty01 > .72f || PressureDebt > .9f ? ThemeMechanicIntensity.High : ThemeMechanicIntensity.Medium;
            request = new ThemeMechanicRequest(true, definition, intensity, signature);
            signatureScheduled |= signature; scheduledCount++; beatsSinceMechanic = 0;
            RewardDebt = UnityEngine.Mathf.Clamp(RewardDebt + (signature ? .6f : .35f), 0f, 1.5f);
            PressureDebt = 0f;
            return true;
        }
    }
}
```

- [ ] **Step 5: Run both gates and commit**

Expected editor report adds `10,000 director beats validated` and both runs exit 0.

```powershell
git add Assets/Scripts/Runtime/PenguinRun/ThemeMechanics Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: add deterministic theme mechanic director"
```

### Task 4: Implement lifecycle, cancellation, and bounded modifiers

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicRuntime.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicModels.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`
- Modify: `tools/VerifyThemeMechanicsPhase4.ps1`

- [ ] **Step 1: Write failing lifecycle tests**

Add static requirements for `Begin`, `Tick`, `Cancel`, `RecordKeypoint`, and `ModifiersFor`. Add editor tests covering the exact phase order `Telegraph -> Active -> Resolve -> Reward -> Cooldown -> Idle`, timeout cancellation, Boss cancellation, and identity modifiers after every cancellation reason.

- [ ] **Step 2: Run Unity validation and confirm it fails**

Expected: missing `ThemeMechanicRuntime`.

- [ ] **Step 3: Implement lifecycle with one cleanup path**

Create `ThemeMechanicRuntime.cs`. `Begin` accepts a valid request only while idle. `Tick` advances phase timers and assigns the new immutable value exposed by `public ThemeMechanicSnapshot Snapshot { get; private set; }`. Initialize it to `ThemeMechanicSnapshot.Empty`; `FinishToIdentity()` and every cancellation path must restore that value before returning to idle. `Cancel(reason)` sets `Cancelled` for one tick, records the reason, then clears the request, keypoint counters, timer, and modifiers. `RecordKeypoint(bool perfect)` increments completed/perfect counts only during `Active`.

Use this exact modifier switch while active; all other phases return `Identity`:

```csharp
private static ThemeMechanicModifiers ModifiersFor(ThemeMechanicId id, ThemeMechanicIntensity intensity)
{
    var strength = intensity switch { ThemeMechanicIntensity.Low => .65f,
        ThemeMechanicIntensity.High => 1f, ThemeMechanicIntensity.Signature => 1f, _ => .82f };
    return id switch
    {
        ThemeMechanicId.IceInertia => new(1f - .28f * strength, 0f, 1f, 1f, 1f),
        ThemeMechanicId.CedarCollapse => ThemeMechanicModifiers.Identity,
        ThemeMechanicId.AuroraPolarity => new(1f, .12f * strength, 1f, 1f, 1f),
        ThemeMechanicId.MistBeacon => new(1f, 0f, 1f, 1f, 1f - .55f * strength),
        ThemeMechanicId.OceanTide => new(1f, -.10f * strength, 1.08f, 1f, 1f),
        ThemeMechanicId.SkyWind => new(1f, .14f * strength, .74f, 1f + .55f * strength, 1f),
        _ => ThemeMechanicModifiers.Identity,
    };
}
```

- [ ] **Step 4: Verify every cancellation path restores identity**

Run Unity validation. Expected report contains `lifecycle and six cancellation cleanup paths valid` and no modifier component outside its bounds.

- [ ] **Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/PenguinRun/ThemeMechanics Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: add safe theme mechanic lifecycle"
```

### Task 5: Deliver the Ice Lake vertical slice and visual apparatus pool

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicPatternCatalog.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicSpawner.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`
- Modify: `tools/VerifyThemeMechanicsPhase4.ps1`

- [ ] **Step 1: Add failing vertical-slice tests**

Require the pattern catalog and spawner in the static gate. In editor validation, instantiate the Ice Lake pattern, assert three cracked-plate beats plus two rebound-gate beats, run spawner reset/reuse twice, and assert no more than 24 active apparatus roots and no stale `ice/floe_bridge` child after a normal event.

- [ ] **Step 2: Add concrete pattern values**

Define `ThemeMechanicBeat` in the models file with `Seconds`, `Lane`, `ApparatusPath`, `RequiresJump`, `RequiresSlide`, `Rewarding`. Create `ThemeMechanicPatternCatalog.IceNormal` and `.IceSignature` with these exact beats:

```csharp
private static readonly ThemeMechanicBeat[] IceNormal =
{
    new(.8f, RunnerLane.Left,   "ice/cracked_plate", true,  false, false),
    new(2.1f, RunnerLane.Right, "ice/crystal_rebound_gate", false, false, false),
    new(3.6f, RunnerLane.Center,"ice/cracked_plate", true,  false, false),
    new(5.2f, RunnerLane.Left,  "ice/crystal_rebound_gate", false, false, true),
    new(6.7f, RunnerLane.Right, "ice/cracked_plate", true,  false, true),
};
private static readonly ThemeMechanicBeat[] IceSignature =
{
    new(.7f, RunnerLane.Center,"ice/floe_bridge", true, false, false),
    new(2.0f, RunnerLane.Left, "ice/floe_bridge", true, false, false),
    new(3.3f, RunnerLane.Right,"ice/floe_bridge", true, false, false),
    new(4.6f, RunnerLane.Center,"ice/floe_bridge", true, false, true),
    new(5.9f, RunnerLane.Left, "ice/floe_bridge", true, false, true),
};
```

Expose `For(ThemeMechanicId id, bool signature)`; return an empty read-only span for definitions not yet implemented so no other theme can start before Task 11.

- [ ] **Step 3: Implement the bounded visual spawner**

`ThemeMechanicSpawner` receives `Transform sceneRoot`, `float laneWidth`, and `float groundY`. Preallocate 24 roots named `ThemeMechanicProp_00` through `_23` and a fixed-capacity beat cursor; `Tick` must not allocate collections. `Begin` stores the pattern and run distance. `Tick` activates each beat once, positions it at `lane.ToX(laneWidth)`, `TrackTerrain.SurfaceY`, and the standard spawn lead; `Clear` disables all roots, clears `CustomVisual` attachments via `RunnerVisualFactory.ClearAttachment`, and resets the active count. Missing custom resources create a cyan/yellow telegraph ring only and return `false` to validation for critical signature beats. Pool exhaustion skips noncritical decoration; failure to reserve a critical apparatus reports `MissingCriticalAsset` to the controller, which cancels the runtime and schedules its safe reward breath instead of instantiating another object.

- [ ] **Step 4: Apply bounded movement modifiers without changing lane authority**

Change `PlayerMotor.Tick` to receive `in ThemeMechanicModifiers mechanic`. Apply `mechanic.GravityMultiplier` to gravity, multiply lane lerp by `LaneResponseMultiplier`, then add drift:

```csharp
var targetX = Lane.ToX(laneWidth);
var pos = player.position;
var response = laneLerp * mechanic.LaneResponseMultiplier;
pos.x = Mathf.Lerp(pos.x, targetX, Mathf.Clamp01(response * dt));
pos.x += mechanic.LaneDriftLanesPerSecond * laneWidth * dt;
pos.x = Mathf.Clamp(pos.x, -laneWidth, laneWidth);
```

Do not change `Lane`, `ApplyAction`, `ObstacleColliderSpec`, or collision bounds. Add narrow internal methods to `SegmentSpawner`: `SpawnMechanicObstacle(string apparatusPath, RunnerLane lane, float z, float runDistance)` and `ClearMechanicVisuals()`; both delegate to existing spawning/pool paths. Ice's cracked plates use the existing `StandardHigh` path; crystal rebound and floe bridge beats are visual/keypoint-only.

- [ ] **Step 5: Run lifecycle, pool, and collision regression; commit**

Expected: Ice pattern and pool validation pass, existing custom map batch validator still reports 48 valid prefabs, and `git diff -- Assets/Scripts/Runtime/PenguinRun/ObstacleColliderSpec.cs` is empty.

```powershell
git add Assets/Scripts/Runtime/PenguinRun/ThemeMechanics Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: add ice lake mechanic vertical slice"
```

### Task 6: Add mastery grading, resonance, and automatic journey contracts

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasteryModels.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/JourneyContractCatalog.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasteryTracker.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`
- Modify: `tools/VerifyThemeMechanicsPhase4.ps1`

- [ ] **Step 1: Add failing mastery and contract tests**

Require `ThemeMasteryTracker`, `JourneyContractCatalog`, `RecordKeypoint`, `RecordDamage`, `ResolveEvent`, `Resonance01`, and `RunSnapshot`. Editor tests must assert: three completed keypoints => `Completed`; all perfect => `Perfect`; all perfect and no damage => `Flawless`; damage removes exactly 35 resonance points; 100 points activates exactly 8 seconds; five contract kinds exist; same seed/theme/mode selects the same contract.

- [ ] **Step 2: Create immutable mastery and contract models**

Add public `JourneyContractKind { PerfectStreak, FlawlessEvents, ResonanceFish, SignatureComplete, SkillBeforeDistance }` and public immutable `ThemeMasteryRunSnapshot` because they cross the public growth/result boundary. Keep `JourneyContractDefinition`, `JourneyContractSnapshot`, and `ThemeMasteryHudSnapshot` internal. The run snapshot must contain theme, eventsCompleted, perfectCount, flawlessCount, bestPerfectStreak, signatureCompleted, contractCompleted, contractKind, earnedMasteryStars, and `tutorialSeen`.

- [ ] **Step 3: Implement five deterministic contracts**

Create catalog entries with exact targets:

```csharp
new(JourneyContractKind.PerfectStreak, "连续回响", "连续完成 3 次完美环境应对", 3),
new(JourneyContractKind.FlawlessEvents, "无伤穿越", "无伤完成 2 个主题事件", 2),
new(JourneyContractKind.ResonanceFish, "丰收回响", "一次回响状态中收集 20 条鱼", 20),
new(JourneyContractKind.SignatureComplete, "地标征服", "完成一个标志性主题事件", 1),
new(JourneyContractKind.SkillBeforeDistance, "技巧冲刺", "800 米前取得 1200 技巧分", 1200),
```

Filter out `SignatureComplete` when the current mode policy disables signatures. Select from remaining definitions using injected `IRunRandom.Range`.

- [ ] **Step 4: Implement tracker transitions**

The tracker constructor receives `tutorialSeenBeforeRun` from the mastery store. `BeginEvent` clears per-event counters and exposes the longer action hint exactly once when that value is false; it then marks the run snapshot's `tutorialSeen` without writing permanent data. `RecordKeypoint(perfect)` increments required/completed and current streak. `RecordDamage` marks the event damaged, breaks streak, and subtracts `.35f` from resonance. `ResolveEvent(signature)` grades, adds `.20f/.32f/.45f` resonance for Completed/Perfect/Flawless, updates contract progress, and starts an 8-second resonance timer at 1.0. `RecordFish`, `RecordSkill`, and `Tick` update only their matching contract and resonance timer. `RunSnapshot` returns an immutable copy.

- [ ] **Step 5: Validate and commit**

Expected editor report includes `five deterministic contracts and resonance lifecycle valid`.

```powershell
git add Assets/Scripts/Runtime/PenguinRun/ThemeMechanics Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: add theme mastery and journey contracts"
```

### Task 7: Wire director, runtime, spawner, and mastery into the run controller

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerRunResult.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/PickupSystem.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/CollisionSystem.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`

- [ ] **Step 1: Write failing controller integration checks**

In the static gate require controller fields `mechanicDirector`, `mechanicRuntime`, `mechanicSpawner`, `themeMastery`, plus calls to `Cancel(ThemeMechanicCancelReason.Boss)`, `Cancel(ThemeMechanicCancelReason.RouteEvent)`, and the new mastery snapshot passed into `RunnerRunResult`.

- [ ] **Step 2: Construct and reset the systems**

In `RunnerGameController` construct all four systems after `WorldDirector`. Use `config.ThemeMechanicMode` and `config.RunSeed`. In `ResetRun`, reset director/runtime/spawner/tracker before seeding initial obstacles. Expose read-only `ThemeMechanicSnapshot ThemeMechanic` and `ThemeMasteryHudSnapshot ThemeMasteryHud` properties for the presentation layer.

- [ ] **Step 3: Insert the simulation in the existing order**

In `TickRunningSimulation`, record the polled action with the tracker, then pass `mechanicRuntime.Snapshot.Modifiers` to `PlayerMotor.Tick`. In `TickRunningWorld`:

1. Cancel for Boss/route/theme transition before ticking the runtime.
2. Tick current runtime and visual spawner.
3. When idle, build context from world/config/Boss/route and call director.
4. On accepted request, call runtime/spawner/tracker `Begin` in that order.
5. Evaluate due keypoints from player lane/action/grounded state.
6. Resolve and award through `WorldDirector.AddSkillScore` and existing fish APIs.

Set `spawner.SpawnPaused` true for active signature events, Boss, or route choice; normal theme events leave ordinary spawning active at reduced density.

- [ ] **Step 4: Route damage, pickup, and result signals**

Add callbacks rather than new authority: extend the existing collision resolve/tick entry point with an optional trailing `System.Action onPlayerDamaged = null` and invoke it exactly once after an existing damage outcome; extend the existing pickup tick entry point with an optional trailing `System.Action<int> onFishCollected = null` and invoke it exactly once after an existing fish pickup, passing the awarded count. Update only their existing controller call sites, which forward the callbacks to `ThemeMasteryTracker.RecordDamage` and `RecordFish`. Extend `RunnerRunResult` constructor/property with `ThemeMasteryRunSnapshot themeMastery`; copy the value directly because it is immutable.

- [ ] **Step 5: Run all existing run-depth and collision gates; commit**

Run `VerifyRunDepthPhase2.ps1`, `VerifyCustomMapAssetsBatch1.ps1`, the new static gate, and Unity validation. Expected: all pass and `ObstacleColliderSpec.cs` remains unchanged.

```powershell
git add Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs Assets/Scripts/Runtime/PenguinRun/Systems Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: integrate theme mechanics into run lifecycle"
```

### Task 8: Add explicit Story, Endless, and Daily policies and stable seeds

**Files:**
- Modify: `Assets/Scripts/Runtime/Game/RunSession.cs`
- Modify: `Assets/Scripts/Runtime/Game/RunnerSessionConfig.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyChapterPage.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`

- [ ] **Step 1: Write failing mode/seed tests**

Add validation for three enum values, `PrepareStoryRun`, `PrepareEndlessRun`, `TryPrepareDailyRun`, `NextRunSeed`, and `RunSeed`. Assert daily seed is stable for a supplied UTC date, Story seed is stable for the same chapter/theme/attempt, and Endless seeds differ across consecutive runs.

- [ ] **Step 2: Replace the boolean-only session mode**

Add `public enum RunnerRunMode { Story, Endless, Daily }`, `RunSession.NextRunMode`, `RunSession.NextRunSeed`, and keep `NextRunDaily` as a compatibility property returning `NextRunMode == Daily`. `PrepareStoryRun` accepts an explicit theme and uses the saved chapter attempt count. `PrepareEndlessRun` retains theme cycling and increments an in-memory attempt counter. `TryPrepareDailyRun(DateTime utc, DifficultyKind difficulty, out string error)` retains date theme selection, records the seed atomically on success, and leaves the prior pending session untouched on failure.

- [ ] **Step 3: Derive one stable run seed**

Add immutable public `RunnerSessionConfig.Mode` and `RunSeed` properties plus internal `ThemeMechanicMode ThemeMechanicMode { get; }`, avoiding exposure of an internal runtime enum from the public config type. `RunnerSessionConfig.Snapshot()` copies the already recorded `RunSession.NextRunSeed`; the three preparation methods are the only callers of this helper, so no scene reload can silently change a run:

```csharp
private static int ResolveRunSeed(RunnerRunMode mode, RunnerMapTheme theme, int attempt, System.DateTime utc)
{
    unchecked {
        var day = utc.Year * 1000 + utc.DayOfYear;
        return mode switch {
            RunnerRunMode.Daily => day * 486187739 ^ (int)theme * 16777619,
            RunnerRunMode.Story => 0x13579BDF ^ (int)theme * 73856093 ^ attempt * 19349663,
            _ => System.Environment.TickCount ^ (int)theme * 83492791 ^ attempt * 297121507,
        };
    }
}
```

Use `System.DateTime.UtcNow` only in `MainMenuBootstrap` when it calls `TryPrepareDailyRun`. Catch seed/config exceptions inside that method and return an exception-safe Chinese error; `MainMenuBootstrap` must not load the run scene on false and routes the text through the existing `PanelEvent.Toast`. Story/Endless use the already recorded run seed after scene reload. Remove the controller's direct `Random.InitState` date formula; initialize legacy Unity randomness once from `config.RunSeed` and ensure the new director/contracts use injected xorshift only.

- [ ] **Step 4: Split chapter and dock actions**

Rename `LobbyChapterPage.Bind(... Action onStartJourney)` callback use to Story semantics. In `MainMenuBootstrap`, bind the chapter primary CTA to `StartStory`, keep dock run action bound to `StartEndless`, add `BeforeStoryRun` to `DifficultyOverlayMode`, label it `继续章节 · 选择难度`, and call `RunSession.PrepareStoryRun(RunSession.NextRunTheme, pendingDifficulty)` on confirmation. Change the Daily confirmation path to call `TryPrepareDailyRun(DateTime.UtcNow, pendingDifficulty, out error)` and load the run scene only when it returns true.

- [ ] **Step 5: Validate modes and commit**

Expected: daily replay validation produces an identical 500-beat event sequence; chapter CTA and endless dock contract checks both pass.

```powershell
git add Assets/Scripts/Runtime/Game Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: add story endless and daily mechanic policies"
```

### Task 9: Add minimal HUD, pause, and result presentation

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/RunResultOverlay.cs`
- Modify: `Assets/Editor/ProjectUiLayoutRegressionTest.cs`
- Modify: `tools/VerifyThemeMechanicsPhase4.ps1`

- [ ] **Step 1: Add failing HUD layout contracts**

Require `MechanicBanner`, `ResonanceFill`, `ContractSummary`, `SetThemeMechanic`, and `SetThemeMastery`. Extend the layout regression to assert all three are inside safe area at 1280x720, 1920x1080, and 2560x1080, and inactive while `SetBossMode` is active.

- [ ] **Step 2: Create the restrained HUD elements**

Place `MechanicBanner` at anchors `(0.28, 0.76)-(0.72, 0.88)`, below Boss focus; title uses `UiTheme.Caption`, hint uses `UiTheme.Micro`. Place a 4-pixel `ResonanceFill` at `(0.36, 0.185)-(0.64, 0.198)`. Add `ContractSummary` to `pauseRoot` below `PausedText`; do not create a permanent run-screen contract card.

- [ ] **Step 3: Bind snapshots without presentation-owned state**

Add:

```csharp
public void SetThemeMechanic(ThemeMechanicSnapshot snapshot)
public void SetThemeMastery(ThemeMasteryHudSnapshot snapshot)
```

Banner is visible only for Telegraph/Active/Reward and when not in Boss mode. Resonance fill width equals `snapshot.Resonance01`; contract text updates only when progress changes. `PenguinRunnerGame.Update` calls both methods next to existing route/skill HUD binding.

- [ ] **Step 4: Add result summaries**

Extend `JourneyResultViewModel` with `ThemeMasteryTitle`, `ThemeMasteryBody`, and `ContractBody`. Build text from the immutable run snapshot, for example `冰湖回响 · 完美 4 · 无伤 2` and `旅途契约完成 · 连续回响`. Add one compact parchment row to `RunResultOverlay`, below the existing route/build/skill highlights.

- [ ] **Step 5: Run three-resolution regression and commit**

Expected: `[ProjectUiLayoutRegressionTest] PASS: UI layout regressions are covered.` plus phase-four static gate pass.

```powershell
git add Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs Assets/Scripts/Runtime/Game/UI Assets/Editor/ProjectUiLayoutRegressionTest.cs tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: present theme mechanics and mastery results"
```

### Task 10: Persist theme mastery through the single growth transaction

**Files:**
- Create: `Assets/Scripts/Runtime/Game/Growth/ThemeMasteryStore.cs`
- Modify: `Assets/Scripts/Runtime/Game/Growth/GrowthTransaction.cs`
- Modify: `Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`

- [ ] **Step 1: Write failing migration and transaction tests**

Add editor validation that seeds existing best score, fish, camp, and Boss keys; runs `ThemeMasteryStore.EnsureMigrated`; asserts existing values are unchanged and six mastery records plus six tutorial flags read zero/false. Apply one run snapshot twice and assert counts increase exactly twice while stars are clamped 0–3 and the tutorial flag remains true. Assert no `PlayerSave.Flush` occurs inside `ThemeMasteryStore` by static source check.

- [ ] **Step 2: Create a versioned mastery store**

Use `PlayerSave.GetGrowthInt/SetGrowthInt` with prefix `theme_mastery_v1_{theme}_`. Store `events`, `perfect`, `flawless`, `best_streak`, `signature`, `contracts`, `stars`, and boolean-as-int `tutorial`; store schema version under `theme_mastery_schema`. `EnsureMigrated` writes version 1 only when absent. `For(theme)` returns a public clamped immutable `ThemeMasteryRecord`, including `TutorialSeen`, because `GrowthTransactionResult` publicly exposes it.

Star rules are exact: one star at 3 completed events; two stars at 5 perfect and 1 contract; three stars only after a flawless signature. `ApplyRun` takes max for best streak/stars/signature/tutorial and sums non-negative counters.

- [ ] **Step 3: Extend the growth transaction input/result**

Add `ThemeMasteryRunSnapshot ThemeMastery` to `GrowthTransactionInput`. In `GrowthTransaction.Apply`, call `ThemeMasteryStore.ApplyRun(input.ThemeMastery)` after discoveries and before companion award. Add `ThemeMasteryRecord ThemeMastery` and `bool ThemeMasteryStarGained` to the result. Update strongest summary priority: new discovery, mastery star, companion level, claimable goal, companion XP.

- [ ] **Step 4: Propagate the immutable snapshot once**

Extend `RunOutcomeRouter.Apply` and `Result` with the snapshot/result. `PenguinRunnerGame.HandleRunFinished` passes `RunnerRunResult.ThemeMastery`. Keep `PlayerSave.Flush` at the existing single location after records/growth are applied.

- [ ] **Step 5: Run persistence tests and commit**

Expected: migration report states `existing save keys preserved; six zero-default mastery records valid`.

```powershell
git add Assets/Scripts/Runtime/Game/Growth Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs
git commit -m "feat: persist theme mastery progression"
```

### Task 11: Implement Cedar, Aurora, Mist, Ocean, and Sky patterns

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicPatternCatalog.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicSpawner.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`

- [ ] **Step 1: Add failing pattern coverage tests**

For each of the six IDs, require at least five normal beats and five signature beats, at least one reward beat, and both configured apparatus paths. Validate that decoy beats are visual-only, tide beats reference validated existing obstacle kinds, and no pattern requests more than 24 active roots.

- [ ] **Step 2: Add exact normal beat sequences**

Add these five-beat sequences; times are seconds from Active start and lanes are `L/C/R`:

| Mechanic | Normal beats |
|---|---|
| Cedar | `.8 L swing_log`, `2.2 R root_gallery slide`, `3.8 C swing_log`, `5.4 L root_gallery slide`, `7.0 R swing_log reward` |
| Aurora | `.7 C polarity_gate`, `2.0 L prism_beam slide`, `3.4 R polarity_gate`, `4.9 C prism_beam slide`, `6.5 L polarity_gate reward` |
| Mist | `.9 R decoy_shadow visual`, `2.4 L rotating_beacon`, `4.0 C decoy_shadow visual`, `5.8 R rotating_beacon`, `7.6 C rotating_beacon reward` |
| Ocean | `.8 L coral_lift jump`, `2.3 C bubble_vault jump`, `3.9 R coral_lift slide`, `5.5 L bubble_vault jump`, `7.1 C coral_lift reward` |
| Sky | `.7 C wind_gate`, `2.2 R cloud_platform jump`, `3.8 L wind_gate`, `5.4 C cloud_platform jump`, `7.2 R wind_gate reward` |

Encode them as explicit `ThemeMechanicBeat[]`; do not generate lane/timing values randomly.

- [ ] **Step 3: Add exact signature sequences**

Add six to eight beats per setpiece: Cedar alternates collapsing `ruin_escape` gates L/R/C; Aurora alternates polarity gates with two centered prism slides; Mist alternates true rotating beacons with two non-colliding decoys; Ocean forms a jump/slide/jump wave-tunnel rhythm; Sky forms six L/C/R glide-chain platforms. Ice remains the Task 5 sequence.

- [ ] **Step 4: Map beats to existing collision-safe obstacles**

`SegmentSpawner.SpawnMechanicObstacle(string path, RunnerLane lane, float z, float runDistance)` maps only to existing kinds:

```csharp
return path switch
{
    "cedar/swing_log" => SpawnExisting(ObstacleKind.StandardHigh, lane, z, runDistance),
    "cedar/root_gallery" => SpawnExisting(ObstacleKind.SlideGate, lane, z, runDistance),
    "aurora/prism_beam" => SpawnExisting(ObstacleKind.SlideGate, lane, z, runDistance),
    "ocean/coral_lift" => SpawnExisting(ObstacleKind.StandardHigh, lane, z, runDistance),
    "sky/cloud_platform" => SpawnExisting(ObstacleKind.WideWall, lane, z, runDistance),
    _ => true,
};
```

Add `private bool SpawnExisting(ObstacleKind kind, RunnerLane lane, float z, float runDistance)` as a narrow wrapper around `SegmentSpawner`'s current pooled obstacle creation path. It must preserve the existing `ObstacleColliderSpec`, placement, activation, and pool bookkeeping without constructing or resizing any collider. `SpawnMechanicObstacle` returns that wrapper's success value and owns no additional collision logic.

Polarity, beacon, bubble, wind, decoy, and setpiece visual beats are evaluated by runtime keypoints and do not create new damage colliders. Ocean tide chooses existing high/slide variants; it never scales a collider.

- [ ] **Step 5: Validate all themes and commit**

Expected report: `12 patterns / at least 60 beats / 0 new collider authorities valid`.

```powershell
git add Assets/Scripts/Runtime/PenguinRun/ThemeMechanics Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs
git commit -m "feat: add five remaining theme mechanic patterns"
```

### Task 12: Build eighteen detailed dual-LOD apparatus and setpiece prefabs

**Files:**
- Create: `Assets/Scripts/Editor/PenguinRun/ThemeMechanicSculptureBuilder.cs`
- Create: `Assets/Scripts/Editor/PenguinRun/ThemeMechanicPrefabBuilder.cs`
- Create: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicPreviewCapture.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/RunnerSculptureBuilder.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/PenguinVisualPrefabBuilder.cs`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`
- Create: `Assets/Resources/PenguinRun/Prefabs/ThemeMechanics/**`
- Create: `Artifacts/ThemeMechanicPreviews/*.png`

- [ ] **Step 1: Add failing asset-count and preview tests**

Static gate requires `ThemeMechanicPrefabBuilder`, `BuildBatch`, `LODGroup`, and `ProjectThemeMechanicPreviewCapture.HasVisibleContent`. Unity validation requires exactly 18 prefabs, one LODGroup each, rendered LOD0/LOD1, valid non-error materials, finite bounds, no Collider, and no more than 24 renderers per LOD0 prefab.

- [ ] **Step 2: Expose shared sculpture helpers**

Change `RunnerSculptureBuilder.Part` and `Beam` from `private` to `internal`; keep signatures and behavior unchanged. Change `PenguinVisualPrefabBuilder.ThemePalette`, `Material`, and `EnsureFolder` to `internal`. Re-run the existing 48-prefab validator before using the helpers; expected counts remain 48, 469 LOD0 parts, 251 LOD1 parts.

- [ ] **Step 3: Implement exact shape recipes**

`ThemeMechanicSculptureBuilder.Build(root, path, palette, low)` uses these named groups:

| Asset | Required LOD0 groups |
|---|---|
| cracked_plate | 6 radial ice slabs, 5 dark crack beams, 3 snow chips |
| crystal_rebound_gate | 2 crystal posts, 1 glowing core, 4 brace beams |
| floe_bridge | 7 offset ice plates, 4 crack beams, 2 edge crystals |
| swing_log | 1 capsule log, 2 rope pairs, 2 metal collars, 6 bark ribs |
| root_gallery | 4 root arches, 3 brace beams, 5 snow clumps |
| ruin_escape | 3 leaning stone/wood towers, 6 falling beams, 4 dust markers |
| polarity_gate | 2 colored poles, 3 copper rings, 1 compass core |
| prism_beam | 2 tripod posts, 1 emissive beam, 3 prism shards |
| magnetic_rebuild | 5 floating stone blocks, 4 copper arcs, 2 glow cores |
| decoy_shadow | 3 translucent silhouette fins, 1 dim false lamp |
| rotating_beacon | 1 stone base, 1 mast, 4 lens fins, 1 bright core |
| floodgate_chase | 2 gate towers, 1 moving light carriage, 6 water/rope beams |
| coral_lift | 7 coral branches, 4 shell plates, 3 bubble spheres |
| bubble_vault | 5 bubble spheres, 2 coral anchors, 4 foam rings |
| wave_tunnel | 8 curved-looking capsule ribs, 6 foam spheres, 2 coral gates |
| wind_gate | 2 cloth vanes, 1 mast, 4 rope beams, 1 compass hub |
| cloud_platform | 5 cloud spheres, 3 wood planks, 4 rope braces |
| island_glide_chain | 4 floating rock blocks, 6 cloud spheres, 5 sail/rope guides |

LOD1 uses at most half the listed repeated parts while retaining silhouette and glow core. Use only cached theme palette materials.

- [ ] **Step 4: Build prefabs and six previews**

`ThemeMechanicPrefabBuilder.BuildBatchFromCommandLine` loops the 18 catalog paths and saves under `Assets/Resources/PenguinRun/Prefabs/ThemeMechanics/{path}.prefab`, with LOD thresholds `.60`, `.22`, `.03`, cross-fade, and collider stripping. Capture one 1600x900 shelf preview per theme showing apparatus A, apparatus B, and signature; reject images with fewer than 1% non-background pixels. Run capture without `-nographics`.

Run:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe'
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path `
  -executeMethod PenguinRun.Editor.ThemeMechanicPrefabBuilder.BuildBatchFromCommandLine `
  -logFile '_tmp/theme-mechanic-prefabs.log'
& $unity -batchmode -quit -projectPath (Get-Location).Path `
  -executeMethod PenguinRun.Editor.ProjectThemeMechanicPreviewCapture.CaptureFromCommandLine `
  -logFile '_tmp/theme-mechanic-previews.log'
```

Expected: both exit 0; the first reports 18 prefabs and the second reports six nonblank 1600x900 previews.

- [ ] **Step 5: Inspect previews, validate, and commit**

Inspect all six PNGs at original resolution. Reject clipped objects, indistinguishable silhouettes, missing glow cores, or theme pairs that read as recolors. Run both visual validators and commit only after exact counts pass.

```powershell
git add Assets/Scripts/Editor/PenguinRun Assets/Resources/PenguinRun/Prefabs/ThemeMechanics Assets/Art/PenguinRun/Materials Artifacts/ThemeMechanicPreviews tools/VerifyThemeMechanicsPhase4.ps1
git commit -m "feat: add detailed theme mechanic apparatus assets"
```

### Task 13: Complete full regression, performance evidence, docs, and Android delivery

**Files:**
- Modify: `tools/BuildLatestAndroidApk.ps1`
- Modify: `docs/CURRENT_DESIGN.md`
- Create: `docs/THEME_MECHANICS_PHASE4_DELIVERY.md`
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs`

- [ ] **Step 1: Add the new gate to Android pre-build checks**

Insert `VerifyThemeMechanicsPhase4.ps1` after `VerifyCustomMapAssetsBatch1.ps1`. Do not use `-SkipChecks` for the final build.

- [ ] **Step 2: Run every static gate**

Run in order:

```powershell
$scripts = @(
  'VerifyUiArtP0.ps1','VerifyStorybookUiPhase1.ps1','VerifyRunDepthPhase2.ps1',
  'VerifyLongTermGrowthPhase3.ps1','VerifyPremiumStorybookHome.ps1',
  'VerifyCustomMapAssetsBatch1.ps1','VerifyThemeMechanicsPhase4.ps1')
foreach($script in $scripts) { & (Join-Path '.\tools' $script); if(-not $?) { exit 1 } }
```

Expected: seven pass lines and exit 0.

- [ ] **Step 3: Run Unity deterministic, lifecycle, asset, and layout validation**

Run `ProjectThemeMechanicValidation.ValidateFromCommandLine`, `ProjectVisualAssetValidation.ValidateFromCommandLine`, and `ProjectUiLayoutRegressionTest.RunFromCommandLine` in separate Unity invocations:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe'
$methods = @(
  'PenguinRun.Editor.ProjectThemeMechanicValidation.ValidateFromCommandLine',
  'PenguinRun.Editor.ProjectVisualAssetValidation.ValidateFromCommandLine',
  'PenguinRun.Editor.ProjectUiLayoutRegressionTest.RunFromCommandLine')
foreach($method in $methods) {
  $slug = ($method -split '\.')[-1]
  & $unity -batchmode -nographics -quit -projectPath (Get-Location).Path `
    -executeMethod $method -logFile "_tmp/$slug.log"
  if($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
```

Expected phase-four report includes six definitions, 10,000 director beats, 12 patterns, 18 dual-LOD prefabs, five contracts, cancellation cleanup, and pool reset. Expected layout report is PASS at all three resolutions.

- [ ] **Step 4: Record 15-minute soak and performance budgets**

Add a validation simulation of 900 seconds at 60 ticks/second. Assert active apparatus <= 24, active event FX <= 12, no modifier remains non-identity outside Active, no same core within three beats, and all pooled roots return inactive after reset. Add static/runtime checks that event updates never access `renderer.material`, never create runtime rigidbody fragments, and allocate no temporary collections after warmup. Record peak active objects, total scheduled events, cancellations, reward beats, maximum renderer count, and measured managed allocations in the report.

- [ ] **Step 5: Build and verify Android APK**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/BuildLatestAndroidApk.ps1
```

Expected: current package/version read from `ProjectSettings`, v2 signature verification succeeds, stable latest APK is copied, and SHA-256 is printed.

- [ ] **Step 6: Write delivery documentation**

`docs/THEME_MECHANICS_PHASE4_DELIVERY.md` records delivered definitions, apparatus paths, signature events, mastery/contract rules, mode policies, validation counts, preview paths, APK path/hash, collision authority statement, and any measured performance values. Update `CURRENT_DESIGN.md` with a short phase-four status and link rather than duplicating the report.

- [ ] **Step 7: Check cleanliness and commit**

```powershell
git diff --check
git status --short
git add tools/BuildLatestAndroidApk.ps1 docs Assets/Scripts/Editor/PenguinRun/ProjectThemeMechanicValidation.cs
git commit -m "docs: complete theme mechanics phase four delivery"
```

Expected: worktree clean after commit, with no modifications to `ObstacleColliderSpec.cs` and no untracked runtime assets.
