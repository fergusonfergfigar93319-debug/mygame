# Protagonist Avatar Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the runtime-built protagonist with a high-quality rigged Gugugaga avatar, drive all core and interaction animation from data, preserve gameplay judgment, and retain the procedural penguin as a safe fallback.

**Architecture:** Introduce an `IPlayerCharacterPresentation` boundary between gameplay and visuals. A `PlayerAvatarPresenter` consumes a compact `RunnerAvatarState`, builds an `AnimatorOverrideController` from ScriptableObject definitions, and delegates animation, expression, secondary motion, accessories, and FX to focused components; the existing `PenguinCharacter` becomes the fallback implementation.

**Tech Stack:** Unity 6000.4.5f1, C#/.NET, Mecanim Animator, ScriptableObject, Blender FBX pipeline, stylized PBR materials, PowerShell contract gates, Unity batch-mode validation and preview capture.

---

## File map

Runtime contracts and state:

- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/IPlayerCharacterPresentation.cs` — presentation interface consumed by game flow.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarState.cs` — immutable gameplay-to-visual snapshot and buff flags.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarInteraction.cs` — interaction animation IDs.

Data assets:

- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarDefinition.cs` — top-level avatar configuration.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationSet.cs` — canonical clip map and override builder.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAppearanceSet.cs` — renderer/material overrides.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAccessorySocketSet.cs` — stable bone/socket declarations.

Runtime presentation:

- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/PlayerAvatarPresenter.cs` — composition root and fallback selection.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs` — Animator parameter and trigger adapter.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerExpressionDriver.cs` — blink/look/face controls.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerSecondaryMotion.cs` — scarf and goggles motion.
- Create `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerCharacterFx.cs` — shield, dash, magnet, invulnerability, rings, buff motes.
- Modify `Assets/Scripts/Runtime/PenguinRun/PenguinCharacter.cs` — implement the compatibility interface only; keep procedural construction intact.
- Modify `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs` — produce `RunnerAvatarState` and consume the interface.
- Modify `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs` — load the default definition and create the presenter.
- Modify `Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs` — expose read-only vertical velocity.

Editor and asset pipeline:

- Create `Assets/Scripts/Editor/PenguinRun/RunnerAvatarModelImportProcessor.cs` — enforce Humanoid/Generic import, clips, materials, and compression.
- Create `Assets/Scripts/Editor/PenguinRun/RunnerAvatarAssetBuilder.cs` — create controller, override assets, definition, prefab, and LODGroup.
- Create `Assets/Scripts/Editor/PenguinRun/ProjectRunnerAvatarValidation.cs` — structural, budget, and gameplay-boundary validation.
- Modify `Assets/Scripts/Editor/PenguinRun/ProjectPenguinPreviewCapture.cs` — capture the new presenter in nine required poses.
- Create `tools/VerifyProtagonistAvatarRevamp.ps1` — fast static release gate.

Art source and runtime assets:

- Create `Artifacts/CharacterSource/Gugugaga/gugugaga_high.blend` — high-poly sculpt source.
- Create `Artifacts/CharacterSource/Gugugaga/gugugaga_game.blend` — retopology, UV, rig, and animation source.
- Create `Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_avatar.fbx` — LOD0 rig and clips.
- Create `Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_lod1.fbx` and `gugugaga_lod2.fbx` — reduced meshes using the same bind pose.
- Create `Assets/Resources/PenguinRun/Characters/Gugugaga/T_gugugaga_basecolor.png`, `T_gugugaga_normal.png`, and `T_gugugaga_mask.png` — 2K texture set.
- Create generated materials, controller, prefab, animation set, appearance set, socket set, and avatar definition below `Assets/Resources/PenguinRun/Characters/Gugugaga/`.

## Task 1: Add a failing avatar contract gate

**Files:**
- Create: `tools/VerifyProtagonistAvatarRevamp.ps1`

- [ ] **Step 1: Write the failing static contract gate**

```powershell
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
function Require-File([string]$path) {
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $path))) { $failures.Add("Missing: $path") }
}
function Require-Pattern([string]$path, [string]$pattern, [string]$label) {
    $full = Join-Path $ProjectRoot $path
    if (-not (Test-Path -LiteralPath $full) -or (Get-Content -Raw -Encoding utf8 $full) -notmatch $pattern) {
        $failures.Add("Missing contract: $label")
    }
}
foreach ($file in @(
    'Assets/Scripts/Runtime/PenguinRun/Avatar/IPlayerCharacterPresentation.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarState.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarDefinition.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/PlayerAvatarPresenter.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerCharacterFx.cs',
    'Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_avatar.fbx',
    'Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_lod1.fbx',
    'Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_lod2.fbx'
)) { Require-File $file }
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs' 'public float VerticalVelocity\s*=>' 'read-only vertical velocity'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' 'PlayerAvatarPresenter\.Create' 'presenter creation entry point'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Avatar/PlayerAvatarPresenter.cs' 'PenguinCharacter\.CreateCute' 'procedural fallback'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs' 'applyRootMotion\s*=\s*false' 'root motion disabled'
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: protagonist avatar revamp contracts hold.'
```

- [ ] **Step 2: Run the gate and verify it fails on the first missing runtime file**

Run: `powershell -ExecutionPolicy Bypass -File tools/VerifyProtagonistAvatarRevamp.ps1`

Expected: exit code `1` with `Missing: Assets/Scripts/Runtime/PenguinRun/Avatar/IPlayerCharacterPresentation.cs`.

- [ ] **Step 3: Commit the failing release contract**

```powershell
git add tools/VerifyProtagonistAvatarRevamp.ps1
git commit -m "test: define protagonist avatar revamp contracts"
```

## Task 2: Introduce the presentation boundary and immutable state

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/IPlayerCharacterPresentation.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarState.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarInteraction.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinCharacter.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs`

