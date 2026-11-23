#ifndef HB_AMBIENT_OCCLUSION_PASS_INCLUDED
#define HB_AMBIENT_OCCLUSION_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

TEXTURE2D(_HB_AOSource);
float4 _HB_AOParams;
float4 _HB_AOSteps;
float4 _HB_AOSize;
float4 _HB_AOBlur;

#define HB_AO_RADIUS      _HB_AOParams.x
#define HB_AO_PROJECTION  _HB_AOParams.y
#define HB_AO_POWER       _HB_AOParams.z
#define HB_AO_SLICES      ((int)_HB_AOSteps.x)
#define HB_AO_STEPS       ((int)_HB_AOSteps.y)
#define HB_AO_MAX_PIXELS  _HB_AOSteps.z
#define HB_AO_MIN_PIXELS  _HB_AOSteps.w
#define HB_AO_FALLOFF     0.615
#define HB_AO_SKY         0.9999

static const float s_HB_AOBayer[16] =
{
    0.0, 8.0, 2.0, 10.0,
    12.0, 4.0, 14.0, 6.0,
    3.0, 11.0, 1.0, 9.0,
    15.0, 7.0, 13.0, 5.0
};

float4 HB_AOVertex(uint vertexID : SV_VertexID) : SV_POSITION
{
    return GetFullScreenTriangleVertexPosition(vertexID);
}

float HB_AORawDepth(float2 uv)
{
    float2 screen = UnityStereoTransformScreenSpaceTex(uv);
    float2 scaled = ClampAndScaleUVForBilinear(screen, _CameraDepthTexture_TexelSize.xy);

    return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, scaled, 0).r;
}

float HB_AOEyeDepth(float2 uv)
{
    return LinearEyeDepth(HB_AORawDepth(uv), _ZBufferParams);
}

float3 HB_AORelative(float2 uv, float raw)
{
    return ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP) - _WorldSpaceCameraPos;
}

float HB_AOFastAcos(float x)
{
    float a = abs(x);
    float r = (-0.156583 * a + HALF_PI) * sqrt(1.0 - a);

    return x >= 0.0 ? r : PI - r;
}

