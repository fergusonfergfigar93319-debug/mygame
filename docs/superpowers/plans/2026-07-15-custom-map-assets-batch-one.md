# Custom Map Assets Batch One Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a catalog-driven visual-asset pipeline and ship the first custom set: 18 theme-specific obstacles, 6 pickups, and 24 environment props without changing collision or gameplay behavior.

**Architecture:** `RunnerVisualAssetCatalog` derives stable prefab resource paths from theme/type keys; `RunnerVisualFactory` owns cached loading, attachment, reset, and fallback. Editor tooling converts independently generated LOD0/LOD1 model sources into validated runtime prefabs, while `SegmentSpawner` and scenery code retain gameplay placement and use the old procedural meshes only as explicit final fallback.

**Tech Stack:** Unity 6.0.4, C#, Built-in Render Pipeline, FBX source models, Unity Prefabs and `LODGroup`, hand-painted PBR textures, `Resources` runtime loading, existing object pools, PowerShell static contracts, Unity editor validation.

---

## File structure

**Create**

- `tools/VerifyCustomMapAssetsBatch1.ps1` — catalog and first-batch coverage gate.
- `Assets/Scripts/Runtime/PenguinRun/Visuals/PickupVisualId.cs` — six first-batch pickup identities, including ordinary fish snacks.
- `Assets/Scripts/Runtime/PenguinRun/Visuals/EnvironmentPropId.cs` — four first-batch environment roles and future stable IDs.
- `Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualAssetCatalog.cs` — stable keys, paths, offsets, and fallback descriptors.
- `Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualFactory.cs` — cache, instantiate, reset, and missing-asset log-once behavior.
- `Assets/Scripts/Editor/PenguinRun/PenguinVisualPrefabBuilder.cs` — build LOD prefabs from source FBX pairs.
- `Assets/Scripts/Editor/PenguinRun/ProjectVisualAssetValidation.cs` — coverage, bounds, material, LOD, and collision-readability checks.
- `Assets/Art/PenguinRun/Obstacles/{IceLake,Cedar,Aurora,Mist,Ocean,Sky}/` — 18 custom LOD0/LOD1 obstacle source pairs and textures.
- `Assets/Art/PenguinRun/Pickups/` — 6 custom LOD0/LOD1 pickup source pairs and textures.
- `Assets/Art/PenguinRun/Environment/{IceLake,Cedar,Aurora,Mist,Ocean,Sky}/` — 24 custom LOD0/LOD1 environment source pairs and textures.
- `Assets/Resources/PenguinRun/Prefabs/Obstacles/{ice_lake,cedar,aurora,mist,ocean,sky}/` — generated runtime obstacle prefabs.
- `Assets/Resources/PenguinRun/Prefabs/Pickups/` — generated runtime pickup prefabs.
- `Assets/Resources/PenguinRun/Prefabs/Environment/{ice_lake,cedar,aurora,mist,ocean,sky}/` — generated runtime environment prefabs.

**Modify**

- `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs` — request custom obstacle and pickup visuals before existing fallbacks.
- `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs` — request custom environment assets before procedural landmarks.
- `Assets/Scripts/Runtime/PenguinRun/ThemePresentationProfile.cs` — map landmark recipes to stable `EnvironmentPropId` values.
- `Assets/Scripts/Runtime/PenguinRun/PowerUpVisualProfile.cs` — keep color/halo behavior as fallback and accent only.
- `tools/VerifyUiArtP0.ps1` — require the catalog/factory without removing existing mobile gates.
- `docs/CURRENT_DESIGN.md` and `docs/UI_ART_REVAMP_PLAN.md` — record actual asset coverage and runtime evidence.

## First-batch model matrix

Each entry requires an independently authored `*_lod0.fbx` and `*_lod1.fbx`. LOD0 is the detailed handcrafted asset; LOD1 preserves the same silhouette at roughly 20–35% of LOD0 triangles.

| Group | Stable IDs |
| --- | --- |
| Obstacles per theme | `standard_high`, `slide_gate`, `wide_wall` |
| Pickups | `fish_snack`, `dash`, `magnet`, `shield`, `score_star`, `glide_feather` |
| Environment per theme | `landmark`, `signpost`, `light`, `edge_prop` |
| Themes | `ice_lake`, `cedar`, `aurora`, `mist`, `ocean`, `sky` |

