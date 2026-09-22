# Lighting and Ambient

The shader core and the ambient model, delivered in Phase 1. Together they are what makes a flat,
untextured material read as something worth looking at — see [Architecture D10](../Architecture.md).

## Setup

1. Add **HiddenBull Style** as a renderer feature on your URP renderer.
2. Set the renderer's **Rendering Path** to **Forward+** ([D1](../Architecture.md)).
3. Add a **Style Ambient** override to a global Volume.
4. Create a material using **HiddenBull/URP Style/Lit**.

Without step 1 the shader falls back to built-in default ambient values rather than rendering
black, but the Volume will have no effect until the feature is present.

Step 3 in detail, since it is not the Add Component menu: select a GameObject with a **Volume**
component (`GameObject > Volume > Global Volume` creates one), open its **Profile**, then
**Add Override > HiddenBull > Style Ambient**.

## Style Ambient (Volume override)

| Setting | What it does |
|---|---|
| Sky Color | Applied to surfaces facing up |
| Horizon Color | The band around the horizon — this is what makes backlit silhouettes read |
| Ground Color | Applied to surfaces facing down, usually bounced ground light |
| Sky Falloff | How quickly sky takes over above the horizon. Lower = tighter, more defined band |
| Ground Falloff | The same, below the horizon |
| Intensity | Overall ambient strength |
| Baked Weight | Blends from the gradient toward baked lightmaps and probes. 0 = gradient only |

Ambient lives on a Volume rather than on the renderer feature because it is an artistic, per-area
setting that should blend as the camera moves between interior and exterior. The feature's own
settings are performance settings, fixed per quality level — a separate concern.

## Material settings

**Surface** — Base Color. With no texture toggles on, the material samples nothing at all; this is
the fast path, not a reduced one ([D11](../Architecture.md)).

**Shading** — the two numbers that shape the whole light-to-shadow transition
([D14](../Architecture.md)):

| | Diffuse Wrap | Diffuse Softness |
|---|---|---|
| Rock, hard-edged props | 0.0 – 0.1 | 0.2 – 0.6 |
| General environment | 0.1 – 0.3 | 0.8 – 1.2 |
| Skin, soft organic surfaces | 0.4 – 0.7 | 1.2 – 2.0 |

The lit side flattening out at high wrap is intended. In this style the gradation on a lit surface
comes from the ambient gradient, not from the cosine falloff.

*Shadow Terminator Fade* fades the shadow map out near the terminator. Shadow maps produce acne
where a surface is nearly edge-on to the light, and wrapped diffuse makes it worse by lighting
geometry the shadow map already treats as self-shadowed. Raise it if a ragged band appears along the
terminator; lower it if shadows leak onto surfaces that should be dark. Phase 2 makes this redundant
by resolving the terminator properly in the screen-space mask instead of hiding it.

**Optional terms** — each behind a keyword, each free when off ([D15](../Architecture.md)):

- *Rim Light* — a fresnel edge multiplied by the sky colour, so edges pick up the environment
  instead of glowing. A white Rim Color means "pure sky colour"; any other value tints away from it.
- *Specular* — URP's GGX specular, driven by the same shaped diffuse term so highlights cannot
  spill past the stylized terminator. Environment reflection is sampled from the ambient gradient
  along the reflection vector rather than from a reflection probe, so reflections can never
  disagree with the ambient beside them.
- *Vertex Color Tint* — multiplies albedo. In an albedo-only workflow this is how one material
  covers many variations without extra draw calls.

**Textures (optional)** — Base Map and Normal Map, hidden until enabled.

## Baked lighting

Set lights to **Mixed** with the project's Mixed Lighting mode on **Baked Indirect**. Direct light
and shadows stay realtime and keep the style; indirect light bakes and feeds the ambient slot,
blended in with *Baked Weight* ([D16](../Architecture.md)).

**Shadowmask and Subtractive are not supported** and no shader variants are compiled for them.
`Tools > HiddenBull > URP Style > Validate Scene Lighting` reports a scene configured that way, and
the same check runs automatically when a scene is opened, staying silent when nothing is wrong.

Vertex colour is not applied in the Meta pass, so a material relying on Vertex Color Tint bounces
light as if untinted. Worth knowing before tinting large static surfaces that contribute much bounce.

## Keyword budget

Material keywords, all `shader_feature_local` and all off by default — an albedo-only material
compiles to one variant:

`_HB_BASE_MAP` · `_NORMALMAP` · `_HB_VERTEX_COLOR` · `_HB_SPECULAR` · `_HB_RIM` ·
`_ALPHATEST_ON` · `_RECEIVE_SHADOWS_OFF`

Two URP keywords that the standard shaders declare are deliberately **not** compiled:
`SHADOWS_SHADOWMASK` and `LIGHTMAP_SHADOW_MIXING`. Both exist to support mixed lighting modes the
package rejects under D16, and dropping them removes a fourfold variant multiplication.

`DEBUG_DISPLAY` is also not declared, so URP's rendering debugger views do not apply to this shader.
A known gap, traded for a smaller variant count.

## Cost

Not yet measured. Per the [roadmap](../Roadmap.md), a feature without a recorded cost is not
complete — this table is filled in from the sandbox profiling scene before Phase 1 is signed off.

| Configuration | GPU cost |
|---|---|
| Albedo only, no optional terms | *pending* |
| Rim enabled | *pending* |
| Specular enabled | *pending* |
| Base + normal map | *pending* |

## Not in this phase

- Transparency and blending — Phase 4 ([D9](../Architecture.md)).
- Emission — Phase 5, alongside bloom.
- The brush layer — Phase 2, where it shares a source with the shadow mask ([D17](../Architecture.md)).
- Mask and detail texture slots.
