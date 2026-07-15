# Premium Storybook Home Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current lobby card composition with a premium, interactive storybook camp home while preserving all save, navigation, and run-start behavior.

**Architecture:** Keep Unity 6 Built-in Render Pipeline, runtime-built uGUI, and the existing `LobbyPanelRouter`. Add focused top-ribbon, journal-page, chapter-page, camp-scene, and hotspot components; `MainMenuBootstrap` assembles an immutable `LobbyViewModel`, delegates rendering, and remains the only owner of navigation commands.

**Tech Stack:** Unity 6.0.4, C#, uGUI, TextMeshPro, existing DOTween-compatible tween helpers, PNG storybook art under `Resources`, PowerShell static contracts, Unity editor layout regression checks.

---

## File structure

**Create**

- `tools/VerifyPremiumStorybookHome.ps1` — static contract gate for the premium home.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyTopRibbon.cs` — player, currency, rare-resource, and season state.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyJournalPage.cs` — one prioritized recommendation with progress and reward.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyChapterPage.cs` — next route, danger, difficulty, reward, and primary start action.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCampScene.cs` — panorama, mascot/companion, building overlays, hotspots, and scene-input gate.
- `Assets/Scripts/Runtime/Game/UI/Lobby/CampVisualAssetCatalog.cs` — stable resource paths and fallbacks for the twelve home art surfaces.
- `Assets/Resources/PenguinRun/StorybookUI/PremiumHome/` — text-free transparent overlays and portraits.

**Modify**

- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs` — complete immutable home snapshot.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyDock.cs` — centered paper-bookmark navigation and active state.
- `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs` — compose the new home components and gate scene input while panels are open.
- `Assets/Editor/ProjectUiLayoutRegressionTest.cs` — verify home layout at wide and narrow landscape sizes.
- `tools/VerifyStorybookUiPhase1.ps1` — accept the new component boundaries without weakening prior contracts.
- `docs/CURRENT_DESIGN.md` and `docs/UI_ART_REVAMP_PLAN.md` — record actual delivered state and verification evidence.

**Remove after Task 6 integration**

- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyBackground.cs` and `.meta` — panorama ownership moves to `LobbyCampScene`.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyStoryCard.cs` and `.meta` — split into journal and chapter pages.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs` and `.meta` — character ownership moves to `LobbyCampScene`.

## Working-tree rule

Before every commit, stage only the files named by that task and inspect `git diff --cached --name-status`. Never stage `.superpowers/brainstorm`, Unity recovery files, `Library`, `Logs`, `_tmp`, or unrelated user changes.

---

### Task 1: Lock the premium-home contract

**Files:**

- Create: `tools/VerifyPremiumStorybookHome.ps1`
- Test: `tools/VerifyStorybookUiPhase1.ps1`
- Test: `tools/VerifyPremiumStorybookHome.ps1`

- [ ] **Step 1: Run the inherited UI baseline**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
```

Expected: `P0 UI/art static contracts passed.` and `Storybook UI phase-one static contracts passed.`

- [ ] **Step 2: Create the focused failing verifier**

Create `tools/VerifyPremiumStorybookHome.ps1` with:

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

$model = Read-Required 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs'
foreach ($field in 'PlayerLevel','RareResource','SeasonLabel','JournalRewardLabel','NextChapterTitle','NextChapterBody','DifficultyLabel','RouteRewardLabel') {
    Require $model "\b$field\b" "LobbyViewModel.$field"
}

foreach ($component in 'LobbyTopRibbon','LobbyJournalPage','LobbyChapterPage','LobbyCampScene','CampVisualAssetCatalog') {
    $text = Read-Required "Assets/Scripts/Runtime/Game/UI/Lobby/$component.cs"
    Require $text "(class|struct)\s+$component" "premium home component $component"
}

$campScene = Read-Required 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCampScene.cs'
foreach ($method in 'Bind','SetInputEnabled','SetPanelOpen') {
    Require $campScene "\b$method\s*\(" "LobbyCampScene.$method"
}

