#ifndef HIDDENBULL_URPSTYLE_LIQUID_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIQUID_PASS_INCLUDED

#include "LiquidInput.hlsl"

#if defined(HB_LIQUID_COLOR) || defined(HB_LIQUID_UNDERWATER)
    #define _HB_SPECULAR
    #include "../Library/StyleLighting.hlsl"
#endif

#include "../Library/StyleFog.hlsl"
#include "../Library/StyleTransparency.hlsl"
#include "../Library/StyleWeatherSurface.hlsl"

#define HB_LIQUID_MURK_BLUR      0.35h
#define HB_CAUSTIC_TINT          2.0h
#define HB_CAUSTIC_MURK          0.996h
#define HB_CAUSTIC_SHADE         0.6
#define HB_CAUSTIC_LIFT          1.5
#define HB_CAUSTIC_RISE          1.0
#define HB_GLOW_BASE             0.75h
#define HB_GLOW_GRAIN            0.5h
#define HB_UNDER_WINDOW          0.66h
#define HB_UNDER_WINDOW_SOFT     0.06h
#define HB_UNDER_DEEP            64.0
#define HB_UNDER_SOLVE_STEPS     3
#define HB_LIQUID_EDGE_SOFT      4.0h
#define HB_BUBBLE_BEND           0.6h
#define HB_REFRACTION_FULL_DEPTH 1.0
#define HB_LIQUID_SHELTER_LIFT   (HB_SKY_EXPOSURE_BIAS + HB_SKY_EXPOSURE_RANGE)

