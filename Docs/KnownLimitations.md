# Known Limitations

## Weather

- The shelter map sees the world from above only. A floating object shelters the ground below it as if
  it reached down to the ground.
- Small sheltering objects (roughly under 1.5 m across) do not keep rain off.
- Static weather patterns are in world space and slide across a moving object; use Moving there.

## Billboards

- Only Mesh Renderers are captured and drawn; Skinned Mesh Renderers are not.
- Changing a plant's meshes or materials needs a re-bake.
- Selecting a GPU-drawn plant in the Scene view works, but no selection outline is drawn.
- On a terrain with any baked prototype, unbaked prototypes are not drawn.

## Shader Graph

Graphs use the URP Unlit target, so compared with the Lit shader they do not have:

- baked shadow mask (Mixed lighting) and lightmaps; they are lit by probes or Adaptive Probe Volumes;
- SSAO and URP decals;
- order-independent transparency, colored transparent shadows and the Rest Pose brush space;
- a Receive Weather setting: graph objects always shelter the ground under them.

## Transparency

- An Additive surface overlapping an Alpha or Premultiply surface in the same pixel can come out slightly
  brighter or dimmer than exact, because order-independent transparency normalises the layers together.

## Statistics window

Unity's Statistics window does not count GPU-driven (indirect) draws, so billboards and grass are
missing from its triangle and vertex counts. The HB Style Billboard inspector shows the real numbers
for billboards.
