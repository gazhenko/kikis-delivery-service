# Walking, broom carry and controls

## Film study

This pass reviewed [Studio Ghibli’s official still gallery](https://www.ghibli.jp/works/majo/), especially [the standing dress silhouette](https://www.ghibli.jp/gallery/majo006.jpg), [the doorstep broom grip](https://www.ghibli.jp/gallery/majo034.jpg), [the crouched takeoff](https://www.ghibli.jp/gallery/majo046.jpg) and [Jiji on Kiki’s shoulder](https://www.ghibli.jp/gallery/majo050.jpg). The carried broom uses the doorstep orientation: upright at her side, bristles down, its tip resting on the pavement. Reference images were inspected outside the project and are not bundled as game art. All rig, meshes, morphs and motion remain original project work.

The existing cel model is retained and extended with a dedicated carry grip, broom-ground contact marker, relaxed free-hand drawing and alternating walking-cloth morphs. The corresponding editable Blender source and FBX are regenerated together. World assets are unchanged.

## Performance

Grounded Kiki walks instead of sliding astride the broom. The walking pace is capped at 2.1 m/s, supports slower analog movement and uses less energy than flight while the delivery clock keeps running. Space or the controller’s south button takes off; touching down automatically returns to walking. No additional mode menu is needed.

`RiderGroundPerformance` extends the existing flight performance with world-space foot plants, separate swing and support phases, a pelvis-support solve, short settling steps, a swinging free arm and a ground-fitted broom. An initial review exposed an elevated carrying elbow and unreachable foot targets during turns. The elbow pole and shared pelvis solve were corrected rather than relaxing contact tolerances. The original failures are retained in the verification record.

The bow, hair, dress and satchel overlap the walk at different rates. Jiji perches on the shoulder on foot and moves to the broom during the mounting arc. The broom sweeps beside the hip before entering the riding position; the right hand slides between the carry and riding grips while the left hand reaches in later. Takeoff, bank, boost, brake, landing, gaze, blinking and brief smear drawings remain active. Landing drawings have enough vertical accommodation to reach the actual court before settling into a standing pose.

## Controls

The controls review used Warner Bros.’ [official Hogwarts Legacy accessibility guide](https://portkeygamessupport.wbgames.com/hc/en-us/articles/11893402078355-Hogwarts-Legacy-Accessibility-Features-A11Y), particularly its separate sensitivity, dead-zone, vertical-inversion and camera-follow options. Cruise, braking and the following bindings are choices for this game; they are not a claim to reproduce Hogwarts Legacy’s complete flight model.

| Action | Keyboard / mouse | Xbox-style controller | PlayStation-style controller |
| --- | --- | --- | --- |
| Walk / fly | WASD | Left stick | Left stick |
| Look | Mouse; optional hold-RMB mode | Right stick | Right stick |
| Take off / rise | Space | A or RB | Cross or R1 |
| Descend | C or Ctrl | LB; B also works outside menus | L1; Circle also works outside menus |
| Boost | Hold Shift | Hold RT | Hold R2 |
| Brake / hover | Hold Q | Hold LT | Hold L2 |
| Toggle cruise | F | L3 | L3 |
| Recenter camera | R | R3 | R3 |
| Land / deliver / visit | E | X | Square |
| Menu | Tab / Escape | Y / Start | Triangle / Options |
| Confirm / back in menus | Enter / Escape; mouse click | A / B | Cross / Circle |

Cruise supplies forward movement without holding an input. Braking overrides boost and movement, and cancels cruise. Reverse input, menus and controller disconnection also stop cruise. A second interaction press or manual steering/braking cancels landing assistance. Landing assistance now accepts clear walkable street surfaces within 18.5 m below Kiki, as well as the existing delivery courts. It rejects steep faces, water and blocked standing space. The broom catches a fall from the quay instead of leaving an on-foot character hovering over water.

The current input device follows meaningful activity, not a timeout or mere controller connection. Stick input is rescaled outside a configurable radial dead zone; camera input has a gentler response near the center. Climbing and descending on the bumpers leave both thumbs available for movement and looking. PlayStation layouts get matching button names.

Menus expose mouse sensitivity, stick sensitivity, stick dead zone, vertical look inversion, free/hold mouse look and camera-follow speed. Preferences persist separately from game progress. Free mouse look captures the cursor during keyboard/mouse play and releases it for menus, controller play or lost focus. Closing a menu explicitly requires release of the submit/cancel lift buttons before they can affect flight; the native tests caught and corrected this ordering issue. QA runs do not alter saved preferences or capture the desktop cursor.

Native review also exposed camera clipping through the bakery awning. The scene now adds camera-only mesh surfaces to the existing street façade batches, covering overhangs that simple flight collision volumes omit. These surfaces do not collide with Kiki. Camera warps notify Cinemachine, preventing its damping from dragging the view across town after a hospital return or reset.

## Verification modes

- `--koriko-walk-check`: native keyboard/controller ground movement, carry contact, foot plants, stop, mount, approach and dismount. Add `--koriko-record-walk` to capture every 24 Hz drawing for the reel.
- `--koriko-controls-check`: synthetic Keyboard, Mouse, Gamepad and DualShockGamepad events through the real Input System, motor, camera, UI raycaster, navigation and submit actions. Includes device switching/disconnection, drift, analog speeds, cruise, boost, braking, altitude, recentering, settings, held menu buttons and ordinary-street landing.
- Existing `--koriko-flight-check` and `--koriko-motion-check`: delivery-route and airborne-performance regressions.

All modes use fresh simulations and never read or write player saves. Fixed-rate animation/input captures are not performance benchmarks. Synthetic controller layouts do not establish physical USB/Bluetooth compatibility or ergonomic feel. See [the native evidence and completed test record](verification/walking-controls/README.md).