struct LiquidAttributes
{
    float4 positionOS : POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LiquidVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD1;
    float2 restXZ     : TEXCOORD0;
    float2 shore      : TEXCOORD2;
    float2 local      : TEXCOORD3;
    float3 slope      : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

LiquidVaryings HiddenBullLiquidVertex(LiquidAttributes input)
{
    LiquidVaryings output = (LiquidVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    s_Seat = UNITY_MATRIX_M._m13 + _HB_LiquidSeat;

    float cell = HB_LiquidCell(s_Seat);

    float2 origin = HB_LiquidOrigin(cell);
    float2 flat;
    float rim = 1.0;
    float spacing;
    bool beyond = false;

    UNITY_BRANCH
    if (HB_LiquidTiled())
    {
        flat = TransformObjectToWorld(float3(input.positionOS.x, 0.0, input.positionOS.z)).xz;
        spacing = HB_LiquidSpacing(_WaterGrid.x, 0.0, flat);

        UNITY_BRANCH
        if (HB_LiquidStill())
            rim = HB_LiquidBoxRim(input.positionOS.xz * _WaterBox.xy * 2.0);
    }
    else
    {
        float ring = round(log2(max(UNITY_MATRIX_M._m00, 1.0)));
        float fine = cell * exp2(ring);
        float2 placed = origin + mul(UNITY_MATRIX_M, float4(input.positionOS.xyz, 1.0)).xz * cell;
        float2 tight = floor(placed / fine + 0.5) * fine;
        float2 loose = floor(placed / (fine * 2.0) + 0.5) * (fine * 2.0);
        float2 eye = abs(loose - GetCameraPositionWS().xz);
        float morph = HB_LiquidMorph(cell, ring, max(eye.x, eye.y), HB_LiquidLift(s_Seat));
        float2 centre = GetCameraPositionWS().xz;

        flat = morph >= 1.0 ? loose : (morph <= 0.0 ? tight : lerp(tight, loose, morph));

        float2 past = abs(flat - centre) - _WaterGrid.y;

        beyond = max(past.x, past.y) > fine * HB_LIQUID_PAST;
        flat = clamp(flat, centre - _WaterGrid.y, centre + _WaterGrid.y);
        rim = HB_LiquidRim(flat, centre);
        spacing = HB_LiquidSpacing(cell, HB_LiquidLift(s_Seat), flat);
    }

    float4 placed = float4(0.0, 0.0, 0.0, 0.0);
    bool within = !HB_LiquidTiled() && HB_LiquidInside(flat);

    UNITY_BRANCH
    if (HB_LiquidTiled())
    {
        placed = HB_LevelRead(_WaterLevelMap, _WaterArea, flat);
    }
    else if (within)
    {
        placed = HB_LevelRead(_WaterLevelMap, _WaterArea, flat);
        placed.r = 0.0;
    }

    rim *= (1.0 - placed.g) * (1.0 + _WaterUnbatched);

    UNITY_BRANCH
    if ((HB_LiquidTiled() && _WaterArea.z > 0.0) || within)
        rim *= 1.0 - HB_LevelRead(_WaterWeightMap, _WaterArea, flat).a;

    s_Stream = placed.ba;

    UNITY_BRANCH
    if ((HB_LiquidTiled() || within) && HB_LiquidLayered())
        s_Mix = HB_LiquidLayerWeights(flat, HB_LAYER_ROUGH * 0.5);

    float3 rest = float3(flat.x, UNITY_MATRIX_M._m13 + placed.r, flat.y);
    float3 moved = HB_LiquidDisplace(rest, spacing, output.shore) * rim;

    output.shore.x *= rim;

    output.restXZ = rest.xz;
    output.local = flat - origin;

    UNITY_BRANCH
    if ((HB_LiquidTiled() && _WaterArea.z > 0.0) || within)
    {
        float2 east = float2(HB_LIQUID_SLOPE_SPAN, 0.0);
        float2 north = float2(0.0, HB_LIQUID_SLOPE_SPAN);

        float4 ahead = HB_LevelRead(_WaterLevelMap, _WaterArea, flat + east);
        float4 behind = HB_LevelRead(_WaterLevelMap, _WaterArea, flat - east);
        float4 above = HB_LevelRead(_WaterLevelMap, _WaterArea, flat + north);
        float4 below = HB_LevelRead(_WaterLevelMap, _WaterArea, flat - north);

        output.slope = float3(ahead.r - behind.r, above.r - below.r,
                              length(ahead.ba - behind.ba) + length(above.ba - below.ba))
                     / (HB_LIQUID_SLOPE_SPAN * 2.0);
        output.slope.xy *= within ? 0.0 : 1.0;
    }

#ifdef HB_LIQUID_SKY
    output.positionWS = rest;
#else
    output.positionWS = rest + moved;
#endif

    output.positionCS = TransformWorldToHClip(output.positionWS);

    if (beyond)
        output.positionCS = asfloat(0x7FC00000).xxxx;

    return output;
}

void HB_LiquidEnter(LiquidVaryings input)
{
    s_Seat = UNITY_MATRIX_M._m13 + _HB_LiquidSeat;

    float4 placed = HB_LevelRead(_WaterLevelMap, _WaterArea, input.restXZ);

    s_Fade = 1.0 - placed.g;
    s_Stream = placed.ba;
    s_Normal = normalize(float3(-input.slope.x, 1.0, -input.slope.y));

    UNITY_BRANCH
    if (!HB_LiquidTiled())
    {
        if (HB_LiquidSheets(input.restXZ, HB_LIQUID_NEAR) > 0.5)
        {
            if (abs(placed.r) > HB_LIQUID_LEVEL)
                clip(-1.0);

#if !defined(HB_LIQUID_FORWARD)
            if (HB_LiquidSheeted(input.restXZ))
                clip(-1.0);
#endif
        }
    }

    s_Shear = input.slope.z;

    if (s_Fade <= 0.0)
        clip(-1.0);

    float2 restDx = ddx(input.restXZ);
    float2 restDy = ddy(input.restXZ);

    UNITY_BRANCH
    if (HB_LiquidLayered())
    {
        float3 painted = HB_LiquidLayerPaint(input.restXZ);

        UNITY_BRANCH
        if (any(painted > 0.0))
            s_Mix = HB_LiquidLayerBlend(painted, HB_LiquidLayerRough(input.restXZ, restDx, restDy));
    }

    HB_LiquidCut(input.positionCS, input.positionWS);
}

struct HiddenBullLiquidSurface
{
    float2 uv;
    half3 geometryNormalWS;
    half3 tangentWS;
    half3 bitangentWS;
    half3 normalWS;
    half fold;
    half trail;
};

HiddenBullLiquidSurface HB_LiquidSurface(LiquidVaryings input, bool frontFace)
{
    HiddenBullLiquidSurface surface;

    half facing = frontFace ? 1.0h : -1.0h;
    half3 up = half3(s_Normal);
    half3 flat = normalize(half3(1.0h, 0.0h, 0.0h) - up * up.x);
    half3 normal = up * facing;
    half3 bitangent = cross(flat, up) * facing;
    half3 tangent = flat;

    surface.uv = HB_LiquidUV(input.local);
    surface.geometryNormalWS = normal;
    surface.tangentWS = tangent;
    surface.bitangentWS = bitangent;
    surface.normalWS = TransformTangentToWorld(HB_LiquidNormalTS(surface.uv), half3x3(tangent, bitangent, normal));

    float2 slope;

    surface.fold = HB_LiquidWaves(input.restXZ, input.local, input.shore, slope);
#if defined(HB_LIQUID_COLOR)
    surface.trail = HB_LiquidTrail(input.restXZ, input.shore, input.positionWS);
#else
    surface.trail = 0.0h;
#endif
    surface.normalWS = NormalizeNormalPerPixel(surface.normalWS + half3(-slope.x, 0.0, -slope.y) * facing);

    UNITY_BRANCH
    if (_ReceiveWeather > 0.5)
    {
        float3 lifted = input.positionWS + float3(0.0, HB_LIQUID_SHELTER_LIFT, 0.0);

        HiddenBullWeather weather = (HiddenBullWeather)0;
        weather.rain = HB_RainReach(lifted, normal);

        surface.normalWS = HB_RainRipples(input.positionWS, surface.normalWS, weather);
    }

    return surface;
}

void HB_ApplyBubbleNormal(inout HiddenBullLiquidSurface surface, HiddenBullBubbles bubbles, half strength)
{
    half3 bend = surface.tangentWS * bubbles.slope.x + surface.bitangentWS * bubbles.slope.y;

    surface.normalWS = NormalizeNormalPerPixel(surface.normalWS + bend * strength);
}

float HB_LiquidPath(float3 positionWS, float thickness, bool frontFace)
{
    return frontFace ? thickness : max(LinearEyeDepth(positionWS, GetWorldToViewMatrix()), 0.0);
}

half HB_LiquidEdge(float thickness)
{
    return half(1.0 - saturate(thickness / HB_LiquidFoamDistance()));
}

half HB_LiquidCover(half spots, half veil)
{
    half solid = spots * spots;

    return min(solid + (1.0h - solid) * min(spots - solid + veil, HB_FOAM_GLOW), HB_FOAM_COVER_MOST);
}

half HB_LiquidCrustAt(float2 uv)
{
    half crust = 0.0h;

    UNITY_BRANCH
    if (HB_LiquidCrusted())
    {
        float3 keep = s_Mix;
        float own = HB_LiquidOwnShare(keep);

        UNITY_BRANCH
        if (own > 0.0 && _CrustAmount > 0.0h)
        {
            s_Mix = float3(0.0, 0.0, 0.0);
            crust += HB_LavaCrust(HB_LavaCool(uv)) * half(own);
        }

        UNITY_BRANCH
        if (keep.x > 0.0 && _OtherFoam[0].w > 0.0)
        {
            s_Mix = float3(1.0, 0.0, 0.0);
            crust += HB_LavaCrust(HB_LavaCool(uv)) * half(keep.x);
        }

        UNITY_BRANCH
        if (keep.y > 0.0 && _OtherFoam[1].w > 0.0)
        {
            s_Mix = float3(0.0, 1.0, 0.0);
            crust += HB_LavaCrust(HB_LavaCool(uv)) * half(keep.y);
        }

        UNITY_BRANCH
        if (keep.z > 0.0 && _OtherFoam[2].w > 0.0)
        {
            s_Mix = float3(0.0, 0.0, 1.0);
            crust += HB_LavaCrust(HB_LavaCool(uv)) * half(keep.z);
        }

        s_Mix = keep;

        crust *= HB_FOAM_COVER_MOST;
    }

    return crust;
}

half HB_LiquidCrust(float2 uv)
{
#if defined(_HB_LIQUID_STREAMS)
    HiddenBullStream stream = HB_LiquidStreamOf(uv);
    half crust;

    UNITY_BRANCH
    if (stream.moving)
    {
        HB_LiquidFootprint(stream.firstDx, stream.firstDy);

        half first = HB_LiquidCrustAt(stream.first);

        HB_LiquidFootprint(stream.secondDx, stream.secondDy);
        crust = lerp(first, HB_LiquidCrustAt(stream.second), stream.blend);
    }
    else
    {
        HB_LiquidFootprint(stream.baseDx, stream.baseDy);
        crust = HB_LiquidCrustAt(uv);
    }

    HB_LiquidFootprint(stream.baseDx, stream.baseDy);

    return crust;
#else
    HB_LiquidFootprint(ddx(uv), ddy(uv));

    return HB_LiquidCrustAt(uv);
#endif
}

half HB_LiquidThrob()
{
    half throb = half(0.85 + 0.15 * sin(HB_LiquidTime() * float(_Pulse) + float(s_Grain) * 9.0));

    return lerp(1.0h, throb, saturate(HB_LiquidCrustAmount() * 8.0h));
}

half HB_LiquidSpotsAt(float2 uv, float thickness, half fold, half trail, float3 positionWS,
                      out HiddenBullBubbles bubbles, out half veil)
{
    half foam = HB_LiquidFoam(uv, thickness, fold, trail, positionWS, veil);

    bubbles = (HiddenBullBubbles)0;

    UNITY_BRANCH
    if (HB_LiquidBubbled())
    {
        half amount = saturate(HB_LiquidBubbleAmount() * (1.0h + 2.0h * HB_LiquidEdge(thickness)));

        bubbles = HB_LiquidBubbleField(uv, amount, HB_BUBBLE_CELLS, 1.0);

        half share = HB_LiquidBubbleShare();

        bubbles.dome *= share;
        bubbles.ring *= share;
        bubbles.burst *= share;
        bubbles.slope *= share;
        bubbles.lens *= float(share);
        foam = saturate(foam + bubbles.dome * 0.3h + bubbles.ring * 0.9h);
    }

    return foam;
}

half HB_LiquidSpots(float2 uv, float thickness, half fold, half trail, float3 positionWS,
                    out HiddenBullBubbles bubbles, out half veil)
{
#if defined(_HB_LIQUID_STREAMS)
    HiddenBullStream stream = HB_LiquidStreamOf(uv);
    half foam;

    HB_LiquidFootprint(stream.baseDx, stream.baseDy);
    s_Grain = HB_FoamGrain(uv);

    UNITY_BRANCH
    if (stream.moving)
    {
        HiddenBullBubbles other;
        half thin;

        HB_LiquidFootprint(stream.firstDx, stream.firstDy);
        foam = HB_LiquidSpotsAt(stream.first, thickness, fold, trail, positionWS, bubbles, veil);
        HB_LiquidFootprint(stream.secondDx, stream.secondDy);

        half next = HB_LiquidSpotsAt(stream.second, thickness, fold, trail, positionWS, other, thin);

        bubbles.dome = lerp(bubbles.dome, other.dome, stream.blend);
        bubbles.ring = lerp(bubbles.ring, other.ring, stream.blend);
        bubbles.burst = lerp(bubbles.burst, other.burst, stream.blend);
        bubbles.slope = lerp(bubbles.slope, other.slope, stream.blend);
        bubbles.lens = lerp(bubbles.lens, other.lens, float(stream.blend));
        veil = lerp(veil, thin, stream.blend);
        foam = lerp(foam, next, stream.blend);
    }
    else
    {
        foam = HB_LiquidSpotsAt(uv, thickness, fold, trail, positionWS, bubbles, veil);
    }

    HB_LiquidFootprint(stream.baseDx, stream.baseDy);

    return foam;
#else
    HB_LiquidFootprint(ddx(uv), ddy(uv));
    s_Grain = HB_FoamGrain(uv);

    return HB_LiquidSpotsAt(uv, thickness, fold, trail, positionWS, bubbles, veil);
#endif
}

#if defined(HB_LIQUID_COLOR) || defined(HB_LIQUID_UNDERWATER)

InputData HB_LiquidInputData(LiquidVaryings input, half3 normalWS, float2 screenUV)
{
    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.normalizedScreenSpaceUV = screenUV;
    inputData.bakedGI = HB_SampleProbeGI(inputData.positionWS, normalWS, inputData.viewDirectionWS,
                                         input.positionCS.xy, inputData.shadowMask);

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif

    return inputData;
}

HiddenBullStyleData HB_LiquidStyle()
{
    HiddenBullStyleData style = (HiddenBullStyleData)0;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = 0.35h;
    style.bakedSoftness = 0.5h;
    return style;
}

bool HB_LiquidHasKeyLight()
{
    return dot(_HB_KeyDirection.xyz, _HB_KeyDirection.xyz) > 0.5;
}

half3 HB_LiquidSunColour()
{
    return HB_LiquidHasKeyLight() ? half3(_HB_KeyColor.rgb) : half3(_MainLightColor.rgb);
}

half3 HB_LiquidSunDirection()
{
    return HB_LiquidHasKeyLight() ? half3(_HB_KeyDirection.xyz) : half3(_MainLightPosition.xyz);
}

#endif

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)

#define HB_CAUSTIC_TAPS 16

static const float2 s_CausticDisk[HB_CAUSTIC_TAPS] =
{
    float2(0.9239, 0.3827), float2(-0.3827, 0.9239), float2(-0.9239, -0.3827), float2(0.3827, -0.9239),
    float2(0.1768, 0.0), float2(-0.2257, 0.2068), float2(0.0339, -0.3938), float2(0.2847, 0.3692),
    float2(-0.5122, -0.1028), float2(0.4899, -0.3053), float2(-0.1806, 0.5846), float2(-0.3019, -0.5933),
    float2(0.6444, 0.2678), float2(-0.6922, 0.2746), float2(0.3577, -0.6842), float2(0.2298, 0.7783)
};

half HB_CausticShade(float3 floorWS, float2 aside)
{
    float3 shifted = floorWS + float3(aside.x, length(aside) * HB_CAUSTIC_RISE, aside.y) * HB_CAUSTIC_SHADE
                   + float3(HB_LiquidSunDirection()) * (length(aside) * HB_CAUSTIC_SHADE * HB_CAUSTIC_LIFT);

    half4 crisp = GetMainLightShadowParams();

    crisp.y = SOFT_SHADOW_QUALITY_OFF;

    return MainLightRealtimeShadow(TransformWorldToShadowCoord(shifted), crisp, GetMainLightShadowSamplingData());
}

#endif

#if defined(HB_LIQUID_DEPTH)

half HiddenBullLiquidDepthFragment(LiquidVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_LiquidEnter(input);

    return input.positionCS.z;
}

#elif defined(HB_LIQUID_PRIME)

#define HB_LIQUID_UNDER 0.25
#define HB_LIQUID_GRAZE 0.02

float HB_LiquidYield(float3 positionWS, float depth)
{
    float3 ray = normalize(positionWS - GetCameraPositionWS());
    float reach = HB_LIQUID_UNDER / max(abs(ray.y), HB_LIQUID_GRAZE);
    float eye = LinearEyeDepth(depth, _ZBufferParams) + reach * dot(ray, -UNITY_MATRIX_V[2].xyz);

    return (1.0 / eye - _ZBufferParams.w) / _ZBufferParams.z;
}

void HiddenBullLiquidPrimeFragment(LiquidVaryings input, out float outDepth : SV_Depth)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float4 placed = HB_LevelRead(_WaterLevelMap, _WaterArea, input.restXZ);

