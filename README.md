# Kiki's Delivery Service — desktop prototype

A Unity 6.3 LTS / URP first neighborhood for the approved desktop, third-person rebuild. The previous browser game remains in `../kikis-delivery-service`.

## Playable intent

Depart Osono's bakery, fly along the shopping street to the clock square, reveal the harbor, follow the quay, climb toward Madame's garden and return home. Tombo's workshop and the airship add short and elevated delivery approaches. The first sightseeing circuit is designed for roughly 60–90 seconds; this timing still requires a real playtest.

Timed jobs, ingredient shopping, recipes, broom upgrades, crows, cozy/challenging modes, hunger, energy, hospital returns and local saves are implemented in source. Days and nights each last 150 simulation seconds; sleeping advances eight game hours. Menus do not end a turn or stop the delivery clock.

## Current status

The Unity VM is licensed, the project compiles, and a universal Mac development build runs on Apple M1 Pro / Metal. Kiki's model uses film-referenced proportions and cel materials. The latest pass replaces the rigid shared pose timing with expressive takeoff, boost, banking, braking and landing, independent movement in the bow, hair, skirt, legs, bag and Jiji, fitted gaze and short mesh smear drawings. Hands follow the tilting broom and shoes meet the visible landing surfaces.

The final Mac build passed **10 motion checks**, **six character checks** and **23 delivery-route checks**. The [31.75-second native motion reel](Docs/verification/motion-pass/reel/Kiki-motion-reel.mp4) includes close-up actions and the real gameplay camera at 1280 × 720. Character and route checks ran at 1920 × 1080; the route averaged 18.17 ms per frame (about 55 fps), including captures and menus, not a controlled performance benchmark. It checks five landing courts, deliveries, sleep, recipes, upgrades and synthetic controller input. The unchanged independent core simulation's earlier 25-check .NET 8 result remains baseline evidence.

Dialogue-specific acting, repeated architecture, foliage, ambient sound, release performance profiling and a physical-controller playtest remain work for the desktop rebuild. The flight performance is original project animation with procedural overlap, not imported production-film clips.

See the [motion implementation](Docs/MOTION.md), [latest clips and verification](Docs/verification/motion-pass/README.md), [character references and source](Docs/CHARACTER.md), [earlier rendering research](Docs/CEL_ART_PASS.md), [baseline verification](Docs/VERIFICATION.md), [visual direction](Docs/DIRECTION.md), [asset provenance](Docs/ART.md) and [development VM](Docs/VM.md).

## Play the Mac build

Open `Builds/mac/Kiki’s Delivery Service.app`. On a fresh game, choose Cozy or Challenging; an existing save continues automatically. Unity Editor and a Unity sign-in are not required to play the exported app. A distributable copy is packaged as `Builds/mac/Kiki-Delivery-Mac.zip`; this local development build is not notarized for public distribution.

```sh
open 'Builds/mac/Kiki’s Delivery Service.app'
```

At home, pick a delivery and choose Fly. Follow the highlighted minimap destination, then use E for an assisted landing and again to deliver. Tab opens the bakery when home and settings while away. Settings includes Save & quit.

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

The core project does not need Unity. Blender generation rebuilds the two editable sources and FBX exports, then produces seven preview PNGs in `Docs/previews/`. Append `-- --character-only` to rebuild only Kiki/Jiji and the five character previews, preserving the neighborhood art. Run from this project directory. Do not regenerate source art over an artist's manual `.blend` edits without preserving those edits first.

A development player accepts `--koriko-flight-check`. It creates synthetic keyboard and gamepad events to fly a delivery circuit with the real movement code and camera, checks bakery interactions and visible shoe contact, captures native screenshots, and writes `flight-check/result.txt` under Unity's persistent data directory. It exits nonzero on a failed check and never loads or writes the player's save. This runner has passed on the Mac; it complements a human/controller playtest. The latest results and captures are in `Docs/verification/motion-pass/flight/`; earlier runs remain under `Docs/verification/character-pass/`, `Docs/verification/film-pass/` and `Docs/verification/1920x1080/`.

Use `--koriko-art-check` for eight repeatable art viewpoints, without gameplay assertions. This writes `art-check/` in the same persistent-data directory and also avoids player saves. `Tools/build-on-vm.sh` now rejects shader compiler errors before copying a player back from the VM.

Use `--koriko-character-check` for eight native model viewpoints and six rig assertions, including a real takeoff and cruise above 20 m/s. It writes `character-check/` and also avoids player saves. The neutral study backgrounds make face, silhouette, cloth and hand contact easier to review independently of the town.

Use `--koriko-motion-check` for a deterministic 24 fps native motion reel and ten action/contact/exposure assertions. It writes PNG sequences and CSV telemetry to `motion-check/`, without player saves. `Tools/export-motion-review.py` encodes these into four H.264 clips and a combined reel with ffmpeg. See [capture details](Docs/MOTION.md); this recording mode is not a frame-rate benchmark.

This first district has six delivery courts including home, rather than the previous region's 22 locations. Desktop is the current target; the original browser version remains the mobile/iPad route.
