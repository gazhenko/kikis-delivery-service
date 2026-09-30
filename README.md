# Kiki's Delivery Service — desktop prototype

A Unity 6.3 LTS / URP first neighborhood for the approved desktop, third-person rebuild. The previous browser game remains in `../kikis-delivery-service`.

## Playable intent

Depart Osono's bakery, fly along the shopping street to the clock square, reveal the harbor, follow the quay, climb toward Madame's garden and return home. Tombo's workshop and the airship add short and elevated delivery approaches. The first sightseeing circuit is designed for roughly 60–90 seconds; this timing still requires a real playtest.

Timed jobs, ingredient shopping, recipes, broom upgrades, crows, cozy/challenging modes, hunger, energy, hospital returns and local saves are implemented in source. Days and nights each last 150 simulation seconds; sleeping advances eight game hours. Menus do not end a turn or stop the delivery clock.

## Current status

The Unity VM is licensed, and native development builds run on Mac / Metal and Omarchy / OpenGLCore. Kiki now walks on foot, carrying her broom upright at her side with its bristles down, and mounts naturally on takeoff. Planted footsteps, overlapping dress/bow/hair motion, a relaxed free arm and Jiji’s shoulder-to-broom transition extend the film-referenced cel character.

The controls pass adds free mouse look, optional hold-RMB look, cruise, braking, recentering, bumper altitude controls, analog walking, street landing and configurable camera sensitivity/dead zone/inversion. Xbox and PlayStation prompts follow the active device. Menu confirmation cannot accidentally become takeoff. Detailed façade surfaces also keep the orbit camera out of shop awnings and roofs.

The environment retains joined narrow street façades, varied roofs, the clock tower, painted materials, connected service yards, cultivated gardens, orchard undergrowth and working-quay detail. The minimap uses the same 87 building/landmark footprints and 32 paths exported with the world. See [the walking/controls implementation and reference study](Docs/WALKING_CONTROLS.md), [native test record and walking reel](Docs/verification/walking-controls/README.md), and [earlier environment verification](Docs/verification/environment-pass/README.md).

The [polish pass](Docs/POLISH_PASS.md) adds a synthesized soundscape and an original title waltz, working aerial fog, and lit streets and households at night under stars and a moon. It brings chimney smoke, gulls, bobbing moored boats, headlands, a lighthouse, islands and a distant hill town. Kiki visibly carries each parcel and the fitted lantern on her broom, bows and waves on delivery, and is guided by a destination tag, a ribbon circle at the true delivery radius and Jiji's tips. The title flyover offers Continue or a backed-up New game. [Native verification](Docs/verification/polish-pass/README.md).

Animated townspeople and recipients, a human listening test, release profiling and a physical-controller playtest remain work for the desktop rebuild. Art, sound and flight performance are original project work; no production-film meshes, animation clips, recordings or film music are bundled.

See the [environment art notes and texture prompt](Docs/ENVIRONMENT.md), [motion implementation](Docs/MOTION.md), [motion clips and verification](Docs/verification/motion-pass/README.md), [character references and source](Docs/CHARACTER.md), [earlier rendering research](Docs/CEL_ART_PASS.md), [baseline verification](Docs/VERIFICATION.md), [visual direction](Docs/DIRECTION.md), [asset provenance](Docs/ART.md) and [development VM](Docs/VM.md).

## Play the Mac build

Open `Builds/mac/Kiki’s Delivery Service.app`. On a fresh game, choose Cozy or Challenging. With a save, choose *Continue your deliveries*; *Start a new game…* copies the current save to `koriko-desktop-v1.before-new-game.json` before starting again. Unity Editor and a Unity sign-in are not required to play the exported app. A distributable copy is packaged as `Builds/mac/Kiki-Delivery-Mac.zip`; this local development build is not notarized for public distribution.

```sh
open 'Builds/mac/Kiki’s Delivery Service.app'
```

At home, pick a delivery and choose Go outside. The parcel hangs from Kiki's broom. Walk with WASD or the left stick; Space, A/Cross or RB/R1 mounts and rises. Landing automatically returns to walking. Follow the paper tag (or the minimap line) to the ribbon circle, use E for an assisted landing, then press E inside the circle to deliver. Tab opens the bakery when home and settings while away. Settings includes *Controls & camera*, *Sound & display* (volumes, window or full screen, control hints) and Save & quit; Escape or B steps back from a sub-page.

## Play on Omarchy

Choose **Kiki’s Delivery Service** in the application launcher, or run `kiki-delivery`. The native Linux installation lives at `~/Games/KikiDelivery/current`, with versioned releases alongside it. It does not require Unity Editor or a Unity sign-in. The Mac desktop shortcut and Linux launcher use the September 30 polish release `20260930-9f5ea8f`, with previous releases kept for rollback. See [installation checks and locations](Docs/INSTALLATIONS.md).

`KORIKO_MAC_DESTINATION=Builds/mac-candidate bash Tools/build-on-vm.sh` stages a Mac candidate without replacing the installed app. `bash Tools/build-linux-on-vm.sh` exports a fresh Linux development player using the licensed VM, rejects shader compiler errors, and copies it into `Builds/linux/`. It does not deploy that export or change the Mac installation.

