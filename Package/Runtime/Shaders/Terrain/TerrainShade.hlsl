#ifndef HB_TERRAIN_SHADE_INCLUDED
#define HB_TERRAIN_SHADE_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainInput.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"

#define HB_TERRAIN_CURVE_SCALE      2.0
#define HB_TERRAIN_CURVE_SHADE      0.5h
#define HB_TERRAIN_MACRO_SHADE      0.175h
#define HB_TERRAIN_MACRO_LARGE      24.0
#define HB_TERRAIN_MACRO_SMALL      7.0

half HB_TerrainBend(float curvature)
{
    return half(clamp(curvature * HB_TERRAIN_CURVE_SCALE, -1.0, 1.0));
}

half HB_TerrainShade(float2 xzWS, half bend)
{
    half macro = half(HB_BrushField(xzWS / HB_TERRAIN_MACRO_LARGE) * 0.65
                    + HB_BrushField(xzWS / HB_TERRAIN_MACRO_SMALL) * 0.35);
    half shade = 1.0h - bend * half(HB_TERRAIN_CURVATURE) * HB_TERRAIN_CURVE_SHADE
               + macro * half(HB_TERRAIN_MACRO) * HB_TERRAIN_MACRO_SHADE;

    return max(shade, 0.0h);
}

#endif
