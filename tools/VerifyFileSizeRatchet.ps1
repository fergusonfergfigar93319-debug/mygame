# File-size ratchet for the oversized runtime files.
#
# The stated risk in GAME_OPTIMIZATION_PLAN is that edits to Boss, apparatus and
# pickup code interfere with each other because they all land in the same few
# very large files. Splitting them safely needs runtime verification that this
# environment cannot currently provide (no Android device, and the journey smoke
# test cannot capture frames in batchmode), so this gate does the part that is
# verifiable: it stops the bleeding.
#
# Each ceiling below is the file's line count at the time it was recorded. A file
# may shrink freely -- shrinking is the goal -- and the gate reports headroom so
# the ceilings can be lowered as work lands. Growing past the ceiling fails.
#
# When a split lands, lower the ceiling in the same change. Raising a ceiling is
# a deliberate decision that belongs in review, which is the point.
#
# ASCII only on purpose. Without a BOM, PowerShell 5.1 reads these files as ANSI
# and non-ASCII comment bytes surface as misleading parse errors.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

# Recorded 2026-07-26, set roughly 2.5% above the measured count. That is enough
# slack for a bug fix or a comment without tripping the gate, and far too little
# to absorb a new feature dropped into an already-oversized file.
$ceilings = [ordered]@{
    'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs'      = 2400
    'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs'           = 2180 # lowered 2026-09-17: IMGUI overlay moved out
    'Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs'       = 1900
    'Assets/Scripts/Runtime/Game/UI/CampShopPanel.cs'                  = 1820
    'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'              = 1390
    'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs'        = 1190
}

$rows = @()
foreach ($entry in $ceilings.GetEnumerator()) {
    $path = Join-Path $ProjectRoot $entry.Key
    if (-not (Test-Path -LiteralPath $path)) {
        # A tracked file disappearing usually means it was split or renamed; the
        # ratchet entry has to be updated in the same change.
        $failures.Add("Ratchet entry points at a missing file: $($entry.Key)")
        continue
    }

    $count = (Get-Content -LiteralPath $path).Count
    $ceiling = [int]$entry.Value
    $rows += [pscustomobject]@{
        File     = Split-Path $entry.Key -Leaf
        Lines    = $count
        Ceiling  = $ceiling
        Headroom = $ceiling - $count
    }

    if ($count -gt $ceiling) {
        $failures.Add("$($entry.Key): $count lines exceeds the $ceiling ceiling by $($count - $ceiling). Split it or justify raising the ceiling.")
    }
}

$rows | Format-Table -AutoSize | Out-String | Write-Host

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}

Write-Host "PASS: all $($rows.Count) tracked files are within their size ceiling."
