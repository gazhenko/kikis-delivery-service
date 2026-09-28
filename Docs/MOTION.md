# Kiki flight performance

The previous animation held the entire rig at 12 Hz and derived almost every pose from speed and one shared sine clock. That gave the character a rigid, coordinated movement even though the materials looked drawn. This pass separates acting, balance, secondary movement and the brief drawings that bridge fast actions.

## Performance

`RiderPerformance` now uses timed action accents and damped overlapping movement:

- Takeoff: compress, look upward, reach, then fold the legs into the flying seat. The input and flight motor respond immediately; the animation does not add control latency.
- Acceleration and boost: lean forward, tuck in, then settle into cruise. A boost gets a brief stretched silhouette.
- Steering: eyes and head look into the requested heading, shoulders bank behind them, and the feet, satchel and bow catch up at different rates. Left and right legs have different poses and timing.
- Braking: counter-lean, lower the legs, nod the broom, then recover balance. This also happens when releasing boost causes substantial deceleration.
- Hover and rest: breathing, small weight changes, asymmetric dangling feet, occasional glances and shaped blinks. Jiji has independent head, ear and tail responses.
- Touchdown: bend the knees, absorb the impact and recover. Visible shoes stay on the actual court surface; the visual correction does not move the collision controller or delivery coordinates.

Springs run with bounded substeps rather than holding every transform at the same rate. Flutter targets are sampled at 24 Hz; the fast deformation drawings have explicit exposure times. Camera, steering and collision retain their existing responsive updates.

## Rig and face

`Tools/character_model.py` remains the editable source for `art-source/KikiAndJiji.blend` and the exported FBX. The new rig adds separate bow-loop, eyebrow, Jiji-head, ear and tail pivots. Hair stream/side shapes and two hem shapes provide actual mesh deformation. The drawn fold meshes and cloth contour use the same weights.

Gaze uses face-conforming morphs for the irises, pupils and glints. Translating the original thin eye patches in a straight line caused an iris to sink into the curved face during a hard turn; the native review caught and corrected this. The quiet smile switches to a small breath expression during selected takeoff, braking and landing accents. Brows and eyelids support the action without changing the character's basic design.

Two-joint arm solving retains broom contact as the torso, broom and shoulders move independently. Wrist orientation follows the grip's rotation. A small analytic cloth contact correction keeps the skirt surface outside the articulated upper legs and knees; the cel paint, ink, fold lines, depth and shadow all use the same correction. This is a compact rig-specific solution, not a general cloth simulation.

## Smear drawings

`CharacterDeform.hlsl` briefly bends and stretches the rider's geometry along a rapid change of direction. Bow tips, clothes, feet and broom ends receive more displacement; the facial drawing receives very little. Paint, ink, depth and shadow share the deformation function, avoiding separated outlines or undeformed shadows.

Each event has a preparation drawing followed by one full smear drawing and one reduced smear drawing, each exposed for 1/24 second. A cooldown prevents repeated direction input from leaving the mesh continuously stretched. Steady cruise, ordinary idle and settled landings have no smear. This does not add camera blur, ghost copies, noise or a screen filter.

The rig and motion are original project work. The [official film stills](https://www.ghibli.jp/works/majo/) remain the visual reference; no production animation clips or film rig were supplied. The earlier [rendering research](CEL_ART_PASS.md) discusses the distinct treatment of characters and painted backgrounds. Shapefarm's [Orbitals development account](https://www.unrealengine.com/tech-blog/stepping-inside-a-retro-anime-inspired-game-a-look-into-the-rendering-of-orbitals) also describes separating stepped character/VFX timing from camera rendering. Our spring-driven performance and mesh smear implementation are project-specific choices, not a reproduction of that game's animation system.

## Native review

Launch the development player with `--koriko-motion-check`. It drives the real input system through takeoff, boost, braking, both turn directions and landing. It captures three neutral-background studies and a fourth clip with the normal gameplay camera, environment and HUD. It also records pose/contact telemetry and checks the motion's ranges, contact, speed and smear exposure. Like the existing checks, it uses a fresh game and never loads or writes the player's save.

The capture fixes simulation time at 24 Hz so each exported frame is one movie drawing. Capture time is **not** a performance benchmark. Render performance is checked separately with the existing full delivery route.

Encode a captured run with:

```sh
python3 Tools/export-motion-review.py \
  "$HOME/Library/Application Support/com.Koriko.Kiki---s-Delivery-Service/motion-check" \
  Docs/verification/motion-pass/reel
```

This requires ffmpeg and preserves the actual 24 fps timing. It copies telemetry and selected original frames alongside the four clips and combined reel. See the [motion verification record](verification/motion-pass/README.md) for the completed run, visual revisions and remaining limits.
