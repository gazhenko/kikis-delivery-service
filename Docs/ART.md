# Art sources and current limits

## Shipped project assets

| Asset | Source | Role |
| --- | --- | --- |
| `Assets/Koriko/Art/PaintedFilmSurfaces.png` | Built-in image generation, 2026-09-27; [exact prompt](references/painted-film-atlas.md) | Current, quieter poster-color surfaces and interface paper |
| `Assets/Koriko/Art/ShopPaintings.png` | Built-in image generation, 2026-09-27; [exact prompt](references/shop-paintings.md) | Painted interiors and signs mapped onto actual storefronts |
| `Assets/Koriko/Art/PaintedSurfaces.png` | Built-in image generation, 2026-09-27; prompt below | Sixteen related painted surface swatches, including paper for interface cards |
| `Assets/Koriko/Art/PaintedSky.png` | Built-in image generation, 2026-09-27; prompt below | Panoramic cloud painting with a horizon blended into distance fog |
| `Assets/Koriko/Art/KorikoNeighborhood.fbx` | Original project geometry, authored by `Tools/build_art.py` | Connected neighborhood, courts, gardens, quay and airship |
| `Assets/Koriko/Art/KikiAndJiji.fbx` | Original project geometry, authored by `Tools/character_model.py`, exported by `Tools/build_art.py` | Film-referenced Kiki, Jiji, broom and shoulder bag; articulated limbs and flight cloth |
| `art-source/*.blend` | Editable Blender 4.5.3 exports of the same source | Geometry and material editing |
| `ItemIcons.png` | Existing browser project's `public/art/film-inventory-atlas.png` | Illustrated inventory and HUD icons |
| `Portraits.png` | Existing browser project's `public/art/character-portraits.png` | Reserved for dialogue; currently unused |
| `GameIcon.png` | Existing browser project's `public/favicon.png` | Desktop application icon |
| `Art/Fonts/AlegreyaSans-*.ttf` | Google Fonts, Alegreya Sans; accompanying SIL OFL | Interface typography |

The project has permission from the user to use its existing generated assets and to depict Kiki, Jiji and film locations. This is a fan prototype, not evidence of a commercial license from Studio Ghibli. No film frames, film music or extracted commercial game assets are shipped.

Film gallery used as visual reference: https://www.ghibli.jp/works/majo/

Fonts: https://github.com/google/fonts/tree/main/ofl/alegreyasans

## Generated surface atlas

Method: built-in image generation. No post-generation pixel edits. Copied from `/Users/jemmygazhenko/.codex/generated_images/01a0dc1b-85f5-78c3-9a6b-e1c31de25ed3/exec-60066063-2dc8-4b15-8eef-f88be95f409a.png`.

The returned image is 1254 × 1254, despite a 2048 × 2048 request. It has sixteen occupied cells. Repeating materials use mirrored UV sampling, cell insets and color blending to reduce visible boundaries. Roof patterns and background strokes still need review at real gameplay distances.

Exact generation prompt:

> Use case: illustration-story. Asset type: production material texture atlas for a Unity 3D Kiki's Delivery Service fan game. Generate one square 2048 x 2048 opaque image, divided mathematically into exactly FOUR columns and FOUR rows of equal square texture swatches, no drawn borders or labels. Traditional hand-painted Japanese animated-film background painting in the style of Kiki's Delivery Service, gentle gouache brushwork, soft age and color modulation, restrained detail, no dots, no grain, no stippling. Each cell is a FLAT FRONT-ON SURFACE that fills every pixel of its cell and can tile, no perspective, no scene, no objects, no lighting gradients, no directional baked shadows. Exact row-major material order: ROW1 warm ivory lime plaster, pale dusty rose plaster, faded honey ochre plaster, muted seafoam green plaster. ROW2 irregular terracotta roof tiles small even horizontal rows, desaturated blue slate roof tiles small even horizontal rows, warm weathered vertical timber planks, cool sandy limestone small dressed blocks. ROW3 warm gray cobblestone paving, sunlit mossy meadow grass painted in broad soft strokes, dark leafy green canopy surface painterly organic overlapping leaves, warm dry garden soil. ROW4 antique cream paper with extremely faint broad watercolor wash no grain, faded navy cloth, red vermilion cotton cloth, pale golden straw thin soft lengthwise strokes. Each swatch MUST cover exactly its quarter-width and quarter-height rectangular cell, clean straight divisions at 25%,50%,75%; no gutters, no margins, no objects placed on the materials, no text, no lettering, no logos, no dark outlines, no checkerboard transparency. Palette warm and restrained; surfaces should look painted by the same background artist and read as quiet textures at a distance. Reduce micro-detail, keep brush marks broad and organic. Complete 16 materials, opaque RGB image.

