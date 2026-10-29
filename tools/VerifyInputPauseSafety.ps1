param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
$controller = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.cs') -Raw -Encoding UTF8
$controller += Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/RunnerGameController.Input.cs') -Raw -Encoding UTF8
$motor = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs') -Raw -Encoding UTF8
$hostCode = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs') -Raw -Encoding UTF8
function Expect([bool]$condition, [string]$message) { if (-not $condition) { $failures.Add($message) } }
function Body([string]$code, [string]$signature) {
    $start = $code.IndexOf($signature)
    if ($start -lt 0) { $failures.Add("Missing method: $signature"); return '' }
    $start = $code.IndexOf('{', $start)
    $depth = 0
    for ($i = $start; $i -lt $code.Length; $i++) {
        if ($code[$i] -eq '{') { $depth++ }
        if ($code[$i] -eq '}') { $depth--; if ($depth -eq 0) { return $code.Substring($start, $i - $start + 1) } }
    }
    $failures.Add("Unclosed method: $signature")
    return ''
}
$start = $controller.IndexOf('public float TickRunningSimulation(')
$end = $controller.IndexOf('public void TickRunningWorld(', $start)
$tick = $controller.Substring($start, $end - $start)
$dashTick = $tick.IndexOf('var dashCdBefore')
$pauseGuard = $tick.IndexOf('if (world.Paused)')
$guideGuard = $tick.LastIndexOf('if (waitingForGuidedReaction)')
Expect ($pauseGuard -ge 0 -and $guideGuard -gt $pauseGuard -and $dashTick -gt $guideGuard) 'Frozen simulation guards must precede dash cooldown/buffer ticking.'
if ($pauseGuard -ge 0 -and $dashTick -gt $pauseGuard) {
    Expect ($tick.Substring($pauseGuard, $dashTick - $pauseGuard).Contains('return 0f;')) 'Pause must return without advancing simulation.'
}
if ($guideGuard -ge 0 -and $dashTick -gt $guideGuard) {
    Expect ($tick.Substring($guideGuard, $dashTick - $guideGuard).Contains('return 0f;')) 'Guided freeze must return without advancing simulation.'
}
$reset = Body $controller 'public void ResetInput()'
foreach ($statement in @('input.Reset();','motor.ClearBufferedActions();','dashBufferTimer = 0f;','lastRunnerAction = RunnerAction.None;')) {
    Expect ($reset.Contains($statement)) "Controller input reset must execute: $statement"
}
$motorReset = Body $motor 'public void ClearBufferedActions()'
Expect ($motorReset.Contains('jumpBufferTimer = 0f;') -and $motorReset.Contains('slideBufferTimer = 0f;')) 'Motor must clear both queued actions.'
Expect ((Body $controller 'public void ResetRun(bool start)').Contains('ResetInput();')) 'Every attempt reset must clear input.'
$changed = Body $controller 'private void HandleStateChanged('
foreach ($state in @('Paused','ThemeTransition','DeathSequence','GameOver','Finished','Idle','Countdown')) {
    Expect ($changed.Contains('GameStateMachine.State.' + $state)) "Input reset must cover state: $state"
}
Expect ($changed.Contains('ResetInput();')) 'State transitions must actually invoke input reset.'
foreach ($signature in @('private void OnApplicationFocus(bool hasFocus)','private void OnApplicationPause(bool pauseStatus)','private void OnDisable()')) {
    $callback = Body $hostCode $signature
    Expect ($callback.Contains('game?.ResetInput();') -and $callback.Contains('game?.SetPaused(true, false);')) "Lifecycle callback must clear input and pause: $signature"
}
if ($failures.Count) { $failures | ForEach-Object { Write-Host "FAIL: $_" }; exit 1 }
Write-Host 'PASS: input/pause lifecycle source contracts.'
