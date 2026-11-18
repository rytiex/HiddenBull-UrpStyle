#ifndef HB_GRAPH_SPECULAR_INCLUDED
#define HB_GRAPH_SPECULAR_INCLUDED

#ifndef _HB_SPECULAR
#define _HB_SPECULAR
#endif

void HB_Specular_float(float Metallic, float Smoothness, out float4x4 Out)
{
    Out = (float4x4)0;
    Out[0] = float4(Metallic, Smoothness, 0.0, 0.0);
}

#endif
