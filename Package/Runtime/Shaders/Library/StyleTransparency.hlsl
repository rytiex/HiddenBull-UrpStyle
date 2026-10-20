#ifndef HIDDENBULL_URPSTYLE_TRANSPARENCY_INCLUDED
#define HIDDENBULL_URPSTYLE_TRANSPARENCY_INCLUDED

TEXTURE2D(_HB_OITB0);
TEXTURE2D(_HB_OITMoments);
TEXTURE2D(_HB_OITBehindMoments);
TEXTURE2D(_HB_OITInfo);

float4 _HB_OITParams;

#define HB_OIT_BIAS           _HB_OITParams.x
#define HB_OIT_LOG_NEAR       _HB_OITParams.y
#define HB_OIT_RCP_LOG_RANGE  _HB_OITParams.z

#define HB_OIT_OVERESTIMATION 0.25
#define HB_OIT_MIN_TRANSMISSION 1e-6
#define HB_OIT_EMPTY          1e-5
#define HB_OIT_BEHIND         0.01
#define HB_OIT_BEHIND_BITS    9.0

#define HB_REFRACTION_DEPTH   0.25
#define HB_REFRACTION_NEAR    0.05
#define HB_REFRACTION_REACH   0.1
#define HB_REFRACTION_BORDER  0.05
#define HB_STROKE_BEND        1.0

float HB_OITDepth(float3 positionWS)
{
    float eye = max(LinearEyeDepth(positionWS, GetWorldToViewMatrix()), 1e-4);

    return saturate((log(eye) - HB_OIT_LOG_NEAR) * HB_OIT_RCP_LOG_RANGE) * 2.0 - 1.0;
}

float HB_OITAbsorbance(half alpha)
{
    return -log(max(1.0 - float(alpha), HB_OIT_MIN_TRANSMISSION));
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

float HB_OITTransmittance(float b0, float4 moments, float depth)
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

    float front = w0 * HB_OIT_OVERESTIMATION
                + (z1 < depth ? w1 : 0.0)
                + (z2 < depth ? w2 : 0.0);

    return exp(-b0 * saturate(front));
}

float HB_OITFrontTransmittanceAt(uint2 pixel, float depth)
{
    return HB_OITTransmittance(LOAD_TEXTURE2D(_HB_OITB0, pixel).r, LOAD_TEXTURE2D(_HB_OITMoments, pixel), depth);
}

float HB_OITBehindTransmittanceAt(uint2 pixel, float depth)
{
    return HB_OITTransmittance(LOAD_TEXTURE2D(_HB_OITB0, pixel).g, LOAD_TEXTURE2D(_HB_OITBehindMoments, pixel),
                               depth);
}

float HB_OITTransmittanceAt(uint2 pixel, float depth)
{
    return HB_OITFrontTransmittanceAt(pixel, depth) * HB_OITBehindTransmittanceAt(pixel, depth);
}

bool HB_OITBehindRefraction(uint2 pixel, float depth)
{
    float stored = LOAD_TEXTURE2D(_HB_OITInfo, pixel).r;
    float here = depth * 0.5 + 0.5;
    float eye = exp(here / HB_OIT_RCP_LOG_RANGE + HB_OIT_LOG_NEAR);

    float quantum = exp2(floor(log2(max(stored, 1e-6))) - HB_OIT_BEHIND_BITS);
    float margin = max(HB_OIT_BEHIND / eye * HB_OIT_RCP_LOG_RANGE, quantum);

    return here > 1.0 - stored + margin;
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

#endif
