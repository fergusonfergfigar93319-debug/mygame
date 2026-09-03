param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing Image 2 asset: $relativePath")
        return $null
    }
    Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require-Pattern([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

$assetRoot = 'Assets/Resources/PenguinRun/StorybookUI/Image2'
foreach ($asset in 'camp_panorama_image2','chapter_card_image2','ice_run_backdrop_image2',
    'result_celebration_image2','codex_boss_gallery_image2','shop_supply_board_image2','journal_map_image2') {
    Require-File "$assetRoot/$asset.png" | Out-Null
    $meta = Require-File "$assetRoot/$asset.png.meta"
    Require-Pattern $meta 'TextureImporter:' "$asset has a Unity texture importer"
    Require-Pattern $meta 'textureType:\s*8' "$asset imports as a Unity Sprite"
}

$catalog = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/CampVisualAssetCatalog.cs'
foreach ($path in 'camp_panorama_image2','chapter_card_image2','ice_run_backdrop_image2') {
    Require-Pattern $catalog ([regex]::Escape($path)) "catalog path for $path"
}

$image2Catalog = Require-File 'Assets/Scripts/Runtime/Game/UI/Image2ArtCatalog.cs'
foreach ($path in 'result_celebration_image2','codex_boss_gallery_image2','shop_supply_board_image2','journal_map_image2') {
    Require-Pattern $image2Catalog ([regex]::Escape($path)) "secondary catalog path for $path"
}

$chapter = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyChapterPage.cs'
Require-Pattern $chapter 'CampVisualAssetCatalog\.ChapterBackdrop' 'chapter card consumes the Image 2 backdrop'

$transition = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/SceneTransitionManager.cs'
Require-Pattern $transition 'CampVisualAssetCatalog\.RunBackdrop' 'loading page consumes the Image 2 run backdrop'

$journal = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyJournalPage.cs'
Require-Pattern $journal 'Image2ArtCatalog\.JournalMap' 'journal page consumes the Image 2 map art'

$codex = Require-File 'Assets/Scripts/Runtime/Game/UI/CodexPanel.cs'
Require-Pattern $codex 'Image2ArtCatalog\.CodexGallery' 'codex consumes the Image 2 boss gallery art'

$shop = Require-File 'Assets/Scripts/Runtime/Game/UI/CampShopPanel.cs'
Require-Pattern $shop 'Image2ArtCatalog\.ShopSupply' 'shop consumes the Image 2 supply board art'

$result = Require-File 'Assets/Scripts/Runtime/Game/UI/RunResultOverlay.cs'
Require-Pattern $result 'Image2ArtCatalog\.ResultCelebration' 'result overlay consumes the Image 2 celebration art'

$death = Require-File 'Assets/Scripts/Runtime/Game/UI/RunnerDeathOverlay.cs'
Require-Pattern $death 'Image2ArtCatalog\.ResultCelebration' 'death card consumes the Image 2 celebration art'

$profile = Require-File 'Assets/Scripts/Runtime/Game/UI/ProfileStatsPanel.cs'
Require-Pattern $profile 'Image2ArtCatalog\.JournalMap' 'profile sheet consumes the Image 2 journal art'

$hud = Require-File 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs'
Require-Pattern $hud 'CampVisualAssetCatalog\.RunBackdrop' 'pause card consumes the Image 2 run backdrop'

$vignette = Require-File 'Assets/Scripts/Runtime/Game/UI/UiBuilder.cs'
Require-Pattern $vignette 'AttachArtVignette' 'paper vignettes share one attach helper'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Image 2 art static contracts passed.'
