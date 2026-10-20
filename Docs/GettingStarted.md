# Getting Started

## 1. Install

Add the package to `Packages/manifest.json` (see the [README](README.md#installation)) and let Unity
import it.

## 2. Add the renderer feature

Select your **Universal Renderer Data** asset and add the **HiddenBull Style** renderer feature. It owns
every per-camera pass of the package: style globals, shelter (sky occlusion) maps, fog, grass,
transparency, rain and snow, waves and underwater, and the GPU billboard system.

Without the feature, the shaders still render, but weather, fog, grass and billboards are off.

The feature's inspector holds the project-wide quality settings:

| Section | What it controls |
|---|---|
| Brush | Stroke size per tile, fade distances, randomize, grow with distance |
| Clouds | Global cloud settings |
| Shadows | Shadow quality, distance, softness, contact hardening, brush on shadows, debug views |
| Grass | Off / Low / Medium / High / Ultra, each with its own distance, density and translucency |
| Precipitation | Off / Low / Medium / High / Ultra share of falling drops and flakes, far layer on or off |
| Liquid Quality | Low / Medium / High detail of every liquid |
| Fog Debug, Weather Debug | Debug views (Sky Exposure, Wetness, Snow, Limits, Soak) |

## 3. URP asset and renderer

| Setting | Value |
|---|---|
| Rendering Path | Forward+ |
| Depth Texture | On |
| Depth Priming Mode | Forced; the renderer feature sets it on its renderer automatically |
| MSAA | Off (depth priming does not work with MSAA) |
| LOD Cross Fade | On (used by the billboard system's fades) |
| Light Probe System | Adaptive Probe Volumes recommended |

## 4. Volumes

Add a Global Volume and add the package's overrides from **Add Override → HiddenBull**. Each one is
optional; missing overrides use their defaults. Give the scene a skybox material with the
**HB URP Style → Sky** shader so the sky matches the volumes.

| Override | Purpose |
|---|---|
| HB Style Sky | Day, dusk and night sky gradients and ambient light |
| HB Style Celestial | Sun, moon and stars |
| HB Style Clouds | Cloud coverage and shading |
| HB Style Fog | Height fog with sun scattering, per time of day |
| HB Style Weather | Rain, snow, wind and the wet / dry / settle / melt / cover / uncover times |

The sun is driven by the **HB Style Sun** component (Add Component → HiddenBull → HiddenBull Style →
Environment), which is added to your directional light. Its **Time Of Day** and **Heading** move the
sun; the sky, fog and lighting follow.

## 5. First material

Create a material and pick **HB URP Style → Lit**. With no textures it is already lit in the style; turn
on Maps, Brush, Weather and the other sections as needed. See [Materials](Features/Materials.md).

## Components

All components live under Add Component → HiddenBull → HiddenBull Style:

| Category | Components |
|---|---|
| Vegetation | HB Style Billboard, HB Style Grass Bender |
| Liquid | HB Style Liquid Terrain, HB Style Liquid Pool, HB Style Liquid Cutter, HB Style Liquid Floater |
| Environment | HB Style Sun, HB Style Weather Mask |
| Terrain | HB Style Terrain |
