# Liquids

Water, lava and acid use the **HB URP Style → Liquid** shader with depth gradients, waves, caustics,
foam, bubbles and refraction. Liquids shelter what is below them from rain and snow.

## Components (Liquid category)

| Component | Purpose |
|---|---|
| HB Style Liquid Terrain | Connected water over a terrain: sea level plus painted lakes and rivers that flow into each other. Its surface is a camera-centred clipmap. |
| HB Style Liquid Pool | An isolated box of liquid (a basin, a vat) that never joins other water |
| HB Style Liquid Cutter | Cuts liquid away inside a shape (boat hulls, rooms below the waterline) and can keep the inside dry |
| HB Style Liquid Floater | Makes an object float, with draft, tilt and lean |

## Underwater

When the camera goes under the surface, the underwater pass tints and fogs the view, adds light shafts,
caustics on the floor and bubbles. The waterline on screen follows the rendered wave mesh exactly, so
there is no gap between the surface and the underwater view when the camera sits at the waterline.

## Quality

The renderer feature's **Liquid Quality** (Low / Medium / High) drops the coarser features first: soft
caustic shadows, crest trails and light shafts.
