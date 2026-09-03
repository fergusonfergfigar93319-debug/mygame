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
| PX-012 | P2 | Run visuals | Inspect the runner and distant props in `03-running.png`. | The silhouette and some distant set dressing remain visually simple compared with the refined lobby. | View-space avatar lighting plus a mid-ground CC0 belt at 13–16 m. | Visual evidence retained in `Artifacts/PlayerExperienceAudit`; far-belt models locked by `VerifyCubePetAndNatureProps.ps1`. | Runner silhouette fixed 2026-07-26; mid-ground belt landed 2026-09-03; Android frame-rate still deferred |

## Authority boundaries

- `ObstacleColliderSpec` and `RunnerHitResolver` remain collision authority.
- `RunOutcomeRouter.Apply` remains the sole growth/save transaction authority.
- Visual geometry and effects remain non-authoritative.
- Boss, route-event, and theme-mechanic exclusion rules remain unchanged.

## Final evidence

- Static release gates: all eight passed, including `VerifyPlayerExperienceAudit.ps1` in the Android pre-build chain.
- Unity validators: player-experience, UI-layout, theme-mechanic, and visual-asset validators passed.
- Runtime coverage: the fast MainMenu startup probe and the headed home-to-result journey both passed without game errors or exceptions.
- Journey evidence: five non-blank captures are stored in `Artifacts/PlayerExperienceAudit` for home, difficulty, running, pause, and result states.
- Stability soak: 18 scenarios x 900 seconds x 60 Hz passed; 373 events, 24 cancellations, 344 rewards, peak apparatus 6/24, peak event FX 0/12, and 0 managed bytes allocated by the measured hot path.
- Visual assets: 48 prefabs passed dual-LOD validation; LOD0 totals 83,904 triangles and LOD1 totals 44,748 triangles.
- Android artifact: `Builds/Android/PenguinRun-Latest-Test-v1.1.2-4.apk`, package `com.example.mygame`, 92.81 MB, SHA-256 `B550444F8A6F6B838E6281C7875BD3C1335592AAB02FF437E7368CF6FA06C105`.
- APK signing: APK Signature Scheme v2 verified, one Android Debug signer.
- Device acceptance: not executed because `adb devices -l` reported no connected Android device on 2026-07-16.
- Deferred risk: PX-012 mid-ground belt is in code; Android on-device frame-rate and draw-call acceptance still need a device.

## 2026-07-26 update on PX-012

The runner half of PX-012 is resolved. The mid-ground set-dressing belt landed 2026-09-03. Cube roadside lamps, flags, crystals, snow banks, seaweed, and theme set pieces (coral / ruin / chest / arch / veil / cloud) were swapped to existing sculptures / CC0 models the same day.

The root cause turned out to be lighting, not model detail. `GugugagaStylized.shader`
used a world-space key direction whose +Z pointed down the track, so the side of the
avatar the player actually sees — its back — sat permanently at the 0.38 ambient wrap
floor. No amount of extra geometry or brighter vertex colour would have fixed that.
The key light is now fixed in view space, and measured avatar `meanLuma` at real play
distance went from 53.8 to 76.0.

Alongside it the silhouette was reworked (single lofted flipper instead of a fat
ellipsoid plus three detached torso blobs; surface patches clipped to the body
outline; goggles seated on the forehead). LOD triangle counts went *down*:
36608/19160/7328 to 29616/15712/6792.

New gate `ProjectAvatarGameplayViewCapture` shoots the three lanes from the real
runner camera and asserts `meanLuma >= 65` and `detailStdDev >= 18`. The threshold is
calibrated so the pre-fix shader fails it. The close-range
`ProjectPenguinPreviewCapture` could never have caught this: it shoots at 4 m with a
34-degree lens, while the player sees 10.5 m through a 72-degree one.

Mid-ground set dressing landed 2026-09-03: a second CC0 belt at |x| 13–16 m
fills the empty band between near props and distant ridges. Collision authority
is unchanged (open props still strip colliders). Still deferred: Android
on-device frame-rate acceptance (no device attached to this machine).
