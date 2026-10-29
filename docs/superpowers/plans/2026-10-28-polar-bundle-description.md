# Polar Bundle Description Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline in the current chat. Preserve the existing dirty checkout; do not create worktrees, dispatch agents, commit, merge or push. Steps use checkbox syntax for tracking.

**Goal:** Make the Polar bundle description accurately list its existing two voucher rewards.

**Architecture:** Change only the shared catalog description. Keep price, Dispense, PlayerSave and UI consumers unchanged. Reuse the existing 33-scenario purchase regression.

**Tech Stack:** Unity C#, PowerShell 7, bundled Roslyn.

---

## Files

- `Assets/Scripts/Runtime/Game/Shop/ShopCatalog.cs`: change the `bundle_polar` Description only.
- `tools/TestShopBundlePurchase.ps1` and `tools/tests/ShopBundlePurchaseRegression.cs`: existing regression, no edits required.
- `docs/GDD.md`, `docs/CURRENT_DESIGN.md`, `docs/VERIFICATION.md`: update G5 status and RED/GREEN evidence, without claiming M1 or Unity acceptance.

## Execution

- [x] Run `pwsh -NoProfile -File tools/TestShopBundlePurchase.ps1` before the fix. Expected and observed: `FAILED: 1/33 bundle purchase scenarios.` for the Polar description assertion.
- [x] Replace the Polar catalog description with exactly:

```csharp
Description = "600 鱼干 · 双倍鱼干券 ×2 · 得分加成券 ×2 · 解锁极光绿围巾（一次性）。",
```

- [x] Run `pwsh -NoProfile -File tools/TestShopBundlePurchase.ps1`: 33 scenarios passed. Both `TestCampUpgradePreview.ps1` variants passed 74 scenarios each.
- [x] Run every `Verify*.ps1` gate: 24/25 passed including M1; inherited BossEncounter file-size failure unchanged. `CompileFullRoslyn.ps1` passed with 205 sources.
- [x] Update design/verification records; retain original failed-test evidence. SHA256 checks confirmed `ShopStore` and `PlayerSave` unchanged during this batch; the catalog change is restricted to the approved description.
- [x] Run `git diff --check`: passed. Keep all changes uncommitted in the existing checkout. Unity layout/persistence remains unverified.
