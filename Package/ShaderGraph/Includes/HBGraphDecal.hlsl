#ifndef HB_GRAPH_DECAL_INCLUDED
#define HB_GRAPH_DECAL_INCLUDED

#if !defined(SHADERGRAPH_PREVIEW) && !defined(SHADERGRAPH_PREVIEW_MAIN)
    #define HB_GRAPH_DECAL_BRUSH
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"
#endif

#define HB_GRAPH_DECAL_WEAR_BAND 0.2
#define HB_GRAPH_DECAL_KEEP_FROM 0.35
#define HB_GRAPH_DECAL_KEEP_TO 0.65

void HB_Decal_float(UnityTexture2D BaseMap, float4 BaseColor, UnityTexture2D NormalMap, float NormalScale,
                    float NormalBlend, bool AffectNormal, bool AffectSurface, float Metallic, float Smoothness,
                    float Wear, float Size, float StrokeAlpha, float3 PositionWS, float3 PositionOS, float2 UV,
                    out float3 Color, out float Alpha, out float3 NormalTS, out float NormalAlpha,
                    out float MetallicOut, out float SmoothnessOut, out float SurfaceAlpha)
{
    float2 uv = UV * BaseMap.scaleTranslate.xy + BaseMap.scaleTranslate.zw;
    float4 base = SAMPLE_TEXTURE2D(BaseMap.tex, BaseMap.samplerstate, uv) * BaseColor;
    float2 box = PositionOS.xz + 0.5;
    float edge = min(min(box.x, 1.0 - box.x), min(box.y, 1.0 - box.y));
    float coverage = 0.0;

#ifdef HB_GRAPH_DECAL_BRUSH
    float3 normalWS = normalize(cross(ddy(PositionWS), ddx(PositionWS)));

    normalWS = dot(normalWS, GetWorldSpaceNormalizeViewDir(PositionWS)) < 0.0 ? -normalWS : normalWS;
    coverage = HB_SampleBrush(PositionWS, half3(normalWS), 0.0h, float3(0.0, 0.0, 0.0),
                              max(half(Size), half(HB_EPSILON)), 1.0h).coverage;
#endif

    float keep = saturate(edge / max(Wear * HB_GRAPH_DECAL_WEAR_BAND, 1e-4) + coverage * Wear);
    float alpha = saturate(base.a) * smoothstep(HB_GRAPH_DECAL_KEEP_FROM, HB_GRAPH_DECAL_KEEP_TO, keep);

    Alpha = saturate(alpha + coverage * StrokeAlpha * alpha * (1.0 - alpha) * 4.0);
    Color = base.rgb;
    NormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(NormalMap.tex, NormalMap.samplerstate, uv), NormalScale);
    NormalAlpha = AffectNormal ? Alpha * NormalBlend : 0.0;
    MetallicOut = Metallic;
    SmoothnessOut = Smoothness;
    SurfaceAlpha = AffectSurface ? Alpha : 0.0;
}

#endif
