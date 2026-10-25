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