## Generated sky panorama

Method: built-in image generation. Inspected the generated 1774 × 887 image and copied it without pixel edits from `/Users/jemmygazhenko/.codex/generated_images/01a0dc1b-85f5-78c3-9a6b-e1c31de25ed3/exec-907a1a91-40b3-45de-a554-e4036c63b614.png`. The sky shader wraps it horizontally, darkens it through the continuous night cycle, and blends the horizon into the scene's atmospheric color. No film image is embedded.

Exact generation prompt:

> Use case: illustration-story. Asset type: production skybox background texture for a three-dimensional Kiki's Delivery Service fan game, not a concept scene. Create one very wide 2:1 panoramic 2048 by 1024 opaque sky painting. Only SKY and CLOUDS, no land, water, buildings, characters, sun disk, moon, text or border. Traditional hand-painted Japanese animated-film background painting, evoking Kiki's Delivery Service: luminous early-summer blue-green sky, warm ivory cumulus with soft lavender-blue undersides, broad gouache strokes, quiet watercolor transitions. The painting is a full spherical equirectangular panorama: the equatorial horizon is exactly halfway down the image; keep the LOWER HALF nearly empty very pale blue-green haze, continuous around the sphere. Place a few lovely asymmetrical cloud banks mostly between 15% and 42% down the image, with plenty of clear blue sky between them and uncluttered zenith at the top. Match the exact colors at left and right edges so the panorama wraps horizontally; keep both side edges simple sky with no cloud cut in half. Avoid a hard horizon line. Soft depth and dimensional, hand-shaped clouds; not photographic, not vector clipart, not airbrushed plastic, no noisy texture, no dot pattern, no grain, no stippling, no outlines. Calm, spacious and optimistic. The entire image is an original painted sky texture; no typography or labels.

## Material and model direction

The environment shares a restricted palette and painted surface atlas. The [film pass](CEL_ART_PASS.md) introduced a separate character cel shader with authored lit/shadow colors and selective pixel-sized ink contours. The subsequent [character study](CHARACTER.md) rebuilt the face, eyes, continuous bob and fringe, ribbon, bow loops, loose smock, articulated limbs, red flats, bag and broom against specific official film stills. Face and hair normals keep cel shadows broad. Glossy rim bands are disabled; the palette uses dark navy cloth, almost-black hair, red ribbon and warm skin.

The rig uses named transform pivots and a flight cloth blend shape. A two-joint arm solver keeps both hands on the broom while the torso leans, and the dress and drawn folds move over the bent legs. Ink fades at overlapping shoulder joins to avoid unwanted internal hatch marks. Blink, bow, head and leg poses are held at 12 Hz while controls and camera remain smooth. The editable source is original geometry; no film image is applied to the character. Blender previews in `previews/` are source-art previews. Native Unity captures are kept separately under `verification/`.

The latest layout extends terrain beneath the distant trees and hills, connects the northern roads, and adds a park loop, orchard fencing and a small pasture with two cows. These are original project geometry, not canonical film landmarks. A painted cloud panorama now fills the sky, and night lighting warms the windows.

The film pass adds gently shaped roof edges and dormers, a bakery gable, painted shop windows, illustrated location signs, balcony rails, and petal-shaped flowers. The Blender sources retain these details as editable geometry. Background materials now combine broad texture painting with authored vertex tones; character materials use explicitly chosen cel colors.

Remaining art work includes a character expression set and authored movement/landing animation, distinctive film-location silhouettes, richer back gardens and ambient sound. The town layout is an authored interpretation rather than a canonical film map. Repeated frontages and simple foliage still read as procedural; further environment work needs bespoke forms and placement beyond changing the renderer.
