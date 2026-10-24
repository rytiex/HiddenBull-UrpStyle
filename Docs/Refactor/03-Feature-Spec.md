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

### Billboard system — Keep (worked well)
- GPU instancing, compute culling (frustum + sun-extruded shadow casters), mesh near / 8-view billboard far,
  dithered cross-fade, FOV-aware distances (60° reference), probe lighting, editor picking, inspector counter.
- Works with terrain plant painting of our own terrain (not Unity trees).

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
- Precipitation: GPU procedural quads (no CPU mesh), **snowflake texture**, rain streaks, far layer; hidden
  under roofs / behind glass; triangle budget per quality level (Ultra ≤ ~150k triangles drawn).
- Weather state is driven by the Environment profile / API (rain, snow, wind, wet/dry/settle/melt/cover/
  uncover times, Instant). Presets: Clear, Drizzle, Rain, Storm, Light Snow, Blizzard, Sleet.
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
