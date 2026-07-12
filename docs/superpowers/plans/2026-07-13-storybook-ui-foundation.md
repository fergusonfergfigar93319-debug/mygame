# Storybook UI Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the first independently playable upgrade: a coherent “storybook camp” visual system across the lobby, major panels, runner HUD, journey results, and loading flow without changing runner or Boss rules.

**Architecture:** Keep Unity 6 Built-in Render Pipeline, uGUI, TextMeshPro, and programmatic scene construction. Extend `UiTheme`, introduce focused storybook presentation components under `Game/UI/Storybook` and `Game/UI/Lobby`, and leave gameplay state authoritative in existing stores/controllers; presentation reads immutable view models and emits callbacks only.

**Tech Stack:** Unity 6.0.4, C#, uGUI 2.0, TextMeshPro 3.0.6, DOTween with existing fallback paths, PowerShell static contracts, Unity EditMode tests when the local editor license is available.

---

## File structure

**Create**

- `tools/VerifyStorybookUiPhase1.ps1` — incremental static contract gate for this phase.
- `Assets/Scripts/Runtime/Game/UI/Storybook/StorybookUi.cs` — paper cards, stitched outlines, labels, buttons, section dividers, and safe fallbacks.
- `Assets/Scripts/Runtime/Game/UI/Storybook/StorybookArtCatalog.cs` — centralized resource paths and fallback behavior for generated art.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs` — immutable lobby data assembled from current stores.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyBackground.cs` — illustrated panorama, parallax silhouettes, snow motes, and restored-building overlays.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyStoryCard.cs` — current objective, progress, and one primary action.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs` — mascot/companion portrait, bond hint, and interaction callback.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyDock.cs` — stable navigation and badges.
- `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyPanelRouter.cs` — one active panel, Escape/back handling, and idempotent close.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs` — retained uGUI HUD updated only when values change.
- `Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs` — unified result data for death and completed runs.
- `Assets/Resources/PenguinRun/StorybookUI/lobby_panorama.png` — text-free camp panorama.
- `Assets/Resources/PenguinRun/StorybookUI/mascot_gugugaga.png` — transparent mascot illustration.

**Modify**

- `Assets/Scripts/Runtime/Game/UI/UiTheme.cs` — storybook tokens and semantic palette.
- `Assets/Scripts/Runtime/Game/UI/UiBuilder.cs` — delegate storybook primitives without breaking existing public calls.
- `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs` — retain composition/root lifecycle and delegate lobby responsibilities.
- `Assets/Scripts/Runtime/Game/UI/PanelHeader.cs` — storybook panel chrome.
- `Assets/Scripts/Runtime/Game/UI/MissionPanel.cs` — shared scaffold and semantic mission states.
- `Assets/Scripts/Runtime/Game/UI/CodexPanel.cs` — chapter-page presentation.
- `Assets/Scripts/Runtime/Game/UI/CampShopPanel.cs` — shared scaffold and warm camp palette.
- `Assets/Scripts/Runtime/Game/UI/SettingsPanel.cs` — shared scaffold and accessible controls.
- `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs` — own and update `RunnerHudCanvas`; retain gameplay orchestration.
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHud.cs` — keep only non-visual formatting helpers during migration, then remove its IMGUI draw path.
- `Assets/Scripts/Runtime/Game/UI/RunnerDeathOverlay.cs` — use the unified journey result presentation.
- `Assets/Scripts/Runtime/Game/UI/RunResultOverlay.cs` — render a journey-book result page.
- `Assets/Scripts/Runtime/PenguinRun/Systems/SceneTransitionManager.cs` — replace loading IMGUI with retained uGUI.
- `docs/CURRENT_DESIGN.md` and `docs/UI_ART_REVAMP_PLAN.md` — record the delivered phase and remaining phases.

## Working-tree rule

The repository already contains related, uncommitted P0 UI/art work. Before each commit, stage only the files named by that task and inspect `git diff --cached --name-status`. Never stage `.superpowers/`, unrelated user files, scene recovery files, or the whole repository with `git add .`.

---

### Task 1: Lock the phase-one contract and baseline

**Files:**

- Create: `tools/VerifyStorybookUiPhase1.ps1`
- Test: `tools/VerifyUiArtP0.ps1`
- Test: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Verify the inherited P0 baseline**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
git diff --check
```

Expected: `P0 UI/art static contracts passed.`; only existing line-ending warnings are allowed from `git diff --check`.

