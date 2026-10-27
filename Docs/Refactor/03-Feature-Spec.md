# 03 — Feature Specification

Legend: **Keep** = the old look/behaviour was approved, rebuild it cleanly. **Change** = rebuild differently.
**New** = did not exist. Every item lists what "done" means.

---

## 1. Lighting core — Keep

- Wrapped diffuse (Diffuse Wrap, Diffuse Softness), Half Tone, baked tones, gradient ambient from the
  environment profile blended with lightmaps / APV / probes, painterly specular and rim, Forward+ extra
  lights, light layers, light cookies, SSAO, URP decals (DBuffer).
- Shadowmask (Mixed) supported; distant shadows from the bake. Lightmapped statics, probe-lit dynamics.
- Done: albedo-only material ≤ 500 slots; every family uses the same core; identical look across shaders.

## 2. Shadows — Change

- Keep the package filter: contact hardening within **15 m** (user forbids shrinking it), soft beyond.
- Add per-level presets like Grass (distance, cascades, resolution, softness, taps) in the Quality profile.
- Guard every custom filter with the empty-shadowmap test (`_MainLightShadowmapSize.z < 2`, see pitfalls).
- Debug views move to the Debug window.
- Colored transparent shadows (tint atlas) — Keep, but skip the cull when no tint caster is visible.
- Done: Low/Medium/High/Ultra measurable cost steps; no cascade leaks on huge triangles with Near Plane = 1
  (documented), debug views working.

## 3. Brush — Change ("felt everywhere")

The brush is the signature of the style. One global atlas + one world scale, read by **every** shader that
draws the world: Lit, Foliage, Terrain, Grass, Billboard, Liquid foam/shore, Sky and Clouds, Shadows edges,
Particles, Decals, Shader Graph nodes.

- ≤ 2 triplanar planes, 1–2 octaves; stroke offset of textures (painted look) + normal shift + albedo/tone
  variation; Object / World / Rest Pose spaces; distance growth instead of fade where it looks better.
- A short per-shader table in docs: which brush terms each family uses.
- Done: brush visible on grass blades and flowers (was missing), on terrain, sky and water foam.

## 4. Lit / Foliage / Billboard / Particle / Decal — Keep + Change

### Lit
- Sections: Surface, Maps (Base/Normal/Mask, Triplanar with its own Mapping Space), Shading, Brush,
  Specular/Rim, Subsurface (skin/cloth), Emission, Weather (Receive Weather None / Static / Moving), Cavity.
- Receive Weather: **Static** = world-space patterns, full wetness, snow blanket, shelters what is below;
  **Moving** = beaded drops / flakes in object or rest space, **never shelters**; **None** = untouched.
- Mask map: R metallic, G occlusion, B height (terrain), A smoothness.
- `DisableBatching = True` where shaders read per-renderer data (bounds, object space).
- Every cbuffer variable is declared in Properties (SRP Batcher + GPU Resident Drawer compatibility).

### Foliage — Change
Problems to solve: merged meshes, translucency and self-shadowing looked bad; canopy from renderer bounds
broke with batching/merging.
- **Bake canopy data into the mesh** (editor tool / import step): canopy normal (from a fitted proxy shape
  per plant cluster) and thickness into vertex attributes. Works for merged meshes, static batching and GPU
  instancing. Runtime no longer reads renderer bounds for lighting.
- Translucency from baked thickness, light from behind all day, strongest at low sun; snowy leaves stop
  glowing.
- Self-shadow softening on the back side so tall bushes do not go black.
- Snow on the up-facing side of leaves only (no two-sided snow).
- Wind with rest-space anchor (brush/weather do not slide).
- Open points for Phase 7 (from the reverted first build, 2026-10-24):
  - A canopy normal baked in object space is wrong once static batching merges rotated plants: batching moves
    positions, normals and tangents into world space but leaves UV channels as they are.
  - Plants merged at runtime that were never baked get no canopy data at all.
  - Baking by hand per plant model is a chore; an import-time bake (like the brush anchor postprocessor) is an option.
  - How merged meshes and the billboard system work together needs its own discussion.
  - Speckled, dithered-looking patches on some stem / branch cards in the test; not measured, cause unknown.

### Billboard system — Keep (worked well)
- GPU instancing, compute culling (frustum + sun-extruded shadow casters), mesh near / 8-view billboard far,
  dithered cross-fade, FOV-aware distances (60° reference), probe lighting, editor picking, inspector counter.
- Works with terrain plant painting of our own terrain (not Unity trees); plant painting moves to Phase 7 with
  the billboard system.

### Particle — Keep: Alpha / Premultiply / Additive, flipbook blending, lit option, OIT.
### Decal — Keep: URP DBuffer projector in style.

## 5. Atmosphere (fog + horizon) — Change

Replace the froxel volumetric fog (heavy, many patches) with an analytic model evaluated in every surface
shader and the sky:
- **Height fog**: start/end distance, base height, falloff, max opacity, day/dusk/night colours.
- **Horizon / aerial perspective**: distant layers fade toward the sky colour with distance and height, so
  mountain ridges recede in steps and the sea meets the sky without pixel noise at the horizon.
- **Sun inscatter**: analytic glow toward the sun; optional cheap sun-shaft term only if it stays in budget.
- Transparents and liquids use the same function. Shader Graph gets a node.
- Decided 2026-10-24 (revised after the 2a test): two separate terms.
  **Fog** — height + distance fog with its **own Day/Dusk/Night fog gradients** read along the view direction (a
  third slice of the sky LUT), near and mid range; it also hazes the sky.
  **Horizon** — far aerial perspective fading toward the **sky colour** with its own start/end distances, so the
  horizon stays seamless; the optional **horizon layers** (quality toggle) step this term into soft-edged layers.
- Key light: **HB Style Sun writes the Light's colour and intensity** (Sky Light gradient, Moon Light, fade
  across the sun–moon swap, overcast dimming), so URP shaders and the lightmapper see the same light.
- Done: no 3D textures, no history buffers, no full-screen fog pass; sky and terrain meet seamlessly.
- **Revised 2026-10-27 (after Phase 4, user):** the rebuild had lost the old fog's **Paint** and **Silhouette**.
  Decided: opaque surfaces are fogged by **one full-screen atmosphere pass** after the skybox (before
  precipitation and transparents), reading the depth texture, instead of in every surface shader; Lit and Terrain
  no longer apply atmosphere themselves (frees ~130 Lit slots). The sky, clouds, precipitation and later the
  transparents keep the in-shader functions. Both features return as before, in the profile's Fog Painting
  group: **Fog Paint** (0–1, default 1: fog colour broken by brush strokes with the sky's Sky Paint / Sky Brush,
  laid in world space on the surface behind the fog and sized by distance, handing over to the sky's strokes
  where the fog swallows the scene; the sky haze is painted the same way) and **Silhouette** (0–2, default 1:
  each stroke takes its fog from the stroke-shifted neighbour in the depth texture, so outlines break into the
  farther, foggier side; it only adds fog). The horizon term now fades toward the painted sky gradient, so
  ridges meet the painted sky without a seam. The pass blends premultiplied (no colour copy); sky pixels are
  skipped; overlay cameras are not fogged.

## 6. Sky — Change + New

- Keep gradient sky, sun/moon discs and glow, stars, painterly brush over the sky.
- New: **aurora** (night, profile-controlled), richer **stars** (colour temperature, size variation,
  twinkle, milky-way band), better **cloud translucency** (silver lining, powder darkening, sun-through).
