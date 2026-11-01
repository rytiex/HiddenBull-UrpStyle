#ifndef HB_GRASS_NOISE_INCLUDED
#define HB_GRASS_NOISE_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWind.hlsl"

#define HB_GRASS_CLUMP_WIDE   5.0
#define HB_GRASS_CLUMP_MID    2.7
#define HB_GRASS_CLUMP_FINE   1.3
#define HB_GRASS_CLUMP_SPREAD 2.5
#define HB_GRASS_CLUMP_HEIGHT 0.5
#define HB_GRASS_CLUMP_TONE   0.35
#define HB_GRASS_BEND_DROP    0.35
#define HB_TRAIL_SPAN         64.0
#define HB_SNOW_PRESS_DARK    0.35h

float HB_GrassClump(float2 xz)
{
    float2 turned = float2(xz.x * 0.8 - xz.y * 0.6, xz.x * 0.6 + xz.y * 0.8);
    float2 crossed = float2(xz.x * 0.28 + xz.y * 0.96, xz.y * 0.28 - xz.x * 0.96);
    float wide = HB_WindNoise(xz / HB_GRASS_CLUMP_WIDE);
    float mid = HB_WindNoise(turned / HB_GRASS_CLUMP_MID + 11.3);
    float fine = HB_WindNoise(crossed / HB_GRASS_CLUMP_FINE + 23.7);

    return clamp((wide * 0.5 + mid * 0.3 + fine * 0.2 - 0.5) * HB_GRASS_CLUMP_SPREAD, -1.0, 1.0);
}

#endif
