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

#endif
