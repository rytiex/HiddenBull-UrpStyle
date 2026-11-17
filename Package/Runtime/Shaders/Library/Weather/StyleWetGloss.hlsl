#ifndef HB_STYLE_WET_GLOSS_INCLUDED
#define HB_STYLE_WET_GLOSS_INCLUDED

#define HB_WET_FLAT_SMOOTHNESS  0.92h
#define HB_WET_WALL_SMOOTHNESS  0.6h
#define HB_FLOW_SMOOTHNESS      0.95h
#define HB_SNOW_SMOOTHNESS      0.2h

half HB_WeatherSmoothness(half smoothness, half flat, half wet, half flow, half snow, half shine)
{
    half gloss = max(smoothness, lerp(HB_WET_WALL_SMOOTHNESS, HB_WET_FLAT_SMOOTHNESS, flat));

    smoothness = lerp(smoothness, gloss, wet * shine);
    smoothness = lerp(smoothness, HB_FLOW_SMOOTHNESS, flow * shine);
    return lerp(smoothness, HB_SNOW_SMOOTHNESS, snow);
}

half HB_DecalSmoothnessOverWeather(half weathered, half4 maos, half flat, half wet, half flow, half snow, half shine)
{
    half cover = 1.0h - maos.w;
    half decal = max(maos.z, lerp(HB_WET_WALL_SMOOTHNESS, HB_WET_FLAT_SMOOTHNESS, flat) * cover);

    decal = lerp(maos.z, decal, wet * shine);
    decal = lerp(decal, HB_FLOW_SMOOTHNESS * cover, flow * shine);
    decal = lerp(decal, HB_SNOW_SMOOTHNESS * cover, snow);
    return weathered * maos.w + decal;
}

#endif
