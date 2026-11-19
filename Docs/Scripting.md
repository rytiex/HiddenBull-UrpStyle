# Scripting

Everything is in the `HiddenBull.UrpStyle` namespace.

## Quality at runtime

`HBStyleRenderSettings` holds one level per module. The game decides the levels (for example from its own
graphics menu) and saves them itself; the package does not store them.

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
| `GetLayersAt(position, layers, weights)` | Every layer at the point with its weight |

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