- [ ] **Step 1: Add the interface and state types**

```csharp
using UnityEngine;

namespace PenguinRun
{
    internal interface IPlayerCharacterPresentation
    {
        Transform Root { get; }
        void Tick(float deltaTime, in RunnerAvatarState state);
        void TriggerHurt();
        void TriggerPerfectDodge();
        void TriggerCelebration();
        void TriggerInteraction(RunnerAvatarInteraction interaction);
    }
}
```

```csharp
using System;

namespace PenguinRun
{
    [Flags]
    internal enum RunnerAvatarBuffFlags
    {
        None = 0, Dash = 1, Magnet = 2, Shield = 4, Invulnerable = 8,
        Glide = 16, ScoreBoost = 32, SlowMo = 64, DoubleFish = 128,
        IceMirror = 256, AuroraChain = 512, FogLantern = 1024,
        TreantArmor = 2048, CoralBounce = 4096, ThunderFeather = 8192, FishBomb = 16384
    }

    internal readonly struct RunnerAvatarState
    {
        public RunnerAvatarState(bool running, bool grounded, bool sliding, bool gameOver,
            float speed01, float laneLean, float verticalVelocity, RunnerAvatarBuffFlags buffs)
        {
            Running = running; Grounded = grounded; Sliding = sliding; GameOver = gameOver;
            Speed01 = speed01; LaneLean = laneLean; VerticalVelocity = verticalVelocity; Buffs = buffs;
        }
        public bool Running { get; }
        public bool Grounded { get; }
        public bool Sliding { get; }
        public bool GameOver { get; }
        public float Speed01 { get; }
        public float LaneLean { get; }
        public float VerticalVelocity { get; }
        public RunnerAvatarBuffFlags Buffs { get; }
        public bool Has(RunnerAvatarBuffFlags flag) => (Buffs & flag) != 0;
    }
}
```

```csharp
namespace PenguinRun
{
    internal enum RunnerAvatarInteraction { FishPickup, ShieldActivate, DashActivate, MagnetActivate, BossCounter }
}
```

- [ ] **Step 2: Adapt the procedural character without changing its visuals**

Make `PenguinCharacter : IPlayerCharacterPresentation`; implement `Tick` by forwarding state fields to its existing `Animate`, add `TriggerHurt` and `TriggerPerfectDodge` forwarding methods, keep the existing `TriggerCelebration` as the interface implementation, and make `TriggerInteraction` a no-op. Convert the existing `SetAuxBuffState(WorldDirector)` usage later; do not remove it in this task.

