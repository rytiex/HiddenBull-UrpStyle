#ifndef HIDDENBULL_URPSTYLE_WEATHER_SURFACE_INCLUDED
#define HIDDENBULL_URPSTYLE_WEATHER_SURFACE_INCLUDED

#include "StyleBrush.hlsl"
#include "StyleSkyExposure.hlsl"
#include "StyleShore.hlsl"

float4 _HB_WeatherSurface;

#define HB_SURFACE_WET          _HB_WeatherSurface.x
#define HB_SURFACE_SNOW         _HB_WeatherSurface.y
#define HB_SURFACE_RAIN         _HB_WeatherSurface.z
#define HB_SURFACE_CLOCK        _HB_WeatherSurface.w

#define HB_WEATHER_STROKE       4.0
#define HB_WEATHER_CONTRAST     2.0h
#define HB_WEATHER_EDGE_BRUSH   0.35h

#define HB_WET_SPREAD           0.5h
#define HB_WET_PATCH_DELAY      0.6h
#define HB_WET_EDGE_DELAY       0.4h
#define HB_WET_SPOT_RAIN        10.0h
#define HB_WET_STEEP            0.966h
#define HB_WET_STEEP_EDGE       0.026h
#define HB_WET_UNDERSIDE        0.25h
#define HB_WET_SHORE_OPEN       4.0h
#define HB_FLOW_STEEP           0.7h
#define HB_FLOW_STEEP_EDGE      0.2h

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

#define HB_BEAD_JITTER          0.4
#define HB_BEAD_RADIUS          0.3
#define HB_BEAD_SOFT            0.06
#define HB_BEAD_STEEPEST        0.35
#define HB_DROP_BEND            0.4
#define HB_FLAKE_BEND           0.2

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

#define HB_SNOW_ALBEDO          half3(0.5h, 0.54h, 0.6h)
#define HB_SNOW_SMOOTHNESS      0.2h
#ifndef HB_SNOW_FULL_SLOPE
    #define HB_SNOW_FULL_SLOPE  0.407h
#endif
#ifndef HB_SNOW_NO_SLOPE
    #define HB_SNOW_NO_SLOPE    0.174h
#endif
#define HB_SNOW_SLOPE_EDGE      0.1h
#define HB_SNOW_SLOPE_BRUSH     0.6h
#define HB_SNOW_SOFTEN          0.35h
#define HB_SNOW_STEEP_CALM      1.4h
#define HB_SNOW_FLAT_OBJECT     0.1
#define HB_SNOW_FADE_IN         3.0h

#define HB_SNOW_SPREAD          0.6h
#define HB_SNOW_PATCH_DELAY     0.5h
#ifndef HB_SNOW_EDGE_DELAY
    #define HB_SNOW_EDGE_DELAY  0.35h
#endif
#define HB_SNOW_DROP_DELAY      0.3h
#define HB_SNOW_BRUSH_DELAY     0.15h
#define HB_SNOW_EDGE_WIDTH      0.2
#define HB_SNOW_PATCH_SCALE     3.1
#define HB_SNOW_DETAIL_SCALE    0.9

#define HB_WET_DARKEN           0.2h
#define HB_WET_FLAT_SMOOTHNESS  0.92h
#define HB_WET_WALL_SMOOTHNESS  0.6h
#define HB_GROUND_WET_DARKEN    0.55h
#define HB_GROUND_WET_FILM      0.35h

#define HB_RIPPLE_CELL          0.35
#define HB_RIPPLE_SPEED         1.3
#define HB_RIPPLE_JITTER        0.5
#define HB_RIPPLE_REACH         0.35
#define HB_RIPPLE_WIDTH         0.05
#define HB_RIPPLE_WAVE          0.08
#define HB_RIPPLE_STRENGTH      0.6h

struct HiddenBullWeather
{
    half wet;
    half snow;
    half rain;
    half flow;
    half3 flowBend;
    half3 debug;
};

half HB_WetSteep(half normalY)
{
    return saturate((HB_WET_STEEP - normalY) / HB_WET_STEEP_EDGE);
}

half HB_WetPatches(float2 xz)
{
    return half(0.65 * (HB_BrushField(xz / HB_SNOW_PATCH_SCALE) * 0.5 + 0.5)
              + 0.35 * (HB_BrushField(xz / HB_SNOW_DETAIL_SCALE + 17.0) * 0.5 + 0.5));
}

