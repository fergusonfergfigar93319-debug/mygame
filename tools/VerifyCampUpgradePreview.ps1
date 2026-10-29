# Source-contract gate for the camp upgrade duration preview.
# Keep this file ASCII-only for Windows PowerShell 5.1.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-Code([string]$path) {
    $code = Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw -Encoding UTF8
    return [regex]::Replace($code, '(?s)/\*.*?\*/|(?m)//[^\r\n]*', '')
}

function Expect([bool]$condition, [string]$message) {
    if (-not $condition) { $failures.Add($message) }
}

$upgrades = Read-Code 'Assets/Scripts/Runtime/Game/Save/CampUpgrades.cs'
$tuning = Read-Code 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerTuning.cs'
$panel = Read-Code 'Assets/Scripts/Runtime/Game/UI/CampShopPanel.cs'
$configFields = @(
    'DashBaseSeconds', 'DashPerLevelSeconds',
    'MagnetBaseSeconds', 'MagnetPerLevelSeconds',
    'ShieldBaseSeconds', 'ShieldPerTuanLevelSeconds',
    'GlideBaseSeconds', 'GlidePerTuanLevelSeconds',
    'ScoreStarBaseSeconds', 'ScoreStarPerPolarLevelSeconds',
    'DoubleFishBaseSeconds', 'DoubleFishPerPolarLevelSeconds',
    'HourglassBaseSeconds', 'HourglassPerPolarLevelSeconds',
    'PowerUpChancePerPolarLevel'
)

Expect ($upgrades.Contains('RunnerTuningConfig.Default()')) 'Camp previews must consume the default runner tuning config.'
Expect ($upgrades.Contains('private static readonly RunnerTuningConfig PreviewTuning')) 'Preview tuning should be cached once.'
foreach ($field in $configFields) {
    Expect ($upgrades.Contains("PreviewTuning.$field")) "Camp previews do not read $field from the default tuning config."
}
foreach ($mapping in @(
    'dashBaseSeconds = c.DashBaseSeconds',
    'dashPerLevelSeconds = c.DashPerLevelSeconds',
    'magnetBaseSeconds = c.MagnetBaseSeconds',
    'magnetPerLevelSeconds = c.MagnetPerLevelSeconds',
    'shieldBaseSeconds = c.ShieldBaseSeconds',
    'shieldPerLevelSeconds = c.ShieldPerTuanLevelSeconds',
    'glideBaseSeconds = c.GlideBaseSeconds',
    'glidePerLevelSeconds = c.GlidePerTuanLevelSeconds',
    'scoreStarBaseSeconds = c.ScoreStarBaseSeconds',
    'scoreStarPerLevelSeconds = c.ScoreStarPerPolarLevelSeconds',
    'doubleFishBaseSeconds = c.DoubleFishBaseSeconds',
    'doubleFishPerPolarLevelSeconds = c.DoubleFishPerPolarLevelSeconds',
    'hourglassBaseSeconds = c.HourglassBaseSeconds',
    'hourglassPerPolarLevelSeconds = c.HourglassPerPolarLevelSeconds',
    'powerUpChancePerPolarLevel = c.PowerUpChancePerPolarLevel'
)) {
    Expect ($tuning.Contains($mapping)) "Gameplay tuning mapping is missing: $mapping"
}

foreach ($hook in @(
    'kind == CampUpgradeKind.Polar ? 90f',
    'kind == CampUpgradeKind.Tuan ? 64f : 46f',
    '240f + previewHeight - 46f',
    'new Vector2(0f, previewHeight)',
    'previewHeight - 46f) * 0.5f'
)) {
    Expect ($panel.Contains($hook)) "Multi-line preview layout hook is missing: $hook"
}

$overrideFields = @($configFields)
$scriptGuid = 'a3e28f2697984be8b4ea674ddf21fae8'
$assetPaths = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Recurse -File -Include '*.unity', '*.prefab'
$runnerInstances = 0
foreach ($asset in $assetPaths) {
    $yaml = Get-Content -LiteralPath $asset.FullName -Raw -Encoding UTF8
    $blocks = [regex]::Matches($yaml, '(?ms)^--- !u!114 .*?(?=^--- !u!|\z)')
    foreach ($block in $blocks) {
        $body = $block.Value
        if ($body -notmatch "m_Script:\s*\{[^\r\n]*guid:\s*$scriptGuid(?:,|\s*\})" -and
            $body -notmatch 'm_EditorClassIdentifier:\s*Assembly-CSharp::PenguinRun\.PenguinRunnerGame') { continue }
        $runnerInstances++
        foreach ($field in $overrideFields) {
            if ($body -match "(?m)^\s*$([regex]::Escape($field))\s*:") {
                $failures.Add("Runner instance in $($asset.FullName) overrides preview default $field.")
            }
        }
    }

    foreach ($field in $overrideFields) {
        if ($yaml -match "(?m)^\s*propertyPath:\s*tuningConfig\.$([regex]::Escape($field))\s*$") {
            $failures.Add("Serialized prefab override in $($asset.FullName) changes tuningConfig.$field.")
        }
    }
}
Expect ($runnerInstances -gt 0) 'No shipped PenguinRunnerGame scene or prefab instance was found for override scanning.'

if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host "PASS: default preview source, runtime mapping, multi-line layout, and $runnerInstances runner asset instance(s) without independent tuning overrides."
