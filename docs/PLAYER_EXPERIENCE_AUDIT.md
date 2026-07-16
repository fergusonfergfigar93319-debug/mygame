# Player Experience Audit

Audit date: 2026-07-16

Primary target: Android landscape, including 16:9 and 20:9 devices

Journey: startup/home -> mode and difficulty -> first run -> mechanics/Boss -> pause/interruption -> death/result -> growth return

## Issue register

| ID | Severity | Journey stage | Reproduction | Evidence | Fix | Verification | Status |
|---|---|---|---|---|---|---|---|
| PX-001 | P0 | Run | Touch the undocumented screen-corner band during a run. | `PenguinRunnerGame.IsReturnButtonTouch` ends the run directly. | Replace with explicit Pause and Return to Camp controls. | Player-experience validator and headed journey smoke pass. | Fixed |
| PX-002 | P0 | Death / Retry | Die, choose Retry, then inspect saved growth from the completed attempt. | Retry calls `ResetRun(true)` without `RunOutcomeRouter.Apply`. | Settle every attempt exactly once before retry reset. | Settlement-gate tests pass; only one runtime `RunOutcomeRouter.Apply` call site remains. | Fixed |
| PX-003 | P1 | Death / Retry | Trigger a theme transition/world event, die, then Retry. | Retry does not reset theme transition, world events, or pooled particles. | Central retry presentation reset. | Reset source and lifecycle tests pass. | Fixed |
| PX-004 | P1 | Lobby | Open first-login, check-in, or difficulty modal and press Android Back. | `MainMenuBootstrap.Update` checks routed panels only, then quits. | Add top-most modal ownership before panel/quit handling. | Modal-stack LIFO tests and journey smoke pass. | Fixed |
| PX-005 | P1 | Run / Interruption | Background the app or look for a pause action. | Lifecycle callbacks only alter performance; pause gesture is undiscoverable. | Visible pause flow plus idempotent interruption pause. | HUD/layout checks pass; `04-paused.png` captures the explicit flow. | Fixed |
| PX-006 | P1 | Difficulty / Launch | Dispatch StartFinal twice before the scene transition completes. | Run preparation happens before transition idempotency and may consume boosters twice. | Gate launch before `RunSession.Prepare*`. | Launch-gate unit checks and journey double-invoke pass. | Fixed |
| PX-007 | P2 | Run input | Compare the same physical gesture across 720p and high-density landscape screens. | Swipe/tap/double-tap thresholds are fixed pixels. | Use bounded viewport-relative thresholds. | Classifier tests pass at 1280x720, 1920x1080, and 2560x1440. | Fixed |
| PX-008 | P2 | Regression coverage | Run the existing runtime smoke. | It remains in MainMenu for eight seconds and never proves a player journey. | Add real-scene home-to-result smoke with screenshots. | Headed journey smoke passes with five non-blank screenshots. | Fixed |
| PX-009 | P2 | Run HUD | Start a run with no active power-up. | `03-running.png` shows an empty dark Buffs card occupying the lower-left view. | Hide the Buffs card whenever its summary is empty. | HUD validation passes; empty card is absent in `03-running.png`. | Fixed |
| PX-010 | P2 | Result | Return to camp and inspect the result page. | `05-result.png` shows the best/rank subline behind the score card. | Separate Subline and ScoreCard bounds. | Layout regression passes; `05-result.png` confirms separation. | Fixed |
| PX-011 | P2 | Difficulty | Open the difficulty modal on a 20:9 display. | `02-difficulty.png` shows speed/Boss timing in near-invisible tertiary gray. | Promote parameter text to the secondary contrast token. | Source validation passes; headed journey screenshot recaptured. | Fixed |
| PX-012 | P2 | Run visuals | Inspect the runner and distant props in `03-running.png`. | The silhouette and some distant set dressing remain visually simple compared with the refined lobby. | Defer to a dedicated detailed-model/LOD asset batch so collision and mobile budgets can be validated together. | Visual evidence retained in `Artifacts/PlayerExperienceAudit`. | Deferred |

## Authority boundaries

- `ObstacleColliderSpec` and `RunnerHitResolver` remain collision authority.
- `RunOutcomeRouter.Apply` remains the sole growth/save transaction authority.
- Visual geometry and effects remain non-authoritative.
- Boss, route-event, and theme-mechanic exclusion rules remain unchanged.

## Final evidence

To be completed after implementation: static gates, Unity validators, journey screenshots, soak result, APK path/hash/signature, and any deferred P2/P3 risks.
