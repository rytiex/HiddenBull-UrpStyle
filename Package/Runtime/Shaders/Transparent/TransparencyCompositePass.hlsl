#ifndef HB_TRANSPARENCY_COMPOSITE_PASS_INCLUDED
#define HB_TRANSPARENCY_COMPOSITE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleGlobals.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparency.hlsl"

TEXTURE2D(_HB_OITAccumulated);
TEXTURE2D(_HB_OITTint);
TEXTURE2D(_HB_OITLight);
TEXTURE2D(_HB_OITRefraction);
TEXTURE2D(_HB_OITBackground);
TEXTURE2D_FLOAT(_HB_OITBackgroundDepth);

#define HB_DEBUG_TRANSPARENCY_OPACITY 21
#define HB_DEBUG_TRANSPARENCY_TINT    22
#define HB_DEBUG_TRANSPARENCY_LIGHT   23
#define HB_DEBUG_TRANSPARENCY_BEND    24

#define HB_REFRACTION_BLUR_RADIUS     0.05
#define HB_REFRACTION_BLUR_TEXEL      0.5
#define HB_REFRACTION_BLUR_SOFTNESS   0.25
#define HB_REFRACTION_DISPERSION      0.35
#define HB_REFRACTION_TAPS            6
#define HB_REFRACTION_DEBUG_SCALE     10.0
#define HB_UPSAMPLE_EPSILON           0.01

#define HB_UPSAMPLED(textureName, upsample) \
    (LOAD_TEXTURE2D(textureName, upsample.texel0) * upsample.weight.x \
   + LOAD_TEXTURE2D(textureName, upsample.texel1) * upsample.weight.y \
   + LOAD_TEXTURE2D(textureName, upsample.texel2) * upsample.weight.z \
   + LOAD_TEXTURE2D(textureName, upsample.texel3) * upsample.weight.w)

struct HBUpsample
{
    int2 texel0;
    int2 texel1;
    int2 texel2;
    int2 texel3;
    float4 weight;
};

float4 HB_CompositeVertex(uint vertexID : SV_VertexID) : SV_POSITION
{
    return GetFullScreenTriangleVertexPosition(vertexID);
}

float HB_HalfEyeDepth(int2 texel)
{
    return LinearEyeDepth(LOAD_TEXTURE2D(_HB_OITDepth, texel).r, _ZBufferParams);
}

HBUpsample HB_Upsample(uint2 pixel)
{
    float2 position = float2(pixel) * 0.5 - 0.25;
    int2 base = int2(floor(position));
    float2 blend = position - float2(base);
    int2 limit = int2(HB_OIT_HALF_SIZE) - 1;

    HBUpsample upsample;
    upsample.texel0 = clamp(base, int2(0, 0), limit);
    upsample.texel1 = clamp(base + int2(1, 0), int2(0, 0), limit);
    upsample.texel2 = clamp(base + int2(0, 1), int2(0, 0), limit);
    upsample.texel3 = clamp(base + int2(1, 1), int2(0, 0), limit);

    float eye = LinearEyeDepth(LoadSceneDepth(pixel), _ZBufferParams);
    float4 eyes = float4(HB_HalfEyeDepth(upsample.texel0), HB_HalfEyeDepth(upsample.texel1),
                         HB_HalfEyeDepth(upsample.texel2), HB_HalfEyeDepth(upsample.texel3));
    float4 bilinear = float4((1.0 - blend.x) * (1.0 - blend.y), blend.x * (1.0 - blend.y),
                             (1.0 - blend.x) * blend.y, blend.x * blend.y);
    float4 weight = bilinear / (HB_UPSAMPLE_EPSILON + abs(eyes - eye) / eye);

    upsample.weight = weight / dot(weight, float4(1.0, 1.0, 1.0, 1.0));
    return upsample;
}

bool HB_OITEmpty(uint2 pixel)
{
    float2 corner = floor(float2(pixel) * 0.5 - 0.25) + 1.0;
    float4 clear = GATHER_ALPHA_TEXTURE2D(_HB_OITTint, sampler_PointClamp, corner / HB_OIT_HALF_SIZE);

    return all(clear > 0.5);
}

float3 HB_AddedColour(HBUpsample upsample, float absorbance)
{
    float4 accumulated = HB_UPSAMPLED(_HB_OITAccumulated, upsample);
    float3 light = HB_UPSAMPLED(_HB_OITLight, upsample).rgb;

    float3 surface = accumulated.rgb;

    if (absorbance >= HB_OIT_EMPTY && accumulated.a >= HB_OIT_EMPTY)
        surface *= (1.0 - exp(-absorbance)) / accumulated.a;

    return surface + light;
}

