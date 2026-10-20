# HiddenBull URP Style — Documentation

A shared Universal Render Pipeline toolkit for HiddenBull games: shaders, renderer features, volume
components and editor tools that give every project the same painterly look, with performance treated
as the first requirement.

## Requirements

| | |
|---|---|
| Unity | 6000.0 or newer (developed on 6000.4.1f1) |
| URP | 17.4.0 or newer |
| Rendering path | Forward+ |
| Target | PC and console |

## Start here

1. **[Getting Started](GettingStarted.md)** — install the package, add the renderer feature, set up the
   URP asset and the volumes, make your first material.
2. **[Materials](Features/Materials.md)** — the Lit, Foliage and Billboard shaders and every section of
   their inspectors.
3. **[Tools](Tools.md)** — Texture Studio, Billboard Baker, Terrain Layers and the Bake Panel.

## Features

| Page | What it covers |
|---|---|
| [Materials](Features/Materials.md) | Lit, Foliage, Billboard, Decal and Particle shaders |
| [Weather](Features/Weather.md) | Rain, snow, wetness, shelter under roofs, Static and Moving surfaces |
| [Terrain](Features/Terrain.md) | HB Style Terrain, automatic layers, GPU grass, terrain trees |
| [Billboards](Features/Billboards.md) | GPU-drawn plants: meshes up close, billboards far away |
| [Liquids](Features/Liquids.md) | Sea, lakes and rivers, pools, cutters, floaters, underwater |
| [Sky, Fog and Clouds](Features/SkyFogClouds.md) | Time of day, sky gradients, volumetric fog, clouds |
| [Shader Graph](Features/ShaderGraph.md) | The HB node library and the HB Lit graph template |
| [Lighting and Shadows](Features/Lighting.md) | Light, ambient, mixed lighting and shadow settings |
| [Brush](Features/Brush.md) | The brush atlas and the painterly stroke layer |

## Reference

- **[Performance](Performance.md)** — quality settings, what each system costs, build stripping.
- **[Known Limitations](KnownLimitations.md)** — what is not supported, and why.
- **[Roadmap](Roadmap.md)**.

## Installation

Add to your project's `Packages/manifest.json`:

```json
"com.hiddenbull.urpstyle": "https://github.com/rytiex/HiddenBull-UrpStyle.git?path=Package"
```