    outDepth = input.positionCS.z;

    if (placed.g >= 1.0)
        clip(-1.0);

    UNITY_BRANCH
    if (!HB_LiquidTiled())
    {
        if (HB_LiquidSheets(input.restXZ, HB_LIQUID_NEAR) > 0.5 && abs(placed.r) > HB_LIQUID_LEVEL)
            clip(-1.0);

        if (HB_LiquidSheeted(input.restXZ))
            outDepth = HB_LiquidYield(input.positionWS, input.positionCS.z);
    }

    HB_LiquidCut(input.positionCS, input.positionWS);
}

#elif defined(HB_LIQUID_COLOR)

float2 HB_LiquidBend(float3 positionWS, float2 screenUV, HiddenBullLiquidSurface surface, half3 viewWS,
                     float thickness, half share)
{
    float2 bend = HB_RefractionOffset(positionWS, surface.normalWS, viewWS, HB_LiquidRefraction(),
                                      float3(0.0, 0.0, 0.0), 0.0h) * share;

    UNITY_BRANCH
    if (any(bend != 0.0))
    {
        float reached = max(HB_LiquidBehind(positionWS, screenUV + bend), 0.0);
        float steepness = abs(dot(viewWS, surface.geometryNormalWS));

        bend *= saturate(min(thickness, reached) * steepness / HB_REFRACTION_FULL_DEPTH);

        if (HB_LiquidBehind(positionWS, screenUV + bend) <= 0.0)
            bend = float2(0.0, 0.0);
    }

    return bend;
}

