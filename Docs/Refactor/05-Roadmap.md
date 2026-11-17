# 05 — Roadmap

Every phase: **design discussion → implement the agreed batch → offline compile + cost report → one test
list for the user → fixes**. A phase is done when the user confirms the visual check and its cost is
written down in `Docs/`.

| Phase | Content | Deliverable / check |
|---|---|---|
| **0 Foundation** | Branch `refactor/v1`; empty `Package/` rebuilt (asmdefs, folders, naming); HLSL Core module + globals CBUFFER; `HBStyleFeature` skeleton with per-camera policy (base / overlay / preview), pruned camera state; `HBStyleQualityProfile`; Debug window; `HBStyleEnvironmentProfile` + `HBStyleSun` + `HBStyleEnvironment` API (data only); offline compile tooling scripts in the repo (`Tools~/`) | Empty scene renders through the feature with no cost; overlay camera test scene in the sandbox |
| **1 Lit core** | Lighting, Shadow (presets, guard, debug), Brush modules; `HB URP Style/Lit` with sections; DepthOnly/DepthNormals; Depth Priming forced; SRP Batcher + GRD compatible cbuffer | Lit within budget; rock with one light and no textures looks right; shadows debug views |
| **2 Atmosphere + Sky** | Atmosphere module (height fog, horizon/aerial perspective, sun inscatter) used by Lit; Sky shader (gradients, sun/moon, stars+, aurora, clouds with better translucency, cloud atlas in profile); environment zones. Split (2026-10-24) into **2a** key light + sky port + atmosphere in Lit, **2b** fog split (own-colour Fog + sky-colour Horizon), stars+, aurora, cloud translucency, Environment and Quality profile inspectors (section headers, per-section Reset), **2c** zones + runtime profile blending, each with its own test list | Mountains recede in layers, sea horizon clean; profile blending at runtime |
| **3 Weather** | Shelter near/far with roof margin dilation; weather zone (indoor box); surfaces (Static/Moving) in Lit; precipitation (procedural, soft snow dots, budgets); presets and API transitions; `HBStyleSun` renamed to `HBStyleEnvironmentController`, which holds rain / snow / wind / wind heading next to the time of day; the profile's Weather section holds style only (timings, rain colour, Cloud Response); rain and snow raise cloud coverage and overcast. Rebuilt in six steps after the first batch failed (2026-10-24): shelter map + debug view, wetness, snow, rain details, Moving drops and flakes, precipitation (6a near layer, 6b far layer, 6c splashes (removed), 6d wind streaks); one test per step | No indoor leaks in sandbox houses; timings; budgets met |
| **4 Terrain** | `HBStyleTerrain`: heightmap asset, clipmap rendering, hidden TerrainData collider, import from Unity TerrainData; layers on component; rule bake + hand paint; editing overlay (sculpt, paint) ; curvature; weather on terrain (terrain does not shelter itself, ripples on flat ground) | Sandbox scenes moved to our terrain; no basemap artefacts; cost report |
| **5 Grass** | Painted grass map (density + height); GPU generated leaves; wind gust; flowers; benders; weather on grass; gradual weather after a roof is removed | Paint grass on/off and height; ≤ 2 ms Ultra meadow (measured 2.1 ms in the editor) |
| **7 Foliage + Billboards** (ran before 6) | Foliage and Foliage Body shaders, Bake Plant (canopy + billboard), Sway + Flutter, always-Static weather; `StylePlantSystem` (compute cull, view and shadow lists, cross-fade, FOV distances, picking, counter); sun-view shadow atlas; Hi-Z occlusion for plants and grass; Plants palette on our terrain; colliders; harvest API | Merged and separate bushes look the same; tall bushes not black |
| **6 Liquids** | Liquid layers (water/lava/acid); terrain level map (sea + lakes + rivers); visual waves (baked wave textures), near-only displacement by quality; depth gradient, caustics, foam, crest; pools; cutters; floaters; height query; underwater with exact waterline | Approved look preserved; cost report; waterline seamless |
| **8 Transparency** | Sorted forward (MBOIT replaced) with Tinted / Refractive / Alpha / Premultiply / Additive; nearest-glass split for rain and refraction; skip when none visible; blurred refraction pyramid; coloured shadows; particles; Transparency quality levels | ≤ 0.6 ms without refraction (measured in Phase 12); tinted glass reads as glass |
| **9 Cavity + Decal** | Screen-space cavity pass; decal shader | Edge highlights on props/buildings ≤ 0.4 ms |
| **10 Texture Studio** | Shared framework (fullscreen, presets everywhere, GPU generation); Terrain import/edit; Grass & per-flower presets; Liquid/Cloud presets | Fast generation, consistent UI |
| **11 Shader Graph** | Node library: Lighting, Brush, Weather Static/Moving, Atmosphere, Cavity | Unlit graph matches Lit look |
| **12 Finish** | Runtime settings API (`HBStyleRenderSettings`), Bake Panel, variant stripping (provable only), docs, golden screenshots, final Profiler measurement in a build | Docs complete; measured cost table |

