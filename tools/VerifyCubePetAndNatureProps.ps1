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
Require-Match $avatarCs 'stridePhase' 'CubePet has stride-driven run motion'
Require-Match $avatarCs 'ApplyWarmPaletteBoost' 'CubePet warms near-white materials'
Require-Match $avatarCs 'landSquashTimer' 'CubePet has landing squash'

$gameCs = Require-File 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
Require-Match $gameCs 'CubePetPenguinCharacter\.TryCreate' 'runner prefers CubePet protagonist'
Require-Match $gameCs 'NaturePropCatalog\.' 'runner uses NaturePropCatalog'
Require-Match $gameCs 'ThemeRoadsideDressing\.PlaceFarBelt' 'runner places the mid-ground far belt'
Require-Match $gameCs 'RegisterSceneryProp\(go,\s*x,\s*z,\s*snapToGround\)' 'open nature props register for track align'
Require-Match $gameCs 'NaturePropCatalog\.ApplyScale' 'open nature props apply pack scale'
Require-Match $gameCs 'prop_flag' 'Sky uses flag prop'
Require-Match $gameCs 'tent_detailedOpen' 'Cedar uses camp tent'
Require-Match $gameCs 'canoe' 'Ocean uses canoe prop'
Require-Match $gameCs 'sky_cloud' 'Sky uses cloud prop'
Require-Match $gameCs 'GetComponentsInChildren<Collider>' 'open props strip colliders'
Require-Match $gameCs 'EnvironmentPropId\.Light' 'cube lamps replaced by theme light sculptures'
Require-Match $gameCs 'EnvironmentPropId\.Signpost' 'cube flags replaced by theme signpost sculptures'
Require-Match $gameCs 'NaturePropCatalog\.IceCrystal' 'crystals use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.SnowBank' 'snow banks use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.OceanWeed' 'seaweed uses catalog models'
Require-Match $gameCs 'NaturePropCatalog\.OceanShell' 'seashells use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.CoralReef' 'coral set pieces use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.TreasureChest' 'chests use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.CedarRuin' 'cedar ruins use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.AuroraBeacon' 'aurora beacons use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.IceArch' 'ice arches use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.SkyArch' 'sky arches use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.MistVeil' 'mist veils use catalog models'
Require-Match $gameCs 'NaturePropCatalog\.SkyCloud' 'floating clouds use sky_cloud'
Require-Match $gameCs 'bool snapToGround' 'open props can spawn off the ground'
if ($gameCs -match 'AddLampGroup|AddBannerFlag|Warm Track Lamp|Penguin Run Banner|Seaweed Cluster|Aurora Crystal Cluster') {
    $failures.Add('runner still builds cube lamp/flag/weed/crystal placeholders')
}
if ($gameCs -match 'Coral Stalk|Chest Base|Broken Cedar Post|Beacon Crystal|Low Fog Veil|Arch Pillar|Cloud Main|Floating Stone') {
    $failures.Add('runner still builds cube theme set-piece placeholders')
}

$catalogCs = Require-File 'Assets/Scripts/Runtime/PenguinRun/Visuals/NaturePropCatalog.cs'
Require-Match $catalogCs 'class\s+NaturePropCatalog' 'NaturePropCatalog type'
Require-Match $catalogCs 'vendor_un_PineTree_Snow_1' 'catalog IceLake snow pine'
Require-Match $catalogCs 'vendor_Bush_2_A_Color1' 'catalog mixes KayKit bush'
Require-Match $catalogCs 'vendor_Tree_Bare_1_A_Color1' 'catalog mixes KayKit bare tree'
Require-Match $catalogCs 'vendor_Rock_2_A_Color1' 'catalog mixes KayKit rock'
Require-Match $catalogCs 'vendor_sn/Pine_1' 'catalog Cedar stylized pine'
Require-Match $catalogCs 'vendor_un_PalmTree_1' 'catalog Ocean palm'
Require-Match $catalogCs 'vendor_un_Rock_1' 'catalog Sky rock accent'
Require-Match $catalogCs 'ScaleFor' 'catalog pack scale multipliers'
Require-Match $catalogCs 'IceLakeFar' 'catalog IceLake far-belt models'
Require-Match $catalogCs 'CedarFar' 'catalog Cedar far-belt models'
Require-Match $catalogCs 'AuroraFar' 'catalog Aurora far-belt models'
Require-Match $catalogCs 'MistFar' 'catalog Mist far-belt models'
Require-Match $catalogCs 'OceanFar' 'catalog Ocean far-belt models'
Require-Match $catalogCs 'SkyFar' 'catalog Sky far-belt models'
Require-Match $catalogCs 'IceCrystal' 'catalog ice crystal models'
Require-Match $catalogCs 'SnowBank' 'catalog snow bank models'
Require-Match $catalogCs 'OceanWeed' 'catalog ocean weed models'
Require-Match $catalogCs 'OceanShell' 'catalog ocean shell models'
Require-Match $catalogCs 'CoralReef' 'catalog coral reef models'
Require-Match $catalogCs 'TreasureChest' 'catalog treasure chest model'
Require-Match $catalogCs 'CedarRuin' 'catalog cedar ruin models'
Require-Match $catalogCs 'AuroraBeacon' 'catalog aurora beacon models'
Require-Match $catalogCs 'IceArch' 'catalog ice arch model'
Require-Match $catalogCs 'SkyArch' 'catalog sky arch models'
Require-Match $catalogCs 'MistVeil' 'catalog mist veil models'
Require-Match $catalogCs 'SkyCloud' 'catalog sky cloud model'

