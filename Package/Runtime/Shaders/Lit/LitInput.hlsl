#ifndef HIDDENBULL_URPSTYLE_LIT_INPUT_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "../Library/StyleCommon.hlsl"

// Every pass of the shader includes this file, and this is the only place UnityPerMaterial is
// declared. The SRP Batcher silently drops a shader whose CBUFFER layout differs between passes,
// and "silently" is the problem — keeping the layout in one shared file makes that impossible.

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _RimColor;
    half _Cutoff;
    half _BumpScale;
    half _Metallic;
    half _Smoothness;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _ShadowTerminator;
    half _RimPower;
    half _RimIntensity;
    // _Cull and the keyword toggles are deliberately absent: they drive render state and keywords
    // only, are never read from HLSL, and URP's own Lit keeps them out of this CBUFFER too.
    half _Surface;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _RimColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseWrap)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _ShadowTerminator)
    UNITY_DOTS_INSTANCED_PROP(float , _RimPower)
    UNITY_DOTS_INSTANCED_PROP(float , _RimIntensity)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
#define _RimColor           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _RimColor)
#define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff)
#define _BumpScale          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale)
#define _Metallic           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic)
#define _Smoothness         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness)
#define _DiffuseWrap        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseWrap)
#define _DiffuseSoftness    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseSoftness)
#define _ShadowTerminator   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _ShadowTerminator)
#define _RimPower           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _RimPower)
#define _RimIntensity       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _RimIntensity)
#define _Surface            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Surface)
#endif

/// Builds URP's SurfaceData.
///
/// The albedo-only path (Architecture D11) is the fast one: with no keywords enabled this samples
/// nothing at all. Texture slots exist, but a material that does not use them pays no fetch, no
/// variant and no register pressure — the inverse of URP's usual arrangement, where the textured
/// path is the default and flat shading is the degenerate case.
void InitializeHiddenBullSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = _BaseColor;

#ifdef _HB_BASE_MAP
    albedoAlpha *= SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
#endif

#ifdef _HB_VERTEX_COLOR
    // In an albedo-only workflow this is how one material covers many variations without extra
    // draw calls or extra materials.
    albedoAlpha *= vertexColor;
#endif

    outSurfaceData.alpha = AlphaDiscard(albedoAlpha.a, _Cutoff);
    outSurfaceData.albedo = AlphaModulate(albedoAlpha.rgb, outSurfaceData.alpha);

    // Gated on _NORMALMAP inside SampleNormal, so this costs nothing when the keyword is off.
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    outSurfaceData.metallic = _Metallic;
    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.occlusion = 1.0h;
    outSurfaceData.emission = 0.0h;
}

/// Builds the style parameters that sit alongside SurfaceData.
HiddenBullStyleData InitializeHiddenBullStyleData()
{
    HiddenBullStyleData style;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.shadowTerminator = _ShadowTerminator;
    style.rimColor = _RimColor.rgb;
    style.rimPower = _RimPower;
    style.rimIntensity = _RimIntensity;
    return style;
}

#endif // HIDDENBULL_URPSTYLE_LIT_INPUT_INCLUDED
