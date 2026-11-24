# Changelog

## 1.1.9

- No more "floating point division by zero" shader warning in the Underwater and liquid passes.

## 1.1.8

- Lens flare: a sun flare (glow, rays, ghosts and halo) that fades behind objects, clouds and the horizon and is off
  underwater, and faint ghosts from small bright lights. Sun and lights have their own strength;
  `HBStyleRenderSettings.lensFlare` turns it off from code. New Lighting → Lens Flare Sun
  debug view.
- Tools → HiddenBull → URP Style → Optimize Variants: finds shader keywords the project never uses (LOD cross
  fade, light cookies, DOTS instancing) and turns them off; `LIGHTMAP_SHADOW_MIXING` is stripped by itself when no
  Subtractive bake and no Shadowmask quality level exist. Shows variant counts from the last build.
- Bloom Intensity defaults to 1.
- Ambient occlusion reads depth and normals by pixel, so they always match when the Scene and Game views differ
  in size.
- The shelter map no longer breaks with glass shadows on High.
- Shadow filter loop no longer warns in single-cascade variants.

## 1.1.7

- Bloom and screen-space ambient occlusion, each with quality levels and an AO debug view.
- Cleaner glass shadow rendering.

## 1.1.6

- Terrain and grass use baked shadow masks; realtime shadows blend across cascades.

## 1.1.5

- Brush settings moved from the Environment Profiles to the renderer feature; old values are migrated.
- Weather state can be copied; plants are erased per kind.
- Plant shelter and liquid foam lighting adjusted.

## 1.1.4

- Terrain hex sampling reuses coordinates and weights and skips missing albedo, mask and normal maps.

## 1.1.3

- Terrain liquid shader variants are kept in builds.

## 1.1.2

- Foliage forward passes include URP Core.hlsl.

## 1.1.1

- No precipitation in preview cameras.
- Terrain textures copy correctly with a mipmap limit.

## 1.1.0

- Terrain layer sampling fades with distance (Layer Detail), with a debug view.
- Terrains can stop the sea beyond their edges.
- Filtered and preview cameras skip HB Style rendering.
- Terrain bake signatures no longer depend on the system language.

## 1.0.9

- Atmosphere settings live in the Environment Profile; old Atmosphere Profiles are migrated.

## 1.0.8

- Overcast dims the sky, clouds and fog from the light the scene lost, keeping baked ambient light.
- Texture Studio Mask page: metallic, occlusion, height and smoothness maps from constants, textures or estimates.

## 1.0.7

- Terrain layer query docs and painted state check fixed.

## 1.0.6

- Terrain maps are stored as lossless compressed data; layers become texture arrays at runtime.

## 1.0.5

- Shelter around liquid edges and liquid front faces fixed.
- Generated plant colliders keep the prefab's layer and tag.

## 1.0.4

- Weather Masks target Static, Moving or Both; foliage and suitable materials can cast shelter.
- Deep water shoreline coverage refined; pool height no longer draws from below.

## 1.0.3

- `HBStyleMaterial` keeps texture slots and shader keywords in step at runtime.

## 1.0.2

- Terrain holes are left out of the baked mesh.

## 1.0.1

- Bake Panel creates or saves the probe baking set for the active scene.

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
