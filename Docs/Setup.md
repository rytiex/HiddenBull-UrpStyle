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

The feature has six settings:

- **Quality**: an HB Style Quality Profile with the level of each module. Leave it empty to use the defaults.
  Levels are explained in [Performance](Performance.md); a game can change them at runtime, see
  [Scripting](Scripting.md#quality-at-runtime).
- **Overlay Shadow Near**: for first-person hands or weapons drawn by an overlay camera.
- **Brush**: the brush atlas, the world size of one tile, the stroke fade distances and the break-up, shared by
  every HB Style shader in every scene. Make the atlas in the [Texture Studio](Tools.md#texture-studio) (Brush
  page). Older Environment Profiles keep their brush hidden; the feature copies it once.
- **Bloom**: a wide, soft glow around bright areas such as sunlit windows, the sky and lamps, the same in every
  scene. The glow keeps the colour of what shines; **Tint** only warms or cools all of it. It works best with HDR
  on the URP asset; without HDR only near-white pixels glow. The Bloom quality level sets how wide it reaches.
- **Lens Flare**: two parts that turn on and off apart. **Sun Intensity** draws a glow, thin rays, ghosts and a
  halo from the sun while it is on screen; it fades as the sun goes behind objects, clouds or the horizon, is off
  underwater and takes the sun's colour. **Light Intensity** adds faint ghosts from small bright lights such as
  lamps and headlights; large bright areas never flare, and **Light Threshold** keeps water glints and dim lamps
  out. It follows the Bloom quality level (Off turns it off too) and works with Bloom Intensity at 0.
  **Lighting → Lens Flare Sun** in the debug window shows the sky the sun is tested against (grey), where the sun
  is (red ring) and how visible it is (bar).
- **Ambient Occlusion**: soft shading in corners, creases and under objects, the same in every scene. **Radius**
  is how far in metres a surface looks for what blocks its ambient light; far from the camera the reach grows so
  distant buildings keep their shading. **Direct Lighting** lets it darken sunlight and lamps too. It shades HB Lit,
  Foliage and Terrain surfaces (not grass or glass). Remove URP's own Screen Space Ambient Occlusion feature from
  the renderer; both write the same texture. **Lighting → Ambient Occlusion** in the debug window shows it alone.

## Profiles

Create profiles from *Create → HiddenBull → URP Style → Profiles*.

| Profile | Holds |
|---|---|
| Environment | Environment side: sky, light, sun and moon, night sky, clouds. Atmosphere side: fog for clear, rainy and snowy weather, overcast, falling rain and snow, wet and snowy surfaces. |
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
- The HB Style Terrain takes baked shadows but no baked light: each Bake gives it a shadowmask at 2 texels per
  metre (up to Max Lightmap Size), so static shadows reach it beyond the shadow distance and with shadows off.

## Cameras

- The base camera draws everything. Overlay cameras (hands, weapons) take fog and weather from their base camera
  but draw no fog pass, rain or grass of their own.
- Falling rain and snow are drawn only when the Environment Controller's layer is in the camera's culling mask.
