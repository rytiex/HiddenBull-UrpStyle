#ifndef HB_GRAPH_SUBSURFACE_INCLUDED
#define HB_GRAPH_SUBSURFACE_INCLUDED

#ifndef _HB_SUBSURFACE
#define _HB_SUBSURFACE
#endif

#if !defined(HB_GRAPH_PREVIEW) && (defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN))
#define HB_GRAPH_PREVIEW
#endif

void HB_Subsurface_float(float3 Color, float Width, float Strength, float3 SheenColor, float SheenStrength,
                         float SheenWidth, float Weave, float WeaveScale, float Transmission,
                         float3 TransmissionColor, float Distortion, float Concentration,
                         UnityTexture2D TranslucencyMap, float2 UV, out float4x4 Out)
{
    Out = (float4x4)0;

#ifdef _HB_CLOTH
    Out[0] = float4(SheenColor, SheenStrength);
    Out[1] = float4(SheenWidth, Weave, WeaveScale, Transmission);
    Out[2] = float4(TransmissionColor, Distortion);
    Out[3] = float4(Concentration, 0.0, 0.0, 0.0);
#else
    float translucency = 1.0;

    #if defined(_HB_THICKNESS_MAP) || defined(HB_GRAPH_PREVIEW)
    float2 uv = UV * TranslucencyMap.scaleTranslate.xy + TranslucencyMap.scaleTranslate.zw;
    translucency = SAMPLE_TEXTURE2D(TranslucencyMap.tex, TranslucencyMap.samplerstate, uv).r;
    #endif

    Out[0] = float4(Color, Width);
    Out[1] = float4(Strength, Transmission, Distortion, Concentration);
    Out[2] = float4(TransmissionColor, translucency);
#endif
}

#endif
