param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    $path = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
        return $null
    }
    return Get-Content -Raw -Encoding utf8 -LiteralPath $path
}

function Require-Pattern([string]$content, [string]$pattern, [string]$description) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add("Missing contract: $description")
    }
}

$schema = Require-File 'Assets/Scripts/Runtime/Game/Save/SaveSchema.cs'
Require-Pattern $schema 'CurrentVersion\s*=\s*3\b' 'save schema version three'
Require-Pattern $schema '\bEnsureMigrated\s*\(' 'non-destructive save migration entrypoint'
Require-Pattern $schema '\bMigrateLegacyDiscoveries\s*\(' 'legacy discovery migration'
if ($schema -match 'DeleteAll|DeleteKey') { $failures.Add('SaveSchema must not delete existing player data') }

$playerSave = Require-File 'Assets/Scripts/Runtime/Game/Save/PlayerSave.cs'
foreach ($helper in 'ReadGrowthInt', 'WriteGrowthInt', 'ReadGrowthString', 'WriteGrowthString') {
    Require-Pattern $playerSave "\b$helper\s*\(" "PlayerSave.$helper"
}

$campModels = Require-File 'Assets/Scripts/Runtime/Game/Growth/CampBuildingModels.cs'
foreach ($node in 'Bonfire', 'Workshop', 'CompanionHut', 'LibraryTent', 'Lookout') {
    Require-Pattern $campModels "\b$node\b" "CampBuildingId.$node"
}
foreach ($state in 'Locked', 'Affordable', 'Rebuilt') {
    Require-Pattern $campModels "\b$state\b" "camp node visible state $state"
}

foreach ($field in 'FishCosts', 'Prerequisite', 'Story') {
    Require-Pattern $campModels "\b$field\b" "camp definition includes $field"
}
$campCatalog = Require-File 'Assets/Scripts/Runtime/Game/Growth/CampBuildingCatalog.cs'
Require-Pattern $campCatalog 'MaxLevel\s*=\s*3\b' 'camp building max level three'

$campStore = Require-File 'Assets/Scripts/Runtime/Game/Growth/CampRebuildStore.cs'
foreach ($method in 'Snapshot', 'CanPurchase', 'TryPurchase', 'CountAffordableNodes') {
    Require-Pattern $campStore "\b$method\s*\(" "CampRebuildStore.$method"
}
Require-Pattern $campStore 'AddCampInvested' 'rebuild purchases update lifetime investment'

$bondModels = Require-File 'Assets/Scripts/Runtime/Game/Growth/CompanionBondModels.cs'
foreach ($companion in 'TuanTuan', 'Dror') {
    Require-Pattern $bondModels "\b$companion\b" "CompanionId.$companion"
}
Require-Pattern $bondModels '\bCompanionBondSnapshot\b' 'immutable companion bond snapshot'

$bondStore = Require-File 'Assets/Scripts/Runtime/Game/Growth/CompanionBondStore.cs'
Require-Pattern $bondStore 'MaxLevel\s*=\s*5\b' 'bond level cap five'
foreach ($method in 'AwardAfterRun', 'GetPassiveHelperBonus', 'GetConditionalHelperChance') {
    Require-Pattern $bondStore "\b$method\s*\(" "CompanionBondStore.$method"
}
Require-Pattern $bondStore 'RiskyRoutesCompleted|riskyRoutesCompleted' 'bond XP consumes route risk'
Require-Pattern $bondStore 'SkillScore|skillScore' 'bond XP consumes skill score'

$discoveryCatalog = Require-File 'Assets/Scripts/Runtime/Game/Growth/CodexDiscoveryCatalog.cs'
foreach ($group in 'Route', 'Relic') {
    Require-Pattern $discoveryCatalog "\b$group\b" "codex discovery group $group"
}
foreach ($relic in 'relic_ice_fracture', 'relic_cedar_root', 'relic_aurora_polarity', 'relic_mist_decoy', 'relic_coral_rebound', 'relic_storm_draft') {
    Require-Pattern $discoveryCatalog $relic "codex catalog includes $relic"
}
Require-Pattern $discoveryCatalog '\bUnknownClue\b' 'unknown discoveries expose clues'

$discoveryStore = Require-File 'Assets/Scripts/Runtime/Game/Growth/CodexDiscoveryStore.cs'
foreach ($method in 'Discover', 'IsDiscovered', 'IsNew', 'MarkAllSeen') {
    Require-Pattern $discoveryStore "\b$method\s*\(" "CodexDiscoveryStore.$method"
}