## Model production standard

- One Unity unit equals one meter; pivot is centered on X/Z and placed at the lowest contact point on Y.
- Forward is +Z, up is +Y, scale is `(1,1,1)`, and transforms are applied before export.
- No text, letters, logos, baked ground plane, camera, or light objects.
- LOD0 budgets: obstacle 6k–18k triangles, pickup 2k–7k, landmark 8k–22k, small environment prop 2k–10k.
- LOD1 budgets: 20–35% of matching LOD0 triangles; preserve openings, dangerous height, and primary silhouette.
- Textures use hand-painted albedo, normal, and packed metallic/roughness/occlusion where available; transparent materials are limited to ice, glass, bubbles, and thin cloth edges.
- Models must look authored through structure and surface detail, not through color tint alone.

---

### Task 1: Lock catalog and first-batch coverage

**Files:**

- Create: `tools/VerifyCustomMapAssetsBatch1.ps1`
- Test: `tools/VerifyUiArtP0.ps1`
- Test: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Run the inherited art baseline**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
```

Expected: `P0 UI/art static contracts passed.`

- [ ] **Step 2: Create the first-batch verifier**

Create `tools/VerifyCustomMapAssetsBatch1.ps1` with:

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
    if ($null -eq $content -or $content -notmatch $pattern) { $failures.Add("Missing contract: $description") }
}

$catalog = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualAssetCatalog.cs'
$factory = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualFactory.cs'
$pickupIds = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Visuals/PickupVisualId.cs'
$propIds = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Visuals/EnvironmentPropId.cs'
foreach ($method in 'ForObstacle','ForPickup','ForEnvironment','AllBatchOneObstacleKeys','AllBatchOnePickupKeys','AllBatchOneEnvironmentKeys') {
    Require $catalog "\b$method\b" "RunnerVisualAssetCatalog.$method"
}
foreach ($method in 'TryAttachObstacle','TryAttachPickup','TryAttachEnvironment','ClearAttachment') {
    Require $factory "\b$method\s*\(" "RunnerVisualFactory.$method"
}
foreach ($id in 'FishSnack','Dash','Magnet','Shield','ScoreStar','GlideFeather') { Require $pickupIds "\b$id\b" "PickupVisualId.$id" }
foreach ($id in 'Landmark','Signpost','Light','EdgeProp') { Require $propIds "\b$id\b" "EnvironmentPropId.$id" }

$spawner = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
Require $spawner 'visualFactory\.TryAttachObstacle\(' 'SegmentSpawner custom obstacle path'
Require $spawner 'visualFactory\.TryAttachPickup\(' 'SegmentSpawner custom pickup path'
$runner = Read-Required 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
Require $runner 'visualFactory\.TryAttachEnvironment\(' 'runner custom environment path'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}
Write-Output 'Custom map asset batch-one static contracts passed.'
```

