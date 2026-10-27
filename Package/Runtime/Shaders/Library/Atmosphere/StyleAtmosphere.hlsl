#ifndef HB_STYLE_ATMOSPHERE_INCLUDED
#define HB_STYLE_ATMOSPHERE_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSky.hlsl"

#define HB_FOG_DENSITY          _HB_Fog.x
#define HB_FOG_FALLOFF          _HB_Fog.y
#define HB_FOG_BASE_HEIGHT      _HB_Fog.z
#define HB_FOG_MAX_OPACITY      _HB_Fog.w
#define HB_FOG_SCATTERING       _HB_FogScatter.x
#define HB_FOG_START            _HB_FogScatter.y
#define HB_FOG_LAYER_SOFTNESS   _HB_FogScatter.z
#define HB_HORIZON_DENSITY      _HB_Horizon.x
#define HB_HORIZON_FALLOFF      _HB_Horizon.y
#define HB_HORIZON_START        _HB_Horizon.z
#define HB_HORIZON_LAYERS       _HB_Horizon.w
#define HB_FOG_PAINT            _HB_FogPaint.x
#define HB_FOG_SILHOUETTE       _HB_FogPaint.y

#define HB_FOG_SCATTER_POWER    8.0h
#define HB_FOG_SILHOUETTE_TILES 8.0

#define HB_DEBUG_FOG_AMOUNT     7

struct HBAtmosphere
{
    half fog;
    half horizon;
};

half HB_HeightIntegral(float3 positionWS, float3 cameraPositionWS, float density, float falloff, float start)
{
    float3 ray = positionWS - cameraPositionWS;
    float rayLength = length(ray);
    float travelled = rayLength - start;

    half amount = 0.0h;

    UNITY_BRANCH
    if (density > 0.0 && travelled > 0.0)
    {
        float3 direction = ray / max(rayLength, 1e-4);
        float3 origin = cameraPositionWS + direction * start;

        float rate = max(falloff, 1e-4);
        float height = clamp((origin.y - HB_FOG_BASE_HEIGHT) * rate, -40.0, 40.0);
        float densityAtOrigin = density * exp(-height);
        float verticalRate = direction.y * rate;

        float integrated = abs(verticalRate) > 1e-4
            ? densityAtOrigin * (1.0 - exp(-clamp(verticalRate * travelled, -40.0, 40.0))) / verticalRate
            : densityAtOrigin * travelled;

        amount = half(saturate(1.0 - exp(-max(integrated, 0.0))));
    }

    return amount;
}

half HB_HorizonLayers(half amount)
{
    float layers = HB_HORIZON_LAYERS;

    UNITY_BRANCH
    if (layers > 0.5)
    {
        float x = float(amount) * layers;
        float step = floor(x);
        float edge = 0.5 * max(float(HB_FOG_LAYER_SOFTNESS), 1e-3);

        amount = half((step + smoothstep(0.5 - edge, 0.5 + edge, x - step)) / layers);
    }

    return amount;
}

HBAtmosphere HB_AtmosphereAmount(float3 positionWS, float3 cameraPositionWS)
{
    HBAtmosphere atmosphere;

    atmosphere.fog = HB_HeightIntegral(positionWS, cameraPositionWS, HB_FOG_DENSITY, HB_FOG_FALLOFF, HB_FOG_START)
                   * half(HB_FOG_MAX_OPACITY);
    atmosphere.horizon = HB_HorizonLayers(HB_HeightIntegral(positionWS, cameraPositionWS, HB_HORIZON_DENSITY,
                                                            HB_HORIZON_FALLOFF, HB_HORIZON_START));

    return atmosphere;
}

half HB_FogAmount(float3 positionWS, float3 cameraPositionWS)
{
    return HB_HeightIntegral(positionWS, cameraPositionWS, HB_FOG_DENSITY, HB_FOG_FALLOFF, HB_FOG_START)
         * half(HB_FOG_MAX_OPACITY);
}

