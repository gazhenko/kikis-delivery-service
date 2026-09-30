# Polish pass: sound, light, town life and wayfinding

September 30, 2026. This pass keeps the approved direction: a third-person delivery adventure with painted backgrounds, drawn-looking cel characters, authored motion and the bakery life loop. It adds what the game was missing in play, and it fixes defects found by reading the source, reviewing native captures and looking at the two real saves on the Mac and Omarchy. Kiki's rig, meshes, walking and flight performance, the delivery rules and the save format are unchanged. Native results are in [the verification record](verification/polish-pass/README.md).

## What the review found

- **The fog never rendered in native players.** `Daylight` enabled linear fog at runtime, but the generated scene was saved without fog and Graphics settings strip fog modes automatically. The players therefore shipped without fog variants; hills and the far town stayed fully saturated to the horizon. The scene builder now saves linear fog into the scene. The distances were retuned to 95–640 m, so the far side of town stays readable while hills and headlands dissolve into the painted horizon.
- **Kiki's turn smears displaced the crows.** Crows used Kiki's `Cel_Ink` material, and the character shader applies the rider's smear deformation to every mesh that uses it. For two drawings after a sharp turn, a crow 30 m away could be swept by several metres. Birds now use cel materials with a `_Detached` flag that skips the rider's deformation.
- **The game was silent.** There was an `AudioListener` but no sound at all.
- **The delivery loop was hard to discover.** Both real saves contain zero deliveries: the Mac save has reached day 33 with five hospital returns, and the Omarchy save ends in the northern orchard after 3½ minutes. The only guidance was a small minimap dot. The delivery radius was invisible, and "Walk closer to the delivery court" appeared even when Kiki carried no parcel. Energy fell with no warning until the hospital.
- **Several promises were only on paper.** The moonlight lantern "lights the way home" but was not visible. A collected parcel did not appear anywhere on Kiki. Street lamps stayed dark at night, and every window lit at the same instant.
- **Menu and title problems.** With a save present, choosing *Cozy* or *Challenging* silently continued that save at the new difficulty, and there was no way to start again. Escape or B on a settings sub-page closed everything. A full-width controls bar stayed on screen for the whole game, and "Easing into…" notices remained after landing.
- **The horizon.** The seawall ran straight to the edge of the world, the boats were placeholder ellipsoids that could not move, and nothing continued the town beyond the playable district.

## Changes

### Sound
`Soundscape` synthesizes 40 clips at startup on a worker thread (about 0.5 s on an M1 Pro). No recordings or film music are used; everything is original and built from source.

- Beds: wind that follows speed, height and boost; a sea that swells every four seconds near the quay; crickets after dark.
- Town: songbirds by day near the ground, gulls calling from the actual circling gulls, crows cawing as they start a chase, and the clock tower striking at six, noon and six (positioned at the tower; a sleep skip is silent).
- Kiki: footsteps from the real planted footfalls, which differ on cobbles, grass and the wooden deck; takeoff and boost whooshes; a landing thump.
- Bakery and interface: wooden ticks for menu navigation, soft chimes, coins, a sizzle when cooking starts and a double ding when it finishes, a lullaby for sleep and a delivery fanfare.
- Music: an original sixteen-bar waltz in F, played on a music box over plucked-string bass and chords in a small room. It plays under the title and, more quietly, while the bakery board is open. Flight keeps the soundscape instead of a looping track.
- Settings → *Sound & display* has master, music and effects volume. Preferences are kept outside the save. Development checks run muted, apart from the tour, which plays at reduced volume so it can measure the listener.

### Light and atmosphere
- Working aerial fog (see above).
- Thirty street lamps draw a painted halo after dusk and lay a warm pool on the paving beneath them. This is a single additive mesh, so it has no lighting cost.
- Each window carries a household value exported from Blender. Most windows light warmly at their own moment of dusk, some stay dim and about a fifth stay dark.
- The sky's cloud banks drift slowly. Painted stars twinkle at night (only in open sky, never over clouds), and a moon rises over the harbour.
- The sea draws a glitter path of short dashes toward the moon at night and toward a low morning sun, on the sea's existing 12 Hz drawing clock.

### Town life
- Smoke rises from sixteen chimneys, and the bakery chimney always draws more. The puffs are flat two-tone cel shapes with a lobed, drawn edge that lean away on the sea breeze.
- Seven gulls with bent, black-tipped wings glide and occasionally flap over the harbour.
- Five bespoke moored boats replace the placeholder hulls: two sloops with cream mainsails and furled jibs, a working boat with a wheelhouse, drying net and floats, and two rowing boats at the piers. They ride a slow swell, pitching and rolling on the 12 Hz drawing clock.
- Takeoff and touchdown kick up a small scuff of dust, and a crow strike leaves a puff of dark feathers.

