# Architecture

This document records the decisions the package is built on, and why. It is the reference a
future change is argued against — if a decision here turns out to be wrong, it gets revised here
first, with the reasoning, before any code moves.

## Context

- **Unity 6** (developed and tested against `6000.4.1f1`)
- **URP 17.4.0** (`com.unity.render-pipelines.universal`)

  The minimum is pinned to exactly the version the package is developed against. Earlier 17.x
  releases moved shader keywords and API accessibility between them — `_FORWARD_PLUS` became
  `_CLUSTER_LIGHT_LOOP`, for one — and a lower floor would mean claiming support for versions
  nobody has tested. Raising the floor is cheap; a silently broken lighting path is not.
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

Depth, normals and the occlusion + bent normal buffer are consumed by more than
one pass. A single renderer feature owns and declares these resources through Render Graph;
individual features do not re-derive buffers another feature already produced.

This is the backbone of the package. A feature that cannot express its inputs as a dependency on
this shared set does not belong as a separate renderer feature.

### D5 — Shadows are a signature feature, and their design is open

Shadows should be crisp where a caster meets its receiver and soften with distance, with the soft
end breaking up rather than falling off cleanly.

The technique is deliberately not fixed here. A screen-space mask over URP shadow maps was built and
reverted: it worked, but almost everything that made it hard to author turned out to be baggage of
shadow mapping rather than anything to do with the style — bias, cascade seams, texel density
limits, acne. Whatever replaces it is decided before it is built, not during.

Until then the package uses URP shadows unchanged.

### D6 — Signature feature: directional, coloured occlusion

Standard SSAO multiplies ambient by grey, which reads as dead. Instead:

- **GTAO with bent normals** — produces not just *how occluded* a point is, but *which direction
  it is open to*.
- Ambient is sampled along the bent normal, so occlusion naturally takes on the colour of the sky
  gradient and the dominant light instead of darkening uniformly.
- **Multi-bounce approximation** tints occlusion by surface albedo, so a corner next to a red wall
  goes red. Nearly free relative to its visual payoff.

Full screen-space GI is out of scope for now. It remains an option for a high-end renderer later,
at half resolution with temporal accumulation.

### D7 — Quality scaling is Unity's, not the package's

*Revised. The original decision gave the package its own four-level tier system in a `StyleProfile`
asset. That was a mistake and has been removed; the reasoning for the change is below.*

Performance settings live in a block serialized directly on
`HiddenBullStyleFeature`. There is one set of settings per renderer, and no tier enum.

Unity already has a quality mechanism, and it works by asset indirection: a Quality Level points at
a URP Asset, which points at a Renderer, which carries its features' settings. URP itself scales
this way — shadow resolution and cascade counts have no tier enum, there is simply a `URP_Asset_PC`
and a `URP_Asset_Mobile`. A tier system inside the package would sit *beside* that mechanism rather
than on top of it, leaving two ways to express the same thing and an obvious question of which one
wins.

So quality scaling means authoring one renderer per quality level, and switching at runtime means
`QualitySettings.SetQualityLevel`. Nothing is lost; the responsibility moves to where the engine
already handles it.

Variant stripping is unaffected: the stripper walks the renderer features of every URP Asset in
Graphics Settings, which is how URP's own prefiltering works.

The split between the two homes for settings is worth stating, because it decides where anything
new belongs:

| Home | For |
|---|---|
| **Volume** | Artistic settings that blend as the camera moves between areas — `StyleAmbient` |
| **Renderer feature** | Performance settings, fixed per quality level — brush atlas and scale |

Every feature must still be switchable off entirely. A configuration with everything disabled is a
valid one, and is what a low-end renderer uses.

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

**Emission is deferred** to Phase 5, where it arrives alongside bloom. It is additive to the shader
rather than structural, so postponing it costs nothing.

### D16 — Baking policy: direct light stays realtime, indirect may be baked

Baked lighting and a custom BRDF do not mix by default. Unity bakes irradiance using *its* Lambert
model, so baked light arrives already convolved and the wrapped diffuse of D14 cannot be applied to
it. A scene mixing baked walls with realtime props would show two different lighting characters.

The split is therefore fixed:

