# Scripting

Everything is in the `HiddenBull.UrpStyle` namespace.

## Quality at runtime

`HBStyleRenderSettings` holds one level per module, plus `lensFlare`, an on/off switch for the lens flare. The
game decides the levels (for example from its own graphics menu) and saves them itself; the package does not store
them.

```csharp
var settings = HBStyleRenderSettings.Get();
settings.shadows = HBStyleQualityLevel.High;
settings.grass = HBStyleGrassQualityLevel.Medium;
HBStyleRenderSettings.Set(settings);
```

| Member | |
|---|---|
| `Get()` | The levels in use now: the ones set from code, or the Quality Profile's when nothing was set |
| `Set(settings)` | Overrides the Quality Profile for every camera |
| `Restore()` | Goes back to the Quality Profile |
| `authored` | The Quality Profile's levels |
| `From(profile)` | The levels of any Quality Profile, for "Low / Medium / High" presets in a menu |
| `isSet`, `changed` | Whether an override is active, and an event when it changes |

The override ends when Play mode stops. What each level does is in [Performance](Performance.md).

## Environment and weather

`HBStyleEnvironment` reads and drives the scene's Environment Controller.

```csharp
HBStyleEnvironment.timeOfDay = 18.5f;
HBStyleEnvironment.SetWeather(rain: 1f, snow: 0f, wind: 0.8f, seconds: 30f);
HBStyleEnvironment.BlendTo(stormProfile, seconds: 10f);
```

| Member | |
|---|---|
| `timeOfDay` | Hour, 0 to 24; read and write |
| `rain`, `snow`, `wind` | Current weather, 0 to 1 |
| `wetness`, `snowCover` | How wet and snowy open surfaces are right now |
| `SetWeather(rain, snow, wind, seconds)` | Moves the weather to new values over time |
| `SetProfile(profile)`, `BlendTo(profile, seconds)` | Switches the Environment Profile at once or over time |
| `profile`, `profileBlend`, `controller` | The target profile, blend progress, and the controller |
| `weatherChanged`, `profileChanged` | Events |

## Textures from code

Lit, Transparent, Foliage and Foliage Body skip a map slot that has no texture. The material Inspector turns the
slot on when you assign a texture, but `Material.SetTexture` from code does not, so the texture stays unused.
`HBStyleMaterial` sets the texture and turns the slot on or off with it:

```csharp
HBStyleMaterial.SetTexture(renderer.material, "_BumpMap", normalMap);
```

| Member | |
|---|---|
| `SetTexture(material, name, texture)` | Sets the texture; `null` turns the slot off |
| `Refresh(material)` | Matches every slot to the textures already set, after changes made another way |

It covers Base Map, Normal Map, Mask Map and the Translucency Map. A build only keeps the slots that some material
in the project uses: if no material asset has a normal map on that shader, a normal map set from code shows in the
editor but not in the build. Keep one material asset with the map assigned to be safe. Shader Graph materials are
not covered.

## Liquid queries

`HBStyleLiquid` answers where the surface of any liquid is, terrain water and pools alike, including waves.
Use it for floating objects, swimming and footstep sounds.

```csharp
if (HBStyleLiquid.TryGetSurface(transform.position, out var height, out var normal))
{
    var underwater = transform.position.y < height;
}
```

| Member | Returns |
|---|---|
| `TryGetHeight(position, out height)` | Height of the liquid surface at the point's X and Z |
| `TryGetSurface(position, out height, out normal)` | Height and surface normal |
| `TryGetDepth(position, out depth)` | Distance from the surface down to the floor |
| `any` | Whether any liquid exists |

Each takes an optional `waves` argument (default `true`); `false` reads the still level. Each also has an overload
with `out Component liquid`, which tells you which terrain or pool was hit.

**Multiplayer**: waves move with a shared clock and the wind. Call `HBStyleLiquid.Sync(time, wind, windHeading)`
with the server's values so floating objects match on every machine; `Unsync()` goes back to the local clock.

## Terrain queries

| `HBStyleTerrain` member | Returns |
|---|---|
| `TryGetHeight(position, out height)` | Ground height under the point |
| `TryGetNormal(position, out normal)` | Ground normal |
| `GetLayerAt(position)` | The strongest ground layer, for footstep sounds or effects |
| `GetLayersAt(position, layers, weights)` | Up to three strongest layers at the point with their weights (0–1); returns how many it found |

The layer queries read the baked layer map, so hand painting counts once the terrain is baked. They return `null`
or `0` outside the terrain or before the first bake. Compare the result with your **HB Style Terrain Layer**
assets; a footstep or impact lookup is one call:

```csharp
var terrain = hit.collider.GetComponentInParent<HBStyleTerrain>();
var layer = terrain != null ? terrain.GetLayerAt(hit.point) : null;
```

Each call reads four pixels and allocates nothing; reuse the `layers` and `weights` arrays.

## Harvesting plants

Painted plants with colliders get an `HBStylePlantBody` in Play mode. Harvest hides the plant and turns its
colliders off:

```csharp
if (Physics.Raycast(ray, out var hit) && hit.collider.TryGetComponent<HBStylePlantBody>(out var plant))
    plant.Harvest();
```

| `HBStyleTerrain` member | |
|---|---|
| `Harvest(id)`, `Restore(id)` | Removes or brings back one plant |
| `harvested` | The ids of every harvested plant |

Harvest state lives only at runtime. Save `harvested` with the game and call `Harvest(id)` for each id on load.
Ids are the same on every machine, so in multiplayer send `plant.id` and call `terrain.Harvest(id)` everywhere;
a player who joins late receives `terrain.harvested`.
