param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
# Locks the GDD M1 "effects must be real" contracts (docs/GDD.md section 15.1, G1/G2/G4/G5).
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
function Expect([bool]$condition, [string]$message) { if (-not $condition) { $failures.Add($message) } }
function Src([string]$rel) { Get-Content -LiteralPath (Join-Path $ProjectRoot $rel) -Raw -Encoding UTF8 }

$runtime = Join-Path $ProjectRoot 'Assets/Scripts/Runtime'
$models = Src 'Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeModels.cs'
$state = Src 'Assets/Scripts/Runtime/PenguinRun/Systems/RunUpgradeState.cs'
$allOther = (Get-ChildItem -LiteralPath $runtime -Recurse -Filter *.cs |
    Where-Object { $_.Name -ne 'RunUpgradeState.cs' } |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 }) -join "`n"

# G1: every journey-note effect is exposed, and every exposed property has a gameplay reader.
$enumMatch = [regex]::Match($models, 'enum RunUpgradeEffect\s*\{([^}]*)\}')
Expect $enumMatch.Success 'RunUpgradeEffect enum not found.'
$effects = $enumMatch.Groups[1].Value -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
Expect ($effects.Count -ge 12) "Expected at least 12 upgrade effects, found $($effects.Count)."
foreach ($effect in $effects) {
    $prop = [regex]::Match($state, 'public float (\w+) =>[^;]*EffectValue\(RunUpgradeEffect\.' + $effect + '\)')
    if (-not $prop.Success) { $failures.Add("Upgrade effect $effect is never exposed by RunUpgradeState."); continue }
    $name = $prop.Groups[1].Value
    Expect ([regex]::IsMatch($allOther, '\.' + $name + '\b')) "Upgrade effect $effect ($name) has no gameplay reader."
}

# G2: the random power-up roll must leave a reachable band for GlideFeather.
$config = Src 'Assets/Scripts/Runtime/PenguinRun/RunnerTuningConfig.cs'
function Num([string]$field) {
    $m = [regex]::Match($config, $field + '\s*=\s*([0-9.]+)f')
    if (-not $m.Success) { $failures.Add("Tuning field missing: $field"); return 0.0 }
    return [double]$m.Groups[1].Value
}
$top = (Num 'PowerPickScoreStarMax') + (Num 'PowerPickDoubleFishBand') + (Num 'PowerPickHourglassBand') + 0.06
Expect ($top -le 0.975) "GlideFeather roll band is unreachable or under 2.5% (bands sum to $top)."
$tuning = Src 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerTuning.cs'
$mirror = [regex]::Match($tuning, 'powerPickScoreStarMax\s*=\s*([0-9.]+)f')
Expect ($mirror.Success -and [double]$mirror.Groups[1].Value -eq (Num 'PowerPickScoreStarMax')) 'RunnerTuning ScoreStar band must mirror RunnerTuningConfig.'

# G4: resonance must pay out in gameplay, not only on the HUD.
$controller = Src 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs'
$award = $controller.IndexOf('private void AwardSkill(')
Expect ($award -ge 0) 'AwardSkill not found.'
if ($award -ge 0) {
    $body = $controller.Substring($award, [Math]::Min(600, $controller.Length - $award))
    Expect ($body.Contains('themeMastery.ScaleSkill(')) 'AwardSkill must route base score through ThemeMasteryTracker.ScaleSkill.'
}
$tracker = Src 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasteryTracker.cs'
Expect ([regex]::IsMatch($tracker, 'ScaleSkill\(int baseScore\)\s*=>\s*IsResonating \? [^;]*ResonanceSkillMultiplier')) 'ScaleSkill must apply ResonanceSkillMultiplier while resonating.'
$mult = [regex]::Match($tracker, 'ResonanceSkillMultiplier\s*=\s*([0-9.]+)f')
Expect ($mult.Success -and [double]$mult.Groups[1].Value -gt 1.0) 'ResonanceSkillMultiplier must be a real bonus (> 1).'

# G5: bundle descriptions must match what ShopStore grants.
$catalog = Src 'Assets/Scripts/Runtime/Game/Shop/ShopCatalog.cs'
$store = Src 'Assets/Scripts/Runtime/Game/Shop/ShopStore.cs'
Expect (-not $catalog.Contains([string]::new([char[]](0x6781,0x5730,0x9752,0x56F4,0x5DFE)))) 'Polar bundle text names the free default scarf; it grants scarf_aurora.'
$auroraGrant = [regex]::Match($store, 'case ShopItemKind\.BundleAurora:(.*?)break;', 'Singleline')
Expect ($auroraGrant.Success -and $auroraGrant.Groups[1].Value.Contains('BoosterLuckyStart + 3')) 'Aurora bundle advertises every booster x3; lucky start must also be +3.'

if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: GDD M1 effect-reality contracts.'