- [ ] **Step 3: Verify red state**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyCustomMapAssetsBatch1.ps1
```

Expected: FAIL for the three missing runtime files and the missing integration calls.

- [ ] **Step 4: Commit the verifier**

```powershell
git add -- tools/VerifyCustomMapAssetsBatch1.ps1
git diff --cached --check
git commit -m "test: lock custom map asset batch one"
```

---

### Task 2: Define stable visual keys and descriptors

**Files:**

- Create: `Assets/Scripts/Runtime/PenguinRun/Visuals/EnvironmentPropId.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Visuals/PickupVisualId.cs`
- Create: `Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualAssetCatalog.cs`
- Test: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Create environment role IDs**

Create:

```csharp
namespace PenguinRun
{
    public enum EnvironmentPropId
    {
        Landmark,
        Signpost,
        Light,
        EdgeProp,
    }
}
```

Create `PickupVisualId.cs`:

```csharp
namespace PenguinRun
{
    public enum PickupVisualId
    {
        FishSnack,
        Dash,
        Magnet,
        Shield,
        ScoreStar,
        GlideFeather,
    }
}
```

- [ ] **Step 2: Define an immutable descriptor**

Add to `RunnerVisualAssetCatalog.cs`:

```csharp
public readonly struct RunnerVisualDescriptor
{
    public readonly string ResourcePath;
    public readonly Vector3 LocalPosition;
    public readonly Vector3 LocalEuler;
    public readonly Vector3 LocalScale;
    public readonly string FallbackResourcePath;

    public RunnerVisualDescriptor(string resourcePath, Vector3 position, Vector3 euler,
        Vector3 scale, string fallbackResourcePath = "")
    {
        ResourcePath = resourcePath ?? string.Empty;
        LocalPosition = position;
        LocalEuler = euler;
        LocalScale = scale;
        FallbackResourcePath = fallbackResourcePath ?? string.Empty;
    }
}
```

- [ ] **Step 3: Implement deterministic path derivation**

`RunnerVisualAssetCatalog` must expose:

```csharp
public static RunnerVisualDescriptor ForObstacle(RunnerMapTheme theme, ObstacleKind kind);
public static RunnerVisualDescriptor ForPickup(PickupVisualId id);
public static RunnerVisualDescriptor ForEnvironment(RunnerMapTheme theme, EnvironmentPropId id);
public static IEnumerable<(RunnerMapTheme theme, ObstacleKind kind)> AllBatchOneObstacleKeys();
public static IEnumerable<PickupVisualId> AllBatchOnePickupKeys();
public static IEnumerable<(RunnerMapTheme theme, EnvironmentPropId id)> AllBatchOneEnvironmentKeys();
public static bool TryGetPickupId(PowerUpKind kind, out PickupVisualId id);
```

Use exact theme slugs:

```csharp
IceLakeEcho => "ice_lake", CedarRuins => "cedar", AuroraField => "aurora",
MistDike => "mist", OceanReef => "ocean", SkyFlight => "sky"
```

Use obstacle slugs only for `StandardHigh`, `SlideGate`, and `WideWall`. Unsupported obstacle kinds return an empty descriptor so the existing fallback remains authoritative. Pickup slugs cover the six first-batch kinds. Resource paths follow:

```text
PenguinRun/Prefabs/Obstacles/ice_lake/slide_gate
PenguinRun/Prefabs/Pickups/glide_feather
PenguinRun/Prefabs/Environment/ocean/landmark
```

Obstacle position/scale is keyed by gameplay kind, not by theme. `SlideGate` uses `localPosition=(0,-0.24,0)` and `localScale=(1,1,1)`; `StandardHigh` uses `(0,-0.55,0)`; `WideWall` uses `(0,-0.55,0)`. Pickup and environment descriptors use zero position/euler and unit scale.

- [ ] **Step 4: Run the verifier and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyCustomMapAssetsBatch1.ps1
git add -- Assets/Scripts/Runtime/PenguinRun/Visuals/EnvironmentPropId.cs Assets/Scripts/Runtime/PenguinRun/Visuals/PickupVisualId.cs Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualAssetCatalog.cs
git diff --cached --check
git commit -m "feat: add runner visual asset catalog"
```

Expected: catalog/ID failures disappear; factory and integration failures remain.

---

### Task 3: Implement cached attachment and explicit fallback

**Files:**

- Create: `Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualFactory.cs`
- Test: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Implement the factory API**

Create a `public sealed class RunnerVisualFactory` with one `Dictionary<string, GameObject>` cache and one `HashSet<string>` missing-log set. Its public surface is:

```csharp
public bool TryAttachObstacle(Transform root, RunnerMapTheme theme, ObstacleKind kind);
public bool TryAttachPickup(Transform root, PickupVisualId id);
public bool TryAttachEnvironment(Transform root, RunnerMapTheme theme, EnvironmentPropId id);
public void ClearAttachment(Transform root);
```

All three attach methods delegate to one private `TryAttach(Transform, RunnerVisualDescriptor)` method.

- [ ] **Step 2: Implement deterministic child ownership**

`ClearAttachment` destroys only a direct child named `CustomVisual`. `TryAttach` clears the previous attachment, loads `descriptor.ResourcePath`, tries `FallbackResourcePath` when present, logs one warning per missing exact path, and returns false when neither loads. On success it instantiates under `root`, names the instance `CustomVisual`, and applies descriptor position, euler, and scale.

