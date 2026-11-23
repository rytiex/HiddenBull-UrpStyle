# Performance

## Quality levels

Each module has its own level in the **HB Style Quality Profile** on the renderer feature. A game can change
them at runtime with `HBStyleRenderSettings` (see [Scripting](Scripting.md#quality-at-runtime)). Every level's
values can be edited in the profile; the table says what the levels change.

| Module | Levels | What the level changes |
|---|---|---|
| Shadows | Off – Ultra | Distance, cascades (blended at their edges), resolution and softness of the sun's shadows |
| Atmosphere | Off – Ultra | Sun colour in the fog and drifting fog patches; Off turns fog off |
| Weather | Low – Ultra | Size of the shelter map and how far small rain and snow details reach |
| Precipitation | Off – Ultra | How many drops and flakes fall, and the far layer |
| Terrain | Low – Ultra | How fast terrain detail drops with distance and how far layers are drawn in full |
| Grass | Off – Ultra | How far grass reaches, how thick it grows, how many triangles a blade has |
| Anti Aliasing | Off – Ultra | How carefully edges and pixel-sized details are smoothed |
| Transparency | Low – Ultra | Refraction resolution, coloured glass shadows (High and Ultra), simple light on Low |
| Reflections | Off – Ultra | Screen-space reflection steps; Off mirrors only the sky |
| Bloom | Off – Ultra | How wide the glow reaches and whether small bright points are steadied; Off turns it off |
| Ambient Occlusion | Off – Ultra | Directions and samples searched per pixel; Low and Medium work at half resolution |

To turn the weather off, set Rain and Snow to 0; nothing of it is drawn then.

## Shader cost

Instruction slots and texture reads of the main pixel shaders, compiled offline with the heaviest URP keyword set
(cascades, soft shadows, Forward+ light loop, shadowmask, light layers, cookies, SSAO, decals). URP's own Lit is
the reference. Every section turned on is the worst case; a material only pays for the sections it uses.

| Shader | Slots | Texture reads |
|---|---|---|
| URP Lit (reference) | 1048 | 101 |
| Lit, colour only | 1751 | 116 |
| Lit, every section, triplanar | 3770 | 147 |
| Shader Graph, HB Base with maps and brush | 2230 | 122 |
| Foliage, every section | 3590 | 140 |
| Plant billboard | 2363 | 137 |
| Terrain | 4814 | 175 |
| Grass | 850 | 19 |
| Liquid surface | 3778 | 149 |
| Transparent, alpha | 2176 | 49 |
| Transparent, tinted or refractive | 2381 | 78 |
| Particle, lit | 1339 | 35 |
| Sky | 1133 | 14 |
| Fog (full screen, once per frame) | 604 | 22 |

<!-- Frame cost: filled in from the final Profiler measurement in a game scene. -->

## Where the time goes

- **Fog** is one full-screen pass, so its cost does not grow with the number of objects.
- **Grass and plants** are culled and drawn on the GPU. Plants hidden behind hills and buildings are skipped.
  Mesh trees are the most expensive thing in a dense forest: the billboard takes over at the Switch Distance, so
  keep that short where many trees stand together.
- **Weather** redraws at most one part of its shelter map per frame.
- **Glass** costs extra targets only when glass is on screen, and a blurred copy of the screen only when
  refractive glass is.
- **Debug views** are compiled only in the editor and stripped from builds.

## Limitations

- **Rendering**: Forward+ and Render Graph only. No MSAA (use the package's anti aliasing), no WebGL, no mobile.
- **Lighting**: built for Mixed lighting with Shadowmask. The HB Style Terrain takes baked shadows (one
  shadowmask chart of up to Max Lightmap Size) but no baked light.
- **Terrain**: one square terrain up to 4 × 4 km, no tiles or streaming. Caves and overhangs are separate meshes
  through terrain holes.
- **Plants**: no middle detail level between the mesh and the billboard. A LOD Group on a plant draws every LOD.
  Plants with the same name share their baked files.
- **Weather**: one rain, snow and wind state for the whole scene. Shelter is seen from above, so a floating object
  keeps the ground under it dry as if it reached down to it.
- **Liquids**: no waterfalls; pools never join each other or the terrain's water. Liquids are visual: floating
  rocks in a liquid have no collision.
- **Shader Graph**: opaque graphs only; use HB URP Style/Transparent for see-through materials.
- **Statistics window**: Unity does not count GPU-driven draws, so grass and plants are missing from its triangle
  count. Use the Profiler or the Frame Debugger.
