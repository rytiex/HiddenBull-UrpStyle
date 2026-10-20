# Brush

Visible painterly strokes over the lighting, from a brush atlas made in the editor.

## Making an atlas

Open **Tools → HiddenBull → URP Style → Texture Studio**, Brush page. Start from a preset (Dry Brush,
Loaded Brush, Fine Hatch, Painterly Patches) and adjust:

| Group | Settings |
|---|---|
| Strokes | Count, Length, Width, Angle, Angle Jitter, Taper, Edge Softness, Opacity Variation |
| Brush Detail | Bristles, Edge Break-up, Pigment Density, Canvas Grain (all baked, free at runtime) |
| Warp Spread | How broad the relief displacement is |
| Output | Resolution, Contrast, Seed |

Keep the preview at 2 × 2 or more to spot seams, and keep **coverage** near 50%: high coverage reads as
dark cracks, low coverage as isolated strokes. **Generate and Save** writes the atlas with the right
import settings (linear, repeat, mipmaps, high-quality compression); do not change them.

## Setup

Assign the atlas on the **HiddenBull Style** renderer feature, under Brush:

| Setting | What it does |
|---|---|
| Atlas | The generated texture. Empty turns the brush off on every material |
| World Size Per Tile | Metres covered by one tile; one scale for the whole project |
| Fade Start / Fade End | Distance over which strokes fade to flat |
| Randomize | Breaks the atlas up so it never reads as a repeating tile |
| Grow With Distance | Paints the distance with broader strokes instead of fading them out |

## Material settings

| Setting | What it does |
|---|---|
| Enable Brush | Off by default; when off the material pays nothing |
| Brush Space | *Object* for props, *World* for static geometry, *Rest Pose* for skinned characters |
| Relief | How far the brush bends the shading normal (carries it into ambient, rim and specular) |
| Brush Shading Break-up | How far the brush breaks up the light-to-shadow edge |
| Brush Albedo Variation | How far the brush varies surface color |
| Warmth | Warm / cool shift between strokes and gaps |

World space shares one continuous field across neighbouring objects but slides on moving ones; Object
space moves with the object and compensates for its scale.

## Limits

- Strokes are a fixed world size, so they fade out with distance instead of aliasing.
- Fine strokes can shimmer under camera motion; a larger World Size Per Tile helps most.
- Triplanar blending shows a soft seam on surfaces at 45° to two axes.
