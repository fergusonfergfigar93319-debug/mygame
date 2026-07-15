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
foreach ($id in 'FishSnack','Dash','Magnet','Shield','ScoreStar','GlideFeather') {
    Require $pickupIds "\b$id\b" "PickupVisualId.$id"
}
foreach ($id in 'Landmark','Signpost','Light','EdgeProp') {
    Require $propIds "\b$id\b" "EnvironmentPropId.$id"
}

$spawner = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
Require $spawner 'visualFactory\.TryAttachObstacle\(' 'SegmentSpawner custom obstacle path'
Require $spawner 'visualFactory\.TryAttachPickup\(' 'SegmentSpawner custom pickup path'
$runner = Read-Required 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
Require $runner 'visualFactory\.TryAttachEnvironment\(' 'runner custom environment path'

$prefabBuilder = Read-Required 'Assets/Scripts/Editor/PenguinRun/PenguinVisualPrefabBuilder.cs'
Require $prefabBuilder 'Build Batch One Prefabs' 'batch-one prefab builder menu'
Require $prefabBuilder '\bLODGroup\b' 'batch-one prefab LOD construction'
$assetValidation = Read-Required 'Assets/Scripts/Editor/PenguinRun/ProjectVisualAssetValidation.cs'
Require $assetValidation '\bRunNow\s*\(' 'custom visual validation entry point'
Require $assetValidation 'ValidateFromCommandLine\s*\(' 'non-mutating command-line validation entry point'
Require $assetValidation 'Validate Batch One' 'custom visual validation menu'
Require $assetValidation 'ValidateMissingFallback\s*\(' 'missing custom visual fallback regression'
Require $assetValidation 'ValidatePoolReset\s*\(' 'pooled pickup custom visual reset regression'
$previewCapture = Read-Required 'Assets/Scripts/Editor/PenguinRun/ProjectVisualPreviewCapture.cs'
Require $previewCapture 'CaptureAllFromCommandLine\s*\(' 'preview capture command-line entry point'
Require $previewCapture 'HasVisibleContent\s*\(' 'blank preview rejection'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}
Write-Output 'Custom map asset batch-one static contracts passed.'
