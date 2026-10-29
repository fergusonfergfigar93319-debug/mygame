param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [switch]$AlternateDefaults
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run in a fresh PowerShell 7 process.' }
$sources = @(
    'tools/tests/CampUpgradePreviewBoundary.cs',
    'Assets/Scripts/Runtime/Game/DifficultyPreset.cs',
    'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs',
    'Assets/Scripts/Runtime/Game/Save/CampUpgrades.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerTuningConfig.cs',
    'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerTuning.cs',
    'tools/tests/CampUpgradePreviewRegression.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
if ($AlternateDefaults) {
    # Mutate only the in-memory config source to prove previews follow tuning, not copied constants.
    $usingLines = [System.Collections.Generic.List[string]]::new()
    $code = foreach ($source in $sources) {
        $contents = Get-Content -Raw -Encoding utf8 -LiteralPath $source
        foreach ($using in [regex]::Matches($contents, '(?m)^\s*using\s+[^;]+;\s*\r?\n')) {
            if (-not $usingLines.Contains($using.Value.Trim())) { $usingLines.Add($using.Value.Trim()) }
        }
        $contents = [regex]::Replace($contents, '(?m)^\s*using\s+[^;]+;\s*\r?\n', '')
        if ((Split-Path $source -Leaf) -eq 'RunnerTuningConfig.cs') {
            $fieldValues = [ordered]@{
                DashBaseSeconds = '8.25'; DashPerLevelSeconds = '1.15'
                MagnetBaseSeconds = '9.5'; MagnetPerLevelSeconds = '0.75'
                ShieldBaseSeconds = '3.25'; ShieldPerTuanLevelSeconds = '0.45'
                GlideBaseSeconds = '6.25'; GlidePerTuanLevelSeconds = '0.55'
                ScoreStarBaseSeconds = '9.75'; ScoreStarPerPolarLevelSeconds = '0.35'
                DoubleFishBaseSeconds = '7.25'; DoubleFishPerPolarLevelSeconds = '0.45'
                HourglassBaseSeconds = '5.75'; HourglassPerPolarLevelSeconds = '0.25'
                PowerUpChancePerPolarLevel = '0.035'
            }
            foreach ($field in $fieldValues.Keys) {
                $pattern = '(public float ' + [regex]::Escape($field) + ' = )[0-9.]+f;'
                if ($contents -notmatch $pattern) { throw "Cannot override config field in memory: $field" }
                $contents = [regex]::Replace($contents, $pattern, ('${1}' + $fieldValues[$field] + 'f;'))
            }
        }
        $contents
    }
    Add-Type -TypeDefinition (($usingLines -join "`n") + "`n" + ($code -join "`n"))
} else {
    Add-Type -Path $sources
}
$failures = [PenguinRun.Tests.CampUpgradePreviewRegression]::Run()
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host "PASS: $([PenguinRun.Tests.CampUpgradePreviewRegression]::CaseCount) camp upgrade preview scenarios (alternate defaults: $AlternateDefaults)."