bool HB_TransparencyDebug()
{
    return HB_DEBUG_VIEW >= HB_DEBUG_TRANSPARENCY_OPACITY && HB_DEBUG_VIEW <= HB_DEBUG_TRANSPARENCY_BEND;
}

half3 HB_TransparencyDebugColour(HBUpsample upsample)
{
    if (HB_DEBUG_VIEW == HB_DEBUG_TRANSPARENCY_TINT)
        return half3(HB_UPSAMPLED(_HB_OITTint, upsample).rgb);

    if (HB_DEBUG_VIEW == HB_DEBUG_TRANSPARENCY_LIGHT)
        return half3(HB_UPSAMPLED(_HB_OITLight, upsample).rgb);

    if (HB_DEBUG_VIEW == HB_DEBUG_TRANSPARENCY_BEND)
    {
        if (!HB_OIT_REFRACTIVE)
            return half3(0.0h, 0.0h, 0.0h);

        float4 refraction = HB_UPSAMPLED(_HB_OITRefraction, upsample);

        return half3(saturate(abs(refraction.xy) * HB_REFRACTION_DEBUG_SCALE), saturate(refraction.z));
    }

    half opacity = half(1.0 - exp(-HB_UPSAMPLED(_HB_OITB0, upsample).r));

    return half3(opacity, opacity, opacity);
}

half4 HB_TransmitFragment(float4 positionCS : SV_POSITION) : SV_Target
{
    if (HB_TransparencyDebug())
        return half4(0.0h, 0.0h, 0.0h, 1.0h);

    uint2 pixel = uint2(positionCS.xy);

    if (HB_OITEmpty(pixel))
        discard;

    HBUpsample upsample = HB_Upsample(pixel);
    float absorbance = HB_UPSAMPLED(_HB_OITB0, upsample).r;
    half3 tint = half3(HB_UPSAMPLED(_HB_OITTint, upsample).rgb);

    return half4(tint * half(exp(-absorbance)), 1.0h);
}

float3 HB_Background(float2 uv, float lod)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_OITBackground, sampler_LinearClamp, saturate(uv), lod).rgb;
}

float HB_BackgroundSpread(float2 uv, float glass)
{
    return 1.0 - glass * SAMPLE_TEXTURE2D_LOD(_HB_OITBackgroundDepth, sampler_PointClamp, saturate(uv), 0).r;
}

float3 HB_Refracted(float2 target, float2 offset, float blur, float dispersion, float spread, float glass,
                    uint2 pixel)
{
    float2 chroma = offset * (dispersion * HB_REFRACTION_DISPERSION);
    bool split = dispersion > 0.0;
    float radius = blur * spread * HB_REFRACTION_BLUR_RADIUS;
    float pixels = radius * _ScreenSize.y;
    float3 result;

    UNITY_BRANCH
    if (pixels < 1.0)
    {
        result = split ? float3(HB_Background(target + chroma, 0.0).r, HB_Background(target, 0.0).g,
                                HB_Background(target - chroma, 0.0).b)
                       : HB_Background(target, 0.0);
    }
    else
    {
        float lod = log2(max(pixels * HB_REFRACTION_BLUR_TEXEL, 1.0));
        float2 scale = radius * float2(_ScreenSize.y * _ScreenSize.z, 1.0);
        float rotation = InterleavedGradientNoise(float2(pixel), 0) * TWO_PI;
        float3 sum = HB_Background(target, lod);
        float3 weight = float3(1.0, 1.0, 1.0);

        UNITY_UNROLL
        for (int i = 0; i < HB_REFRACTION_TAPS; i++)
        {
            float angle = rotation + i * (TWO_PI / HB_REFRACTION_TAPS);
            float along = sqrt((i + 0.5) / HB_REFRACTION_TAPS);
            float2 tap = target + float2(cos(angle), sin(angle)) * scale * along;
            float reach = HB_BackgroundSpread(tap, glass) / spread;
            float tapWeight = saturate((reach - along) / HB_REFRACTION_BLUR_SOFTNESS);
            int channel = i % 3;
            float3 mask = split ? float3(channel == 0, channel == 1, channel == 2) : float3(1.0, 1.0, 1.0);

            sum += HB_Background(tap + chroma * (1 - channel), lod) * (mask * tapWeight);
            weight += mask * tapWeight;
        }

        result = sum / weight;
    }

    return result;
}

