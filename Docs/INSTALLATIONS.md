# Desktop installations: October 2, 2026 (the film look, v1.0.0)

Both machines now run **`20261002-2175792`** (source `2175792`), the first release player: no development watermark, painted washes and the painter's line in the town, and Kiki drawn on twos ([design](FILM_LOOK.md), [record](verification/film-look/README.md)). The same source is published on GitHub as v1.0.0 with macOS, Linux and Windows archives.

- **Mac:** the candidate passed 17 walking, 46 control, 6 character, 25 route, 10 motion and 33 tour checks (at 720p and 1080p) before the swap; the Desktop shortcut passed the tour afterwards. Rollback: `Builds/mac/previous/Kiki-Delivery-20260930-d88266f-Mac.zip` (hash verified).
- **Omarchy:** the staged release passed 10 motion, 33 tour, 46 control, 17 walking and 25 route checks; all 275 hashes verified before `current` switched; the installed launcher passed the tour. Rollback: `releases/20260930-d88266f` and earlier.
- Saves were untouched.

---

# Desktop installations: September 30, 2026 (film character pass)

Both machines received **`20260930-d88266f`** (source `d88266f6ed0844c99bd6b0c0c422f58ecb85fa76`), a dedicated pass on Kiki and Jiji toward the film's cel look and motion ([record](verification/character-film-pass/README.md)).

Checks:
- **Mac:** 33 tour and 6 character checks through the Desktop shortcut after installation. The staged candidate also passed 46 control, 17 walking and 25 route checks.
- **Omarchy:** the staged release passed 10 motion, 33 tour, 46 control, 17 walking and 25 route checks. All 275 hashes verified before `current` switched, and the installed launcher passed the tour.

Rollback:
- **Mac:** `Builds/mac/previous/Kiki-Delivery-20260930-9f5ea8f-Mac.zip` (hash verified).
- **Omarchy:** `releases/20260930-9f5ea8f` and earlier.

Because the Mac was nearly full, its three older archives and a stale motion-check output were moved to Omarchy's `~/Games/KikiDelivery/archive/` with hash verification. Saves were untouched.

---

# Desktop installations: September 30, 2026 (follow-up)

Both machines received **`20260930-9f5ea8f`** (source `9f5ea8f568f6e96488c0fbc200824a9f1647a0f6`). It fixes three defects visible in a photo of the Omarchy session:
- Jiji merged with Kiki's hair from behind; he now has his own blue-black paint and turns slightly outward on her shoulder.
- Her free arm locked straight behind her while walking.
- Roof eaves were open to the sky from street level.

The Mac candidate passed 33 tour, 46 control, 17 walking, 25 route and 6 character checks, plus 12 environment views, before the swap. After it, the tour passed again through the Desktop shortcut. The Omarchy staged release passed 33 tour, 46 control, 17 walking, 25 route and 10 motion checks. All 275 hashes verified before `current` switched, and the installed launcher passed the tour. The [follow-up record](verification/polish-pass/follow-up/README.md) has the evidence.

Rollback:
- **Mac:** `Builds/mac/previous/Kiki-Delivery-20260930-9679e3a-Mac.zip`; its hash was verified before the move. The September 28 and 29 archives are also kept.
- **Omarchy:** `releases/20260930-9679e3a`, `20260929-49ba433` and `20260928-b093c2f` remain.

Saves were untouched.

---

# Polish release 20260930-9679e3a: September 30, 2026

Earlier the same day, both installations received the polish-pass release **`20260930-9679e3a`**. It was built from source commit `9679e3a8c8247e9c81bfca50dbb434537bef162e` with Unity 6000.3.20f1 and adds sound, night light, town life, wayfinding and harbor polish ([design](POLISH_PASS.md), [verification](verification/polish-pass/README.md)). The earlier installation records remain below.

## Mac: current release

- Launch the existing Desktop shortcut, `~/Desktop/Kiki’s Delivery Service (Prototype).app`. It still resolves to `~/kikis-delivery-desktop/Builds/mac/Kiki’s Delivery Service.app`, which now holds the new universal Intel/Apple Silicon development player.
- The candidate was built with `KORIKO_MAC_DESTINATION=Builds/mac-candidate bash Tools/build-on-vm.sh` and verified before the swap:
  - 33 tour checks;
  - 46 control checks;
  - 17 walking checks;
  - 25 route checks;
  - 6 character checks;
  - 12 environment views.
