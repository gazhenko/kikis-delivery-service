# Verification record — 2026-09-27

## Build and hardware

- Unity **6000.3.20f1**, URP **17.3.0**, Input System **1.17.0**, Cinemachine **3.1.7**. Unity Personal activation completed in the Ubuntu 24.04 Proxmox VM. Package resolution, C# compilation, shader compilation and the Mac export succeeded.
- VM 130 builds with 8 vCPUs and no accelerated GPU. Its llvmpipe renderer is not used as evidence of gameplay performance. Existing Proxmox workloads were left intact.
- The universal Mac player contains **arm64 and x86_64** executables. Local `codesign --verify --deep --strict` passed. It is a development build, not a notarized public release.
- Native testing ran on **Apple M1 Pro / Metal, 16 GPU cores, 16 GB RAM**, at **1440 × 900** and **1920 × 1080**. The earlier 1440 run preceded the extended terrain, final paper UI and frame cap. These runs are not a resolution comparison.
- The rebuilt scene validates four rider import anchors and six landing-floor raycasts before export. Generated Unity scene, settings and `.meta` GUIDs are preserved in source control.

## Completed gameplay checks

The native `--koriko-flight-check` runner passed **18 checks** using synthetic keyboard and gamepad events with the actual Input System, flight motor, camera, UI and renderer:

1. Collect the bakery parcel.
2. Land slowly at the clock square.
3. Deliver through the interaction input.
4. Land slowly at the harbor.
5. Land slowly at Madame's garden.
6. Land slowly at the elevated airship platform.
7. Return and land slowly at the bakery.
8. Sleep advances the clock by eight hours.
9. Sleep restores energy.
10. Buy an ingredient using controller UI submit.
11. Start a recipe through controller UI.
12. Coffee completes and grants its fatigue buff.
13. Time continues while the kitchen is open.
14. Controller navigation scrolls lower recipe rows into view.
15. A broom purchase applies through controller UI.
16. Controller rise input reaches the flight motor.
17. Unity save serialization roundtrip succeeds.
18. All scene material shaders are supported on Metal.

The runner approaches the airship below its envelope, lands at the cargo court, and returns to the bakery. It never loads or writes the player's save. Its save check covers `JsonUtility` and state restoration; it does **not** establish successful save-file recovery after an application restart.

The independent .NET 8.0.425 core simulation previously passed **25 checks** for day/night boundaries, exact sleep skip, deadlines, position/speed/height delivery rules, daily refresh, modes, purchases, upgrades, cooking, buffs, fatigue, crow damage, hospital returns and save validation. Core rules were unchanged during the native import and landscape fixes.

## Visual review and fixes

Native screenshots were reviewed at takeoff, cruise, landing, return and in the bakery. Initial failures and selected later frames are retained under `verification/`; Blender source previews are separately stored in `previews/`.

- Corrected the rider FBX transform: the first native build showed Kiki sideways with a vertical broom. Explicit orientation anchors now keep rider, broom and Jiji upright.
- Corrected assisted landing that stopped while hovering above a destination; the motor now continues descending until grounded. All five route landings passed.
- Extended the terrain under northern trees, added distant hills, and widened the sea/quay so the scene no longer exposes that terrain edge from the garden approach.
- Fixed reversed terrain normals caught by the landing-floor checks before export.
- Connected the northern streets to match the town plan. Added a park path loop, garden beds, benches, orchard fencing and a pasture with two cows.
- Added the generated painted sky, warmer windows at night and brighter dawn. Reviewed noon, sunset and night captures. These three phases are set explicitly by the runner; this is not a complete five-minute day/night soak test.
- Added paper-backed prompts and notices, separated controls from vitality meters, kept cooking time live, and made the bakery board reserve space for the energy meter on wide windows.

## Performance evidence

The player caps rendering at 60 fps. The final 1920 × 1080 automated route measured **18.54 ms mean frame time**, approximately **54 fps**, with peaks of **1,142 draw calls** and **1,891,466 triangles**. This includes screenshot captures, menus and route stops, excludes the first three seconds, and is not a clean GPU benchmark or evidence of stable 60 fps. The exact final-run measurement is retained in `verification/1920x1080/result.txt`.

The earlier uncapped 1440 × 900 run is retained under `verification/1440x900/`; its different scene revision and frame cap prevent a useful direct comparison.

## Remaining checks and quality limits

- Human flight, camera and landing playtest; physical Xbox/PlayStation controller testing. Synthetic input does not establish comfort or hardware mappings.
- Save, quit, relaunch and continue through the real file path. Play longer sessions in both modes, including exhaustion, crows and deadline failure in the native player; their rules currently have core checks.
- Profile a release build and reduce draw calls, repeated geometry and distant foliage cost. The stable 60 fps target remains open.
- Native Mac was the only target tested. No Windows/Linux export, new browser/mobile/iPad build or touch controls were tested for this desktop rebuild. The previous browser deployment was not changed.
- Character anatomy, expressions, movement animation and architecture are prototype quality. Repeated facades and foliage still need bespoke art. Audio is not implemented. The engine migration has not yet delivered the requested moving, hand-painted-film aesthetic.

The next useful milestone is a finished bakery street with production character animation and ambient sound, judged in motion before extending the map.
