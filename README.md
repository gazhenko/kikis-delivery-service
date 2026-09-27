# Kiki's Delivery Service — desktop prototype

A Unity 6.3 LTS / URP first neighborhood for the approved desktop, third-person rebuild. The previous browser game remains in `../kikis-delivery-service`.

## Playable intent

Depart Osono's bakery, fly along the shopping street to the clock square, reveal the harbor, follow the quay, climb toward Madame's garden and return home. Tombo's workshop and the airship add short and elevated delivery approaches. The first sightseeing circuit is designed for roughly 60–90 seconds; this timing still requires a real playtest.

Timed jobs, ingredient shopping, recipes, broom upgrades, crows, cozy/challenging modes, hunger, energy, hospital returns and local saves are implemented in source. Days and nights each last 150 simulation seconds; sleeping advances eight game hours. Menus do not end a turn or stop the delivery clock.

## Current status

The Unity VM is licensed, the project compiles, and a universal Mac development build runs on Apple M1 Pro / Metal. The latest character pass rebuilds Kiki's face, bob, ribbon, bow, dress, limbs, shoes, bag and broom against film references. It keeps the cel palette and held poses, adds hands that stay on the broom and cloth that follows seated flight, and corrects the visible landing surfaces so the shoes meet the ground.

The final Mac build passed six focused character checks and all 23 delivery-route checks at 1920 × 1080. Eight character study views and eight world views cover standing, cruising, noon, sunset and night. The route averaged 18.80 ms per frame (about 53 fps); this includes captures and menus, not a clean performance benchmark. The 23 route assertions include the previous 18 gameplay checks plus shoe contact at five landing courts. The earlier prototype passed at 1440 × 900, and the unchanged independent core simulation has passed 25 checks with .NET 8.

The model has been substantially refined; facial expressions and flight/landing animation remain simplified. Repeated architecture, foliage, ambient sound, release performance profiling and a physical-controller playtest remain work for the desktop rebuild.

See the [character references and source](Docs/CHARACTER.md), [latest screenshots and verification](Docs/verification/character-pass/README.md), [earlier rendering research](Docs/CEL_ART_PASS.md), [baseline verification](Docs/VERIFICATION.md), [visual direction](Docs/DIRECTION.md), [asset provenance](Docs/ART.md) and [development VM](Docs/VM.md).

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

A development player accepts `--koriko-flight-check`. It creates synthetic keyboard and gamepad events to fly a delivery circuit with the real movement code and camera, checks bakery interactions and visible shoe contact, captures native screenshots, and writes `flight-check/result.txt` under Unity's persistent data directory. It exits nonzero on a failed check and never loads or writes the player's save. This runner has passed on the Mac; it complements a human/controller playtest. The latest results and captures are in `Docs/verification/character-pass/flight/`; earlier runs remain under `Docs/verification/film-pass/` and `Docs/verification/1920x1080/`.

Use `--koriko-art-check` for eight repeatable art viewpoints, without gameplay assertions. This writes `art-check/` in the same persistent-data directory and also avoids player saves. `Tools/build-on-vm.sh` now rejects shader compiler errors before copying a player back from the VM.

Use `--koriko-character-check` for eight native model viewpoints and six rig assertions, including a real takeoff and cruise above 20 m/s. It writes `character-check/` and also avoids player saves. The neutral study backgrounds make face, silhouette, cloth and hand contact easier to review independently of the town.

This first district has six delivery courts including home, rather than the previous region's 22 locations. Desktop is the current target; the original browser version remains the mobile/iPad route.