- [ ] **Step 2: Create the focused contract helper and first failing token checks**

Create `tools/VerifyStorybookUiPhase1.ps1` with:

```powershell
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }
    Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require-Pattern([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

$theme = Require-File 'Assets/Scripts/Runtime/Game/UI/UiTheme.cs'
foreach ($token in 'PaperBase','PaperWarm','InkPrimary','InkSecondary','PineGreen','MistBlue','CoralAction','BerryDanger','StitchBorder','ShadowWarm') {
    Require-Pattern $theme "\b$token\b" "UiTheme.$token"
}
Require-Pattern $theme 'StorybookCardRadius' 'storybook card radius token'
Require-Pattern $theme 'StorybookPageTurnDuration' 'storybook page-turn duration token'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}
Write-Output 'Storybook UI phase-one static contracts passed.'
```

- [ ] **Step 3: Run the new gate and confirm the intended failure**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
```

Expected: FAIL listing the missing `UiTheme.PaperBase` and other storybook tokens.

- [ ] **Step 4: Commit the verifier only**

```powershell
git add -- tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "test: lock storybook UI foundation contracts"
```

Expected staged file: only `tools/VerifyStorybookUiPhase1.ps1`.

---

### Task 2: Add semantic storybook design tokens

**Files:**

- Modify: `Assets/Scripts/Runtime/Game/UI/UiTheme.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/UiBuilder.cs`
- Test: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Add the semantic palette and motion tokens**

Add these members to `UiTheme`:

```csharp
public static readonly Color PaperBase = new(0.965f, 0.918f, 0.792f, 1f);
public static readonly Color PaperWarm = new(1f, 0.968f, 0.89f, 1f);
public static readonly Color InkPrimary = new(0.145f, 0.235f, 0.255f, 1f);
public static readonly Color InkSecondary = new(0.275f, 0.39f, 0.40f, 0.82f);
public static readonly Color PineGreen = new(0.235f, 0.47f, 0.45f, 1f);
public static readonly Color MistBlue = new(0.64f, 0.82f, 0.82f, 1f);
public static readonly Color CoralAction = new(0.925f, 0.39f, 0.255f, 1f);
public static readonly Color BerryDanger = new(0.45f, 0.24f, 0.35f, 1f);
public static readonly Color StitchBorder = new(0.19f, 0.32f, 0.33f, 0.72f);
public static readonly Color ShadowWarm = new(0.20f, 0.16f, 0.13f, 0.24f);
public const float StorybookCardRadius = 24f;
public const float StorybookPageTurnDuration = 0.34f;
public const float StorybookPunchDuration = 0.18f;
```

Keep existing token names as compatibility aliases; do not recolor runner HUD or Boss warnings in this step.

- [ ] **Step 2: Retarget only semantic compatibility aliases in `UiBuilder`**

Use the new palette for menu surfaces while retaining public field names:

```csharp
public static readonly Color WarmGold = UiTheme.CoralAction;
public static readonly Color SurfaceBase = UiTheme.PaperBase;
public static readonly Color Surface1 = UiTheme.PaperWarm;
public static readonly Color Surface2 = Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.28f);
public static readonly Color SurfaceElevated = Color.white;
public static readonly Color TextPrimary = UiTheme.InkPrimary;
public static readonly Color TextSecondary = UiTheme.InkSecondary;
public static readonly Color TextOnAccent = Color.white;
```

Keep `AccentCyan`, `Danger`, and runtime/Boss-specific colors unchanged until their consumers receive semantic replacements.

- [ ] **Step 3: Run contracts**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
```

Expected: both gates pass.

- [ ] **Step 4: Commit scoped changes**

```powershell
git add -- Assets/Scripts/Runtime/Game/UI/UiTheme.cs Assets/Scripts/Runtime/Game/UI/UiBuilder.cs
git diff --cached --name-status
git commit -m "feat: add storybook semantic UI tokens"
```

---

### Task 3: Create and register storybook art with fallbacks

**Files:**

