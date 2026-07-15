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

$models = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicModels.cs'
$random = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/DeterministicRunRandom.cs'
foreach ($name in 'ThemeMechanicId','ThemeMechanicPhase','ThemeMechanicMode','ThemeMechanicIntensity','ThemeMechanicDefinition','ThemeMechanicModifiers','ThemeMechanicSnapshot') {
    Require $models "\b$name\b" $name
}
Require $random '\bIRunRandom\b' 'injected run random interface'
Require $random '\bXorShiftRunRandom\b' 'deterministic xorshift implementation'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Theme mechanics phase-four static contracts passed.'