#define HB_LIQUID_BLUR      0.03
#define HB_LIQUID_BLUR_TAPS 6
#define HB_LIQUID_SOLID     0.004h

TEXTURE2D_X(_HB_LiquidBackdrop);

half3 HB_LiquidBackdrop(float3 positionWS, float2 uv, half blur, float2 pixel)
{
    UNITY_BRANCH
    if (blur <= 0.0h)
        return SAMPLE_TEXTURE2D_X_LOD(_HB_LiquidBackdrop, sampler_LinearClamp, uv, 0).rgb;

    float2 radius = float(blur) * HB_LIQUID_BLUR * float2(_ScreenParams.y / _ScreenParams.x, 1.0);
    float rotation = InterleavedGradientNoise(pixel, 0) * TWO_PI;
    half3 sum = half3(0.0h, 0.0h, 0.0h);

    UNITY_UNROLL
    for (int tap = 0; tap < HB_LIQUID_BLUR_TAPS; tap++)
    {
        float angle = rotation + tap * (TWO_PI / HB_LIQUID_BLUR_TAPS);
        float2 aside = uv + float2(cos(angle), sin(angle)) * radius * sqrt((tap + 0.5) / HB_LIQUID_BLUR_TAPS);

        aside = HB_LiquidBehind(positionWS, aside) <= 0.0 ? uv : aside;
        sum += SAMPLE_TEXTURE2D_X_LOD(_HB_LiquidBackdrop, sampler_LinearClamp, saturate(aside), 0).rgb;
    }

    return sum * (1.0h / HB_LIQUID_BLUR_TAPS);
}

