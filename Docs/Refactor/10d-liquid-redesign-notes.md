# 10d Liquid redesign notes (2026-11-17, discussion draft, nothing built yet)

## Why the current lava breaks

- The crust is not read from a mask. `LiquidWaves.compute` PlateMask guesses plates from the normal map:
  flat = plate, steep = crack (slope smoothstep 0.4-0.7, 5x5 max).
- Studio lava Height is now Rocks + Swirl: rocks are bumps, open melt is flat. The guess flips:
  - rock edges become glowing "cracks" (orange rings);
  - all open melt becomes one giant plate. PlateSpread (384 rounds) cannot label it, so it splits into
    square blocks with different hash tones, and blocks above the Crust threshold turn dark.
- Every runtime threshold on derived data is a speckle source:
  - plate detection and its size filter;
  - the crust threshold (S:1143);
  - the rim edge (S:1491);
  - veins = 1 - plate (S:1219);
  - clusters / open noise (S:1065, fixed 7 m cell).

## Current Look: overlaps found

- Crust > 0 silently disables PatternMap (Look.cs:130). Crust and Lines both trigger the plate bake.
- DiffuseWrap / DiffuseSoftness sit in Crust but light everything (S:1375).
- PatternScale's tooltip says crust patches, but crust uses the ripple tiling.
- Tiling also scales foam, glints and bubbles (FoamShape.z).
- Pulse drives both bubble timing and crust throb.
- SurfaceDistortion vs underwater Distortion, and RefractionBlur vs UnderBlur, have similar names but
  different roles.
- LineRim / Swirl / Clusters are dead when Lines = 0.

## Proposed shape

### Page (like the Material page)

- Left panel: the picked layer's settings. With nothing picked, the Look sections.
- Middle: one layer list, plus the Look entry.
- Right: preview (Liquid / Flat, map keys N H T S L) with the preset shelf at the bottom.

### Presets

- JSON under `Presets/Liquid/<Category>/`: Water, Acid, Lava, plus user saves.
- Shelf with Revert / Save / Save As / Delete, same as the Material shelf.
- A preset holds the layer list and every Look value.

### Layers

- One list, not channel tabs.
- Each layer is a Shape (kind + its sliders) plus Targets:
  - Tone, Solid, Lines: each with an on/off, Blend, Threshold, Feather and Opacity;
  - Height: on/off and Strength.
- One Rocks layer can feed Solid and Height together, so rock mask and rock bumps always match.

### Look = the system

Sections:
- Colour: gradient by Tone, depth tint, glow.
- Solid: rock colour, edge glow width/colour.
- Lines: colour, opacity.
- Motion: flow speed, swirl, rock drift.
- Waves.
- Surface / Lighting: smoothness, refraction, diffuse wrap moved here.
- Foam and Bubbles.
- Underwater.

### Removed

- Crust, Pattern section, plate bake (StyleLiquidPlates, Plate* kernels), clusters, rim edge, veins.

### Shader

The mask map is RGBA: R Tone, G Solid, B Lines, A Height (A is studio only).
- Tone and Lines use a two-phase flow: 2 taps cross-fading along a slow flow field, so nothing smears.
- Solid uses one tap drifting with the flow, so rocks never fade.
- Rock edge glow comes from a band around the authored mask edge (mask is pre-feathered, mips keep it
  soft), not from detection.
- Wall transition: the existing shore depth ramp makes a crust band plus a glow line, soft instead of cut.
- Reads: the plates tap and the roots are gone; the mask adds 3 taps per live layer.
  Budget check needed before building.

## Open questions for the user

Answered 2026-11-17, all recommended options:
- Layers: one list + Targets (no channel tabs).
- Presets: Material-style shelf (Water, Acid, Lava + user saves; Revert / Save / Save As / Delete).
- Look: a fixed "Look" row at the top of the layer list opens the Look sections on the left; no tabs.
- Old system: remove completely (Crust, Pattern section, plate bake, clusters, rim edge, veins). Old LIQ
  assets must be saved again.

Not started. Start only after the user says everything is okay.

## Build status (2026-11-17)

Built as one job: Look row + one layer list with per-layer Targets (Tone/Solid/Lines + Height), preset shelf
(built-in Liquid/Water, Acid, Lava + user JSON presets), mask map baked in the Studio and read by the runtime,
plate system, Crust, Pattern map and their kernels removed. Liquid Surface 3768 slots / 148 reads. Compiled offline,
not run in Unity. Waiting for the user's single final look (lava first).
