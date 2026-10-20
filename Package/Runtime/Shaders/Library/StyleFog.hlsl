#ifndef HIDDENBULL_URPSTYLE_FOG_INCLUDED
#define HIDDENBULL_URPSTYLE_FOG_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "StyleCommon.hlsl"
#include "StyleSky.hlsl"

float4 _HB_FogParams;
float4 _HB_FogScatter;

float HB_FogDensity(float3 positionWS)
{
    float falloff = max(_HB_FogParams.y, 1e-4);
    float relativeHeight = clamp((positionWS.y - _HB_FogParams.z) * falloff, -40.0, 40.0);

    return _HB_FogParams.x * exp(-relativeHeight);
}

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
    float densityAtOrigin = HB_FogDensity(origin);

    float verticalRate = direction.y * falloff;

    float integrated = abs(verticalRate) > 1e-4
        ? densityAtOrigin * (1.0 - exp(-clamp(verticalRate * travelled, -40.0, 40.0))) / verticalRate
        : densityAtOrigin * travelled;

    return half(saturate(1.0 - exp(-max(integrated, 0.0))) * _HB_FogParams.w);
}

#define HB_FOG_SCATTER_POWER 8.0h
#define HB_FOG_SUN_SHARE     0.35h

#define HB_FOG_PAINT      _HB_SkyPaint.y
#define HB_FOG_SILHOUETTE _HB_SkyPaint.z

half HB_FogDomePaint(half3 direction)
{
    if (HB_BRUSH_ATLAS_BOUND <= 0.5h)
        return 0.0h;

    return half(HB_FOG_PAINT) * (1.0h - smoothstep(0.85h, 1.0h, abs(direction.y)));
}

half3 HB_FogColourPainted(half3 direction, half4 atlas, half paint, half sun, half sky)
{
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

    half3 colour = HB_SampleSkyLut(up, HB_LUT_TIME, HB_LUT_FOG) * tone;

    half scattering = half(_HB_FogScatter.x) * half(_HB_FogScatter.w);

    colour *= saturate(sky + sun * scattering);

    if (scattering > 0.0h && _HB_SunDirection.w > 0.5)
    {
        half towardSun = half(saturate(dot(direction, _HB_SunDirection.xyz)));

        colour *= 1.0h - HB_FOG_SUN_SHARE * scattering * (1.0h - sun) * towardSun * towardSun;

        half scatter = PositivePow(towardSun, HB_FOG_SCATTER_POWER);

        colour = lerp(colour, _HB_SunColor.rgb * tone, saturate(scatter * scattering * sun));
    }

    return colour;
}

half3 HB_FogColour(half3 direction)
{
    half paint = HB_FogDomePaint(direction);
    half4 atlas = half4(0.5h, 0.5h, 0.5h, 1.0h);

    if (paint > 0.0h)
        atlas = HB_SkyStroke(direction);

    return HB_FogColourPainted(direction, atlas, paint, 1.0h, 1.0h);
}

half HB_SkyHaze(half3 direction)
{
    float density = _HB_FogParams.x;
    if (density <= 0.0)
        return 0.0h;

    float falloff = max(_HB_FogParams.y, 1e-4);
    float densityAtCamera = HB_FogDensity(GetCameraPositionWS());

    float opticalDepth = densityAtCamera / max(abs(direction.y) * falloff, 1e-3);

    return half(saturate(1.0 - exp(-min(opticalDepth, 40.0))) * _HB_FogParams.w);
}

half3 HB_SkyWithFog(half3 direction)
{
    half3 sky = HB_SkyColour(direction);
    half haze = HB_SkyHaze(direction);

    if (haze <= 0.0h)
        return sky;

    return lerp(sky, HB_FogColour(direction), haze);
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

half HB_FogPaintStroke(float3 anchorWS, half3 direction, float span, half amount, out half4 atlas)
{
    atlas = half4(0.5h, 0.5h, 0.5h, 1.0h);

    if (HB_BRUSH_ATLAS_BOUND <= 0.5h || HB_FOG_PAINT <= 0.0)
        return 0.0h;

    half paint = half(HB_FOG_PAINT);
    atlas = HB_FogWorldStroke(anchorWS, HB_TriplanarBlend(direction), span, HB_SKY_BRUSH_SMOOTH * 4.0);

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

TEXTURE3D(_HB_FogVolume);
float4 _HB_FogVolumeParams;

#define HB_FOG_HAS_VOLUME (_HB_FogVolumeParams.w > 0.5)

struct HB_FogSample
{
    half amount;
    half sun;
    half sky;
};

HB_FogSample HB_SampleFogVolume(float2 screenUV, float eyeDepth)
{
    float slices = _HB_FogVolumeParams.z;
    float slice = (log(max(eyeDepth, 1e-4)) - _HB_FogVolumeParams.x) * _HB_FogVolumeParams.y * slices;

    float w = clamp((slice - 0.5) / slices, 0.5 / slices, 1.0 - 0.5 / slices);
    float3 volume = SAMPLE_TEXTURE3D_LOD(_HB_FogVolume, sampler_LinearClamp, float3(screenUV, w), 0).rgb;

    volume *= saturate(slice);

    float opacity = max(volume.b, 1e-4);

    HB_FogSample fog;
    fog.amount = half(volume.b * _HB_FogParams.w);
    fog.sun = half(saturate(volume.r / opacity));
    fog.sky = half(saturate(volume.g / opacity));

    return fog;
}

#define HB_FOG_SILHOUETTE_TILES 8.0

float HB_SceneDepth(float2 uv)
{
    uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraDepthTexture_TexelSize.xy);

    return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0).r;
}

