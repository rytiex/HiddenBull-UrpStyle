#ifndef HB_GRAPH_EMISSION_INCLUDED
#define HB_GRAPH_EMISSION_INCLUDED

#ifndef HB_GRAPH_EMISSION
#define HB_GRAPH_EMISSION
#endif

void HB_Emission_float(float3 Color, UnityTexture2D Map, float2 UV, out float4x4 Out)
{
    float2 uv = UV * Map.scaleTranslate.xy + Map.scaleTranslate.zw;
    float3 emission = SAMPLE_TEXTURE2D(Map.tex, Map.samplerstate, uv).rgb * Color;

    Out = (float4x4)0;
    Out[0] = float4(emission, 0.0);
}

#endif
