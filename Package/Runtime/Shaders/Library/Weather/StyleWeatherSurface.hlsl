#ifndef HB_STYLE_WEATHER_SURFACE_INCLUDED
#define HB_STYLE_WEATHER_SURFACE_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleShelter.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleTones.hlsl"

#define HB_WEATHER_CONTRAST     2.0h
#define HB_BODY_TRACKED         0x80000000u
#define HB_BODY_LEVELS          127u
#define HB_BODY_UNIT            (1.0 / 127.0)
#define HB_BODY_SNOW_SHIFT      7u
#define HB_BODY_CLOCK_SHIFT     14u
#define HB_BODY_CLOCK_MASK      0x1FFFFu
#define HB_BODY_CLOCK_RATE      32.0
#define HB_FLAKE_UP             2.0h

#define HB_BEAD_JITTER          0.4
#define HB_BEAD_RADIUS          0.3
#define HB_BEAD_SOFT            0.06
#define HB_BEAD_STEEPEST        0.35
#define HB_DROP_BEND            0.4
#define HB_FLAKE_BEND           0.2
#define HB_WEATHER_DETAIL_FADE  5.0

#define HB_WET_DARKEN           0.2h
#define HB_GROUND_WET_DARKEN    0.3h
#define HB_WET_FLAT_SMOOTHNESS  0.92h
#define HB_WET_WALL_SMOOTHNESS  0.6h

#define HB_SNOW_ALBEDO          half3(0.5h, 0.54h, 0.6h)
#define HB_SNOW_SMOOTHNESS      0.2h
#define HB_SNOW_FULL_SLOPE      0.407h
#define HB_SNOW_NO_SLOPE        0.174h
#define HB_SNOW_SLOPE_EDGE      0.1h
#define HB_SNOW_SLOPE_BRUSH     0.6h
#define HB_SNOW_SOFTEN          0.35h
#define HB_SNOW_SPREAD          0.6h
#define HB_SNOW_FADE_IN         3.0h
#define HB_SNOW_STEEP_CALM      1.4h
#define HB_SNOW_PATCH_DELAY     0.5h
#define HB_SNOW_EDGE_DELAY      0.35h
#define HB_SNOW_DROP_DELAY      0.3h
#define HB_SNOW_BRUSH_DELAY     0.15h
#define HB_SNOW_EDGE_WIDTH      0.2
#define HB_SNOW_FLAT_OBJECT     0.1

#define HB_WEATHER_EDGE_BRUSH   0.35h
#define HB_PATCH_SCALE          3.1
#define HB_PATCH_DETAIL_SCALE   0.9
#define HB_NOISE_REACH          0.75
#define HB_NOISE_STRETCH        1.5

#define HB_WET_SPREAD           0.5h
#define HB_WET_PATCH_DELAY      0.6h
#define HB_WET_EDGE_DELAY       0.4h
#define HB_WET_STEEP            0.966h
#define HB_WET_STEEP_EDGE       0.026h
#define HB_WET_SPOT_RAIN        10.0h

#define HB_SPOT_CELL            0.15
#define HB_SPOT_JITTER          0.9
#define HB_SPOT_RADIUS          1.4h
#define HB_SPOT_SOFT            0.08h
#define HB_SPOT_RAGGED          0.12h
#define HB_SPOT_BRUSH           0.08h
#define HB_SPOT_ONSET           0.08h
#define HB_SPOT_GROW            0.45h
#define HB_SPOT_GROWN           0.53h
#define HB_SPOT_FILM_RANGE      0.05h
#define HB_SPOT_AVERAGE         1.9h
#define HB_SPOT_STEEPEST        0.342h
#define HB_SPOT_STEEP_EDGE      0.1h

#define HB_FACING_DOWN          0.5h
#define HB_FACING_EDGE          0.4h