half4 HiddenBullLiquidColorFragment(LiquidVaryings input,
                                    FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_LiquidEnter(input);

    uint2 pixel = uint2(input.positionCS.xy);

    bool front = IS_FRONT_VFACE(frontFace, true, false);
    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

    HiddenBullLiquidSurface surface = HB_LiquidSurface(input, front);

    float thickness = HB_LiquidThickness(input.positionWS, screenUV);

    HiddenBullBubbles bubbles;
    half veil;
    half spots = HB_LiquidSpots(surface.uv, thickness, surface.fold, surface.trail, input.positionWS, bubbles,
                                veil);

    HB_ApplyBubbleNormal(surface, bubbles, HB_BUBBLE_BEND);

    half fogAmount = HB_FogAmountAt(input.positionWS, screenUV);
    half clear = 1.0h - fogAmount;

    half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    float path = HB_LiquidReach(HB_LiquidPath(input.positionWS, thickness, front), input.positionWS);
    half4 deepWater;
    half4 water = HB_LiquidGradientPair(path, HB_UNDER_DEEP, deepWater);
    half murk = water.a;
    half3 tone = water.rgb;

    SurfaceData surfaceData = (SurfaceData)0;
    half spread = lerp(HB_LIQUID_CREST_HARD, HB_LIQUID_CREST_SOFT, HB_LiquidCrestSoftness());
    half rise = min(HB_LIQUID_CREST_FROM, half(HB_LiquidCapFrom()) * HB_LIQUID_CREST_EARLY);
    half weave = (s_Grain - 0.5h) * HB_LIQUID_CREST_GRAIN;
    half crested = saturate((surface.fold - rise + spread - weave) / (2.0h * spread))
                 * smoothstep(0.0h, rise, surface.fold);

    surfaceData.smoothness = HB_LiquidSmoothness();
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    InputData inputData = HB_LiquidInputData(input, surface.normalWS, screenUV);

    half sunVisibility;
    half lightReach;

    half3 reflection = HiddenBullFragmentLit(inputData, surfaceData, HB_LiquidStyle(), HB_NoBrush(), 0.0h,
                                             sunVisibility, lightReach).rgb;

    half3 skyLight = (HB_GradientAmbient(half3(0.0h, 1.0h, 0.0h)) + HB_GradientAmbient(half3(1.0h, 0.0h, 0.0h)))
                   * 0.5h;
    half3 sunLight = HB_LiquidSunColour() * sunVisibility;

    reflection += sunLight * HB_LiquidGlints(surface.uv, surface.normalWS, inputData.viewDirectionWS,
                                             HB_LiquidSunDirection());
    reflection *= saturate(murk * HB_LIQUID_EDGE_SOFT);

    half3 lit = skyLight + sunLight * 0.5h
              + HB_LiquidGlow() * (HB_GLOW_BASE + HB_GLOW_GRAIN * s_Grain) * HB_LiquidThrob();
    half3 inscatter = tone * murk * lit;
    half window = 1.0h;

    UNITY_BRANCH
    if (!front)
    {
        window = smoothstep(HB_UNDER_WINDOW - HB_UNDER_WINDOW_SOFT, HB_UNDER_WINDOW + HB_UNDER_WINDOW_SOFT,
                            abs(dot(viewWS, surface.normalWS)));
        inscatter = lerp(deepWater.rgb * lit, inscatter, window);
        reflection *= 1.0h - murk;
    }

    half3 sunDirection = HB_LiquidSunDirection();
    half backlit = saturate(dot(viewWS, -sunDirection) * 0.5h + 0.5h);
    half crest = crested * lerp(HB_LIQUID_CREST_BASE, 1.0h, backlit * backlit)
               * saturate(1.0h - exp(-4.0h * max(sunDirection.y, 0.0h)));

    inscatter += (sunLight + skyLight * HB_LIQUID_CREST_SKY) * HB_LiquidCrestColour()
               * (crest * HB_LIQUID_CREST_LIGHT * (1.0h - HB_LiquidCover(spots, veil)));

    UNITY_BRANCH
    if (front && murk < HB_CAUSTIC_MURK && HB_LiquidCausticAmount() > 0.0h && thickness > 0.0)
    {
        float2 floorUV = screenUV;
        float3 floorWS = ComputeWorldSpacePosition(floorUV, SampleSceneDepth(floorUV), UNITY_MATRIX_I_VP);
        half caustics = HB_LiquidCaustics(floorWS, input.positionWS.y, HB_LiquidSunDirection());

        UNITY_BRANCH
        if (caustics > 0.0h)
        {
        #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
            half lit = HB_CausticShade(floorWS, float2(0.0, 0.0));

            UNITY_BRANCH
            if (!HB_LIQUID_LOW)
            {
                UNITY_LOOP
                for (int edge = 0; edge < 4; edge++)
                    lit += HB_CausticShade(floorWS, s_CausticDisk[edge]);

                UNITY_BRANCH
                if (HB_LIQUID_FINE && lit > 0.0h && lit < 5.0h)
                {
                    UNITY_LOOP
                    for (int tap = 4; tap < HB_CAUSTIC_TAPS; tap++)
                        lit += HB_CausticShade(floorWS, s_CausticDisk[tap]);

                    lit *= 1.0h / (HB_CAUSTIC_TAPS + 1.0h);
                }
                else
                {
                    lit *= 0.2h;
                }
            }

            caustics *= lit;
        #endif

            half3 glint = lerp(half3(1.0h, 1.0h, 1.0h), tone, saturate(murk * HB_CAUSTIC_TINT));

            inscatter += HB_LiquidSunColour() * glint * (caustics * (1.0h - murk));
        }
    }

    half3 spotColour = HB_LiquidFoamColour() * (skyLight + sunLight * saturate(dot(surface.normalWS,
                                                                            HB_LiquidSunDirection()) * 0.5h + 0.5h));

    half foamed = HB_LiquidCover(spots, veil);
    half crust = HB_LiquidCrust(surface.uv);

    spotColour = lerp(HB_LiquidCrustColour() * (skyLight + sunLight * saturate(dot(surface.normalWS,
                                                                         HB_LiquidSunDirection()) * 0.5h + 0.5h)),
                      spotColour, saturate(foamed / max(max(foamed, crust), 1e-3h)));

    UNITY_BRANCH
    if (spots + veil + crust > 0.0h)
        spotColour = HB_FogSurface(spotColour, HB_ResolveFog(input.positionWS, screenUV, false));

    UNITY_BRANCH
    if (!front)
        spotColour = lerp(spotColour, tone * lit, murk);

    half fade = half(s_Fade);
    half cover = max(foamed, crust) * fade;
    half3 colour = lerp(HB_FogLight(reflection + inscatter, fogAmount) * fade, spotColour, cover);

    half4 white = half4(1.0h, 1.0h, 1.0h, 1.0h);

    half3 through = PositivePow(clamp(HB_LiquidShoreColour(), 1e-3h, 1.0h),
                                half(min(path / HB_LIQUID_TINT_DEPTH, 64.0)))
                  * ((1.0h - murk) * window);

    through = lerp(white.rgb, through, fade);

    half4 tint = half4(lerp(white.rgb, through, clear), 1.0h);

    half blur = saturate(HB_LiquidRefractionBlur() + murk * HB_LIQUID_MURK_BLUR) * clear
              * saturate(murk * HB_LIQUID_EDGE_SOFT) * fade;
    half3 open = tint.rgb * (1.0h - cover);

    UNITY_BRANCH
    if (max(open.r, max(open.g, open.b)) > HB_LIQUID_SOLID)
    {
        float2 bend = HB_LiquidBend(input.positionWS, screenUV, surface, viewWS, thickness,
                                    clear * (1.0h - spots) * fade);

        colour += HB_LiquidBackdrop(input.positionWS, screenUV + bend, blur, float2(pixel)) * open;
    }

    return half4(colour, 1.0h);
}

#elif defined(HB_LIQUID_UNDERWATER)

