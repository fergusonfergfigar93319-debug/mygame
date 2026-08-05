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
Require-Match $gameCs 'NaturePropCatalog\.' 'runner uses NaturePropCatalog'
Require-Match $gameCs 'RegisterSceneryProp\(go,\s*x,\s*z\)' 'open nature props register for track align'
Require-Match $gameCs 'NaturePropCatalog\.ApplyScale' 'open nature props apply pack scale'
Require-Match $gameCs 'prop_flag' 'Sky uses flag prop'
Require-Match $gameCs 'tent_detailedOpen' 'Cedar uses camp tent'
Require-Match $gameCs 'canoe' 'Ocean uses canoe prop'
Require-Match $gameCs 'sky_cloud' 'Sky uses cloud prop'
Require-Match $gameCs 'GetComponentsInChildren<Collider>' 'open props strip colliders'

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

$snModels = @('Pine_1','Bush_Common','Flower_3_Group','Mushroom_Common','DeadTree_1')
foreach ($name in $snModels) {
    Require-File "Assets/Resources/PenguinRun/Models/vendor_sn/$name.fbx" | Out-Null
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