Use `Resources.Load<GameObject>` and cache null results as well as successful prefabs. Never call `renderer.material`; imported prefab materials remain shared.

- [ ] **Step 3: Verify and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyCustomMapAssetsBatch1.ps1
git add -- Assets/Scripts/Runtime/PenguinRun/Visuals/RunnerVisualFactory.cs
git diff --cached --check
git commit -m "feat: add cached runner visual factory"
```

---

### Task 4: Build the editor prefab pipeline and validator

**Files:**

- Create: `Assets/Scripts/Editor/PenguinRun/PenguinVisualPrefabBuilder.cs`
- Create: `Assets/Scripts/Editor/PenguinRun/ProjectVisualAssetValidation.cs`

- [ ] **Step 1: Build prefabs from LOD source pairs**

Add a menu item `PenguinRun/Visuals/Build Batch One Prefabs`. For every catalog key, compute source paths under `Assets/Art/PenguinRun`, load `<name>_lod0.fbx` and `<name>_lod1.fbx`, instantiate both under a new root, and add `LODGroup` with transitions `0.60`, `0.22`, and cull at `0.03`. Disable colliders found inside imported models. Save to the exact `Assets/Resources/PenguinRun/Prefabs` path matching the catalog.

The builder must fail with a collected list of missing LOD pairs rather than creating partial prefabs silently.

- [ ] **Step 2: Validate runtime prefab structure**

Add `PenguinRun/Visuals/Validate Batch One` and a public `RunNow(out string report)`. For every catalog key require:

```text
Prefab resolves through Resources.
Exactly one LODGroup exists.
LOD0 and LOD1 each contain at least one Renderer.
No Collider exists below the visual prefab root.
No renderer uses the built-in error material.
Bounds are finite and nonzero.
Obstacle bounds do not make the recommended opening narrower or lower than its ObstacleColliderSpec.
```

Write `PASS: custom map asset batch one is valid.` or a newline-delimited failure report.

- [ ] **Step 3: Commit tooling before source assets**

```powershell
git add -- Assets/Scripts/Editor/PenguinRun/PenguinVisualPrefabBuilder.cs Assets/Scripts/Editor/PenguinRun/ProjectVisualAssetValidation.cs
git diff --cached --check
git commit -m "tool: add runner visual prefab pipeline"
```

---

### Task 5: Produce the six pickup models

**Files:**

- Create: `Assets/Art/PenguinRun/Pickups/*_lod0.fbx`
- Create: `Assets/Art/PenguinRun/Pickups/*_lod1.fbx`
- Create: `Assets/Art/PenguinRun/Pickups/Textures/*`

- [ ] **Step 1: Probe and select the 3D production path**

At execution time, load the installed 3D-generation skill and run its credential probe. Record only `SET/MISSING` markers. If generation is unavailable, stop the asset-production tasks and report them blocked; catalog/factory work may continue, but batch-one visual completion may not be claimed.

- [ ] **Step 2: Generate each pickup as an independent model**

Use the shared model standard plus these exact briefs, producing both LOD0 and LOD1:

```text
fish_snack — small golden dried-fish charm tied with coral thread, carved scale marks, edible but iconic, no coin shape.
dash — compact wooden-and-brass sled rocket shaped like a forward fish, three coral chevrons, canvas fins.
magnet — horseshoe navigation compass made from weathered copper and blue enamel, clear two-pole silhouette.
shield — round stitched hide-and-wood guardian buckler with a penguin-wing crest, warm gold rim.
score_star — five-point carved amber wayfinder star with inset pine-green compass rose, thick readable silhouette.
glide_feather — long layered white-and-blue storm feather charm with leather binding and a small brass ring.
```

- [ ] **Step 3: Inspect and commit pickup sources**

Verify pivots, orientation, triangle budgets, transparent backgrounds in texture sources, and absence of text. Commit only the pickup source folders:

```powershell
git add -- Assets/Art/PenguinRun/Pickups
git diff --cached --name-status
git commit -m "art: add handcrafted pickup model set"
```

---

### Task 6: Produce the ice-lake and cedar theme packs

**Files:**

- Create: `Assets/Art/PenguinRun/Obstacles/IceLake/*`
- Create: `Assets/Art/PenguinRun/Environment/IceLake/*`
- Create: `Assets/Art/PenguinRun/Obstacles/Cedar/*`
- Create: `Assets/Art/PenguinRun/Environment/Cedar/*`

- [ ] **Step 1: Generate the ice-lake pack**

Produce LOD0/LOD1 pairs from these briefs:

```text
standard_high — layered blue ice pressure ridge with trapped air bubbles, chipped jumpable crown and clear low base.
slide_gate — transparent ice arch formed by two frozen slabs, hanging icicles above a generous central slide opening.
wide_wall — fractured glacier barricade spanning three lanes, stacked ice plates and a central echo-rune crack without text.
landmark — tall split ice tuning-fork monolith with internal bubbles and wind-cut base.
signpost — short frozen driftwood sign with abstract carved direction notch, no letters.
light — squat whale-oil lantern sealed inside cloudy ice, warm core and metal handle.
edge_prop — cluster of broken ice plates, snow crust and a half-buried rope anchor.
```

- [ ] **Step 2: Generate the cedar pack**

Produce LOD0/LOD1 pairs from these briefs:

```text
standard_high — chopped snow-covered cedar stump barricade with visible rings, wood wedges and broken branches.
slide_gate — heavy fallen cedar beam held by forked trunks, rope lashings and snow sag, clear slide opening.
wide_wall — ruined timber palisade with diagonal braces, missing boards and safe visual lane edges.
landmark — ancient split cedar guardian trunk wrapped in old expedition ropes and small blank wooden charms.
signpost — hand-carved cedar trail marker with three directional branches and no writing.
light — iron-caged amber lantern hanging from a bent cedar branch with leather binding.
edge_prop — rope coil, wood wedges, pine cones and a mossy broken bridge post under snow.
```

- [ ] **Step 3: Commit the two packs**

```powershell
git add -- Assets/Art/PenguinRun/Obstacles/IceLake Assets/Art/PenguinRun/Environment/IceLake Assets/Art/PenguinRun/Obstacles/Cedar Assets/Art/PenguinRun/Environment/Cedar
git diff --cached --name-status
git commit -m "art: add ice lake and cedar visual packs"
```

---

### Task 7: Produce the aurora and mist theme packs

**Files:**

- Create: `Assets/Art/PenguinRun/Obstacles/Aurora/*`
- Create: `Assets/Art/PenguinRun/Environment/Aurora/*`
- Create: `Assets/Art/PenguinRun/Obstacles/Mist/*`
- Create: `Assets/Art/PenguinRun/Environment/Mist/*`

- [ ] **Step 1: Generate the aurora pack**

```text
standard_high — dark magnetic stone core bound by offset copper rings and two small aurora crystal fins.
slide_gate — grounded copper induction arch with suspended magnetic bead and clear central slide opening.
wide_wall — three linked magnetic pylons with stone buttresses, copper coils and restrained emissive seams.
landmark — tall aurora conductor beacon with stacked copper rings, dark stone base and crystal crown.
signpost — magnetic compass post with abstract north needle, no text or numbers.
light — copper cage containing a floating green-violet aurora shard.
edge_prop — discarded coils, cracked magnet stones and insulated expedition cable.
```

- [ ] **Step 2: Generate the mist pack**

```text
standard_high — wet stacked dike stones with rusted brace, moss line and dripping top edge.
slide_gate — low floodgate frame with raised wooden sluice, iron chain and clear slide opening.
wide_wall — battered seawall section with angled supports, drainage holes and rust streaks.
landmark — crooked fog-watch tower base with large hooded lantern and wet stone steps.
signpost — short iron navigation vane on a wet stone plinth, abstract direction arrow only.
light — heavy brass fog lantern with cloudy glass, protective cage and hanging ring.
edge_prop — broken valve wheel, wet chain, sandbags and mossy drainage pipe.
```

- [ ] **Step 3: Commit the two packs**

```powershell
git add -- Assets/Art/PenguinRun/Obstacles/Aurora Assets/Art/PenguinRun/Environment/Aurora Assets/Art/PenguinRun/Obstacles/Mist Assets/Art/PenguinRun/Environment/Mist
git diff --cached --name-status
git commit -m "art: add aurora and mist visual packs"
```

---

### Task 8: Produce the ocean and sky theme packs

**Files:**

- Create: `Assets/Art/PenguinRun/Obstacles/Ocean/*`
- Create: `Assets/Art/PenguinRun/Environment/Ocean/*`
- Create: `Assets/Art/PenguinRun/Obstacles/Sky/*`
- Create: `Assets/Art/PenguinRun/Environment/Sky/*`

- [ ] **Step 1: Generate the ocean pack**

```text
standard_high — dense branching coral mound with shell armor, worn driftwood core and readable jumpable crown.
slide_gate — curved antler-coral arch bound to driftwood feet, hanging kelp tips above a clear opening.
wide_wall — reef barricade of broad coral fans, shell plates and shipwreck ribs spanning three lanes.
landmark — towering spiral shell shrine grown over an old mast and branching coral base.
signpost — driftwood current marker with shell arrow and no writing.
light — pearlescent shell lantern with a turquoise bioluminescent core and rope handle.
edge_prop — broken oar, small coral cluster, rope net and scattered shells.
```

- [ ] **Step 2: Generate the sky pack**

```text
standard_high — compact floating wreckage pile of pale wood, canvas bundle and cloud crystal anchor.
slide_gate — wind-bent sail frame with two floating wood supports and a high canvas crossbar.
wide_wall — crashed balloon gondola side with patched sail panels, ropes and cloud-crystal ballast.
landmark — tall wind shrine made from floating wood rings, feather vanes and a cloud-crystal core.
signpost — lightweight wind-direction post with layered feather arrows and no writing.
light — brass sky lantern suspended inside a small open balloon frame with cream canvas.
edge_prop — folded sailcloth, rope spool, loose feathers and a cracked cloud crystal.
```

- [ ] **Step 3: Commit the two packs**

```powershell
git add -- Assets/Art/PenguinRun/Obstacles/Ocean Assets/Art/PenguinRun/Environment/Ocean Assets/Art/PenguinRun/Obstacles/Sky Assets/Art/PenguinRun/Environment/Sky
git diff --cached --name-status
git commit -m "art: add ocean and sky visual packs"
```

---

### Task 9: Build and validate all first-batch prefabs

**Files:**

- Create: `Assets/Resources/PenguinRun/Prefabs/Obstacles/**/*.prefab`
- Create: `Assets/Resources/PenguinRun/Prefabs/Pickups/*.prefab`
- Create: `Assets/Resources/PenguinRun/Prefabs/Environment/**/*.prefab`
- Test: `Assets/Scripts/Editor/PenguinRun/ProjectVisualAssetValidation.cs`

- [ ] **Step 1: Run the prefab builder in a licensed editor**

Invoke `PenguinRun/Visuals/Build Batch One Prefabs`. Expected: 18 obstacle, 6 pickup, and 24 environment prefabs, each with LOD0/LOD1 renderers and no collider.

- [ ] **Step 2: Run visual validation**

Invoke `PenguinRun/Visuals/Validate Batch One`. Expected: `PASS: custom map asset batch one is valid.` Fix source scale, pivots, materials, or bounds and rebuild until the validator passes. If Unity licensing blocks the builder, record prefab production as blocked and do not manufacture prefab YAML by hand.

- [ ] **Step 3: Commit generated runtime prefabs**

```powershell
git add -- Assets/Resources/PenguinRun/Prefabs
git diff --cached --name-status
git commit -m "feat: build custom runner visual prefabs"
```

---

### Task 10: Integrate custom obstacles without changing collision

**Files:**

- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs`
- Test: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Add one factory owned by the spawner**

