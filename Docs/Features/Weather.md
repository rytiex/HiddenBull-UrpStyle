# Weather

Rain, snow and wind come from the **Style Weather** volume override. Surfaces whose material has
**Receive Weather** set to Static or Moving wet and snow over time wherever they have open sky above.

## Style Weather volume

| Setting | Meaning |
|---|---|
| Rain, Snow, Wind, Wind Heading | Intensity of each, and the wind direction |
| Rain Color | Tint of falling rain |
| Speed | Multiplies every timing below |
| Instant | Skips all timing (also always on outside Play mode) |
| Wet Time | Seconds of full rain until open surfaces are soaked |
| Dry Time | Seconds after the rain until they are dry |
| Settle Time | Seconds of snowfall until the snow lies at full depth |
| Melt Time | Seconds after the snow stops until it has melted |
| Cover Time | Seconds until something that comes to rest over a spot has cleared the snow under it |
| Uncover Time | Seconds until an uncovered spot is snowed on and wetted like its surroundings again |

The volume's preset buttons (Clear, Drizzle, Rain, Storm, Light Snow, Blizzard, Sleet) set rain, snow
and wind only.

## Shelter

The renderer feature keeps a per-camera shelter map: a 512 × 512 height map 160 m around the camera,
rendered from above, plus a coarser 1 km map farther out. A surface is sheltered where something is
above it, so floors under roofs and ground under trees stay dry and bare.

- **Static** materials and every opaque renderer without Receive Weather shelter the ground under them.
- **Moving** materials never shelter: a character or a parked car leaves the snow and rain alone.
- Terrain never shelters (it is the ground).
- Small things roughly under 1.5 m across do not keep rain off; snow still respects them.
- When a sheltering object comes to rest, the spot under it clears over **Cover Time**; when it leaves,
  snow and wetness return over **Uncover Time**, the same on flat ground and on slopes.

**HB Style Weather Mask** (Environment) marks a sphere as sheltered, for example a cave mouth, and can
also block falling precipitation.

## Falling rain and snow

Drawn as GPU-generated drops in boxes around the camera, with a far layer filling the distance. Drops
under a roof or behind glass are hidden. The amount drawn follows the renderer feature's
**Precipitation** quality: Ultra is the full look, lower levels draw fewer drops, Low drops the far
layer, Off draws none (surfaces still wet and snow).

## Debugging

Set **Weather Debug** on the renderer feature to Sky Exposure, Wetness, Snow, Limits or Soak to see each
term on screen.