- Create: `Assets/Resources/PenguinRun/StorybookUI/lobby_panorama.png`
- Create: `Assets/Resources/PenguinRun/StorybookUI/mascot_gugugaga.png`
- Create: `Assets/Scripts/Runtime/Game/UI/Storybook/StorybookArtCatalog.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Extend the gate before creating assets**

Add:

```powershell
$art = Require-File 'Assets/Scripts/Runtime/Game/UI/Storybook/StorybookArtCatalog.cs'
Require-Pattern $art 'LobbyPanoramaPath' 'lobby panorama resource path'
Require-Pattern $art 'MascotPath' 'mascot resource path'
Require-Pattern $art 'CreateFallbackMascot' 'mascot fallback'
foreach ($asset in @(
    'Assets/Resources/PenguinRun/StorybookUI/lobby_panorama.png',
    'Assets/Resources/PenguinRun/StorybookUI/mascot_gugugaga.png'
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $asset))) {
        $failures.Add("Missing storybook art: $asset")
    }
}
```

Run the gate and expect failures for the catalog and both images.

- [ ] **Step 2: Generate the two text-free visual assets**

Use the image-generation workflow with these exact art briefs:

```text
lobby_panorama.png — 2048x1024 landscape game background, warm hand-painted children's storybook, rebuilding arctic penguin camp at dusk, canvas tents, pine silhouettes, distant ice mountains, small lanterns, cream paper grain, muted mist blue and pine green with coral accents, clear open center for UI and mascot, no characters, no text, no logos, no border.

mascot_gugugaga.png — 1024x1024 transparent background, full-body cute original young penguin explorer named Gugugaga, warm hand-painted children's storybook style, cream belly, charcoal-blue feathers, coral scarf, tiny canvas satchel, friendly determined expression, waving pose, clean readable silhouette, soft painted edge, no text, no logo, no ground shadow.
```

Place outputs at the exact resource paths above and verify alpha is present in the mascot file.

- [ ] **Step 3: Implement the resource catalog and deterministic fallback**

Create:

```csharp
using UnityEngine;

namespace PenguinRun.Game.UI.Storybook
{
    public static class StorybookArtCatalog
    {
        public const string LobbyPanoramaPath = "PenguinRun/StorybookUI/lobby_panorama";
        public const string MascotPath = "PenguinRun/StorybookUI/mascot_gugugaga";

        public static Sprite LobbyPanorama => Resources.Load<Sprite>(LobbyPanoramaPath);
        public static Sprite Mascot => Resources.Load<Sprite>(MascotPath) ?? CreateFallbackMascot();

        public static Sprite CreateFallbackMascot()
        {
            return UiBuilder.CircleSprite;
        }
    }
}
```

- [ ] **Step 4: Run contracts and commit only new assets/catalog**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
git add -- Assets/Resources/PenguinRun/StorybookUI Assets/Scripts/Runtime/Game/UI/Storybook/StorybookArtCatalog.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "feat: add storybook lobby art foundation"
```

---

### Task 4: Build reusable storybook UI primitives

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/Storybook/StorybookUi.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/UiBuilder.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Add failing primitive contracts**

Append checks for `CreatePaperCard`, `CreateStoryLabel`, `CreatePrimaryAction`, `CreateSectionDivider`, `AddStitchedOutline`, and `PlayPageTurnIn`; run the gate and expect those six failures.

- [ ] **Step 2: Implement the focused primitive API**

Create `StorybookUi` with this public surface:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI.Storybook
{
    public static class StorybookUi
    {
        public static RectTransform CreatePaperCard(string name, Transform parent, Color? color = null)
        {
            var card = UiBuilder.CreateRect(name, parent, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, color ?? UiTheme.PaperWarm, rounded: true);
            UiBuilder.AddShadow(card.gameObject, UiTheme.ShadowWarm, new Vector2(7f, -8f));
            AddStitchedOutline(card.gameObject);
            return card;
        }

        public static TextMeshProUGUI CreateStoryLabel(string name, Transform parent, string text, int size)
            => UiBuilder.CreateTmpText(name, parent, text, size, FontStyle.Bold,
                TextAnchor.MiddleLeft, UiTheme.InkPrimary);

        public static Button CreatePrimaryAction(string name, Transform parent, string text)
            => UiBuilder.CreatePrimaryButton(name, parent, text, UiTheme.Body, null);

        public static RectTransform CreateSectionDivider(string name, Transform parent)
            => UiBuilder.CreateRect(name, parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 2f), Vector2.zero, UiTheme.StitchBorder);

        public static void AddStitchedOutline(GameObject target)
            => UiBuilder.AddOutline(target, UiTheme.StitchBorder, new Vector2(2f, -2f));

        public static void PlayPageTurnIn(RectTransform target)
            => target.PlayPanelPopIn(UiTheme.StorybookPageTurnDuration);
    }
}
```

- [ ] **Step 3: Add `UiBuilder` forwarding helpers only where existing panels need compatibility**

Do not duplicate token values. Forward to `StorybookUi` and keep old factory signatures intact.

- [ ] **Step 4: Run both contract gates and commit**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
git add -- Assets/Scripts/Runtime/Game/UI/Storybook/StorybookUi.cs Assets/Scripts/Runtime/Game/UI/UiBuilder.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "feat: add reusable storybook UI primitives"
```

