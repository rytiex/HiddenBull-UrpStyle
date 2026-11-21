#ifndef HB_STYLE_ATMOSPHERE_INCLUDED
#define HB_STYLE_ATMOSPHERE_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSky.hlsl"

#define HB_FOG_DENSITY          _HB_Fog.x
#define HB_FOG_FADE_RATE        _HB_Fog.y
#define HB_FOG_LAYER_TOP        _HB_Fog.z
#define HB_FOG_MAX_OPACITY      _HB_Fog.w
#define HB_FOG_SCATTERING       _HB_FogScatter.x
#define HB_FOG_START            _HB_FogScatter.y
#define HB_FOG_PAINT            _HB_FogPaint.x
#define HB_FOG_SILHOUETTE       _HB_FogPaint.y
#define HB_FOG_CLUMPS           _HB_FogClumps.x
#define HB_FOG_CLUMP_RATE       _HB_FogClumps.y
#define HB_FOG_DRIFT            _HB_FogClumps.zw
#define HB_FOG_OVERCAST_GREY    _HB_FogScatter.z
#define HB_FOG_AIR_HAZE         _HB_FogPaint.z
#define HB_FOG_LAYER_COVER      _HB_FogPaint.w

#define HB_FOG_GLOW_SHARE       0.35h
#define HB_FOG_CORE_POWER       12.0h
#define HB_FOG_SILHOUETTE_TILES 8.0
#define HB_FOG_CLUMP_FLAT       3.0
#define HB_FOG_CLUMP_REACH      8.0
#define HB_FOG_CLUMP_SAMPLES    3
#define HB_FOG_CLUMP_CEILING    2.0
#define HB_FOG_AIR_HEIGHT       1000.0
#define HB_FOG_NOISE_TILE       float3(1.0 / 16.0, 1.0 / 8.0, 1.0 / 16.0)

#define HB_DEBUG_FOG_AMOUNT     7

float HB_FogFallShare(float fall)
{
    return fall < 1e-3 ? 1.0 - 0.5 * fall : (1.0 - exp(-fall)) / fall;
}

float HB_FogOpticalDepth(float3 origin, float3 direction, float travelled)
{
    float density = HB_FOG_DENSITY;
    float rate = HB_FOG_FADE_RATE;
    float depth = density * travelled;

    UNITY_BRANCH
    if (rate > 0.0)
    {
        float start = origin.y - HB_FOG_LAYER_TOP;
        float end = start + direction.y * travelled;
        float lowest = min(start, end);
        float highest = max(start, end);
        float span = highest - lowest;

        if (span < 1e-3)
        {
            float middle = 0.5 * (start + end);
            float height = max(middle, 0.0);
            float air = middle > 0.0 ? HB_FOG_AIR_HAZE * exp(-height / HB_FOG_AIR_HEIGHT) : 0.0;

            depth = travelled * (density * exp(-rate * height) + air);
        }
        else
        {
            float below = saturate(-lowest / span);
            float low = max(lowest, 0.0);
            float high = max(highest, 0.0);
            float share = (high - low) / span;
            float fade = exp(-rate * low) * HB_FogFallShare(rate * (high - low));
            float air = exp(-low / HB_FOG_AIR_HEIGHT) * HB_FogFallShare((high - low) / HB_FOG_AIR_HEIGHT);

            depth = travelled * (density * (below + share * fade) + HB_FOG_AIR_HAZE * share * air);
        }
    }

    return depth;
}

float HB_FogCoverDepth(float cameraHeight, float endHeight, float travelled)
{
    float rate = HB_FOG_FADE_RATE;
    float lift = saturate((cameraHeight - HB_FOG_LAYER_TOP) * rate);
    float sink = saturate(((HB_FOG_LAYER_TOP - endHeight) * rate + 0.5) / 1.5);

    return rate > 0.0 ? HB_FOG_LAYER_COVER * HB_FOG_DENSITY * travelled * lift * sink * sink : 0.0;
}

TEXTURE3D(_HB_FogNoise);
SAMPLER(sampler_HB_FogNoise);

half HB_FogClump(float3 positionWS)
{
    float3 position = float3(positionWS.x - HB_FOG_DRIFT.x, positionWS.y * HB_FOG_CLUMP_FLAT,
                             positionWS.z - HB_FOG_DRIFT.y) * HB_FOG_CLUMP_RATE;

    return SAMPLE_TEXTURE3D_LOD(_HB_FogNoise, sampler_HB_FogNoise, position * HB_FOG_NOISE_TILE, 0).r * 2.0h;
}

