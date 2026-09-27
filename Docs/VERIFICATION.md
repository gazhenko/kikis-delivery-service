# Verification record — 2026-09-27

## Completed

- .NET 8.0.425, macOS ARM64: **25 core simulation checks passed**. They exercise day/night boundaries, the exact eight-hour sleep skip, pickup deadlines, delivery position/speed/height rules, daily refresh, modes, purchases, upgrades, cooking, buffs, fatigue, crow damage, hospital returns and save validation/roundtrips.
- Blender 4.5.3 LTS, macOS ARM64: FBX and editable `.blend` exports produced; source-art views inspected at 1280 × 800 over multiple passes. Corrected character pivots, surface repetition, ground normals, missing garden/airship colliders, side windows and street overlap. Final source previews also show the revised character proportions, rounded tail and fuller broom.
- Proxmox VM 130: guest agent and key-based SSH work. Unity 6000.3.20f1 and Mac/Windows Mono modules installed. Download checksums verified.
- Ubuntu desktop at 1440 × 900: Unity Hub visibly opened; private VNC listens only on loopback; SSH tunnel and macOS Screen Sharing launched. Firefox is installed for the user's sign-in flow.
- Subsequent macOS connection check: Screen Sharing initially stopped at a password prompt. Configured a dedicated private VNC password, signed in through the native UI, and confirmed a full framebuffer session and the `ubuntu:99` window. This replaces the earlier launch-only connection check.
- Mesa `glxinfo -B`: llvmpipe, OpenGL 4.5, `Accelerated: no`.
- First Unity editor launch reached its license check and exited because there is no active license. This confirms the executable starts, not that the project compiles.
- Installed Unity source inspection identified and corrected the uGUI assembly reference and a URP property with an internal setter.
- Prepared a native `--koriko-flight-check` route runner using synthetic Input System keyboard events and the actual game motor/camera. It is unexecuted pending a licensed build; it does not substitute for physical-controller or human testing.

## Still required

1. Account holder completes Hub terms, signs in and activates an Editor license.
2. Import the Unity project; fix any C#, shader or package errors; verify generated scene and six landing-floor checks.
3. Fly bakery → clock square → harbor → garden → bakery using the actual controls and camera. Check the roofline, camera collisions, turns and landing courts. Test the elevated airship approach.
4. Test controller focus, off-screen row scrolling, recipe completion, continuous menu time, sleep transition, exhaustion return and save/reload in the actual player.
5. Build the universal Mac player on the VM, copy it to the Mac and test at 1440 × 900 and 1920 × 1080. Check executable architectures and local launch/signing requirements.
6. Review noon, sunset and night captures; measure frame time and draw calls on named GPU hardware. No FPS claim is established yet.

No desktop player, browser/mobile build, physical controller or iPad test has been completed for this rebuild. Existing browser deployment was not changed.

## Remaining quality risks

The character is an articulated prototype rather than a finished sculpt/skinned animation rig. The first district needs a playtest-driven art pass and more distinct background silhouettes. Audio is not implemented yet. The new engine alone does not establish the requested hand-painted film quality. The small district is the place to prove that quality before expanding the map.