```csharp
public void Tick(float deltaTime, in RunnerAvatarState state) => Animate(
    deltaTime, state.Running, state.Grounded, state.Sliding, state.GameOver,
    state.Speed01, state.LaneLean,
    state.Has(RunnerAvatarBuffFlags.Dash) ? 1f : 0f,
    state.Has(RunnerAvatarBuffFlags.Magnet) ? 1f : 0f,
    state.Has(RunnerAvatarBuffFlags.Shield) ? 1f : 0f,
    state.Has(RunnerAvatarBuffFlags.Invulnerable) ? 1f : 0f);
public void TriggerHurt() => TriggerHurtFlash();
public void TriggerPerfectDodge() => TriggerPerfectDodgeBurst();
public void TriggerInteraction(RunnerAvatarInteraction interaction) { }
```

- [ ] **Step 3: Expose vertical velocity without granting write access**

Add to `PlayerMotor` beside `LaneLean01`:

```csharp
public float VerticalVelocity => velocityY;
```

- [ ] **Step 4: Run the compile gate**

Run: `powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1`

Expected: `COMPILE OK`.

- [ ] **Step 5: Commit the presentation contract**

```powershell
git add Assets/Scripts/Runtime/PenguinRun/Avatar Assets/Scripts/Runtime/PenguinRun/PenguinCharacter.cs Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs
git commit -m "refactor: introduce player avatar presentation boundary"
```

## Task 3: Add data-driven avatar assets and validation

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarDefinition.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationSet.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAppearanceSet.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAccessorySocketSet.cs`
- Create: `Assets/Scripts/Editor/PenguinRun/ProjectRunnerAvatarValidation.cs`

- [ ] **Step 1: Add complete ScriptableObject contracts**

`RunnerAvatarDefinition` must contain `avatarId`, `modelPrefab`, `baseController`, `animations`, `appearance`, `sockets`, `localPosition`, `localEulerAngles`, `localScale`, and `fallbackToProcedural`. `RunnerAnimationSet` must expose all sixteen canonical locomotion, reaction, celebration, and interaction clips and return a dictionary keyed by the base controller placeholder names below.

```csharp
public IReadOnlyDictionary<string, AnimationClip> Overrides => new Dictionary<string, AnimationClip>
{
    ["Idle"] = idle, ["Run"] = run, ["LaneLeft"] = laneLeft, ["LaneRight"] = laneRight,
    ["Jump"] = jump, ["Fall"] = fall, ["Land"] = land, ["Slide"] = slide,
    ["Hurt"] = hurt, ["Defeat"] = defeat, ["Celebrate"] = celebrate,
    ["FishPickup"] = fishPickup, ["ShieldActivate"] = shieldActivate,
    ["DashActivate"] = dashActivate, ["MagnetActivate"] = magnetActivate,
    ["BossCounter"] = bossCounter
};
```

`RunnerAppearanceSet` stores serializable `{ string rendererPath; Material material; }` overrides. `RunnerAccessorySocketSet` stores serializable `{ string id; string relativePath; }` entries for `head`, `face`, `neck`, and `back`. Because the FBX uses a Generic rig, socket resolution must use validated relative paths rather than `HumanBodyBones`.

- [ ] **Step 2: Add a validator with explicit failure messages**

`ProjectRunnerAvatarValidation.ValidateDefinition` must fail when the ID is empty; prefab, controller, or animation set is null; a required clip is null; root motion is enabled on the prefab Animator; required sockets cannot resolve; any texture exceeds 2048; LOD0 exceeds 50,000 triangles; LOD1 exceeds 22,000; LOD2 exceeds 8,000; or the prefab uses more than three unique materials.

```csharp
private static void Fail(List<string> errors, string avatarId, string message) =>
    errors.Add($"[{avatarId}] {message}");