half HB_RainSpots(float2 xz, half wet, half brushCoverage, float footprint)
{
    float2 p = xz / HB_SPOT_CELL;

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

            dots = max(dots, saturate((size - half(distance(p, centre))) / HB_SPOT_SOFT));
        }
    }

    half blur = half(saturate(footprint / HB_SPOT_CELL * 2.0 - 1.0));

    dots = lerp(dots, saturate(wet * HB_SPOT_AVERAGE), blur);

    return max(dots, saturate((wet - HB_SPOT_GROWN) / HB_SPOT_FILM_RANGE));
}

float3 HB_BeadSpace(float3 positionWS, float3 anchorOS)
{
#if defined(_HB_BRUSH_ANCHOR) || defined(HB_ANCHOR_REST)
    return anchorOS * HB_ObjectScale();
#else
    return HB_ObjectSpacePosition(positionWS);
#endif
}

half HB_Beads(float3 space, half amount, half density, float cellSize, float seed, float life, float footprint,
              out float3 slope)
{
    float3 q = space / cellSize;
    float3 home = floor(q - 0.5);

    half share = amount * density;
    half bead = 0.0h;
    float3 offset = float3(0.0, 0.0, 0.0);

    UNITY_UNROLL
    for (int i = 0; i < 8; i++)
    {
        float3 cell = home + float3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        float3 timing = HB_BrushHash(cell.xy + cell.z * 97.0 + seed + 919.0);

        float cycle = HB_SURFACE_CLOCK / (life * (0.6 + timing.x * 0.8)) + timing.y;
        float age = frac(cycle);
        float2 generation = floor(cycle) * float2(13.0, 29.0);

        float3 place = HB_BrushHash(cell.xy + cell.z * 157.0 + seed + generation);
        float3 kind = HB_BrushHash(cell.xy + cell.z * 311.0 + seed + 452.0 + generation);

        float3 centre = cell + 0.5 + (place - 0.5) * HB_BEAD_JITTER;
        float radius = HB_BEAD_RADIUS * (0.6 + kind.y * 0.4) * (1.0 - age * age);
        float3 away = q - centre;

        half cover = half(saturate((radius - length(away)) / HB_BEAD_SOFT)) * (half(kind.x) < share ? 1.0h : 0.0h);

        if (cover > bead)
        {
            bead = cover;
            offset = away / radius;
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

HiddenBullWeather HB_ResolveWeather(float3 positionWS, half3 normalWS, half brushCoverage, float3 strokeShift,
                                    float3 anchorOS, half receive, half4 beads, half2 beadLife, half2 limits)
{
    HiddenBullWeather weather;
    weather.wet = 0.0h;
    weather.snow = 0.0h;
    weather.rain = 0.0h;
    weather.flow = 0.0h;
    weather.flowBend = half3(0.0h, 0.0h, 0.0h);
    weather.debug = half3(1.0h, 0.0h, 1.0h);

    float3 beadSpace = HB_BeadSpace(positionWS, anchorOS);
    float3 beadDx = ddx(beadSpace);
    float3 beadDy = ddy(beadSpace);
    float3 positionDx = ddx(positionWS);
    float3 positionDy = ddy(positionWS);

    float footprint = length(abs(positionDx) + abs(positionDy));

#ifdef HB_WEATHER_SNOW_ONLY
    bool active = HB_SURFACE_SNOW > 0.0;
#else
    bool active = HB_SURFACE_WET > 0.0 || HB_SURFACE_SNOW > 0.0;
#endif

    half shore = receive > 0.0h ? HB_ShoreWet(positionWS) : 0.0h;

    UNITY_BRANCH
    if (receive > 0.0h && (active || shore > 0.0h))
    {
#ifdef HB_WEATHER_NO_BOUNDS
        float3 boundsMin = float3(-1e9, 0.0, -1e9);
        float3 boundsMax = float3(1e9, 0.0, 1e9);
#else
        float3 boundsMin = unity_RendererBounds_Min.xyz;
        float3 boundsMax = unity_RendererBounds_Max.xyz;
#endif
        float3 size = max(boundsMax - boundsMin, 1e-3);

        float2 xz = positionWS.xz;

        float2 shift = strokeShift.xz * HB_WEATHER_STROKE;
        half edgeBrush = 0.5h + brushCoverage * HB_WEATHER_EDGE_BRUSH;

        half exposure = HB_SkyExposure(positionWS, normalWS, shift, false);
        exposure = saturate((exposure - 0.5h) * HB_WEATHER_CONTRAST + edgeBrush);

        half patches = HB_WetPatches(xz);

        float2 toEdge = min(xz - boundsMin.xz, boundsMax.xz - xz);
        half edge = half(1.0 - saturate(min(toEdge.x, toEdge.y) / HB_SNOW_EDGE_WIDTH));

        half cover = half(HB_SURFACE_SNOW) * limits.y;
        bool beaded = receive > 1.5h;
        half snow = 0.0h;

        UNITY_BRANCH
        if (cover > 0.0h && !beaded)
        {
            half height = size.y > HB_SNOW_FLAT_OBJECT ? half(saturate((positionWS.y - boundsMin.y) / size.y)) : 1.0h;
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

            snow = slopeSnow * saturate(settled * HB_SNOW_FADE_IN) * exposure;
        }

        weather.snow = snow * (1.0h - shore);

#ifdef HB_WEATHER_SNOW_ONLY
        half wet = 0.0h;
#else
        half wet = half(HB_SURFACE_WET);
#endif
        half open = exposure;

        UNITY_BRANCH
        if ((wet > 0.0h || (beaded && cover > 0.0h)) && exposure < 1.0h && normalWS.y > -HB_WET_UNDERSIDE)
        {
            half through = HB_SkyExposure(positionWS, normalWS, shift, true);
            through = saturate((through - 0.5h) * HB_WEATHER_CONTRAST + edgeBrush);

            open = lerp(max(exposure, through), exposure, saturate(-normalWS.y / HB_WET_UNDERSIDE));
        }

        half fullWet = 0.0h;

#ifndef HB_WEATHER_SNOW_ONLY
        half rain = half(HB_SURFACE_RAIN);
        half steepWet = HB_WetSteep(normalWS.y);
        half raining = saturate(rain * HB_WET_SPOT_RAIN);

        half patchy = saturate((wet - saturate(patches * HB_WET_PATCH_DELAY + edge * HB_WET_EDGE_DELAY)
                                    * HB_WET_SPREAD) / (1.0h - HB_WET_SPREAD));
        half flat = patchy;

        UNITY_BRANCH
        if (raining > 0.0h && steepWet < 1.0h)
        {
            half spotted = wet < HB_SPOT_GROWN + HB_SPOT_FILM_RANGE
                ? HB_RainSpots(xz, wet, brushCoverage, footprint)
                : 1.0h;

            flat = lerp(patchy, spotted, raining);
        }

        half drained = saturate((wet - patches * HB_WET_PATCH_DELAY * HB_WET_SPREAD) / (1.0h - HB_WET_SPREAD));
        half sloped = lerp(drained, saturate(wet / (1.0h - HB_WET_SPREAD)), raining);

        half lapped = shore < 1.0h ? saturate(shore * HB_WET_SHORE_OPEN) : 0.0h;
        half reach = max(open, lapped) * (1.0h - weather.snow);
        fullWet = lerp(flat, sloped, steepWet) * reach;

        weather.wet = saturate(fullWet + shore) * limits.x;
        weather.rain = beaded ? 0.0h : rain * reach;

        half running = weather.rain;

        half steepFlow = saturate((HB_FLOW_STEEP - normalWS.y) / HB_FLOW_STEEP_EDGE);

        UNITY_BRANCH
        if (running > 0.0h && steepFlow > 0.0h && footprint < HB_FLOW_SPACING)
        {
            half3 bend;
            half flow = HB_RainFlow(positionWS, normalWS, running, footprint, bend);

            weather.flow = flow * steepFlow;
            weather.flowBend = bend * steepFlow;
        }
#endif

        UNITY_BRANCH
        if (beaded)
        {
            float3 dropSlope = float3(0.0, 0.0, 0.0);
            float3 flakeSlope = float3(0.0, 0.0, 0.0);
            half drops = 0.0h;
            half flakes = 0.0h;

            UNITY_BRANCH
            if (fullWet > 0.0h && footprint < beads.y * HB_BEAD_RADIUS * 2.0)
                drops = HB_Beads(beadSpace, fullWet, beads.x, beads.y, 71.0, max(beadLife.x, 0.1h), footprint,
                                 dropSlope);

            UNITY_BRANCH
            if (cover > 0.0h && open > 0.0h && footprint < beads.w * HB_BEAD_RADIUS * 2.0)
                flakes = HB_Beads(beadSpace, cover * open, beads.z, beads.w, 211.0, max(beadLife.y, 0.1h), footprint,
                                  flakeSlope);

            float3 slope = lerp(dropSlope * HB_DROP_BEND, flakeSlope * HB_FLAKE_BEND, float(flakes));

            weather.flow = drops * (1.0h - flakes);
            weather.flowBend = HB_BeadBend(slope, beadDx, beadDy, positionDx, positionDy, normalWS);
            weather.snow = flakes;
        }

        UNITY_BRANCH
        if (HB_WEATHER_DEBUG > 0.5)
        {
            int mode = (int)HB_WEATHER_DEBUG;

            weather.debug = mode == 1 ? half3(exposure, exposure, exposure)
                          : mode == 2 ? half3(fullWet, fullWet, fullWet)
                          : mode == 3 ? half3(snow, snow, snow)
                          : mode == 4 ? half3(limits.x, limits.y, 0.0h)
                          : half3(HB_SURFACE_WET, HB_SURFACE_SNOW, HB_SURFACE_RAIN);
        }
    }

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

half HB_RainReach(float3 positionWS, half3 normalWS)
{
    half rain = half(HB_SURFACE_RAIN);
    half open = 0.0h;

    UNITY_BRANCH
    if (rain > 0.0h)
    {
        open = HB_SkyExposure(positionWS, normalWS, float2(0.0, 0.0), false);
        open = saturate((open - 0.5h) * HB_WEATHER_CONTRAST + 0.5h);

        UNITY_BRANCH
        if (open < 1.0h)
        {
            half through = HB_SkyExposure(positionWS, normalWS, float2(0.0, 0.0), true);

            open = max(open, saturate((through - 0.5h) * HB_WEATHER_CONTRAST + 0.5h));
        }
    }

    return rain * open;
}

half3 HB_RainRipples(float3 positionWS, half3 normalWS, HiddenBullWeather weather)
{
    half3 bend = weather.flowBend;

    UNITY_BRANCH
    if (weather.rain > 0.0h && normalWS.y > HB_WET_STEEP - HB_WET_STEEP_EDGE)
    {
        float2 p = positionWS.xz / HB_RIPPLE_CELL;
        float2 tilt = HB_RippleLayer(p, 0.0, weather.rain) + HB_RippleLayer(p + 0.5, 389.0, weather.rain);

        bend += half3(half(tilt.x), 0.0h, half(tilt.y))
              * HB_RIPPLE_STRENGTH * weather.rain * (1.0h - HB_WetSteep(normalWS.y));
    }

    return any(bend != 0.0h) ? SafeNormalize(normalWS + bend) : normalWS;
}

void HB_SoftenBrushUnderSnow(half snow, inout HiddenBullStyleData style)
{
    half keep = 1.0h - snow * HB_SNOW_SOFTEN;

    style.brushAlbedo *= keep;
    style.brushRelief *= keep;
    style.brushShading *= keep;
}

void HB_ApplyWeather(HiddenBullWeather weather, inout SurfaceData surfaceData, half3 normalWS)
{
    if (weather.wet > 0.0h || weather.snow > 0.0h || weather.flow > 0.0h)
    {
        half flat = saturate(normalWS.y);
        half wet = weather.wet;
        half flow = weather.flow;

        surfaceData.albedo = PositivePow(max(surfaceData.albedo, 0.0h), 1.0h + wet)
                           * (1.0h - HB_WET_DARKEN * wet) * (1.0h - HB_FLOW_DARKEN * flow);
        surfaceData.smoothness = lerp(surfaceData.smoothness,
                                      max(surfaceData.smoothness,
                                          lerp(HB_WET_WALL_SMOOTHNESS, HB_WET_FLAT_SMOOTHNESS, flat)),
                                      wet);
        surfaceData.smoothness = lerp(surfaceData.smoothness, HB_FLOW_SMOOTHNESS, flow);

        half snow = weather.snow;

        surfaceData.albedo = lerp(surfaceData.albedo, HB_SNOW_ALBEDO, snow);
        surfaceData.smoothness = lerp(surfaceData.smoothness, HB_SNOW_SMOOTHNESS, snow);
        surfaceData.metallic = lerp(surfaceData.metallic, 0.0h, snow);
        surfaceData.emission *= 1.0h - snow;
    }
}

half HB_ApplyGroundWeather(HiddenBullWeather weather, inout SurfaceData surfaceData, half3 normalWS)
{
    half wet = weather.wet;

    surfaceData.albedo *= 1.0h - HB_GROUND_WET_DARKEN * wet;
    weather.wet = 0.0h;

    HB_ApplyWeather(weather, surfaceData, normalWS);

    return max(wet * HB_GROUND_WET_FILM, weather.flow);
}

#endif