bool HB_IsSurface(float depth)
{
#if UNITY_REVERSED_Z
    return depth > 0.0;
#else
    return depth < 1.0;
#endif
}

half HB_FogAmountAt(float3 positionWS, float2 screenUV)
{
    if (!HB_FOG_HAS_VOLUME)
        return 0.0h;

    return HB_SampleFogVolume(screenUV, LinearEyeDepth(positionWS, GetWorldToViewMatrix())).amount;
}

struct HB_FogResult
{
    half amount;
    half sun;
    half sky;
    half3 direction;
    half3 colour;
};

HB_FogResult HB_ResolveFog(float3 positionWS, float2 screenUV, bool silhouette)
{
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 ray = positionWS - cameraPositionWS;
    float rayLength = max(length(ray), 1e-4);

    HB_FogResult result;
    result.amount = 0.0h;
    result.sun = 1.0h;
    result.sky = 1.0h;
    result.direction = half3(ray / rayLength);
    result.colour = half3(0.0h, 0.0h, 0.0h);

    if (!HB_FOG_HAS_VOLUME)
        return result;

    HB_FogSample fog = HB_SampleFogVolume(screenUV, LinearEyeDepth(positionWS, GetWorldToViewMatrix()));

    float tileAngle = HB_FogTileAngle();
    float span = rayLength * tileAngle;
    half strength = half(HB_FOG_SILHOUETTE);

    UNITY_BRANCH
    if (silhouette && HB_BRUSH_ATLAS_BOUND > 0.5h && strength > 0.0h)
    {
        half4 stroke = HB_FogWorldStroke(positionWS, HB_TriplanarBlend(result.direction),
                                         span / HB_FOG_SILHOUETTE_TILES, 1.0);

        float2 spine = float2(stroke.rg * 2.0h - 1.0h)
                     * (HB_BRUSH_SPINE_RANGE * strength * tileAngle / HB_FOG_SILHOUETTE_TILES);

        float3 painted = float3(result.direction) + UNITY_MATRIX_V[0].xyz * spine.x
                                                  + UNITY_MATRIX_V[1].xyz * spine.y;

        float2 paintedUV = saturate(ComputeNormalizedDeviceCoordinates(cameraPositionWS + painted,
                                                                       UNITY_MATRIX_VP));

        float paintedDepth = HB_SceneDepth(paintedUV);

        HB_FogSample paintedFog;

        if (HB_IsSurface(paintedDepth))
        {
            paintedFog = HB_SampleFogVolume(paintedUV, LinearEyeDepth(paintedDepth, _ZBufferParams));
        }
        else
        {
            float3 paintedWS = ComputeWorldSpacePosition(paintedUV, paintedDepth, UNITY_MATRIX_I_VP);

            paintedFog = HB_SampleFogVolume(paintedUV, 1e6);
            paintedFog.amount = HB_SkyHaze(half3(normalize(paintedWS - cameraPositionWS)));
        }

        if (paintedFog.amount > fog.amount)
        {
            fog = paintedFog;
            result.direction = half3(normalize(painted));
        }
    }

    result.amount = fog.amount;
    result.sun = fog.sun;
    result.sky = fog.sky;

    UNITY_BRANCH
    if (fog.amount > 0.0h)
    {
        half4 atlas;
        half paint = HB_FogPaintStroke(positionWS, result.direction, span, fog.amount, atlas);

        result.colour = HB_FogColourPainted(result.direction, atlas, paint, fog.sun, fog.sky);
    }

    return result;
}

HB_FogResult HB_ResolveSkyFog(float2 screenUV, half3 direction)
{
    HB_FogResult result;
    result.amount = 0.0h;
    result.sun = 1.0h;
    result.sky = 1.0h;
    result.direction = direction;
    result.colour = half3(0.0h, 0.0h, 0.0h);

    if (!HB_FOG_HAS_VOLUME)
        return result;

    HB_FogSample far = HB_SampleFogVolume(screenUV, 1e6);
    half haze = HB_SkyHaze(direction);

    result.amount = saturate((far.amount - haze) / max(1.0h - haze, HB_EPSILON));
    result.sun = far.sun;
    result.sky = far.sky;

    UNITY_BRANCH
    if (result.amount > 0.0h)
    {
        half paint = HB_FogDomePaint(direction);
        half4 atlas = paint > 0.0h ? HB_SkyStroke(direction) : half4(0.5h, 0.5h, 0.5h, 1.0h);

        result.colour = HB_FogColourPainted(direction, atlas, paint, far.sun, far.sky);
    }

    return result;
}

half3 HB_FogSurface(half3 colour, HB_FogResult fog)
{
    return lerp(colour, fog.colour, fog.amount);
}

half3 HB_FogLight(half3 light, half amount)
{
    return light * (1.0h - amount);
}

#endif
