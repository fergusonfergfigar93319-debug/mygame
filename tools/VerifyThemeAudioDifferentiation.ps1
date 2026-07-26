# Static gate for per-theme audio identity.
#
# Before this batch all six map themes shared one ambience clip and one set of
# impact sounds, so the only audible difference between them was the BGM track.
# This gate holds the line: every theme must carry a complete audio profile, and
# no two themes may resolve to the same bed or the same surface timbre.
#
# ASCII only on purpose. Without a BOM, PowerShell 5.1 reads these files as ANSI
# and non-ASCII comment bytes surface as misleading parse errors.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

$themes = @('IceLakeEcho', 'CedarRuins', 'AuroraField', 'MistDike', 'OceanReef', 'SkyFlight')

foreach ($file in @(
    'Assets/Scripts/Runtime/PenguinRun/ThemeAudioProfile.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerAudio.cs',
    'tools/generate_theme_audio.py'
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $file))) {
        $failures.Add("Missing: $file")
    }
}

$profilePath = Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/ThemeAudioProfile.cs'
if (Test-Path -LiteralPath $profilePath) {
    $profile = Get-Content -Raw -Encoding utf8 -LiteralPath $profilePath

    $ambienceByTheme = @{}
    $surfaceByTheme = @{}

    foreach ($theme in $themes) {
        # Each arm looks like:  RunnerMapTheme.X => new( "clip", p, v, "clip", p, c, b),
        $pattern = 'RunnerMapTheme\.' + $theme + '\s*=>\s*new\(\s*"([^"]+)"\s*,\s*([0-9.]+)f\s*,\s*([0-9.]+)f\s*,\s*"([^"]+)"\s*,\s*([0-9.]+)f'
        $match = [regex]::Match($profile, $pattern)
        if (-not $match.Success) {
            $failures.Add("Theme has no complete audio profile: $theme")
            continue
        }

        $ambienceByTheme[$theme] = $match.Groups[1].Value
        $surfaceByTheme[$theme] = $match.Groups[4].Value + '@' + $match.Groups[5].Value

        foreach ($clip in @($match.Groups[1].Value, $match.Groups[4].Value)) {
            $wav = Join-Path $ProjectRoot "Assets/Resources/PenguinRun/$clip.wav"
            $ogg = Join-Path $ProjectRoot "Assets/Resources/PenguinRun/$clip.ogg"
            if (-not (Test-Path -LiteralPath $wav) -and -not (Test-Path -LiteralPath $ogg)) {
                $failures.Add("Theme $theme references a missing clip: $clip")
            }
        }
    }

    # Sharing a bed or a surface timbre is exactly the regression this gate exists
    # to catch, so duplicates fail rather than warn.
    foreach ($group in ($ambienceByTheme.GetEnumerator() | Group-Object -Property Value)) {
        if ($group.Count -gt 1) {
            $failures.Add("Ambience bed '$($group.Name)' is shared by: " + (($group.Group | ForEach-Object { $_.Key }) -join ', '))
        }
    }
    foreach ($group in ($surfaceByTheme.GetEnumerator() | Group-Object -Property Value)) {
        if ($group.Count -gt 1) {
            $failures.Add("Surface timbre '$($group.Name)' is shared by: " + (($group.Group | ForEach-Object { $_.Key }) -join ', '))
        }
    }
}

function Require-Pattern([string]$relativePath, [string]$pattern, [string]$label) {
    $fullPath = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath) -or
        (Get-Content -Raw -Encoding utf8 -LiteralPath $fullPath) -notmatch $pattern) {
        $failures.Add("Missing contract: $label")
    }
}

# The bed has to follow a mid-run theme switch, or the picture changes and the
# sound stays on the previous map.
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' `
    'runnerAudio\.ApplyTheme' 'ambience follows the theme switch'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/RunnerAudio.cs' `
    'public void ApplyTheme' 'theme entry point on the audio system'
# A player who picked a fixed ambience keeps it; only the untouched default is themed.
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/RunnerAudio.cs' `
    'ambienceTrack != PlayerSave\.AmbienceThemed' 'explicit ambience choice is respected'
Require-Pattern 'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs' `
    'DefaultAmbienceTrack = AmbienceThemed' 'themed ambience is the default'
Require-Pattern 'Assets/Scripts/Runtime/Game/UI/SettingsPanel.cs' `
    'PlayerSave\.AmbienceThemed' 'themed ambience is selectable in settings'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}

Write-Host "PASS: all $($themes.Count) themes carry a distinct audio profile."