$growthTransaction = Require-File 'Assets/Scripts/Runtime/Game/Growth/GrowthTransaction.cs'
Require-Pattern $growthTransaction '\bGrowthTransactionInput\b' 'immutable post-run growth input'
Require-Pattern $growthTransaction '\bGrowthTransactionResult\b' 'aggregate post-run growth result'
Require-Pattern $growthTransaction '\bApply\s*\(' 'GrowthTransaction.Apply'
foreach ($integration in 'MissionStore\.RecordAfterRun', 'CodexUnlocks\.ApplyAfterRun', 'CompanionBondStore\.AwardAfterRun', 'CodexDiscoveryStore\.Discover') {
    Require-Pattern $growthTransaction $integration "growth transaction integrates $integration"
}

$outcomeRouter = Require-File 'Assets/Scripts/Runtime/Game/RunOutcomeRouter.cs'
Require-Pattern $outcomeRouter 'GrowthTransaction\.Apply\s*\(' 'run outcome invokes one growth transaction'
Require-Pattern $outcomeRouter '\bGrowth\b' 'run result carries growth result'
$flushCount = if ($null -eq $outcomeRouter) { 0 } else { ([regex]::Matches($outcomeRouter, 'PlayerSave\.Flush\s*\(')).Count }
if ($flushCount -ne 1) { $failures.Add("RunOutcomeRouter must contain exactly one PlayerSave.Flush call; found $flushCount") }

$missionStore = Require-File 'Assets/Scripts/Runtime/Game/Mission/MissionStore.cs'
if ($missionStore -match 'RecordAfterRun[\s\S]*?PlayerPrefs\.Save\s*\([\s\S]*?LifetimeRunsTotal') {
    $failures.Add('MissionStore.RecordAfterRun must not own the post-run flush boundary')
}

$recommendation = Require-File 'Assets/Scripts/Runtime/Game/Growth/GrowthRecommendation.cs'
foreach ($kind in 'ClaimGoal', 'ReviewDiscovery', 'RebuildCamp', 'UpgradeCamp', 'StartJourney') {
    Require-Pattern $recommendation "\b$kind\b" "growth recommendation kind $kind"
}
Require-Pattern $recommendation '\bBuild\s*\(' 'single lobby recommendation builder'

$campPanel = Require-File 'Assets/Scripts/Runtime/Game/UI/CampPanel.cs'
Require-Pattern $campPanel '\bBuildReconstructionSection\s*\(' 'camp panel reconstruction section'
Require-Pattern $campPanel 'CampRebuildStore\.TryPurchase' 'camp panel routes rebuild purchases through store'

$codexPanel = Require-File 'Assets/Scripts/Runtime/Game/UI/CodexPanel.cs'
Require-Pattern $codexPanel '\bBuildDiscoverySection\s*\(' 'codex grouped discovery section'
Require-Pattern $codexPanel 'CodexDiscoveryStore\.MarkAllSeen' 'codex marks presented discoveries seen'

$background = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyBackground.cs'
foreach ($layer in 'BonfireLayer', 'WorkshopLayer', 'CompanionHutLayer', 'LibraryTentLayer', 'LookoutLayer') {
    Require-Pattern $background $layer "lobby camp scenery $layer"
}
Require-Pattern $background 'CampRebuildSnapshot' 'lobby background consumes reconstruction snapshot'

$companionCorner = Require-File 'Assets/Scripts/Runtime/Game/UI/Lobby/LobbyCompanionCorner.cs'
Require-Pattern $companionCorner 'BondLevel|BondProgress' 'companion corner surfaces bond progress'

$mainMenu = Require-File 'Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs'
Require-Pattern $mainMenu 'SaveSchema\.EnsureMigrated\s*\(' 'save migration runs before lobby reads'
Require-Pattern $mainMenu 'GrowthRecommendation\.Build\s*\(' 'lobby uses one growth recommendation'
Require-Pattern $mainMenu 'GrowthRecommendationKind' 'lobby primary action uses typed routing'

$journeyModel = Require-File 'Assets/Scripts/Runtime/Game/UI/JourneyResultViewModel.cs'
Require-Pattern $journeyModel 'result\.Growth' 'journey result uses growth transaction feedback'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Long-term growth phase-three static contracts passed.'
