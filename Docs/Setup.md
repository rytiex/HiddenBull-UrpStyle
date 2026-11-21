# Setup

## URP asset and renderer

| Setting | Value |
|---|---|
| Rendering Path | Forward+ |
| Depth Texture | On |
| MSAA | Off |
| Mixed Lighting | On |
| Light Probe System | Adaptive Probe Volumes |

Add the **HB Style** renderer feature to the Universal Renderer Data. It runs every pass of the package (style
globals, fog, shelter from rain and snow, grass and plants, glass, liquids, reflections, anti aliasing) and
sets Depth Priming to Forced on its renderer. Without it the shaders still draw, but fog, weather, grass, plants
and liquids are missing.

The feature has two settings:

- **Quality**: an HB Style Quality Profile with the level of each module. Leave it empty to use the defaults.
  Levels are explained in [Performance](Performance.md); a game can change them at runtime, see
  [Scripting](Scripting.md#quality-at-runtime).
- **Overlay Shadow Near**: for first-person hands or weapons drawn by an overlay camera.

## Profiles

Create profiles from *Create → HiddenBull → URP Style → Profiles*.

| Profile | Holds |
|---|---|
| Environment | Environment side: sky, light, sun and moon, night sky, clouds, brush. Atmosphere side: fog for clear, rainy and snowy weather, overcast, falling rain and snow, wet and snowy surfaces. |
| Quality | The level of each module: shadows, fog, weather, grass, terrain, glass, reflections. |

The Environment Profile inspector has two sides, **Environment** and **Atmosphere**, each split into tabs of
folding groups; the menu on a group's header puts back its default values. With **Enabled** off on the Atmosphere
side there is no fog, and rain and snow do not close the sky. Profiles made with an older version, which had a
separate Atmosphere Profile asset, take its settings over the first time the project opens; the old asset can
then be deleted.

## Environment Controller

Add **HB Style Environment Controller** to the scene's directional light (*Add Component → HiddenBull → HB Style →
Environment*). It is the one place where the scene's time and weather live:

- **Profile**: the Environment Profile.
- **Time Of Day** and **Heading**: move the sun and moon. Sky, fog, ambient and the light's colour and intensity
  follow. The controller writes the Light's colour and intensity, so values typed on the Light are replaced.
- **Rain**, **Snow**, **Wind**, **Wind Heading**: the weather. The preset buttons (Clear, Drizzle, Rain, Storm,
  Light Snow, Blizzard, Sleet) set these values.

Outside Play mode the weather applies at once. In Play mode surfaces wet, dry, gather snow and melt over the
times set on the Atmosphere side of the Environment Profile.

Set the scene's skybox to a material with the **HB URP Style/Sky** shader.

## Lighting and baking

The style is built for **Mixed lighting with Shadowmask**: static objects are lightmapped, everything else is lit
by Adaptive Probe Volumes, and the sun gives realtime shadows near the camera and baked shadows beyond.

Open *Tools → HiddenBull → URP Style → Bake Panel*. Its **Checks** tab lists what the scene needs and fixes most
items with one click: Baked GI, Shadowmask mode, Mixed Lighting and Adaptive Probe Volumes on the URP asset, a
probe baking set with Dilation on, a Mixed directional sun with shadows and the Environment Controller.
**Lighting** sets the bake quality from four presets, and **Bake** bakes lightmaps and probes together and shows a
report. See [Tools](Tools.md#bake-panel).

Things worth knowing:

- Set **Time Of Day** to the hour you want before baking; the baked sun comes from it.
- During a bake, fog and weather are held neutral and clouds keep the profile's Coverage, so the baked light
  looks right in any weather.
- Set the sun's **Shadow Near Plane** to 1 or more; lower values can leak shadow on very large faces.
- Turn on **Dilation** in the probe baking set; without it dark contour lines appear on the ground.
- The HB Style Terrain takes no baked light: it gets realtime shadows and the style's ambient.

## Cameras

- The base camera draws everything. Overlay cameras (hands, weapons) take fog and weather from their base camera
  but draw no fog pass, rain or grass of their own.
- Falling rain and snow are drawn only when the Environment Controller's layer is in the camera's culling mask.