public static void ValidateFromCommandLine()
{
    var definition = Resources.Load<RunnerAvatarDefinition>("PenguinRun/Characters/Gugugaga/GugugagaAvatar");
    var errors = ValidateDefinition(definition);
    if (errors.Count == 0) { Debug.Log("[ProjectRunnerAvatarValidation] PASS"); EditorApplication.Exit(0); }
    Debug.LogError("[ProjectRunnerAvatarValidation] FAIL:\n" + string.Join("\n", errors));
    EditorApplication.Exit(1);
}
```

- [ ] **Step 3: Compile and confirm the static gate still fails only on missing presenter/assets**

Run: `powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1`

Expected: `COMPILE OK`.

Run: `powershell -ExecutionPolicy Bypass -File tools/VerifyProtagonistAvatarRevamp.ps1`

Expected: exit code `1`; definition/state missing-file failures are gone, model/presenter failures remain.

- [ ] **Step 4: Commit data contracts and validation**

```powershell
git add Assets/Scripts/Runtime/PenguinRun/Avatar Assets/Scripts/Editor/PenguinRun/ProjectRunnerAvatarValidation.cs
git commit -m "feat: add data-driven runner avatar definitions"
```

## Task 4: Author and import the high-quality Gugugaga model

**Files:**
- Create: `Artifacts/CharacterSource/Gugugaga/gugugaga_high.blend`
- Create: `Artifacts/CharacterSource/Gugugaga/gugugaga_game.blend`
- Create: `Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_avatar.fbx`
- Create: `Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_lod1.fbx`
- Create: `Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_lod2.fbx`
- Create: `Assets/Resources/PenguinRun/Characters/Gugugaga/T_gugugaga_basecolor.png`
- Create: `Assets/Resources/PenguinRun/Characters/Gugugaga/T_gugugaga_normal.png`
- Create: `Assets/Resources/PenguinRun/Characters/Gugugaga/T_gugugaga_mask.png`
- Create: `Assets/Scripts/Editor/PenguinRun/RunnerAvatarModelImportProcessor.cs`

- [ ] **Step 1: Use the Blender skill to build the approved silhouette**

Create front, side, and rear turntable checkpoints before retopology. The model must have a forward lean, wide readable feet, tapered wings, integrated bronze-rim cyan goggles, and a red scarf with a separate trailing section. Keep facial landmarks readable at the actual runner camera distance.

Expected source output: `gugugaga_high.blend` contains named collections `BODY_HIGH`, `ACCESSORIES_HIGH`, and `BAKE_CAGE`; no unapplied negative scale.

- [ ] **Step 2: Retopologize, UV, bake, and create three LODs**

Use one skeleton and bind pose for all LODs. Target 35k–50k, <=22k, and <=8k triangles. Pack body, face, scarf, goggles, and fittings into one 2K UV set; bake tangent-space normal, AO, and curvature into the specified texture files.

Expected: no non-manifold geometry, no weighted vertex with more than four influences, no visible scarf/body or goggles/face intersection in neutral pose.

- [ ] **Step 3: Rig the required bones and author the animation clips**

Required bones: root, pelvis, spine, chest, neck, head, jaw/beak, eyes, eyelids, left/right wing chains, left/right leg and foot chains, three scarf bones, goggles auxiliary bone, and the four socket transforms `Socket_Head`, `Socket_Face`, `Socket_Neck`, `Socket_Back`.

Author clips named exactly: `Idle`, `Run`, `LaneLeft`, `LaneRight`, `Jump`, `Fall`, `Land`, `Slide`, `Hurt`, `Defeat`, `Celebrate`, `FishPickup`, `ShieldActivate`, `DashActivate`, `MagnetActivate`, `BossCounter`. Make locomotion clips in-place and ensure the first/last frames of looping clips match.

- [ ] **Step 4: Add deterministic model import settings**

```csharp
internal sealed class RunnerAvatarModelImportProcessor : AssetPostprocessor
{
    private const string Root = "Assets/Resources/PenguinRun/Characters/Gugugaga/";
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = assetPath.EndsWith("gugugaga_avatar.fbx");
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        importer.importBlendShapes = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }
}
```

- [ ] **Step 5: Import in Unity and run the model audit**

Run Unity batch import, then run `ProjectRunnerAvatarValidation.ValidateFromCommandLine` after Task 5 creates the definition. Before that, inspect the imported FBX in Unity and confirm clip names, bone hierarchy, normals, bounds, and LOD triangle counts match the source.

- [ ] **Step 6: Commit source and runtime art separately**

```powershell
git add Artifacts/CharacterSource/Gugugaga Assets/Resources/PenguinRun/Characters/Gugugaga Assets/Scripts/Editor/PenguinRun/RunnerAvatarModelImportProcessor.cs
git commit -m "art: add high-quality rigged Gugugaga avatar"
```

## Task 5: Build the Animator, appearance, sockets, prefab, and definition

**Files:**
- Create: `Assets/Scripts/Editor/PenguinRun/RunnerAvatarAssetBuilder.cs`
- Generate: `Assets/Resources/PenguinRun/Characters/Gugugaga/GugugagaBase.controller`
- Generate: `Assets/Resources/PenguinRun/Characters/Gugugaga/GugugagaAnimations.asset`
- Generate: `Assets/Resources/PenguinRun/Characters/Gugugaga/GugugagaAppearance.asset`
- Generate: `Assets/Resources/PenguinRun/Characters/Gugugaga/GugugagaSockets.asset`
- Generate: `Assets/Resources/PenguinRun/Characters/Gugugaga/GugugagaAvatar.prefab`
- Generate: `Assets/Resources/PenguinRun/Characters/Gugugaga/GugugagaAvatar.asset`

- [ ] **Step 1: Implement an idempotent asset builder**

The builder must load all clips by exact name, create three Animator layers (`Base`, `Actions`, `Expression`), add parameters `Running`, `Grounded`, `Sliding`, `GameOver`, `Speed01`, `LaneLean`, `VerticalSpeed`, `Hurt`, `Celebrate`, `PerfectDodge`, `Interaction`, and `PlayInteraction`, and create transitions with no exit time for gameplay-critical states.

```csharp
[MenuItem("PenguinRun/Avatar/Build Gugugaga Avatar")]
public static void Build()
{
    var clips = LoadClips(ModelPath);
    RequireClipNames(clips, RequiredClipNames);
    var controller = BuildController(clips);
    var prefab = BuildPrefab(controller);
    var animations = BuildAnimationSet(clips);
    var appearance = BuildAppearanceSet();
    var sockets = BuildSocketSet();
    BuildDefinition(prefab, controller, animations, appearance, sockets);
    AssetDatabase.SaveAssets();
    ProjectRunnerAvatarValidation.ThrowIfInvalid();
}
```

- [ ] **Step 2: Configure the prefab and LODGroup**

The prefab root contains `Animator`, `RunnerExpressionDriver`, `RunnerSecondaryMotion`, and `LODGroup`. Use screen-relative thresholds `0.18`, `0.055`, and `0.012`; do not add a cull-only final LOD. Set `Animator.applyRootMotion = false`, culling mode `AlwaysAnimate`, and update mode `Normal`.

- [ ] **Step 3: Run the builder and validation in batch mode**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.RunnerAvatarAssetBuilder.BuildFromCommandLine -logFile _tmp/avatar-build.log
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectRunnerAvatarValidation.ValidateFromCommandLine -logFile _tmp/avatar-validation.log
```

