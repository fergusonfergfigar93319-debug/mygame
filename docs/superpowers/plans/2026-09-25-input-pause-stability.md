# Input and Pause Stability Implementation Plan

> Use superpowers:executing-plans and test-driven-development. Preserve existing uncommitted work.

**Goal:** Canceled/interrupted input cannot trigger gameplay; paused simulation cannot advance cooldown or consume queued movement.

**Architecture:** Keep Unity input sampling in RunnerInput and preserve gesture thresholds. Reset transient input at lifecycle boundaries without resetting physical motor state. Place simulation timers after freeze guards.

**Tech Stack:** Unity 6000.4.5f1, C#, PowerShell gates, standalone input regression, Unity editor validation.

### Task 1: Pointer lifecycle
- [x] Write executable cases against real RunnerInput: canceled tap/swipe, finger ownership, multi-touch latch, reset, ordinary tap/double tap/swipe.
- [x] Run old implementation and record expected failures (13 failing assertions).
- [x] Implement RunnerInput.Reset and repair cancellation/ownership/latching without changing thresholds or key mapping.
- [x] Run initial behavior regression to green and review. Extended normal-control cases are included in final verification.

### Task 2: Controller integration
- [x] Add tools/VerifyInputPauseSafety.ps1; run on old code and confirm failure (four source contracts).
- [x] Add editor behavior validation for paused cooldown, queued actions and live-state preservation.
- [x] Add PlayerMotor.ClearBufferedActions clearing only jumpBufferTimer and slideBufferTimer. Controller ResetInput clears pointer, motor buffers, dashBufferTimer and lastRunnerAction.
- [x] Reset input for run reset, pause/resume, state transitions and application interruption.
- [x] Move dash ticking below pause and guided-reaction returns, preserving active gameplay timing.
- [x] Stay within existing file-size ceilings; lower the controller ceiling from 1190 to 1177 using the existing gate's counting convention.

### Task 3: Verification
- [x] Run behavior suite (18 scenarios), all tools/Verify*.ps1 gates (20 passed) and tools/CompileFullRoslyn.ps1 (192 sources compiled).
- [ ] Run Unity input/pause and player-experience validation; record actual results.
- [x] Update docs/VERIFICATION.md and inspect final diff without committing existing user work; git diff --check passes.

Acceptance: canceled contacts return None; multi-touch toggles pause once until released; no tap chain or queued action survives interruption; cooldown and active movement remain frozen during pause; ordinary controls unchanged. Android device feel requires separate manual validation.

Environment limitations: the implementation subagent service failed before starting, so execution and two-pass review continued locally. Unity batchmode exited 198 before project compilation because no valid Editor license was available. Do not mark the Unity behavioral validation complete until rerun after license activation. No user save, global license setting, or unrelated working change was modified.