Add:

```csharp
private readonly RunnerVisualFactory visualFactory = new();
```

Do not remove `modelPrefabCache`, procedural builders, or primitive fallbacks in this task.

- [ ] **Step 2: Request the exact obstacle kind first**

In `SpawnStandardObstacle`, replace the current first visual decision with:

```csharp
var obstacleKind = lowBarrier ? ObstacleKind.SlideGate : ObstacleKind.StandardHigh;
var builtWithModel = visualFactory.TryAttachObstacle(obstacle.transform, config.MapTheme, obstacleKind);
if (!builtWithModel) builtWithModel = TryBuildOpenModelObstacle(obstacle.transform, lowBarrier, palette);
if (!builtWithModel) builtWithModel = TryBuildProceduralObstacle(obstacle.transform, lowBarrier, palette);
```

In `SpawnWideSnowWall`, try `ObstacleKind.WideWall` before `TryBuildOpenModelWideWall`. Leave `RegisterObstacle` calls and every `ObstacleColliderSpec` unchanged.

- [ ] **Step 3: Run the static gate and collision-source diff**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyCustomMapAssetsBatch1.ps1
git diff -- Assets/Scripts/Runtime/PenguinRun/ObstacleColliderSpec.cs
```

Expected: obstacle integration contract passes and the collider-spec diff is empty.

- [ ] **Step 4: Commit obstacle integration**

```powershell
git add -- Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs
git diff --cached --check
git commit -m "feat: attach custom theme obstacles"
```

---

### Task 11: Integrate custom pickup models with pool reset

**Files:**

- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PowerUpVisualProfile.cs`
- Test: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Attach the ordinary fish-snack model in the coin pool**

