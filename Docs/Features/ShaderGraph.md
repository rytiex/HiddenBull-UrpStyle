# Shader Graph

The package's look is available in Shader Graph without writing code, as a node library and a ready
graph template. Graphs use the **URP Unlit** target; the HB nodes do the lighting.

## HB Lit template

Create one with **Create → Shader Graph → HiddenBull Style → Lit Shader Graph**, or pick **HB Lit** in
**Create → Shader Graph → From Template...**. The shader appears as `HB URP Style/<graph name>`.

It comes wired:

```
Base Map × Base Color ┐
Normal Map ───────────┼─ HB_GraphSurface ─ HB Weather Static ─ HB Style Lighting ─ Base Color
Mask Map ─────────────┘
```

Material settings: Base Map, Base Color, Normal Map, Normal Strength, Mask Map, Metallic, Smoothness,
Occlusion Strength, Wetness, Snow Cover, Diffuse Wrap, Diffuse Softness, Half Tone, Brush Size and Brush
Space. For things that move, replace HB Weather Static with HB Weather Moving.

## Nodes

Find them under **HB URP Style** in the Create Node menu. Position, normal and screen position come from
inside the nodes; a Normal input left at zero uses the mesh normal.

| Node | Inputs | Outputs |
|---|---|---|
| HB Style Lighting | Albedo, Normal WS, Metallic, Smoothness, Occlusion, Emission, Diffuse Wrap, Diffuse Softness, Half Tone, Wet, Brush Size (0 = off), Brush Space (0 World, 1 Object), Brush Shading / Relief / Albedo / Ambient | Color |
| HB Weather Static | Albedo, Smoothness, Normal WS, Wetness, Snow Cover | Out Albedo, Out Smoothness, Out Normal WS, Wet |
| HB Weather Moving | As Static, plus Drop / Flake Density, Size and Life | As Static |
| HB Fog | Color | Out |

Connect all four weather outputs to the lighting node: drops, ripples and running water show mostly
through the normal and the smoothness.

The lighting node brings its own keywords (main light shadows and cascades, soft shadows, additional
lights, Forward+ and probe volumes), so shadows and lights work without extra setup. **HB Fog** is for
transparent graphs only; opaque graphs are fogged by the package's fog pass.

## Not available in Shader Graph

See [Known Limitations](../KnownLimitations.md#shader-graph).
