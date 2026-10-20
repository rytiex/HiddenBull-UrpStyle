#ifndef HIDDENBULL_URPSTYLE_LIT_SURFACE_VERTEX_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_SURFACE_VERTEX_INCLUDED

#include "LitSurface.hlsl"

#ifndef HB_SKY_PASS_FLAG
#define HB_SKY_PASS_FLAG
float _HB_SkyOcclusionPass;
#endif

#if defined(HB_OIT_REFRACTION)
    #define HB_VERTEX_UV
    #define HB_VERTEX_POSITION
    #ifdef _HB_TRIPLANAR
        #define HB_VERTEX_NORMAL
    #endif
#elif defined(HB_OIT_MOMENTS)
    #define HB_VERTEX_UV
    #define HB_VERTEX_POSITION
    #define HB_VERTEX_NORMAL
    #ifdef _NORMALMAP
        #define HB_VERTEX_TANGENT
    #endif
#elif defined(HB_SHADOW_TINT)
    #define HB_VERTEX_UV
    #ifdef _HB_TRIPLANAR
        #define HB_VERTEX_POSITION
        #define HB_VERTEX_NORMAL
    #endif
#elif defined(HB_DEPTH_NORMALS)
    #define HB_VERTEX_NORMAL
    #if defined(_ALPHATEST_ON) || defined(_NORMALMAP)
        #define HB_VERTEX_UV
        #ifdef _HB_TRIPLANAR
            #define HB_VERTEX_POSITION
        #endif
    #endif
    #if defined(_NORMALMAP) && !defined(_HB_TRIPLANAR)
        #define HB_VERTEX_TANGENT
    #endif
#else
    #ifdef _ALPHATEST_ON
        #define HB_VERTEX_UV
        #ifdef _HB_TRIPLANAR
            #define HB_VERTEX_POSITION
            #define HB_VERTEX_NORMAL
        #endif
    #endif
#endif

#if defined(HB_VERTEX_POSITION) && defined(HB_VERTEX_NORMAL) && (defined(_HB_BRUSH_ANCHOR) || defined(HB_ANCHOR_REST))
    #define HB_VERTEX_ANCHOR
#endif

#if !defined(HB_SHADOW_CASTER) && !defined(HB_SHADOW_TINT)
    #define HB_VERTEX_STEREO
#endif

#ifdef HB_SHADOW_CASTER
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;
#endif

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct HiddenBullSurfaceAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
#ifdef HB_VERTEX_TANGENT
    float4 tangentOS  : TANGENT;
#endif
#ifdef HB_VERTEX_UV
    float2 texcoord   : TEXCOORD0;
#endif
#if defined(HB_VERTEX_ANCHOR) && !defined(HB_ANCHOR_REST)
    float3 brushAnchorOS : TEXCOORD3;
#endif
#ifdef HB_VERTEX_COLOR
    half4 color       : COLOR;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HiddenBullSurfaceVaryings
{
#ifdef HB_VERTEX_UV
    float2 uv          : TEXCOORD0;
#endif
#ifdef HB_VERTEX_POSITION
    float3 positionWS  : TEXCOORD1;
#endif
#ifdef HB_VERTEX_NORMAL
    half3 normalWS     : TEXCOORD2;
#endif
#ifdef HB_VERTEX_TANGENT
    half3 tangentWS    : TEXCOORD3;
    half3 bitangentWS  : TEXCOORD4;
#endif
#ifdef HB_VERTEX_ANCHOR
    float3 brushAnchor : TEXCOORD5;
#endif
    float4 positionCS  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
#ifdef HB_VERTEX_STEREO
    UNITY_VERTEX_OUTPUT_STEREO
#endif
};

HiddenBullSurfaceVaryings HiddenBullSurfaceVertex(HiddenBullSurfaceAttributes input)
{
    HiddenBullSurfaceVaryings output = (HiddenBullSurfaceVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
#ifdef HB_VERTEX_STEREO
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#endif

    float3 restOS = input.positionOS.xyz;

#ifdef HB_HAS_VERTEX_HOOK
    #ifdef HB_VERTEX_COLOR
        HB_VertexHook(input.positionOS.xyz, input.color);
    #else
        HB_VertexHook(input.positionOS.xyz, half4(1.0h, 1.0h, 1.0h, 1.0h));
    #endif
#endif

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

#ifdef HB_SHADOW_CASTER
    float3 biasNormalWS = TransformObjectToWorldNormal(input.normalOS);

    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
        float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
        float3 lightDirectionWS = _LightDirection;
    #endif

    output.positionCS = ApplyShadowClamping(
        TransformWorldToHClip(ApplyShadowBias(positionWS, biasNormalWS, lightDirectionWS)));
#else
    output.positionCS = TransformWorldToHClip(positionWS);

    if (_HB_SkyOcclusionPass > 0.5 && _ReceiveWeather > 1.5)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);
#endif

#ifdef HB_VERTEX_UV
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif

#ifdef HB_VERTEX_POSITION
    output.positionWS = positionWS;
#endif

#ifdef HB_VERTEX_TANGENT
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.normalWS = normalInput.normalWS;
    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
#elif defined(HB_VERTEX_NORMAL)
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif

#if defined(HB_VERTEX_ANCHOR) && defined(HB_ANCHOR_REST)
    output.brushAnchor = restOS;
#elif defined(HB_VERTEX_ANCHOR)
    output.brushAnchor = input.brushAnchorOS;
#endif

    return output;
}

HiddenBullSurfaceInput HB_SurfaceFromVaryings(HiddenBullSurfaceVaryings input, bool frontFace)
{
    float2 uv = float2(0.0, 0.0);
    float3 positionWS = float3(0.0, 0.0, 0.0);
    half3 normalWS = half3(0.0h, 0.0h, 1.0h);
    half3 tangentWS = half3(1.0h, 0.0h, 0.0h);
    half3 bitangentWS = half3(0.0h, 1.0h, 0.0h);
    float3 anchorOS = float3(0.0, 0.0, 0.0);

#ifdef HB_VERTEX_UV
    uv = input.uv;
#endif
#ifdef HB_VERTEX_POSITION
    positionWS = input.positionWS;
#endif
#ifdef HB_VERTEX_NORMAL
    normalWS = input.normalWS;
#endif
#ifdef HB_VERTEX_TANGENT
    tangentWS = input.tangentWS;
    bitangentWS = input.bitangentWS;
#endif
#ifdef HB_VERTEX_ANCHOR
    anchorOS = input.brushAnchor;
#endif

    return HB_SurfaceInput(uv, positionWS, normalWS, tangentWS, bitangentWS, anchorOS, frontFace);
}

#endif
