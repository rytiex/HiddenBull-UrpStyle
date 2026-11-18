#ifndef HB_GRAPH_WEATHER_INCLUDED
#define HB_GRAPH_WEATHER_INCLUDED

#ifndef _HB_WEATHER
#define _HB_WEATHER
#endif

#ifndef HB_WEATHER
#define HB_WEATHER
#endif

void HB_WeatherStatic_float(float Wetness, float SnowCover, out float4x4 Out)
{
    Out = (float4x4)0;
    Out[0] = float4(Wetness, SnowCover, 1.0, 0.0);
}

#endif