| | Source | Goes through the style |
|---|---|---|
| Direct light and shadows | Always realtime | Yes — wrapped diffuse (D14) |
| Indirect light | May be baked | Yes — enters through the ambient slot |

Lights use **Mixed / Baked Indirect**. Direct lighting and shadows stay realtime and keep the
style; bounced light bakes into lightmaps and probes.

**Ambient is a pluggable slot.** The gradient of D12 is one provider; baked lightmaps and probes
are another. Both feed the same slot, so the bent-normal colouring of D6 applies identically
whichever is in use — exteriors can run on the gradient alone, interiors can bake indirect light.

**Shadowmask and Subtractive modes are not supported.** Both bake shadows into lightmaps, and baked
shadows have none of the stylized penumbra of D5 — the scene would end up speaking two different
shadow languages. An editor validation check reports lights configured this way rather than letting
the scene degrade silently.

### D17 — Brush layer: world or object space, never screen space, always one scale

The style calls for visible brush strokes on surfaces. Two separate layers produce it:

- **Shading brush** — the diffuse terminator of D14 is warped and broken up by the brush source,
  so the light-to-shadow transition reads as painted patches rather than a smooth gradient.
  Essentially free: it perturbs a term already being computed.
- **Albedo brush** — a triplanar brush pattern varying surface colour. No UVs required, works on
  any mesh, and a single brush atlas shipped with the package serves the whole project, so the
  albedo-only workflow of D11 is preserved: no per-asset texture authoring.

Anchoring rules, which are not negotiable if the effect is to hold together:

- **Never screen space.** Screen-anchored strokes swim across surfaces as the camera moves.
- **World space** for static geometry, **object space** for anything that moves — a world-anchored
  brush slides across a carried object.
- **One scale everywhere.** Stroke size is a fixed world-space measure, not scaled per object and
  not compensated for camera distance. Object-space mode divides out the object's lossy scale, so a
  scaled-up mesh does not get stretched strokes and sit oddly next to its neighbours.

Costs and risks, stated up front: triplanar sampling is three texture fetches, so the layer sits
behind `shader_feature_local` and a material that wants no brush pays nothing. The brush atlas must
be mip-mapped and the layer must fade toward flat at distance, or strokes alias into noise — which
also makes this layer dependent on the temporal stability of D8.

The brush layer ships in **Phase 2**.

### D18 — The brush atlas is generated offline, not sampled procedurally at runtime

Procedural noise evaluated in the shader was rejected for the brush: noise reads as *noise*, not as
paint. But synthesising strokes **offline and baking them to a texture** takes the good half of both
options — real stroke shapes, no runtime synthesis cost, and unlimited variations without waiting
on an artist. A seed makes any result reproducible.

The package therefore ships a generator (`Tools > HiddenBull > URP Style > Brush Atlas Generator`)
rather than a fixed texture. Hand-painted input can be added later as a second front-end to the same
baker; the output format is what matters and it does not change.

**Channel layout.** One texture, one sample, two consumers:

| Channel | Content | Used by |
|---|---|---|
| RG | Signed warp vector | Perturbing the shading normal, and whatever draws shadow edges later |
| B | Stroke coverage | Surface shading — the terminator break-up and the albedo tint |

Two separate textures would cost six triplanar samples instead of three.

**The atlas must tile seamlessly.** It repeats forever across world space (D17), so a visible seam
ruins everything downstream. Guaranteeing this is the generator's main job and the strongest
argument for synthesis over hand painting, where seamlessness is painstaking to achieve.

**The stroke channel is centred on 0.5**, by subtracting the generated field's own mean. At runtime
the tint is read as `(B - 0.5) * 2`, so a centred field leaves average brightness untouched and the
brush only redistributes it. Without this, raising brush strength would also darken or brighten the
whole surface, and the two effects would be impossible to tune apart.

**Stored linear and uncompressed-to-high-quality.** RG holds vectors, not colour: sRGB encoding or
aggressive block compression turns warp artefacts into visible wobble along shadow edges.

## Non-goals

- Deferred rendering support (see D1).
- Mobile and WebGL as primary targets. Features should degrade gracefully when switched off, but
  none is designed around tile-based GPU constraints.
- ShaderGraph as a source of truth (see D2).
