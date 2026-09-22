# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.4.0] - 2026-09-22

### Changed — breaking

- **Quality scaling now uses Unity's mechanism instead of the package's own.** `StyleProfile`,
  `StyleTierSettings` and `StyleQualityTier` are removed. Performance settings are serialized
  directly on `HiddenBullStyleFeature` as a `StyleSettings` block, one set per renderer.

  Unity already scales quality by asset indirection — a Quality Level points at a URP Asset, which
  points at a Renderer, which carries its features' settings — and URP itself works that way for
  shadow resolution and cascades. A tier system inside the package sat beside that mechanism rather
  than on top of it, leaving two ways to express the same thing. Scaling quality now means
  authoring one renderer per quality level; switching at runtime means
  `QualitySettings.SetQualityLevel`. Architecture D7 is revised accordingly.

  **Upgrading:** delete any `StyleProfile` asset and re-check the settings on the HiddenBull Style
  renderer feature. Nothing in Phase 1 read the profile, so no visual output changes.

- `StyleResolutionScale` moved to `StyleSettings.cs`; its `ToDivisor` extension now lives on
  `StyleResolution` rather than `StyleQuality`.

## [0.3.1] - 2026-09-22

### Fixed

- **Diffuse Wrap had almost no visible effect, and the unlit side never went dark.** The transition
  band was centred on the terminator without clamping to the range the shaded value can actually
  take. At the default wrap of zero the band reached below zero, so the shadow side sat at a grey
  floor of roughly half intensity, and most of the band fell outside the reachable range — which is
  what made the parameter look inert. The band edges are now clamped, so blacks are black and wrap
  moves the terminator as intended.
- **Ragged self-shadowing band at the terminator.** Shadow maps produce acne where a surface is
  nearly edge-on to the light, and wrapped diffuse makes it worse by lighting geometry the shadow
  map already treats as self-shadowed. The shadow term now fades in as the surface turns toward the
  light, hiding the band behind the diffuse falloff.

### Added

- `Shadow Terminator Fade` material parameter controlling that fade. Raise it if the ragged band is
  visible; lower it if shadows leak onto surfaces that should be dark.
- Custom `StyleProfile` inspector: tiers are named rather than listed as "Element 0–3", the default
  tier is marked, and each collapsed tier shows a one-line summary of what it enables.

## [0.3.0] - 2026-09-22

Phase 1 — shader core and ambient model. A single directional light and no textures at all are now
enough to light a scene in the style.

### Added

- Shared HLSL lighting library under `Runtime/Shaders/Library/`. Every shader family in the package
  routes through it, so the style cannot drift between them.
  - Analytic wrapped diffuse: two parameters shape the whole light-to-shadow transition, with no
    texture fetch and no keyword.
  - Three-zone gradient ambient (sky / horizon / ground) blended along the surface normal.
  - Fresnel rim light tinted by the sky colour.
- `HiddenBull/URP Style/Lit` master shader with ForwardLit, ShadowCaster, DepthOnly, DepthNormals
  and Meta passes. Albedo-only is the default path and samples no textures; base map, normal map,
  specular, rim and vertex colour tint are each behind a local shader feature.
- `StyleAmbient` volume component driving the gradient, including a blend weight toward baked
  lightmaps and probes.
- `HiddenBullStyleFeature` renderer feature, which owns the shared resource set later phases build
  on and publishes the ambient model per camera through Render Graph.
- `LitShaderGUI` material inspector: texture slots stay hidden until enabled, so the albedo-only
  default reads as the primary path rather than a stripped-down one.
- Scene lighting validation reporting Shadowmask, Subtractive and fully Baked lights, which the
  style cannot render consistently. Runs on scene open and from
  `Tools > HiddenBull > URP Style > Validate Scene Lighting`.
- Tests covering shader compilation, the presence of every required pass, SRP Batcher compatibility
  and the ambient fallback values.
- `Docs/Features/Lighting.md`.

### Changed

- Architecture decisions D10–D17 recorded: the environment-driven style thesis, albedo-only as the
  primary path, the three-zone ambient model, analytic wrapped diffuse, the optional term set, the
  baking policy, and the brush layer's anchoring rules.
- The ambient model moved from Phase 3 to Phase 1 — in this style it *is* the lighting model, so a
  Phase 1 without it would have produced output nobody could judge.

- Minimum URP raised from 17.0.3 to 17.4.0, the version the package is developed and verified
  against. Keywords and API accessibility moved between 17.x releases, so a lower floor would claim
  support for versions nobody has tested.

### Known gaps

- `DEBUG_DISPLAY` is not declared, so URP's rendering debugger views do not apply to this shader.
  Traded deliberately for a smaller variant count.
- Vertex colour tint is not applied in the Meta pass, so tinted surfaces bounce light as if
  untinted.
- GPU costs are not yet measured; the table in `Docs/Features/Lighting.md` is unfilled.

## [0.2.0] - 2026-09-22

Phase 0 — foundation. No rendering code yet; this release establishes the structure everything
after it is built on.

### Added

- Package folder structure: `Runtime/{Core,Rendering,Volume,Shaders}`, `Editor/{Inspectors,Setup,Validation,ShaderStripping,ShaderGUI}`, `Tests/{Runtime,Editor}`.
- `StyleProfile` ScriptableObject holding one `StyleTierSettings` per `StyleQualityTier`, the single source of configuration for renderer features and for editor-side variant stripping.
- `StyleQualityTier` (Low/Medium/High/Ultra) and `StyleResolutionScale` with per-tier defaults.
- Test assemblies plus coverage for the profile's tier-resolution invariants.
- Documentation: architecture decisions, roadmap, and contribution guide under `Docs/`.

### Changed

- Runtime assembly now references `Unity.RenderPipelines.Core.Runtime`, required for Render Graph and Volume APIs.
- Editor assembly now references `Unity.RenderPipelines.Core.Editor`.

### Removed

- Placeholder `StyleSettings` ScriptableObject, superseded by `StyleProfile`.

## [0.1.0] - 2026-09-22

### Added

- Initial package skeleton: package.json, Runtime/Editor assembly definitions, Samples~ folder.
- Placeholder `StyleSettings` ScriptableObject.