## Open and build

1. Use Unity **6000.3.20f1**, revision `c9ba695d4f07`, with Mac Mono or Windows Mono support as appropriate.
2. Sign into Unity Hub and activate a suitable Editor license.
3. Open this folder. Package dependencies resolve from `Packages/manifest.json`.
4. The editor creates `Assets/Koriko/Scenes/BakeryToHarbor.unity` after its first successful import. `Koriko > Rebuild prototype scene` regenerates it from authored FBX assets.
5. Press Play. Choose cozy or challenging, select a delivery and fly.
6. `Koriko > Build Mac prototype` exports a universal development app to `Builds/mac/`. Windows and Linux menu commands are also provided; platform support must be installed.

Batch build example, after activation:

```sh
~/Unity/6000.3.20f1/Editor/Unity -batchmode -nographics -buildTarget StandaloneOSX \
  -projectPath "$PWD" -executeMethod Koriko.Editor.DesktopBuild.Mac \
  -quit -logFile Logs/mac-build.log
```

## Controls

| Action | Keyboard / mouse | Controller |
| --- | --- | --- |
| Walk / fly | WASD | Left stick |
| Take off / rise | Space | A / Cross or RB / R1 |
| Descend | Ctrl or C | LB / L1 |
| Boost | Hold Shift | Hold RT / R2 |
| Brake / hover | Hold Q | Hold LT / L2 |
| Cruise toggle | F | L3 |
| Recenter | R | R3 |
| Look | Mouse; optional hold-RMB | Right stick |
| Land / deliver / bakery | E | X / Square |
| Bakery or settings | Tab | Y / Triangle |
| Close / settings | Escape | Start / Options |

Interact near a delivery court or above a clear street starts an assisted landing. Interact again, steer, climb or brake to cancel it. Deliveries require a slow, grounded landing. In menus, use the mouse, keyboard navigation/Enter, or controller D-pad/A/Cross; B/Circle goes back. Open **Controls & camera** in settings for sensitivity, inversion, dead-zone, mouse-look and camera-follow options.

## Source and checks

```sh
dotnet run --project Tests/Koriko.Core.Checks.csproj
blender --background --threads 4 --python Tools/build_art.py
```

The core project does not need Unity. Blender generation rebuilds the two editable sources and FBX exports, then produces seven preview PNGs in `Docs/previews/`. Append `-- --character-only` to rebuild only Kiki/Jiji, or `-- --world-only` to rebuild the environment, its layout data and two previews while preserving the character. Run from this project directory. Do not regenerate source art over an artist's manual `.blend` edits without preserving those edits first.

A development player accepts `--koriko-flight-check`. It creates synthetic keyboard and gamepad events to visit all six courts with the real movement code and camera, checks bakery interactions and visible shoe contact, captures native screenshots, and writes `flight-check/result.txt` under Unity's persistent data directory. It exits nonzero on a failed check and never loads or writes the player's save. This runner has passed on the Mac and the installed Omarchy/Linux player; it complements a human/controller playtest. The latest results are in `Docs/verification/walking-controls/`; earlier results remain in their dated verification folders.

Use `--koriko-walk-check` for on-foot movement, foot and hand contact, broom carry and keyboard/controller mounting and landing. Add `--koriko-record-walk` for every drawing. Use `--koriko-controls-check` for the native keyboard/mouse/Xbox/PlayStation input matrix. These runs isolate player saves and control preferences. Synthetic devices do not replace a physical-controller compatibility or ergonomic playtest.

Use `--koriko-tour-check` for the polish pass. It captures the title flyover, the bakery, parcel carry, the destination tag and ribbon circle, a delivery with its receipt and wave, lamplit streets, the lantern, the moonlit harbor, boats and headlands, the hill town and the settings pages. It exports every synthesized sound as WAV with levels, records twelve seconds of the listener mix and runs 33 assertions, writing to `tour-check/`. It plays at reduced volume and never reads or writes saves or preferences; other checks run muted.

Use `--koriko-environment-check` for twelve native street, rooftop, garden, harbor and day/night viewpoints. It writes `environment-check/` without loading or writing player saves. These captures support visual review and are not a route benchmark.

Use `--koriko-art-check` for eight repeatable art viewpoints, without gameplay assertions. This writes `art-check/` in the same persistent-data directory and also avoids player saves. `Tools/build-on-vm.sh` now rejects shader compiler errors before copying a player back from the VM.

Use `--koriko-character-check` for eight native model viewpoints and six rig assertions, including a real takeoff and cruise above 20 m/s. It writes `character-check/` and also avoids player saves. The neutral study backgrounds make face, silhouette, cloth and hand contact easier to review independently of the town.

Use `--koriko-motion-check` for a deterministic 24 fps native motion reel and ten action/contact/exposure assertions. It writes PNG sequences and CSV telemetry to `motion-check/`, without player saves. `Tools/export-motion-review.py` encodes these into four H.264 clips and a combined reel with ffmpeg. See [capture details](Docs/MOTION.md); this recording mode is not a frame-rate benchmark.

This first district has six delivery courts including home, rather than the previous region's 22 locations. Desktop is the current target; the original browser version remains the mobile/iPad route.
