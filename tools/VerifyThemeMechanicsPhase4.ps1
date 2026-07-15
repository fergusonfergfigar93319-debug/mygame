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

$catalog = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicCatalog.cs'
foreach ($id in 'IceInertia','CedarCollapse','AuroraPolarity','MistBeacon','OceanTide','SkyWind') {
    Require $catalog "ThemeMechanicId\.$id" "catalog definition $id"
}
Require $catalog '\bForTheme\s*\(' 'theme lookup'
Require $catalog '\bTryForTheme\s*\(' 'safe theme lookup'
Require $catalog '\bPolicyFor\s*\(' 'mode policy lookup'

$director = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/MapMechanicDirector.cs'
foreach ($name in 'MapMechanicDirector','RecordBeat','TrySchedule','RewardDebt','PressureDebt') {
    Require $director "\b$name\b" $name
}
Require $models '\bThemeMechanicContext\b' 'director context model'
Require $models '\bThemeMechanicRequest\b' 'director request model'

$runtime = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicRuntime.cs'
foreach ($name in 'ThemeMechanicRuntime','Begin','Tick','Cancel','RecordKeypoint','ModifiersFor','Snapshot') {
    Require $runtime "\b$name\b" $name
}

$patterns = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicPatternCatalog.cs'
$spawner = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMechanicSpawner.cs'
Require $patterns '\bThemeMechanicPatternCatalog\b' 'theme mechanic pattern catalog'
Require $patterns '\bIceNormal\b' 'Ice Lake normal pattern'
Require $patterns '\bIceSignature\b' 'Ice Lake signature pattern'
Require $spawner '\bThemeMechanicSpawner\b' 'bounded theme mechanic spawner'
Require $spawner '\bCapacity\b' 'apparatus pool capacity'

$mastery = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/ThemeMasteryTracker.cs'
$contracts = Read-Required 'Assets/Scripts/Runtime/PenguinRun/ThemeMechanics/JourneyContractCatalog.cs'
foreach ($name in 'ThemeMasteryTracker','RecordKeypoint','RecordDamage','ResolveEvent','Resonance01','RunSnapshot') {
    Require $mastery "\b$name\b" $name
}
Require $contracts '\bJourneyContractCatalog\b' 'journey contract catalog'
foreach ($kind in 'PerfectStreak','FlawlessEvents','ResonanceFish','SignatureComplete','SkillBeforeDistance') {
    Require $contracts "JourneyContractKind\.$kind" "journey contract $kind"
}

$session = Read-Required 'Assets/Scripts/Runtime/Game/RunSession.cs'
$sessionConfig = Read-Required 'Assets/Scripts/Runtime/Game/RunnerSessionConfig.cs'
foreach ($name in 'RunnerRunMode','PrepareStoryRun','PrepareEndlessRun','TryPrepareDailyRun','NextRunSeed') {
    Require $session "\b$name\b" $name
}
foreach ($name in 'Mode','ThemeMechanicMode','RunSeed') {
    Require $sessionConfig "\b$name\b" "session config $name"
}

$controller = Read-Required 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs'
$runResult = Read-Required 'Assets/Scripts/Runtime/PenguinRun/Systems/RunnerRunResult.cs'
foreach ($name in 'mechanicDirector','mechanicRuntime','mechanicSpawner','themeMastery') {
    Require $controller "\b$name\b" "controller integration $name"
}
Require $controller 'Cancel\(ThemeMechanicCancelReason\.Boss\)' 'Boss mechanic cancellation'
Require $controller 'Cancel\(ThemeMechanicCancelReason\.RouteEvent\)' 'route mechanic cancellation'
Require $controller 'Cancel\(ThemeMechanicCancelReason\.ThemeTransition\)' 'theme transition mechanic cancellation'
Require $runResult '\bThemeMasteryRunSnapshot\b' 'mastery run result snapshot'

$masteryStore = Read-Required 'Assets/Scripts/Runtime/Game/Growth/ThemeMasteryStore.cs'
foreach ($name in 'ThemeMasteryStore','ThemeMasteryRecord','EnsureMigrated','ApplyRun','TutorialSeen') {
    Require $masteryStore "\b$name\b" "mastery persistence $name"
}
if ($null -ne $masteryStore -and $masteryStore -match 'PlayerSave\.Flush') {
    $failures.Add('ThemeMasteryStore must not flush outside the growth transaction.')
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output 'Theme mechanics phase-four static contracts passed.'
