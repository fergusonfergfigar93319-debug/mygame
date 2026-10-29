# Lobby Reward Truth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. The reviewed spec selects inline execution in the existing workspace, without agents or another worktree.

**Goal:** Make lobby reward copy describe existing, claimable rewards without changing the economy.

**Architecture:** A pure `LobbyRewardSummary` consumes the existing mission-state snapshot and returns its count, fish total, and label. `MainMenuBootstrap` reads Daily and Achievement states once per model refresh, binds the summary through the existing view model, and uses neutral journey/daily hints. Mission definitions, store mutations, save data, layout, and settlement remain unchanged.

**Tech Stack:** Unity 6000.4 C#, PowerShell 7 `Add-Type` production-logic harness, ASCII-only source-contract gate, bundled Roslyn.

**Spec:** `docs/superpowers/specs/2026-10-30-lobby-reward-truth-design.md`, approved by the user's subsequent “开始”.

## Files and responsibilities

- Create `Assets/Scripts/Runtime/Game/UI/Lobby/LobbyRewardSummary.cs` and its Unity `.meta`: read-only aggregation and neutral strings.
- Modify `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs`: snapshot/binding, ticker, removal of the unused reward-chip field.
- Create `tools/tests/LobbyRewardSummaryRegression.cs`: actual production catalog/state/summary behavior, with isolated synthetic input only.
- Create `tools/TestLobbyRewardSummary.ps1`: compile the pure production sources and run the regression cases in a fresh process.
- Create `tools/VerifyLobbyRewardTruth.ps1`: source wiring, removal of unsupported promises, preservation of real booster/sign-in previews.
- Append verification notes to `docs/VERIFICATION.md` and current-design notes to `docs/CURRENT_DESIGN.md`; update spec status and these task checkboxes.

## Task 1: Lock the failures

- [x] Add the new harness before production changes. It explicitly fails if the production summary source is missing, otherwise compiles these files:

```powershell
$sources = @(
    'Assets/Scripts/Runtime/Game/Mission/MissionModels.cs',
    'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyRewardSummary.cs',
    'tools/tests/LobbyRewardSummaryRegression.cs'
) | ForEach-Object { Join-Path $ProjectRoot $_ }
Add-Type -Path $sources
$failures = [PenguinRun.Tests.LobbyRewardSummaryRegression]::Run()
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
```

- [x] Implement regression cases for null/empty input, incomplete/claimed missions, each real mission definition, mixed Daily/Achievement snapshots, repeated reads, changing synthetic reward values, negative/zero rewards, totals above `int.MaxValue`, one-pass enumeration, and immutable summary values after inputs change. Use assertions of the following form; real catalog definitions are never mutated:

```csharp
var definition = MissionCatalog.All[0];
var state = new MissionUiState { Definition = definition, Completed = true, Claimable = true, Progress = definition.Target };
var summary = LobbyRewardSummary.FromMissions(new[] { state });
Check(summary.ClaimableCount == 1, "claimable count");
Check(summary.FishTotal == definition.RewardFishSnacks, "catalog reward");
Check(summary.JournalLabel == $"可领取 1 份任务奖励 · 共 {definition.RewardFishSnacks} 鱼干", "real amount label");
```

- [x] Add the ASCII-only static gate. Strip comments before checking `MainMenuBootstrap` and the summary. Require both mission tracks, summary-derived count/label, the neutral journey/ticker constants, no store writes in the summary, no `HasPlayedDailyToday` in `BuildLobbyViewModel`, and preservation of `PreviewDailyCheckInReward` and `ShopStore.InventoryCount`. Reject only the unsupported menu promises and unused chip; do not reject valid shop/game double-fish text.
- [x] Run `pwsh -NoProfile -File tools/TestLobbyRewardSummary.ps1` and `pwsh -NoProfile -File tools/VerifyLobbyRewardTruth.ps1`. Expected: exit 1 for missing summary and existing unsupported lobby promises, not a compiler/parser error. Record the failures.

## Task 2: Minimal production implementation

