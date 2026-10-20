# Performance

Performance is the first requirement of the package. Numbers below are from the development PC
(RTX 3060, 1080p, Unity editor) and are a guide, not a guarantee.

## Quality settings

| Where | Setting | Effect |
|---|---|---|
| Renderer feature | Grass | Off to Ultra: distance, density, translucency per level |
| Renderer feature | Precipitation | Off to Ultra: share of drops drawn, far layer |
| Renderer feature | Liquid Quality | Low / Medium / High liquid detail |
| Renderer feature | Shadows | Quality, distance, softness, contact hardening |
| URP renderer | Depth Priming Mode | Forced, set by the feature: dense foliage is shaded once per pixel |

## Changing quality at runtime

`HBStyleRenderSettings` reads and writes the player-facing quality settings of the renderer feature, for a
game's settings menu. It works in builds and in Play mode.

```csharp
var settings = HBStyleRenderSettings.Get();

settings.shadowQuality = HBStyleShadowQuality.Medium;
settings.grass = HBStyleGrassQuality.Low;
settings.precipitation = HBStylePrecipitationQuality.High;
settings.liquid = HBStyleLiquidQuality.Medium;

HBStyleRenderSettings.Set(settings);
```

| Field | Values |
|---|---|
| shadowQuality | Low / Medium / High / Ultra; changing it resets the distance to that level's default |
| shadowDistance | Metres, clamped to the range of the quality level |
| grass | Off / Low / Medium / High / Ultra |
| precipitation | Off / Low / Medium / High / Ultra |
| liquid | Low / Medium / High |

- `Set` writes only what differs, to every HiddenBull Style feature of the active URP asset, and returns
  whether anything changed. Changes take effect on the next frame.
- `TryGet` returns false when no renderer has the feature; `Get` then returns `HBStyleRenderSettings.Default`.
- The struct is serializable, so a game can save it with `JsonUtility`.
- In the editor, values changed in Play mode are put back when Play mode ends, so a settings menu does
  not overwrite the renderer asset.
- Shadow quality changes cascades, resolution and distance on the URP asset at runtime. URP's soft
  shadow quality can only be written in the editor, so in a build it stays at the authored value; the
  package's own shadow filter still follows the quality level.

## What costs what

| System | Cost notes |
|---|---|
| Lit / Foliage | Fragment cost is dominated by weather; a material with Receive Weather None skips it |
| Terrain | Strongest 3 painted layers per pixel; automatic layers baked into splat maps |
| Grass | Around 2.5–4.5 ms on Ultra in a full-screen meadow; lower levels scale down |
| Billboards | One indirect draw per mesh/material pair per pass for all copies; CPU cost does not grow with the number of plants |
| Shelter maps | Two extra top-down depth renders per camera (near 160 m and far 1 km, the far one refreshed every 2 s or 32 m) |
| Underwater | Full-screen pass only when the camera is within 1 m of the water or below it; the waterline is solved on a 64 × 36 grid |
| Colored transparent shadows | Their cull is skipped while no Cast Colored Shadows material is in view |

## Build stripping

At build time the package removes, from its own shaders only:

- the GPU billboard (procedural instancing) variants when the project has no baked billboard data;
- DOTS instancing variants when Entities Graphics is absent and GPU Resident Drawer is off in every URP
  asset.

URP strips its own keywords (shadows, shadow mask, reflection probes and so on) from the package's
shaders according to the URP asset settings.