Expected: both exit `0`; validation log contains `[ProjectRunnerAvatarValidation] PASS`.

- [ ] **Step 4: Commit builder and generated assets**

```powershell
git add Assets/Scripts/Editor/PenguinRun/RunnerAvatarAssetBuilder.cs Assets/Resources/PenguinRun/Characters/Gugugaga
git commit -m "feat: build Gugugaga avatar data assets"
```

## Task 6: Implement the runtime presenter and animation modules

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/PlayerAvatarPresenter.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerExpressionDriver.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerSecondaryMotion.cs`

- [ ] **Step 1: Implement the animation driver with cached hashes**

```csharp
internal sealed class RunnerAnimationDriver
{
    private static readonly int Running = Animator.StringToHash("Running");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int Sliding = Animator.StringToHash("Sliding");
    private static readonly int GameOver = Animator.StringToHash("GameOver");
    private static readonly int Speed01 = Animator.StringToHash("Speed01");
    private static readonly int LaneLean = Animator.StringToHash("LaneLean");
    private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Celebrate = Animator.StringToHash("Celebrate");
    private static readonly int PerfectDodge = Animator.StringToHash("PerfectDodge");
    private static readonly int Interaction = Animator.StringToHash("Interaction");
    private static readonly int PlayInteraction = Animator.StringToHash("PlayInteraction");
    private readonly Animator animator;
    public RunnerAnimationDriver(Animator animator) { this.animator = animator; animator.applyRootMotion = false; }
    public void Tick(in RunnerAvatarState s)
    {
        animator.SetBool(Running, s.Running); animator.SetBool(Grounded, s.Grounded);
        animator.SetBool(Sliding, s.Sliding); animator.SetBool(GameOver, s.GameOver);
        animator.SetFloat(Speed01, s.Speed01, 0.08f, Time.deltaTime);
        animator.SetFloat(LaneLean, s.LaneLean, 0.06f, Time.deltaTime);
        animator.SetFloat(VerticalSpeed, s.VerticalVelocity);
    }
    public void TriggerHurt() => animator.SetTrigger(Hurt);
    public void TriggerCelebrate() => animator.SetTrigger(Celebrate);
    public void TriggerPerfectDodge() => animator.SetTrigger(PerfectDodge);
    public void TriggerInteraction(RunnerAvatarInteraction value) { animator.SetInteger(Interaction, (int)value); animator.SetTrigger(PlayInteraction); }
}
```

- [ ] **Step 2: Implement expression and secondary motion with zero per-frame allocations**

Cache eyelid blend-shape indices and eye/scarf/goggles transforms during bind. Blink uses a deterministic timer with a short close/open curve; look direction follows lane lean. Scarf bones use critically damped local rotations derived from speed, lane lean, and vertical velocity. Expose `SetQuality(bool secondaryMotionEnabled)` and reset every cached transform to bind pose when disabled.

- [ ] **Step 3: Implement the presenter and its fallback**

```csharp
public static IPlayerCharacterPresentation Create(Transform parent, RunnerAvatarDefinition definition)
{
    if (definition == null || definition.ModelPrefab == null)
        return PenguinCharacter.CreateCute(parent);
    try { return new PlayerAvatarPresenter(parent, definition); }
    catch (System.Exception ex)
    {
        Debug.LogWarning($"Avatar '{definition.AvatarId}' failed; using procedural fallback. {ex.Message}");
        return PenguinCharacter.CreateCute(parent);
    }
}
```

The constructor instantiates the prefab, applies definition transform, builds an `AnimatorOverrideController` from `RunnerAnimationSet.Overrides`, applies appearance materials by renderer path, resolves sockets once, and creates animation/expression/secondary-motion/FX modules. `Tick` forwards one state snapshot to each module.

- [ ] **Step 4: Compile and run the static gate**

Run: `powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1`

Expected: `COMPILE OK`.

Run: `powershell -ExecutionPolicy Bypass -File tools/VerifyProtagonistAvatarRevamp.ps1`

Expected: presenter/root-motion/fallback contracts pass; only missing integration or FX failures remain.

- [ ] **Step 5: Commit the presenter modules**

```powershell
git add Assets/Scripts/Runtime/PenguinRun/Avatar
git commit -m "feat: add modular player avatar presenter"
```

## Task 7: Extract character FX and preserve every existing feedback path

**Files:**
- Create: `Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerCharacterFx.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinCharacter.cs`

- [ ] **Step 1: Move FX ownership into a model-independent module**

Recreate shield aura, dash trail, magnet halo, invulnerability ring, jump/landing ring, slide trail, power aura, dizzy stars, and three buff motes under a dedicated `Character FX` child of the player root. Reuse `RunnerVisuals.CreatePrimitive` and cached renderers/materials; do not use `Renderer.material` inside `Tick` after construction.

```csharp
public void Tick(float dt, in RunnerAvatarState state)
{
    shieldAura.SetActive(state.Has(RunnerAvatarBuffFlags.Shield));
    dashTrail.SetActive(state.Has(RunnerAvatarBuffFlags.Dash));
    magnetHalo.SetActive(state.Has(RunnerAvatarBuffFlags.Magnet));
    invulnerableRing.SetActive(state.Has(RunnerAvatarBuffFlags.Invulnerable));
    slideTrail.SetActive(state.Sliding);
    dizzyRoot.SetActive(state.GameOver);
    // Update cached transforms and MaterialPropertyBlocks only.
}
```

- [ ] **Step 2: Add trigger methods without animation authority**

`TriggerHurt`, `TriggerPerfectDodge`, and `TriggerCelebration` update only FX timers. Animation triggers remain in `RunnerAnimationDriver`. `TriggerInteraction` emits a short color burst based on the interaction ID.

- [ ] **Step 3: Keep the procedural fallback behavior stable**

Leave the existing procedural visuals intact until integration is verified. Only replace duplicated buff flag assembly with `RunnerAvatarState.Has` inside the compatibility `Tick` method.

- [ ] **Step 4: Compile and commit**

Run: `powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1`

Expected: `COMPILE OK`.

```powershell
git add Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerCharacterFx.cs Assets/Scripts/Runtime/PenguinRun/PenguinCharacter.cs
git commit -m "refactor: separate player character feedback effects"
```

## Task 8: Integrate the presenter without changing gameplay judgment

**Files:**
- Modify: `Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`

- [ ] **Step 1: Change both character fields and constructor arguments to the interface**

Replace `PenguinCharacter` fields/arguments with `IPlayerCharacterPresentation`. Do not change `PlayerMotor`, collision, obstacle, or Boss method signatures.

- [ ] **Step 2: Centralize snapshot construction in `RunnerGameController`**

```csharp
private RunnerAvatarState CaptureAvatarState(bool running, bool gameOver, float speed01)
{
    var buffs = RunnerAvatarBuffFlags.None;
    if (world.DashTimer > 0f) buffs |= RunnerAvatarBuffFlags.Dash;
    if (world.MagnetTimer > 0f) buffs |= RunnerAvatarBuffFlags.Magnet;
    if (world.ShieldTimer > 0f) buffs |= RunnerAvatarBuffFlags.Shield;
    if (world.HitInvulnerabilityTimer > 0f) buffs |= RunnerAvatarBuffFlags.Invulnerable;
    if (world.GlideTimer > 0f) buffs |= RunnerAvatarBuffFlags.Glide;
    if (world.ScoreBoostTimer > 0f) buffs |= RunnerAvatarBuffFlags.ScoreBoost;
    if (world.SlowMoTimer > 0f) buffs |= RunnerAvatarBuffFlags.SlowMo;
    if (world.DoubleFishTimer > 0f) buffs |= RunnerAvatarBuffFlags.DoubleFish;
    if (world.IceMirrorTimer > 0f) buffs |= RunnerAvatarBuffFlags.IceMirror;
    if (world.AuroraChainTimer > 0f) buffs |= RunnerAvatarBuffFlags.AuroraChain;
    if (world.FogLanternTimer > 0f) buffs |= RunnerAvatarBuffFlags.FogLantern;
    if (world.TreantArmorHits > 0) buffs |= RunnerAvatarBuffFlags.TreantArmor;
    if (world.CoralBounceReady) buffs |= RunnerAvatarBuffFlags.CoralBounce;
    if (world.ThunderFeatherTimer > 0f) buffs |= RunnerAvatarBuffFlags.ThunderFeather;
    if (world.FishBombs > 0) buffs |= RunnerAvatarBuffFlags.FishBomb;
    return new RunnerAvatarState(running, motor.Grounded, motor.SlideTimer > 0f, gameOver,
        speed01, motor.LaneLean01, motor.VerticalVelocity, buffs);
}
```

Replace every `character.Animate(...)` and `SetAuxBuffState(world)` call with `character.Tick(dt, CaptureAvatarState(...))`. Route existing hurt, perfect-dodge, and celebration calls through the interface; route pickup/power-up/Boss-counter events to `TriggerInteraction` at their existing gameplay trigger sites.

- [ ] **Step 3: Replace the creation entry point and preserve fallback**

```csharp
var definition = Resources.Load<RunnerAvatarDefinition>("PenguinRun/Characters/Gugugaga/GugugagaAvatar");
character = PlayerAvatarPresenter.Create(player.transform, definition);
```

Keep the outer `player` GameObject, its scale, visibility control, and `PlayerMotor` binding unchanged.

- [ ] **Step 4: Add static assertions that judgment code is untouched**

Extend `VerifyProtagonistAvatarRevamp.ps1` to require `BodyHeight`, `StandingBodyHeight`, and `SlidingBodyHeight` in `PlayerMotor`, and to forbid `applyRootMotion = true` under the avatar runtime directory.

- [ ] **Step 5: Run all compile and static gates**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyProtagonistAvatarRevamp.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyVisualJudgmentRevamp.ps1
```

