#ifndef HB_LIT_INPUT_INCLUDED
#define HB_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleTones.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BrushMask_ST;
    half4 _BaseColor;
    half4 _RimColor;
    half4 _EmissionColor;
    half4 _SubsurfaceColor;
    half4 _SheenColor;
    half4 _TransmissionColor;
    half _Cutoff;
    half _BumpScale;
    half _Metallic;
    half _Smoothness;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _BakedTones;
    half _BakedSoftness;
    half _RimPower;
    half _RimIntensity;
    half _BrushObjectSpace;
    half _TriplanarSpace;
    half _BrushScale;
    half _BrushPaint;
    half _BrushEdgeKeep;
    half _ToneStrokes;
    half _BakedStrokes;
    half _BrushShading;
    half _BrushAmbient;
    half _BrushAlbedo;
    half _BrushRelief;
    half _SubsurfaceStrength;
    half _SubsurfaceWidth;
    half _Transmission;
    half _TransmissionBend;
    half _TransmissionFocus;
    half _SheenStrength;
    half _SheenPower;
    half _Weave;
    half _WeaveScale;
    half _ReceiveWeather;
    half _Wetness;
    half _SnowCover;
    half _DropDensity;
    half _DropSize;
    half _DropLife;
    half _FlakeDensity;
    half _FlakeSize;
    half _FlakeLife;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
#define _EmissionColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor)
#define _Cutoff        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff)
#define _Metallic      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic)
#define _Smoothness    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness)
#endif

TEXTURE2D(_BrushMask);
SAMPLER(sampler_BrushMask);
TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);

#define HB_PAINT_EDGE_START 0.15h
#define HB_PAINT_EDGE_END   0.35h
#define HB_TRIPLANAR_CUTOFF 0.02h

half HB_SampleBrushMask(float2 uv)
{
#ifdef _HB_BRUSH_MASK
    return SAMPLE_TEXTURE2D(_BrushMask, sampler_BrushMask, TRANSFORM_TEX(uv, _BrushMask)).r;
#else
    return 1.0h;
#endif
}

half HB_PaintKeep(half4 painted, half4 original)
{
    half difference = length(sqrt(max(painted.rgb, 0.0h)) - sqrt(max(original.rgb, 0.0h)));

    return lerp(1.0h, 1.0h - smoothstep(HB_PAINT_EDGE_START, HB_PAINT_EDGE_END, difference), half(_BrushEdgeKeep));
}

void HB_ResolveSurfaceColour(half4 albedoAlpha, inout SurfaceData surfaceData)
{
    surfaceData.alpha = AlphaDiscard(albedoAlpha.a, _Cutoff);
    surfaceData.albedo = albedoAlpha.rgb;
}

void HB_ResolveSurfaceMask(half4 mask, inout SurfaceData surfaceData)
{
#ifdef _HB_MASK_MAP
    surfaceData.metallic = mask.r;
    surfaceData.smoothness = mask.a * _Smoothness;
    surfaceData.occlusion = mask.g;
#else
    surfaceData.metallic = _Metallic;
    surfaceData.smoothness = _Smoothness;
    surfaceData.occlusion = 1.0h;
#endif
}

void HB_InitializeSurfaceData(float2 uv, float2 shift, float2 uvDdx, float2 uvDdy, out SurfaceData surfaceData)
{
    surfaceData = (SurfaceData)0;

    half4 albedoAlpha = _BaseColor;
    float2 detailUV = uv + shift;

#ifdef _HB_BASE_MAP
    half4 painted = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, detailUV, uvDdx, uvDdy);

    #ifdef _HB_BRUSH
    half4 original = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uv, uvDdx, uvDdy);
    half keep = HB_PaintKeep(painted, original);

    painted = half4(lerp(original.rgb, painted.rgb, keep), original.a);
    detailUV = uv + shift * keep;
    #endif

    albedoAlpha *= painted;
#endif

    HB_ResolveSurfaceColour(albedoAlpha, surfaceData);

#ifdef _NORMALMAP
    surfaceData.normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, detailUV, uvDdx, uvDdy),
                                             _BumpScale);
#else
    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
#endif

#ifdef _HB_MASK_MAP
    HB_ResolveSurfaceMask(SAMPLE_TEXTURE2D_GRAD(_MaskMap, sampler_MaskMap, detailUV, uvDdx, uvDdy), surfaceData);
#else
    HB_ResolveSurfaceMask(half4(0.0h, 1.0h, 0.0h, 1.0h), surfaceData);
#endif

#ifdef _HB_EMISSION
    surfaceData.emission = SAMPLE_TEXTURE2D_GRAD(_EmissionMap, sampler_EmissionMap, detailUV, uvDdx, uvDdy).rgb
                         * half3(_EmissionColor.rgb);
#endif
}

#ifdef _HB_TRIPLANAR

struct HBTriplanar
{
    float3 position;
    float3 positionDx;
    float3 positionDy;
    half3 blend;
    half3 normal;
    half localSpace;
};