half HB_FogAmountClumped(float3 positionWS, float3 cameraPositionWS, out half clump)
{
    float3 ray = positionWS - cameraPositionWS;
    float rayLength = length(ray);
    float travelled = rayLength - HB_FOG_START;

    half amount = 0.0h;
    clump = 1.0h;

    UNITY_BRANCH
    if (HB_FOG_DENSITY > 0.0 && travelled > 0.0)
    {
        float3 direction = ray / max(rayLength, 1e-4);
        float3 origin = cameraPositionWS + direction * HB_FOG_START;
        float depth = HB_FogOpticalDepth(origin, direction, travelled);
        float clumps = HB_FOG_CLUMPS;

        float enter = 0.0;
        float leave = travelled;
        float rate = HB_FOG_FADE_RATE;

        if (rate > 0.0)
        {
            float above = origin.y - HB_FOG_LAYER_TOP - HB_FOG_CLUMP_CEILING / rate;

            if (abs(direction.y) > 1e-5)
            {
                float cross = -above / direction.y;

                if (direction.y > 0.0)
                    leave = min(leave, cross);
                else
                    enter = max(enter, cross);
            }
            else if (above > 0.0)
            {
                leave = 0.0;
            }
        }

        float span = leave - enter;

        UNITY_BRANCH
        if (clumps > 0.0 && span > 0.0)
        {
            float reach = min(span, HB_FOG_CLUMP_REACH / max(HB_FOG_CLUMP_RATE, 1e-4));
            half sum = 0.0h;

            UNITY_UNROLL
            for (int i = 0; i < HB_FOG_CLUMP_SAMPLES; i++)
                sum += HB_FogClump(origin + direction * (enter + reach * ((float(i) + 0.5) / HB_FOG_CLUMP_SAMPLES)));

            clump = half(lerp(1.0, float(sum) / HB_FOG_CLUMP_SAMPLES, clumps * reach / span));
            depth *= clump;
        }

        depth += HB_FogCoverDepth(cameraPositionWS.y, positionWS.y, travelled);

        amount = half(saturate(1.0 - exp(-depth)) * HB_FOG_MAX_OPACITY);
    }

    return amount;
}

half HB_FogAmount(float3 positionWS, float3 cameraPositionWS)
{
    half clump;

    return HB_FogAmountClumped(positionWS, cameraPositionWS, clump);
}

half HB_FogAmountEven(float3 positionWS, float3 cameraPositionWS)
{
    float3 ray = positionWS - cameraPositionWS;
    float rayLength = length(ray);
    float travelled = rayLength - HB_FOG_START;

    half amount = 0.0h;

    UNITY_BRANCH
    if (HB_FOG_DENSITY > 0.0 && travelled > 0.0)
    {
        float3 direction = ray / max(rayLength, 1e-4);
        float depth = HB_FogOpticalDepth(cameraPositionWS + direction * HB_FOG_START, direction, travelled);

        depth += HB_FogCoverDepth(cameraPositionWS.y, positionWS.y, travelled);

        amount = half(saturate(1.0 - exp(-depth)) * HB_FOG_MAX_OPACITY);
    }

    return amount;
}

half HB_SkyHaze(half3 direction)
{
    half haze = 0.0h;

    UNITY_BRANCH
    if (HB_FOG_DENSITY > 0.0)
    {
        float rate = HB_FOG_FADE_RATE;
        float rise = direction.y;
        float clearSight = 0.0;

        if (rate > 0.0 && rise > 1e-3)
        {
            float start = GetCameraPositionWS().y - HB_FOG_LAYER_TOP;
            float below = max(-start, 0.0) / rise;
            float lift = max(start, 0.0);
            float fade = exp(-rate * lift) / rate;
            float air = exp(-lift / HB_FOG_AIR_HEIGHT) * HB_FOG_AIR_HEIGHT;
            float depth = HB_FOG_DENSITY * (below + fade / rise) + HB_FOG_AIR_HAZE * air / rise;

            clearSight = exp(-min(depth, 40.0));
        }

        haze = half((1.0 - clearSight) * HB_FOG_MAX_OPACITY);
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

half HB_FogSunShare(half3 direction)
{
    half scattering = half(HB_FOG_SCATTERING);
    half share = 0.0h;

    UNITY_BRANCH
    if (scattering > 0.0h)
    {
        half towardSun = half(saturate(dot(float3(direction), _HB_SunDirection.xyz)));
        half glow = towardSun * towardSun;
        half core = PositivePow(towardSun, HB_FOG_CORE_POWER);

        share = saturate(lerp(core, glow, HB_FOG_GLOW_SHARE) * scattering);
    }

    return share;
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

    colour = lerp(colour, half3(_HB_SunColor.rgb) * tone, HB_FogSunShare(direction));

    colour = lerp(colour, dot(colour, half3(0.2126h, 0.7152h, 0.0722h)).xxx, half(HB_FOG_OVERCAST_GREY));

    return colour * half(HB_OVERCAST_DIM);
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
    half clump;
    half fog = HB_FogAmountClumped(positionWS, cameraPositionWS, clump);
    half3 direction = half3(normalize(positionWS - cameraPositionWS));

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_FOG_AMOUNT)
        return half3(fog, HB_FogSunShare(direction), clump * 0.5h);
#endif

    UNITY_BRANCH
    if (fog > 0.0h)
        colour = lerp(colour, HB_FogColour(direction), fog);

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
