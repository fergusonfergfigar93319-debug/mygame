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

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Storybook UI phase-one static contracts passed.'
