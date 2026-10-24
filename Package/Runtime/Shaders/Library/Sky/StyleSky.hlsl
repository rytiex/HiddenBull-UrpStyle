#ifndef HB_STYLE_SKY_INCLUDED
#define HB_STYLE_SKY_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSkyLut.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"

#define HB_SKY_INTENSITY     _HB_Sky.x
#define HB_SKY_SUNSET_FOCUS  _HB_Sky.y
#define HB_SKY_BRUSH         _HB_Sky.z
#define HB_SKY_BRUSH_SMOOTH  _HB_Sky.w
#define HB_SKY_BRUSH_SCALE   _HB_SkyBrush.x
#define HB_SKY_PAINT         _HB_SkyBrush.y
#define HB_SKY_AWAY_ROW      _HB_SkyBrush.z
#define HB_SKY_TIME          _HB_SkyBrush.w
#define HB_SKY_ROW           _HB_Environment.y

#define HB_STAR_COLOUR       _HB_StarLook.x
#define HB_STAR_SIZE_SPREAD  _HB_StarLook.y
#define HB_AURORA_HEADING    _HB_StarLook.zw

#define HB_AURORA_INTENSITY  _HB_Aurora.x
#define HB_AURORA_SCALE      _HB_Aurora.y
#define HB_AURORA_SPEED      _HB_Aurora.z
#define HB_AURORA_HEIGHT     _HB_Aurora.w
#define HB_AURORA_DISTANCE   3.0
#define HB_AURORA_FOLD       0.6
#define HB_AURORA_LENGTH     5.0
#define HB_AURORA_HORIZON    0.02
#define HB_AURORA_COUNT      _HB_AuroraLow.w
#define HB_AURORA_SPREAD     0.9

#define HB_SKY_SUNSET_DELAY  0.35h
#define HB_SKY_BODY_MASK     0.005h
#define HB_SKY_GLOW_MASK     0.15h

half HB_SunFacing(half3 direction)
{
    float2 azimuth = _HB_SunDirection.xz;

    half toward = half(dot(direction.xz, azimuth * rsqrt(max(dot(azimuth, azimuth), 1e-6))));

    half swept = saturate((half(HB_SKY_TIME) - HB_SKY_SUNSET_DELAY) * rcp(1.0h - HB_SKY_SUNSET_DELAY));

    half front = lerp(-1.0h, 1.0h, swept);
    half width = lerp(2.2h, 0.85h, half(HB_SKY_SUNSET_FOCUS));

    half t = saturate((toward - front + width) * rcp(2.0h * width));

    return t * t * t * (t * (t * 6.0h - 15.0h) + 10.0h);
}

half3 HB_SkySample(half3 direction, half up)
{
    half3 toward = HB_SampleSkyLut(up, HB_SKY_ROW, HB_LUT_SKY);
    half3 away = HB_SampleSkyLut(up, HB_SKY_AWAY_ROW, HB_LUT_SKY);

    return lerp(away, toward, HB_SunFacing(direction)) * half(HB_SKY_INTENSITY);
}

float HB_SkyBrushTurns(float scale)
{
    return max(round(scale * 4.0), 1.0);
}

float2 HB_SkyBrushUV(half3 direction, float scale)
{
    float azimuth = atan2(direction.z, direction.x) * (0.5 / PI);

    return float2(azimuth * HB_SkyBrushTurns(scale), direction.y * scale);
}

half4 HB_SkyStroke(half3 direction)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas,
                                HB_SkyBrushUV(direction, max(HB_SKY_BRUSH_SCALE, 1e-3)), HB_SKY_BRUSH_SMOOTH * 4.0);
}

half3 HB_SkyPaintDirection(half3 direction, half4 stroke, half reach, out half lift)
{
    float scale = max(HB_SKY_BRUSH_SCALE, 1e-3);
    float2 spine = float2(stroke.rg * 2.0h - 1.0h) * (HB_BRUSH_SPINE_RANGE * reach);

    float sine, cosine;
    sincos(spine.x * (TWO_PI / HB_SkyBrushTurns(scale)), sine, cosine);

    lift = half(spine.y / scale);

    return half3(direction.x * cosine - direction.z * sine, direction.y, direction.x * sine + direction.z * cosine);
}

