#ifndef HB_LIT_META_PASS_INCLUDED
#define HB_LIT_META_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSurface.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

struct HBMetaVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV      : TEXCOORD1;
    float4 LightCoord : TEXCOORD2;
#endif
    float3 positionWS : TEXCOORD3;
    float3 normalWS   : TEXCOORD4;
};

HBMetaVaryings HB_MetaVertex(Attributes input)
{
    Varyings meta = UniversalVertexMeta(input);

    HBMetaVaryings output = (HBMetaVaryings)0;
    output.positionCS = meta.positionCS;
    output.uv = meta.uv;
#ifdef EDITOR_VISUALIZATION
    output.VizUV = meta.VizUV;
    output.LightCoord = meta.LightCoord;
#endif
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    return output;
}

half4 HB_MetaFragment(HBMetaVaryings input) : SV_Target
{
    HBSurfaceInput surface = HB_SurfaceInput(input.uv, input.positionWS, half3(input.normalWS), half3(1.0h, 0.0h, 0.0h),
                                             half3(0.0h, 1.0h, 0.0h), float3(0.0, 0.0, 0.0), true);

    SurfaceData surfaceData;
    half3 mappedNormalWS;

    HB_ResolveSurface(surface, float2(0.0, 0.0), ddx(input.uv), ddy(input.uv), float3(0.0, 0.0, 0.0), surfaceData,
                      mappedNormalWS);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness,
                       surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.Emission = surfaceData.emission;

    Varyings meta = (Varyings)0;
    meta.positionCS = input.positionCS;
    meta.uv = input.uv;
#ifdef EDITOR_VISUALIZATION
    meta.VizUV = input.VizUV;
    meta.LightCoord = input.LightCoord;
#endif

    return UniversalFragmentMeta(meta, metaInput);
}

#endif