float4 HB_AOHorizon(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 uv = positionCS.xy * _HB_AOSize.zw;
    float raw = HB_AORawDepth(uv);
    float depth = LinearEyeDepth(raw, _ZBufferParams);

    if (depth >= _ProjectionParams.z * HB_AO_SKY)
        return 1.0;

    float3 position = HB_AORelative(uv, raw);
    float3 ray = position / depth;
    float3 rayX = (HB_AORelative(uv + float2(_HB_AOSize.z, 0.0), raw) - position) / depth;
    float3 rayY = (HB_AORelative(uv + float2(0.0, _HB_AOSize.w), raw) - position) / depth;
    float3 view = normalize(-position);
    float3 normal = normalize(SampleSceneNormals(uv));

    float radius = max(HB_AO_RADIUS, depth * HB_AO_MIN_PIXELS / HB_AO_PROJECTION);
    float radiusPixels = min(radius * HB_AO_PROJECTION / depth, HB_AO_MAX_PIXELS);

    if (radiusPixels < 1.0)
        return 1.0;

    uint2 cell = (uint2)positionCS.xy & 3u;
    float sliceNoise = (s_HB_AOBayer[cell.y * 4u + cell.x] + 0.5) / 16.0;
    float stepNoise = (s_HB_AOBayer[cell.x * 4u + ((cell.y + 2u) & 3u)] + 0.5) / 16.0;

    float falloffRange = HB_AO_FALLOFF * radius;
    float falloffMul = -1.0 / falloffRange;
    float falloffAdd = (radius - falloffRange) / falloffRange + 1.0;
    float nearest = 1.3 / radiusPixels;

    int slices = max(HB_AO_SLICES, 1);
    int steps = max(HB_AO_STEPS, 1);
    float visibility = 0.0;

    [loop]
    for (int slice = 0; slice < slices; slice++)
    {
        float angle = (slice + sliceNoise) / slices * PI;
        float2 omega = float2(cos(angle), sin(angle));
        float3 rayStep = rayX * omega.x + rayY * omega.y;
        float3 direction = normalize(rayStep);
        float3 ortho = direction - dot(direction, view) * view;
        float3 axis = normalize(cross(ortho, view));
        float3 projected = normal - axis * dot(normal, axis);
        float projectedLength = length(projected);
        float signNormal = dot(ortho, projected) >= 0.0 ? 1.0 : -1.0;
        float cosNormal = saturate(dot(projected, view) / max(projectedLength, 1e-4));
        float n = signNormal * HB_AOFastAcos(cosNormal);
        float lowCos0 = cos(n + HALF_PI);
        float lowCos1 = cos(n - HALF_PI);
        float horizonCos0 = lowCos0;
        float horizonCos1 = lowCos1;

        [loop]
        for (int tap = 0; tap < steps; tap++)
        {
            float s = (tap + stepNoise) / steps;
            float pixels = (s * s + nearest) * radiusPixels;
            float2 offset = omega * pixels * _HB_AOSize.zw;
            float3 shift = rayStep * pixels;

            float3 delta0 = (ray + shift) * HB_AOEyeDepth(uv + offset) - position;
            float3 delta1 = (ray - shift) * HB_AOEyeDepth(uv - offset) - position;
            float distance0 = length(delta0);
            float distance1 = length(delta1);
            float weight0 = saturate(distance0 * falloffMul + falloffAdd);
            float weight1 = saturate(distance1 * falloffMul + falloffAdd);
            float sampleCos0 = lerp(lowCos0, dot(delta0, view) / max(distance0, 1e-4), weight0);
            float sampleCos1 = lerp(lowCos1, dot(delta1, view) / max(distance1, 1e-4), weight1);

            horizonCos0 = max(horizonCos0, sampleCos0);
            horizonCos1 = max(horizonCos1, sampleCos1);
        }

        projectedLength = lerp(projectedLength, 1.0, 0.05);

        float h0 = -HB_AOFastAcos(horizonCos1);
        float h1 = HB_AOFastAcos(horizonCos0);

        h0 = n + clamp(h0 - n, -HALF_PI, HALF_PI);
        h1 = n + clamp(h1 - n, -HALF_PI, HALF_PI);

        float sinNormal = sin(n);
        float arc0 = (cosNormal + 2.0 * h0 * sinNormal - cos(2.0 * h0 - n)) * 0.25;
        float arc1 = (cosNormal + 2.0 * h1 * sinNormal - cos(2.0 * h1 - n)) * 0.25;

        visibility += projectedLength * (arc0 + arc1);
    }

    visibility = saturate(visibility / slices);

    return max(PositivePow(visibility, HB_AO_POWER), 0.03);
}

float4 HB_AOBlur(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 uv = positionCS.xy * _HB_AOSize.zw;
    float centre = HB_AOEyeDepth(uv);
    float tolerance = 1.0 / (centre * 0.1 + 0.05);
    float total = 0.0;
    float weights = 0.0;

    [unroll]
    for (int tap = -2; tap <= 2; tap++)
    {
        float2 tapUV = uv + _HB_AOBlur.xy * tap;
        float weight = (abs(tap) == 2 ? 0.5 : 1.0) * saturate(1.0 - abs(HB_AOEyeDepth(tapUV) - centre) * tolerance);

        total += SAMPLE_TEXTURE2D_LOD(_HB_AOSource, sampler_PointClamp, tapUV, 0).r * weight;
        weights += weight;
    }

    return total / max(weights, 1e-4);
}

float4 HB_AODebug(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 uv = positionCS.xy * _HB_AOSize.zw;
    float ao = SAMPLE_TEXTURE2D_LOD(_HB_AOSource, sampler_LinearClamp, uv, 0).r;

    return float4(ao.xxx, 1.0);
}

#endif