half3 HB_SkyGradient(half3 direction)
{
    half up = direction.y;
    half brush = half(HB_SKY_BRUSH);
    half paint = half(HB_SKY_PAINT);
    half mask = smoothstep(0.0h, 0.2h, up) * (1.0h - smoothstep(0.85h, 1.0h, up));

    half3 colour;

    UNITY_BRANCH
    if ((brush <= 0.0h && paint <= 0.0h) || HB_BRUSH_ATLAS_BOUND <= 0.5h || mask <= 0.0h)
    {
        colour = HB_SkySample(direction, up);
    }
    else
    {
        half4 atlas = HB_SkyStroke(direction);
        half stroke = (atlas.b * 2.0h - 1.0h) * mask;

        half lift;
        half3 painted = HB_SkyPaintDirection(direction, atlas, paint * mask, lift);

        colour = HB_SkySample(painted, up + lift + stroke * brush * 0.32h) * (1.0h + stroke * brush * 0.5h);
    }

    return colour;
}

float2 HB_CelestialPlane(float3 direction, float3 axis)
{
    float3 reference = abs(axis.y) > 0.99 ? float3(0.0, 0.0, 1.0) : float3(0.0, 1.0, 0.0);
    float3 right = normalize(cross(axis, reference));
    float3 up = cross(right, axis);

    return float2(dot(direction, right), dot(direction, up));
}

half3 HB_CelestialBody(float3 direction, float3 axis, float4 disc, float4 glow, float4 colour, half bodyMask,
                       half glowMask)
{
    half cosAngle = half(saturate(dot(direction, axis)));

    half halo = PositivePow(cosAngle, max(half(glow.x), half(HB_EPSILON))) * half(glow.y);

    half rayStrength = half(glow.z) * bodyMask;

    if (rayStrength > 0.0h)
    {
        float2 rayPlane = HB_CelestialPlane(direction, axis);
        half angle = half(atan2(rayPlane.y, rayPlane.x));
        half rays = sin(angle * 12.0h) * 0.5h + 0.5h;
        halo *= 1.0h + rays * rays * rayStrength;
    }

    half body = 0.0h;

    if (bodyMask > 0.0h && cosAngle > half(disc.x) - half(disc.y) * 2.0h)
    {
        half edgeOffset = 0.0h;

        if (disc.z > 0.0h && HB_BRUSH_ATLAS_BOUND > 0.5h)
        {
            float2 plane = HB_CelestialPlane(direction, axis);

            half4 atlas = SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas, plane * 6.0, 0);
            edgeOffset = (atlas.b * 2.0h - 1.0h) * half(disc.z);
        }

        half threshold = half(disc.x) + edgeOffset * (1.0h - half(disc.x));
        half softness = max(half(disc.y), half(HB_EPSILON));

        body = smoothstep(threshold - softness, threshold + softness, cosAngle);
    }

    return colour.rgb * colour.a * (body * bodyMask + halo * glowMask);
}

float HB_StarHash(float3 cell)
{
    float3 p = frac(cell * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.x + p.y) * p.z);
}

