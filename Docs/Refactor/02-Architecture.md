# 02 — Target Architecture

## 1. Layers

```
Data (ScriptableObjects)          Scene (components)              Rendering (one renderer feature)
-------------------------         -----------------------         --------------------------------
HBStyleEnvironmentProfile   --->  HBStyleEnvironmentController --> StyleFrameGlobals (one CBUFFER upload)
                                  (time, weather, profile)
HBStyleQualityProfile       --->  HBStyleFeature (renderer)   --> pass scheduler (per camera, base only)
HBStyleTerrainLayer (+Liquid)     HBStyleTerrain / Liquid     --> terrain + liquid clipmap draw
HBStyleBillboardAsset             HBStyleBillboard            --> GPU instancing system
                                  HBStyleEnvironmentZone      --> CPU profile blend (no Volume stack)
```

Shaders compose **modules** from one HLSL library:

```
Core      : common maths, packing, globals CBUFFER, camera helpers
Lighting  : wrapped diffuse, half tone, gradient ambient, specular/rim, extra lights, shadowmask
Shadow    : main-light filter (contact hardening ≤ 15 m), empty-shadowmap guard, debug
Brush     : atlas sampling (≤ 2 triplanar planes, 1–2 octaves), stroke offset, normal shift
Atmosphere: height fog + horizon/aerial perspective + sun inscatter (analytic, shared by sky and surfaces)
Weather   : sky exposure (shelter), wetness, snow, drops/ripples (detail ≤ 30 m)
Cavity    : edge highlight / cavity darkening (reads the cavity buffer)
Surface   : per-shader-family inputs (Lit, Foliage, Terrain, Grass, Liquid, Billboard, Particle, Decal)
```

A shader includes only the modules it uses; features inside a module are `shader_feature_local` and cost
nothing when off. **Modules never include each other in cycles; Surface → modules → Core.**

## 2. Renderer feature: `HBStyleFeature`

One `ScriptableRendererFeature`, Render Graph only. It holds references to the **Quality profile** and the
internal shaders/computes (auto-filled in editor, serialized for builds). It owns the pass scheduler.

### Per-camera policy (decided up front)

| Camera | What runs |
|---|---|
| Base Game / Scene camera | everything enabled by quality |
| **Overlay camera** (FPS hands etc.) | globals only + transparency for its own transparents; **no** world passes (grass, precipitation, shelter, liquid prepasses, cavity of the world). World objects it draws use the base camera's global textures. |
| Preview / Reflection camera | nothing that touches per-camera state; never advances simulations |
| Any camera | GPU-generated content (grass, billboards, precipitation) respects the **camera culling mask** via the owner's layer |

Per-camera state lives in a `StyleCameraState` keyed by camera, created lazily and **pruned** when a camera
has not rendered for 2 s. Never key or gate anything on `Time.frameCount` in edit mode.

### Pass list (in order)

| Pass | Event | Runs when |
|---|---|---|
| Globals (frame CBUFFER, environment, brush, quality) | BeforeRenderingShadows (must be before shadow casters) | always |
| Shelter (top-down sky depth, near + far, with margin dilation) | BeforeRenderingShadows | weather active |
| Liquid prepass (wave textures, caustics) | BeforeRenderingOpaques | a liquid is visible |
| Terrain/Grass culling (compute) | BeforeRenderingOpaques | terrain visible |
| Cavity (screen-space, from depth-normals) | AfterRenderingPrePasses | cavity quality > Off |
| Opaques (URP) — every surface shader applies atmosphere itself | — | — |
| Precipitation (procedural) | AfterRenderingSkybox | weather active |
| Transparency (MBOIT + composite, refraction only if a refractive surface is visible) | BeforeRenderingTransparents | a transparent of ours is visible |
| Underwater (full screen) | BeforeRenderingPostProcessing | camera is below/at a liquid surface |

There is **no full-screen fog pass**: atmosphere is evaluated in each surface shader and the sky shader
(depth priming makes opaque shading once per pixel).

### Depth priming

The feature sets its renderer's **Depth Priming Mode = Forced** in the editor (asset write), because dense
foliage overdraw is the biggest opaque cost. Every opaque shader has DepthOnly + DepthNormals passes.

## 3. Quality: `HBStyleQualityProfile`

One ScriptableObject referenced by the feature; every module has the same pattern as the old Grass setting:
a level enum (Off / Low / Medium / High / Ultra) plus per-level parameters.

| Module | Per-level parameters |
|---|---|
| Shadows | distance, cascades, resolution, softness, taps (contact-hardening reach is fixed at 15 m on every level) |
| Grass | distance, density, translucency |
| Precipitation | particle share, far layer, max triangles |
| Liquid | wave texture size, caustics, foam detail, near displacement on/off |
| Atmosphere | horizon layers on/off, sun inscatter on/off |
| Cavity | off / half res / full res |
| Billboards | distance multiplier |
| Weather | shelter resolution, detail distance |

