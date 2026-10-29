param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run in a fresh PowerShell 7 process.' }
$sources = @(
    'tools/tests/ShopPurchaseBoundary.cs',
    'Assets/Scripts/Runtime/Game/DifficultyPreset.cs',
    'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs',
    'Assets/Scripts/Runtime/Game/Shop/ShopCatalog.cs',
    'Assets/Scripts/Runtime/Game/Shop/ShopStore.cs',
    'tools/tests/ShopBundlePurchaseRegression.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
Add-Type -Path $sources
$failures = [PenguinRun.Tests.ShopBundlePurchaseRegression]::Run()
if ($failures.Count) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    Write-Host "FAILED: $($failures.Count)/$([PenguinRun.Tests.ShopBundlePurchaseRegression]::CaseCount) bundle purchase scenarios."
    exit 1
}
Write-Host "PASS: $([PenguinRun.Tests.ShopBundlePurchaseRegression]::CaseCount) bundle purchase scenarios (in-memory storage only)."
