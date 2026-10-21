# Materials

All shaders share one lighting library, so the style is identical across them: wrapped diffuse with a
soft, painted terminator, half-tone shading, gradient ambient, the package's contact-hardening shadows
and the brush layer.

## HB URP Style / Lit

The master shader for everything that is not a plant, terrain or liquid. Sections can be switched on
from their headers; a section that is off costs nothing.

| Section | Contents |
|---|---|
| Surface | Opaque / transparent, blend mode, alpha clip, cast colored shadows (transparent only) |
| Maps | Base, Normal and Mask maps; Triplanar Mapping with its own Mapping Space |
| Shading | Diffuse Wrap, Diffuse Softness, Half Tone, baked tones |
| Brush | Painterly strokes over the lighting, in their own Brush Space |
| Specular, Rim | Painterly specular highlight and rim light |
| Subsurface | Skin or cloth transmission |
| Emission | HDR emission color and map |
| Weather | Receive Weather, Wetness, Snow Cover, drop and flake settings |

### Mask Map

| Channel | Meaning |
|---|---|
| R | Metallic |
| G | Ambient occlusion |
| B | Not used by Lit (terrain layers use it as height) |
| A | Smoothness (multiplied by the Smoothness slider) |

Build one from separate maps with **Texture Studio → Mask Map** ([Tools](../Tools.md)).

### Triplanar Mapping and spaces

**Triplanar Mapping** lays the Base, Normal and Mask maps from three sides instead of by the mesh's
UVs, so meshes without usable UVs are still textured. Its **Mapping Space** is World, Object or Rest
Pose. The brush has its own **Brush Space**, so a character can keep UV textures while the brush is
anchored to Rest Pose, or the other way round.

Rest Pose needs a skinned mesh whose rest pose was written at import (Project Settings → HiddenBull URP
Style).

### Receive Weather

| Value | Use it for | Behavior |
|---|---|---|
| None | Water, fire, screens | Weather does not touch the surface |
| Static | Ground, walls, rocks, buildings | Full wetness, drop spots, running water on slopes, ripples on flat ground, a settling snow blanket. Patterns are in world space. Shelters what lies under it. |
| Moving | Characters, vehicles, props | Wets like Static, but rain leaves beaded drops and snow lies as small flakes. They travel with the object (set Brush Space to Rest Pose on animated characters). Never shelters the ground under it. |

See [Weather](Weather.md) for the global side.

### Transparency

Transparent Lit materials are drawn by the package's order-independent transparency (moment-based
OIT), with refraction inside it, so overlapping glass needs no sorting. **Cast Colored Shadows** makes
a transparent surface tint the light passing through it instead of casting a solid shadow.

Blend modes: **Alpha** fades the whole surface, **Premultiply** keeps highlights at full strength (glass,
water), **Additive** adds light without darkening what is behind (fire, glows, beams; no shadow, no
refraction; set Receive Weather to None).

## HB URP Style / Foliage

Lit's variant for leaves, bushes and grass cards:

- Wind with a rest-space anchor, so the brush and weather beads do not slide while leaves move.
- Canopy normals: lighting uses the normal of a rounded canopy fitted to the renderer bounds, so a bush
  reads as one soft volume instead of a noisy pile of cards. A tall bush keeps lit sides down to its base
  and a long, low hedge keeps its lower half facing outwards. **Canopy Shape** blends back towards the
  mesh normals, for meshes whose normals were already transferred from a proxy shape. Combining several
  plants into one mesh gives them one shared canopy.
- Grass-like translucency in **Translucency Color**: it lights the side away from the sun all day and
  is strongest with a low sun.
- Snow and rain settle on the canopy as a whole: weather uses the leaf's normal averaged with the canopy
  normal, so up-facing leaves on the upper part of the plant take snow and undersides stay bare. Leaves
  hold snow on gentler slopes than the ground and right to the plant's rim; snowy leaves stop glowing.
- GPU Instancing is on by default.
- Its material holds only Foliage's own properties (no metallic, specular or rim), so it stays
  compatible with the SRP Batcher and the GPU Resident Drawer.

## Billboard

`Hidden/HiddenBull/Style Billboard` is not picked by hand: the [Billboard Baker](Billboards.md) creates
its material. It turns 8 baked views toward the camera, lights them with canopy normals and receives
weather like the plant it replaces.

## Decal and Particle

- **HB URP Style / Decal** — URP decal projector shader in the package's style.
- **HB URP Style / Particle** — transparent particles with the package's lighting and fog.