half HB_SkyHaze(half3 direction)
{
    half haze = 0.0h;

    UNITY_BRANCH
    if (HB_FOG_DENSITY > 0.0)
    {
        float rate = max(HB_FOG_FALLOFF, 1e-4);
        float height = clamp((GetCameraPositionWS().y - HB_FOG_BASE_HEIGHT) * rate, -40.0, 40.0);
        float opticalDepth = HB_FOG_DENSITY * exp(-height) / max(abs(direction.y) * rate, 1e-3);

        haze = half(saturate(1.0 - exp(-min(opticalDepth, 40.0))) * HB_FOG_MAX_OPACITY);
    }

    return haze;
}

half HB_FogDomePaint(half3 direction)
{
    half paint = 0.0h;

    if (HB_BRUSH_ATLAS_BOUND > 0.5h)
        paint = half(HB_FOG_PAINT) * (1.0h - smoothstep(0.85h, 1.0h, abs(direction.y)));

    return paint;
}

half3 HB_FogColourPainted(half3 direction, half4 atlas, half paint)
{
    half up = direction.y;
    half tone = 1.0h;

    UNITY_BRANCH
    if (paint > 0.0h)
    {
        half stroke = (atlas.b * 2.0h - 1.0h) * half(HB_SKY_BRUSH) * paint;
        half lift;

        direction = HB_SkyPaintDirection(direction, atlas, half(HB_SKY_PAINT) * paint, lift);
        up += lift + stroke * 0.32h;
        tone += stroke * 0.5h;
    }

    half3 colour = HB_SampleSkyLut(up, HB_SKY_ROW, HB_LUT_FOG) * tone;
    half scattering = half(HB_FOG_SCATTERING);

    UNITY_BRANCH
    if (scattering > 0.0h)
    {
        half towardSun = half(saturate(dot(float3(direction), _HB_SunDirection.xyz)));
        half scatter = PositivePow(towardSun, HB_FOG_SCATTER_POWER);

        colour = lerp(colour, half3(_HB_SunColor.rgb) * tone, saturate(scatter * scattering));
    }

    return colour;
}

half3 HB_FogColour(half3 direction)
{
    half paint = HB_FogDomePaint(direction);
    half4 atlas = half4(0.5h, 0.5h, 0.5h, 1.0h);

    UNITY_BRANCH
    if (paint > 0.0h)
        atlas = HB_SkyStroke(direction);

    return HB_FogColourPainted(direction, atlas, paint);
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

    return sum * rcp(max(total, half(HB_EPSILON)));
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

    half paint = 0.0h;

    UNITY_BRANCH
    if (HB_BRUSH_ATLAS_BOUND > 0.5h && HB_FOG_PAINT > 0.0)
    {
        paint = half(HB_FOG_PAINT);
        atlas = HB_FogWorldStroke(anchorWS, HB_TriplanarBlend(direction), span, HB_SKY_BRUSH_SMOOTH * 4.0);

        half far = smoothstep(0.5h, 1.0h, amount * rcp(max(half(HB_FOG_MAX_OPACITY), half(HB_EPSILON))));

        UNITY_BRANCH
        if (far > 0.0h)
        {
            half dome = HB_FogDomePaint(direction);

            if (dome > 0.0h)
                atlas = lerp(atlas, HB_SkyStroke(direction), far);

            paint = lerp(paint, dome, far);
        }
    }

    return paint;
}

half3 HB_ApplyAtmosphere(half3 colour, float3 positionWS)
{
#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW != 0 && HB_DEBUG_VIEW != HB_DEBUG_FOG_AMOUNT)
        return colour;
#endif

    float3 cameraPositionWS = GetCameraPositionWS();
    HBAtmosphere atmosphere = HB_AtmosphereAmount(positionWS, cameraPositionWS);

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_FOG_AMOUNT)
        return half3(atmosphere.fog, atmosphere.horizon, 0.0h);
#endif

    UNITY_BRANCH
    if (atmosphere.fog + atmosphere.horizon > 0.0h)
    {
        half3 direction = half3(normalize(positionWS - cameraPositionWS));

        colour = lerp(colour, HB_SkySample(direction, direction.y), atmosphere.horizon);
        colour = lerp(colour, HB_FogColour(direction), atmosphere.fog);
    }

    return colour;
}

half3 HB_AtmosphereSky(half3 skyColour, half3 direction)
{
    half haze = HB_SkyHaze(direction);

    UNITY_BRANCH
    if (haze > 0.0h)
        skyColour = lerp(skyColour, HB_FogColour(direction), haze);

    return skyColour;
}

#endif
