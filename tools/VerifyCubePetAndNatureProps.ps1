# Static gate: Kenney Cube Pet protagonist + theme roadside open models exist.
# ASCII only (PowerShell 5.1 without BOM).
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$rel) {
    $path = Join-Path $ProjectRoot $rel
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("missing $rel")
        return $null
    }
    return Get-Content -LiteralPath $path -Raw
}

function Require-Match([string]$text, [string]$pattern, [string]$label) {
    if ($null -eq $text) { return }
    if ($text -notmatch $pattern) { $failures.Add($label) }
}

Require-File 'Assets/Resources/PenguinRun/Characters/CubePetPenguin/animal-penguin.fbx' | Out-Null
$penguinMeta = Require-File 'Assets/Resources/PenguinRun/Characters/CubePetPenguin/animal-penguin.fbx.meta'
Require-Match $penguinMeta 'ModelImporter:' 'cube pet penguin meta has ModelImporter'
Require-Match $penguinMeta 'guid:' 'cube pet penguin meta has guid'

$avatarCs = Require-File 'Assets/Scripts/Runtime/PenguinRun/Avatar/CubePetPenguinCharacter.cs'
Require-Match $avatarCs 'class\s+CubePetPenguinCharacter' 'CubePetPenguinCharacter type'
Require-Match $avatarCs 'IPlayerCharacterPresentation' 'CubePet implements presentation interface'
Require-Match $avatarCs 'TryCreate' 'CubePet TryCreate entry'

$gameCs = Require-File 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
Require-Match $gameCs 'CubePetPenguinCharacter\.TryCreate' 'runner prefers CubePet protagonist'
Require-Match $gameCs 'tree_pine_snow' 'IceLake uses snow pine prop'
Require-Match $gameCs 'tree_pineTallC_detailed' 'Cedar uses detailed pine'
Require-Match $gameCs 'tree_detailed_fall' 'Aurora uses fall tree'
Require-Match $gameCs 'mushroom_redTall' 'Mist uses tall mushrooms'
Require-Match $gameCs 'tree_palmTall' 'Ocean uses tall palm'
Require-Match $gameCs 'prop_flag' 'Sky uses flag prop'
Require-Match $gameCs 'tent_detailedOpen' 'Cedar uses camp tent'
Require-Match $gameCs 'canoe' 'Ocean uses canoe prop'
Require-Match $gameCs 'CubePets/cubepet_polar' 'IceLake uses cube pet polar'
Require-Match $gameCs 'sky_cloud' 'Sky uses cloud prop'
Require-Match $gameCs 'GetComponentsInChildren<Collider>' 'open props strip colliders'

$fbxModels = @(
    'tree_pine_snow','tree_snow','block_snow_large',
    'tree_pineTallC_detailed','tent_detailedOpen','campfire_stones',
    'tree_detailed_fall','tree_thin_fall','flower_purpleA',
    'mushroom_redTall','cliff_half_stone','plant_bushDetailed',
    'tree_palmTall','canoe','prop_flag','crate_strong'
)
foreach ($name in $fbxModels) {
    Require-File "Assets/Resources/PenguinRun/Models/$name.fbx" | Out-Null
    $meta = Require-File "Assets/Resources/PenguinRun/Models/$name.fbx.meta"
    Require-Match $meta 'ModelImporter:' "$name meta has ModelImporter"
}

Require-File 'Assets/Resources/PenguinRun/Models/sky_cloud.glb' | Out-Null
$petModels = @(
    'CubePets/cubepet_polar','CubePets/cubepet_deer','CubePets/cubepet_chick',
    'CubePets/cubepet_fox','CubePets/cubepet_fish','CubePets/cubepet_crab'
)
foreach ($name in $petModels) {
    Require-File "Assets/Resources/PenguinRun/Models/$name.fbx" | Out-Null
}

$credits = Require-File 'Assets/Resources/PenguinRun/OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt'
Require-Match $credits 'Cube Pets' 'credits mention Cube Pets'
Require-Match $credits 'Platformer Kit' 'credits mention Platformer Kit'
Require-Match $credits 'Starter-Kit-3D-Platformer|sky_cloud|cloud.glb' 'credits mention starter cloud'

$importer = Require-File 'Assets/Scripts/Editor/PenguinRun/VendorCc0PackImporter.cs'
Require-Match $importer 'VendorCc0PackImporter' 'vendor pack importer exists'

if ($failures.Count -gt 0) {
    Write-Host 'FAIL: VerifyCubePetAndNatureProps'
    $failures | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host 'PASS: Cube Pet protagonist and theme roadside open models are locked.'
exit 0