- Cloud atlas moves from the renderer feature to the **Environment profile** (Clouds section).
- Done: sky shader within budget; horizon matches the atmosphere function.

## 7. Weather — Change

- Shelter: top-down sky depth near (≈160 m, 512²) + far (≈1 km) for the base camera only.
- **Roof margin**: occluder heights are dilated outward (configurable, default ~0.4 m) so snow/rain do not
  leak just inside walls and under eaves. Plus `HBStyleWeatherZone` (box) that forces "indoors".
- Moving materials never shelter; terrain does not shelter itself (it is the ground).
- Cover / uncover timing with a time-based fade on big drops (slopes included).
- Surface effects: wetness, drop spots, running water on slopes, ripples on flat ground, snow blanket
  (Static); beads / flakes (Moving). Detail effects only within ~30 m.
- Precipitation: GPU procedural quads (no CPU mesh), round soft snow dots (snowflake texture dropped 2026-10-25),
  rain streaks, far layer; hidden under roofs / behind glass; triangle budget per quality level (Ultra ≤ ~150k
  triangles drawn).
- Weather state is driven by the Environment profile / API (rain, snow, wind, wet/dry/settle/melt/cover/
  uncover times, Instant). Presets: Clear, Drizzle, Rain, Storm, Light Snow, Blizzard, Sleet.
- Decided 2026-10-24 (Phase 3 discussion):
  - **State lives on the controller.** `HBStyleSun` becomes `HBStyleEnvironmentController` ("HB Style Environment
    Controller", same script GUID so placed components survive), still on the directional light and still writing
    its colour and intensity. Fields side by side: Profile, Time of Day, Sun Heading, **Rain, Snow, Wind, Wind
    Heading**. `HBStyleEnvironment.SetWeather` moves these values over time in Play mode (the sliders move like the
    clock).
  - **The profile's Weather section holds style only**: wet / dry / settle / melt / uncover times (Cover Time
    removed 2026-10-25: nothing used it), rain colour and similar look settings, and **Cloud Response** (0–1, default 1: how far rain and snow raise cloud
    coverage and overcast; the key light dims through the existing overcast rule; no effect without a cloud atlas).
    Zones blend this section per camera like the others. Presets (Clear … Sleet) set the controller's values.
  - Rain, snow and wind are **one global state**; no per-region climate (no rain/snow-allowed switches) for now.
  - **Interiors are protected by shelter only**; no `HBStyleWeatherZone` box in Phase 3. Spaces below terrain level
    are discussed with the Terrain phase; a box is added only if a real leak shows up. Environment zones for caves
    should not take over the Weather section (they blend per camera, so the view out of the cave would dry up too).
  - Cloud drift stays a Clouds style setting; the controller's wind does not move clouds.
  - Outside Play mode weather is **instant** (surfaces sit at the slider values); timings run in Play mode.
  - Snow and wetness never read `unity_RendererBounds` (broke under static batching); heights come from world
    position and the shelter maps.
  - Surface cost: the approved old snow / wet look is ported piece by piece and measured, look-neutral
    optimisations first; if Lit still exceeds its budget, the trade-offs are shown in a table and decided together.
  - Phase 3 is built in **one batch** with one test list.
- First batch (2026-10-24) failed its Unity test (Static objects did not wet, drops missing on some meshes, dry
  rings around tall objects, overcast too dark) and was removed. Lessons kept for the rebuild:
  - Build in small visible steps, each checked in Unity before the next; a debug view first.
  - A roof margin that dilates tall things puts dry rings around pillars; add margins only for a measured leak.
  - Ledge / height terms can come from shelter taps instead of renderer bounds (simulated to match on a crate).
- Rebuild order (decided 2026-10-24), one step per test: **1** shelter map only (top-down depth, no closing,
  margin, follow or far map) with the Weather/Shelter debug view and a map preview in the Debug window;
  **2** wetness (darken, gloss, timing); **3** snow cover; **4** rain details one by one (spots, ripples, running
  water); **5** Moving drops and flakes; **6** falling precipitation. Each step reports its cost; the package
  stays near its 2.5 ms frame cost.
- Step 1 checked by the user (2026-10-24): the map is right; edges were blocky on the 0.31 m texels. Decided:
  the shelter lookup is a 4×4 B-spline built from 4 Gathers (Lit forward target 4.5; same reads as before,
  simulated equal to 16 taps to 3e-7), and Weather quality has no Off (Low–Ultra; weather is switched off by
  Rain and Snow = 0; Precipitation keeps its Off).
- Step 2 (decided 2026-10-24): plain wetness only. Open surfaces (from the shelter) darken and gloss as the
  wetness state rises; surfaces without Specular get the wet film reflection and highlight; the material's
  Wetness (0–1) sets how far. Patches, spots and streaks come one by one in step 4.
- Step 2 checked by the user (2026-10-25). Found: overlay (FPS hands) cameras read a released shelter map, because
  the map was a per-frame render-graph texture of the base camera → the map is now a persistent texture per base
  camera (pruned after 2 s unused), so overlays read their base camera's map.
- Moving surfaces should wet and dry over time when they leave or enter cover (decided 2026-10-25, step 5): a
  component on moving objects keeps its own wetness and snow from a CPU readback of the shelter map and passes them
  through the Renderer Shader User Value (Unity 6.3+, no batching cost).
- Step 3 (decided 2026-10-25): plain snow cover. Open, upward-facing surfaces whiten as snow settles (slope rule of
  the old look, brush coverage breaks the slope edge), timing from Settle / Melt Time, the material's Snow Cover
  sets how far; wetness hides under snow and brush strokes soften. Moving surfaces hold no snow until step 5.
- Step 3 test (2026-10-25): overlay hands still unaffected → overlay cameras now import and bind their base
  camera's shelter map and matrices themselves in the globals pass instead of relying on globals left by the base
  camera's render graph. Snow looked complete far too early (Snow Cover 0.1 ≈ full white) because the old
  formula's ÷0.4 and ×3 were meant for the staggered patch delays → plain snow is linear in the settled amount and
  Snow Cover; the stagger returns with the patches in step 4.
- Steps 1–3 confirmed by the user (2026-10-25), FPS hands dry under roofs. Step 4 order: **4a** patchy settling
  (wet and snow) with ledge-edge and height delays and brush break-up of shelter edges; **4b** rain spots;
  **4c** ripples; **4d** running water — one test each.
- Step 4a: the old approved formulas return (patch noise, wet spread 0.5, snow spread 0.6 with ×3 fade-in, edge
  delay, height delay, brush delay, walls drain while it rains). Edge and height come from the same 4 shelter
  Gathers (ledge drop 0.25 m, gain 5, as simulated earlier). With patches, a low Snow Cover settles in fewer,
  fully white patches instead of a faint tint.
- Step 4a test (2026-10-25): bands and cuts in the snow on a sphere. Simulated: the shelter-based edge and height
  jump along a sphere (edge 0 → 0.78 → 1 → 0.61, height 0.75 → 0.62 → 0.92 between 30° and 54°), soft thresholds
  do not fix it; the old bounds terms are smooth. Decided: edge and height use `unity_RendererBounds` again (Lit
  already has `DisableBatching = True`, which fixed the old static-batching snow bug), replacing the earlier
  "never read renderer bounds" note. Slopes no longer wet twice as fast while it rains; they dry in patches as before.
