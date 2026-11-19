# Changelog

## 1.0.0

First release of the rebuilt package.

- One renderer feature (HB Style) with per-module quality levels in an HB Style Quality Profile, changeable at
  runtime through `HBStyleRenderSettings`.
- Shaders: Lit, Foliage, Foliage Body, Transparent, Particle, Decal and Sky, sharing one lighting model and brush;
  Shader Graph nodes and graph templates with the same code.
- Environment: Environment and Atmosphere Profiles, Environment Controller with time of day and weather,
  Environment Zones; layered fog with Clear, Rain and Snow states, overcast, aurora and stars.
- Weather: rain, snow and wind with shelter from roofs, wet and snowy surfaces, Weather Masks and Weather Bodies.
- HB Style Terrain with rule-based layers, sculpt and paint tools, GPU grass and flowers, GPU plants with
  billboards, harvesting, and connected liquids; isolated Liquid Pools, cutters and trails.
- Sorted glass with tinted transmission, refraction and coloured shadows; screen-space reflections.
- Tools: Texture Studio, Bake Panel and Debug window.
