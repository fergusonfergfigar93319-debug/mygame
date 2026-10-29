# Lock truthful lobby reward copy without rejecting valid double-fish products.
# ASCII-only source for Windows PowerShell 5.1 compatibility.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-Code([string]$path) {
    $fullPath = Join-Path $ProjectRoot $path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $failures.Add("Missing production source: $path")
        return ''
    }
    $code = Get-Content -LiteralPath $fullPath -Raw -Encoding UTF8
    return [regex]::Replace($code, '(?s)/\*.*?\*/|(?m)//[^\r\n]*', '')
}

function Expect([bool]$condition, [string]$message) {
    if (-not $condition) { $failures.Add($message) }
}

$menu = Read-Code 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'
$summary = Read-Code 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyRewardSummary.cs'
$model = [regex]::Match($menu,
    '(?s)private static LobbyViewModel BuildLobbyViewModel\(\).*?(?=private void RefreshStorybookLobby\()').Value
Expect ($model.Length -gt 0) 'Lobby view-model refresh was not found.'
foreach ($hook in @(
    'MissionStore.MissionStates(MissionTrack.Daily)',
    'missionStates.AddRange(MissionStore.MissionStates(MissionTrack.Achievement))',
    'LobbyRewardSummary.FromMissions(missionStates)',
    'var claimableMissions = rewardSummary.ClaimableCount;',
    'rewardSummary.JournalLabel',
    'LobbyRewardSummary.JourneyRewardLabel'
)) {
    Expect ($model.Contains($hook)) "Missing real reward snapshot/binding: $hook"
}
Expect (-not $model.Contains('HasPlayedDailyToday')) 'Ordinary journey reward copy must not depend on daily participation.'
Expect ($menu.Contains('topTickerText.text = LobbyRewardSummary.DailyChallengeHint;')) 'Daily ticker must use the neutral challenge hint.'
Expect (-not $menu.Contains('dailyFishRewardChipText')) 'Unused fixed-reward chip field or assignment remains.'

$unsupportedPromises = @(
    '\u4eca\u65e5\u5956\u52b1\uff1a\u9c7c\u5e72\s*\u00d7\s*80',
    '\u4eca\u65e5\u9996\u80dc',
    '\u4eca\u65e5\u6311\u6218[^"\r\n]*\u53cc\u500d\u9c7c\u5e72',
    '\+50\s*\u9c7c\u5e72'
)
foreach ($promise in $unsupportedPromises) {
    Expect (-not [regex]::IsMatch($menu, $promise)) "Unsupported lobby reward promise remains: $promise"
}

foreach ($member in @('public readonly struct LobbyRewardSummary', 'ClaimableCount', 'FishTotal',
    'state.Claimable', 'state.Definition.RewardFishSnacks', 'long fish', 'Math.Max(0,')) {
    Expect ($summary.Contains($member)) "Missing read-only reward aggregation contract: $member"
}
Expect (-not [regex]::IsMatch($summary,
    '\b(PlayerPrefs|PlayerSave|MissionStore|ShopStore|RunOutcomeRouter|GrowthTransaction)\s*\.')) 'Summary must not call save or transaction stores.'
Expect ($menu.Contains('PlayerSave.PreviewDailyCheckInReward()')) 'Real sign-in preview was removed.'
Expect ($menu.Contains('ShopStore.InventoryCount(ShopItemKind.BoosterDoubleFish)')) 'Real double-fish ticket inventory preview was removed.'
Expect ($menu.Contains('UpdateBoosterPreview();')) 'Real next-run booster preview is no longer refreshed.'

if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: truthful lobby reward snapshot/bindings, neutral hints, and real reward previews.'