Change `BuildFishSnackVisual` from static to instance method so it can use `visualFactory`. Build the same `Fish Coin` root, then call:

```csharp
if (!visualFactory.TryAttachPickup(root.transform, PickupVisualId.FishSnack))
{
    var meshRoot = new GameObject("FishMesh");
    meshRoot.transform.SetParent(root.transform, false);
    AttachProceduralMesh(meshRoot.transform, ObstacleMeshBuilder.BuildFishSnack(),
        new Color(1f, 0.72f, 0.28f), "ProceduralFishSnack");
}
```

Keep the existing pickup glow and spin behavior outside the fallback branch so both custom and procedural fish snacks receive the same gameplay feedback.

- [ ] **Step 2: Attach a supported power-up model before procedural effects**

In `CreatePowerUpRoot`, call:

```csharp
var hasCustomVisual = RunnerVisualAssetCatalog.TryGetPickupId(kind, out var visualId)
    && visualFactory.TryAttachPickup(root.transform, visualId);
if (!hasCustomVisual)
{
    AddPowerUpEffects(root.transform, kind, coreColor, secondaryColor);
}
else
{
    AddPickupRecognitionRing(root.transform, coreColor, visualProfile.PulseScale);
}
```

Extract the existing common ring creation into `AddPickupRecognitionRing`. A custom pickup keeps only the low-profile recognition ring and pulse; it does not receive primitive geometry for the same item.

