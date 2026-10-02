# The film look: cel shading and drawing on twos

October 2, 2026. The goal for this pass: the game should look and feel like the film, with the animation carrying the nuances of a Studio Ghibli picture. The town stays a painted background and the characters are cels, as in the film itself, and the whole image is drawn the way the film is drawn.

## What a Ghibli frame does

From the official trailer and the still gallery, studied as references only:

- **Backgrounds are paintings.** A sunlit wall is one flat wash, its shaded side another, and the edge between them is a brush stroke, not a gradient. Cast shadows are hard shapes in a cooler, bluer colour. Distant hills dissolve into the haze. There are no specular highlights anywhere.
- **Characters are cels.** Two tones per paint with a hard edge, a lit face, coloured trace lines, and highlights in the eyes.
- **Animation is on twos.** A new drawing every two frames of 24, with ones for fast action. Hair, cloth and anything loose follows a beat behind the body. Nothing slides: feet are planted exactly.
- **Camera moves are calm.** Pans and follows are smooth and the horizon stays level, while the drawings step.

## What changed

### Painted backgrounds (`Koriko/Painted`)
- Three flat washes: sunlit, half-tone and shadow, with values 1, 0.42 and 0. The band edge is pushed about by the brush marks of the painted atlas, so the boundary wobbles like a stroke instead of following the geometry.
- Cast shadows are hard-edged painted shapes (a narrow step on the shadow attenuation), and shadowed paint on upward faces takes the sky's blue.
- A `DepthNormals` pass on the painted, character and sea shaders, so the painter's line can find creases.

### The painter's line and film grade (`Koriko/FilmEdges`)
A full-screen pass runs once over the finished opaque town, before smoke, lamplight and the HUD (URP's `FullScreenPassRendererFeature`, injected before transparents, with depth and normals). It draws:
- a silhouette line where depth breaks, with a threshold that grows with distance so far roofs are not scribbled over;
- a crease line where surface normals turn sharply (roof edges, corners, lamp posts);
- in a warm dark ink that multiplies the paint beneath it, thins into the distance, fades against open sky and turns bluer at night.

The same pass grades the frame like film stock: a touch of warmth in daylight, slightly richer colour and lifted blacks.

### Drawing on twos (`RiderPerformance`)
- A drawing clock at 24 slots a second exposes Kiki's pose on even slots: twelve drawings a second. The root, the camera and the town move smoothly every frame.
- Fast actions go on ones: the push-off, a hard landing, braking, a smear, a sharp bank, the hand-over, strong acceleration.
- Between drawings every `Pose`, `Offset` and `Shape` call is a no-op, so the drawn pose holds. Per-frame fits that build on a drawing (the pelvis support over planted feet, the broom tip on the ground) restore the drawing's position first, so they never accumulate across a hold.
- Planted feet stay exact every frame: the stance leg is solved to its world-space plant each frame, while the swing foot's arc is held between drawings. A foot always lands at the full step, whatever drawing the swing was holding; planting short left the stance leg overreached as the body walked on.
- The free hand's target is chosen per drawing and carried in the body's frame between drawings. Hands on the broom are solved every frame, so the grip never opens.
- Blinks and the mouth change only on a drawing, which makes a blink three drawings long, as drawn blinks are.
- Hair, bow, hem, satchel and Jiji keep their own 12 Hz clock, aligned to the body's drawings.

### Small acting
- Jiji's tail curls and uncurls at rest and one ear flicks every seven seconds or so.

### Release players
Players are now built without the development flag, so there is no "Development Build" watermark. The opt-in `--koriko-*` checks still run in release players. `KORIKO_DEV_BUILD=1` keeps a development player for profiling.

## Verification
See [verification/film-look/README.md](verification/film-look/README.md) for the native captures, the drawing-rate evidence and the full check results on macOS and Linux.
