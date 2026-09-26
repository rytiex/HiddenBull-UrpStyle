#ifndef HIDDENBULL_URPSTYLE_FOG_INCLUDED
#define HIDDENBULL_URPSTYLE_FOG_INCLUDED

#include "StyleCommon.hlsl"
#include "StyleSky.hlsl"

float4 _HB_FogParams;
float4 _HB_FogScatter;

half HB_FogAmount(float3 positionWS, float3 cameraPositionWS)
{
    float density = _HB_FogParams.x;
    if (density <= 0.0)
        return 0.0h;

    float3 ray = positionWS - cameraPositionWS;
    float rayLength = length(ray);
    if (rayLength < 1e-4)
        return 0.0h;

    float3 direction = ray / rayLength;

    float travelled = rayLength - _HB_FogScatter.z;
    if (travelled <= 0.0)
        return 0.0h;

    float3 origin = cameraPositionWS + direction * _HB_FogScatter.z;

    float falloff = max(_HB_FogParams.y, 1e-4);
    float relativeHeight = clamp((origin.y - _HB_FogParams.z) * falloff, -40.0, 40.0);
    float densityAtOrigin = density * exp(-relativeHeight);

    float verticalRate = direction.y * falloff;

    float integrated = abs(verticalRate) > 1e-4
        ? densityAtOrigin * (1.0 - exp(-clamp(verticalRate * travelled, -40.0, 40.0))) / verticalRate
        : densityAtOrigin * travelled;

    return half(saturate(1.0 - exp(-max(integrated, 0.0))) * _HB_FogParams.w);
}

#define HB_FOG_SCATTER_POWER 8.0h

#define HB_FOG_PAINT      _HB_SkyPaint.y
#define HB_FOG_SILHOUETTE _HB_SkyPaint.z

half HB_FogDomePaint(half3 direction)
{
    if (HB_BRUSH_ATLAS_BOUND <= 0.5h)
        return 0.0h;

    return half(HB_FOG_PAINT) * (1.0h - smoothstep(0.85h, 1.0h, abs(direction.y)));
}

half3 HB_FogColourPainted(half3 direction, half4 atlas, half paint, half sunlit, half reach)
{
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    half shade = half(_HB_FogScatter.y);
#else
    half shade = 0.0h;
#endif

    half up = direction.y;
    half tone = 1.0h;

    if (paint > 0.0h)
    {
        half stroke = (atlas.b * 2.0h - 1.0h) * half(HB_SKY_BRUSH) * paint;

        half lift;
        direction = HB_SkyPaintDirection(direction, atlas, half(HB_SKY_PAINT) * paint, lift);

        up += lift + stroke * 0.32h;
        tone += stroke * 0.5h;
    }

    half3 colour = HB_SampleSkyLut(up, HB_LUT_TIME, HB_LUT_FOG);

    half amount = half(_HB_FogScatter.x) * half(_HB_FogScatter.w) * sunlit * reach;
    if (amount > 0.0h && _HB_SunDirection.w > 0.5)
    {
        half towardSun = half(saturate(dot(direction, _HB_SunDirection.xyz)));
        half scatter = PositivePow(towardSun, HB_FOG_SCATTER_POWER);

        colour = lerp(colour, _HB_SunColor.rgb, saturate(scatter * amount));
    }

    return colour * (tone * PositivePow(reach, shade));
}

half3 HB_FogColour(half3 direction, half sunlit, half reach)
{
    half paint = HB_FogDomePaint(direction);
    half4 atlas = half4(0.5h, 0.5h, 0.5h, 1.0h);

    if (paint > 0.0h)
        atlas = HB_SkyStroke(direction);

    return HB_FogColourPainted(direction, atlas, paint, sunlit, reach);
}

half HB_SkyHaze(half3 direction)
{
    float density = _HB_FogParams.x;
    if (density <= 0.0)
        return 0.0h;

    float falloff = max(_HB_FogParams.y, 1e-4);
    float relativeHeight = clamp((GetCameraPositionWS().y - _HB_FogParams.z) * falloff, -40.0, 40.0);
    float densityAtCamera = density * exp(-relativeHeight);

    float opticalDepth = densityAtCamera / max(abs(direction.y) * falloff, 1e-3);

    return half(saturate(1.0 - exp(-min(opticalDepth, 40.0))) * _HB_FogParams.w);
}

