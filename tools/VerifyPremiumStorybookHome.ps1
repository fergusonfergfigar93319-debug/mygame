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

foreach ($asset in 'camp_bonfire_atlas','camp_workshop_atlas','camp_companion_hut_atlas','camp_library_atlas','camp_lookout_atlas','camp_foreground_props','camp_smoke_soft','camp_glow_soft') {
    Read-Required "Assets/Resources/PenguinRun/StorybookUI/PremiumHome/$asset.png" | Out-Null
}

$campScene = Read-Required 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCampScene.cs'
foreach ($method in 'Bind','SetInputEnabled','SetPanelOpen') {
    Require $campScene "\b$method\s*\(" "LobbyCampScene.$method"
}
Require $campScene 'CampVisualAssetCatalog\.Glow' 'premium bonfire glow is integrated into the camp scene'
Require $campScene 'CampVisualAssetCatalog\.Smoke' 'premium bonfire smoke is integrated into the camp scene'

$mainMenu = Read-Required 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'
foreach ($call in 'LobbyTopRibbon\.Build\(','LobbyJournalPage\.Build\(','LobbyChapterPage\.Build\(','LobbyCampScene\.Build\(') {
    Require $mainMenu $call "MainMenuBootstrap delegates $call"
}
if ($mainMenu -match 'LobbyStoryCard\.Build\(') { $failures.Add('Legacy combined LobbyStoryCard is still constructed.') }
if ($mainMenu -match 'LobbyCompanionCorner\.Build\(') { $failures.Add('Legacy companion card is still constructed.') }

$layoutRegression = Read-Required 'Assets/Editor/ProjectUiLayoutRegressionTest.cs'
Require $layoutRegression 'ValidatePremiumHomeLayouts\s*\(' 'premium home multi-resolution layout regression'
foreach ($resolution in '1920, 1080','2560, 1080','1280, 720') {
    Require $layoutRegression ([regex]::Escape($resolution)) "premium home layout resolution $resolution"
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}
Write-Output 'Premium storybook home static contracts passed.'