---

### Task 5: Extract lobby data and panel routing

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs`
- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyPanelRouter.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Add failing contracts for immutable lobby data and idempotent routing**

Require the strings `readonly struct LobbyViewModel`, `CurrentStoryTitle`, `PrimaryActionLabel`, `CompanionHint`, `Open`, `Close`, and `currentPanel`; also reject a `MainMenuBootstrap.cs` line count above 1900 after extraction.

- [ ] **Step 2: Define the immutable lobby view model**

```csharp
namespace PenguinRun.Game.UI.Lobby
{
    public readonly struct LobbyViewModel
    {
        public readonly string CurrentStoryTitle;
        public readonly string CurrentStoryBody;
        public readonly string PrimaryActionLabel;
        public readonly float StoryProgress01;
        public readonly string CompanionHint;
        public readonly int Fish;
        public readonly int MissionBadge;

        public LobbyViewModel(string title, string body, string action, float progress,
            string companionHint, int fish, int missionBadge)
        {
            CurrentStoryTitle = title;
            CurrentStoryBody = body;
            PrimaryActionLabel = action;
            StoryProgress01 = UnityEngine.Mathf.Clamp01(progress);
            CompanionHint = companionHint;
            Fish = fish;
            MissionBadge = UnityEngine.Mathf.Max(0, missionBadge);
        }
    }
}
```

Assemble it in `MainMenuBootstrap` from existing `PlayerSave`, `MissionStore`, difficulty, and daily challenge state. Do not add new save keys in phase one.

- [ ] **Step 3: Implement one-active-panel routing**

`LobbyPanelRouter.Open` must call `Close()` first, parent the new panel under the existing panel layer, and play page-turn animation. `Close()` must be safe when no panel is open and invoke one refresh callback exactly once.

- [ ] **Step 4: Replace `OpenPanel` / `ClosePanel` internals with the router**

Keep their existing callers and Escape behavior; only change delegation:

```csharp
private void OpenPanel(GameObject panel, string panelName) => panelRouter.Open(panel, panelName);
private void ClosePanel() => panelRouter.Close();
```

- [ ] **Step 5: Run gates and commit**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
git add -- Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs Assets/Scripts/Runtime/Game/UI/Lobby/LobbyPanelRouter.cs Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "refactor: extract lobby data and panel routing"
```

---

### Task 6: Recompose the lobby as a storybook camp

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyBackground.cs`
- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyStoryCard.cs`
- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs`
- Create: `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyDock.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Add failing composition contracts**

Require all four new types, `LobbyStoryCard.Bind`, `LobbyCompanionCorner.Bind`, `LobbyDock.SetSelected`, panorama fallback handling, and `MainMenuBootstrap` delegation to each component.

- [ ] **Step 2: Implement `LobbyBackground`**

Build a full-screen `Image` using `StorybookArtCatalog.LobbyPanorama`; if null, use a `PaperBase -> MistBlue` layered fallback. Add two low-cost parallax silhouette strips and at most 24 pooled snow motes. Decorative layers must set `raycastTarget = false`.

- [ ] **Step 3: Implement `LobbyStoryCard`**

Expose exactly:

```csharp
public void Bind(LobbyViewModel model, System.Action onPrimaryAction)
```

Render one chapter label, title, two-line body, progress bar, and one coral primary action. Rebinding replaces the button listener rather than stacking listeners.

- [ ] **Step 4: Implement `LobbyCompanionCorner` and `LobbyDock`**

`LobbyCompanionCorner` uses the transparent mascot, a bond/status caption, a subtle breathing tween, and one callback. `LobbyDock` owns the five stable destinations and badges; it does not open panels directly.

- [ ] **Step 5: Reduce `MainMenuBootstrap` to composition**

Keep scene lifecycle, save reads, start-run commands, and panel factory callbacks. Move background drawing, story-card drawing, mascot drawing, dock construction, selected-state drawing, and panel ownership into the new components. Delete `DrawPenguinMascot` after all calls are replaced.