- Step 4a confirmed (2026-10-25). The bare band on a sphere's steep sides is the snow slope rule (no snow beyond
  ~70° tilt); simulated: no self-shelter on spheres of 0.5–3 m. The user chose to build 4b–4d together: rain spots
  on flat ground, ripples on flat wet ground, running water down faces steeper than ~45°; all three fade out at the
  Weather quality's detail distance; running water follows the material's Wetness.
- Steps 4b–4d confirmed (2026-10-25). Step 5 decided: Moving surfaces never show running water or a snow blanket;
  they carry a uniform wet film plus rain drops and snow flakes that work the same way (upward faces, same density,
  size and life defaults), drops are water and flakes are snow. **Without** an `HBStyleWeatherBody`, and in Edit
  mode, the wet / snow amount follows the shelter per pixel at once; the component sits on the object's root and
  covers all renderers below it; it reads the shelter at one probe point (transform position + Probe Offset,
  turned with the transform only when Follow Rotation is on, so an FPS camera looking up or down keeps the point)
  from a CPU readback of the key game camera's map (`Camera.main`, else the last base Game camera; every 0.2 s),
  follows the global wetness / snow with Uncover Time (rising) and Dry / Melt Time (falling), and packs wetness and
  snow into the Renderer Shader User Value (bit 31 = tracked). Static and Moving are separate keywords again
  (`_HB_WEATHER`, `_HB_WEATHER_MOVING`); the Lit inspector re-validates keywords when a material opens. The
  component runs in Play mode only; its inspector shows the shelter map camera, open-to-sky share, wet and snow.
- Drops and flakes are discs in the surface plane (plane from the bead-space derivatives); each 3D cell layer is
  present by a tent along the normal, so density and size do not depend on where a face sits in the cell grid
  (simulated: flat faces 0.13–0.145 at every offset, before 0.000–0.104; spheres even at every latitude). Beads
  only form while precipitation reaches the surface: their clock runs while rain or snow falls (global clock,
  `_HB_WeatherSurface.w`) and, for a Weather Body, while it falls on the body (scaled by its open share). Under
  cover or after the rain the beads hold still and vanish one by one as the surface dries or melts. User value
  packing: bits 0–6 wet, 7–13 snow, 14–30 clock (1/32 s, wraps after 4096 s), bit 31 tracked.
- Step 5 confirmed (2026-10-25). All features + Moving was over the Lit budget (+2910); drops and flakes now share
  one 8-cell loop on the larger of the two sizes: each cell carries a flake with the snow's share of the total and
  a drop otherwise, so with only rain or only snow the look is unchanged; with both, they split the cells, and a
  smaller Drop / Flake Size draws smaller beads on the shared grid.
