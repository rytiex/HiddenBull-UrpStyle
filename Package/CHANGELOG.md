# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.5.0] - 2026-09-22

First working version: the shader core, the ambient model and the brush layer.

### Added

**Shader core**

- Shared HLSL lighting library. Every shader family in the package routes through it, so the style
  cannot drift between them.
- `HiddenBull/URP Style/Lit` master shader with ForwardLit, ShadowCaster, DepthOnly, DepthNormals
  and Meta passes. Albedo-only is the default path and samples no textures; base map, normal map,
  specular and rim are each behind a local shader feature.
- Analytic wrapped diffuse: two parameters shape the whole light-to-shadow transition, with no
  texture fetch and no keyword.
- `Shadow Terminator Fade` material parameter, which hides self-shadowing acne near the terminator.
- `LitShaderGUI` material inspector, hiding texture slots until they are enabled.

**Ambient**

- Three-zone gradient ambient (sky / horizon / ground) blended along the surface normal, on a
  `StyleAmbient` volume override, with a blend weight toward baked lightmaps and probes.
- `HiddenBullStyleFeature` renderer feature, publishing the style's globals per camera through
  Render Graph.

**Brush**

- Brush atlas generator (`Tools > HiddenBull > URP Style > Brush Atlas Generator`): strokes
  synthesised from parameters with a live preview, four presets, a coverage readout, and a seamless
  16-bit PNG export with its import settings configured.
- Four modulation layers, all baked so none costs anything at runtime: bristles, edge break-up,
  pigment density and canvas grain.
- Brush layer in the master shader, behind `_HB_BRUSH`. Triplanar, anchored in
  world or object space at one shared scale, doing nothing until an atlas is assigned. Breaks up the
  terminator, varies albedo, perturbs the shading normal, and shifts colour temperature.

**Other**

- Scene lighting validation reporting Shadowmask, Subtractive and fully Baked lights, which the
  style cannot render consistently.
- Tests covering shader compilation, the presence of every required pass, SRP Batcher compatibility
  and the ambient fallback values.

### Known gaps

- Shadows are URP's own. The package's own shadow system is not started.
- `DEBUG_DISPLAY` is not declared, so URP's rendering debugger views do not apply to this shader.
- Transparency, emission and vertex colour tinting are not implemented.
- GPU costs are not yet measured.