$mainMenu = Read-Required 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'
foreach ($call in 'LobbyTopRibbon\.Build\(','LobbyJournalPage\.Build\(','LobbyChapterPage\.Build\(','LobbyCampScene\.Build\(') {
    Require $mainMenu $call "MainMenuBootstrap delegates $call"
}
if ($mainMenu -match 'LobbyStoryCard\.Build\(') { $failures.Add('Legacy combined LobbyStoryCard is still constructed.') }
if ($mainMenu -match 'LobbyCompanionCorner\.Build\(') { $failures.Add('Legacy companion card is still constructed.') }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}
Write-Output 'Premium storybook home static contracts passed.'
```

- [ ] **Step 3: Run the new gate and verify red state**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyPremiumStorybookHome.ps1
```

Expected: FAIL listing the missing view-model fields and five missing component files.

- [ ] **Step 4: Commit only the verifier**

```powershell
git add -- tools/VerifyPremiumStorybookHome.ps1
git diff --cached --name-status
git commit -m "test: lock premium storybook home contracts"
```

---

### Task 2: Expand the immutable lobby snapshot

**Files:**

- Modify: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`
- Test: `tools/VerifyPremiumStorybookHome.ps1`

- [ ] **Step 1: Add the complete home fields**

Add these fields to `LobbyViewModel`:

```csharp
public readonly int PlayerLevel;
public readonly int RareResource;
public readonly string SeasonLabel;
public readonly string JournalRewardLabel;
public readonly string NextChapterTitle;
public readonly string NextChapterBody;
public readonly string DifficultyLabel;
public readonly string RouteRewardLabel;
```

Extend the constructor with matching parameters and normalize them with `Mathf.Max(0, value)` or `value ?? string.Empty`. Keep all existing fields so current callers compile during migration.

- [ ] **Step 2: Assemble presentation text in one place**

In `MainMenuBootstrap.BuildLobbyViewModel`, derive the new values before constructing the model:

```csharp
var difficulty = DifficultyPreset.ForKind(PlayerSave.SelectedDifficulty);
var chapterTitle = recommendation.Kind == GrowthRecommendationKind.StartJourney
    ? "雪松遗迹"
    : recommendation.Title;
var chapterBody = recommendation.Kind == GrowthRecommendationKind.StartJourney
    ? "穿过低雾中的旧木桥，避开倒伏雪松和滚落石块。"
    : recommendation.Body;
var journalReward = claimableMissions > 0 ? $"可领取 {claimableMissions} 份任务奖励" : "今日奖励：鱼干 × 80";
var routeReward = HasPlayedDailyToday() ? "伙伴手记 · 普通" : "今日首胜 · 双倍鱼干";
```

Pass `PlayerSave.PlayerLevel`, a nonnegative rare-resource value from the existing save schema (use `0` when the schema has no rare-resource field), `"极光季 · 第三章"`, and the four derived strings into the model. Do not add a save key in this task.

- [ ] **Step 3: Run the focused gate**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyPremiumStorybookHome.ps1
```

Expected: the eight view-model-field failures disappear; component failures remain.

- [ ] **Step 4: Commit the snapshot change**

```powershell
git add -- Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs
git diff --cached --check
git commit -m "feat: expand premium lobby view model"
```

---

