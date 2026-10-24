# 06 — Old Package Reference (main branch, before v1)

The old `Package/` (≈ 172 source files, ≈ 46 500 lines) is the reference for **look and proven maths**, not
for structure. Read a file when rebuilding its system; port the idea, not the code layout. Line counts show
where the weight was.

## Shader library (`Runtime/Shaders/Library`)

| File | What is worth reusing | Notes for v1 |
|---|---|---|
| StyleLighting.hlsl (400) | `HiddenBullFragmentLit`: wrapped diffuse, half tone, ambient mix, specular/rim, extra light loop, shadowmask | Core of the Lighting module; split debug out |
| StyleShadow.hlsl | Contact-hardening filter, `HB_MainLightShadow`, empty-map guard, debug modes | Keep 15 m reach; filter chosen once per pixel |
| StyleBrush.hlsl (406) | 2-plane triplanar, octave loop, stroke offset, normal shift | Brush module as is, cleaned |
| StyleAmbient.hlsl, StyleSky.hlsl | Gradient ambient, sky gradients, sun/moon | Move parameters to the profile CBUFFER |
| StyleFog.hlsl (391) | Fog colour/sun scattering terms | Volumetric froxel parts are dropped |
| StyleClouds.hlsl | Stepped painterly cloud shading | Improve translucency |
| StyleSkyExposure.hlsl, StyleShore.hlsl, StyleWeatherSurface.hlsl (547) | Approved snow/wet look (pre-refactor ffb0d53^ version), detail fade ≤ 30 m | Weather module; add roof-margin input |
| StyleWaves.hlsl (418) | Gerstner cascades bake, depth-scaled waves | Visual-only waves in v1 |
| StyleTransparency.hlsl, StyleShadowTint.hlsl | MBOIT moments/resolve, front/behind split, tint atlas | Keep model (see pitfalls), add Tinted mode |
| StyleCanopy.hlsl | Canopy normal from bounds | Replaced by baked canopy attributes |
| StyleBillboardInstancing.hlsl | Procedural instancing setup | Port as is |

## Shaders

| Folder | Notes |
|---|---|
| Lit (Lit.shader 487, LitInput 443) | One cbuffer superset caused BRG errors in Foliage; v1 uses per-family cbuffers |
| Foliage | Wind, rest anchor, translucency |
| Terrain (TerrainArrays 478) | Texture-array top-3 picks, hex tiling, rules — port to own terrain |
| Grass (StyleGrass.compute 429) | GPU generation, wind sweep — port; input becomes the painted grass map |
| Liquid (LiquidPass 1221, LiquidInput 1134) | Approved look; very large — rebuild leaner |
| Weather (StyleShelter.compute 593, StylePrecipitation.shader 452) | Shelter follow + fade; procedural rain/snow |
| Fog | Froxel volumetric — **drop** |
| Sky (GradientSky) | Port and extend |
| Transparency | Composite — keep model |
| Billboard, Particle, Decal | Port |

## C# runtime (`Runtime/Core`, `Runtime/Rendering`)

| File | Notes |
|---|---|
| HBStyleFeature.cs | Pass order, overlay-camera early return, depth priming forcing — port the policy |
| StyleSkyOcclusion.cs (1707) | Shelter maps; far too large — rebuild around the per-camera policy |
| StyleWavesPass.cs (852), HBStyleLiquidTerrain.cs (1277), HBStyleLiquidTerrainData.cs (620), HBStyleLiquid.cs (643) | Liquid system — reuse clipmap/CDLOD grid logic (lattice + morph, never relocate border vertices) for terrain and liquid |
| StyleBillboardSystem.cs (808) | Port nearly as is |
| StyleGrassPass.cs (639) | Port; culling mask check |
| StyleGlobalsPass.cs (653) | Replace with one CBUFFER upload |
| StyleTransparencyPass.cs, StyleTransparentShadows.cs | Port with skip-when-empty |
| StyleFogPass.cs | Drop |
| HBStyleRenderSettings.cs | Port (runtime quality API, editor restore on Play exit) |
| HBStyleShadowQualitySettings.cs | Becomes a Quality profile section |
| HBStyleTerrain*.cs | Drop with Unity terrain; reuse rule-bake / hand-paint maths |
| Volume/* (Sky, Celestial, Clouds, Fog, Weather) | Become profile sections |

## Editor

| File | Notes |
|---|---|
| StyleLiquidTerrainEditor.cs (2027) | Painting tools — base for the shared terrain/liquid editing overlay |
| StyleTerrainWindow.cs (1281) | Rule histogram UI — move onto the component inspector |
| LitShaderGUI.cs (1157) | Section headers, reset actions — rebuild smaller per family |
| Authoring/* (Texture Studio pages, bakers) | CPU-heavy generation — move to GPU; shared page base |
| Billboard/* | Baker, picking, inspector counter — port |
| Bake/StyleBakePanel.cs | Port |
| ShaderGraph/ (HBShaderGraph.hlsl, Sub Graphs) | Sub Graph JSON format notes below |

## Shader Graph notes (from the old node library)

- Sub Graph files are multi-JSON documents. Enum keyword with an empty entry → "_"; names join as
  `ref_entry`. KeywordDefinition 1 = MultiCompile, Scope 1 = Global, Stages 2 = Fragment.
- SubGraphNode slot id = XOR of the four int32 values of the property GUID bytes; sub graph fileID
  `-5475051401550479605`.
- A graph's shader path must not start with "Hidden".
