# Static gate for the storybook (paper) palette in the lobby/panel UI layer.
#
# The paper redesign was carried out by remapping the alias layer in UiBuilder --
# SurfaceBase, Surface1, Surface2 and WarmGold all moved onto storybook tokens.
# One alias was missed: AccentCyan still pointed at the legacy neon palette. That
# single omission is why the camp shop, codex, leaderboard, result screen and
# death screen all rendered their "selected" and "primary action" elements as
# neon cyan on cream paper.
#
# Nothing could have caught it, because the legacy tokens are still legitimately
# used by the in-run HUD (RunnerHudCanvas), which draws over the dark 3D scene.
# So this gate is scoped to the paper layer only.
#
# ASCII only on purpose. Without a BOM, PowerShell 5.1 reads these files as ANSI
# and non-ASCII comment bytes surface as misleading parse errors.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

$paperLayer = Join-Path $ProjectRoot 'Assets/Scripts/Runtime/Game/UI'
if (-not (Test-Path -LiteralPath $paperLayer)) {
    Write-Host "FAIL: missing paper UI layer at Assets/Scripts/Runtime/Game/UI"
    exit 1
}

# Comment lines are stripped first: the doc comments that explain this rule quote
# the very token being banned.
function Get-Code([string]$path) {
    return (Get-Content -Encoding utf8 -LiteralPath $path |
        Where-Object { $_ -notmatch '^\s*//' }) -join "`n"
}

$banned = @(
    @{ Pattern = 'UiTheme\.AccentCyan';
       Label   = 'legacy neon accent (use UiBuilder.AccentActive)' }
    @{ Pattern = 'UiTheme\.SurfaceBase|UiTheme\.Surface1|UiTheme\.Surface2|UiTheme\.SurfaceElevated';
       Label   = 'legacy dark surface (use the UiBuilder.Surface* aliases)' }
    @{ Pattern = 'UiTheme\.TextPrimary|UiTheme\.TextSecondary';
       Label   = 'legacy light-on-dark text (use UiBuilder.TextPrimary/TextSecondary)' }
)

foreach ($file in Get-ChildItem -LiteralPath $paperLayer -Recurse -Filter '*.cs') {
    $code = Get-Code $file.FullName
    foreach ($rule in $banned) {
        if ($code -match $rule.Pattern) {
            $failures.Add("$($file.Name): $($rule.Label)")
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

$uiBuilder = 'Assets/Scripts/Runtime/Game/UI/UiBuilder.cs'
Require-Pattern $uiBuilder 'AccentActive\s*=\s*UiTheme\.PineGreen' 'active accent is a storybook token'
# The post-run screens must speak the same CTA language as the lobby dock.
Require-Pattern $uiBuilder 'CreateButton\(name, parent, label, UiTheme\.CoralAction' 'primary CTA uses the lobby coral'
Require-Pattern $uiBuilder 'CreateButton\(name, parent, label, UiTheme\.MistBlue' 'secondary button sits on paper'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}

Write-Host 'PASS: the paper UI layer references no legacy neon-palette tokens.'