half3 HB_Stars(float3 direction)
{
    half brightness = half(_HB_Stars.y);

    float3 scaled = direction * _HB_Stars.x;
    float3 cell = floor(scaled);

    float presence = HB_StarHash(cell);

    half3 stars = half3(0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (brightness > 0.0h && presence >= _HB_Stars.w)
    {
        float3 offset = float3(HB_StarHash(cell + 11.0), HB_StarHash(cell + 23.0), HB_StarHash(cell + 37.0));
        float size = lerp(1.0, 0.5 + HB_StarHash(cell + 51.0) * 1.5, HB_STAR_SIZE_SPREAD);

        float toCentre = length(scaled - (cell + 0.25 + offset * 0.5));
        half star = half(saturate(1.0 - toCentre * 6.0 / size));
        star = star * star * star;

        half twinkle = 1.0h - half(_HB_Stars.z) * (0.5h + 0.5h * half(sin(_Time.y * 3.0 + presence * 40.0)));

        half temperature = half(HB_StarHash(cell + 71.0));
        half3 tint = lerp(half3(0.70h, 0.80h, 1.00h), half3(1.00h, 0.78h, 0.55h), temperature);

        stars = lerp(half3(1.0h, 1.0h, 1.0h), tint, half(HB_STAR_COLOUR)) * (star * brightness * twinkle);
    }

    return stars;
}

half3 HB_AuroraColour(half t)
{
    return t < 0.5h ? lerp(half3(_HB_AuroraLow.rgb), half3(_HB_AuroraMid.rgb), t * 2.0h)
                    : lerp(half3(_HB_AuroraMid.rgb), half3(_HB_AuroraHigh.rgb), t * 2.0h - 1.0h);
}

half3 HB_AuroraCurtain(float3 direction, float2 heading, float distance, float phase, float scale, float drift)
{
    half3 glow = half3(0.0h, 0.0h, 0.0h);

    float toward = direction.x * heading.x + direction.z * heading.y;

    UNITY_BRANCH
    if (toward > 0.05)
    {
        float across = direction.x * heading.y - direction.z * heading.x;
        float fold = HB_AURORA_FOLD * scale;

        float travel = distance / toward;
        float x = across * travel;

        UNITY_UNROLL
        for (int i = 0; i < 2; i++)
        {
            float bend = fold * (sin(x * 0.8 / scale + drift * 0.2 + phase)
                               + 0.5 * HB_BrushField(float2(x * 0.3 / scale + phase, drift * 0.05)));

            travel = (distance + bend) / toward;
            x = across * travel;
        }

        float height = direction.y * travel;
        float bottom = 1.0 + 0.15 * HB_BrushField(float2(x * 1.5 / scale + phase, drift * 0.1));
        half v = half((height - bottom) / max(HB_AURORA_HEIGHT, 1e-3));

        half body = smoothstep(-0.05h, 0.06h, v) * saturate(1.0h - v) * saturate(1.0h - v);
        half along = half(x / (HB_AURORA_LENGTH * scale));
        half span = exp(-along * along);
        half rays = half(0.6 + 0.4 * HB_BrushField(float2(x * 6.0 / scale + phase, drift * 0.1)));

        glow = HB_AuroraColour(saturate(v)) * (body * span * rays);
    }

    return glow;
}

half3 HB_Aurora(float3 direction)
{
    half3 glow = half3(0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (HB_AURORA_INTENSITY > 0.0 && direction.y > HB_AURORA_HORIZON)
    {
        float scale = HB_AURORA_SCALE;
        float drift = _Time.y * HB_AURORA_SPEED;
        float2 heading = HB_AURORA_HEADING;
        int count = (int)HB_AURORA_COUNT;
        float centre = (count - 1) * 0.5;

        UNITY_LOOP
        for (int i = 0; i < count; i++)
        {
            float s, c;
            sincos((i - centre) * HB_AURORA_SPREAD, s, c);

            float2 turned = float2(heading.x * c + heading.y * s, heading.y * c - heading.x * s);
            float distance = HB_AURORA_DISTANCE * (1.0 + 0.35 * frac(i * 0.618));

            glow += HB_AuroraCurtain(direction, turned, distance, i * 2.4, scale, drift);
        }

        glow *= half(HB_AURORA_INTENSITY) * smoothstep(half(HB_AURORA_HORIZON), 0.2h, half(direction.y));
    }

    return glow;
}

half3 HB_SkyColour(half3 direction)
{
    half3 sky = HB_SkyGradient(direction);

    half glowMask = smoothstep(-HB_SKY_GLOW_MASK, 0.0h, direction.y);

    UNITY_BRANCH
    if (glowMask > 0.0h)
    {
        half bodyMask = smoothstep(-HB_SKY_BODY_MASK, HB_SKY_BODY_MASK, direction.y);

        half3 celestial = (HB_Stars(direction) + HB_Aurora(direction)) * bodyMask;

        if (_HB_SunColor.a > 0.0)
        {
            celestial += HB_CelestialBody(direction, _HB_SunDirection.xyz, _HB_SunDisc, _HB_SunGlow, _HB_SunColor,
                                          bodyMask, glowMask);
        }

        if (_HB_MoonColor.a > 0.0)
        {
            celestial += HB_CelestialBody(direction, -_HB_SunDirection.xyz, _HB_MoonDisc, _HB_MoonGlow,
                                          _HB_MoonColor, bodyMask, glowMask);
        }

        sky += celestial;
    }

    return sky;
}

#endif
