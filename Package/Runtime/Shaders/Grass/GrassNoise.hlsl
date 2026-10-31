#ifndef HB_GRASS_NOISE_INCLUDED
#define HB_GRASS_NOISE_INCLUDED

#define HB_GRASS_CLUMP_WIDE   5.0
#define HB_GRASS_CLUMP_MID    2.7
#define HB_GRASS_CLUMP_FINE   1.3
#define HB_GRASS_CLUMP_SPREAD 2.5
#define HB_GRASS_CLUMP_HEIGHT 0.5
#define HB_GRASS_CLUMP_TONE   0.35
#define HB_GRASS_GUST_SPEED   3.0
#define HB_GRASS_GUST_RISE    7.0
#define HB_GRASS_GUST_WIDE    45.0
#define HB_GRASS_GUST_LONG    20.0
#define HB_GRASS_GUST_LOW     0.45
#define HB_GRASS_GUST_HIGH    0.8

float HB_GrassNoise(float2 position)
{
    float2 cell = floor(position);
    float2 blend = position - cell;
    float2 step = float2(127.1, 311.7);
    float4 corners = frac(sin(float4(dot(cell, step), dot(cell + float2(1.0, 0.0), step),
                                     dot(cell + float2(0.0, 1.0), step), dot(cell + float2(1.0, 1.0), step)))
                          * 43758.5453);

    blend = blend * blend * (3.0 - 2.0 * blend);

    return lerp(lerp(corners.x, corners.y, blend.x), lerp(corners.z, corners.w, blend.x), blend.y);
}

float HB_GrassClump(float2 xz)
{
    float2 turned = float2(xz.x * 0.8 - xz.y * 0.6, xz.x * 0.6 + xz.y * 0.8);
    float2 crossed = float2(xz.x * 0.28 + xz.y * 0.96, xz.y * 0.28 - xz.x * 0.96);
    float wide = HB_GrassNoise(xz / HB_GRASS_CLUMP_WIDE);
    float mid = HB_GrassNoise(turned / HB_GRASS_CLUMP_MID + 11.3);
    float fine = HB_GrassNoise(crossed / HB_GRASS_CLUMP_FINE + 23.7);

    return clamp((wide * 0.5 + mid * 0.3 + fine * 0.2 - 0.5) * HB_GRASS_CLUMP_SPREAD, -1.0, 1.0);
}

#endif
