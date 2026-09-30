# Follow-up release 20260930-9f5ea8f: Jiji, the walking arm and roof eaves

September 30, 2026, after the user played release `20260930-9679e3a` on Omarchy's 34" 3440 × 1440 display. The session started a new game: the previous save was backed up to `koriko-desktop-v1.before-new-game.json` first. The player made two deliveries in about two minutes and quit cleanly; the player log shows no errors. A photo of the screen, taken while walking home from the harbor, showed three defects. Source commit: `9f5ea8f568f6e96488c0fbc200824a9f1647a0f6`.

| Defect in the photo | Cause | Fix |
| --- | --- | --- |
| Seen from behind, Jiji on Kiki's shoulder merged with her hair into one black mass that read like long hair or a cape | Jiji was modeled with Kiki's `Ink` material (the near-black of her pupils and line work), next to her near-black hair | Jiji gets his own blue-black cel paint with a cool sheen: darker than her dress, cooler than her hair and outlined in ink. His pupils and mouth stay ink. On her shoulder he turns 22° outward so his ears and profile read from behind. |
| The free arm stuck out stiffly behind her while walking | A symmetric ±0.14 m swing at near full reach straightened the elbow on the back swing | The arm swings more forward than back, rises slightly on the forward swing and keeps a soft elbow |
| A dark roof-edge line floated above a gable, with sky beneath it | Roof planes are single-sided, so from street level the eave underside was culled | The two roof materials draw both faces. Back faces shade with a flipped normal, so eaves read as shaded tile undersides. Shadow and depth passes use the same cull setting. |

[Before, from the 9679e3a tour](before-9679e3a-rear-shoulder.png). After:
- [rear shoulder study](03d-shoulder-jiji-rear.png);
- [walking frames from the gameplay camera](03b-walk-rear-a.png) and [a second one](03c-walk-rear-b.png);
- [street-level eaves on Metal](03e-street-eaves.png) and [on OpenGL](omarchy/03e-street-eaves.png);
- [Jiji on the broom](12-parcel-heavy.png) and [from the front](07b-delivery-wave.png).

The tour gained these rear and eaves views and still runs its 33 assertions.

## Checks on the installed release

| Suite | Mac: Apple M1 Pro / Metal | Omarchy: RTX 3080 / OpenGLCore |
| --- | --- | --- |
| Polish tour | [33 passed](mac/tour-result.txt), through the Desktop shortcut after installation | [33 passed](omarchy/tour-result.txt), through the installed `kiki-delivery` launcher |
| Controls | [46 passed](mac/controls-result.txt) | [46 passed](omarchy/controls-result.txt) |
| Walking | [17 passed](mac/walk-result.txt), contact errors 0.0000 m | [17 passed](omarchy/walk-result.txt) |
| Six-court route | [25 passed](mac/flight-result.txt), 16.90 ms/frame | [25 passed](omarchy/flight-result.txt), 16.88 ms/frame |
| Airborne motion | Not repeated | [10 passed](omarchy/motion-result.txt), identical metrics |
| Character study | [6 passed](mac/character-result.txt) | Not repeated |
| Environment views | [Captured](mac/environment-result.txt) | Not repeated |

The 27 core rules checks passed unchanged.

Peak recorded draw calls on the Mac route rose from 2,036 to 2,338. The two double-sided roof materials no longer share render state with the rest of the painted materials. Mean route frame time was unchanged: 16.88 ms before, 16.90 ms now. On Omarchy peak draw calls were 6,340 (previously 6,440). These are development-player route measurements, not benchmarks.

## Installation

- **Mac:** the candidate was verified before the swap. The `9679e3a` archive matched its recorded SHA-256 and passed integrity testing before moving to `Builds/mac/previous/Kiki-Delivery-20260930-9679e3a-Mac.zip`. The September 28 and 29 archives remain alongside it. The new app passed `codesign --verify --deep --strict`, and its archive passed integrity testing ([metadata](mac-release.json)).
- **Omarchy:** all [275 files](omarchy-file-hashes.json) matched their manifest after staging and again before `current` switched atomically to `releases/20260930-9f5ea8f` ([metadata](omarchy-release.json)). The `20260930-9679e3a`, `20260929-49ba433` and `20260928-b093c2f` releases remain for rollback.
- Both player saves were untouched by the checks: the Mac save is still dated September 27 and the Omarchy save is still the user's session from 13:27.
