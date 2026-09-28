# Flight animation pass — native verification

2026-09-27. The uniform held-pose loop has been replaced with takeoff, boost, banking, braking, hover and landing performances; independent overlap in the bow, hair, skirt, legs, bag and Jiji; fitted eye movement; and brief mesh smear drawings. See [implementation and source](../../MOTION.md).

## Watch the result

[Combined native motion reel — 31.75 seconds](reel/Kiki-motion-reel.mp4), or the individual clips:

1. [Takeoff, boost and braking](reel/01-takeoff-boost-brake.mp4) — 213 drawings, 8.875 seconds.
2. [Banking, reversals and smear poses](reel/02-banks-and-smears.mp4) — 200 drawings, 8.333 seconds.
3. [Landing, recovery and rest](reel/03-landing-and-rest.mp4) — 159 drawings, 6.625 seconds.
4. [Normal gameplay camera and HUD](reel/04-gameplay-camera.mp4) — 190 drawings, 7.917 seconds.

These are actual Unity/Metal renders, not generated concept animation. The first three clips isolate the character against a neutral background. The fourth uses the real third-person camera, town, painted sky and HUD. The silent H.264 reel is 1280 × 720, 24 fps, 762 frames, about 6.7 MiB. Capture uses a fixed simulation timestep and synthetic input; it is not a real-time performance benchmark. Selected original PNG drawings and per-frame CSV telemetry are beside each clip.

## Visual passes and corrections

Native frame sequences, adjacent smear drawings, pose telemetry, close-up model views and gameplay captures were inspected. The review included both turn directions, acceleration, boost release, hover, touchdown, front/profile/rear views and daytime/night rendering.

- The first pass exposed a disappearing iris during a sideways glance. Face-conforming gaze morphs replace straight translations of the thin painted eye features. Compare the [rejected hard-turn drawing](first-review/gaze-and-cloth-before-fix.png) with the corrected frames in the banking clip.
- Increased skirt clearance, reduced lateral hem deformation and removed always-covered upper-thigh surfaces. Added a shared paint/ink/depth/shadow contact correction around the moving legs, so secondary animation does not depend solely on a single static seated pose.
- Separated the bow loops and Jiji's head, ears and tail. Wind bends the lower bob while its crown stays attached. The two legs trail and recover at different rates; the hands stay on the moving broom and their orientation follows its grips.
- Restored the skybox when switching the recorder from its neutral study background to the gameplay camera. This was a capture setup issue, not a change to the normal game's sky.
- The initial motion assertion incorrectly counted three airborne shot-setup Warps as expected player takeoffs. It now requires the actual keyboard takeoff and touchdown. The rejected result is retained in `first-review/`; Warp intentionally resets animation without inventing a takeoff action.

The third build attempt stalled in Unity's package manager before scene generation. Its owned build process was terminated and the normal build retried successfully. No package versions, license settings or gameplay saves were changed to recover it.

## Completed checks

The final universal Mac development app was built with Unity 6000.3.20f1 / URP 17.3.0 on the licensed Ubuntu VM. Required pivots, flight/wind/gaze morphs, import anchors, 77 collision meshes and six landing courts validated. Native tests ran on Apple M1 Pro / Metal, with no reported runtime exceptions or shader errors:

| Native run | Size | Result |
| --- | --- | --- |
| [Motion and contact telemetry](reel/result.txt) | 1280 × 720 | 10/10 passed |
| [Character studies](character/result.txt) | 1920 × 1080 | 6/6 passed |
| [Delivery/bakery circuit](flight/result.txt) | 1920 × 1080 | 23/23 passed |

The motion reel contains 10 smear events and 20 deformed drawings; the longest continuous smear is **2/24 second**. Rest has zero smear. Rig hand and sole contact errors round below 0.1 mm in the recorded metrics; their acceptance tolerances are 2.5 cm and 3.5 cm. Body bank spans −20.6° to +24.4°, body pitch −9.0° to +29.5°, and bow overlap −25.8° to +28.2°. Actual boost speed reaches 26.32 m/s. These measurements supplement visual review; they cannot establish artistic quality by themselves.

The full route is bakery → clock square → harbor → Madame's garden → airship cargo court → bakery. It passed visible-floor contact at all five arrivals, delivery interaction, eight-hour sleep, restored energy, ingredient purchase, cooking and coffee buff, continuous time in menus, controller UI scrolling, broom upgrade, controller rise input and save serialization. Screenshots include noon, sunset and night. Testing never loaded or wrote the player's save.

The final route averaged **18.17 ms/frame**, about **55 fps**, with peaks of **1,452 draw calls** and **2,467,194 triangles**. The prior character pass measured 18.80 ms. These single automated routes include captures and menus; their difference is not a controlled optimization claim, and they do not establish a stable 60 fps floor.

`codesign --verify --deep --strict` passed. The app contains arm64 and x86_64 executables; runtime testing used arm64 only. The refreshed **122.7 MiB** ZIP passed integrity verification. [Artifact checksum and check counts](artifact-manifest.json) identify the delivered package. The existing desktop shortcut points to this app.

## Limits

This remains a development build, not a notarized public release. The test inputs are synthetic keyboard/gamepad events, not a physical-controller or human comfort playtest. Windows, Linux, Intel Mac, mobile and browser runtimes were not exercised in this pass. The earlier browser game is separate.

The animation uses project-authored action accents, morphs and procedural overlap; no production-film rig or clips were available. Dialogue acting, expanded environment art and ambient sound remain separate work. The new animation and drawing exposure can be judged directly in the reel without relying on the still portraits or assertion counts.