- It then replaced the installed app at the same path. After the swap, `codesign --verify --deep --strict` passed and the tour passed 33 checks again through the Desktop shortcut.
- `Builds/mac/Kiki-Delivery-Mac.zip` was regenerated (146 MB) and passed ZIP integrity testing. See the [SHA-256 and metadata](verification/polish-pass/mac-release.json).
- **Rollback:** the previous release's archive, `Builds/mac/previous/Kiki-Delivery-20260929-Mac.zip`, matched its recorded SHA-256 (`35686ad1…`) and passed integrity testing before it was moved. The September 28 archive also remains in `previous/`.
- This is still a local development build and has not been notarized.

## Omarchy: current release

- Open **Kiki’s Delivery Service** in the application launcher or run `~/.local/bin/kiki-delivery`. `~/Games/KikiDelivery/current` now resolves to `releases/20260930-9679e3a`.
- The export was staged as a new release directory, where all **275 files** matched their [SHA-256 manifest](verification/polish-pass/omarchy-file-hashes.json). No libraries were unresolved and the desktop entry validated.
- The staged player then passed:
  - 33 tour checks;
  - 46 control checks;
  - 17 walking checks;
  - 25 route checks;
  - 10 airborne-motion checks.
- The hashes were verified again before `current` was switched atomically. After the switch, the installed launcher passed all 33 tour checks at 1280 × 720 on an NVIDIA RTX 3080 with OpenGLCore. [Release metadata](verification/polish-pass/omarchy-release.json).
- **Rollback:** `releases/20260929-49ba433` and `releases/20260928-b093c2f` remain. Pointing `current` back at one restores it.
- QA runs float and size only their own window. On Hyprland 0.56 this uses the Lua dispatchers `hl.dsp.window.float`, `resize` and `move`.

## Saves

