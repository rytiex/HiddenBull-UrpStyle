#ifndef HB_SHELTER_UNDERSIDE_PASS_INCLUDED
#define HB_SHELTER_UNDERSIDE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define HB_SHELTER_MOVING   1.5
#define HB_FACING_DOWN      -0.1h

float _ReceiveWeather;

struct HBUndersideAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
};

struct HBUndersideVaryings
{
    float4 positionCS : SV_POSITION;
    half facing       : TEXCOORD0;
};

HBUndersideVaryings HB_UndersideVertex(HBUndersideAttributes input)
{
    HBUndersideVaryings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.facing = half(TransformObjectToWorldNormal(input.normalOS).y);

    if (_ReceiveWeather > HB_SHELTER_MOVING)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);

    return output;
}

half4 HB_UndersideFragment(HBUndersideVaryings input) : SV_Target
{
    clip(HB_FACING_DOWN - input.facing);

    return 0.0h;
}

#endif
