#ifndef HB_LIQUID_CUT_PASS_INCLUDED
#define HB_LIQUID_CUT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_HB_LiquidCutIds);

float4 _HB_CutWrite;
float _HB_CutTarget;

#define HB_LIQUID_CUT_EVERY 3

struct HBLiquidCutAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct HBLiquidCutVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
};

HBLiquidCutVaryings HB_LiquidCutVertex(HBLiquidCutAttributes input)
{
    HBLiquidCutVaryings output;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

float4 HB_LiquidCutMaskFragment(HBLiquidCutVaryings input) : SV_Target
{
    uint targets = uint(round(_HB_CutTarget));

    UNITY_BRANCH
    if ((targets & HB_LIQUID_CUT_EVERY) != HB_LIQUID_CUT_EVERY)
    {
        uint kind = uint(round(LOAD_TEXTURE2D(_HB_LiquidCutIds, uint2(input.positionCS.xy)).r));

        if ((targets & kind) == 0)
            discard;
    }

    float3 across = cross(ddx(input.positionWS), ddy(input.positionWS));
    float3 outward = dot(across, input.normalWS) < 0.0 ? -across : across;
    float3 ray = IsPerspectiveProjection() ? input.positionWS - GetCameraPositionWS() : GetViewForwardDir();

    return _HB_CutWrite * (dot(outward, ray) > 0.0 ? 1.0 : -1.0);
}

#endif
