# Kiki character study

## Film character pass: September 30, 2026

After play-testing, the user asked for a dedicated pass on Kiki and Jiji. The goal: a character that feels drawn straight from the film, with 2D cel shading in a 3D world, and anatomically correct motion that follows the film. The reference was [the official IMAX trailer](https://www.youtube.com/watch?v=e-ENiLJPwDQ). Its frames were studied privately and are not stored in the project or shipped.

From the trailer:
- **Palette.** Kiki's hair is auburn, not black. Her smock is a dark purple. Her skin is pale cream with pink blush. The satchel is orange-tan. Jiji is small and black, with big white eyes.
- **Proportions.** Kiki is about five heads tall. Her bow is roughly as wide as her head. Her smock falls loosely to below the knee, with sleeves to the elbow.
- **Faces.** They are drawn almost entirely in the lit tone. Hair casts little shadow on the face. The eyes are tall and near-black, with a heavy upper lash line and a highlight.
- **Line work.** Every shape has a coloured trace line: reddish-brown around skin, dark around hair.
- **Flight.** Seated astride, the smock lies over her lap and hangs down behind her over the broom, and only her flats show beneath it. Her arms reach forward to hold the handle with both hands.
- **Secondary motion.** Hair, bow and cloth are animated on twos.

The changes:
- **Model and paint** (`Tools/character_model.py`, regenerated with Blender 4.5.3; world files untouched):
  - the film palette;
  - a head scaled to 0.84 and a bow scaled to 0.8;
  - a below-the-knee smock, elbow sleeves and a soft flap satchel;
  - new eyes: tall iris, lash flick, lower lid mark and two highlights, with short high brows;
  - larger blush and a softer nose;
  - a shaded neck, cloth-coloured upper arms inside the sleeves, and jagged bob ends at the back;
  - a few curved smock folds;
  - Jiji rebuilt at film scale, with his own blue-black paint.
- **Cel shading** (`Cel.shader`, `SceneBuilder`, `Daylight`):
  - the character's key light sits high and to the side of the camera, so every form has a clear shadow shape;
  - faces keep the lit tone apart from a thin far-jaw shadow;
  - each paint has one chosen shadow colour, and marks such as eyes, blush and folds are flat;
  - the inside of the smock is painted in its shadow colour.
- **Ink** (`Ink.shader`, `SceneBuilder`): every body part has a colour-matched outline, sized in 1080p pixels (it scales with the window) and thinning with distance. Painted marks and joint spheres are skipped.
- **Motion** (`RiderPerformance`):
  - hair, bow, hem, satchel and Jiji update on twos (12 drawings a second), while body contact stays smooth;
  - the film's seated flight: thighs forward, shins trailing, feet pointed, and the smock draped over the lap and behind over the broom;
  - walking gait corrections: the pelvis shifts over the standing leg, and the free arm opposes the legs, back at its own side's heel strike;
  - the free hand rests on the satchel, and the broom is carried close with a bent elbow;
  - shin cloth contact keeps the longer smock clear of the walking legs.

[Native verification of this pass](verification/character-film-pass/README.md).


The subsequent [walking and controls pass](WALKING_CONTROLS.md) adds on-foot acting, the upright side carry, planted footsteps and transitions between walking and flight. Its [native walking reel and verification](verification/walking-controls/README.md) extend the historical results below.

The September 27 character revision uses the film itself as the design reference. The previous model's long lower face, pointed block fringe, spherical sleeves and rigid standing flight pose were the main likeness problems. This revision rebuilds the model, facial surfaces and articulation together while retaining the cel renderer.

## Reference views

The references were inspected from [Studio Ghibli's official film gallery](https://www.ghibli.jp/works/majo/). Stills are reference material only and are not included in the game or used as character textures.

| Film still | Features studied |
| --- | --- |
| [Bakery conversation](https://www.ghibli.jp/gallery/majo023.jpg) | Cheek and chin proportions, eye spacing, irregular swept fringe |
| [Shop profile](https://www.ghibli.jp/gallery/majo020.jpg) | Small nose profile, visible ear, red headband, soft bow loops |
| [Leaving home](https://www.ghibli.jp/gallery/majo009.jpg) | Bob from behind, shoulder line, bag and diagonal strap |
| [Broom flight](https://www.ghibli.jp/gallery/majo011.jpg) | Forward lean, bent knees, relaxed shoulders and broom grip |
| [Seated conversation](https://www.ghibli.jp/gallery/majo038.jpg) | Loose dress, elbow-length sleeves, red flats and simple cloth folds |

The references vary with expression, pose and lighting. This is an interpretation as a freely viewable 3D character, not a traced single frame.

## Source and rendering

`Tools/character_model.py` contains the editable surface profiles and mesh construction; `Tools/build_art.py -- --character-only` exports `art-source/KikiAndJiji.blend` and `Assets/Koriko/Art/KikiAndJiji.fbx` without rebuilding the town. Five Blender source previews cover front, profile, rear, three-quarter and portrait views. They are source previews, not native gameplay evidence.

The face has a continuous cheek/jaw/nose surface. Thin conforming meshes carry the eyes, blush and facial marks; the eyes are not protruding spheres. The full bob and swept fringe form one continuous surface, avoiding the exposed temples and overlapping slabs caught in early reviews. The ribbon and gathered bow loops have depth for side and rear views. Clothing uses a loose smock profile, open cuffs and a flight cloth blend shape that folds over the seated thighs. The drawn cloth folds deform with the dress.

Elbows and knees have named pivots and rounded joints. Two-joint arm solving keeps the hands attached to broom grip targets while the body leans. A salmon shoulder bag with fitted straps, low red flats with closed toes, bound broom straw and a refined Jiji complete the silhouette. On the ground, the visible soles are fitted to the actual floor height; this accounts for the controller settling into its collision skin. Original import anchors and gameplay collision dimensions remain intact.

Character materials have their own lit/shadow colors, with the hair and dress kept dark and free of shiny accent bands. Skin has controlled cheek normals and simplified cel lighting. Selective contours support the silhouette. The animated dress contour receives the same blend-shape weight as the cloth itself. The subsequent [motion pass](MOTION.md) replaces the uniform 12 Hz pose hold with takeoff, bank, boost, brake and landing performances, independent secondary motion, face-conforming gaze and brief smear drawings. Movement and camera controls remain responsive.

## Acceptance and verification

The opt-in development-player flag `--koriko-character-check` captures the actual imported model with native materials from five neutral-background viewpoints, then takes off using the real flight motor and captures three flying poses above 20 m/s. It checks hand-to-grip alignment within 2 cm, imported flight cloth, coordinated cloth/ink deformation and shader support. It never loads or writes the player's save. These isolated views are art studies, not gameplay or performance benchmarks.

The completed [native review and comparison](verification/character-pass/README.md) includes all eight character angles, in-world screenshots and a fresh 23-check delivery route, including five visible-floor contact checks. Character volume, face readability, hair intersections, cuff joins and hand contact were judged visually in addition to the automated assertions; a passing build cannot establish likeness on its own.