#define HB_FLOW_STEEP           0.7h
#define HB_FLOW_STEEP_EDGE      0.2h
#define HB_FLOW_SPACING         0.05
#define HB_FLOW_DENSITY         0.5h
#define HB_FLOW_JITTER          0.3
#define HB_FLOW_WIDTH           0.3
#define HB_FLOW_SPEED           0.6
#define HB_FLOW_SEGMENT         0.8
#define HB_FLOW_HEAD            0.04
#define HB_FLOW_TRAIL           0.45
#define HB_FLOW_TRAIL_WET       0.5h
#define HB_FLOW_BEND            0.5h
#define HB_FLOW_DARKEN          0.25h
#define HB_FLOW_SMOOTHNESS      0.95h

#define HB_RIPPLE_CELL          0.35
#define HB_RIPPLE_SPEED         1.3
#define HB_RIPPLE_JITTER        0.5
#define HB_RIPPLE_REACH         0.35
#define HB_RIPPLE_WIDTH         0.05
#define HB_RIPPLE_WAVE          0.08
#define HB_RIPPLE_STRENGTH      0.6h

struct HBWeather
{
    half wet;
    half snow;
    half rain;
    half flow;
    half3 flowBend;
};

struct HBWeatherMaterial
{
    half wetness;
    half snowCover;
    half4 beads;
    half2 beadLife;
};

float HB_WeatherHash(float3 cell)
{
    uint3 q = uint3(int3(cell));
    uint n = (q.x * 73856093u) ^ (q.y * 19349663u) ^ (q.z * 83492791u);
    n = (n ^ (n >> 13u)) * 1274126177u;

    return float((n ^ (n >> 16u)) & 0xFFFFFFu) * (1.0 / 16777215.0);
}

