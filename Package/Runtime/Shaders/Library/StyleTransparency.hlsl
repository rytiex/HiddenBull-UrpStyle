#ifndef HIDDENBULL_URPSTYLE_TRANSPARENCY_INCLUDED
#define HIDDENBULL_URPSTYLE_TRANSPARENCY_INCLUDED

TEXTURE2D(_HB_OITB0);
TEXTURE2D(_HB_OITMoments);

float4 _HB_OITParams;

#define HB_OIT_BIAS           _HB_OITParams.x
#define HB_OIT_LOG_NEAR       _HB_OITParams.y
#define HB_OIT_RCP_LOG_RANGE  _HB_OITParams.z

#define HB_OIT_OVERESTIMATION 0.25
#define HB_OIT_MAX_ALPHA      0.999
#define HB_OIT_EMPTY          1e-5

float HB_OITDepth(float3 positionWS)
{
    float eye = max(LinearEyeDepth(positionWS, GetWorldToViewMatrix()), 1e-4);

    return saturate((log(eye) - HB_OIT_LOG_NEAR) * HB_OIT_RCP_LOG_RANGE) * 2.0 - 1.0;
}

float HB_OITAbsorbance(half alpha)
{
    return -log(1.0 - min(float(alpha), HB_OIT_MAX_ALPHA));
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

float HB_OITTransmittanceAt(float2 positionCS, float depth)
{
    uint2 pixel = uint2(positionCS);

    float b0 = LOAD_TEXTURE2D(_HB_OITB0, pixel).r;
    float4 moments = LOAD_TEXTURE2D(_HB_OITMoments, pixel);

    return HB_OITTransmittance(b0, moments, depth);
}

#endif