Expected: `COMPILE OK`; all three commands exit `0` and print `PASS`.

- [ ] **Step 6: Commit the integration**

```powershell
git add Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs tools/VerifyProtagonistAvatarRevamp.ps1
git commit -m "feat: integrate data-driven protagonist avatar"
```

## Task 9: Expand preview, runtime, and performance verification

**Files:**
- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectPenguinPreviewCapture.cs`
- Modify: `Assets/Editor/ProjectPlayerJourneySmokeTest.cs`
- Modify: `docs/CURRENT_DESIGN.md`
- Modify: `CONTEXT.md`

- [ ] **Step 1: Capture all required poses through the real presenter**

Update preview creation to load `GugugagaAvatar`, call `PlayerAvatarPresenter.Create`, and capture `front`, `back`, `side`, `run`, `lane`, `jump`, `slide`, `hurt`, `defeat`, and `celebrate` images at 960×720. Drive each pose for 120 frames with deterministic `RunnerAvatarState` inputs and triggers.

- [ ] **Step 2: Add smoke assertions for the real model and fallback**

In `ProjectPlayerJourneySmokeTest`, assert that the runtime player contains an `Animator`, its `applyRootMotion` is false, its active renderer count is nonzero, and no avatar error is logged. Add a separate editor validation method that calls `PlayerAvatarPresenter.Create(parent, null)` and asserts the returned root contains the procedural `Cute Penguin` child.

- [ ] **Step 3: Run Unity validation, preview capture, and the journey smoke test**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectRunnerAvatarValidation.ValidateFromCommandLine -logFile _tmp/avatar-validation.log
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectPenguinPreviewCapture.CaptureAllFromCommandLine -logFile _tmp/avatar-preview.log
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -projectPath . -executeMethod ProjectPlayerJourneySmokeTest.RunFromCommandLine -logFile _tmp/avatar-journey.log
```