- [ ] **Step 3: Reset pooled attachments deterministically**

At the start of `ReturnPowerUp`, call `visualFactory.ClearAttachment(power.transform)`. Continue destroying effect children and preserving `Pickup Core`. Reset rotation and pulse configuration before returning to the pool.

- [ ] **Step 4: Verify and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyCustomMapAssetsBatch1.ps1
git add -- Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs Assets/Scripts/Runtime/PenguinRun/PowerUpVisualProfile.cs
git diff --cached --check
git commit -m "feat: attach custom pickup models"
```

---

### Task 12: Integrate custom environment props

**Files:**

- Modify: `Assets/Scripts/Runtime/PenguinRun/ThemePresentationProfile.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Test: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Map theme profiles to four environment roles**

Extend `ThemePresentationProfile` with four `EnvironmentPropId` fields named `Landmark`, `Signpost`, `Light`, and `EdgeProp`. Each theme uses those same semantic IDs; the catalog path makes its actual model theme-specific.

- [ ] **Step 2: Add a runner-owned factory**

Add `private readonly RunnerVisualFactory visualFactory = new();` to `PenguinRunnerGame`. In the existing scenery/landmark creation path, create an empty visual root and call:

```csharp
if (!visualFactory.TryAttachEnvironment(root.transform, theme, propId))
{
    BuildProceduralEnvironmentFallback(root.transform, profile, propId);
}
RegisterSceneryProp(root);
```

Spawn the landmark sparsely, and distribute signpost/light/edge props only beyond the existing `SceneryMinAbsX` corridor. Keep `ApplyViewCorridorFade` active for every registered root.

