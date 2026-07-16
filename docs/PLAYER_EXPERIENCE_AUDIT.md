# Player Experience Audit

Audit date: 2026-07-16  
Primary target: Android landscape, including 16:9 and 20:9 devices  
Journey: startup/home -> mode and difficulty -> first run -> mechanics/Boss -> pause/interruption -> death/result -> growth return

## Issue register

| ID | Severity | Journey stage | Reproduction | Evidence | Fix | Verification | Status |
|---|---|---|---|---|---|---|---|
| PX-001 | P0 | Run | Touch the undocumented screen-corner band during a run. | `PenguinRunnerGame.IsReturnButtonTouch` ends the run directly. | Replace with explicit Pause and Return to Camp controls. | Pending player-experience validator and journey smoke. | Open |
| PX-002 | P0 | Death / Retry | Die, choose Retry, then inspect saved growth from the completed attempt. | Retry calls `ResetRun(true)` without `RunOutcomeRouter.Apply`. | Settle every attempt exactly once before retry reset. | Pending settlement-gate tests. | Open |
| PX-003 | P1 | Death / Retry | Trigger a theme transition/world event, die, then Retry. | Retry does not reset theme transition, world events, or pooled particles. | Central retry presentation reset. | Pending reset contracts and soak. | Open |
| PX-004 | P1 | Lobby | Open first-login, check-in, or difficulty modal and press Android Back. | `MainMenuBootstrap.Update` checks routed panels only, then quits. | Add top-most modal ownership before panel/quit handling. | Pending modal-stack tests and device acceptance. | Open |
| PX-005 | P1 | Run / Interruption | Background the app or look for a pause action. | Lifecycle callbacks only alter performance; pause gesture is undiscoverable. | Visible pause flow plus idempotent interruption pause. | Pending HUD/layout/journey checks. | Open |
| PX-006 | P1 | Difficulty / Launch | Dispatch StartFinal twice before the scene transition completes. | Run preparation happens before transition idempotency and may consume boosters twice. | Gate launch before `RunSession.Prepare*`. | Pending launch-gate tests and journey double-invoke. | Open |
| PX-007 | P2 | Run input | Compare the same physical gesture across 720p and high-density landscape screens. | Swipe/tap/double-tap thresholds are fixed pixels. | Use bounded viewport-relative thresholds. | Pending normalized classifier tests. | Open |
| PX-008 | P2 | Regression coverage | Run the existing runtime smoke. | It remains in MainMenu for eight seconds and never proves a player journey. | Add real-scene home-to-result smoke with screenshots. | Pending headed journey smoke. | Open |

## Authority boundaries

- `ObstacleColliderSpec` and `RunnerHitResolver` remain collision authority.
- `RunOutcomeRouter.Apply` remains the sole growth/save transaction authority.
- Visual geometry and effects remain non-authoritative.
- Boss, route-event, and theme-mechanic exclusion rules remain unchanged.

## Final evidence

To be completed after implementation: static gates, Unity validators, journey screenshots, soak result, APK path/hash/signature, and any deferred P2/P3 risks.

