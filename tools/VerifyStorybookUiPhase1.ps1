param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }

    return Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require-Pattern([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

$theme = Require-File 'Assets/Scripts/Runtime/Game/UI/UiTheme.cs'
foreach ($token in 'PaperBase', 'PaperWarm', 'InkPrimary', 'InkSecondary', 'PineGreen', 'MistBlue', 'CoralAction', 'BerryDanger', 'StitchBorder', 'ShadowWarm') {
    Require-Pattern $theme "\b$token\b" "UiTheme.$token"
}
Require-Pattern $theme 'StorybookCardRadius' 'storybook card radius token'
Require-Pattern $theme 'StorybookPageTurnDuration' 'storybook page-turn duration token'

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

$storybook = Require-File 'Assets/Scripts/Runtime/Game/UI/Storybook/StorybookUi.cs'
foreach ($primitive in 'CreatePaperCard', 'CreateStoryLabel', 'CreatePrimaryAction', 'CreateSectionDivider', 'AddStitchedOutline', 'PlayPageTurnIn') {
    Require-Pattern $storybook "\b$primitive\b" "StorybookUi.$primitive"
}
Require-Pattern $storybook 'CreateTmpText\("StoryLabel"' 'storybook primary action uses a TMP label'

$lobbyModel = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyViewModel.cs'
Require-Pattern $lobbyModel 'readonly\s+struct\s+LobbyViewModel' 'immutable lobby view model'
foreach ($field in 'CurrentStoryTitle', 'PrimaryActionLabel', 'CompanionHint') {
    Require-Pattern $lobbyModel "\b$field\b" "LobbyViewModel.$field"
}

$panelRouter = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyPanelRouter.cs'
Require-Pattern $panelRouter 'void\s+Open\s*\(' 'panel router open command'
Require-Pattern $panelRouter 'void\s+Close\s*\(' 'panel router close command'
Require-Pattern $panelRouter 'currentPanel' 'panel router owns the active panel'

$mainMenu = Require-File 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'
Require-Pattern $mainMenu 'panelRouter\.Open\(' 'main menu delegates panel opening'
Require-Pattern $mainMenu 'panelRouter\.Close\(' 'main menu delegates panel closing'

foreach ($component in @(
    'LobbyBackground',
    'LobbyStoryCard',
    'LobbyCompanionCorner',
    'LobbyDock'
)) {
    $componentText = Require-File "Assets/Scripts/Runtime/Game/UI/Lobby/$component.cs"
    Require-Pattern $componentText "class\s+$component" "lobby component $component"
}
$storyCard = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyStoryCard.cs'
Require-Pattern $storyCard 'void\s+Bind\s*\(' 'LobbyStoryCard.Bind'
$companionCorner = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs'
Require-Pattern $companionCorner 'void\s+Bind\s*\(' 'LobbyCompanionCorner.Bind'
$lobbyDock = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyDock.cs'
Require-Pattern $lobbyDock 'void\s+SetSelected\s*\(' 'LobbyDock.SetSelected'
Require-Pattern $mainMenu 'LobbyBackground\.Build\(' 'main menu delegates lobby background'
Require-Pattern $mainMenu 'LobbyStoryCard\.Build\(' 'main menu delegates lobby story card'
Require-Pattern $mainMenu 'LobbyCompanionCorner\.Build\(' 'main menu delegates companion corner'
Require-Pattern $mainMenu 'LobbyDock\.Build\(' 'main menu delegates lobby dock'
if ((Get-Content -Encoding utf8 -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs')).Count -gt 1900) {
    $failures.Add('MainMenuBootstrap still exceeds the phase-one 1900-line composition boundary')
}
if ($mainMenu -match 'DrawPenguinMascot') {
    $failures.Add('MainMenuBootstrap still contains the legacy procedural mascot')
}

$panelHeader = Require-File 'Assets/Scripts/Runtime/Game/UI/PanelHeader.cs'
Require-Pattern $panelHeader 'UiTheme\.PaperWarm' 'panel header uses paper surface'
Require-Pattern $panelHeader 'UiTheme\.InkPrimary' 'panel header uses ink text'
Require-Pattern $panelHeader 'StorybookUi\.CreateSectionDivider' 'panel header uses stitched divider'
foreach ($panelFile in 'MissionPanel.cs', 'CodexPanel.cs', 'CampShopPanel.cs', 'SettingsPanel.cs') {
    $panelText = Require-File "Assets/Scripts/Runtime/Game/UI/$panelFile"
    Require-Pattern $panelText 'PanelHeader\.Create\(' "$panelFile uses the shared chapter header"
    Require-Pattern $panelText 'UiTheme\.PaperBase' "$panelFile uses the storybook page surface"
    if ($panelText -match 'UiBuilder\.MenuPanelSolidBg') {
        $failures.Add("$panelFile still uses the legacy dark menu background")
    }
}

$hudCanvas = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs'
foreach ($method in 'BindStatic', 'RefreshDynamic', 'SetBossMode', 'SetPaused', 'SetFeedback', 'Dispose') {
    Require-Pattern $hudCanvas "\b$method\b" "RunnerHudCanvas.$method"
}
Require-Pattern $hudCanvas 'EnsureEventSystem' 'retained HUD supports direct runner-scene launch'
$runnerGame = Require-File 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
Require-Pattern $runnerGame 'RunnerHudCanvas' 'runner game owns the retained HUD canvas'
Require-Pattern $runnerGame 'hudCanvas\.RefreshDynamic' 'runner game refreshes the retained HUD'
if ($runnerGame -match 'hud\.Draw\(') {
    $failures.Add('PenguinRunnerGame still draws runtime HUD statistics through IMGUI')
}
if (Test-Path -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHud.cs')) {
    $failures.Add('Legacy IMGUI RunnerHud.cs still exists after retained-HUD migration')
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Storybook UI phase-one static contracts passed.'
