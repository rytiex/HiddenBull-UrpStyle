#ifndef HB_STYLE_TRANSPARENCY_INCLUDED
#define HB_STYLE_TRANSPARENCY_INCLUDED

TEXTURE2D_FLOAT(_HB_TransparentNearest);
TEXTURE2D(_HB_RefractionBackground);

float _HB_TransparentStage;

#define HB_TRANSPARENT_SPLIT    (_HB_TransparentStage > 0.5)
#define HB_TRANSPARENT_FRONT    (_HB_TransparentStage > 1.5)

#define HB_NEAREST_MIN_EYE      1e-3
#define HB_NEAREST_MARGIN       0.01

#define HB_REFRACTION_DEPTH     0.25
#define HB_REFRACTION_NEAR      0.05
#define HB_REFRACTION_REACH     0.1
#define HB_REFRACTION_BORDER    0.05
#define HB_STROKE_BEND          1.0
#define HB_REFRACTION_BLUR_MIPS 6.0
#define HB_REFRACTION_DISPERSION 0.35
#define HB_REFRACTION_SHARP     1e-3

float HB_NearestValue(float eye)
{
    return 1.0 / max(eye, HB_NEAREST_MIN_EYE);
}

bool HB_InFrontOfNearest(uint2 pixel, float eye)
{
    float nearest = LOAD_TEXTURE2D(_HB_TransparentNearest, pixel).r;

    return nearest > 0.0 && eye * nearest < 1.0 + HB_NEAREST_MARGIN;
}

bool HB_InFrontOfRefractive(uint2 pixel, float eye)
{
    float refractive = LOAD_TEXTURE2D(_HB_TransparentNearest, pixel).g;

    return refractive <= 0.0 || eye * refractive < 1.0 + HB_NEAREST_MARGIN;
}

bool HB_TransparentOutsideStage(uint2 pixel, float eye)
{
    return HB_TRANSPARENT_SPLIT && HB_TRANSPARENT_FRONT != HB_InFrontOfRefractive(pixel, eye);
}

float2 HB_RefractionOffset(float3 positionWS, half3 normalWS, half3 viewDirectionWS, half strength,
                           float3 strokeShift, half strokeBend)
{
    float3 normal = float3(normalWS);
    float3 view = float3(viewDirectionWS);

    float3 shift = (view * dot(normal, view) - normal) * (strength * HB_REFRACTION_DEPTH)
                 + strokeShift * (strokeBend * HB_STROKE_BEND);

    float4 from = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
    float4 to = mul(UNITY_MATRIX_VP, float4(positionWS + shift, 1.0));

    float scale = 0.5 / max(from.w, HB_REFRACTION_NEAR);
    float2 uv = from.xy * scale;
    float2 offset = (to.xy - from.xy) * scale;

#if UNITY_UV_STARTS_AT_TOP
    uv.y = -uv.y;
    offset.y = -offset.y;
#endif

    uv += 0.5;
    offset *= min(1.0, HB_REFRACTION_REACH / max(length(offset), 1e-6));

    float2 target = uv + offset;
    float2 border = min(target, 1.0 - target);

    return offset * saturate(min(border.x, border.y) / HB_REFRACTION_BORDER);
}

half3 HB_BackgroundAt(float2 uv, float lod)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_RefractionBackground, sampler_LinearClamp, uv, lod).rgb;
}

half3 HB_BackgroundSmooth(float2 uv, float level)
{
    float width, height, mips;
    _HB_RefractionBackground.GetDimensions(uint(level), width, height, mips);

    float2 texel = 1.0 / float2(width, height);
    float2 position = uv / texel - 0.5;
    float2 f = frac(position);
    float2 base = position - f;

    float2 f2 = f * f;
    float2 f3 = f2 * f;
    float2 w0 = (1.0 - f) * (1.0 - f) * (1.0 - f) / 6.0;
    float2 w1 = (3.0 * f3 - 6.0 * f2 + 4.0) / 6.0;
    float2 w3 = f3 / 6.0;
    float2 w2 = 1.0 - w0 - w1 - w3;

    float2 g0 = w0 + w1;
    float2 g1 = w2 + w3;
    float2 near = (base - 0.5 + w1 / g0) * texel;
    float2 far = (base + 1.5 + w3 / g1) * texel;

    float3 low = float3(HB_BackgroundAt(near, level)) * g0.x
               + float3(HB_BackgroundAt(float2(far.x, near.y), level)) * g1.x;
    float3 high = float3(HB_BackgroundAt(float2(near.x, far.y), level)) * g0.x
                + float3(HB_BackgroundAt(far, level)) * g1.x;

    return half3(low * g0.y + high * g1.y);
}

half3 HB_RefractionBackground(float2 uv, float lod)
{
    half3 colour;

    UNITY_BRANCH
    if (lod < HB_REFRACTION_SHARP)
    {
        colour = HB_BackgroundAt(uv, 0.0);
    }
    else
    {
        float level = floor(lod);
        float upper = min(level + 1.0, HB_REFRACTION_BLUR_MIPS);

        colour = lerp(HB_BackgroundSmooth(uv, level), HB_BackgroundSmooth(uv, upper), half(lod - level));
    }

    return colour;
}

half3 HB_Refracted(float2 uv, float2 offset, half blur, half dispersion)
{
    float lod = blur * HB_REFRACTION_BLUR_MIPS;
    float2 target = saturate(uv + offset);

    half3 colour;

    UNITY_BRANCH
    if (dispersion <= 0.0h)
    {
        colour = HB_RefractionBackground(target, lod);
    }
    else
    {
        float2 chroma = offset * (dispersion * HB_REFRACTION_DISPERSION);

        colour = half3(HB_RefractionBackground(saturate(target + chroma), lod).r,
                       HB_RefractionBackground(target, lod).g,
                       HB_RefractionBackground(saturate(target - chroma), lod).b);
    }

    return colour;
}

half3 HB_TintTransmission(half3 colour, half strength)
{
    return lerp(half3(1.0h, 1.0h, 1.0h), colour, strength);
}

half HB_TintMean(half3 transmission)
{
    return dot(transmission, half3(1.0h, 1.0h, 1.0h) / 3.0h);
}

#endif
