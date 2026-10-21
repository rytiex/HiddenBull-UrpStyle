# Terrain

## Setup

1. Give the terrain a material with **HB URP Style → Terrain** as its Material Template.
2. Add **HB Style Terrain** (Terrain category) to the terrain object.
3. Assign a **Profile** and open **Tools → HiddenBull → URP Style → Terrain Layers** (or the Open
   Terrain Layers button) to edit the layers.

## Layers

- **Painted layers** are Unity terrain layers, sampled from texture arrays: the strongest 3 per pixel
  are used.
- **Automatic layers** dress the ground by slope and height ranges, in order, with softness, weight,
  variation (patches of tint), anti-tiling and a luminance height blend at their edges. They are baked
  into the splat maps; **Auto Bake** re-bakes when the terrain changes, including undo and redo.
- Curvature shading darkens hollows and lifts ridges.
- Patches beyond the terrain's **Base Map Dist** are drawn with the same layer colours the near shader
  fades to at distance, from the package's layer arrays rather than Unity's baked basemap.
- Each automatic layer can carry grass and flowers.

The **Layer Map** debug view shows each automatic layer in a fixed color.

## Grass

GPU grass is generated around the camera wherever a layer has grass and the terrain material has a
**Clump Texture** (make one in Texture Studio → Grass). There is no component: it follows the
renderer feature's **Grass** quality (distance, density, translucency per level). Near grass is
crossed cards with full detail; distant grass is a single camera-facing card.

**HB Style Grass Bender** (Vegetation) pushes grass aside around an object.

## Trees and bushes

Paint plants with Unity's **Paint Trees** tool. Prototypes whose prefab was baked with the
[Billboard Baker](Billboards.md) are drawn by the GPU billboard system: meshes up close, billboards far
away. The HB Style Terrain inspector lists every prototype as **GPU** or **Not baked**, with a Bake
button.

When at least one prototype is baked, Unity's own tree drawing is switched off for that terrain, so
unbaked prototypes are not drawn until baked. Tree colliders keep working. While it is off the terrain's
Tree Distance reads 0; the original value is put back whenever the scene is saved or play mode changes.