float HB_WeatherNoise(float3 x)
{
    float3 s = floor(x + dot(x, 1.0 / 3.0));
    float3 x0 = x - s + dot(s, 1.0 / 6.0);
    float3 e = step(x0.yzx, x0.xyz);
    float3 i1 = e * (1.0 - e.zxy);
    float3 i2 = 1.0 - e.zxy * (1.0 - e);
    float3 x1 = x0 - i1 + 1.0 / 6.0;
    float3 x2 = x0 - i2 + 1.0 / 3.0;
    float3 x3 = x0 - 0.5;

    float4 w = max(HB_NOISE_REACH - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    w *= w;
    w *= w;

    float4 h = float4(HB_WeatherHash(s), HB_WeatherHash(s + i1), HB_WeatherHash(s + i2), HB_WeatherHash(s + 1.0));

    return dot(w, h) / max(dot(w, 1.0), 1e-6);
}

half HB_WeatherPatches(float3 positionWS)
{
    return half(0.65 * HB_WeatherNoise(positionWS / (HB_PATCH_SCALE * HB_NOISE_STRETCH))
              + 0.35 * HB_WeatherNoise(positionWS / (HB_PATCH_DETAIL_SCALE * HB_NOISE_STRETCH) + 17.0));
}

half HB_WeatherDetail(float3 positionWS)
{
    return half(saturate((HB_WEATHER_DETAIL - HB_ViewDepth(positionWS)) / HB_WEATHER_DETAIL_FADE));
}

half HB_RainSpots(float2 xz, half3 normalWS, half wet, half brushCoverage)
{
    float2 p = xz / HB_SPOT_CELL;
    float2 rise = float2(normalWS.xz) / max(float(normalWS.y), HB_SPOT_STEEPEST);

    half ragged = half(HB_BrushField(p * 1.7 + 29.0)) * HB_SPOT_RAGGED + brushCoverage * HB_SPOT_BRUSH;
    float2 home = floor(p);
    half dots = 0.0h;

    UNITY_UNROLL
    for (int z = -1; z <= 1; z++)
    {
        UNITY_UNROLL
        for (int x = -1; x <= 1; x++)
        {
            float2 cell = home + float2(x, z);
            float3 hash = HB_BrushHash(cell + 613.0);
            float2 centre = cell + 0.5 + (hash.xy - 0.5) * HB_SPOT_JITTER;

            half grow = saturate((wet - half(hash.z) * HB_SPOT_ONSET) / HB_SPOT_GROW);
            half size = grow * (HB_SPOT_RADIUS * (0.75h + half(frac(hash.z * 7.31)) * 0.25h) + ragged);

            float2 away = p - centre;
            float along = dot(away, rise);

            dots = max(dots, saturate((size - half(sqrt(dot(away, away) + along * along))) / HB_SPOT_SOFT));
        }
    }

    return dots;
}

half HB_SpottedWet(float2 xz, half3 normalWS, half wet, half brushCoverage, float footprint, half detail)
{
    half average = saturate(wet * HB_SPOT_AVERAGE);
    half spotted = average;

    UNITY_BRANCH
    if (detail > 0.0h && wet < HB_SPOT_GROWN + HB_SPOT_FILM_RANGE)
    {
        half blur = half(saturate(footprint / HB_SPOT_CELL * 2.0 - 1.0));

        spotted = lerp(average, lerp(HB_RainSpots(xz, normalWS, wet, brushCoverage), average, blur), detail);
    }

    return max(spotted, saturate((wet - HB_SPOT_GROWN) / HB_SPOT_FILM_RANGE));
}

half HB_RainFlowColumns(float u, float height, float seed, half amount, out half bend)
{
    u /= HB_FLOW_SPACING;
    float column = floor(u);

    float3 hash = HB_BrushHash(float2(column, seed));
    float3 pick = HB_BrushHash(float2(column, seed + 404.0));

    float across = (u - column - 0.5 - (hash.x - 0.5) * HB_FLOW_JITTER) / HB_FLOW_WIDTH;
    float speed = HB_FLOW_SPEED * (0.6 + hash.y * 0.8);
    float along = frac((height + HB_SURFACE_CLOCK * speed) / HB_FLOW_SEGMENT + hash.z);

    half trail = along < HB_FLOW_HEAD
        ? 1.0h
        : half(saturate(1.0 - (along - HB_FLOW_HEAD) / HB_FLOW_TRAIL)) * HB_FLOW_TRAIL_WET;
    half strength = trail * (abs(across) < 1.0 ? 1.0h : 0.0h) * (pick.x <= amount * HB_FLOW_DENSITY ? 1.0h : 0.0h);

    bend = half(across) * strength;

    return saturate(1.0h - half(abs(across))) * strength;
}

half HB_RainFlow(float3 positionWS, half3 normalWS, half amount, float footprint, out half3 bend)
{
    half2 weights = normalWS.xz * normalWS.xz;
    weights *= weights;
    weights /= max(weights.x + weights.y, 1e-4h);

    half bendX;
    half bendZ;
    half flowX = HB_RainFlowColumns(positionWS.z, positionWS.y, 419.0, amount, bendX);
    half flowZ = HB_RainFlowColumns(positionWS.x, positionWS.y, 587.0, amount, bendZ);

    half fade = half(saturate((HB_FLOW_SPACING - footprint) / (HB_FLOW_SPACING * 0.5)));

    bend = half3(bendZ * weights.y, 0.0h, bendX * weights.x) * (HB_FLOW_BEND * fade);

    return (flowX * weights.x + flowZ * weights.y) * fade;
}

HBWeather HB_ResolveSurfaceWeather(float3 positionWS, half3 normalWS, half brushCoverage, half wetLimit,
                                   half snowLimit, bool ground, half groundHeight)
{
    HBWeather weather;
    weather.wet = 0.0h;
    weather.snow = 0.0h;
    weather.rain = 0.0h;
    weather.flow = 0.0h;
    weather.flowBend = half3(0.0h, 0.0h, 0.0h);

    half wet = half(HB_SURFACE_WET) * (wetLimit > 0.0h ? 1.0h : 0.0h);
    half cover = half(HB_SURFACE_SNOW) * snowLimit;

    UNITY_BRANCH
    if (wet > 0.0h || cover > 0.0h)
    {
        float3 boundsMin = ground ? positionWS - 1e3 : unity_RendererBounds_Min.xyz;
        float3 boundsMax = ground ? positionWS + 1e3 : unity_RendererBounds_Max.xyz;
        float3 size = max(boundsMax - boundsMin, 1e-3);
        float2 toEdge = min(positionWS.xz - boundsMin.xz, boundsMax.xz - positionWS.xz);
        float3 positionDx = ddx(positionWS);
        float3 positionDy = ddy(positionWS);
        float footprint = length(abs(positionDx) + abs(positionDy));

        half edgeBrush = 0.5h + brushCoverage * HB_WEATHER_EDGE_BRUSH;
        half facing = saturate((normalWS.y + HB_FACING_DOWN) / HB_FACING_EDGE);
        half open = saturate((HB_ShelterOpen(positionWS, normalWS) - 0.5h) * HB_WEATHER_CONTRAST + edgeBrush)
                  * facing;
        half patches = HB_WeatherPatches(positionWS);
        half edge = half(1.0 - saturate(min(toEdge.x, toEdge.y) / HB_SNOW_EDGE_WIDTH));
        half height = ground ? groundHeight
                    : size.y > HB_SNOW_FLAT_OBJECT ? half(saturate((positionWS.y - boundsMin.y) / size.y)) : 1.0h;
        half detail = HB_WeatherDetail(positionWS);

        UNITY_BRANCH
        if (cover > 0.0h)
        {
            half steep = saturate(1.0h - normalWS.y);
            half scatter = saturate(1.0h - steep * HB_SNOW_STEEP_CALM);
            half delay = saturate(patches * HB_SNOW_PATCH_DELAY + edge * HB_SNOW_EDGE_DELAY
                                - brushCoverage * HB_SNOW_BRUSH_DELAY * scatter
                                + (1.0h - height) * HB_SNOW_DROP_DELAY);
            half settled = saturate((cover - delay * HB_SNOW_SPREAD) / (1.0h - HB_SNOW_SPREAD));

            half slope = saturate((normalWS.y - HB_SNOW_NO_SLOPE) / (HB_SNOW_FULL_SLOPE - HB_SNOW_NO_SLOPE));
            half pattern = saturate(0.5h + brushCoverage * 0.5h) * HB_SNOW_SLOPE_BRUSH
                         + patches * (1.0h - HB_SNOW_SLOPE_BRUSH);
            half slopeSnow = saturate((slope - (1.0h - pattern) * (1.0h - HB_SNOW_SLOPE_EDGE)) / HB_SNOW_SLOPE_EDGE);

            weather.snow = slopeSnow * saturate(settled * HB_SNOW_FADE_IN) * open;
        }

        half rain = half(HB_SURFACE_RAIN);
        half raining = saturate(rain * HB_WET_SPOT_RAIN);
        half steepWet = saturate((HB_WET_STEEP - normalWS.y) / HB_WET_STEEP_EDGE);
        half flat = saturate((wet - saturate(patches * HB_WET_PATCH_DELAY + edge * HB_WET_EDGE_DELAY) * HB_WET_SPREAD)
                             / (1.0h - HB_WET_SPREAD));

        half drained = saturate((wet - patches * HB_WET_PATCH_DELAY * HB_WET_SPREAD) / (1.0h - HB_WET_SPREAD));
        half spotReach = raining * saturate((normalWS.y - HB_SPOT_STEEPEST) / HB_SPOT_STEEP_EDGE);
        half soaked = lerp(flat, drained, steepWet);

        UNITY_BRANCH
        if (!ground && spotReach > 0.0h)
        {
            half spotted = HB_SpottedWet(positionWS.xz, normalWS, lerp(wet, drained, steepWet), brushCoverage,
                                         footprint, detail);

            soaked = lerp(soaked, spotted, spotReach);
        }

        half reach = open * (1.0h - weather.snow);

        weather.wet = soaked * reach * wetLimit;
        weather.rain = rain * reach * detail;

        half steepFlow = saturate((HB_FLOW_STEEP - normalWS.y) / HB_FLOW_STEEP_EDGE);

        UNITY_BRANCH
        if (!ground && weather.rain > 0.0h && steepFlow > 0.0h && footprint < HB_FLOW_SPACING)
        {
            half3 bend;
            half flow = HB_RainFlow(positionWS, normalWS, weather.rain, footprint, bend);

            weather.flow = flow * steepFlow * wetLimit;
            weather.flowBend = bend * steepFlow;
        }
    }

    return weather;
}

float3 HB_BeadSpace(float3 positionWS, float3 anchorOS)
{
#if defined(_HB_BRUSH_ANCHOR)
    return any(anchorOS != 0.0) ? anchorOS * HB_ObjectScale() : HB_ObjectSpacePosition(positionWS);
#else
    return HB_ObjectSpacePosition(positionWS);
#endif
}

float3 HB_BeadNormal(float3 spaceDx, float3 spaceDy)
{
    float3 crossed = cross(spaceDx, spaceDy);
    float length2 = dot(crossed, crossed);

    return length2 > 1e-20 ? crossed * rsqrt(length2) : float3(0.0, 1.0, 0.0);
}

half HB_Beads(float3 space, float3 normal, half dropShare, half flakeShare, float cellSize, float2 scale,
              float2 life, float clock, float footprint, out float3 slope, out half flake)
{
    float3 q = space / cellSize;
    float3 home = floor(q - 0.5);

    half flakeRatio = flakeShare / max(dropShare + flakeShare, 1e-4h);
    half bead = 0.0h;
    float3 offset = float3(0.0, 0.0, 0.0);

    flake = 0.0h;

    UNITY_UNROLL
    for (int i = 0; i < 8; i++)
    {
        float3 cell = home + float3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        float3 timing = HB_BrushHash(cell.xy + cell.z * 97.0 + 990.0);
        half flakeCell = half(timing.z) < flakeRatio ? 1.0h : 0.0h;

        float cycle = clock / (lerp(life.x, life.y, flakeCell) * (0.6 + timing.x * 0.8)) + timing.y;
        float age = frac(cycle);
        float2 generation = floor(cycle) * float2(13.0, 29.0);

        float3 place = HB_BrushHash(cell.xy + cell.z * 157.0 + 71.0 + generation);
        float3 kind = HB_BrushHash(cell.xy + cell.z * 311.0 + 523.0 + generation);

        float3 jitter = (place - 0.5) * HB_BEAD_JITTER;
        float3 centre = cell + 0.5 + jitter - dot(jitter, normal) * normal;
        float radius = HB_BEAD_RADIUS * lerp(scale.x, scale.y, flakeCell) * (0.6 + kind.y * 0.4) * (1.0 - age * age);
        float3 away = q - centre;
        float depth = dot(away, normal);
        half layer = half(saturate(1.0 - abs(depth)));

        away -= depth * normal;

        half cover = half(saturate((radius - length(away)) / HB_BEAD_SOFT))
                   * (half(kind.x) < lerp(dropShare, flakeShare, flakeCell) * layer ? 1.0h : 0.0h);

        if (cover > bead)
        {
            bead = cover;
            offset = away / radius;
            flake = flakeCell;
        }
    }

    float reach = cellSize * HB_BEAD_RADIUS;

    bead *= half(saturate((reach * 2.0 - footprint) / reach));
    slope = -offset / max(sqrt(saturate(1.0 - dot(offset, offset))), HB_BEAD_STEEPEST) * bead;

    return bead;
}

half3 HB_BeadBend(float3 slope, float3 spaceDx, float3 spaceDy, float3 positionDx, float3 positionDy,
                  half3 normalWS)
{
    float3 normal = float3(normalWS);
    float3 acrossDy = cross(positionDy, normal);
    float3 acrossDx = cross(normal, positionDx);
    float determinant = dot(positionDx, acrossDy);

    float3 gradient = (dot(slope, spaceDx) * acrossDy + dot(slope, spaceDy) * acrossDx)
                    * (sign(determinant) / max(abs(determinant), 1e-12));

    return half3(-gradient);
}

HBWeather HB_ResolveMovingWeather(float3 positionWS, half3 normalWS, float3 anchorOS, HBWeatherMaterial material,
                                  half wetAmount, half snowAmount, float clock)
{
    HBWeather weather;
    weather.rain = 0.0h;
    weather.flowBend = half3(0.0h, 0.0h, 0.0h);

    half up = saturate(normalWS.y * HB_FLAKE_UP);
    half wet = wetAmount * material.wetness;
    half snow = snowAmount * up;

    float3 space = HB_BeadSpace(positionWS, anchorOS);
    float3 spaceDx = ddx(space);
    float3 spaceDy = ddy(space);
    float3 beadNormal = HB_BeadNormal(spaceDx, spaceDy);
    float3 positionDx = ddx(positionWS);
    float3 positionDy = ddy(positionWS);
    float footprint = length(abs(positionDx) + abs(positionDy));
    half detail = HB_WeatherDetail(positionWS);

    half dropShare = wet * up * material.beads.x;
    half flakeShare = snow * material.beads.z;
    float cellSize = max(material.beads.y, material.beads.w);
    half drops = 0.0h;
    half flakes = 0.0h;

    UNITY_BRANCH
    if (detail > 0.0h && dropShare + flakeShare > 0.0h && footprint < cellSize * HB_BEAD_RADIUS * 2.0)
    {
        float3 slope;
        half flake;
        half bead = HB_Beads(space, beadNormal, dropShare, flakeShare, cellSize,
                             float2(material.beads.y, material.beads.w) / cellSize, max(material.beadLife, 0.1h),
                             clock, footprint, slope, flake) * detail;

        drops = bead * (1.0h - flake);
        flakes = bead * flake;

        UNITY_BRANCH
        if (bead > 0.0h)
        {
            slope *= lerp(HB_DROP_BEND, HB_FLAKE_BEND, float(flake));
            weather.flowBend = HB_BeadBend(slope, spaceDx, spaceDy, positionDx, positionDy, normalWS);
        }
    }

    weather.snow = flakes;
    weather.flow = drops * (1.0h - flakes);
    weather.wet = wet * (1.0h - flakes);

    return weather;
}

HBWeather HB_ResolveWeather(float3 positionWS, half3 normalWS, half brushCoverage, float3 anchorOS,
                            HBWeatherMaterial material)
{
    HBWeather weather;

#if defined(_HB_WEATHER_MOVING)
    uint body = unity_RendererUserValue;
    half wet = half(float(body & HB_BODY_LEVELS) * HB_BODY_UNIT);
    half snow = half(float((body >> HB_BODY_SNOW_SHIFT) & HB_BODY_LEVELS) * HB_BODY_UNIT);
    float clock = float((body >> HB_BODY_CLOCK_SHIFT) & HB_BODY_CLOCK_MASK) / HB_BODY_CLOCK_RATE;

    UNITY_BRANCH
    if ((body & HB_BODY_TRACKED) == 0u)
    {
        half open = HB_SURFACE_WET > 0.0 || HB_SURFACE_SNOW > 0.0 ? HB_ShelterOpen(positionWS, normalWS) : 0.0h;

        wet = half(HB_SURFACE_WET) * open;
        snow = half(HB_SURFACE_SNOW) * open;
        clock = HB_SURFACE_CLOCK;
    }

    weather = HB_ResolveMovingWeather(positionWS, normalWS, anchorOS, material, wet, snow, clock);
#else
    weather = HB_ResolveSurfaceWeather(positionWS, normalWS, brushCoverage, material.wetness, material.snowCover,
                                       false, 1.0h);
#endif

    return weather;
}

float2 HB_RippleLayer(float2 p, float seed, half amount)
{
    float2 cell = floor(p);
    float cycle = HB_SURFACE_CLOCK * HB_RIPPLE_SPEED + HB_BrushHash(cell + seed).x;
    float3 drop = HB_BrushHash(cell + float2(floor(cycle) * 131.0, seed * 71.0));

    float phase = frac(cycle);
    float2 offset = p - cell - 0.5 - (drop.xy - 0.5) * HB_RIPPLE_JITTER;

    float distanceToCentre = length(offset);
    float gap = distanceToCentre - phase * HB_RIPPLE_REACH;

    float envelope = exp(-gap * gap / (HB_RIPPLE_WIDTH * HB_RIPPLE_WIDTH)) * (1.0 - phase);
    float wave = sin(gap * TWO_PI / HB_RIPPLE_WAVE) * envelope * (drop.z <= amount ? 1.0 : 0.0);

    return offset / max(distanceToCentre, 1e-4) * wave;
}

half3 HB_RainRipples(float3 positionWS, half3 normalWS, HBWeather weather)
{
    half3 bend = weather.flowBend;

    UNITY_BRANCH
    if (weather.rain > 0.0h && normalWS.y > HB_WET_STEEP - HB_WET_STEEP_EDGE)
    {
        float2 p = positionWS.xz / HB_RIPPLE_CELL;
        float2 tilt = HB_RippleLayer(p, 0.0, weather.rain) + HB_RippleLayer(p + 0.5, 389.0, weather.rain);
        half flat = 1.0h - saturate((HB_WET_STEEP - normalWS.y) / HB_WET_STEEP_EDGE);

        bend += half3(half(tilt.x), 0.0h, half(tilt.y)) * HB_RIPPLE_STRENGTH * weather.rain * flat;
    }

    return any(bend != 0.0h) ? SafeNormalize(normalWS + bend) : normalWS;
}

void HB_ApplyWeather(HBWeather weather, inout SurfaceData surfaceData, half3 normalWS, bool ground)
{
    UNITY_BRANCH
    if (weather.wet > 0.0h || weather.snow > 0.0h || weather.flow > 0.0h)
    {
        half wet = weather.wet;
        half flow = weather.flow;
        half snow = weather.snow;
        half flat = saturate(normalWS.y);
        half gloss = max(surfaceData.smoothness, lerp(HB_WET_WALL_SMOOTHNESS, HB_WET_FLAT_SMOOTHNESS, flat));

        if (ground)
            surfaceData.albedo *= 1.0h - HB_GROUND_WET_DARKEN * wet;
        else
            surfaceData.albedo = PositivePow(max(surfaceData.albedo, 0.0h), 1.0h + wet)
                               * (1.0h - HB_WET_DARKEN * wet) * (1.0h - HB_FLOW_DARKEN * flow);
        surfaceData.smoothness = lerp(surfaceData.smoothness, gloss, wet);
        surfaceData.smoothness = lerp(surfaceData.smoothness, HB_FLOW_SMOOTHNESS, flow);

        surfaceData.albedo = lerp(surfaceData.albedo, HB_SNOW_ALBEDO, snow);
        surfaceData.smoothness = lerp(surfaceData.smoothness, HB_SNOW_SMOOTHNESS, snow);
        surfaceData.metallic = lerp(surfaceData.metallic, 0.0h, snow);
        surfaceData.emission *= 1.0h - snow;
    }
}

void HB_SoftenBrushUnderSnow(half snow, inout HBStyleData style)
{
    half keep = 1.0h - snow * HB_SNOW_SOFTEN;

    style.brushAlbedo *= keep;
    style.brushRelief *= keep;
    style.brushShading *= keep;
}

#endif
