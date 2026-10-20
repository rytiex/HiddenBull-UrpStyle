# Tools

All tools are under **Tools → HiddenBull → URP Style**.

## Texture Studio

One window with a page per texture kind. Pages that have presets offer Revert / Save / Save As; presets
are JSON files in `Package/Editor/Authoring/Presets`. Saving asks for a folder (remembered) and names
files `TEX_<Category>_<Type>_<What>_<Index>`.

| Page | Makes |
|---|---|
| Terrain | Tileable hand-painted ground textures (rock, dirt, sand, grass ground, gravel, mud, forest floor with procedural leaves, cobblestone, cliff): albedo, normal, mask and an optional Terrain Layer |
| Grass | Grass clump and flower-head textures for the terrain's grass |
| Liquid | Liquid normal and crust textures |
| Brush | The brush stroke atlas |
| Cloud | The cloud atlas |
| Mask Map | Packs separate maps into the Lit Mask Map: Metallic (R), Occlusion (G), Height (B), Smoothness (A) |

### Mask Map page

For each channel pick a texture, the channel to read (Red, Green, Blue, Alpha or Gray) and **Invert**
(turns a roughness map into smoothness), or leave the texture empty and set a constant **Value**.
Source textures do not need Read/Write, and textures imported as sRGB keep their stored values.
Resolution is the largest input or a fixed size. The map is saved as `TEX_Mask_<Name>_<Index>.png`
with linear import settings; HB Lit or Foliage materials selected in the Project window get it as their
Mask Map with Use Mask Map turned on.

## Billboard Baker

Bakes plants for the GPU billboard system. See [Billboards](Features/Billboards.md).

## Terrain Layers

Edits the HB Style Terrain profile: painted and automatic layers, ranges, variation, grass and flowers.
See [Terrain](Features/Terrain.md).

## Bake Panel

Lighting settings with the package's defaults, quality presets (Potato / Low / Default / Ultra),
checks with one-click fixes, and Bake / Cancel with a report. Lightmap mipmaps are stripped after
each bake unless mipmap limits are on.
