# Compile only real mission models and the pure lobby summary; no player save access.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run in a fresh PowerShell 7 process.' }
$summaryPath = Join-Path $ProjectRoot 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyRewardSummary.cs'
if (-not (Test-Path -LiteralPath $summaryPath)) {
    Write-Host 'FAIL: production LobbyRewardSummary is missing.'
    exit 1
}
$sources = @(
    'Assets/Scripts/Runtime/Game/Mission/MissionModels.cs',
    'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyRewardSummary.cs',
    'tools/tests/LobbyRewardSummaryRegression.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
Add-Type -Path $sources
$failures = [PenguinRun.Tests.LobbyRewardSummaryRegression]::Run()
if ($failures.Count) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "FAILED: $($failures.Count)/$([PenguinRun.Tests.LobbyRewardSummaryRegression]::CaseCount) lobby reward scenarios."
    exit 1
}
Write-Host "PASS: $([PenguinRun.Tests.LobbyRewardSummaryRegression]::CaseCount) lobby reward scenarios (real catalog, read-only synthetic states)."
