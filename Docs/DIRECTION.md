# Koriko — first desktop neighborhood

Approved direction, 2026-09-27: downloadable desktop, a camera behind Kiki, painted backgrounds and drawn-looking characters. Unity 6.3 LTS / URP. The previous Three.js game remains at `../kikis-delivery-service`.

## Experience

Depart Osono's bakery courtyard, pass between connected shopfronts, climb over the market roofline, reveal the harbor and clock tower, follow the quay, turn inland through a garden lane, land at Madame's house, and return to the bakery. A first circuit should take roughly 60–90 seconds at sightseeing pace. Landings use clear, ground-level courts with generous space to brake. The airship is an elevated destination unlocked later.

The prototype is a deliberately limited district, not a replacement claim for the previous region's 22 locations. Distant roofs, wooded hills and boats extend the horizon. The route is an authored interpretation of the film's geography, not a canonical street map.

## Art rules

- All structures belong to a frontage, courtyard or quay. Shared sidewalks and walls connect each assembly.
- Paint variation into surfaces; avoid screen-space grain, halftone dots, random decoration and photorealistic gloss.
- Architecture carries softened painted lighting; characters have clearer cel shadow shapes and selective outlines.
- Warm cream, faded rose and ochre plaster; terracotta/slate roofs; blue-green sea; warm dark timber; navy ink.
- Foliage is grouped into deliberate canopy masses, garden beds, orchard rows and hedges. Quiet paths and landing courts stay clear.
- Camera follows smoothly with a stable horizon. Character pose timing may be stylized independently of responsive controls.
- UI retains functional text, illustrated item icons and a persistent minimap. No marketing slogans during play.

## Preserved mechanics

Timed deliveries and tips; 150-second days and 150-second nights; sleep advances eight in-game hours; money; hunger/energy; hospital return and money loss; ingredient shopping; six recipes and temporary buffs; six upgrade tracks and delivery unlocks; crows; cozy/challenging modes; keyboard/controller controls; local saves. Desktop movement adds vertical steering and actual landing checks.

## Reference sources

- Official film gallery: https://www.ghibli.jp/works/majo/ — town density, architectural color, background/character separation.
- Europa creator: https://www.helderpinto.com/projects/Qn1ZR3 — flight vistas and landscape composition.
- Ni no Kuni: https://www.bandainamcoent.com/games/ni-no-kuni-wrath-of-the-white-witch — compatibility of animated characters and painted scenery.
- Arc System Works artist talk: https://www.ggxrd.com/Motomura_Junya_GuiltyGearXrd.pdf — authored normals, shadows and pose design.
- Spiritfarer animators: https://www.toonboom.com/thunder-lotus-games-on-animating-the-afterlife-in-spiritfarer — personality in everyday actions.

Film frames are references, not shipped textures. Asset provenance and generation prompts are recorded in `ART.md`.

## Acceptance checks

Run actual Unity scene and native player: start a cozy game; take a job; follow rooflines to the destination; brake, descend and deliver; return; shop, cook and sleep; verify eight-hour skip; save/reload; try controller and challenging mode. Inspect all three destination approaches from cruise and landing heights. Inspect daylight, sunset and night. Record frame time and draw calls on named hardware. Targets are 60 fps at 1080p desktop; these are targets until measured.

Automated logic, source or asset checks do not establish real-time Unity appearance or performance. Keep verification status explicit in `VERIFICATION.md`.