- [x] Create this production aggregation unit after the failures have been observed:

```csharp
using System;
using System.Collections.Generic;
using PenguinRun.Game.Mission;

namespace PenguinRun.Game.UI.Lobby
{
    public readonly struct LobbyRewardSummary
    {
        public const string JourneyRewardLabel = "沿途收集鱼干 · 旅程结束后结算";
        public const string DailyChallengeHint = "今日挑战尚未开始 · 挑战当日记录";

        private LobbyRewardSummary(int claimableCount, long fishTotal)
        {
            ClaimableCount = claimableCount;
            FishTotal = fishTotal;
        }

        public int ClaimableCount { get; }
        public long FishTotal { get; }
        public string JournalLabel => ClaimableCount > 0
            ? $"可领取 {ClaimableCount} 份任务奖励 · 共 {FishTotal} 鱼干"
            : "完成任务后可领取鱼干";

        public static LobbyRewardSummary FromMissions(IEnumerable<MissionUiState> missions)
        {
            var count = 0;
            long fish = 0;
            if (missions != null)
            {
                foreach (var state in missions)
                {
                    if (state == null || !state.Claimable || state.Definition == null) continue;
                    count++;
                    fish += Math.Max(0, state.Definition.RewardFishSnacks);
                }
            }
            return new LobbyRewardSummary(count, fish);
        }
    }
}
```

- [x] Add a unique Unity script GUID using the same minimal `.meta` shape as adjacent C# assets; do not change prefabs or scenes.
- [x] In `BuildLobbyViewModel`, replace the independent count with one combined snapshot:

```csharp
var missionStates = MissionStore.MissionStates(MissionTrack.Daily);
missionStates.AddRange(MissionStore.MissionStates(MissionTrack.Achievement));
var rewardSummary = LobbyRewardSummary.FromMissions(missionStates);
var claimableMissions = rewardSummary.ClaimableCount;
```

- [x] Remove the old `journalReward`/`routeReward` branches and pass these values to the existing view-model constructor:

```csharp
rewardSummary.JournalLabel, chapterTitle, chapterBody,
$"{difficulty.DisplayName} · 得分 ×{difficulty.ScoreMultiplier:0.##}",
LobbyRewardSummary.JourneyRewardLabel
```

- [x] Delete the unused `dailyFishRewardChipText` declaration, its comment, and its `+50` assignment. Replace only the unplayed-Daily ticker assignment with:

```csharp
topTickerText.text = LobbyRewardSummary.DailyChallengeHint;
```

- [x] Run the new behavior harness and static gate. Expected: all cases pass; `MainMenuBootstrap` stays below 1390 lines. Do not modify task payouts, prices, sign-in rules, real double-fish previews, or ratchet ceilings.

## Task 3: Regression, records, and handoff

- [x] Run `TestShopBundlePurchase.ps1` (33), `TestCampUpgradePreview.ps1` default and `-AlternateDefaults` (74 each), and the five existing gameplay/input/theme harnesses. All run with `pwsh -NoProfile -File tools/<name>.ps1`.
- [x] Run `pwsh -NoProfile -File tools/CompileFullRoslyn.ps1`. Expected: 206 production/editor source files compile; this is not a Unity or Android runtime result.
- [x] Run every `tools/Verify*.ps1` in fresh child processes and collect all exit codes. Expected: new lobby gate passes; only the already-known `BossEncounter.cs` 1947/1900 file-size failure remains. Do not change that file or raise the ceiling.
- [x] Compare protected-file SHA256 hashes with the baseline captured before changes; verify economy/save/layout sources and existing ratchet are unchanged.
- [x] Update verification/design notes with actual red-to-green counts, precise runtime limitations, and commands. Do not claim Unity page rendering or Android testing unless actually executed.
- [x] Run `git diff --check`, inspect every new/modified file, and check spec coverage. Record remaining UI line-wrap/manual-refresh acceptance explicitly. Keep changes on the existing branch; do not merge, push, or stage the user's unrelated changes.