half3 HB_SkyWithFog(half3 direction)
{
    half3 sky = HB_SkyColour(direction);
    half haze = HB_SkyHaze(direction);

    if (haze <= 0.0h)
        return sky;

    return lerp(sky, HB_FogColour(direction, 1.0h, 1.0h), haze);
}

#define HB_FOG_STEPS  4
#define HB_FOG_SPREAD 0.75
#define HB_FOG_BLUR   0.5
#define HB_FOG_BIAS   0.25
#define HB_FOG_GOLDEN 2.39996323
#define HB_FOG_GOLDEN_FRACTION 0.61803399

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)

float HB_AirSun(float3 positionWS)
{
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    float4 coord = TransformWorldToShadowCoord(positionWS);

    return BEYOND_SHADOW_FAR(coord) ? 1.0 : MainLightRealtimeShadow(coord);
#else
    return 0.0;
#endif
}

bool HB_AirLight(float3 positionWS, float3 bias, float open, out float light)
{
    APVResources resources = FillAPVResources();

    float3 uvw;

    if (!TryToGetPoolUVW(resources, positionWS - _APVWorldOffset, bias, bias, uvw))
    {
        light = 0.0;
        return false;
    }

    float3 stored;

    UNITY_BRANCH
    if (_APVLeakReductionMode == APVLEAKREDUCTIONMODE_QUALITY)
    {
        APVSample probe = QualityLeakReduction(resources, 0xFFFFFFFF, uvw);

        stored = probe.L0;
    }
    else
    {
        WarpUVWLeakReduction(resources, 0xFFFFFFFF, uvw);

        stored = SAMPLE_TEXTURE3D_LOD(resources.L0_L1Rx, s_linear_clamp_sampler, uvw, 0).rgb;
    }

    light = Luminance(stored) / open;

    if (_APVSkyOcclusionWeight > 0)
    {
        light += kSHBasis0 * SAMPLE_TEXTURE3D_LOD(resources.SkyOcclusionL0L1,
                                                  s_linear_clamp_sampler, uvw, 0).x;
    }

    if (HB_AMBIENT_PROBES > 1.5)
        light += HB_AirSun(positionWS) * _HB_KeyColor.a;

    return true;
}

#endif

half HB_FogAirReach(half reach, float3 cameraPositionWS, half3 direction, float rayLength,
                    float2 screenUV)
{
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    float density = max(_HB_FogParams.x, 1e-4);
    float start = min(_HB_FogScatter.z, rayLength * 0.5);
    float span = min(rayLength - start, 3.0 / density);

    if (span <= 0.0)
        return reach;

    span = max(span - HB_FOG_BIAS, span * 0.5);

    float opacity = 1.0 - exp(-density * span);
    float rcpDensity = 1.0 / density;

    float3 origin = cameraPositionWS + float3(direction) * start;
    float2 pixel = screenUV * _ScreenSize.xy;
    float open = max(HB_AMBIENT_OPEN, HB_EPSILON);

    float jitter = InterleavedGradientNoise(pixel, 0);

    float3 forward = float3(direction);
    float3 reference = abs(forward.y) > 0.99 ? float3(0.0, 0.0, 1.0) : float3(0.0, 1.0, 0.0);
    float3 right = normalize(cross(forward, reference));
    float3 up = cross(right, forward);

    float radius = min(span * (HB_FOG_SPREAD / HB_FOG_STEPS), HB_FOG_BLUR);

    float3 shaft = _HB_SunDirection.w > 0.5 ? -_HB_SunDirection.xyz * _APVMinBrickSize : float3(0.0, 0.0, 0.0);

    float lit = 0.0;
    float peak = -1.0;
    float found = 0.0;

    UNITY_UNROLL
    for (int i = 0; i < HB_FOG_STEPS; i++)
    {
        float slice = (i + 0.5) * (1.0 / HB_FOG_STEPS);
        float depth = -log(max(1.0 - slice * opacity, 1e-6)) * rcpDensity;

        float angle = (jitter + i) * HB_FOG_GOLDEN;
        float3 spread = (right * cos(angle) + up * sin(angle)) * (radius * (1.0 - slice))
                      + shaft * frac(jitter + i * HB_FOG_GOLDEN_FRACTION);

        float air;

        if (HB_AirLight(origin + forward * depth + spread, -forward, open, air))
        {
            float light = saturate(air);

            lit += light;
            peak = max(peak, light);
            found += 1.0;
        }
    }

    if (found < 0.5)
        return reach;

    if (found < 1.5)
        return half(lit);

    return half((lit - peak) / (found - 1.0));
#else
    return reach;
#endif
}

