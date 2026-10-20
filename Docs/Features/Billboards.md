# Billboards

The billboard system replaces LOD Groups for plants. Every copy of a plant is culled and drawn on the
GPU in one go: meshes up close, an 8-view billboard far away, nothing past the hide distance.

## Baking

Open **Tools → HiddenBull → URP Style → Billboard Baker**, or right-click an HB Style Billboard
component → Bake Billboard.

| Field | Meaning |
|---|---|
| Target | A plant in the scene or a prefab in the Project window. Prefabs are baked in place, so every copy updates. |
| Billboard From | Distance in metres where the plant turns into its billboard |
| Hide After | Distance in metres where the billboard disappears |
| Folder, Name | Where the files go and their base name |

The bake writes `TEX_Billboard_<Name>_Albedo/Normal`, `MESH_Billboard_<Name>`, `MAT_Billboard_<Name>`
and `DATA_Billboard_<Name>`, adds **HB Style Billboard** to the root, and removes an old LOD Group and
Billboard child left by earlier versions.

The bake captures every mesh renderer under the root, so a tree's trunk (Lit) and leaves (Foliage)
become one billboard. Re-bake after changing the plant's meshes or materials.

## Distances

Distances are for a 60° field of view. Zooming in (a smaller field of view) pushes both distances out,
so plants seen through a scope stay meshes. They can be changed on the DATA asset at any time without
re-baking. Transitions cross-fade with a dither over 10% of each distance.

## How it draws

- A compute shader culls all copies against the camera (plants outside the view whose shadows reach
  into it are kept) and sorts them into mesh and billboard lists.
- Each mesh / material pair and the billboard are one indirect draw for all copies.
- Lighting comes from Adaptive Probe Volumes per pixel, or from light probes per copy.
- The originals' renderers are hidden; selecting a plant by clicking in the Scene view still works.
- In Prefab Mode the plant shows its normal meshes.

The component's inspector shows, for the Game view, how many copies are drawn as meshes, as billboards
and not at all, and the triangles drawn. Unity's Statistics window does not count GPU-driven draws.

## Terrain

Baked prefabs painted with Paint Trees are drawn the same way; see [Terrain](Terrain.md#trees-and-bushes).
