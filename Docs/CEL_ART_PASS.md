# Painted film pass — 2026-09-27

The target is a moving Kiki-inspired animation background with a drawn character in front of it. The existing delivery simulation, connected streets, destination coordinates and controls stay in place while the art is revised.

## Research translated into implementation

| Primary source | Useful observation | Application here |
| --- | --- | --- |
| [Hi-Fi RUSH: developer interview](https://www.unrealengine.com/developer-interviews/hi-fi-rush-was-inspired-by-shaun-of-the-dead-and-futurama) and [Tango's GDC rendering talk](https://gdcvault.com/play/1034330/3D-Toon-Rendering-in-Hi) | The team established a 2D target, controlled light/shadow shapes across the whole world, and treated performance as part of the art constraints. | Deliberate cel and painted-background material profiles; quiet surface detail; controlled shadow colors. No full-screen posterization filter. |
| [Nintendo: Wind Waker HD, Archeology](https://iwataasks.nintendo.com/interviews/wiiu/wind-waker/0/3/) | Nintendo describes preserving the original art's intent through shader interpretation and individual corrections. Link's design retained its impact under shared test shading; sunlight and contrast supported the look. | Improve the rider silhouette, expressive bow and facial marks, then use a restrained palette and graphic sea shapes. These are our implementation choices, not a claim to reproduce Nintendo's renderer. |
| [Shapefarm: Orbitals rendering](https://www.unrealengine.com/tech-blog/stepping-inside-a-retro-anime-inspired-game-a-look-into-the-rendering-of-orbitals) | The developer separates stepped, explicitly colored cel objects from painted environments, uses authored vertex information and brush textures, and keeps camera motion independent of stepped animation. | Dedicated character cel shader, pixel-sized selective ink, painted vertex tones, sampled character poses with smooth flying/camera motion. Preserve the user's preference against grain and dot patterns. |
| [Arc System Works: Guilty Gear Xrd art talk](https://www.ggxrd.com/Motomura_Junya_GuiltyGearXrd.pdf) | Character modeling, shading, rigging and look development are designed together; edited vertex normals remove unwanted shading on major features. | Simplified authored cheek normals and internal drawn seam shapes on the character. |

These games are references for production decisions, not sources of ripped assets or code. Unity URP can implement the needed material and mesh techniques; another engine migration would not supply the missing art.

## Acceptance

Compare native screenshots against the previous prototype at the same bakery, garden and airship viewpoints. Inspect Kiki from behind and in three-quarter views. Check daylight, sunset and night. Re-run the delivery circuit and bakery interactions after the art export, measure rendering cost on the same Mac, and keep the evidence under `Docs/verification/film-pass/`.

The pass must improve silhouette, color grouping, surface painting and motion together. It is not a claim of feature-film production quality or a finished animation rig.

## Implemented art controls

- `Cel.shader` (`Koriko/CharacterCel`): per-material lit/shadow/accent colors, antialiased two-tone transitions, simple face lighting and a separate foreground key. This is a compact forward URP implementation, not Hi-Fi RUSH's deferred renderer or face SDF system.
- `Ink.shader`: screen-pixel contour width with authored vertex-alpha taper. Contours are limited to selected character forms, so background texture detail is not outlined indiscriminately.
- `Painted.shader` and the [new atlas](references/painted-film-atlas.md): large paint marks, authored vertex tones, softer colored background shadows and simplified foliage normals. Atlas cells are inset to reduce edge bleed.
- `PaintedSea.shader`: flat ocean color groups and broken, curved wave strokes sampled at 12 Hz.
- Source geometry: reshaped bow and bangs, cheek normals, drawn facial/cloth marks, smaller temple hair, rounded bow corners, and better-separated Jiji. Building and roof shapes receive a shared gentle deformation so attached trim stays aligned. Bakery gable, shopfronts, balcony rails and facade-mounted illustrations give streets specific details.
- [Shop paintings](references/shop-paintings.md): authored interiors behind actual modeled frames, with bread, books, tea and flowers; illustrated bakery, workshop and postal signs. These are texture-mapped surfaces belonging to the buildings, not floating scenery sprites.
- Motion in this original rendering pass: complete character poses sampled at 12 Hz, including blink/turn/lean accents. The subsequent [flight performance pass](MOTION.md) replaces that rigid shared timing with action accents, overlapping motion and brief mesh smear drawings. Flight translation, steering and camera remain responsive. Canopy movement retains the original sampled timing.
- Presentation: quieter paper panels, a compact landing prompt below the rider, and a parcel card that appears only while carrying a delivery. Notices sit at the top, leaving the broom and character visible. The minimap remains available throughout flight.
- Sky: corrected the wrapped panorama's texture derivatives to avoid a bright mip-filtering seam above the sea.

No grain, halftone, chromatic-aberration or full-screen posterization effect was added. The continuous time, deliveries, recipes, purchases and destinations are preserved.
