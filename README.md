# Kiki's Delivery Service — desktop prototype

A Unity 6.3 LTS / URP first neighborhood for the approved desktop, third-person rebuild. The previous browser game remains in `../kikis-delivery-service`.

## Playable intent

Depart Osono's bakery, fly along the shopping street to the clock square, reveal the harbor, follow the quay, climb toward Madame's garden and return home. Tombo's workshop and the airship add short and elevated delivery approaches. The first sightseeing circuit is designed for roughly 60–90 seconds; this timing still requires a real playtest.

Timed jobs, ingredient shopping, recipes, broom upgrades, crows, cozy/challenging modes, hunger, energy, hospital returns and local saves are implemented in source. Days and nights each last 150 simulation seconds; sleeping advances eight game hours. Menus do not end a turn or stop the delivery clock.

## Current status

The Unity VM is licensed, the project compiles, and a universal Mac development build runs on Apple M1 Pro / Metal. The latest painted-film pass adds character cel colors, selective ink contours, held animation poses, quieter painted scenery, illustrated shopfronts and graphic sea strokes. It passed all 18 native gameplay checks at 1920 × 1080, plus eight fixed art-view captures across noon, sunset and night. The route averaged 18.55 ms per frame (about 54 fps); this includes captures and menus, not a clean performance benchmark.

The checks cover a delivery, five destination landings, cooking, purchases, controller input, continuous menu time, sleep and save serialization. The earlier prototype also passed at 1440 × 900, and the unchanged independent core simulation has passed 25 checks with .NET 8.

The character, buildings and animation are still prototype art. This is a playable foundation for the desktop rebuild; it has not yet reached the requested hand-painted film quality. Audio and a physical-controller playtest remain outstanding.

See the [film-pass research and implementation](Docs/CEL_ART_PASS.md), [latest screenshots and verification](Docs/verification/film-pass/README.md), [baseline verification](Docs/VERIFICATION.md), [visual direction](Docs/DIRECTION.md), [asset provenance](Docs/ART.md) and [development VM](Docs/VM.md).

## Play the Mac build

Open `Builds/mac/Kiki’s Delivery Service.app` and choose Cozy or Challenging. Unity Editor and a Unity sign-in are not required to play the exported app. A distributable copy is packaged as `Builds/mac/Kiki-Delivery-Mac.zip`; this local development build is not notarized for public distribution.

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

The core project does not need Unity. Blender generation rebuilds the two editable sources and FBX exports, then produces four preview PNGs in `Docs/previews/`. Run from this project directory. Do not regenerate source art over an artist's manual `.blend` edits without preserving those edits first.

A development player accepts `--koriko-flight-check`. It creates synthetic keyboard and gamepad events to fly a delivery circuit with the real movement code and camera, checks bakery interactions, captures native screenshots, and writes `flight-check/result.txt` under Unity's persistent data directory. It exits nonzero on a failed check and never loads or writes the player's save. This runner has passed on the Mac; it complements a human/controller playtest. The latest run's results and captures are in `Docs/verification/film-pass/flight/`; the earlier prototype is retained in `Docs/verification/1920x1080/`.

Use `--koriko-art-check` for eight repeatable art viewpoints, without gameplay assertions. This writes `art-check/` in the same persistent-data directory and also avoids player saves. `Tools/build-on-vm.sh` now rejects shader compiler errors before copying a player back from the VM.

This first district has six delivery courts including home, rather than the previous region's 22 locations. Desktop is the current target; the original browser version remains the mobile/iPad route.