`HBStyleRenderSettings` (runtime struct, `Get()` / `Set()`) switches levels at runtime for game settings
menus and restores authored values when Play mode ends in the editor.

**Debug** is not on the feature: an editor window `Tools/HiddenBull/URP Style/Debug` sets one global debug
mode (Shadows: attenuation, unfiltered, blockers, cascades, filter, radius; Weather: exposure, wetness, snow,
limits; Terrain: layer map; Atmosphere: fog amount; Liquid: depth/foam). Debug code is compiled only under
an editor-only keyword stripped from builds.

## 4. Environment: profile instead of Volumes

`HBStyleEnvironmentProfile` (ScriptableObject) groups: **Brush** (global atlas, world scale, distance growth,
patches), **Sky** (day/dusk/night gradients, ambient; the ambient part lands in Phase 1),
**Celestial** (sun, moon, stars, aurora), **Clouds** (atlas, coverage, shading), **Atmosphere** (height fog,
horizon), **Weather style** (timings, rain colour, cloud response; the rain / snow / wind values themselves live on
the controller, decided 2026-10-24).

- `HBStyleEnvironmentController` (was `HBStyleSun`, renamed in Phase 3) on the directional light: time of day, sun
  heading, rain, snow, wind, wind heading, profile reference. It is the single owner of environment state and
  uploads it once per frame into the globals CBUFFER.
- `HBStyleEnvironment` (static runtime API): `SetProfile`, `BlendTo(profile, seconds)`,
  `SetWeather(rain, snow, wind, seconds)`, `TimeOfDay`, events for gameplay.
- `HBStyleEnvironmentZone` (box, priority, blend distance, profile): local overrides (caves, interiors,
  biomes). CPU-side blend only when the camera is inside/near a zone; no per-frame Volume stack evaluation.
- Static scenes: assign a profile and forget. Dynamic scenes: drive the API. Same data in both.
- Decided 2026-10-24 (2c): a zone takes over **chosen sections** of its profile (Brush, Sky, Celestial, Clouds,
  Atmosphere). Layers per base camera: scene profile → API `BlendTo` → zones by priority, each lerped per section;
  overlay cameras use their base camera's position. The sun's **key light follows `Camera.main`'s mix**, or, with no MainCamera tag, the last rendered active
  base Game camera that draws to the screen (cameras with a target texture are skipped). Atlases that
  cannot blend switch at the halfway point; different cloud atlases dip coverage to zero at that point. Each
  profile's LUT is baked once and cached; a mixed LUT is rebuilt only when its inputs change.
- Editor-only previews are allowed to run in Edit mode; anything expensive may run only in Play mode.

## 5. Terrain and Liquid family

```
HBStyleTerrain (component)
 ├─ heightmap (R16 texture asset, editable)            ← import once from Unity TerrainData
 ├─ layers: HBStyleTerrainLayer[] (albedo/normal/mask, tiling, rules, weather limits, grass type)
 ├─ control maps (texture array, top-3 picks per pixel)  ← painted + rule-baked
 ├─ grass map (density/height channel, painted)
 ├─ plant instances (billboard system)                   ← painted
 ├─ liquid: HBStyleLiquidLayer[] + level map (sea level + painted lakes/rivers)
 └─ collider: hidden TerrainData + TerrainCollider synced from the heightmap (fast, robust physics)
```

- Rendering: GPU-instanced CDLOD clipmap around the camera (one grid mesh, vertex fetch of height), shared
  by terrain and liquid. No Unity Terrain renderer, no basemap, no tree distance hacks.
- Liquid depth/shore come straight from the terrain heightmap and level map — no separate seabed baking.
- Isolated pools stay a separate simple component (`HBStyleLiquidPool`, box) that never joins terrain water.
- Editing: a Scene-view tool overlay (Raise/Lower, Smooth, Flatten, Paint Layer, Paint Grass, Paint Plants,
  Paint Water) on the terrain component; the layer list lives **on the same component** (no separate window).

## 6. Module data flow per frame

1. `HBStyleEnvironmentController` resolves environment (profile + zones + API transitions) → globals struct.
2. Feature Globals pass uploads one CBUFFER (environment, brush, quality, weather state, time).
3. Shelter pass updates near/far sky-depth maps for the base camera (dilated by the roof margin).
4. Terrain/grass/billboard computes cull into indirect args.
5. Opaque shaders: Surface → Lighting (+Shadow, Brush, Weather, Cavity) → Atmosphere.
6. Transparents via OIT; precipitation; underwater.

## 7. Assemblies

`HiddenBull.UrpStyle.Runtime`, `HiddenBull.UrpStyle.Editor`, `HiddenBull.UrpStyle.ShaderGraph` (nodes only).
Every asmdef reference must be explicit (the offline compile does **not** enforce asmdef references — check
by hand before using a type from a new assembly).
