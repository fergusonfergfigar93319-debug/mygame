# Static gate for the device quality tiering.
#
# The previous tiering was dead code: RunnerPostFx branched on
# QualitySettings.GetQualityLevel() to decide whether to disable Bloom, but no
# code anywhere set that level from device capability, so every Android device
# read the same build default. The branch also sat behind
# "#if UNITY_ANDROID && !UNITY_EDITOR", so nothing in the Editor could ever
# exercise it and nothing could notice it had stopped working.
#
# This gate keeps the decision in one place and keeps it reachable from tests.
#
# ASCII only on purpose. Without a BOM, PowerShell 5.1 reads these files as ANSI
# and non-ASCII comment bytes surface as misleading parse errors.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $relativePath))) {
        $failures.Add("Missing: $relativePath")
    }
}

function Require-Pattern([string]$relativePath, [string]$pattern, [string]$label) {
    $fullPath = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath) -or
        (Get-Content -Raw -Encoding utf8 -LiteralPath $fullPath) -notmatch $pattern) {
        $failures.Add("Missing contract: $label")
    }
}

# Strips // and /// lines before matching. The regression guards below describe the
# defect they prevent, and those descriptions naturally quote the very code being
# banned -- matching raw text would make the gate fail on its own documentation.
function Forbid-Pattern([string]$relativePath, [string]$pattern, [string]$label) {
    $fullPath = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { return }
    $code = (Get-Content -Encoding utf8 -LiteralPath $fullPath |
        Where-Object { $_ -notmatch '^\s*//' }) -join "`n"
    if ($code -match $pattern) {
        $failures.Add("Forbidden: $label")
    }
}

Require-File 'Assets/Scripts/Runtime/PenguinRun/RunnerQualityTier.cs'
Require-File 'Assets/Scripts/Editor/PenguinRun/ProjectQualityTierValidation.cs'

$tierPath = 'Assets/Scripts/Runtime/PenguinRun/RunnerQualityTier.cs'
foreach ($tier in @('Low', 'Medium', 'High')) {
    Require-Pattern $tierPath ('RunnerQualityTier\.' + $tier + '\s*=>\s*new\(') "budget for the $tier tier"
}

# The decision must exist, be driven by device capability, and actually be written
# through to QualitySettings.
Require-Pattern $tierPath 'SystemInfo\.systemMemorySize' 'tier is chosen from device capability'
Require-Pattern $tierPath 'SystemInfo\.processorCount' 'tier considers core count'
Require-Pattern $tierPath 'SystemInfo\.graphicsMemorySize' 'tier considers graphics memory'
foreach ($lever in @('QualitySettings\.shadows', 'QualitySettings\.lodBias',
                     'QualitySettings\.particleRaycastBudget', 'QualitySettings\.skinWeights',
                     'QualitySettings\.globalTextureMipmapLimit')) {
    Require-Pattern $tierPath $lever "Apply writes $lever"
}

Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' `
    'RunnerDeviceTier\.Apply' 'runtime applies the detected tier'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/RunnerPostFx.cs' `
    'RunnerQualityBudget\.For\(RunnerDeviceTier\.Current\)' 'post-fx reads the shared tier budget'

# Regression guards for the two defects that made the old tiering inert.
Forbid-Pattern 'Assets/Scripts/Runtime/PenguinRun/RunnerPostFx.cs' `
    'QualitySettings\.GetQualityLevel' 'post-fx reads a quality level nothing sets'
Forbid-Pattern 'Assets/Scripts/Runtime/PenguinRun/RunnerPostFx.cs' `
    '#if\s+UNITY_ANDROID' 'tier decision is compiled out of the Editor and cannot be tested'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}

Write-Host 'PASS: quality tiering is device-driven, applied, and Editor-reachable.'
