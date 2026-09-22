# Architecture

This document records the decisions the package is built on, and why. It is the reference a
future change is argued against — if a decision here turns out to be wrong, it gets revised here
first, with the reasoning, before any code moves.

## Context

- **Unity 6** (developed and tested against `6000.4.1f1`)
- **URP 17** (`com.unity.render-pipelines.universal` — minimum `17.0.3`, tested on `17.4.0`)
- **Target platform:** PC and console
- **Perspective:** 3D first / third person
- **Priority order:** performance first, then visual style, then authoring convenience

## Decisions

### D1 — Forward+ rendering path

The package targets **Forward+**, not Deferred.

Deferred locks shading into a fixed G-buffer lighting model. This package exists to ship a
*custom* lighting model, so the two are in permanent conflict. Forward+ in URP 17 already removes
the classic forward light-count limit, which was the main reason to reach for deferred.

Accepted cost: with MSAA and very high light counts, Forward+ can be more expensive than deferred.
Given a custom lighting model was never optional, this is not a real trade.

### D2 — Hand-written HLSL, one shared lighting library

Every shader in the package — lit, unlit, particle, vegetation, terrain — includes the same
lighting library under `Runtime/Shaders/Library/`. No shader reimplements the lighting model.

Rationale:

- The style changes in one place, so it cannot drift between shader families.
- Full control over keywords and variants, which is what makes stripping (see D6) possible.
- Clean diffs in version control. ShaderGraph assets merge badly and hide their cost.

Shader Graph is not used as the source of truth. If artist-facing graph authoring is needed later,
it is exposed as Custom Function nodes calling into the same library — never as a second
implementation.

### D3 — Render Graph only

All passes are authored against the Render Graph API. URP 17 deprecates Compatibility Mode, and
porting non-Render-Graph passes afterwards is expensive. There is no fallback path.

### D4 — One shared resource manager

Depth, normals, the occlusion + bent normal buffer and the shadow mask are consumed by more than
one pass. A single renderer feature owns and declares these resources through Render Graph;
individual features do not re-derive buffers another feature already produced.

This is the backbone of the package. A feature that cannot express its inputs as a dependency on
this shared set does not belong as a separate renderer feature.

### D5 — Signature feature: contact-hardening, painterly shadows

Shadows should be **crisp where the caster meets the receiver and soften with distance**, with the
soft end breaking up into brush-like strokes rather than a clean Gaussian falloff.

Implementation shape — resolved in screen space, not per material:

```
DepthNormals prepass
      ↓
Blocker search              → penumbra width
      ↓
Variable-radius PCF         → raw shadow mask
      ↓
Stylize pass                → penumbra width drives brush warp
      ↓
Screen-space shadow mask    → sampled by all lit shaders
```

Why screen space:

- Cost is independent of material complexity and immune to overdraw — the decisive argument given
  the performance priority.
- The style lives in exactly one pass, so changing the look is a single-file edit.
- Can run at reduced resolution and be tiered (see `StyleTierSettings`).

The brush warp is sampled in **world space**, not screen space, so the strokes stay anchored to
surfaces instead of swimming when the camera moves.

Known limits, accepted deliberately:

- The mask covers the **main directional light**. A mask per additional light is not affordable;
  additional lights use standard PCF. They rarely dominate a frame.
- Transparent surfaces cannot read the mask correctly. They need a fallback path, resolved
  together with the transparency work in Phase 4.

### D6 — Signature feature: directional, coloured occlusion

Standard SSAO multiplies ambient by grey, which reads as dead. Instead:

- **GTAO with bent normals** — produces not just *how occluded* a point is, but *which direction
  it is open to*.
- Ambient is sampled along the bent normal, so occlusion naturally takes on the colour of the sky
  gradient and the dominant light instead of darkening uniformly.
- **Multi-bounce approximation** tints occlusion by surface albedo, so a corner next to a red wall
  goes red. Nearly free relative to its visual payoff.

Full screen-space GI is out of scope for now. It remains an option for an Ultra tier later,
at half resolution with temporal accumulation.

### D7 — Quality tiers are a first-class concept

`StyleProfile` holds one `StyleTierSettings` per `StyleQualityTier`. Renderer features read the
active tier rather than caching their own copies, so a tier change reconfigures the whole style
from one place.

Every feature must define behaviour for every tier, **including being switched off entirely**. The
editor-side variant stripper walks the same data to decide which shader variants can be dropped.

### D8 — Temporal stability is a dependency, not a bonus

Screen-space techniques need temporal stability. Without it, the soft end of the shadow penumbra
shimmers under camera motion. The package currently assumes URP's TAA is available; a dedicated
temporal accumulation layer for the style's own buffers is planned for Phase 6.

