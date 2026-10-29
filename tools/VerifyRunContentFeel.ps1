# Static gate for the 2026-09-21 run-content / run-feel batch.
# ASCII only (PowerShell 5.1 without BOM).
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-Required([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }
    return Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Strip-Comments([string]$content) {
    if ($null -eq $content) { return $null }
    return (($content -split "`n") | Where-Object { $_ -notmatch '^\s*//' }) -join "`n"
}

function Require([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

function Forbid([string]$content, [string]$pattern, [string]$description) {
    if ($null -ne $content -and (Strip-Comments $content) -match $pattern) {
        $failures.Add("Forbidden contract remains: $description")
    }
}

$motion     = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerMotion.cs'
$motor      = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs'
$config     = Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunnerTuningConfig.cs'
$spawner    = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'
$pickup     = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/PickupSystem.cs'
$controller = Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs'
$controller += Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Input.cs'
$waves      = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentWaveDirector.cs'
$hud        = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs'
$game       = Read-Required 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'
$adr        = Read-Required 'docs/adr/0005-defer-narrative-prioritize-run-feel.md'
$context    = Read-Required 'CONTEXT.md'

Require $motion 'internal static class RunnerMotion' 'RunnerMotion helper exists'
Require $motion 'public static float Follow01\(' 'frame-rate independent follow factor'
Require $motor 'RunnerMotion\.Follow01\(' 'PlayerMotor uses exponential follow instead of lerp * dt'
Forbid  $motor 'Mathf\.Lerp\(pos\.x, targetX, Mathf\.Clamp01\(response \* dt\)\)' 'lane follow still uses frame-rate dependent lerp * dt'

Require $config 'CameraFieldOfView\s*=\s*72f' 'play camera FOV matches FramingReference 72'
Require $config 'CameraBehindDistance\s*=\s*10.5f' 'play camera behind distance matches FramingReference'
Require $config 'CameraHeightAboveSurface\s*=\s*6.5f' 'play camera height matches FramingReference'

Require $spawner 'config\.IsOnboarding' 'spawner still honours onboarding'
Require $spawner 'PowerUpKind\.Dash' 'onboarding / forced first pickup is Dash'
Require $spawner 'TryConsumeDashHint' 'shatter-wall dash hint can be consumed'
Require $pickup 'onboardingDashHinted' 'first Dash pickup keeps a per-run teaching flag'
Require $pickup 'public void ResetRun\(\)' 'pickup teaching flag resets with the run'
$jinbi = ([char]0x91D1).ToString() + [char]0x5E01
Forbid  $pickup ("\+1 " + [regex]::Escape($jinbi)) 'collect feedback still uses jinbi instead of fish snacks'
Forbid  $pickup ([regex]::Escape($jinbi) + [char]0x8FDE + [char]0x51FB) 'combo feedback still uses jinbi'

Require $controller 'TryConsumeDashHint' 'controller consumes the shatter dash hint'
Require $controller 'ManualDashCooldownSeconds\s*=\s*2\.5f' 'manual dash cooldown is a named duration'
Require $controller 'public float ManualDashCooldown' 'controller exposes cooldown for the HUD'
$cooldownToast = ([char]0x51B2).ToString() + [char]0x523A + [char]0x51B7 + [char]0x5374
Forbid  $controller ([regex]::Escape($cooldownToast)) 'cooldown must not be toasted on the center feedback line'
Require $hud 'DashReady' 'HUD keeps a persistent dash readiness chip'
Require $hud 'RefreshDashChip' 'dash chip refreshes from cooldown, not every frame of string work'
Require $game 'game\.ManualDashCooldown' 'runner passes manual dash cooldown into the HUD'
Require $controller 'SegmentWaveDirector\.BannerCopy' 'controller shows readable segment banners'
Require $waves 'BannerCopy' 'wave director owns segment banner copy'
Require $waves 'lastPatternKind' 'wave director avoids immediate pattern repeats'
Require $adr 'Accepted' 'ADR-0005 is accepted'
Require $context 'RunContent' 'glossary names run content'
Require $context 'RunFeel' 'glossary names run feel'

if ($failures.Count -gt 0) {
    Write-Host "FAIL: VerifyRunContentFeel"
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}

Write-Host "PASS: VerifyRunContentFeel"
exit 0
