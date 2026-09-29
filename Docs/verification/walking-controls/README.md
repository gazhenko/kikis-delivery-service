# Walking, broom carry and controls verification — 2026-09-29

The character now walks on foot with an upright side-carried broom, planted steps, a swinging free arm, cloth/bow/hair overlap and Jiji on her shoulder. Mounting moves Jiji to the broom and brings the grip into the riding pose. [Implementation, film references and controls](../../WALKING_CONTROLS.md).

## Native animation review

[Watch the 22.79-second walking reel](reel/Kiki-walking-reel.mp4). Three clips contain **547 native frames at 24 Hz**: walking and settling, mount/flight/dismount, and controller analog walking and mounting. [Result](reel/result.txt), [walking telemetry](reel/01-walk-and-settle.csv), [transition telemetry](reel/02-mount-flight-dismount.csv). This reel was captured on Omarchy before the final camera-collision adjustment; character geometry and performance are identical to the installed candidate. Final native checks additionally exercise the updated camera.

The front/three-quarter carry, walking contact, early and late mounting, landing and adjacent transition drawings were visually inspected. The neutral background separates the pose from scenery; it is a review camera, not the gameplay camera. The source previews under `Docs/previews/` are Blender previews and are not substituted for native game captures.

## Completed checks

| Suite | Mac — Apple M1 Pro / Metal | Omarchy — RTX 3080 / OpenGLCore |
| --- | --- | --- |
| Keyboard, mouse, Xbox/PlayStation synthetic input, UI and camera | [45 passed](mac-controls/result.txt) | [45 passed](omarchy-controls/result.txt) |
| Walking, planted feet, carry, mount, flight and dismount | [17 passed](mac-walk/result.txt) | [17 passed during reel capture](reel/result.txt) |
| Six-court delivery route, bakery interactions and persistence | [25 passed](mac-flight/result.txt) | [25 passed](omarchy-flight/result.txt) |
| Airborne action, contact, cloth and smear regression | Not repeated on this platform | [10 passed; 762 sampled frames](omarchy-motion/result.txt) |

All final native captures above rendered at **1280 × 720**, with Unity 6000.3.20f1. Early Omarchy review windows were tiled to 1223 × 1390; the final runs floated and resized only their own QA windows. Separate [core simulation checks passed all 26 assertions](core-result.txt), including reduced walking fatigue without slowing the clock. Both exports completed with shader-error guards enabled. Mac code-signature validation and ZIP integrity validation also passed.

The full route averaged **17.74 ms/frame on Mac** and **16.87 ms/frame on Omarchy**. These runs include screenshots, menus and development-player overhead and are not controlled benchmarks. Walking and controls tests use fixed capture timing and provide no performance measurement.

The airborne regression measured a 26.32 m/s peak speed, banks from −21.1° to 24.4°, 20 smear drawings across ten events, and a longest smear exposure of 2/24 seconds. Hand and sole errors rounded to 0.0000 m at the recorded precision. Walking foot-plant displacement also rounded to 0.0000 m; acceptance thresholds remain explicit in the individual results.

## Revisions driven by checks

- The first walking review exposed an elevated carrying elbow and unreachable planted-foot targets. The downward elbow pole and shared pelvis support were corrected. [Initial contact failures](first-review/walking-contact-failures.txt), [initial elbow capture](first-review/carrying-elbow-before.png).
- Native controller submission exposed a menu-close ordering race: a held confirm button could become takeoff. `ClosePanel` now synchronously requires release before flight lift resumes. The final tests cover held confirm, held cancel and a fresh takeoff press. [Initial failure](first-review/menu-submit-failure.txt).
- The old route runner attempted takeoff without a neutral frame after menu closure. It now releases the button before testing a fresh rise input, matching the deliberate menu guard. Its failure path also no longer removes an already-cleaned-up device. [Initial route result](first-review/route-held-input-test.txt).
- The camera could enter the bakery awning, which was outside the simplified movement collider. Camera-only mesh surfaces now cover the street architecture, and the final input checks verify clearance without affecting rider collision. [Before](first-review/camera-awning-before.png), [after on Mac](mac-controls/01-controls-mouse.png), [after on Omarchy](omarchy-controls/01-controls-mouse.png).
- Camera target warps now notify Cinemachine so damping does not pull the view across town after a reset or hospital return.

[Gameplay at the bakery](mac-flight/01-bakery.png), [ordinary-street landing](omarchy-controls/03-street-landing.png), [PlayStation ground prompts](omarchy-controls/05-controller-ground-hud.png).

## Scope and limits

The native tests inject Keyboard, Mouse, Gamepad and DualShockGamepad events through Unity’s actual Input System, motor, physics, camera, UI raycaster, navigation and submit paths. They use fresh simulations, never load or write player progress, and do not change saved control preferences. QA intentionally does not capture the desktop cursor.

**No physical USB/Bluetooth controller, physical mouse ergonomics, rumble, hot-plug driver compatibility or human flight-feel playtest was performed.** Input logic and synthetic layout coverage passed; hardware feel still needs hands-on validation. Browser, mobile/iPad, Windows and other GPU combinations were not tested in this desktop pass. Fully remappable actions and controller stick swapping are not implemented.

Installation paths, source revision, hashes and launcher checks are recorded in [the installation notes](../../INSTALLATIONS.md).
