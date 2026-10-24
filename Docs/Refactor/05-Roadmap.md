# 05 — Roadmap

Every phase: **design discussion → implement the agreed batch → offline compile + cost report → one test
list for the user → fixes**. A phase is done when the user confirms the visual check and its cost is
written down in `Docs/`.

| Phase | Content | Deliverable / check |
|---|---|---|
| **0 Foundation** | Branch `refactor/v1`; empty `Package/` rebuilt (asmdefs, folders, naming); HLSL Core module + globals CBUFFER; `HBStyleFeature` skeleton with per-camera policy (base / overlay / preview), pruned camera state; `HBStyleQualityProfile`; Debug window; `HBStyleEnvironmentProfile` + `HBStyleSun` + `HBStyleEnvironment` API (data only); offline compile tooling scripts in the repo (`Tools~/`) | Empty scene renders through the feature with no cost; overlay camera test scene in the sandbox |
| **1 Lit core** | Lighting, Shadow (presets, guard, debug), Brush modules; `HB URP Style/Lit` with sections; DepthOnly/DepthNormals; Depth Priming forced; SRP Batcher + GRD compatible cbuffer | Lit within budget; rock with one light and no textures looks right; shadows debug views |
| **2 Atmosphere + Sky** | Atmosphere module (height fog, horizon/aerial perspective, sun inscatter) used by Lit; Sky shader (gradients, sun/moon, stars+, aurora, clouds with better translucency, cloud atlas in profile); environment zones | Mountains recede in layers, sea horizon clean; profile blending at runtime |
| **3 Foliage + Billboards** | Foliage canopy bake tool (vertex attributes), translucency, back-side softening, up-face snow later in P7; port billboard system (GPU instancing, compute cull, cross-fade, FOV distances, picking, counter) | Merged and separate bushes look the same; tall bushes not black |
| **4 Terrain** | `HBStyleTerrain`: heightmap asset, clipmap rendering, hidden TerrainData collider, import from Unity TerrainData; layers on component; rule bake + hand paint; editing overlay (sculpt, paint) ; curvature; plant painting into billboard system | Sandbox scenes moved to our terrain; no basemap artefacts; cost report |
| **5 Grass** | Painted grass map (density + height); GPU generation; wind white sweep; benders; brush on blades/flowers | Paint grass on/off and height; ≤ 2 ms Ultra meadow |
| **6 Liquids** | Liquid layers (water/lava/acid); terrain level map (sea + lakes + rivers); visual waves (baked wave textures), near-only displacement by quality; depth gradient, caustics, foam, crest; pools; cutters; floaters; height query; underwater with exact waterline | Approved look preserved; cost report; waterline seamless |
| **7 Weather** | Shelter near/far with roof margin dilation; weather zone (indoor box); surfaces (Static/Moving); precipitation (procedural, snowflake texture, budgets); presets and API transitions | No indoor leaks in sandbox houses; timings; budgets met |
| **8 Transparency** | MBOIT with Tinted / Refractive / Alpha / Premultiply / Additive; skip when none visible; refraction optimisation; colored shadows; particles | ≤ 0.6 ms without refraction; tinted glass reads as glass |
| **9 Cavity + Decal** | Screen-space cavity pass; decal shader | Edge highlights on props/buildings ≤ 0.4 ms |
| **10 Texture Studio** | Shared framework (fullscreen, presets everywhere, GPU generation); Terrain import/edit; Grass & per-flower presets; Liquid/Cloud presets | Fast generation, consistent UI |
| **11 Shader Graph** | Node library: Lighting, Brush, Weather Static/Moving, Atmosphere, Cavity | Unlit graph matches Lit look |
| **12 Finish** | Runtime settings API (`HBStyleRenderSettings`), Bake Panel, variant stripping (provable only), docs, golden screenshots, final Profiler measurement in a build | Docs complete; measured cost table |

Order rationale: the core (0–2) must be right before anything is built on it; terrain (4) comes before
grass, liquids and weather because all three read terrain data.

## Test assets in the sandbox

Keep or create: a lighting test (rocks, no textures), a village (houses with roofs and interiors for
weather leaks), a terrain with sea, lake and river, an FPS player with an overlay hands camera, a glass and
particle test, a foliage set (separate and merged bushes, tall hedges), and a busy combined scene for cost.