#define HB_UNDER_SPACING     0.25
#define HB_UNDER_WOBBLE      0.006
#define HB_UNDER_CAUSTICS    1.5h
#define HB_UNDER_SHAFT_STEPS 8.0
#define HB_UNDER_SHAFT_RANGE 24.0
#define HB_UNDER_SHAFT_FADE  10.0
#define HB_UNDER_SHAFT_GAIN  0.35h
#define HB_UNDER_SHAFT_LOD   2.0
#define HB_UNDER_BUBBLE_STEPS 16
#define HB_UNDER_BUBBLE_DENSITY 1.5
#define HB_UNDER_BUBBLE_SHARE 0.25
#define HB_UNDER_BUBBLE_SPREAD 0.5
#define HB_UNDER_BUBBLE_NEAR 0.3
#define HB_UNDER_BUBBLE_NUDGE 1e-3
#define HB_UNDER_BUBBLE_LEAST 1e-6
#define HB_UNDER_BUBBLE_SIZE 0.025
#define HB_UNDER_BUBBLE_FAR 6.0
#define HB_UNDER_DARK        18.0
#define HB_UNDER_BELOW       0.55h
#define HB_UNDER_ABOVE       1.2h
#define HB_UNDER_SUN         0.6h
#define HB_UNDER_BUBBLE_RISE 0.12
#define HB_UNDER_BUBBLE_QUICK 1.6
#define HB_UNDER_BUBBLE_SWAY 0.08
#define HB_UNDER_BUBBLE_EDGE 0.12
#define HB_UNDER_BUBBLE_RIM 0.9
#define HB_UNDER_BUBBLE_BODY 0.12
#define HB_UNDER_BUBBLE_SPARK 1.4
#define HB_UNDER_BUBBLE_GLINT float2(-0.38, 0.42)
#define HB_UNDER_BUBBLE_GLINT_SHARP 4.0
#define HB_UNDER_AHEAD       12.0
#define HB_UNDER_SEAM        2.0
#define HB_UNDER_SEAM_DARK   0.6h

float _HB_UnderLevel;
float4 _HB_UnderSlope;

float HB_UnderPlane(float2 positionXZ, float surfaceY)
{
    return surfaceY + dot(_HB_UnderSlope.xy, positionXZ - _HB_UnderSlope.zw);
}

float HB_UnderSurface(float2 positionXZ)
{
    float2 shore;

    float level = _HB_UnderLevel + HB_LiquidRise(positionXZ);

    float still = _WaterArea.z > 0.0 ? HB_LevelRead(_WaterWeightMap, _WaterArea, positionXZ).a : 0.0;

    return level + HB_LiquidDisplace(float3(positionXZ.x, level, positionXZ.y), HB_UNDER_SPACING, shore).y
                 * (1.0 - still);
}

float3 HB_UnderCorner(float2 node, float level, float spacing)
{
    float2 shore;
    float still = _WaterArea.z > 0.0 ? HB_LevelRead(_WaterWeightMap, _WaterArea, node).a : 0.0;
    float3 moved = HB_LiquidDisplace(float3(node.x, level, node.y), spacing, shore) * (1.0 - still);

    return float3(node + moved.xz, level + moved.y);
}

float HB_UnderSurfaceMesh(float2 positionXZ)
{
    float2 shore;

    float seat = _HB_UnderLevel + _HB_LiquidSeat;
    float rise = HB_LiquidRise(positionXZ);
    float level = _HB_UnderLevel + (HB_LiquidTiled() || abs(rise) > HB_LIQUID_LEVEL ? rise : 0.0);
    float still = _WaterArea.z > 0.0 ? HB_LevelRead(_WaterWeightMap, _WaterArea, positionXZ).a : 0.0;
    float cell = HB_LiquidTiled() ? _WaterGrid.x : HB_LiquidCell(seat);
    float lift = HB_LiquidTiled() ? 0.0 : HB_LiquidLift(seat);
    float spacing = HB_LiquidSpacing(cell, lift, positionXZ);
    float2 rest = positionXZ;
    float height = level;

    UNITY_LOOP
    for (int step = 0; step < HB_UNDER_SOLVE_STEPS; step++)
    {
        float3 moved = HB_LiquidDisplace(float3(rest.x, level, rest.y), spacing, shore) * (1.0 - still);

        rest = positionXZ - moved.xz;
        height = level + moved.y;
    }

    UNITY_BRANCH
    if (HB_LiquidTiled())
        return height;

    float2 grid = rest / cell;
    float2 base = floor(grid);
    float2 inside = grid - base;
    bool upper = inside.x + inside.y > 1.0;
    float2 apex = upper ? base + 1.0 : base;
    float2 weights = upper ? 1.0 - inside : inside;

    float2 across = upper ? base : base + 1.0;
    float3 corners[3];

    UNITY_LOOP
    for (int corner = 0; corner < 3; corner++)
    {
        float2 node = corner == 1 ? float2(across.x, apex.y) : corner == 2 ? float2(apex.x, across.y) : apex;

        corners[corner] = HB_UnderCorner(node * cell, level, spacing);
    }

    float2 edgeX = corners[1].xy - corners[0].xy;
    float2 edgeZ = corners[2].xy - corners[0].xy;
    float2 offset = positionXZ - corners[0].xy;
    float area = edgeX.x * edgeZ.y - edgeX.y * edgeZ.x;

    if (abs(area) > 1e-8)
        weights = float2(offset.x * edgeZ.y - offset.y * edgeZ.x, edgeX.x * offset.y - edgeX.y * offset.x) / area;

    return corners[0].z + (corners[1].z - corners[0].z) * weights.x + (corners[2].z - corners[0].z) * weights.y;
}

half HB_UnderShade(float3 positionWS)
{
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    return MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
#else
    return 1.0h;
#endif
}

half HB_UnderShaft(float3 positionWS)
{
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    half4 crisp = GetMainLightShadowParams();

    crisp.y = SOFT_SHADOW_QUALITY_OFF;

    return MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS), crisp, GetMainLightShadowSamplingData());
#else
    return 1.0h;
#endif
}

