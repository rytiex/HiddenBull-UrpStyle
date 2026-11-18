#ifndef HB_GRAPH_RIM_INCLUDED
#define HB_GRAPH_RIM_INCLUDED

#ifndef _HB_RIM
#define _HB_RIM
#endif

void HB_Rim_float(float3 Color, float Power, float Intensity, out float4x4 Out)
{
    Out = (float4x4)0;
    Out[0] = float4(Color, Power);
    Out[1] = float4(Intensity, 0.0, 0.0, 0.0);
}

#endif