### Task 3: Build the top ribbon and paired story pages

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyTopRibbon.cs`
- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyJournalPage.cs`
- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyChapterPage.cs`
- Test: `tools/VerifyPremiumStorybookHome.ps1`

- [ ] **Step 1: Implement the top-ribbon public surface**

Create `LobbyTopRibbon` with this API and ownership boundary:

```csharp
public sealed class LobbyTopRibbon
{
    public RectTransform Root { get; private set; }
    public static LobbyTopRibbon Build(Transform parent);
    public void Bind(LobbyViewModel model);
}
```

`Build` creates four compact pills under anchors `(0.03, 0.90)` to `(0.97, 0.975)`: profile, fish, rare resource, and right-aligned season. Use `PaperWarm` for the profile pill, translucent `InkPrimary` for currencies, and `CoralAction` for the season ribbon. Every label uses `UiTheme.TmpFont` through `UiBuilder.CreateTmpText`.

- [ ] **Step 2: Implement the left journal page**

Create `LobbyJournalPage` with:

```csharp
public sealed class LobbyJournalPage
{
    public RectTransform Root { get; private set; }
    public static LobbyJournalPage Build(Transform parent);
    public void Bind(LobbyViewModel model, System.Action onOpenRecommendation);
}
```

Anchor it to `(0.03, 0.20)`–`(0.27, 0.82)`. Render the eyebrow `今日手记`, `CurrentStoryTitle`, a maximum three-line `CurrentStoryBody`, `StoryProgress01`, `JournalRewardLabel`, and one secondary text button. Rebinding must call `onClick.RemoveAllListeners()` before adding the callback.

- [ ] **Step 3: Implement the right chapter page**

Create `LobbyChapterPage` with:

```csharp
public sealed class LobbyChapterPage
{
    public RectTransform Root { get; private set; }
    public static LobbyChapterPage Build(Transform parent);
    public void Bind(LobbyViewModel model, System.Action onStartJourney);
}
```

Anchor it to `(0.73, 0.20)`–`(0.97, 0.82)`. Render `下一章`, `NextChapterTitle`, `NextChapterBody`, `DifficultyLabel`, `RouteRewardLabel`, a dotted route divider, and a single `翻开旅程` primary button. The button uses at least a 64-pixel height at 1920×1080 reference resolution.

- [ ] **Step 4: Verify the component contracts**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyPremiumStorybookHome.ps1
```

Expected: failures remain only for `LobbyCampScene`, `CampVisualAssetCatalog`, and bootstrap delegation.

- [ ] **Step 5: Commit the three components**

```powershell
git add -- Assets/Scripts/Runtime/Game/UI/Lobby/LobbyTopRibbon.cs Assets/Scripts/Runtime/Game/UI/Lobby/LobbyJournalPage.cs Assets/Scripts/Runtime/Game/UI/Lobby/LobbyChapterPage.cs
git diff --cached --check
git commit -m "feat: add premium lobby story surfaces"
```

---

