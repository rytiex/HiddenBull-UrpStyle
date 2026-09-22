# Brush Layer

Visible brush strokes on surfaces, from an atlas generated in the editor. Phase 2a delivers the
atlas generator and the surface layer.


See [Architecture D17](../Architecture.md) for the anchoring rules and
[D18](../Architecture.md) for why the atlas is generated rather than shipped fixed.

## Generating an atlas

`Tools > HiddenBull > URP Style > Brush Atlas Generator`

Start from a **preset** — Dry Brush, Loaded Brush, Fine Hatch or Painterly Patches — and adjust from
there. A preset is not the final answer, it lands near one, so the work becomes adjusting a brush
rather than discovering what the controls do.

**Strokes**

| Setting | What it does |
|---|---|
| Count | How many strokes are laid down |
| Length | Shortest and longest stroke, as a fraction of the texture |
| Width | Thinnest and thickest stroke |
| Angle | Dominant stroke direction. A large part of why a surface reads as painted rather than noisy |
| Angle Jitter | How far strokes deviate from that direction. 0 is a rigid hatch, 180 has no direction at all |
| Taper | How far each stroke thins toward its ends. Without it strokes end in rounded caps and read as capsules — usually the most obvious tell that a mark was not made by a brush |
| Edge Softness | Hard-edged marks versus a loaded, blended brush |
| Opacity Variation | Uniform strokes read as a pattern; varied ones read as brushwork |

**Brush Detail** — four modulation layers. All of them are spent at bake time, so none costs
anything at runtime; the shader only ever sees a finished texture.

| Layer | What it does |
|---|---|
| Bristles | Fine lines running along each stroke. Usually the highest-impact setting here — it is what separates a brush stroke from a smooth capsule |
| Edge Break-up | Ragged stroke boundaries instead of clean curves. Also feeds the warp channel |
| Pigment Density | Low-frequency variation in how thickly paint sits — where it pooled and where it ran thin |
| Canvas Grain | Fine tooth showing through the paint. Rarely noticed on its own; its absence is what makes a surface look digital |

Pigment and canvas are fields over the whole tile, so their scale is a whole number of cells across
it — anything else would reintroduce the seam the strokes are drawn to avoid. Bristles and edge
break-up are computed in each stroke's own coordinates and tile for free.

**Warp Spread** sits under the detail layers and affects only the RG channels. The warp channel exists to
perturb the shading normal, and a gradient taken straight from the sharp stroke field is
non-zero only in a two-pixel band along each edge — effectively an edge detector, with nothing to
push. The warp is therefore derived from a blurred copy of the field; Warp Spread is
that blur radius. Raise it for broader, softer displacement; lower it for tighter break-up. The
stroke channel stays sharp either way.

**Output** — Resolution, Contrast, Seed (with **Randomise Seed**).

## Import settings

The generator configures these automatically. They matter, so if an atlas is ever imported by hand:

| Setting | Value | Why |
|---|---|---|
| sRGB (Color Texture) | **off** | RG hold a vector, not a colour |
| Ignore PNG Gamma | **on** | The atlas is data; a gamma chunk applied on import skews the warp |
| Wrap Mode | **Repeat** | The tile runs forever across world space |
| Generate Mipmap | **on** | Without them distant strokes alias into noise |
| Replicate Border | **off** | It would break the seamless tile the generator works to produce |
| Aniso Level | **4** or higher | Triplanar strokes land on floors seen at grazing angles, where isotropic filtering smears them |
| Compression | **High Quality** (BC7) | Block artefacts in RG show up as wobble in the relief |
| Alpha Source | **None** | Alpha is unused; excluding it saves memory |

*Fadeout to Gray* is left off deliberately. Grey happens to be this atlas's neutral, so it would
work — but the shader already fades the brush by distance, and two fades tuned independently would
fight each other.

## Reading the preview

The preview fills the window and repeats by a chosen tile count. Leave it at 2 × 2 or higher while
tuning: a seam is invisible on a single tile and obvious the moment it repeats, and a seam that
reaches the scene is far more expensive to notice there.

