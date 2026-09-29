# Desktop installations — 2026-09-29

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