half HB_UnderShafts(float3 eye, float3 heading, float path, float surfaceY, half3 sunDirection, float2 pixel)
{
    half shafts = 0.0h;

    UNITY_BRANCH
    if (HB_LiquidUnderShafts() > 0.0h && sunDirection.y > 0.05h && _HB_CausticParams.y > 0.5 && !HB_LIQUID_LOW)
    {
        float span = min(path, HB_UNDER_SHAFT_RANGE);
        float steps = HB_LIQUID_FINE ? HB_UNDER_SHAFT_STEPS : HB_UNDER_SHAFT_STEPS * 0.5;
        float stride = span / steps;
        float start = InterleavedGradientNoise(pixel, 0) * stride;
        float tiles = _HB_CausticParams.x / max(float(_CausticScale), 0.05);
        float still = 1.0 / (1.0 + HB_CAUSTIC_SHARP);
        float gathered = 0.0;

        UNITY_LOOP
        for (int index = 0; index < (int)steps; index++)
        {
            float along = start + stride * index;
            float3 there = eye + heading * along;
            float under = HB_UnderPlane(there.xz, surfaceY) - there.y;

            if (under <= 0.0)
                continue;

            float2 lit = there.xz + float2(sunDirection.xz) * (under / float(sunDirection.y));
            float focus = HB_CausticFocus(lit * tiles, HB_CAUSTIC_DEEP * HB_CAUSTIC_FOCUS, HB_UNDER_SHAFT_LOD);

            gathered += max(focus - still, 0.0) * float(HB_UnderShaft(there))
                      * exp(-(along + under) / HB_UNDER_SHAFT_FADE);
        }

        shafts = half(gathered * stride) * HB_LiquidUnderShafts() * HB_UNDER_SHAFT_GAIN;
    }

    return shafts;
}

half HB_UnderBubbles(float3 eye, float3 heading, float away)
{
    half bubbles = 0.0h;

    UNITY_BRANCH
    if (HB_LiquidUnderBubbles() > 0.0h)
    {
        int steps = HB_LIQUID_LOW ? HB_UNDER_BUBBLE_STEPS / 2 : HB_UNDER_BUBBLE_STEPS;
        float range = HB_LIQUID_LOW ? HB_UNDER_BUBBLE_FAR * 0.5 : HB_UNDER_BUBBLE_FAR;
        float limit = min(away, range) * HB_UNDER_BUBBLE_DENSITY;
        float3 right = UNITY_MATRIX_V[0].xyz;
        float3 up = UNITY_MATRIX_V[1].xyz;
        float3 origin = eye * HB_UNDER_BUBBLE_DENSITY;
        float2 home = floor(origin.xz);
        float2 seat = origin.xz - home;
        float2 risen = origin.y - _Time.y * HB_UNDER_BUBBLE_RISE * HB_UNDER_BUBBLE_DENSITY
                     * float2(1.0, HB_UNDER_BUBBLE_QUICK);
        float2 floors = floor(risen);
        float2 heights = risen - floors;
        float3 slope = lerp(HB_UNDER_BUBBLE_LEAST, heading, step(HB_UNDER_BUBBLE_LEAST, abs(heading)));
        float3 ahead = step(0.0, slope);
        float march = 0.0;

        UNITY_LOOP
        for (int index = 0; index < steps; index++)
        {
            if (march > limit)
                break;

            float2 walk = seat + heading.xz * march;
            float2 column = floor(walk);
            float2 block = home + column;
            bool fast = frac((block.x + block.y) * 0.5) > 0.25;
            float quick = fast ? HB_UNDER_BUBBLE_QUICK : 1.0;
            float level = (fast ? heights.y : heights.x) + heading.y * march;
            float storey = floor(level);
            float3 local = float3(walk.x - column.x, level - storey, walk.y - column.y);
            float3 exits = (ahead - local) / slope;
            float here = march;

            march += min(exits.x, min(exits.y, exits.z)) + HB_UNDER_BUBBLE_NUDGE;

            float4 chance = HB_LiquidHash4(block + ((fast ? floors.y : floors.x) + storey) * 17.31);

            if (chance.w > float(HB_LiquidUnderBubbles()) * HB_UNDER_BUBBLE_SHARE)
                continue;

            float sway = _Time.y * (1.0 + chance.z) + chance.x * 6.0;
            float3 centre = 0.5 + (chance.xyz - 0.5) * HB_UNDER_BUBBLE_SPREAD
                          + float3(sin(sway), 0.0, cos(sway * 0.7)) * (HB_UNDER_BUBBLE_SWAY * quick);
            float3 toward = centre - local;
            float along = dot(toward, heading);
            float3 across = toward - heading * along;
            float radius = HB_UNDER_BUBBLE_SIZE * HB_UNDER_BUBBLE_DENSITY * quick
                         * (0.35 + 1.3 * chance.z * chance.z);
            float span = length(across) / radius;
            float reach = (here + along) / HB_UNDER_BUBBLE_DENSITY;

            if (span >= 1.0 || reach >= away)
                continue;

            float2 disc = float2(dot(across, right), dot(across, up)) / radius;
            float edge = saturate((1.0 - span) / HB_UNDER_BUBBLE_EDGE);
            float rim = span * span * span * span * HB_UNDER_BUBBLE_RIM;
            float spark = saturate(1.0 - length(disc + HB_UNDER_BUBBLE_GLINT) * HB_UNDER_BUBBLE_GLINT_SHARP);
            float fade = saturate(1.0 - reach / range) * saturate(reach / HB_UNDER_BUBBLE_NEAR - 1.0);
            float look = (HB_UNDER_BUBBLE_BODY + rim) * edge + spark * spark * HB_UNDER_BUBBLE_SPARK;

            bubbles = max(bubbles, half(look * fade * fade));
        }
    }

    return bubbles;
}

TEXTURE2D(_HB_UnderLine);
float4 _HB_UnderLineSize;

float HiddenBullUnderwaterLineFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    UNITY_BRANCH
    if (HB_LiquidLayered())
        s_Mix = HB_LiquidLayerWeights(GetCameraPositionWS().xz, HB_LAYER_ROUGH * 0.5);

    float2 uv = floor(input.texcoord * _HB_UnderLineSize.xy) / max(_HB_UnderLineSize.xy - 1.0, 1.0);
    float3 nearWS = ComputeWorldSpacePosition(uv, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);

    return HB_UnderSurfaceMesh(nearWS.xz) - nearWS.y;
}

half4 HiddenBullUnderwaterFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    UNITY_BRANCH
    if (HB_LiquidLayered())
        s_Mix = HB_LiquidLayerWeights(GetCameraPositionWS().xz, HB_LAYER_ROUGH * 0.5);

    float2 uv = input.texcoord;
    half3 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
    float3 eye = GetCameraPositionWS();
    float3 nearWS = ComputeWorldSpacePosition(uv, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
    float2 lineUV = (uv * (_HB_UnderLineSize.xy - 1.0) + 0.5) * _HB_UnderLineSize.zw;
    float sunk = SAMPLE_TEXTURE2D_LOD(_HB_UnderLine, sampler_LinearClamp, lineUV, 0).r;

    float edge = max(fwidth(sunk), 1e-5);
    half mask = half(saturate(sunk / edge + 0.5)) * (HB_LiquidKept(nearWS.xz) > 0.5 ? 1.0h : 0.0h);
    half seam = half(saturate(1.0 - abs(sunk / edge) / HB_UNDER_SEAM)) * HB_UNDER_SEAM_DARK;

    UNITY_BRANCH
    if (mask <= 0.0h)
        return half4(scene * (1.0h - seam), 1.0h);

    float2 wobble = float2(sin(uv.y * 37.0 + _Time.y * 1.7), cos(uv.x * 29.0 + _Time.y * 1.3))
                  * (HB_UNDER_WOBBLE * float(HB_LiquidUnderDistortion()) * float(mask));
    float2 bent = saturate(uv + wobble);

    half3 colour = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, bent).rgb;
    float3 positionWS = ComputeWorldSpacePosition(bent, SampleSceneDepth(bent), UNITY_MATRIX_I_VP);
    float3 ray = positionWS - eye;
    float away = length(ray);
    float3 heading = ray / max(away, 1e-4);

    float surfaceY = HB_UnderSurface(eye.xz);
    float climb = heading.y - dot(_HB_UnderSlope.xy, heading.xz);
    float leave = climb > 1e-3 ? max(surfaceY - eye.y, 0.0) / climb : 1e9;
    float path = min(away, leave);

    UNITY_BRANCH
    if (HB_LiquidLayered())
    {
        s_Mix = HB_LiquidLayerWeights(eye.xz + heading.xz * min(path, HB_UNDER_AHEAD), HB_LAYER_ROUGH * 0.5);
    }

    half4 water = HB_LiquidGradient(path);
    half3 sunDirection = HB_LiquidSunDirection();
    half3 sunLight = HB_LiquidSunColour();
    half3 skyLight = (HB_GradientAmbient(half3(0.0h, 1.0h, 0.0h)) + HB_GradientAmbient(half3(1.0h, 0.0h, 0.0h)))
                   * 0.5h;
    float sunken = max(surfaceY - eye.y, 0.0);
    half upward = half(saturate(heading.y * 0.5 + 0.5));
    half dim = half(exp(-sunken / HB_UNDER_DARK));
    half3 light = (skyLight + sunLight * 0.5h) * (dim * lerp(HB_UNDER_BELOW, HB_UNDER_ABOVE, upward))
                + sunLight * (dim * HB_UNDER_SUN * PositivePow(saturate(dot(half3(heading), sunDirection)), 6.0h))
                + HB_LiquidGlow();

    UNITY_BRANCH
    if (away < leave)
    {
        half caustics = HB_LiquidCaustics(positionWS, HB_UnderPlane(positionWS.xz, surfaceY), sunDirection);

        UNITY_BRANCH
        if (caustics > 0.0h)
            colour *= 1.0h + caustics * HB_UnderShade(positionWS) * HB_UNDER_CAUSTICS;
    }

    half3 through = PositivePow(clamp(HB_LiquidShoreColour(), 1e-3h, 1.0h),
                                half(min(path / HB_LIQUID_TINT_DEPTH, 64.0)))
                  * (1.0h - water.a);

    colour = colour * through + water.rgb * (water.a * light);
    colour += sunLight * water.rgb * HB_UnderShafts(eye, heading, path, surfaceY, sunDirection, input.positionCS.xy);
    colour += (skyLight + sunLight * 0.5h + HB_LiquidGlow())
            * (HB_UnderBubbles(eye, heading, away) * HB_LiquidUnderBubbles() * dim);

    return half4(lerp(scene, colour, mask) * (1.0h - seam), 1.0h);
}

#elif defined(HB_LIQUID_CUT_DEPTH)

half4 HiddenBullLiquidCutDepthFragment(LiquidVaryings input) : SV_Target
{
    return half4(0.0h, 0.0h, 0.0h, 0.0h);
}

#elif defined(HB_LIQUID_CUT_MASK)

float4 _HB_CutWrite;
float4 _HB_CutEye;
float4 _HB_CutForward;

struct LiquidCutAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
};

struct LiquidCutVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
};

LiquidCutVaryings HiddenBullLiquidCutVertex(LiquidCutAttributes input)
{
    LiquidCutVaryings output;

    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);

    return output;
}

#ifdef HB_LIQUID_CUT_DRY

float4 HiddenBullLiquidCutFragment(LiquidCutVaryings input) : SV_Target
{
    float3 across = cross(ddx(input.positionWS), ddy(input.positionWS));
    float3 outward = dot(across, input.normalWS) < 0.0 ? -across : across;

#ifdef HB_LIQUID_CUT_ROOF
    clip(-dot(outward, _HB_CutForward.xyz));

    return float4(HB_DRY_HEIGHT + input.positionWS.y, 0.0, 0.0, 0.0);
#else
    clip(dot(outward, _HB_CutForward.xyz));

#ifdef HB_LIQUID_CUT_FLOOR
    return float4(HB_DRY_HEIGHT - input.positionWS.y, 0.0, 0.0, 0.0);
#else
    return _HB_CutWrite;
#endif
#endif
}

#else

float4 HiddenBullLiquidCutFragment(LiquidCutVaryings input) : SV_Target
{
    float3 across = cross(ddx(input.positionWS), ddy(input.positionWS));
    float3 outward = dot(across, input.normalWS) < 0.0 ? -across : across;
    float3 ray = _HB_CutEye.w > 0.5 ? input.positionWS - _HB_CutEye.xyz : _HB_CutForward.xyz;

    return _HB_CutWrite * (dot(outward, ray) > 0.0 ? 1.0 : -1.0);
}

#endif

#endif

#endif
