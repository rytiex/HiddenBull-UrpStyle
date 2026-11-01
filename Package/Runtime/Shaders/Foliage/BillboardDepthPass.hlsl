#ifndef HB_BILLBOARD_DEPTH_PASS_INCLUDED
#define HB_BILLBOARD_DEPTH_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"

#ifndef HB_DEPTH_NORMALS
    #define HB_BILLBOARD_SHELTER
#endif

#ifdef HB_BILLBOARD_SHELTER
float _HB_ShelterPass;

#define HB_BILLBOARD_DISC 0.25
#endif

struct HBBillboardDepthAttributes
{
    float4 positionOS : POSITION;
    float2 corner     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBBillboardDepthVaryings
{
    float2 corner     : TEXCOORD0;
    float3 frame      : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

HBBillboardDepthVaryings HB_BillboardDepthVertex(HBBillboardDepthAttributes input)
{
    HBBillboardDepthVaryings output = (HBBillboardDepthVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 toward;
    float3 positionWS = HB_BillboardPosition(input.corner, toward, output.frame);

    output.corner = input.corner;

#ifdef HB_BILLBOARD_SHELTER
    if (_HB_ShelterPass > HB_PLANT_OWN_SHELTER)
    {
        float2 offset = (input.corner - 0.5) * 2.0 * _BillboardRadius * length(GetObjectToWorldMatrix()._m00_m10_m20);

        positionWS = HB_BillboardTop() + float3(offset.x, 0.0, offset.y);
    }

    output.positionCS = HB_PlantClipPosition(positionWS);

    if (_HB_ShelterPass > 0.5 && _HB_ShelterPass < HB_PLANT_OWN_SHELTER)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);
#else
    output.positionCS = HB_PlantClipPosition(positionWS);
#endif

    return output;
}

void HB_BillboardDepthClip(HBBillboardDepthVaryings input)
{
    HB_BillboardClip(input.corner, input.frame);
    LODFadeCrossFade(input.positionCS);
}

#ifdef HB_DEPTH_NORMALS

half4 HB_BillboardDepthNormalsFragment(HBBillboardDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_BillboardDepthClip(input);

    return half4(HB_BillboardNormal(input.corner, input.frame).xyz, 0.0h);
}

#else

half HB_BillboardDepthFragment(HBBillboardDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    if (_HB_ShelterPass > 0.5)
    {
        float2 offset = input.corner - 0.5;

        clip(HB_BILLBOARD_DISC - dot(offset, offset));
    }
    else
    {
        HB_BillboardDepthClip(input);
    }

    return input.positionCS.z;
}

#endif

#endif