- [ ] **Step 6: Verify and commit**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
git add -- Assets/Scripts/Runtime/Game/UI/Lobby Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "feat: recompose lobby as storybook camp"
```

---

### Task 7: Unify major panel chrome

**Files:**

- Modify: `Assets/Scripts/Runtime/Game/UI/PanelHeader.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/MissionPanel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/CodexPanel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/CampShopPanel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/SettingsPanel.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Add failing panel contracts**

Require each panel to use `StorybookUi.CreatePaperCard` or the shared `PanelHeader`, and reject raw uses of the old dark menu background constants inside these five files.

- [ ] **Step 2: Update `PanelHeader` as the shared chapter header**

The header must contain a back button, eyebrow/chapter label, title, optional subtitle, and optional right action. It uses `PaperWarm`, `InkPrimary`, `InkSecondary`, and `StitchBorder` only.

- [ ] **Step 3: Retarget panel semantics**

- Missions: coral for claimable, pine for completed, paper-neutral for active.
- Codex: chapter tabs and discovery cards; locked entries show clue text rather than fake zero values.
- Camp/shop: canvas-tent palette, clear owned/equipped/price states.
- Settings: high-contrast labels, 48 px minimum control target, animated toggle knob, and persistent current value text.

- [ ] **Step 4: Run contracts and commit**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
git add -- Assets/Scripts/Runtime/Game/UI/PanelHeader.cs Assets/Scripts/Runtime/Game/UI/MissionPanel.cs Assets/Scripts/Runtime/Game/UI/CodexPanel.cs Assets/Scripts/Runtime/Game/UI/CampShopPanel.cs Assets/Scripts/Runtime/Game/UI/SettingsPanel.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "feat: unify storybook panel chrome"
```

---

### Task 8: Replace the IMGUI runner HUD with retained uGUI

**Files:**

- Create: `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHud.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Add failing HUD boundary contracts**

Require `RunnerHudCanvas`, `BindStatic`, `RefreshDynamic`, `SetBossMode`, `SetPaused`, and `SetFeedback`. Reject `hud.Draw(` and reject runner-stat drawing inside `PenguinRunnerGame.OnGUI`.

- [ ] **Step 2: Build the retained HUD once**

`RunnerHudCanvas` creates a screen-space overlay canvas with separate static and dynamic roots. Its public API is:

```csharp
public void BindStatic(RunnerSessionConfig config);
public void RefreshDynamic(WorldDirector world, int score, string feedback, float feedbackTimer);
public void SetBossMode(BossSystem boss);
public void SetPaused(bool paused);
public void SetFeedback(string message, Color color, float seconds);
public void Dispose();
```

Top-left contains distance/score/combo, top-right contains fish/lives, lower corners contain companion/buff and Boss warning. The center and lower-middle corridor remain empty except short-lived feedback.

- [ ] **Step 3: Cache displayed values**

Each TMP field updates only when its source value changes. Buff rows are reused from a small list; do not destroy and recreate them each frame. Boss mode hides nonessential statistics and restores them on exit.

- [ ] **Step 4: Integrate lifecycle in `PenguinRunnerGame`**

Create after controller/world initialization, refresh during the existing update loop, and dispose on scene teardown. Keep `OnGUI` only for any editor-only diagnostics that have no runtime UI responsibility.

- [ ] **Step 5: Preserve formatting helpers while removing the visual IMGUI path**

Move pure methods such as buff-summary formatting to `RunnerHudCanvas` or a small internal formatter, then delete `RunnerHud.Draw` and its GUI textures/styles.