HBTriplanar HB_SetupTriplanar(float3 positionWS, half3 normalWS, half objectSpace, float3 anchorOS)
{
    HBBrushSpace space = HB_ResolveBrushSpace(positionWS, normalWS, objectSpace, anchorOS);

    HBTriplanar triplanar;
    triplanar.position = space.position * _BaseMap_ST.x;
    triplanar.positionDx = ddx(triplanar.position);
    triplanar.positionDy = ddy(triplanar.position);
    triplanar.blend = HB_TriplanarBlend(space.normal);
    triplanar.normal = space.normal;
    triplanar.localSpace = space.localSpace;
    return triplanar;
}

float3 HB_TriplanarShift(float3 offsetWS, half localSpace)
{
    float3 local = mul((float3x3)GetWorldToObjectMatrix(), offsetWS) * HB_ObjectScale();

    return lerp(offsetWS, local, localSpace) * _BaseMap_ST.x;
}

half4 HB_SampleTriplanar(TEXTURE2D_PARAM(textureName, samplerName), HBTriplanar triplanar, float3 shift)
{
    float3 p = triplanar.position + shift;
    float2 offset = _BaseMap_ST.zw;
    half4 sum = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (triplanar.blend.x > HB_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, p.zy + offset,
                                     triplanar.positionDx.zy, triplanar.positionDy.zy) * triplanar.blend.x;
    }

    UNITY_BRANCH
    if (triplanar.blend.y > HB_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, p.xz + offset,
                                     triplanar.positionDx.xz, triplanar.positionDy.xz) * triplanar.blend.y;
    }

    UNITY_BRANCH
    if (triplanar.blend.z > HB_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, p.xy + offset,
                                     triplanar.positionDx.xy, triplanar.positionDy.xy) * triplanar.blend.z;
    }

    return sum;
}

half3 HB_SampleTriplanarNormal(HBTriplanar triplanar, float3 shift)
{
    float3 p = triplanar.position + shift;
    float2 offset = _BaseMap_ST.zw;
    half3 normal = triplanar.normal;
    half3 sum = half3(0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (triplanar.blend.x > HB_TRIPLANAR_CUTOFF)
    {
        half3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, p.zy + offset,
                                          triplanar.positionDx.zy, triplanar.positionDy.zy), _BumpScale);
        tangent = half3(tangent.xy + normal.zy, abs(tangent.z) * normal.x);
        sum += tangent.zyx * triplanar.blend.x;
    }

    UNITY_BRANCH
    if (triplanar.blend.y > HB_TRIPLANAR_CUTOFF)
    {
        half3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, p.xz + offset,
                                          triplanar.positionDx.xz, triplanar.positionDy.xz), _BumpScale);
        tangent = half3(tangent.xy + normal.xz, abs(tangent.z) * normal.y);
        sum += tangent.xzy * triplanar.blend.y;
    }

    UNITY_BRANCH
    if (triplanar.blend.z > HB_TRIPLANAR_CUTOFF)
    {
        half3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, p.xy + offset,
                                          triplanar.positionDx.xy, triplanar.positionDy.xy), _BumpScale);
        tangent = half3(tangent.xy + normal.xy, abs(tangent.z) * normal.z);
        sum += tangent * triplanar.blend.z;
    }

    half3 mapped = SafeNormalize(sum);

    return triplanar.localSpace > 0.5h ? half3(TransformObjectToWorldNormal(float3(mapped))) : mapped;
}

void HB_InitializeTriplanarSurfaceData(HBTriplanar triplanar, float3 shift, out SurfaceData surfaceData,
                                       out half3 mappedNormalWS)
{
    surfaceData = (SurfaceData)0;

    half4 albedoAlpha = _BaseColor;
    float3 detail = shift;

#ifdef _HB_BASE_MAP
    half4 painted = HB_SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), triplanar, shift);

    #ifdef _HB_BRUSH
    half4 original = HB_SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), triplanar, float3(0.0, 0.0, 0.0));
    half keep = HB_PaintKeep(painted, original);

    painted = half4(lerp(original.rgb, painted.rgb, keep), original.a);
    detail = shift * keep;
    #endif

    albedoAlpha *= painted;
#endif

    HB_ResolveSurfaceColour(albedoAlpha, surfaceData);

    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);

#ifdef _NORMALMAP
    mappedNormalWS = HB_SampleTriplanarNormal(triplanar, detail);
#else
    mappedNormalWS = half3(0.0h, 0.0h, 0.0h);
#endif

#ifdef _HB_MASK_MAP
    HB_ResolveSurfaceMask(HB_SampleTriplanar(TEXTURE2D_ARGS(_MaskMap, sampler_MaskMap), triplanar, detail),
                          surfaceData);
#else
    HB_ResolveSurfaceMask(half4(0.0h, 1.0h, 0.0h, 1.0h), surfaceData);
#endif

#ifdef _HB_EMISSION
    surfaceData.emission = HB_SampleTriplanar(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), triplanar,
                                              detail).rgb * half3(_EmissionColor.rgb);
#endif
}

#endif

HBStyleData HB_InitializeStyleData()
{
    HBStyleData style;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedTones = _BakedTones;
    style.bakedSoftness = _BakedSoftness;
    style.rimColor = _RimColor.rgb;
    style.rimPower = _RimPower;
    style.rimIntensity = _RimIntensity;
    style.brushShading = _BrushShading;
    style.brushAmbient = _BrushAmbient;
    style.brushAlbedo = _BrushAlbedo;
    style.brushRelief = _BrushRelief;
    return style;
}

#endif