Expected: validation exits `0`; ten non-empty PNGs appear in `Artifacts/PenguinPreviews`; the journey log contains `PASS` and produces the existing five journey screenshots.

- [ ] **Step 4: Perform the visual review in three contrasting themes**

Review Ice Lake, Ocean Reef, and Sky Flight screenshots for face readability, goggle highlights, scarf silhouette, feet contact, clipping, and LOD popping. Reject the build if a gameplay pose is ambiguous or if the goggles/scarf merge into the background at runner distance.

- [ ] **Step 5: Profile the Android medium-quality path**

Use the Unity Profiler on the signed test APK for a five-minute run. Acceptance: 60 FPS target on the existing test device class, <=3 avatar material batches at normal distance, no `GC.Alloc` from avatar scripts during steady running, and no visible LOD pop between LOD0/LOD1. Record measured results in `docs/CURRENT_DESIGN.md`; if no device is connected, record the device pass as explicitly pending rather than claiming success.

- [ ] **Step 6: Update project documentation**

Add the final resource paths, `RunnerAvatarState` boundary, fallback rule, Animator/root-motion constraint, triangle/texture budgets, and validation commands to `CONTEXT.md` and `docs/CURRENT_DESIGN.md`.

- [ ] **Step 7: Run the complete release gate set**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyProtagonistAvatarRevamp.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyVisualJudgmentRevamp.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyPlayerExperienceAudit.ps1
powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1
git diff --check
```

Expected: every script exits `0`, compile prints `COMPILE OK`, and `git diff --check` prints nothing.

- [ ] **Step 8: Commit previews, smoke coverage, and documentation**

```powershell
git add Assets/Scripts/Editor/PenguinRun/ProjectPenguinPreviewCapture.cs Assets/Editor/ProjectPlayerJourneySmokeTest.cs Artifacts/PenguinPreviews CONTEXT.md docs/CURRENT_DESIGN.md
git commit -m "test: verify protagonist avatar presentation"
```

## Task 10: Final regression and delivery audit

**Files:**
- Review only; modify earlier task files only when a regression is found.

- [ ] **Step 1: Verify the working tree contains no accidental generated files**

Run: `git status --short`

Expected: only intentional Unity `.meta`, art, code, preview, and documentation files appear; `_tmp`, `Library`, and local Blender autosaves are absent.

- [ ] **Step 2: Verify spec coverage manually**

Confirm each requirement in `docs/superpowers/specs/2026-07-22-protagonist-avatar-redesign-design.md` maps to a delivered asset, class, validation, screenshot, or explicit device-pending note. Confirm extra skins, player-facing wardrobe UI, root-motion gameplay, real-time fur, refraction, and Boss/companion migration were not added.

- [ ] **Step 3: Run the final gates from a clean Unity editor state**

Repeat Task 9 Step 7, then open the game and manually execute: countdown, run, both lane changes, jump, air-to-slide, shield, dash, magnet, hurt, Boss counter, death, retry, pause/resume, result, and return to lobby.

Expected: no state lock, no visual/physics disagreement, no stale FX after retry, and no new Console exception.

- [ ] **Step 4: Commit any audit-only corrections**

If corrections were required, stage only those files and commit:

```powershell
git commit -m "fix: close protagonist avatar release regressions"
```

If no corrections were required, do not create an empty commit.