Player saves and preferences live outside the release folders and were preserved. The Mac save is in `~/Library/Application Support/com.Koriko.Kiki---s-Delivery-Service/`; the Omarchy save is in `~/.config/unity3d/Koriko/Kiki’s Delivery Service/`. Read-only copies of both [restore under the new release's rules](verification/polish-pass/save-compatibility.txt). Choosing *Start a new game…* in the game copies the current save to `koriko-desktop-v1.before-new-game.json` first. Synthetic input tests and measured audio do not establish physical-controller compatibility, flight feel or how the sound is heard.

---

# Walking and controls release: September 29, 2026

The Mac and Omarchy installations now contain the walking, broom-carry and controls release **`20260929-49ba433`**, built from source commit `49ba433a4e87e7a482784ee309da0df0f4af7b32` with Unity 6000.3.20f1. The older environment-only installation record is retained below.

## Mac — current release

- Launch the existing desktop shortcut: `~/Desktop/Kiki’s Delivery Service (Prototype).app`.
- Its target is `~/kikis-delivery-desktop/Builds/mac/Kiki’s Delivery Service.app`, a universal Intel/Apple Silicon development player.
- The rebuilt app passed `codesign --verify --deep --strict` and native **45-control, 17-walking and 25-delivery-route checks**, all at 1280 × 720 on Apple M1 Pro / Metal. [Results](verification/walking-controls/README.md).
- `Builds/mac/Kiki-Delivery-Mac.zip` was regenerated (138 MiB) and passed ZIP integrity validation. The prior archive remains at `Builds/mac/previous/Kiki-Delivery-20260928-Mac.zip` for rollback. [Release metadata and archive SHA-256](verification/walking-controls/mac-release.json).
- The app remains a local development build, not a notarized public distribution.

## Omarchy — current release

- Open **Kiki’s Delivery Service** in the application launcher, or run `~/.local/bin/kiki-delivery`.
- `~/Games/KikiDelivery/current` now resolves to `releases/20260929-49ba433`.
- All **276 release files** matched their SHA-256 manifest before the symlink was switched atomically. Executable/UnityPlayer dependencies resolved and the desktop entry validated. [Release metadata](verification/walking-controls/omarchy-release.json), [file hashes](verification/walking-controls/omarchy-file-hashes.json).
- The native candidate passed **45 control checks, 25 route checks and 10 airborne-motion checks** at 1280 × 720 on NVIDIA RTX 3080 / OpenGLCore. The walking reel passed 17 checks with the same character implementation; the installed launcher additionally passed all **17 final walking checks**, exiting 0. [Installed-launcher result](verification/walking-controls/omarchy-installed-walk/result.txt). [Verification](verification/walking-controls/README.md).
- The previous `releases/20260928-b093c2f` remains available for rollback. The application-menu icon and command still resolve through `current`.

Player saves live outside the releases and were preserved. Native QA starts fresh temporary simulations and never reads or writes those saves or control preferences. Synthetic input tests do not establish physical-controller compatibility or human flight feel.

---

# Previous environment release — 2026-09-28

Both computers have the environment-art revision `b093c2f9221abe7b2ce53e6964c7ddfeca3e5d96`, exported with Unity 6000.3.20f1. Installation work continued into 2026-09-29 UTC (2026-09-28 local time).

## Mac

- Launch: `~/Desktop/Kiki’s Delivery Service (Prototype).app`.
- That shortcut resolves to `~/kikis-delivery-desktop/Builds/mac/Kiki’s Delivery Service.app`; it already pointed to the latest environment build, so no additional copy was needed.
- Universal Intel/Apple Silicon development app. `codesign --verify --deep --strict` passed.
- Launched the resolved desktop shortcut with `--koriko-environment-check`: exit 0, twelve views, all scene shaders supported, Apple M1 Pro / Metal, 1280 × 720.
- [Fresh installation check](verification/installations-2026-09-28/mac-environment-result.txt). The existing [25-check route result](verification/environment-pass/flight/result.txt) belongs to this same environment build; that longer suite was not repeated on the Mac during installation verification.

## Omarchy

No Kiki installation or launcher was found in the standard application, game, project or Steam directories. A native Linux x86-64 build was exported on the licensed Unity VM and installed over the existing SSH connection.

- Application-menu entry: **Kiki’s Delivery Service**, with the existing Jiji game icon.
- Command: `~/.local/bin/kiki-delivery`.
- Desktop entry: `~/.local/share/applications/kiki-delivery.desktop`.
- Current installation: `~/Games/KikiDelivery/current`.
- Release directory: `~/Games/KikiDelivery/releases/20260928-b093c2f`.
- Save location: `~/.config/unity3d/Koriko/Kiki’s Delivery Service/` (separate from the installation).
- `release.json` records the source revision; `file-hashes.json` contains SHA-256 hashes for all 276 release files. All transferred files matched. The executable and UnityPlayer dependencies resolved, and the desktop entry passed `desktop-file-validate`.

The installed launcher ran the native environment check successfully using NVIDIA GeForce RTX 3080 / OpenGLCore. Twelve views were captured; all scene shaders were supported. Hyprland tiled the requested 1280 × 720 window to an actual rendering size of **1896 × 1030**. The bakery HUD and harbor capture were visually inspected. [Environment result and captures](verification/installations-2026-09-28/omarchy-environment/).

The same launcher then passed **all 25 delivery-route checks**, covering all six landing courts, delivery interaction, eight-hour sleep, ingredient purchases, cooking and coffee, continuous kitchen time, controller menu navigation, broom upgrades, flight input and save serialization. It exited 0. Mean frame time across the automated route was **16.99 ms (about 59 fps)**, including screenshots and menu interactions; this is not a controlled benchmark. [Route result](verification/installations-2026-09-28/omarchy-flight/result.txt).

Verification uses opt-in development checks that start a separate temporary simulation and never load or write the player's saved game. Automated input is not a physical-controller or human playtest.

## Future exports and updates

`bash Tools/build-on-vm.sh` exports the Mac player; `bash Tools/build-linux-on-vm.sh` exports Linux. Both reject shader compiler errors. Do not run a batch export while the VM editor has this project open.

Linux exports land in local `Builds/linux/`; the build script does not install them remotely. Stage a new versioned directory on Omarchy, verify its file hashes and dependencies, then atomically switch the `current` symlink after validation. Keep the prior release for rollback. The launcher and icon resolve through `current`, and saves remain outside all release directories.
