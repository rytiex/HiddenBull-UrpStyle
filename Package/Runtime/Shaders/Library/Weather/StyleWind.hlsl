#ifndef HB_STYLE_WIND_INCLUDED
#define HB_STYLE_WIND_INCLUDED

#define HB_GUST_SPEED 3.0
#define HB_GUST_RISE  7.0
#define HB_GUST_WIDE  45.0
#define HB_GUST_LONG  20.0
#define HB_GUST_LOW   0.45
#define HB_GUST_HIGH  0.8
#define HB_GUST_LAG   2.5
#define HB_GUST_FINE  16.0
#define HB_GUST_SHORT 8.0

float HB_WindNoise(float2 position)
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

float HB_GustStreak(float2 xz, float seed, float2 wind, float strength, float time)
{
    float along = dot(xz, float2(-wind.y, wind.x));
    float ahead = dot(xz, wind) + seed * HB_GUST_LAG;
    float speed = HB_GUST_SPEED + HB_GUST_RISE * strength;

    float wide = HB_WindNoise(float2(along / HB_GUST_WIDE, (ahead - time * speed) / HB_GUST_LONG));
    float fine = HB_WindNoise(float2(along / HB_GUST_FINE + 7.3, (ahead - time * speed * 1.6) / HB_GUST_SHORT));

    return smoothstep(HB_GUST_LOW, HB_GUST_HIGH, wide * 0.65 + fine * 0.35);
}

#endif
