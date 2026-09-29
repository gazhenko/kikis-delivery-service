# Kiki's Delivery Service — desktop prototype

A Unity 6.3 LTS / URP first neighborhood for the approved desktop, third-person rebuild. The previous browser game remains in `../kikis-delivery-service`.

## Playable intent

Depart Osono's bakery, fly along the shopping street to the clock square, reveal the harbor, follow the quay, climb toward Madame's garden and return home. Tombo's workshop and the airship add short and elevated delivery approaches. The first sightseeing circuit is designed for roughly 60–90 seconds; this timing still requires a real playtest.

Timed jobs, ingredient shopping, recipes, broom upgrades, crows, cozy/challenging modes, hunger, energy, hospital returns and local saves are implemented in source. Days and nights each last 150 simulation seconds; sleeping advances eight game hours. Menus do not end a turn or stop the delivery clock.

## Current status

The Unity VM is licensed, the project compiles, and a universal Mac development build runs on Apple M1 Pro / Metal. The latest environment pass adds joined narrow street façades, varied roofs, a rebuilt clock tower, painted surface materials, connected service yards, cultivated gardens, a greenhouse, orchard undergrowth and working-quay detail. Madame's raised garden has a real stair approach. The minimap uses the same 87 building/landmark footprints and 32 paths exported with the world.

The final environment build passed **25 delivery-route checks across all six landing courts** at 1920 × 1080 and native environment/HUD reviews at 1920 × 1080 and 1280 × 720. The route averaged **17.95 ms per frame (about 56 fps)** on M1 Pro / Metal, including captures and menus, not a controlled benchmark. See the [native screenshots, visual revisions and test record](Docs/verification/environment-pass/README.md).

Kiki's film-referenced cel model and motion are preserved: expressive takeoff, boost, banking, braking and landing, overlapping bow/hair/cloth/leg/bag/Jiji movement, fitted gaze and brief mesh smears. The previous **10 motion checks**, **six character checks**, [31.75-second native motion reel](Docs/verification/motion-pass/reel/Kiki-motion-reel.mp4) and independent 25-check core simulation result remain baseline evidence; those separate suites were not rerun for this environment-only change.

More bespoke secondary architecture and boats, animated background town life, ambient sound, release profiling and a physical-controller playtest remain work for the desktop rebuild. Art and flight performance are original project work; no production-film meshes or animation clips are bundled.

See the [environment art notes and texture prompt](Docs/ENVIRONMENT.md), [motion implementation](Docs/MOTION.md), [motion clips and verification](Docs/verification/motion-pass/README.md), [character references and source](Docs/CHARACTER.md), [earlier rendering research](Docs/CEL_ART_PASS.md), [baseline verification](Docs/VERIFICATION.md), [visual direction](Docs/DIRECTION.md), [asset provenance](Docs/ART.md) and [development VM](Docs/VM.md).

## Play the Mac build

Open `Builds/mac/Kiki’s Delivery Service.app`. On a fresh game, choose Cozy or Challenging; an existing save continues automatically. Unity Editor and a Unity sign-in are not required to play the exported app. A distributable copy is packaged as `Builds/mac/Kiki-Delivery-Mac.zip`; this local development build is not notarized for public distribution.

```sh
open 'Builds/mac/Kiki’s Delivery Service.app'
```

At home, pick a delivery and choose Fly. Follow the highlighted minimap destination, then use E for an assisted landing and again to deliver. Tab opens the bakery when home and settings while away. Settings includes Save & quit.

## Play on Omarchy

Choose **Kiki’s Delivery Service** in the application launcher, or run `kiki-delivery`. The native Linux installation lives at `~/Games/KikiDelivery/current`, with versioned releases alongside it. It does not require Unity Editor or a Unity sign-in. The Mac desktop shortcut and this Linux installation contain the same environment-art revision, `b093c2f`. See [installation checks and locations](Docs/INSTALLATIONS.md).

`bash Tools/build-linux-on-vm.sh` exports a fresh Linux development player using the licensed VM, rejects shader compiler errors, and copies it into `Builds/linux/`. It does not deploy that export or change the Mac installation.

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
| Fly | WASD | Left stick |
| Rise / descend | Space / Ctrl or C | A / B |
| Boost | Shift | Right trigger |
| Look | Hold right mouse and drag | Right stick |
| Land / deliver / bakery | E | X |
| Bakery or flight settings | Tab | Y |
| Close / settings | Escape | Start |

Within roughly 14 m of a delivery court and 18 m of its height, interact starts a gentle assisted approach. Steering, climbing or boosting cancels it. Deliveries require a slow, grounded landing. Controller labels use the Xbox convention.

## Source and checks

```sh
dotnet run --project Tests/Koriko.Core.Checks.csproj
blender --background --threads 4 --python Tools/build_art.py
```

The core project does not need Unity. Blender generation rebuilds the two editable sources and FBX exports, then produces seven preview PNGs in `Docs/previews/`. Append `-- --character-only` to rebuild only Kiki/Jiji, or `-- --world-only` to rebuild the environment, its layout data and two previews while preserving the character. Run from this project directory. Do not regenerate source art over an artist's manual `.blend` edits without preserving those edits first.

A development player accepts `--koriko-flight-check`. It creates synthetic keyboard and gamepad events to visit all six courts with the real movement code and camera, checks bakery interactions and visible shoe contact, captures native screenshots, and writes `flight-check/result.txt` under Unity's persistent data directory. It exits nonzero on a failed check and never loads or writes the player's save. This runner has passed on the Mac and the installed Omarchy/Linux player; it complements a human/controller playtest. Mac results and captures are in `Docs/verification/environment-pass/flight/`; the Linux installation result is in `Docs/verification/installations-2026-09-28/omarchy-flight/`.

Use `--koriko-environment-check` for twelve native street, rooftop, garden, harbor and day/night viewpoints. It writes `environment-check/` without loading or writing player saves. These captures support visual review and are not a route benchmark.

Use `--koriko-art-check` for eight repeatable art viewpoints, without gameplay assertions. This writes `art-check/` in the same persistent-data directory and also avoids player saves. `Tools/build-on-vm.sh` now rejects shader compiler errors before copying a player back from the VM.

Use `--koriko-character-check` for eight native model viewpoints and six rig assertions, including a real takeoff and cruise above 20 m/s. It writes `character-check/` and also avoids player saves. The neutral study backgrounds make face, silhouette, cloth and hand contact easier to review independently of the town.

Use `--koriko-motion-check` for a deterministic 24 fps native motion reel and ten action/contact/exposure assertions. It writes PNG sequences and CSV telemetry to `motion-check/`, without player saves. `Tools/export-motion-review.py` encodes these into four H.264 clips and a combined reel with ffmpeg. See [capture details](Docs/MOTION.md); this recording mode is not a frame-rate benchmark.

This first district has six delivery courts including home, rather than the previous region's 22 locations. Desktop is the current target; the original browser version remains the mobile/iPad route.