float HB_NearestGlass(HBUpsample upsample)
{
    return max(max(LOAD_TEXTURE2D(_HB_OITGlass, upsample.texel0).r, LOAD_TEXTURE2D(_HB_OITGlass, upsample.texel1).r),
               max(LOAD_TEXTURE2D(_HB_OITGlass, upsample.texel2).r, LOAD_TEXTURE2D(_HB_OITGlass, upsample.texel3).r));
}

half4 HB_RefractFragment(float4 positionCS : SV_POSITION) : SV_Target
{
    uint2 pixel = uint2(positionCS.xy);

    if (HB_TransparencyDebug())
        return half4(HB_TransparencyDebugColour(HB_Upsample(pixel)), 1.0h);

    if (HB_OITEmpty(pixel))
        discard;

    float4 background = LOAD_TEXTURE2D(_HB_OITBackground, pixel);
    HBUpsample upsample = HB_Upsample(pixel);
    float absorbance = HB_UPSAMPLED(_HB_OITB0, upsample).r;
    float3 tint = HB_UPSAMPLED(_HB_OITTint, upsample).rgb;
    float4 refraction = HB_UPSAMPLED(_HB_OITRefraction, upsample);

    float3 behind = background.rgb;

    UNITY_BRANCH
    if (any(refraction != 0.0))
    {
        float2 uv = (float2(pixel) + 0.5) * _ScreenSize.zw;
        float2 offset = refraction.xy;
        float glass = HB_GlassEyeDepth(HB_NearestGlass(upsample));
        float spread = HB_BackgroundSpread(uv + offset, glass);

        if (spread < 0.0)
        {
            offset = float2(0.0, 0.0);
            spread = HB_BackgroundSpread(uv, glass);
        }

        behind = HB_Refracted(uv + offset, offset, saturate(refraction.z), saturate(refraction.w), saturate(spread),
                              glass, pixel);
    }

    return half4(behind * tint * exp(-absorbance) + HB_AddedColour(upsample, absorbance), background.a);
}

half4 HB_AddFragment(float4 positionCS : SV_POSITION) : SV_Target
{
    uint2 pixel = uint2(positionCS.xy);

    if (HB_TransparencyDebug())
        return half4(HB_TransparencyDebugColour(HB_Upsample(pixel)), 0.0h);

    if (HB_OITEmpty(pixel))
        discard;

    HBUpsample upsample = HB_Upsample(pixel);
    float3 colour = HB_AddedColour(upsample, HB_UPSAMPLED(_HB_OITB0, upsample).r);

    if (all(colour <= 0.0))
        discard;

    return half4(colour, 0.0h);
}

float HB_BackgroundDepthFragment(float4 positionCS : SV_POSITION) : SV_Target
{
    uint2 pixel = uint2(positionCS.xy);
    float absorbance = LOAD_TEXTURE2D(_HB_OITB0, pixel).r;
    float inverse = 1.0 / HB_HalfEyeDepth(int2(pixel));

    UNITY_BRANCH
    if (absorbance >= HB_OIT_EMPTY)
    {
        float mean = LOAD_TEXTURE2D(_HB_OITMoments, pixel).r / absorbance;

        inverse = lerp(inverse, 1.0 / HB_OITEyeDepth(mean), 1.0 - exp(-absorbance));
    }

    return inverse;
}

float HB_DownsampleDepthFragment(float4 positionCS : SV_POSITION, out float outDepth : SV_Depth) : SV_Target
{
    uint2 pixel = uint2(positionCS.xy);
    uint2 limit = uint2(_ScreenSize.xy) - 1;
    uint2 corner = pixel * 2;

    float4 depths = float4(LoadSceneDepth(min(corner, limit)), LoadSceneDepth(min(corner + uint2(1, 0), limit)),
                           LoadSceneDepth(min(corner + uint2(0, 1), limit)),
                           LoadSceneDepth(min(corner + uint2(1, 1), limit)));
    float high = max(max(depths.x, depths.y), max(depths.z, depths.w));
    float low = min(min(depths.x, depths.y), min(depths.z, depths.w));

#if UNITY_REVERSED_Z
    float nearest = high;
    float farthest = low;
#else
    float nearest = low;
    float farthest = high;
#endif

    float depth = ((pixel.x + pixel.y) & 1u) != 0u ? nearest : farthest;

    outDepth = depth;
    return depth;
}

#endif
