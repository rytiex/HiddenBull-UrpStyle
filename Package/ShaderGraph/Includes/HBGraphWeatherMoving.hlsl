#ifndef HB_GRAPH_WEATHER_MOVING_INCLUDED
#define HB_GRAPH_WEATHER_MOVING_INCLUDED

#ifndef _HB_WEATHER_MOVING
#define _HB_WEATHER_MOVING
#endif

#ifndef HB_WEATHER
#define HB_WEATHER
#endif

#ifndef _HB_BRUSH_ANCHOR
#define _HB_BRUSH_ANCHOR
#endif

#if defined(SHADERPASS) && SHADERPASS == SHADERPASS_DEPTHONLY && !defined(SHADERGRAPH_PREVIEW)
#define HB_GRAPH_SHELTER_CASTER

float _HB_ShelterPass;
#endif

void HB_WeatherMoving_float(float Wetness, float DropDensity, float DropSize, float DropLife, float FlakeDensity,
                            float FlakeSize, float FlakeLife, float3 Offset, float Receive, float3 AnchorOS,
                            float3 PositionOS, out float4x4 Out, out float3 PositionOut)
{
    Out = (float4x4)0;
    Out[0] = float4(Wetness, 0.0, 1.0, 0.0);
    Out[1] = float4(DropDensity, DropSize, FlakeDensity, FlakeSize);
    Out[2] = float4(DropLife, FlakeLife, 0.0, 0.0);
    Out[3] = float4(AnchorOS, 0.0);

    PositionOut = PositionOS + Offset;

#ifdef HB_GRAPH_SHELTER_CASTER
    if (_HB_ShelterPass > 0.5)
        PositionOut = float3(0.0, 0.0, 0.0);
#endif
}

#endif
