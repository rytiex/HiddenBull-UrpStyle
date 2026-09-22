# Test Plan — Phase 2a, Brush Source and Surface Layer

Each check says what to do, what a pass looks like, and — the part worth reading — what a failure
would actually mean. A symptom traced to a cause is worth ten reports of "it looks wrong".

## Setup

1. Renderer: **Forward+**, with the **HiddenBull Style** feature added.
2. A Global Volume with a **Style Ambient** override.
3. One directional light, no other lights yet.
4. A test scene with: a large ground plane, a sphere, a cube, and a cube scaled non-uniformly
   (for example 3 × 1 × 1).
5. All using one material on **HiddenBull/URP Style/Lit**, Base Color mid-grey, no textures.

---

## A — Generator

| # | Do | Pass | A failure means |
|---|---|---|---|
| A1 | Apply each of the four presets | Each produces a visibly different character; coverage readout lands roughly 25–65% | A preset outside the band needs its stroke count or width retuned |
| A2 | Set Tiling to 4 × 4 | No seam, no repeating landmark stroke that draws the eye to the grid | A seam means a write escaped the wrap; a repeating landmark means too few, too large strokes |
| A3 | Drag Stroke Count from 10 to 800 | Preview tracks continuously; coverage readout rises with it; warnings appear at the ends | A stuck preview means the change signature is not catching that field |
| A4 | Set Taper to 0, then to 1 | At 0 strokes end in blunt rounded caps; at 1 they come to points | No change means the taper is being overridden by edge break-up |
| A5 | Set Bristles to 0, then 1, at Density 6 and 30 | At 0 strokes are smooth; at 1 fine lines run **along** the stroke, not across it, and get finer with density | Lines running across the stroke means the bristle noise is indexed by the wrong axis |
| A6 | Set Edge Break-up to 0, then 1 | Boundaries go from clean curves to ragged | Strokes getting clipped to straight sides means the drawing bounds do not allow for the wander |
| A7 | Set Canvas Grain to 1, Scale 128 | Fine tooth across painted areas; bare gaps stay bare | Grain visible in the gaps means it is being added rather than multiplied |
| A8 | Set Pigment to 1, Scale 2, Tiling 4 × 4 | Broad patches, still seamless | A seam here means the noise lattice is not wrapping |
| A9 | Change Seed, then change it back | The exact same atlas returns | Non-reproducible output means something is reading an unseeded random source |
| A10 | Undo (Ctrl+Z) several times | Values **and** preview step back together | A stale preview means the undo hook regressed |

## B — Asset and import

| # | Do | Pass | A failure means |
|---|---|---|---|
| B1 | Generate and Save at 512 | PNG written, selected in the Project window | — |
| B2 | Inspect its import settings | sRGB **off**, Ignore PNG Gamma **on**, Wrap **Repeat**, Mipmaps **on**, Aniso **4**, Max Size **512**, Compression **High Quality**, Crunch **off**, Replicate Border **off** | Any one wrong means `ConfigureImporter` missed a field |
| B3 | Manually break three of those settings, then re-export over the same file | All three are restored | Surviving settings mean the overwrite path is not reapplying the full configuration |
| B4 | Generate at 2048 | Max Size reads 2048, not 2048-clamped-down; strokes are the same size relative to the tile as at 512 | Different apparent stroke size means a parameter is in pixels where it should be a fraction |

## C — Surface brush, world space

Assign the atlas to the feature's **Brush > Atlas**. The brush is on by default in new materials.