**Watch the coverage readout.** The stroke field is centred on its own mean, so coverage decides how
the atlas reads:

| Coverage | Result |
|---|---|
| ~50% | Gaps and strokes symmetric, both carrying the effect at full strength |
| High (>70%) | Gaps take the whole dark range, strokes end up near neutral — reads as dark cracks, not brushwork |
| Low (<20%) | Isolated strokes on a flat field |

Aim near 50%. The window warns outside that band.

This is a consequence of the mean-zero rule, not a flaw in it: without centring, raising brush
strength in a material would also darken or brighten the whole surface, and the two effects could
not be tuned apart.

**Generate and Save…** writes a 16-bit PNG and sets its import settings: linear, repeat wrapping,
mipmaps on, high-quality compression. Those settings are not optional — RG hold a vector rather than
a colour, so sRGB encoding or aggressive compression turns warp artefacts into visible wobble along
surfaces, and without mipmaps distant strokes alias into noise.

## Setup

Assign the atlas on the **HiddenBull Style** renderer feature, under Brush:

| Setting | What it does |
|---|---|
| Atlas | The generated texture. Empty disables the brush on every material |
| World Size Per Tile | Metres covered by one tile. The single scale the whole project shares |
| Fade Start / Fade End | Distance over which strokes fade to flat |

The atlas and scale live on the renderer feature rather than on a Volume deliberately. Stroke size
has to be identical project-wide or neighbouring objects visibly disagree; a Volume would blend the
scale as the camera moved between areas, which is exactly what the one-scale rule forbids.

## Material settings

| Setting | What it does |
|---|---|
| Enable Brush | Off by default. When off the material pays nothing — no sample, no variant |
| Brush Space | *Object* by default, *World* for static geometry that should share one continuous field |
| Relief | How far the brush perturbs the shading normal |
| Brush Shading Break-up | How far the brush offsets the terminator, breaking the transition into patches |
| Brush Albedo Variation | How far the brush varies surface colour |
| Warmth | Temperature shift between strokes and the gaps around them |

**Relief is what carries the brush into the rest of the shading.** The other two settings only reach
the terminator and the albedo, so a face that is entirely lit or entirely in shadow receives nothing
but a flat brightness multiply. Perturbing the shading normal with the warp vector puts the brush
into the ambient gradient, the rim and the specular as well, which is what makes a flat face read as
painted rather than merely dirty. It costs no extra sample — the warp channel is already there.

**Warmth** exists for the same reason. Paint varies in temperature, not just in value; strokes that
only get darker and lighter read as grime, while strokes that also sit warmer or cooler read as
paint. Negative values make the strokes cooler than the gaps.

The default space is **Object**, on the grounds that most materials go on individual props and the
worse first impression is strokes sliding across something as it moves. Switch static geometry —
terrain, architecture, anything that should share one unbroken field of strokes across object
boundaries — to World.

**Object space compensates for the object's scale**, so a mesh scaled up does not get stretched
strokes and sit oddly beside its neighbours.

A world-anchored brush slides across an object that moves, which is the whole reason the choice
exists. Static geometry should stay on World so that adjacent objects share one continuous field of
strokes rather than each carrying its own.

## Cost

Triplanar sampling is three texture fetches, so the layer sits behind `shader_feature_local` and a
material that wants no brush pays nothing.

Not yet measured — Phase 2c builds the profiling harness and fills this in, together with Phase 1.

## Known limits

- **Distance.** Strokes are a fixed world size, so beyond some distance they fall below a pixel and
  alias however good the mipmaps are. The fade to flat is the honest answer, not a workaround.
- **Temporal stability.** Until the accumulation layer of [D8](../Architecture.md) lands in Phase 6,
  fine strokes can shimmer under camera motion. Raising World Size Per Tile helps more than any
  other setting.
- **Triplanar seams.** The three projections blend by surface normal; a surface at 45° to two axes
  shows the softest blend. Strokes hide this far better than a structured texture would, but it is
  not invisible.
