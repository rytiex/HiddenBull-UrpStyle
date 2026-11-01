#ifndef HB_STYLE_WEATHER_NOISE_INCLUDED
#define HB_STYLE_WEATHER_NOISE_INCLUDED

#define HB_PATCH_SCALE          3.1
#define HB_PATCH_DETAIL_SCALE   0.9
#define HB_NOISE_REACH          0.75
#define HB_NOISE_STRETCH        1.5

#ifndef HB_SNOW_FULL_SLOPE
    #define HB_SNOW_FULL_SLOPE  0.407h
    #define HB_SNOW_NO_SLOPE    0.174h
#endif
#define HB_SNOW_SLOPE_EDGE      0.1h
#define HB_SNOW_SLOPE_BRUSH     0.6h
#define HB_SNOW_SPREAD          0.6h
#define HB_SNOW_FADE_IN         3.0h
#define HB_SNOW_PATCH_DELAY     0.5h
#define HB_SNOW_DROP_DELAY      0.3h
#define HB_WET_SPREAD           0.5h
#define HB_WET_PATCH_DELAY      0.6h

float HB_WeatherHash(float3 cell)
{
    uint3 q = uint3(int3(cell));
    uint n = (q.x * 73856093u) ^ (q.y * 19349663u) ^ (q.z * 83492791u);
    n = (n ^ (n >> 13u)) * 1274126177u;

    return float((n ^ (n >> 16u)) & 0xFFFFFFu) * (1.0 / 16777215.0);
}

float HB_WeatherNoise(float3 x)
{
    float3 s = floor(x + dot(x, 1.0 / 3.0));
    float3 x0 = x - s + dot(s, 1.0 / 6.0);
    float3 e = step(x0.yzx, x0.xyz);
    float3 i1 = e * (1.0 - e.zxy);
    float3 i2 = 1.0 - e.zxy * (1.0 - e);
    float3 x1 = x0 - i1 + 1.0 / 6.0;
    float3 x2 = x0 - i2 + 1.0 / 3.0;
    float3 x3 = x0 - 0.5;

    float4 w = max(HB_NOISE_REACH - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    w *= w;
    w *= w;

    float4 h = float4(HB_WeatherHash(s), HB_WeatherHash(s + i1), HB_WeatherHash(s + i2), HB_WeatherHash(s + 1.0));

    return dot(w, h) / max(dot(w, 1.0), 1e-6);
}

half HB_WeatherPatches(float3 positionWS)
{
    return half(0.65 * HB_WeatherNoise(positionWS / (HB_PATCH_SCALE * HB_NOISE_STRETCH))
              + 0.35 * HB_WeatherNoise(positionWS / (HB_PATCH_DETAIL_SCALE * HB_NOISE_STRETCH) + 17.0));
}

#endif