### Kiki
- A collected parcel hangs from the broom behind her on a string, swinging on a damped pendulum as the broom moves. It is a paper-and-string box, a cream box with a red ribbon for fragile china, a strapped crate for heavy freight or a canvas sack for the airship. It pops into place on collection and away on delivery. The parcel uses the rider's paint and ink, so smear drawings bend it with her.
- The moonlight lantern, once fitted, hangs from the front of the handle and glows after dusk. When she walks, the upright broom carries it like a lamp on a pole.
- Delivery acting: a quick polite bow, the free hand reaching out to hand the parcel over, then a wave goodbye. Jiji flicks his tail. It lasts under two seconds and never holds up the controls.

### Wayfinding, HUD and feedback
- A paper tag with the parcel icon and distance sits over the destination court. When the court is off screen or behind the camera, the tag clamps to the screen edge with an arrow. With no parcel, the tag leads back to the bakery.
- A painted ribbon circle on the target court is drawn at the true delivery radius, so landing inside it is exactly what counts. It brightens once Kiki is inside, grounded and slow.
- The minimap pulses the target court and draws a faint pencil line to it.
- On handover, a stamped receipt shows the pay and timely tip, and the duplicate rules notice is suppressed. The rules now expose `LastPay` and `LastTip`, which are covered by a new core check.
- Jiji gives one-time tips (to take off, to land, the way home and night deliveries) and repeatable warnings when Kiki is tired or hungry. Tips wait until the screen is quiet.
- Interacting now gives context-aware messages: how far the parcel's court is, that there is no parcel yet, or to fly lower over the ribbon circle. Approach notices clear once Kiki lands.
- The clock and purse share one card, with a painted sun or moon and a coin. The meters pulse when energy or fullness runs low, and the parcel timer turns red in the last quarter.
- Control hints appear when they are useful: at the start of a visit, when the controls change (walking or flying, keyboard or controller) and after a long pause. They then fade. Settings offers *Always* and *Hidden*. The strip never overlaps the energy card at any aspect ratio.

### Title and menus
- The title screen is a slow late-afternoon flyover of the town and bay, with the existing Kiki-and-Jiji portrait and a card. The camera then blends down to Kiki when play begins.
- With a save present, the title offers *Continue your deliveries* (with the day, coins, deliveries and pace) and a separate *Start a new game…*. That option asks for confirmation and copies the current save to `koriko-desktop-v1.before-new-game.json` first.
- Escape or B steps back from *Controls & camera* or *Sound & display* to settings. Settings shows the current day and pace and highlights the active pace.
- Sleep and hospital returns show a caption over the fade. After a hospital return, Kiki faces out of the courtyard.

### Camera
Walking frames Kiki closer, at 5.6 m, and flight keeps 7.5 m. Boosting pulls back a little and widens the lens by up to 5°. Steering, look and collision are unchanged.

### World
`Tools/environment_art.py` adds:
- the moored boats, kept as separate `Boat_` assemblies so the game can move them;
- per-window household values;
- two headlands with lobed coasts, limestone cliffs, pine groves and sea stacks;
- a lighthouse, whose lamp sweeps and flares toward the viewer at night;
- three islands in the haze;
- a 46-house hill town with a church spire on the north-eastern hills.

None of these collide or change the playable district, the six courts or the minimap layout. The world rebuild used the same Blender 4.5.3 as the committed sources, which had no uncommitted manual edits.

## Tools and checks
- `--koriko-tour-check` is a new native review of this pass. It captures the title, the bakery, on-foot carry, the destination tag (in view and clamped), landing, the stamp, the hand-over and the wave. It also covers night streets, the lantern, the moonlit harbour, morning boats, the headlands, the hill town, the night lighthouse, parcel kinds and both settings pages. It exports every synthesized clip as WAV with a level table, records twelve seconds of the final mix at the listener, and runs 33 assertions. Like the other checks, it never reads or writes saves or preferences.
- `Tools/build-on-vm.sh` accepts `KORIKO_MAC_DESTINATION` to stage a candidate, so the installed app is replaced only after verification.
- The controls check now expects B to step back from a sub-page before closing settings.

## Not done, and limits
- Nobody has listened to the audio. The mix was measured (levels, clipping, spectrograms), not heard.
- There are still no animated townspeople or recipients at the courts. The hand-over is acted by Kiki alone.
- No physical-controller or human flight-feel playtest was performed, and no controlled GPU benchmark was run.
- Distant headlands, islands and the hill town are simple painted forms meant to be seen through the haze, not explored.
