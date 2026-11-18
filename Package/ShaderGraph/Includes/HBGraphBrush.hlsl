#ifndef HB_GRAPH_BRUSH_INCLUDED
#define HB_GRAPH_BRUSH_INCLUDED

#ifndef HB_GRAPH_BRUSH
#define HB_GRAPH_BRUSH
#endif

#ifndef HB_GRAPH_SIMPLE
    #if defined(SHADERGRAPH_PREVIEW) || !defined(SHADERPASS) || SHADERPASS != SHADERPASS_UNLIT
        #define HB_GRAPH_SIMPLE
    #endif
#endif

#ifndef HB_GRAPH_SIMPLE
    #ifndef _HB_BRUSH_ANCHOR
        #define _HB_BRUSH_ANCHOR
    #endif

    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"
#endif

void HB_Brush_float(float3 PositionWS, float3 GeometryNormalWS, bool FrontFace, float2 UV, float3 AnchorOS,
                    float Space, float Size, float Paint, float EdgeKeep, float ToneStrokes, float BakedStrokes,
                    float Relief, float Shading, float Ambient, float Albedo, UnityTexture2D Mask, out float4x4 Out,
                    out float4x4 PaintOut)
{
    Out = (float4x4)0;
    PaintOut = (float4x4)0;
    Out[3] = float4(Shading, Ambient, Albedo, Relief);

#ifndef HB_GRAPH_SIMPLE
    half3 normalWS = half3(normalize(FrontFace ? GeometryNormalWS : -GeometryNormalWS));
    half scale = max(half(Size), half(HB_EPSILON));
    half mask = 1.0h;

    #ifdef _HB_BRUSH_MASK
    float2 maskUV = UV * Mask.scaleTranslate.xy + Mask.scaleTranslate.zw;
    mask = half(SAMPLE_TEXTURE2D(Mask.tex, Mask.samplerstate, maskUV).r);
    #endif

    HBBrushSample brush = HB_SampleBrush(PositionWS, normalWS, half(Space), AnchorOS, scale, mask);

    float3 strokeOffset = HB_BrushStrokeOffset(brush.spine, normalWS, scale * brush.growth);
    float2 strokeScreen = HB_BrushScreenOffset(strokeOffset, PositionWS, normalWS);

    Out[0] = float4(brush.spine, brush.coverage);
    Out[1] = float4(strokeOffset * ToneStrokes, 1.0);
    Out[2] = float4(strokeScreen * ToneStrokes, BakedStrokes, 0.0);
    PaintOut[0] = float4(strokeScreen * Paint, EdgeKeep, 1.0);
    PaintOut[1] = float4(strokeOffset * Paint, 0.0);
#endif
}

#endif