$dressingCs = Require-File 'Assets/Scripts/Runtime/PenguinRun/Visuals/ThemeRoadsideDressing.cs'
Require-Match $dressingCs 'class\s+ThemeRoadsideDressing' 'ThemeRoadsideDressing type'
Require-Match $dressingCs 'PlaceFarBelt' 'far belt placement entry'
Require-Match $dressingCs 'MinAbsX\s*=\s*13\.2f' 'far belt stays outside the 8.5m corridor'

$fbxModels = @(
    'tent_detailedOpen','canoe','prop_flag','crate_strong',
    'vendor_un_PineTree_Snow_1','vendor_un_BirchTree_Snow_1','vendor_un_Rock_Snow_1',
    'vendor_un_BirchTree_Autumn_1','vendor_un_Willow_Autumn_1','vendor_un_Rock_Moss_1',
    'vendor_un_PalmTree_1','vendor_un_PineTree_1',
    'vendor_Tree_2_A_Color1','vendor_Bush_1_A_Color1'
)
foreach ($name in $fbxModels) {
    Require-File "Assets/Resources/PenguinRun/Models/$name.fbx" | Out-Null
    $meta = Require-File "Assets/Resources/PenguinRun/Models/$name.fbx.meta"
    Require-Match $meta 'ModelImporter:' "$name meta has ModelImporter"
}

$snModels = @('Pine_1','Bush_Common','Flower_3_Group','Mushroom_Common','DeadTree_1','Rock_Medium_1','Plant_1_Big','Fern_1','Grass_Common_Tall','Mushroom_Laetiporus','Bush_Common_Flowers')
foreach ($name in $snModels) {
    Require-File "Assets/Resources/PenguinRun/Models/vendor_sn/$name.fbx" | Out-Null
}

$farModels = @('tree_pine_snow_small','block_snow_large','log_large','tree_palmTall','cliff_block_rock',
    'vendor_un_Willow_Autumn_1','vendor_un_Willow_Dead_1','vendor_un_CommonTree_Autumn_1',
    'vendor_Tree_2_A_Color1','vendor_Tree_3_A_Color1')
foreach ($name in $farModels) {
    Require-File "Assets/Resources/PenguinRun/Models/$name.fbx" | Out-Null
}

$swapModels = @('stone_tallA','block_snow','block_snow_hexagon','flower_yellowA','vendor_un_Lilypad','vendor_Grass_2_A_Color1',
    'mushroom_redTall','mushroom_tanGroup','log_single','campfire_stones','cliff_half_stone','cliff_block_stone',
    'prop_crate','rock_tallA','vendor_un_WoodLog_Moss','vendor_un_BushBerries_1','vendor_un_Flowers','vendor_Bush_4_A_Color1')
foreach ($name in $swapModels) {
    Require-File "Assets/Resources/PenguinRun/Models/$name.fbx" | Out-Null
}

Require-File 'Assets/Resources/PenguinRun/Models/kaykit_forest_texture.png' | Out-Null
Require-File 'Assets/Resources/PenguinRun/Models/vendor_sn/Leaves.png' | Out-Null
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
Require-Match $credits 'KayKit Forest Nature Pack' 'credits mention KayKit Forest'
Require-Match $credits 'Ultimate Nature Pack' 'credits mention Ultimate Nature'
Require-Match $credits 'Stylized Nature MegaKit' 'credits mention Stylized Nature'

$importer = Require-File 'Assets/Scripts/Editor/PenguinRun/VendorCc0PackImporter.cs'
Require-Match $importer 'VendorCc0PackImporter' 'vendor pack importer exists'
Require-Match $importer 'skip Kenney|kenney' 'importer skips Kenney archives'
Require-Match $importer 'KayKitForest' 'importer recognizes KayKit pack kind'
Require-Match $importer 'UltimateNature' 'importer recognizes Ultimate Nature pack kind'
Require-Match $importer 'StylizedNature' 'importer recognizes Stylized Nature pack kind'
Require-Match $importer 'UltimateAllow' 'importer has Ultimate allow list'
Require-Match $importer 'StylizedAllow' 'importer has Stylized allow list'

$repair = Require-File 'Assets/Scripts/Editor/PenguinRun/VendorModelImportRepair.cs'
Require-Match $repair 'VendorModelImportRepair' 'vendor model import repair exists'
Require-Match $repair 'useFileScale' 'repair enables useFileScale'

$preview = Require-File 'Assets/Scripts/Editor/PenguinRun/ProjectOpenNaturePreviewCapture.cs'
Require-Match $preview 'ProjectOpenNaturePreviewCapture' 'open nature preview capture exists'
Require-Match $preview 'CaptureAllFromCommandLine' 'open nature preview has CLI entry'
Require-Match $preview 'NaturePropCatalog' 'open nature preview uses NaturePropCatalog'
Require-Match $preview 'IceLakeFar|CedarFar|AuroraFar|MistFar|OceanFar|SkyFar' 'open nature preview covers far-belt models'
Require-Match $preview 'IceCrystal|SnowBank|OceanWeed|OceanShell' 'open nature preview covers cube-swap models'
Require-Match $preview 'CoralReef|CedarRuin|AuroraBeacon|IceArch|SkyArch|MistVeil' 'open nature preview covers theme set-piece models'
Require-Match $preview 'RenderTexture' 'open nature preview uses RenderTexture path'

$dock = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyDock.cs'
Require-Match $dock 'primary:\s*false' 'dock Run is not a second Coral primary'
Require-Match $dock '!string\.IsNullOrEmpty\(panelName\)' 'home dock leaves chapter CTA as sole Coral'

if ($failures.Count -gt 0) {
    Write-Host 'FAIL: VerifyCubePetAndNatureProps'
    $failures | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host 'PASS: Cube Pet protagonist and theme roadside open models are locked.'
exit 0