### D9 — Transparency and sorting: layered, not a single fix

There is no one solution. The package will offer, in order of preference:

1. **Dithered alpha (screen-door)** into the opaque queue — for fades, foliage and camera-proximity
   dissolve. Sorting problems disappear because the surface is no longer transparent. Cheapest and
   most correct answer for the majority of cases.
2. **Weighted Blended OIT** — where genuine transparency is required (glass, smoke, water).
   Order-independent and far cheaper than depth peeling.
3. **Transparent depth prepass with per-material sort priority** — for cases that need explicit
   artist control.
4. **Editor debug view** making draw order visible and colour-coded, so sorting bugs are diagnosed
   instead of guessed at.

Detailed design lands in Phase 4.

### D10 — The surface supplies albedo; the environment supplies everything else

This is the thesis the style is built on, drawn from the project's visual references: in all of
them the image is carried by **light and atmosphere, not by texture detail**. A canyon of untextured
flat-shaded rock reads as rich because its shadow side is blue rather than black, because distance
washes geometry toward the sky colour, and because silhouettes separate on rim light.

So a flat, untextured material must already look good. Gradation on a flat surface comes from
sources that cost nothing per-texel:

| Source | Contribution |
|---|---|
| Sky gradient sampled along the normal | Upward faces cool, downward faces warm — gradation for free |
| Bent normal occlusion (D6) | Cavities take the environment's colour instead of a grey multiply |
| Fresnel rim tinted by the sky | Silhouette separation |
| Height fog / aerial perspective | Depth and distance become readable |
| Warm key against cool fill | Colour contrast from a single light |

The practical consequence: **the ambient model is not decoration on top of the lighting model, it
is the lighting model.** Effort spent there pays back on every surface in the frame, including the
ones nobody textured.

### D11 — Albedo-only is the primary path; textures are opt-in

The default material samples no textures at all. Base colour, normal, mask and detail slots exist
but are gated behind `shader_feature_local`, so a material that does not use them pays nothing —
no sample, no variant, no register pressure.

This inverts the usual URP arrangement, where the textured path is the default and the flat path is
a degenerate case. Given D10, the flat path is the one that must be fast.

### D12 — Three-zone gradient ambient

Ambient is a sky / horizon / ground gradient blended along the surface normal (and, once D6 lands,
along the bent normal).

Two colours would be cheaper, but a two-colour hemisphere cannot produce the warm band at the
horizon that the references depend on — that band is what makes a backlit silhouette read. Three
zones cost a couple of extra lerps and buy the single most recognisable feature of the style.

A cubemap override is deliberately deferred. It is the right answer for interiors, and will be
added as an opt-in keyword when interior work actually demands it, not before.

### D13 — The ambient model ships in Phase 1, not Phase 3

Originally the sky and ambient model sat in Phase 3, after the shader core. That ordering was
wrong: by D10 the ambient model *is* the lighting model, so a Phase 1 without it would produce
output nobody could judge.

Phase 1's deliverable is therefore concrete and visual: **a single directional light and zero
textures must be enough to make a rock convincing.** Phase 3 keeps the remaining environment work —
height fog, aerial perspective.

### D14 — Analytic wrapped diffuse

The light-to-shadow transition is shaped by two analytic parameters — how far light wraps past the
terminator, and how soft that transition is — rather than by a ramp texture.

Wide and soft reads as skin; narrow and tight reads as rock. Both come from the same two numbers,
which means no texture sample, no keyword, and per-material variation at zero cost. A ramp texture
would offer more artistic control, including banded toon transitions, but it makes every material
sample a texture and turns every style adjustment into an art task. It stays available as a future
opt-in if a concrete need appears.

### D15 — Optional shading terms

The master shader ships with three terms, each behind `shader_feature_local` and each costing
nothing when off:

- **Rim light** — fresnel edge tinted by the sky colour. The cheapest term in the package and the
  one most responsible for silhouettes reading against a bright background.
- **Specular** — URP's PBR specular, enabled per material. Near-absent on the reference's rock and
  terrain, present on characters, so making it optional lets environment materials skip it entirely.
- **Vertex colour tint** — multiplies albedo. In an albedo-only workflow this is how one material
  covers many variations without extra draw calls. Effectively free.

**Emission is deferred** to Phase 5, where it arrives alongside bloom. It is additive to the shader
rather than structural, so postponing it costs nothing.

## Non-goals

- Deferred rendering support (see D1).
- Mobile and WebGL as primary targets. The tier system should degrade gracefully, but no feature
  is designed around tile-based GPU constraints.
- ShaderGraph as a source of truth (see D2).