- Step 6 decided (2026-10-25), falling precipitation in four parts, one test each:
  - **6a near layer**: GPU procedural quads (no CPU mesh, no particle system) in a box around the base camera that
    wraps drops across its faces; base cameras only, camera culling mask respected; drawn before transparents
    (after the skybox, as in `02`); rain behind or in front of glass is solved with OIT in Phase 8. A drop is
    hidden once it falls below the shelter roof at its position (one map read per drop, soft landing fade).
    Rain: thin streaks stretched along the fall + wind velocity, at least 1.5 px wide, lit by ambient and key light
    and tinted by Rain Color. Snow: round soft dots (no snowflake texture), swaying as they fall. Wind 1 tilts rain
    by 30° and drifts snow at 2 m/s. Rain and snow amounts scale the drop count, times the Precipitation quality
    density; Ultra stays under ~150k triangles. Profile Weather section gains Rain Opacity, Rain Width, Snow Size
    and Snow Opacity; fall speeds and box size are constants. Debug: drawn drop / flake count in the Debug window
    and a Weather/Precipitation view that draws sheltered drops red instead of hiding them.
    Built (2026-10-25): 30 m box, up to 40 000 rain streaks and 30 000 snow flakes at Ultra; rain falls at 9 m/s
    (streak = 0.04 s of travel), snow at 1 m/s with 0.25 m sway; drops fade over the last 0.3 m above the roof or
    ground below them, near the camera (0.3–1.5 m), toward the box edge and in fog. Hidden shader
    `Hidden/HB URP Style/Precipitation`; drawn only when the Environment Controller's layer is in the camera's
    culling mask.
    6a test (2026-10-25), fixes: (1) wet / snow patches were 2D noise on world XZ, so on walls they ran as
    vertical stripes (simulated: vertical gradient 0.0000 on a wall) → 3D simplex value noise, scale ×1.5 so the
    pattern size matches the old one (simulated: gradient 0.0033–0.0035 on floor and wall, old floor 0.0035; mean
    0.53, std 0.16). (2) Rain spots now form on every upward face up to 70°, fading out from 63°, and stay round on
    slopes (distance measured in the slope plane); on slopes they grow with the drained wetness. (3) Faces turned
    more than ~30° downward take no rain or snow (a thin roof's underside saw only its own thickness above it and
    counted as open). (4) Roof Margin works: two compute passes dilate the shelter map's heights by Roof Margin
    (rounded to whole texels, at least one) into a persistent map that every reader binds (Lit, precipitation,
    bodies, overlays, preview); simulated at the worst texel alignment: the 0.1–0.25 m leak under a roof edge is
    gone, and free-standing things get a 0.15–0.35 m dry ring outside (accepted). (5) Weather Bodies ignore the
    profile's Instant and always use Uncover / Dry / Melt Time.
    Second test (2026-10-25): the bright strips at the foot of walls and plinths vanished with Roof Margin 0, so
    they were the margin's dry ring. Decided: the margin grows only overhangs. The shelter pass draws the opaques a
    second time with an override shader (`Hidden/HB URP Style/Shelter Underside`, material properties kept so
    Moving surfaces still drop out) that keeps only faces whose normal points down, with the lowest one winning; a
    neighbour's roof is spread onto a texel only when that neighbour's lowest downward face is at least 0.5 m above
    the texel's own top. Simulated: eave strip covered 10/10 texels, capped cylinder pillar and a box standing on a
    plinth get no ring, a table top covers the floor around it; meshes without a bottom face (open-bottom walls
    under a roof) still get the ring. The user settled on Roof Margin 0.125.
    Third test (2026-10-25): walls under a ceiling stayed wet in a band below it, because the cover rule needs the
    roof at least 0.7 m above the point (0.1 m bias + 0.6 m soft range, kept so curved objects do not shelter
    themselves). The underside map is now drawn whenever the shelter is, bound as `_HB_ShelterUnder` (overlays too),
    and the shelter lookup also reads the 2×2 underside texels at the probe: a downward face above the point covers
    it (soft over 5 cm). Simulated: the band 0.1–0.5 m under a 0.1 m roof went from 0.17–0.83 covered to 1.0;
    spheres of 0.5–6 m stay uncovered on their upper half (0.000) and down to n.y −0.1 (≤ 0.065); horizontal
    pipes 0.000. Lit all-features budget raised to 2550 slots for it.
  - **6b far layer** filling the distance (Precipitation quality Far toggle). Decided (2026-10-25): two vertical
    cylinder curtains around the base camera at 40 m and 80 m radius, procedural streaks (rain) or dots (snow)
    scrolling with the fall and wind velocity, no texture; depth tested against the scene, faded by fog and by the
    rain / snow amount; each curtain pixel reads the shelter map once and hides under roofs (inside the 160 m map).
    Same lighting and Rain Color as the near layer. Drawn in the same precipitation pass, only when the quality's
    Far is on. Built (2026-10-25) with the curtains at **20 m and 60 m** instead of 40 / 80 m: the near box ends at
    15 m, so a first curtain at 40 m would leave no rain in front of anything 15–40 m away. Each curtain is a
    64-sided band from 30 m below to 60 m above the camera (fading over its top 20 m); wind tilts the streaks by
    shearing the pattern and only the fall scrolls (scroll wrapped every 60 m on the CPU, rows hashed with the same
    period, so there is no jump and no float drift). Rain: 524 columns around, rows of 0.1 × radius, streaks 35 % of
    a row; snow: 314 columns, round dots of Snow Size; strokes never thinner than ~1.5 px. Share of filled cells =
    amount × quality density × 0.6 (rain) / 0.5 (snow).
    6b test (2026-10-25): looks right; shelter is lost far from the camera (near map ±80 m; depth window 100 m
    above to 150 m below the camera). Decided: (1) the depth window grows to 1000 m (100 m above to 900 m below;
    16-bit precision 0.4 → 1.5 cm, below every rule's 5–10 cm); (2) a **far shelter map** as in the original spec:
    1 km × 1 km at 512² (~2 m texels), redrawn only when the camera has moved 50 m (or the quality changes), read
    by Lit where the near map ends with one Gather (bilinear, no underside, coarse edges at distance); Lit cost to
    be measured against the 43 spare slots before it lands. Built (2026-10-25): the far map is a persistent D16
    512² texture per base camera, culled and drawn (top surfaces, Moving collapsed, no underside, no margin) only
    when the camera has moved 50 m in any direction since the last draw; it is bound every frame with its own
    view-projection and height (`_HB_ShelterFarDepth`, `_HB_ShelterFarViewProj`, `_HB_ShelterFarHeight`,
    `_HB_ShelterState.y`), overlays included. Lit blends from the near lookup to the far one over the near map's
    last 16 texels (~5 m) and uses the far one alone beyond; the near/far bounds test and the blend share one
    edge distance. Precipitation and Weather Bodies keep using the near map only. Confirmed by the user
    (2026-10-25): far shelter and the 1000 m window work.
  - **6c splashes** where drops land. Decided (2026-10-25): rain only (snow lands silently), scaled by the rain
    amount and the Precipitation quality density. Splashes are not tied to the falling drops: each one lives a
    short cycle (~0.25 s) at a random point within ~12 m of the camera, placed on the surface the shelter map shows
    there (roof or ground, so never under a roof). Look: a small crown (3–5 droplets thrown up and falling back)
    plus a thin ring spreading on the surface; procedural, no texture; same lighting and Rain Color as the drops.
    Built and removed again (2026-10-25): at a real viewing distance the splashes could not be seen, so the user
    dropped them; 6c is not part of Phase 3.
  - **6d wind streaks** in the sky (the old package's ribbons), shown with wind strength. Decided (2026-10-25):
    the old look — thin, winding, stylised ribbons ~30 m above the camera, 15–30 m long, drifting along the wind
    heading, each living a few seconds and fading in and out; procedural, no texture. They appear from Wind 0.3
    and are densest at Wind 1, with or without rain or snow (none when Precipitation quality is Off). Every
    ribbon point reads the shelter map and hides under roofs, so none show indoors. Built (2026-10-25) as a port
    of the old ribbons: up to 12 ribbons at Ultra (× quality density), 20 segments each, in an 80 m box around the
    camera, 30 ± 4 m above it; each lives 2.1–3.9 s on the real clock (they move without rain), travels Wind ×
    12 m/s along the heading while swinging up to 4.5 m sideways and 1.2 m up and down, 0.18–0.42 m wide (at least
    1.5 px) and tapered at both ends; opacity 0.6 × (Wind − 0.3) / 0.7, faded at the box edge and in fog.
- Phase 3 review before commit (2026-10-25): a Weather Body no longer throws when one of its renderers was
  destroyed; the body's cover check also reads the underside map (a downward face above the probe covers it,
  like the surface lookup), so low ceilings count; the unused Cover Time setting was removed and Uncover Time's
  tooltip now says it is for Weather Bodies only.
- Kept from the first batch: Environment Controller weather values, profile Weather style section, wetness /
  snow timing state, Weather and Precipitation quality levels, cloud response.
- Overcast light (decided 2026-10-24): at full overcast 30 % of the direct sun remains and the ambient rises by
  75 %, so a rainy sky no longer darkens the scene to ambient only.
- **Do not** reintroduce per-texel local accumulation (rejected, see pitfalls).
- Done: no indoor leaks in the sandbox houses; ≤ 0.3 ms shelter, ≤ 0.5 ms precipitation at Ultra.

## 8. Terrain — New (own system)

- `HBStyleTerrain`: heightmap (R16), size, resolution; clipmap rendering; collider via hidden TerrainData.
- **Import** from Unity TerrainData: height only (layers/textures not required).
- Layers (`HBStyleTerrainLayer` assets) on the component: albedo/normal/mask, tiling, tint, variation,
  hex anti-tiling, height blend, weather limits, grass type; rules (slope/height ranges, softness, order)
  baked into the control maps in the editor; hand painting preserved across re-bakes; top-3 picks per pixel.
- Curvature shading (cavities darker, ridges lit).
- Editing overlay: sculpt (raise/lower/smooth/flatten), paint layers, paint grass, paint plants, paint water.
- Done: replaces Unity Terrain in the sandbox scenes; same or better look; no basemap artefacts.
- Decided 2026-10-25 (Phase 4 discussion), built in small steps with one Unity test each:
  - **4.1** component + R16 heightmap + import + CDLOD clipmap rendering (flat colour with our lighting, LOD
    debug view) + hidden TerrainData collider; **4.2** layers, control maps, rule bake, hand painting; **4.3**
    sculpt and paint tools; **4.4** look (hex tiling, steep slopes, curvature, brush strokes, detail distance);
    **4.5** weather on terrain.
  - Size: one `HBStyleTerrain` is one terrain of up to 4 km × 4 km with up to a 4097² heightmap (~1 m spacing);
    neighbouring tiles and streaming are out of scope for now.
  - Layers: the strongest **3** per pixel (as specced; the old package used 4).
  - **Holes**: a paintable hole mask; the terrain is not drawn there, the collider gets the hole (TerrainData
    holes) and the shelter map does not see ground there. Cave interiors are separate meshes.
  - Import from Unity TerrainData: **height and holes** only; layer painting, trees and details are not imported
    (maps are usually made from scratch).
  - Heightfield + clipmap stays the base (discussed against voxel Surface Nets 2026-10-25: 3D data, per-chunk
    meshing, LOD seams that need Transvoxel-style stitching, mesh colliders and harder grass / liquids / import;
    no head-to-head benchmark found). Small voxel volumes for caves and overhangs may come later as their own step.
  - 4.1 decisions: data lives in an **`HBStyleTerrainData` asset** (heightmap, holes, later control maps; the
    hidden collider TerrainData is generated in the editor as its sub-asset, so nothing is built at runtime);
    patch selection on the **CPU quadtree** only (one instanced draw, no GPU path kept in reserve), its CPU time
    shown in the Debug window; normals computed per pixel in the shader from neighbouring heights (cost reported
    with 4.1). No meshes are generated: one fixed grid mesh is drawn per patch and reads the heights on the GPU;
    physics is always Unity's TerrainCollider.
  - LOD (final 2026-10-25): **distance-based CDLOD**. Within ~32 m of the camera the terrain is drawn at full
    heightmap resolution on every quality, so it matches the collider and nothing sinks or floats; the quality
    level only sets how fast density drops with distance; rings morph into each other, no pops or cracks. A
    per-area error-driven LOD was chosen first and dropped: varying the ranges across the terrain breaks CDLOD's
    one-level-apart rule far away (cracks) unless skirts or a distance-mipped roughness field are added. Distant
    detail is kept in the lighting by the per-pixel normals.
  - Baked light: **none on the terrain** (user's call 2026-10-25). The terrain gets realtime shadows and our
    stylised ambient; beyond the shadow distance static objects and hills cast no shadow on it. Researched first:
    a hidden proxy mesh for Unity's lightmapper (unverified that a hidden renderer keeps its bake) or our own
    terrain-space shadow bake; either can be added later without changing the rendering.
  - 4.1 built (2026-10-25, compiled offline, not run in Unity):
    - `HBStyleTerrainData` (Create > HiddenBull > URP Style > Terrain Data) keeps a hidden Unity `TerrainData`
      sub-asset as the single height / hole store: it gives the CPU heights (collider, queries) and the GPU
      textures (`heightmapTexture`, `holesTexture`) without a second copy or any syncing; no Unity Terrain
      component is used. The asset also stores min / max height per 16 × 16 patch. Its inspector imports heights
      and holes from a Unity TerrainData or creates a flat terrain (513–4097, any size). Terrains are square
      (width = length); a non-square Unity terrain is refused on import. Rotation and scale of the GameObject
      are ignored, like Unity's terrain.
    - `HBStyleTerrain` (on a GameObject with a TerrainCollider, which it fills): per camera in
      `beginCameraRendering` it selects patches on a CPU quadtree and submits two `RenderMeshInstanced` lists
      (view-culled for the camera, and a shadow-only list within shadow distance + 64 m without view culling);
      Game and Scene cameras only, not overlays. `TryGetHeight` / `TryGetNormal` read the TerrainData.
    - CDLOD after Strugar: patches of 16 quads; a node out of its child's range draws that quadrant at its own
      level with a half patch (8 quads), so every drawn patch lies inside its own range. Morph over the last 25 %
      of each range from the unmorphed vertex's 3D distance. Simulated with the real selection code and the
      shader's morph: 0 level jumps and 0 cracks on 30–40 cameras over hills, a 56° slope, an 84° cliff and a
      vertical cliff for a first range ≥ 96 m (80 m: 14 cracks of ≤ 5 cm at the vertical cliff), the nearest
      32 m always at full resolution. Quality Terrain section: first range Low 96 / Medium 112 / High 128 /
      Ultra 160 m (~110k–170k triangles on a 1 km test terrain).
    - `HB URP Style/Terrain`: flat colour with our lighting (HB_StyleLit, our ambient, realtime shadows,
      atmosphere), normals per pixel from 4 neighbouring heights, holes clipped in every pass; ShadowCaster,
      DepthOnly, DepthNormals; the terrain drops out of the shelter map (it is the ground, not a roof). Debug view
      Terrain/Detail Rings; the Debug window shows patches and CPU selection time for the Game camera.
    - 4.1 test (2026-10-25): first run threw "Material needs to enable instancing"; fixed by removing the
      `DisableBatching` tag (pitfall 1.11). The user then reported the terrain working.
  - 4.2 decisions (2026-10-25):
    - The layer list, the rules and the painted weights live in the **`HBStyleTerrainData` asset** (not on the
      component as first specced), so map and layers always travel together.
    - Layers are our own **`HBStyleTerrainLayer`** assets (albedo / normal / mask, tiling, tint; weather limits,
      grass type and footstep fields can be added later), with an import button that copies textures from a
      Unity TerrainLayer.
    - Up to **16 layers**: 4 RGBA control textures; the shader reads them and samples only the strongest 3
      layers per pixel. Control resolution is chosen per asset (512–4096, default 2048).
    - Rules: slope and height ranges, edge softness, order (later rows win) and edge break-up noise; water
      proximity comes with the liquids (Phase 6). Rules are baked on the GPU **automatically** after a short pause
      when a rule or the heights change, plus a Bake Now button; hand painting is kept across bakes (painting
      itself arrives with the tools in 4.3).
    - Layer textures are resampled into compressed texture arrays at a chosen size (512 / 1024 / 2048, default
      1024), so source textures may differ in size and format.
  - 4.2 as built (2026-10-25, compiled offline, not yet run in Unity):
    - `HBStyleTerrainLayer` (albedo, normal, mask R metallic / G occlusion / B height / A smoothness, tint, size
      and offset in metres, normal strength, smoothness, metallic) with "Import From Unity Terrain Layer"; with a
      mask map the Unity layer's mask remap maximum becomes our smoothness / metallic multiplier.
    - `HBStyleTerrainLayerRule` rows in the data asset; the first row is the base and covers everything, later
      rows cover the ones before them by their opacity where slope, height and break-up noise match. Rows with
      Automatic off are placed only by painting (4.3).
    - Editor-only bake: `StyleTerrainRules.compute` writes the weights (normalised, hand weights added as
      `bake · (1 − Σhand) + hand`) into an RGBA8 array, read back into the asset's readable "Control" sub-asset.
      `StyleTerrainArrays` resamples every layer into "Albedo" (BC7 sRGB), "Normal" (BC5) and "Mask" (BC7)
      array sub-assets with mips. A service bakes 0.4 s after the last rule, layer or height change and skips
      work whose signature is unchanged; tint, tiling, smoothness, metallic and normal strength are read live.
    - Shader: loops over `ceil(N / 4)` control slices, keeps the strongest 3, samples only those (with explicit
      gradients), then height-blends: `key = w + (h − 0.5) · 0.5`, `w' = max(key − top + 0.2, 0)`, normalised.
      Simulated across a border: coverage follows the weight (50 % at w = 0.5), the texture height decides only
      inside w ≈ 0.3–0.7, and a layer under ~25 % weight never punches through (the first sketch, `key = w + h`,
      let a 2 % layer with a tall texture beat a 97 % one). Without baked layers the terrain keeps the flat colour.
    - Normal maps use a tangent frame built from the height normal (T = (n.y, −n.x, 0)). Debug view
      Terrain/Layers colours each pixel by its strongest layer.
    - Query: `GetLayerAt(position)` and `GetLayersAt(position, layers[], weights[])` (up to 3, weights summing to
      1) read the control sub-asset bilinearly on the CPU; they report the baked weights, not the texture-height
      blend.
  - 4.2b decisions (2026-10-25, after the first 4.2 look): the bare Vector2 rule fields were not readable.
    - **`HBStyleTerrainLayerSet`** asset holds the layers and their rules; `HBStyleTerrainData` references a set
      and keeps only the baked maps and the hand painting, so one set serves many scenes and is copied to vary.
      Hand painting is stored per layer identity, not per row, so reordering a shared set does not move paint.
    - Rule UI: min-max sliders over a histogram of the terrain's real heights / slopes, height limited to the
      terrain's actual range in metres, a texture swatch per row, and the selected row's area highlighted in
      the Scene view.
    - Converter: selected Unity TerrainLayers → HB Style Terrain Layers created next to them.
    - Menus: Create > HiddenBull > URP Style > Profiles (Quality, Environment) and > Terrain (Terrain Data,
      Terrain Layer, Terrain Layer Set); GameObject > HiddenBull > HB Style Terrain creates the component,
      collider and a new data asset saved automatically like Unity's terrain.
    - Order: 4.2b → 4.3 (sculpt, holes and layer paint on one brush framework) → 4.4 + 4.5 together, each part
      with its own debug view.
  - 4.2b as built (2026-10-25, compiled offline, not yet run in Unity):
    - `HBStyleTerrainLayerSet` replaces the data's own list (layer lists made in 4.2 must be made again in a
      set). The data keeps a snapshot of the layers it was baked with, so a set edited from another scene leaves
      this terrain consistent (old look) until it is baked again; a terrain bakes on enable when its signature
      changed. The bake signature now includes each row's layer asset.
    - One list drawer serves the set inspector and the data inspector (which edits the referenced set in
      place, with New to create one): texture swatch, foldout per row, Slope and Height as min-max sliders over a
      histogram (256² samples of the terrain, sqrt-scaled bars, blue inside the range); Height spans the
      terrain's real min-max and a handle at an end writes the open value (±10000). Without a terrain in the
      open scene the set inspector shows a plain Height field. New rows get the rule defaults.
    - Selecting a row highlights that layer's baked weight in pink in Scene view cameras only
      (`_HB_TerrainLayers.w`, one extra control read behind a uniform branch).
    - Assets > HiddenBull > Convert To HB Style Terrain Layer turns every selected Unity TerrainLayer into an
      HB layer of the same name next to it. GameObject > HiddenBull > HB Style Terrain creates the object with
      the collider and a new 1025², 1024 × 300 m flat data asset at Assets/HB Style Terrain Data.asset.
    - Data inspector order: Layers (set, resolutions, list, Bake Now), Terrain info, then the Import and New
      Flat Terrain sections as foldouts.
  - 4.2c after the 4.2b test (2026-10-25, compiled offline, not yet run in Unity). User: layers still hard to use,
    highlight looked like an error, tiling repeats, no auto bake ("the old terrain's bake logic was nice").
    - **No auto bake.** While a Terrain Data or Layer Set inspector is open, `StyleTerrainPreviewer` reruns the
      rules compute (at most every 0.1 s, only when the signature changed) into a temporary RenderTexture array
      bound to the terrain (`StyleTerrains.SetPreview`); the data asset is untouched. Leaving the inspector
      returns the terrain to its last bake. Only the **Bake** button writes the control sub-asset. Layer
      texture arrays still rebuild by themselves, 0.5 s after a texture change, while an inspector previews.
    - Status bar on both inspectors: No heights / No layers / Baked, up to date / Previewing changes, not baked
      yet (Needs bake outside the inspector) with the Bake button.
    - **Pick** buttons beside Slope and Height: in the Scene view the cursor shows height and slope of the
      ground; a drag sets the range to the min-max under the drag; Esc cancels.
    - Highlight is now a 45 % blue overlay; it follows the live preview, so dragging a slider moves it at once.
    - **Hex tiling** on the strongest picked layer only (the 2026-10-19 tuning: rate 0.3, luminance weight
      exp2((lum − max) · 10), max-normalise, pow 24); the other two picks sample plainly. +6 reads.
    - Data inspector: status bar, then a Layers box (set + New + Duplicate, list, Bake Settings foldout with the
      two resolutions) and a Heights box (size line, Import and New Flat Terrain foldouts).
    - GameObject > HiddenBull > HB Style Terrain saves "<Scene> Terrain Data" in the scene's same-named folder
      when it exists, else next to the scene; Assets/ only for an unsaved scene.
    - Occlusion culling: only frustum culling per patch. A GPU Hi-Z pass is left to the final measurement phase.
  - 4.2d after the 4.2c test (2026-10-25, compiled offline, not yet run in Unity). User: the panel is unreadable,
    the old Terrain Layers window was nicer; a hard seam and shapes not in the textures came with hex tiling.
    - **Everything on the HB Style Terrain component** (user: one place, several windows make it harder; e.g.
      watch the bake preview while sculpting). Its inspector: Data field + New, status bar (state, Preview
      toggle remembered per user, Bake), tabs **Layers** and **Settings** (4.3 brushes join here). Terrain Data
      only stores; its and the Layer Set's inspectors show a summary and Select buttons for terrains using them.
    - Layers tab follows the old window: slope × height histogram graph (90 × 64 bins, log scale) with every
      automatic layer as a coloured box whose edges drag (dragging a height edge to the border = no limit);
      short list with colour strip, thumbnail and mode; the selected row's details (Where: Automatic, Slope
      min-max, Lowest / Highest with Any, softness, break-up, opacity, Pick Slope / Height In Scene); the layer
      asset's own settings inline under Look; Pick in Scene (old dark hover box with layer, slope, height;
      click selects the layer whose rule matches the point) and Layer Map (Scene view colours by list colour).
    - Hex tiling now on all three picks (removes the seam: a layer was hex-sampled as the strongest pick and
      plainly as the second) and without the luminance weight (the user saw shapes that are not in the
      textures only since hex); weights: barycentric, max-normalised, pow 24. 139 / 140 reads: the terrain
      budget is full, 4.4 and 4.5 need a raise or savings.
    - Height values stay metres above each terrain's own base; Any keeps a side unlimited on every terrain.
    - After the 4.2d test: picked ranges are rounded (0.5°, 0.1 m; a flat-ground raycast returned 4.77e-07 m),
      Lowest / Highest show two decimals and write only when edited, and a Highlight toggle (remembered) sits
      next to Pick in Scene and Layer Map. The user approved the look and the budget use; 4.2 is closed.
  - 4.3 decisions (2026-10-25):
    - Brushes: Raise / Lower (Shift lowers), Smooth, Flatten (to the clicked height), Set Height (typed metres),
      Holes (Shift fills), Paint Layer, Noise, Terrace, Stamp (a height texture pressed into the ground).
    - Shape: our round brush (Size, Strength, Falloff hardness) by default; Unity's terrain brush textures can
      be picked as the shape, with a rotation.
    - Sculpting writes the heights directly; layer rules and hand paint show in the live preview, and the Paint
      Layer brush turns Preview on. Bake writes the layer maps; until then the status says Needs bake. A Clear
      Hand Painting button. Undo for every stroke.
    - One **Brushes** tab on the component (Layers | Brushes | Settings): a tool row like Unity's, the brush
      settings below; Paint Layer picks from the same coloured list as the Layers tab.
    - Built in steps, each tested: **4.3a** brush framework + Raise / Lower, Smooth, Flatten, Set Height (undo,
      collider, patch bounds, preview follow); **4.3b** Holes + Paint Layer (hand array per layer identity,
      Clear Hand Painting); **4.3c** Noise, Terrace, Stamp, Unity brush shapes and rotation.
  - 4.3a as built (2026-10-25, compiled offline, stamp maths simulated, not yet run in Unity):
    - Component tabs Layers | Brushes | Settings. The Brushes tab runs the Scene tool while it is shown
      (`StyleTerrainBrush`): tool row Raise / Lower, Smooth, Flatten, Set Height; Size (m), Strength, Hardness,
      Height for Set Height (Shift-click takes it from the ground). Settings are per user (EditorPrefs).
    - A stroke records one undo of the TerrainData and the data asset (complete object), stamps every
      15 % of the brush size along the drag, writes with `SetHeightsDelayLOD` and syncs (collider, LOD) on
      mouse up. Patch bounds update only for the touched patches (`HBStyleTerrainData.UpdateBounds`), which
      bumps the version, so the quadtree and the layer preview follow during the stroke.
    - Stamp maths (`StyleTerrainStamp`, pure C#): falloff 1 inside Hardness, smoothstep to 0 at the rim.
      Raise adds 2 % of the brush size in metres × Strength per stamp; Smooth moves toward the 3 × 3 average,
      Flatten / Set Height toward the target, both by 50 % × Strength per stamp. Simulated: a 20 m brush at
      0.5 raises 0.20 m at the centre, 0 at the rim, symmetric; a 10 m step smooths into a ramp; flat ground
      stays flat; heights clamp to 0..range (a new flat terrain sits at 0, so Lower cannot dig below it).
    - Cursor: the brush circle drawn on the ground (inner ring for Hardness); orange while lowering.
    - User confirmed 4.3a (2026-10-25).
  - 4.3b as built (2026-10-25, compiled offline, paint mixing simulated, not yet run in Unity):
    - **Holes** (Shift fills): cells whose falloff is ≥ 0.5 are set, written with `SetHolesDelayLOD`; the holes
      texture and collider sync on mouse up (so the cut may show only when the mouse is released).
    - **Paint Layer** (Shift erases): hand paint lives in the data asset as a "Hand" RGBA8 Texture2DArray at the
      control resolution, one channel per **layer asset** (`m_HandLayers`), grown four channels at a time, up to
      16. The rules compute samples each row's own channel (row → channel by layer identity), so reordering a set
      never moves paint, and a layer removed from the set simply stops counting. Mix per stamp:
      every channel × (1 − a), painted channel + a, with a = falloff × Strength × 0.5; floor / ceil keep 8-bit
      steps from stalling (simulated: full cover at a = 0.01…0.25, erasing reaches 0, sum ≤ 100 %).
    - Painting edits the CPU copy through `GetPixelData` and pushes only the touched rectangle to the GPU
      (`Graphics.CopyTexture`), then restarts the preview, so it shows at once; Bake writes it into the control
      maps. Choosing Paint Layer turns Preview on. Clear Hand Painting zeroes the array (undoable).
    - Undo: holes record the TerrainData, painting records the hand array and the data asset (whole objects,
      so one paint undo can hold up to 64 MB at 2048² × 16 channels).
    - Brushes tab: tools in a 3 × 2 grid; Paint Layer lists the set's layers with colour strip and thumbnail.
    - Fixes after the 4.3b test (user: holes and falling objects work): holding the mouse still keeps stamping
      every 0.05 s (not for Holes); a ray that the collider misses (inside a hole) falls back to marching the
      heightfield (half-sample steps, 12 bisections; simulated exact against flat, hill side and hill top, sky
      rays miss), used by the brush and the picker; small brushes always reach at least one sample / texel,
      and the hole brush always sets the cell under the cursor; the Holes tool draws the hole-cell grid around
      the cursor (up to 32 cells each way).
  - 4.3c decisions and build (2026-10-26, compiled offline, maths simulated, not yet run in Unity). The user
    dropped the Stamp brush.
    - **Noise**: world-fixed value-noise fBm (3 octaves, −1…1) scaled like Raise; adds bumps and dips, Shift
      turns them over, the same place grows the same shapes. Setting: Noise Size (m).
    - **Terrace**: pulls toward step · (floor(h / step) + frac^6): flat treads, steep but continuous risers
      (simulated monotonic, no jumps). Setting: Step Height (m).
    - **Shapes** for every brush: thumbnail grid of our Round brush, Unity's built-in terrain brushes
      (loaded as `Brushes/builtin_brush_N.brush`; `UnityEditor.Brush` is internal, so its texture is read
      through its serialized fields) and Brush assets in the project (Refresh button). A shape is read once into
      a 128² mask (grey × alpha, alpha alone for Alpha8) and sampled bilinearly in the brush square; Rotation
      (degrees) turns it, a line on the cursor shows the shape's top. Hardness applies to Round only.
    - Tools grid 4 × 2: Raise / Lower, Smooth, Flatten, Set Height, Holes, Paint Layer, Noise, Terrace.
    - After the 4.3c test: icons were red and some brushes acted square — Unity's brush masks are single-channel
      (red) and several built-ins keep their round shape in the falloff curve, not the mask. Now the finished
      brush texture is taken from the internal `Brush.texture` (reflection; mask field as fallback), the mask
      uses the brightest channel × alpha, and the grid shows grey icons made from that mask. The Scene view
      draws the brush footprint on the ground (40 × 40 cells, brush colour, alpha = weight × 0.45) so shape,
      rotation and hardness are visible. Holes has no shape choice (round with Hardness, plus its grid).
  - 4.3 closed (2026-10-26, user: "devam edelim").
  - 4.4 + 4.5 decisions (2026-10-26):
    - **4.4 look** = the cheap set only: curvature shading (cavities darker, ridges lit; from the heights the
      normal already reads, +1 read), our brush-stroke feel (as in Lit, no reads), large-scale colour variation
      (macro variation, no reads). Detail distance and steep-slope side projection are **not** built.
    - **4.5 weather** = wetness and snow as in Lit (open ground from the shelter map stays dry under roofs and
      bridges; snow by slope and in patches, filling the low parts of the layer texture first), per-layer
      Wetness and Snow Cover on `HBStyleTerrainLayer`, then rain details (spots and ripples on flat ground,
      running water on steep slopes, fading at the Weather detail distance).
    - Terrain forward budget raised to **4500 slots / 175 reads** (from 3500 / 140).
    - Steps, one test each: **4.4** (all three looks, debug view Terrain/Curvature), **4.5a** wetness + snow +
      layer limits, **4.5b** rain details.
  - 4.4 as built (2026-10-26, compiled offline, not yet run in Unity):
    - The terrain material is made by the component, so the look lives in the data asset (`HBStyleTerrainLook`)
      and is edited on a new **Look** tab (Layers | Brushes | Look | Settings), bound per terrain by
      property block (`_HB_TerrainBrush`, `_HB_TerrainStyle`, `_HB_TerrainVariation`). Defaults = Lit's.
    - Brush strokes as in Lit (world space): Brush Size, Brush Paint (layer textures and layer borders are
      sampled at the stroke-shifted position; mip derivatives from the unshifted one), Tone Strokes, Relief,
      Shading / Ambient Break-up, Albedo Variation.
    - Curvature: Laplacian of the heights from the four normal taps plus the centre (+1 read), with the tap
      distance = max(texel, pixel footprint) so it does not shimmer far away (the normal uses the same taps).
      bend = clamp(Laplacian × 2 m, −1…1); albedo × (1 − bend × Curvature × 0.5). Analytic check: a 10 m deep,
      σ 10 m valley floor bends 0.4 (−10 % at Curvature 0.5), a 5 m, σ 5 m ridge −0.8 (+20 %); wide hills
      barely change. Debug view Terrain/Curvature: blue hollows, red ridges.
    - Macro variation: the old 24 m / 7 m two-octave noise, ±17.5 % brightness at 1.
  - 4.4 confirmed by the user (2026-10-26; whole frame 2.6 ms GPU in the editor with the terrain filling the view).
  - 4.5a as built (2026-10-26, compiled offline, not yet run in Unity):
    - The terrain always compiles the weather (`HB_WEATHER`; uniform branches skip it when dry) and calls Lit's
      `HB_ResolveSurfaceWeather` with a new ground path: no renderer-bounds edge delay, and the "height" delay
      comes from the blended layer texture height, so snow settles in the texture's low parts first. Lit passes
      a constant false, so its code and counts are unchanged.
    - `HBStyleTerrainLayer` gained Wetness and Snow Cover (0–1, default 1), blended by the layer weights
      (`_HB_LayerSurface.zw`). Brush strokes soften under snow; wet ground gets the film reflection and
      highlight as in Lit.
    - Rain spots and running water come with the shared function already (spots on flat ground, running water
      only very close on steep ground); ripples and terrain-scale tuning are 4.5b.
    - Debug views Weather/Shelter, Weather/Wetness and Weather/Snow work on the terrain.
  - After the 4.5a test (2026-10-26; 3.7 ms GPU frame in the editor with rain, precipitation and all weather on):
    the ground darkened far too much in rain — Lit's albedo^(1 + wet) turns dark grass ~5× darker. Decided: the
    ground path darkens linearly, at most 30 % (`HB_GROUND_WET_DARKEN`), gloss and film unchanged; rain spots and
    running water are dropped on the ground (15 cm spots and 5 cm flow lines do not read at terrain scale);
    Lit keeps all three. **4.5b is skipped** (no puddles or ripples on the terrain). Terrain weather is done;
    the remaining Phase 4 step is the terrain UI rework.
  - **4.6 UI rework** (2026-10-26, decided after studying the pre-v1 Liquid Terrain editor; compiled offline, not
    yet run in Unity):
    - Every tab opens with a guide card (`StyleTerrainGUI.Guide`): drawn icon, title, help text, shortcut chips,
      colour strip. Sections use coloured header bars: Layers green, Brushes blue (Shape) / orange (Surface),
      Look purple, Settings grey.
    - Brushes tab (`StyleTerrainBrushPanel`): a **Shape** palette (Raise, Smooth, Flatten, Set Height, Noise,
      Terrace) and a **Surface** palette (Holes, then one tile per layer with its texture, list colour and a
      shortened name) as drawn tiles, five per row. Clicking the active tile again (or Esc) puts the tool away,
      so Scene view selection works again; the state is per editor session. A Brush section with a preview (size
      on a log scale, strength, hardness or the rotated shape) and compact sliders; Brush Shape grid below;
      Clear Hand Painting at the bottom. Picking Paint turns Preview on.
    - Scene shortcuts while a tool is out: [ ] brush size ×/÷ 1.15, - = strength ∓0.05, Esc put away.
    - Scene label next to the cursor (dark box, tool colour): ground height (Raise / Lower, Smooth, Flatten with
      its target while stroking, Set Height with its target, Noise, Terrace with the step), Ground / Hole and
      Cut / Fill for Holes, layer name and the painted share under the cursor for Paint.
  - 4.6 test (2026-10-26): the user found everything working except rain and snow sinking into the terrain. Cause:
    precipitation lands on the shelter map, and the terrain is kept out of that map (so slopes do not shelter
    themselves), so drops fell to the ground and were cut by depth. Fix: the precipitation pass binds the
    heightmap of the terrain under the camera (`StyleTerrains.TryGetGround`, per property block) and
    `HB_DropGrounded` fades drops, flakes, far curtains and wind ribbons over the last 0.3 m above the ground,
    exactly as under roofs (+24 slots, +1 read in the precipitation shaders). Only the terrain under the camera
    counts.
  - **Phase 4 closed (2026-10-26)** by the user after the precipitation fix.
  - Gameplay query (user's footsteps; lands with layers in 4.2): `HBStyleTerrain` returns the strongest
    `HBStyleTerrainLayer` at a world point, and on request the point's 3 layers with weights, from a CPU copy of
    the control maps; 4.1 already brings height and normal queries.

## 9. Grass — Change

- **Painted** onto the terrain: a grass map stores density and height (strength = height), like
  set-height on Unity terrain. Layers may define the default grass type; painting can add or remove.
- GPU generated around the camera, crossed cards near, single card far, per-quality distance/density.
- Keep the **wind gust white sweep** that travels across the grass (user loves it).
- Brush pattern on blades and flowers. Grass benders (`HBStyleGrassBender`).
- Respects camera culling mask; never drawn by overlay cameras.

## 10. Liquids — Keep look, Change structure

- Look approved: depth gradient (colour keys = colour, alpha keys = how hidden the floor is, path-based),
  caustics, cellular foam, waterline veil, crest light and foam trails, wind-driven white caps.
- **Waves become mostly visual**: wave detail from small baked wave textures (normals, foam, crest light).
  Vertex displacement only **near the camera** and only at Medium+ quality; default surface is flat at far
  range. A flat or near-flat surface makes the underwater waterline trivial (big cost and bug saver).
- Liquid types as **layers** (`HBStyleLiquidLayer`: water / lava / acid presets) instead of materials.
- Connected water lives in the terrain family (sea level + painted lakes/rivers on the level map); isolated
  `HBStyleLiquidPool` boxes never join. Cutters (box/sphere/mesh, keep dry) and floaters (draft/tilt/lean)
  keep their behaviour. Public height query for gameplay.
- Underwater: tint, fog, shafts, caustics, bubbles; waterline exactly matches the rendered surface.

## 11. Transparency — Change

- Keep MBOIT (4 moments) with refraction inside it; all transparents are our shaders.
- **Transmission tint for every transparent type**, not only refraction: a "Tinted" blend mode that filters
  the background by colour (coloured glass without refraction), so transparents read as tinted, not faded.
- Blend modes: Alpha, Premultiply, Additive, Tinted, Refractive.
- Optimise: skip the whole OIT path when no transparent of ours is in view; refraction background mips and
  blur only when a refractive surface is visible; fewer blur taps; measure MRT bandwidth.
- MSAA unsupported (documented).
- Done: transparency ≤ 0.6 ms without refraction on the reference scene; refraction cost reported.

## 12. Cavity / edge highlight — New

Blender-viewport-style cavity: thin light lines on convex edges, darkening in concave corners.
- Screen-space pass from depth + normals (shared with SSAO's DepthNormals), half or full resolution by
  quality; ridge and valley strength, width in pixels, brush break-up; per-material opt-out.
- Done: reads well on low-poly props and buildings; ≤ 0.4 ms at full res 1080p.

## 13. Texture Studio — Change

Common framework first, then pages.
- **Shared base**: big resizable/fullscreen window (controls left, large preview centre, layer stack right),
  shared preset system (JSON, Revert / Save / Save As, like the old Terrain page) on **every** page, shared
  export naming and remembered folders, GPU (compute/blit) generation instead of CPU per-pixel loops.
- **Terrain**: import external textures as layers and edit them (levels, tint, blend, painterly filter),
  not sliders only.
- **Grass clump & flowers**: more detail and editability; brush pattern on blades/petals; **per-flower
  presets** (each variant picks a preset instead of random look); much faster generation.
- **Liquid**: presets + extra detail options. **Brush**: keep. **Cloud**: keep, presets; cloud types
  (cumulus, stratus, cirrus) if cheap. **Mask Map**: keep.

## 14. Shader Graph — Change

Node library for **Unlit** graphs, no template: HB Style Lighting (incl. specular/metallic), HB Brush,
HB Weather Static, HB Weather Moving, HB Atmosphere, HB Cavity. Each node brings its own keywords. Graph
shaders appear under `HB URP Style/`.

## 15. Tools

- **Bake Panel**: lighting defaults, presets, checks with one-click fixes, bake/cancel with report.
- **Billboard Baker**, **Debug window**, **Texture Studio**, terrain editing overlay (no separate terrain
  window).
- Long editor operations are asynchronous with progress; no multi-second freezes on simple edits.

## 16. Runtime API

- `HBStyleRenderSettings` (quality levels at runtime), `HBStyleEnvironment` (profile, weather, time),
  `HBStyleLiquid` height/depth/surface queries, terrain height query.
