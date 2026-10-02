# Credits

Kiki's Delivery Service (desktop) is an unofficial fan project. The characters, the title and
the world of Koriko belong to Studio Ghibli and to Eiko Kadono's novel. The 1989 film and its
official still gallery (https://www.ghibli.jp/works/majo/) and the official trailer were studied
as visual references only. **No film frames, production drawings, meshes, music or sound from the
film are included in the game or the repository.**

Everything below that is not listed as third-party is original work made for this project.

## Original work

| What | Source in this repository |
| --- | --- |
| Kiki, Jiji, the broom and the satchel: meshes, rig, morphs, cel paint and ink | `Tools/character_model.py`, exported by `Tools/build_art.py` to `Assets/Koriko/Art/KikiAndJiji.fbx` and `art-source/KikiAndJiji.blend` |
| The town of Koriko: streets, houses, gardens, quay, boats, headlands, lighthouse, islands and hill town | `Tools/build_art.py` and `Tools/environment_art.py`, exported to `Assets/Koriko/Art/KorikoNeighborhood.fbx` and `art-source/KorikoNeighborhood.blend` |
| Shaders: painted backgrounds, character cel, coloured ink, painter's line and film grade, sky, sea, lamplight, smoke and the court ring | `Assets/Koriko/Shaders/` |
| Flight, walking and delivery acting; smoke, gulls, lamps and boats; the HUD and its drawn marks | `Assets/Koriko/Runtime/` |
| Every sound and the music (wind, sea, birds, gulls, crows, footsteps, broom, bells, interface and an original waltz) | synthesized at startup by `Assets/Koriko/Runtime/Soundscape.cs`; no recordings or samples |
| Delivery rules, recipes, upgrades and saves | `Assets/Koriko/Core/` |

## Generated bitmap art

These images were produced with an image-generation model from written prompts, then used as
painted surface atlases. The prompts and provenance are recorded in `Docs/ART.md` and
`Docs/ENVIRONMENT.md`; no photographs or film images were used as inputs.

- `Assets/Koriko/Art/EnvironmentSurfaces-v2.png`: plaster, roof tile, timber, stone, cobble, meadow, foliage, soil, brick, copper and cloth cells
- `Assets/Koriko/Art/PaintedFilmSurfaces.png` and `PaintedSurfaces.png`: paper, cloth and straw cells, including the interface paper
- `Assets/Koriko/Art/ShopPaintings.png`: painted shop interiors and signs
- `Assets/Koriko/Art/PaintedSky.png`: the cloud panorama

`Assets/Koriko/Art/ItemIcons.png`, `Portraits.png` and `GameIcon.png` come from the author's
earlier browser version of this game (`../kikis-delivery-service`).

## Third-party

| Asset | License | Use |
| --- | --- | --- |
| Alegreya Sans (Regular, Bold) by Juan Pablo del Peral, Huerta Tipográfica. `Assets/Koriko/Art/Fonts/` | SIL Open Font License 1.1 (`Assets/Koriko/Art/Fonts/OFL.txt`) | All interface text and the logo |
| Unity 6 and its Universal Render Pipeline, Cinemachine and Input System packages | Unity Terms of Service | Engine |

## Tools used in development

Blender 4.5.3 (model generation and previews), ffmpeg (clip and trailer encoding), ImageMagick
(logo composition) and a licensed Unity 6000.3.20f1 editor on a Linux build VM.
