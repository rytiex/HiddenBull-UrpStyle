#ifndef HB_SKY_PASS_INCLUDED
#define HB_SKY_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleClouds.hlsl"

struct HBSkyAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBSkyVaryings
{
    float4 positionCS : SV_POSITION;
    float3 direction  : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

HBSkyVaryings HB_SkyVertex(HBSkyAttributes input)
{
    HBSkyVaryings output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.direction = input.positionOS.xyz;

    return output;
}

half4 HB_SkyFragment(HBSkyVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half3 direction = half3(SafeNormalize(input.direction));

    return half4(HB_SkyWithClouds(direction), 1.0h);
}

#endif