- [ ] **Step 3: Verify and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyCustomMapAssetsBatch1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
git add -- Assets/Scripts/Runtime/PenguinRun/ThemePresentationProfile.cs Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs tools/VerifyUiArtP0.ps1
git diff --cached --check
git commit -m "feat: attach custom theme environment props"
```

---

### Task 13: Add runtime fallback and reset regression checks

**Files:**

- Modify: `Assets/Scripts/Editor/PenguinRun/ProjectVisualAssetValidation.cs`
- Modify: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Test missing exact assets**

Add an editor-only test hook that resolves a deliberately absent resource path through `RunnerVisualFactory`. Assert it returns false, produces no `CustomVisual` child, and emits at most one warning across two attempts.

- [ ] **Step 2: Test pooled pickup reset**

Create a temporary root, attach `Dash`, clear it, attach `Shield`, and assert exactly one direct `CustomVisual` child exists and its prefab name/path matches `Shield`.

- [ ] **Step 3: Test collider independence**

For all 18 obstacle keys, instantiate the visual under a root with the existing marker/spec and assert no imported collider exists and the spec values equal the static `ObstacleColliderSpec` values before and after attachment.

- [ ] **Step 4: Commit regression coverage**

```powershell
git add -- Assets/Scripts/Editor/PenguinRun/ProjectVisualAssetValidation.cs tools/VerifyCustomMapAssetsBatch1.ps1
git diff --cached --check
git commit -m "test: cover visual fallback and pool reset"
```

---

### Task 14: Verify six themes and document actual coverage

**Files:**

- Modify: `docs/CURRENT_DESIGN.md`
- Modify: `docs/UI_ART_REVAMP_PLAN.md`
- Test: `tools/VerifyCustomMapAssetsBatch1.ps1`

- [ ] **Step 1: Run all static and editor gates**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyCustomMapAssetsBatch1.ps1
git diff --check
```

Run `ProjectVisualAssetValidation.RunNow` in a licensed Unity editor and require the exact pass message.

- [ ] **Step 2: Compile and build Android**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\dxh53\Desktop\mygame' -logFile 'C:\Users\dxh53\Desktop\mygame\_tmp\custom-map-assets-unity.log'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/BuildLatestAndroidApk.ps1
```

Record exit codes and artifact paths. A license failure remains a blocker.

- [ ] **Step 3: Capture the required visual matrix**

For each of six themes capture one screenshot containing a custom standard obstacle, slide gate, wide wall, pickup, landmark, and two environment props. Capture a second low-quality screenshot. Confirm the three-lane read corridor is unobstructed and the obstacle action is recognizable before collision range.

- [ ] **Step 4: Record performance evidence**

Record visible triangle count, draw calls, batches, active particles, and frame time for one normal segment and one dense segment on desktop plus an Android device or emulator. Report the device name and resolution.

- [ ] **Step 5: Update documentation and commit**

Document exact delivered counts (`18/54` obstacles, `6/18` pickups, `24/48` environment props), fallback behavior, screenshot paths, performance numbers, Unity/Android status, and the remaining catalog completion phase.

```powershell
git add -- docs/CURRENT_DESIGN.md docs/UI_ART_REVAMP_PLAN.md
git diff --cached --check
git commit -m "docs: record custom map asset batch one"
```

## Batch-one completion criteria

- Catalog coverage is exactly 18 custom obstacles, 6 custom pickups, and 24 custom environment props.
- Every runtime prefab has LOD0/LOD1, shared materials, finite bounds, and no imported collider.
- `SegmentSpawner` and `PenguinRunnerGame` request custom visuals before explicit old fallbacks.
- `ObstacleColliderSpec` and gameplay effects remain unchanged.
- Pooled pickups never retain the previous kind's model or effects.
- Six-theme normal and low-quality screenshots show distinct structure, not tint-only variants.
- Fresh static, editor, compile, Android, screenshot, and performance evidence exists or each blocked item is named precisely.

## Follow-up catalog boundary

This independently testable plan stops at the first production batch defined in the approved design. After its catalog, collision, pooling, LOD, screenshot, and performance evidence passes, a separate catalog-completion plan will produce the remaining 36 obstacle prefabs, 12 pickup prefabs, and 24 environment prefabs through the same proven pipeline. Those remaining assets are outside this batch-one completion claim.