| # | Do | Pass | A failure means |
|---|---|---|---|
| C1 | Leave Atlas empty, Enable Brush on | Surface renders flat, exactly as with the brush off — not black, not grey-washed | A change means the "no atlas bound" gate is not working and a default texture is being sampled |
| C2 | Assign the atlas | Strokes appear on all objects | Nothing appearing means the globals pass is not publishing the atlas |
| C3 | Look at where the ground plane meets the cube | Strokes run continuously across the join, as one field | A discontinuity means world anchoring is broken |
| C4 | Move the cube | Strokes stay fixed in the world; the cube slides through them | Strokes moving with the cube means world mode is using object coordinates |
| C5 | Change World Size Per Tile from 0.5 to 8 | Stroke size changes on every object at once | Any object not tracking means something is reading a stale global |
| C6 | Orbit the camera | Strokes stay welded to surfaces, no sliding | Sliding means something screen-space crept in — see D17 |
| C7 | Set Brush Albedo Variation 0 → 1 | Surface colour breaks into painted variation; overall brightness stays roughly constant | A surface that darkens or brightens overall means the atlas is not mean-centred |
| C8 | Set Brush Shading Break-up 0 → 0.5, with Diffuse Softness at 0.3 | The terminator breaks into patches instead of a smooth curve | Only a colour change means the brush is tinting instead of offsetting the terminator |
| C9 | Walk from 1 m to 100 m away | Strokes fade to flat over the Fade Start → Fade End range, with no popping | A hard step means the fade range is too narrow |
| C9a | On a face **fully in light** (no terminator on it), set Relief 0 → 1 | The face breaks into painted variation, driven by the ambient gradient rather than by any light | No change means the normal perturbation is applied after the shading, or the warp channel is empty |
| C9b | On a face **fully in shadow**, set Relief 0 → 1 | Same break-up, in the ambient | Nothing happening in shadow means relief is being scaled by the light |
| C9c | Set Warmth −1 → +1 | Strokes shift cooler then warmer relative to the gaps; overall brightness stays put | An overall colour cast means the shift is not centred on the coverage |
| C10 | Look along the ground plane at a grazing angle | Strokes stay legible into the distance | Mush means anisotropic filtering did not apply |

## D — Object space

| # | Do | Pass | A failure means |
|---|---|---|---|
| D0 | Create a fresh material | Brush Space already reads **Object** | A new material defaulting to World means the shader property default did not take |
| D1 | Set Brush Space to Object on the moving cube | Strokes now travel with it | No change means the space uniform is not reaching the shader |
| D1a | With Relief above 0, rotate the object-space cube | The relief rotates with it and stays consistent | Relief flickering or flattening on rotation means the warp vector is not being rotated into world space |
| D2 | Rotate that cube | Strokes rotate with it, no swimming | Swimming means the normal is not being transformed to object space |
| D3 | Compare the 3 × 1 × 1 cube against a 1 × 1 × 1 one, both in Object space | Stroke size and shape match; no stretching along the scaled axis | Stretching means the lossy scale is not being divided out — the exact mismatch D17's one-scale rule exists to prevent |
| D4 | Put a world-space object next to an object-space one | Stroke size matches between them | A mismatch means the two paths disagree on scale |

## E — Interaction with Phase 1

| # | Do | Pass | A failure means |
|---|---|---|---|
| E1 | Turn the brush off on the material | Output is identical to before Phase 2a | A difference means the brush is leaking outside its keyword |
| E2 | Enable Rim Light with the brush on | Both read; the rim is not broken up by strokes | Rim following the strokes means the brush is being applied to the wrong term |
| E3 | Enable Specular with the brush on | Highlights sit on the brushed albedo without doubling the pattern | — |
| E4 | Add a point light | It lights the surface with the same terminator break-up as the directional | No break-up on the point light means additional lights are not receiving the offset |
| E5 | Change the Style Ambient colours | Ambient still drives the flat areas; brushwork rides on top | — |

## F — Robustness

| # | Do | Pass | A failure means |
|---|---|---|---|
| F1 | Enter and exit Play mode | Brush unchanged, no console errors | — |
| F2 | Add a second camera | Both show the same brush, no flicker | Flicker means globals are leaking between cameras |
| F3 | Delete the atlas asset while assigned | Falls back to flat, no errors, no pink | Errors mean the null gate is only checked at assign time |
| F4 | Build a Player | No shader errors; brush behaves as in the editor | Editor-only behaviour usually means an editor-only code path |
| F5 | Check the frame time with the brush on and off | A measurable but modest difference | This is a sanity check only — Phase 2c measures it properly |

## Known limits — confirm, do not report

These are accepted consequences, documented in [Architecture](../Architecture.md) and
[Brush](../Features/Brush.md):

- **Shimmering on fine strokes under camera motion.** Until the temporal layer of D8 lands in Phase
  6, raising World Size Per Tile is the mitigation.
- **Triplanar blend softness** on surfaces at 45° to two axes. Strokes hide it better than a
  structured texture would, but it is not invisible.
- **Brush does not yet affect shadows.** The warp channel exists but nothing
  consumes it yet.
