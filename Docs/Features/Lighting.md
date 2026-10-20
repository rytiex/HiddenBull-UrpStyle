# Lighting and Shadows

The lighting model every shader of the package shares. It is what makes a flat, untextured material
read as painted.

## Light

- **Wrapped diffuse.** *Diffuse Wrap* and *Diffuse Softness* shape the whole light-to-shadow transition
  with no texture fetch.
- **Half Tone** lifts the shadow side toward the lit color, the way a painter keeps shadows readable.
- **Specular and Rim** are painterly: the highlight follows the same shaped diffuse term, so it cannot
  spill past the terminator, and the rim takes the sky color instead of glowing.
- **Additional lights** work through Forward+, with the same wrap and shadows.

## Ambient

Ambient light comes from the **HB Style Sky** volume's day, dusk and night gradients (sky, horizon and
ground), blended along the surface normal. Where baked light exists it is used instead: lightmaps on
static objects, Adaptive Probe Volumes or light probes on everything else. Reflections are sampled from
the same gradient, so they never disagree with the ambient beside them.

## Baked and mixed lighting

Use **Mixed** lights with the **Shadowmask** mode. Direct light stays realtime and keeps the style;
indirect light and distant shadows come from the bake. Bake with the [Bake Panel](../Tools.md#bake-panel),
which sets the package's lighting defaults and checks the scene before baking.

## Shadows

Shadows are rendered from URP's shadow maps and filtered by the package. The settings are on the
renderer feature, under **Shadows**:

| Setting | What it does |
|---|---|
| Quality | Writes a set of shadow settings (cascades, resolution) into the URP asset in one go |
| Distance | How far shadows reach, in metres; its range follows the quality |
| Softness | The widest a shadow edge may spread, in metres; 0 hands filtering back to URP |
| Contact | How far a caster must stand off a surface before its shadow reaches full softness |
| Brush | How far the brush drags the shadow edge, in metres |
| Brush Size | World size of one shadow stroke |
| Debug | Replaces the lit color with one term of the shadow calculation |

Use **Debug** before guessing at a shadow problem: it shows which term is at fault.

Very large triangles can show cascade-shaped shadow leaks close to the camera. Raising the light's
**Near Plane** to 1 fixes it.
