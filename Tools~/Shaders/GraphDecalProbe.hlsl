#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphDecal.hlsl"

TEXTURE2D(_GraphBase);
SAMPLER(sampler_GraphBase);
TEXTURE2D(_GraphNormal);
SAMPLER(sampler_GraphNormal);

float4 _GraphBase_ST;
float4 _GraphColor;
float _GraphNormalScale;
float _GraphNormalBlend;
float _GraphAffectNormal;
float _GraphAffectSurface;
float _GraphMetallic;
float _GraphSmoothness;
float _GraphWear;
float _GraphSize;
float _GraphStrokeAlpha;

struct GraphDecalInput
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float2 uv : TEXCOORD1;
};

void GraphDecalFragment(GraphDecalInput input, out float4 target0 : SV_Target0, out float4 target1 : SV_Target1,
                        out float4 target2 : SV_Target2)
{
    float3 color;
    float alpha;
    float3 normalTS;
    float normalAlpha;
    float metallic;
    float smoothness;
    float surfaceAlpha;

    HB_Decal_float(UnityBuildTexture2DStructNoTexelSize(_GraphBase), _GraphColor,
                   UnityBuildTexture2DStructNoScaleNoTexelSize(_GraphNormal), _GraphNormalScale, _GraphNormalBlend,
                   _GraphAffectNormal > 0.5, _GraphAffectSurface > 0.5, _GraphMetallic, _GraphSmoothness, _GraphWear,
                   _GraphSize, _GraphStrokeAlpha, input.positionWS, TransformWorldToObject(input.positionWS), input.uv,
                   color, alpha, normalTS, normalAlpha, metallic, smoothness, surfaceAlpha);
    target0 = float4(color, alpha);
    target1 = float4(normalTS, normalAlpha);
    target2 = float4(metallic, smoothness, 1.0, surfaceAlpha);
}
