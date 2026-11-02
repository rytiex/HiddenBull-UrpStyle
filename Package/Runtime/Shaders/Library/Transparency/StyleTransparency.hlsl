#ifndef HB_STYLE_TRANSPARENCY_INCLUDED
#define HB_STYLE_TRANSPARENCY_INCLUDED

TEXTURE2D_FLOAT(_HB_OITB0);
TEXTURE2D_FLOAT(_HB_OITMoments);
TEXTURE2D_FLOAT(_HB_OITChroma);
TEXTURE2D_FLOAT(_HB_OITGlass);
TEXTURE2D_FLOAT(_HB_OITDepth);

float4 _HB_OITParams;
float4 _HB_OITState;

#define HB_OIT_BIAS             _HB_OITParams.x
#define HB_OIT_LOG_NEAR         _HB_OITParams.y
#define HB_OIT_RCP_LOG_RANGE    _HB_OITParams.z
#define HB_OIT_ROUND_MARGIN     _HB_OITParams.w
#define HB_OIT_REFRACTIVE       (_HB_OITState.x > 0.5)
#define HB_OIT_ROUND            _HB_OITState.y
#define HB_OIT_HALF_SIZE        _HB_OITState.zw

#define HB_OIT_OVERESTIMATION   0.25
#define HB_OIT_MIN_TRANSMISSION 1e-6
#define HB_OIT_EMPTY            1e-5
#define HB_OIT_BEHIND           0.01

#define HB_REFRACTION_DEPTH     0.25
#define HB_REFRACTION_NEAR      0.05
#define HB_REFRACTION_REACH     0.1
#define HB_REFRACTION_BORDER    0.05
#define HB_STROKE_BEND          1.0

float HB_OITDepth(float3 positionWS)
{
    float eye = max(LinearEyeDepth(positionWS, GetWorldToViewMatrix()), 1e-4);

    return saturate((log(eye) - HB_OIT_LOG_NEAR) * HB_OIT_RCP_LOG_RANGE) * 2.0 - 1.0;
}

float HB_OITAbsorbance(half transmission)
{
    return -log(max(float(transmission), HB_OIT_MIN_TRANSMISSION));
}

void HB_OITMoments(float absorbance, float depth, out float b0, out float4 moments)
{
    float depthSquared = depth * depth;

    b0 = absorbance;
    moments = float4(depth, depthSquared, depthSquared * depth, depthSquared * depthSquared) * absorbance;
}

float HB_OITWeight(float b1, float b2, float support, float other0, float other1)
{
    float numerator = b2 - (other0 + other1) * b1 + other0 * other1;
    float denominator = (support - other0) * (support - other1);

    return numerator / (abs(denominator) > 1e-7 ? denominator : 1e-7);
}

float HB_OITTransmittance(float b0, float4 moments, float depth, float absorbance)
{
    if (b0 < HB_OIT_EMPTY)
        return 1.0;

    float4 b = lerp(moments / b0, float4(0.0, 0.375, 0.0, 0.375), HB_OIT_BIAS);

    float d11 = b.y - b.x * b.x;
    float l21d11 = b.z - b.x * b.y;
    float l21 = l21d11 / d11;
    float d22 = b.w - b.y * b.y - l21d11 * l21;

    float3 c = float3(1.0, depth, depth * depth);

    c.y -= b.x;
    c.z -= b.y + l21 * c.y;
    c.y /= d11;
    c.z /= d22;
    c.y -= l21 * c.z;
    c.x -= dot(c.yz, b.xy);

    float p = c.y / c.z;
    float q = c.x / c.z;
    float root = sqrt(max(p * p * 0.25 - q, 0.0));

    float z1 = -p * 0.5 - root;
    float z2 = -p * 0.5 + root;

    float w0 = HB_OITWeight(b.x, b.y, depth, z1, z2);
    float w1 = HB_OITWeight(b.x, b.y, z1, depth, z2);
    float w2 = HB_OITWeight(b.x, b.y, z2, depth, z1);

    float own = min(max(w0, 0.0), absorbance / b0);

    float front = (w0 - own) * HB_OIT_OVERESTIMATION
                + (z1 < depth ? w1 : 0.0)
                + (z2 < depth ? w2 : 0.0);

    return exp(-b0 * saturate(front));
}

float HB_OITTransmittanceAt(uint2 pixel, float depth, float absorbance)
{
    return HB_OITTransmittance(LOAD_TEXTURE2D(_HB_OITB0, pixel).r, LOAD_TEXTURE2D(_HB_OITMoments, pixel), depth,
                               absorbance);
}

float HB_GlassNearness(float depth)
{
    return 0.5 - depth * 0.5;
}

float HB_OITEyeDepth(float depth)
{
    return exp((depth * 0.5 + 0.5) / HB_OIT_RCP_LOG_RANGE + HB_OIT_LOG_NEAR);
}

float HB_GlassEyeDepth(float nearness)
{
    return HB_OITEyeDepth(1.0 - nearness * 2.0);
}

float HB_OITMargin(float depth)
{
    return max(HB_OIT_BEHIND / HB_OITEyeDepth(depth) * HB_OIT_RCP_LOG_RANGE, HB_OIT_ROUND_MARGIN);
}

bool HB_OITBehindGlass(uint2 pixel, float depth)
{
    float here = depth * 0.5 + 0.5;
    float glass = 1.0 - LOAD_TEXTURE2D(_HB_OITGlass, pixel).r;

    return here > glass + HB_OITMargin(depth);
}

float4 HB_OITChroma(half3 transmission, float depth)
{
    float3 absorbance = -log(max(float3(transmission), HB_OIT_MIN_TRANSMISSION));

    return float4(absorbance, (absorbance.r + absorbance.g + absorbance.b) * (depth * 0.5 + 0.5));
}

half3 HB_OITChromaAt(uint2 pixel, float depth)
{
    float4 chroma = LOAD_TEXTURE2D(_HB_OITChroma, pixel);
    float weight = chroma.r + chroma.g + chroma.b;

    if (weight < HB_OIT_EMPTY)
        return half3(1.0h, 1.0h, 1.0h);

    float behind = saturate((depth * 0.5 + 0.5 - chroma.a / weight) / HB_OITMargin(depth) - 1.0);
    float3 transmission = exp(-chroma.rgb);
    float3 colour = transmission / max(dot(transmission, 1.0 / 3.0), HB_OIT_MIN_TRANSMISSION);

    return half3(lerp(1.0, colour, behind));
}

bool HB_OITOutsideRound(uint2 pixel, float depth)
{
    return HB_OIT_ROUND > 0.5 && (HB_OIT_ROUND < 1.5) != HB_OITBehindGlass(pixel, depth);
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

half3 HB_TintTransmission(half3 colour, half strength)
{
    return lerp(half3(1.0h, 1.0h, 1.0h), colour, strength);
}

half HB_TintMean(half3 transmission)
{
    return dot(transmission, half3(1.0h, 1.0h, 1.0h) / 3.0h);
}

#endif