Order rationale: the core (0–2) must be right before anything is built on it; terrain (4) comes before
grass and liquids because both read terrain data.

**Last step of Phase 4 (user, 2026-10-25):** a much better terrain UI. The current component inspector works, but
the pre-v1 Liquid Terrain editor UI was clearly better; study it and bring the terrain tools up to that level when
terrain is otherwise finished.

**After Phase 4, before Phase 5 (user, 2026-10-25):** fog needs a couple of fixes — its silhouette feature and its
brush feel were lost in the rebuild. Look at the pre-v1 fog first to see what produced them. Done with a
full-screen atmosphere pass; confirmed by the user on 2026-10-27.

**Phase 5 plan (user, 2026-10-27):** 5.0 Weather Mask back (dome stamped into the shelter map, Block
Precipitation); 5.1 grass generation, drawing, per-layer grass and the Grass brush; 5.2 look; 5.3 flowers and
benders; 5.4 weather on grass. Details in `03` §7 and §9.

**Phase 5 closed (user, 2026-11-01).** Final state in `03` §9, costs in `08`. Grass moved from cards to real leaves on
the way; the phase also brought Wet Gloss, pressed snow, a bender trail independent of grass and the roof cover map.

**Phase 7 closed (user, 2026-11-01).** The user moved it ahead of Phase 6, so the phase numbers stay and the table
shows the order of work. Final state in `03` §4, costs in `08`. Phase 7 ended with no mid LOD: dense mesh trees are
avoided by hand for now (path written in `03` §4). Colliders, harvest and the billboard leaf light fix were compiled
offline and not run in Unity at close, so the first Phase 6 session in the sandbox should glance at them.

**Phase 8 closed (user, 2026-11-02).** It ran before Phase 6 too. The MBOIT build was replaced by sorted forward
transparency on the way. Final state in `03` §11, offline costs in `08`. The overlay camera case was not tested by the
user. Before Phase 6 comes a polish batch the user agreed on 2026-11-02: the Moving weather rebuild, grass Blade
Detail per quality level, then custom grass texture cards per terrain layer.

**Phase 6 closed (user, 2026-11-15).** Final state in `03` §10 and the shelter block at the top of `03` §7, costs
in `08`. On the way the old liquid look was ported back (2026-11-09), screen-space reflections were added for every
glossy surface, and the shelter system was rewritten as one cascaded map because the near and far maps could not
agree. The phase also brought shore wetness, movement trails, the cutter Roof option and the Box shape of the
Weather Mask. The liquid optimisation step was not run: it needs a measuring tool, and the cost measurement stays
in Phase 12. Next by the table: Phase 9 Cavity + Decal.

**Phase 9 closed (user, 2026-11-16).** Final state in `03` §12 (cavity), the Decal block of `03` §4 and the Grass
Clear / wet film blocks of `03` §9; costs in `08`. On the way the user reversed the decal weather choice (decals sit on
top of snow), matte surfaces got a decal sheen, grass reflects with the ground's formula, and `HBStyleGrassClear`
was added for buildings on grass. After the phase the user decided the order of what is left: **10 Texture Studio, 11
Shader Graph, 12 Finish**, because the package is not complete before 10 and 11. No Sandbox build: the user will put
the package into their own game and measure there.

**Phase 10 closed (user, 2026-11-17).** Final state in the first block of `03` §13; the liquid change it brought is the
Phase 10 block of `03` §10; costs in `08`. On the way the Terrain page became a Substance-style Material page (layers,
modules, masks, emission), texture import was dropped, Grass, Flower, Brush and Cloud got no presets, liquids moved to
Studio-made mask maps (runtime plate bake and Shore Crust removed), and the Mask Map packer was dropped because every
texture of the game comes from the studio. Next by the table: Phase 11 Shader Graph.

**Fixes after Phase 10 (2026-11-17)**, listed at the end of `03` §13: cavity dead zone and mesh-only cavity, Brush
Paint 0.25, terrain specular, wet tone match, shore wetness band, SSR self-hit lift, two terrain editor fixes. Open
before Phase 11: wet grass blades brighter than the ground under them with SSR on.

Swapped on 2026-10-24 (Phase 3 test): Weather moved from 7 to 3 and Foliage + Billboards from 3 to 7. Weather is a
module every later surface family uses (terrain, grass, liquids, foliage snow), so each family gets it from the start
instead of a retrofit; the terrain-specific weather parts land with Terrain. The first Foliage + Billboards build was
reverted; its findings are kept as open points for the Phase 7 discussion.

## Test assets in the sandbox

Keep or create: a lighting test (rocks, no textures), a village (houses with roofs and interiors for
weather leaks), a terrain with sea, lake and river, an FPS player with an overlay hands camera, a glass and
particle test, a foliage set (separate and merged bushes, tall hedges), and a busy combined scene for cost.