half4 HB_FogStrokePlanes(float3 position, half3 blend, float lod)
{
    half4 sum = half4(0.0h, 0.0h, 0.0h, 0.0h);
    half total = 0.0h;

    UNITY_BRANCH
    if (blend.x > 0.02h)
    {
        sum += SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas, position.zy, lod) * blend.x;
        total += blend.x;
    }

    UNITY_BRANCH
    if (blend.y > 0.02h)
    {
        sum += SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas, position.xz, lod) * blend.y;
        total += blend.y;
    }

    UNITY_BRANCH
    if (blend.z > 0.02h)
    {
        sum += SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas, position.xy, lod) * blend.z;
        total += blend.z;
    }

    return sum * rcp(max(total, HB_EPSILON));
}

half4 HB_FogWorldStroke(float3 positionWS, half3 blend, float tile, float lod)
{
    float level = log2(max(tile, 1e-3));
    float base = floor(level);
    float density = exp2(-base);

    half4 fine = HB_FogStrokePlanes(positionWS * density, blend, lod);
    half4 coarse = HB_FogStrokePlanes(positionWS * (density * 0.5), blend, lod);

    return lerp(fine, coarse, half(level - base));
}

float HB_FogTileAngle()
{
    return TWO_PI / HB_SkyBrushTurns(max(HB_SKY_BRUSH_SCALE, 1e-3));
}

half HB_FogPaintStroke(float3 anchorWS, half3 blend, half3 direction, float span, half amount,
                       out half4 atlas)
{
    atlas = half4(0.5h, 0.5h, 0.5h, 1.0h);

    if (HB_BRUSH_ATLAS_BOUND <= 0.5h || HB_FOG_PAINT <= 0.0)
        return 0.0h;

    half paint = half(HB_FOG_PAINT);
    atlas = HB_FogWorldStroke(anchorWS, blend, span, HB_SKY_BRUSH_SMOOTH * 4.0);

    half far = smoothstep(0.5h, 1.0h, amount * rcp(max(half(_HB_FogParams.w), HB_EPSILON)));

    if (far > 0.0h)
    {
        half domePaint = HB_FogDomePaint(direction);

        if (domePaint > 0.0h)
            atlas = lerp(atlas, HB_SkyStroke(direction), far);

        paint = lerp(paint, domePaint, far);
    }

    return paint;
}

TEXTURE2D(_HB_FogReach);
float4 _HB_FogReachSize;

half3 HB_ApplyFog(half3 colour, float3 positionWS, half3 normalWS, half sunlit, float2 screenUV)
{
    float3 cameraPositionWS = GetCameraPositionWS();

    half amount = HB_FogAmount(positionWS, cameraPositionWS);
    if (amount <= 0.0h)
        return colour;

    float3 ray = positionWS - cameraPositionWS;
    float rayLength = max(length(ray), 1e-4);
    half3 direction = half3(ray / rayLength);

    half reach = 1.0h;

    if (_HB_FogReachSize.z > 0.5)
        reach = SAMPLE_TEXTURE2D_LOD(_HB_FogReach, sampler_LinearClamp, screenUV, 0).r;

    half4 atlas;
    half paint = HB_FogPaintStroke(positionWS, HB_TriplanarBlend(normalWS), direction,
                                   rayLength * HB_FogTileAngle(), amount, atlas);

    return lerp(colour, HB_FogColourPainted(direction, atlas, paint, sunlit, reach), amount);
}

#endif