- [ ] **Step 6: Run contracts and commit**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
git add -- Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHud.cs Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "feat: migrate runner HUD to retained uGUI"
```

---

### Task 9: Unify journey results and restart flow

**Files:**

- Create: `Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/RunnerDeathOverlay.cs`
- Modify: `Assets/Scripts/Runtime/Game/UI/RunResultOverlay.cs`
- Modify: `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`

- [ ] **Step 1: Add failing journey-result contracts**

Require one `JourneyResultViewModel` used by both overlays, fields for headline, score, distance, fish, best comparison, review hint, and progression preview, plus both `Retry` and `ReturnToCamp` actions.

- [ ] **Step 2: Implement the immutable result model**

```csharp
namespace PenguinRun.Game.UI
{
    public readonly struct JourneyResultViewModel
    {
        public readonly string Headline;
        public readonly int Score;
        public readonly int DistanceMeters;
        public readonly int Fish;
        public readonly string BestComparison;
        public readonly string ReviewHint;
        public readonly string ProgressionPreview;

        public JourneyResultViewModel(string headline, int score, int distanceMeters, int fish,
            string bestComparison, string reviewHint, string progressionPreview)
        {
            Headline = headline;
            Score = score;
            DistanceMeters = distanceMeters;
            Fish = fish;
            BestComparison = bestComparison ?? string.Empty;
            ReviewHint = reviewHint ?? string.Empty;
            ProgressionPreview = progressionPreview ?? string.Empty;
        }
    }
}
```

- [ ] **Step 3: Render one journey-book hierarchy**

Both death and completed-run flows use the same paper-card hierarchy: headline, three core stats, comparison, review/achievement, progression preview, primary action, secondary action. Death keeps `GetDeathAnalysisHint`; completed runs use `RunOutcomeRouter.Result` to build the preview.

- [ ] **Step 4: Preserve the short retry path**

`Retry` calls the existing `RestartRunFromDeathOverlay`; it must not load the lobby first. `ReturnToCamp` uses the existing scene transition route.

- [ ] **Step 5: Run contracts and commit**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
git add -- Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs Assets/Scripts/Runtime/Game/UI/RunnerDeathOverlay.cs Assets/Scripts/Runtime/Game/UI/RunResultOverlay.cs Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs tools/VerifyStorybookUiPhase1.ps1
git diff --cached --name-status
git commit -m "feat: unify journey result presentation"
```

---

### Task 10: Replace loading IMGUI and complete phase verification

**Files:**

- Modify: `Assets/Scripts/Runtime/PenguinRun/Systems/SceneTransitionManager.cs`
- Modify: `tools/VerifyStorybookUiPhase1.ps1`
- Modify: `docs/CURRENT_DESIGN.md`
- Modify: `docs/UI_ART_REVAMP_PLAN.md`

- [ ] **Step 1: Add the final loading and cleanup contracts**

Require a loading canvas, `CanvasGroup`, storybook loading hint, progress value, and mascot image. Reject `SceneTransitionManager.OnGUI`, runtime `RunnerHud.Draw`, `MainMenuBootstrap.DrawPenguinMascot`, and the old dark menu background in phase-one surfaces.

- [ ] **Step 2: Build a retained loading overlay**

Create the overlay once with the paper palette, mascot, one theme-aware hint, and a progress strip. Fade with `CanvasGroup`; update progress only when the async operation value changes; destroy or reuse the overlay after completion.

- [ ] **Step 3: Run all static verification**

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyUiArtP0.ps1
powershell -ExecutionPolicy Bypass -File tools/VerifyStorybookUiPhase1.ps1
git diff --check
```

Expected: both gates pass and no whitespace errors.

- [ ] **Step 4: Run Unity verification when licensed**

Run the editor in batch mode with the project path and quit after compilation. If the editor returns `No valid Unity Editor license found`, record runtime verification as blocked and do not claim compilation success.

When licensed, manually smoke-test at 16:9:

1. Launch lobby and open/close Missions, Codex, Camp/Shop, and Settings.
2. Start a normal run and confirm the center/lower corridor remains clear.
3. Enter Boss mode and confirm nonessential HUD weakens.
4. Die, retry directly, die again, and return to camp.
5. Complete/exit a run and inspect the journey result page.
6. Trigger scene loading twice and confirm no duplicate canvas or event subscription.

- [ ] **Step 5: Update documentation with actual verification status**

Record delivered components, retained fallbacks, files still awaiting phase two, and whether Unity compile/runtime validation actually ran.

- [ ] **Step 6: Commit the final scoped phase**

```powershell
git add -- Assets/Scripts/Runtime/PenguinRun/Systems/SceneTransitionManager.cs tools/VerifyStorybookUiPhase1.ps1 docs/CURRENT_DESIGN.md docs/UI_ART_REVAMP_PLAN.md
git diff --cached --name-status
git diff --cached --check
git commit -m "feat: complete storybook UI foundation"
```

---

## Phase-one completion criteria

- Lobby, major panels, HUD, results, and loading use one semantic storybook design system.
- `MainMenuBootstrap` is below 1900 lines and delegates background, story card, companion, dock, and panel routing.
- Runtime runner statistics no longer render through IMGUI.
- The playable corridor stays unobstructed in normal and Boss states.
- Missing art uses an explicit fallback and never leaves blank controls.
- Existing P0 and new phase-one static contracts both pass.
- Unity compile/runtime status is reported honestly; license failure is not presented as a code pass.
