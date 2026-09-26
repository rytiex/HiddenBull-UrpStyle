#ifndef HIDDENBULL_URPSTYLE_LIT_TRANSPARENCY_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_TRANSPARENCY_PASS_INCLUDED

#ifdef HB_OIT_MOMENTS

#include "LitInput.hlsl"
#include "../Library/StyleTransparency.hlsl"

struct MomentAttributes
{
    float4 positionOS : POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct MomentVaryings
{
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

MomentVaryings HiddenBullOITMomentsVertex(MomentAttributes input)
{
    MomentVaryings output = (MomentVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    return output;
}

void HiddenBullOITMomentsFragment(MomentVaryings input, out float outB0 : SV_Target0,
                                  out float4 outMoments : SV_Target1)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float absorbance = HB_OITAbsorbance(HB_SurfaceAlpha(input.uv));

    HB_OITMoments(absorbance, HB_OITDepth(input.positionWS), outB0, outMoments);
}

#else

#include "LitForwardPass.hlsl"
#include "../Library/StyleTransparency.hlsl"

half4 HiddenBullOITColorFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    InputData inputData;
    half sunVisibility;

    half4 color = HB_LitColour(input, IS_FRONT_VFACE(frontFace, true, false), inputData, sunVisibility);

    color.rgb = HB_ApplyFog(color.rgb, inputData.positionWS, inputData.normalWS, sunVisibility,
                            inputData.normalizedScreenSpaceUV);

    half transmittance = half(HB_OITTransmittanceAt(input.positionCS.xy,
                                                    HB_OITDepth(inputData.positionWS)));

    half weight = transmittance * color.a;

#ifdef _HB_PREMULTIPLY
    return half4(color.rgb * transmittance, weight);
#else
    return half4(color.rgb * weight, weight);
#endif
}

#endif

#endif