### Task 4: Produce and register the twelve home art surfaces

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/CampVisualAssetCatalog.cs`
- Create: `Assets/Resources/PenguinRun/StorybookUI/PremiumHome/*.png`
- Test: `tools/VerifyPremiumStorybookHome.ps1`

- [ ] **Step 1: Generate the text-free art set**

Use the image-generation skill with this shared prefix for every output:

```text
Original arctic penguin adventure game asset, warm hand-painted children's storybook watercolor and gouache, handcrafted wood and canvas, muted cream paper texture, pine green, mist blue and coral accents, clear readable silhouette, no text, no letters, no logo, no frame, transparent background except the panorama.
```

Create these exact files:

```text
camp_panorama_premium.png — 2048×1024 full landscape camp, open center, layered foreground snow/lanterns/ropes, middle tents/workshop/lookout, distant ice lake and mountains.
mascot_gugugaga_premium.png — 1024×1024 transparent full-body explorer penguin, coral scarf, canvas satchel, waving pose.
companion_tuantuan.png — 1024×1024 transparent calm round penguin companion carrying a field notebook.
companion_dror.png — 1024×1024 transparent tall cautious penguin companion carrying a lantern.
camp_bonfire_atlas.png — 1536×512 transparent strip with three upgrade states of the same bonfire shrine.
camp_workshop_atlas.png — 1536×512 transparent strip with three upgrade states of the same snow-shed workshop.
camp_companion_hut_atlas.png — 1536×512 transparent strip with three upgrade states of the same companion tent.
camp_library_atlas.png — 1536×512 transparent strip with three upgrade states of the same canvas library pavilion.
camp_lookout_atlas.png — 1536×512 transparent strip with three upgrade states of the same wooden lookout.
camp_foreground_props.png — 1024×1024 transparent cluster of lantern, rope coil, crate, footprints and snow-covered signpost.
camp_smoke_soft.png — 512×512 transparent soft watercolor chimney-smoke puff.
camp_glow_soft.png — 512×512 transparent warm lantern glow without hard edges.
```

Inspect every image for accidental text, opaque checkerboard backgrounds, cropped silhouettes, and inconsistent light direction before import.

- [ ] **Step 2: Register stable resource paths and fallbacks**

Create `CampVisualAssetCatalog` with constants for all twelve paths, a private `Dictionary<string, Sprite>` cache, `Load(string path)`, and these public methods:

```csharp
public static Sprite Panorama => Load("PenguinRun/StorybookUI/PremiumHome/camp_panorama_premium")
    ?? StorybookArtCatalog.LobbyPanorama;
public static Sprite Mascot => Load("PenguinRun/StorybookUI/PremiumHome/mascot_gugugaga_premium")
    ?? StorybookArtCatalog.Mascot;
public static Sprite Companion(bool drorUnlocked) => Load(drorUnlocked
    ? "PenguinRun/StorybookUI/PremiumHome/companion_dror"
    : "PenguinRun/StorybookUI/PremiumHome/companion_tuantuan") ?? Mascot;
public static Sprite Building(CampBuildingId id, int level);
public static Sprite ForegroundProps => Load("PenguinRun/StorybookUI/PremiumHome/camp_foreground_props");
public static Sprite Smoke => Load("PenguinRun/StorybookUI/PremiumHome/camp_smoke_soft");
public static Sprite Glow => Load("PenguinRun/StorybookUI/PremiumHome/camp_glow_soft");
```

`Building` selects the correct atlas and returns one of three cached sub-sprites using a one-third horizontal rectangle. Clamp level to 1–3. When the atlas is missing, return `UiBuilder.CircleSprite` so the hotspot remains visible.

- [ ] **Step 3: Extend the verifier with exact asset checks**

Add all twelve file paths to `VerifyPremiumStorybookHome.ps1` and require nonzero file length. Run the gate; expected remaining failures are only `LobbyCampScene` and bootstrap delegation.

- [ ] **Step 4: Commit the art and catalog**

```powershell
git add -- Assets/Resources/PenguinRun/StorybookUI/PremiumHome Assets/Scripts/Runtime/Game/UI/Lobby/CampVisualAssetCatalog.cs tools/VerifyPremiumStorybookHome.ps1
git diff --cached --name-status
git commit -m "feat: add premium camp art catalog"
```

---

### Task 5: Build the interactive camp scene

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCampScene.cs`
- Test: `tools/VerifyPremiumStorybookHome.ps1`

- [ ] **Step 1: Define the scene callbacks**

Add:

```csharp
public readonly struct LobbyCampActions
{
    public readonly System.Action OpenCamp;
    public readonly System.Action OpenCodex;
    public LobbyCampActions(System.Action openCamp, System.Action openCodex)
    {
        OpenCamp = openCamp;
        OpenCodex = openCodex;
    }
}
```

`LobbyCampScene` exposes `Build(Transform, LobbyCampActions)`, `Bind(LobbyViewModel, CampRebuildSnapshot, bool drorUnlocked)`, `SetInputEnabled(bool)`, and `SetPanelOpen(bool)`.

- [ ] **Step 2: Build scene layers in strict order**

Create these child names so layout tests can find them:

```text
PremiumCampScene/Panorama
PremiumCampScene/AmbientWash
PremiumCampScene/BuildingLayer
PremiumCampScene/CharacterLayer/Mascot
PremiumCampScene/CharacterLayer/Companion
PremiumCampScene/ForegroundProps
PremiumCampScene/HotspotLayer
```

The panorama fills the screen. Building overlays live between anchors `x=0.08..0.92`, `y=0.08..0.48`. Characters occupy the center corridor but remain behind both paper pages. Foreground props have `raycastTarget=false`.

- [ ] **Step 3: Create five semantic hotspots**

Map `CampBuildingId` to normalized anchors:

```csharp
CampBuildingId.Bonfire => new Vector2(0.34f, 0.27f),
CampBuildingId.Workshop => new Vector2(0.18f, 0.40f),
CampBuildingId.CompanionHut => new Vector2(0.82f, 0.35f),
CampBuildingId.LibraryTent => new Vector2(0.68f, 0.29f),
CampBuildingId.Lookout => new Vector2(0.87f, 0.54f),
```

Each hotspot is a 56×56 button with a warm glow and a caption shown only when the node is affordable, newly changed, or level zero. All five invoke `OpenCamp`. The companion invokes `OpenCodex`.

- [ ] **Step 4: Implement input and panel gating**

`SetInputEnabled(false)` sets every scene button `interactable=false`. `SetPanelOpen(true)` also reduces hotspot alpha to zero without disabling ambient motion. Do not pause snow, smoke, or character breathing when a panel is open.

- [ ] **Step 5: Run the focused gate and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyPremiumStorybookHome.ps1
git add -- Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCampScene.cs
git diff --cached --check
git commit -m "feat: add interactive premium camp scene"
```

---

### Task 6: Recompose the lobby and book-tab dock

**Files:**

- Modify: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyDock.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`
- Remove: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyStoryCard.cs`
- Remove: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyBackground.cs`
- Remove: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs`
- Test: `tools/VerifyPremiumStorybookHome.ps1`
- Test: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Re-anchor the dock**

Change the dock root to anchors `(0.27, 0.025)`–`(0.73, 0.13)`. Keep five equal destinations, but render each item as a paper bookmark with a small icon well above the label. `Run` remains the visual primary item only when no panel is open; `SetSelected` highlights the actual open panel.

- [ ] **Step 2: Replace bootstrap fields and construction**

Replace `LobbyStoryCard`, `LobbyCompanionCorner`, and `lobbyBackground` fields with:

```csharp
private LobbyTopRibbon lobbyTopRibbon;
private LobbyJournalPage lobbyJournalPage;
private LobbyChapterPage lobbyChapterPage;
private LobbyCampScene lobbyCampScene;
```

In `BuildScene`, build the camp scene first, then ribbon, pages, dock, and panel layer. Bind journal recommendation to `ExecuteLobbyPrimaryAction`, bind chapter primary action to `StartEndless`, and pass `OpenCampShop`/`OpenCodex` into `LobbyCampActions`.

- [ ] **Step 3: Rebind and gate surfaces**

`RefreshStorybookLobby` calls all four `Bind` methods. `SetLobbySurfaceVisible` keeps the panorama active while a panel is open, hides paper pages and dock, calls `lobbyCampScene.SetPanelOpen(!visible)`, and calls `lobbyCampScene.SetInputEnabled(visible)`. `OnPanelRouterChanged` must leave panel input above scene input.

- [ ] **Step 4: Remove legacy construction and source**

Delete `LobbyStoryCard.Build`, `LobbyCompanionCorner.Build`, and `LobbyBackground.Build` calls. Remove all three legacy component source files and their `.meta` files in this task after `rg` confirms there are no remaining references.

- [ ] **Step 5: Update the old phase-one gate without weakening it**

Replace its component list with `LobbyCampScene`, `LobbyJournalPage`, `LobbyChapterPage`, and `LobbyDock`. Continue requiring a separate immutable view model, panel router, art fallback, retained HUD, result view model, and loading canvas.

- [ ] **Step 6: Run gates and commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyPremiumStorybookHome.ps1
git add -- Assets/Scripts/Runtime/Game/UI/Lobby Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --check
git commit -m "feat: recompose premium storybook home"
```

---

### Task 7: Add wide and narrow landscape regression checks

**Files:**

- Modify: `Assets/Editor/ProjectUiLayoutRegressionTest.cs`
- Modify: `tools/VerifyPremiumStorybookHome.ps1`

- [ ] **Step 1: Add a reusable lobby layout assertion**

Add a method that creates a canvas with `CanvasScaler.referenceResolution = new Vector2(width, height)`, builds the premium home components, calls `Canvas.ForceUpdateCanvases`, and asserts:

```text
LobbyJournalPage and LobbyChapterPage do not overlap.
LobbyDock does not overlap either page.
PrimaryChapterAction height is at least 64 reference pixels.
Mascot center remains between normalized x 0.28 and 0.72.
Every hotspot button is at least 48×48 reference pixels.
```

Run it for `1920×1080`, `2340×1080`, and `1280×720`. Destroy the temporary canvas in `finally`.

- [ ] **Step 2: Invoke the check from `RunNow`**

Call `ValidatePremiumHomeLayout(failures)` after existing result-text and panel-router checks. Add `Require-Pattern` checks for the three resolutions to the PowerShell verifier.

- [ ] **Step 3: Run edit-mode regression when Unity is available**

Create `Temp/PenguinRunUiRegression.request` while the editor is open, wait for `Temp/PenguinRunUiRegression.result`, and expect `PASS: UI layout regressions are covered.` If the editor cannot run due to licensing, record the exact message and retain the test as pending runtime evidence.

- [ ] **Step 4: Commit the regression checks**

```powershell
git add -- Assets/Editor/ProjectUiLayoutRegressionTest.cs tools/VerifyPremiumStorybookHome.ps1
git diff --cached --check
git commit -m "test: cover premium lobby layouts"
```

---

### Task 8: Verify, capture, and document the premium home

**Files:**

- Modify: `docs/CURRENT_DESIGN.md`
- Modify: `docs/UI_ART_REVAMP_PLAN.md`
- Test: `tools/VerifyUiArtP0.ps1`
- Test: `tools/VerifyStorybookUiPhase1.ps1`
- Test: `tools/VerifyPremiumStorybookHome.ps1`

- [ ] **Step 1: Run all static gates**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyPremiumStorybookHome.ps1
git diff --check
```

Expected: all three success messages and no whitespace errors.

- [ ] **Step 2: Run Unity compilation and Android build**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\dxh53\Desktop\mygame' -logFile 'C:\Users\dxh53\Desktop\mygame\_tmp\premium-home-unity.log'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/BuildLatestAndroidApk.ps1
```

A Unity license error is a blocker, not a pass.

- [ ] **Step 3: Capture five lobby states**

Capture default, claimable mission, affordable rebuild, panel-open, and narrow-landscape states. Confirm the central camp remains visible, only one primary journey button is present, hotspots do not sit above panel input, and no generated asset contains text artifacts.

- [ ] **Step 4: Update design status with actual evidence**

Record the commands, exit codes, screenshot paths, Unity license status, Android artifact path, and remaining runtime risks. Do not write that runtime verification passed unless the editor and app actually ran.

- [ ] **Step 5: Commit documentation only**

```powershell
git add -- docs/CURRENT_DESIGN.md docs/UI_ART_REVAMP_PLAN.md
git diff --cached --check
git commit -m "docs: record premium storybook home delivery"
```

## Premium-home completion criteria

- The home visibly uses the premium panorama, character art, five building overlays, foreground props, and ambient layers.
- The left journal and right chapter pages remain readable without covering the central camp.
- The chapter page owns the only primary start action.
- Camp and companion hotspots are disabled below panels and remain at least 48×48 reference pixels.
- Legacy combined story card and companion card are no longer constructed.
- Existing UI gates and the new premium-home gate pass.
- Unity, Android, screenshot, and layout evidence are reported from fresh runs or explicitly marked blocked.
