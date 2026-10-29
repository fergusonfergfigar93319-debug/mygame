param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run with pwsh (PowerShell 7) for the bundled Roslyn compiler.' }
$sources = @(
    'tools/tests/RunnerInputEngineBoundary.cs',
    'Assets/Scripts/Runtime/PenguinRun/Input/RunnerGestureClassifier.cs',
    'Assets/Scripts/Runtime/PenguinRun/RunnerInput.cs',
    'tools/tests/RunnerInputRegression.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
Add-Type -Path $sources
$failures = [PenguinRun.Tests.RunnerInputRegression]::Run()
if ($failures.Count -gt 0) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host "PASS: $([PenguinRun.Tests.RunnerInputRegression]::CaseCount) RunnerInput behavior regression scenarios."
