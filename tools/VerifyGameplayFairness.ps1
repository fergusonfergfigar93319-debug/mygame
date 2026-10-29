# Source-contract gate; use TestGameplayFairness.ps1 for behavioral assertions.
# Keep this file ASCII-only for Windows PowerShell 5.1.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
function Read-Code([string]$path) {
    $code = Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw -Encoding UTF8
    return [regex]::Replace($code, '(?s)/\*.*?\*/|(?m)//[^\r\n]*', '')
}
function Expect([bool]$condition, [string]$message) { if (-not $condition) { $failures.Add($message) } }
$collision = Read-Code 'Assets/Scripts/Runtime/PenguinRun/Systems/CollisionSystem.cs'
$director = Read-Code 'Assets/Scripts/Runtime/PenguinRun/Systems/SegmentWaveDirector.cs'
$guard = $collision.IndexOf('!behavior.IsCollidable')
$nearStart = $collision.IndexOf('if (overlapsNow && onNearMiss != null')
$nearEnd = if ($nearStart -ge 0) { $collision.IndexOf('nearMissed.Add(obstacle)', $nearStart) } else { -1 }
Expect ($guard -ge 0 -and $nearStart -gt $guard) 'Inactive obstacle guard must precede all near-miss reward checks.'
Expect ($nearStart -ge 0 -and $nearEnd -gt $nearStart) 'Near-miss reward must retain per-obstacle deduplication.'
if ($nearStart -ge 0 -and $nearEnd -gt $nearStart) {
    $nearCondition = $collision.Substring($nearStart, $nearEnd - $nearStart)
    foreach ($token in @('slideTimerSeconds <= 0f', 'motor.Grounded', '!motor.IsLowPosture', '!perfectDodged.Contains(obstacle)')) {
        Expect ($nearCondition.Contains($token)) "Near-miss eligibility is missing: $token"
    }
}
Expect ($collision -match 'motor\.PerfectActionWindow\s*>\s*0f\s*&&\s*!perfectDodged\.Contains\(obstacle\)\s*&&\s*!nearMissed\.Contains\(obstacle\)') 'Perfect-dodge reward must exclude previously near-missed obstacles.'
Expect ($director.Contains('if (pool[i].PatternKind != lastPatternKind) eligibleCount++;')) 'Selection must count eligible definitions, not retry random draws.'
Expect ($director.Contains('Random.Range(0, eligibleCount)')) 'Selection must draw from eligible definitions only.'
Expect ($director.Contains('if (pool[i].PatternKind == lastPatternKind) continue;')) 'Selection must skip the previous pattern.'
Expect ($director.Contains('if (eligibleCount == 0) return pool[Random.Range(0, pool.Length)];')) 'Single-pattern pools must remain playable.'
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: collision-scoring and segment-variety source contracts.'
