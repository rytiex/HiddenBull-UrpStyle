#ifndef HB_GRASS_PASS_INCLUDED
#define HB_GRASS_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassInput.hlsl"

#if defined(HB_GRASS_FORWARD)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleAmbient.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSky.hlsl"
#endif

StructuredBuffer<uint3> _HB_GrassBlades;
StructuredBuffer<int4> _HB_GrassSlots;
StructuredBuffer<HBGrassDrawn> _HB_GrassList;

#if defined(_HB_GRASS_FLOWERS)
TEXTURE2D_ARRAY(_HB_FlowerHeads);
SAMPLER(sampler_HB_FlowerHeads);

float4 _HB_LayerFlower[16];
#endif

float4 _HB_LayerGrassTip[16];
float4 _HB_LayerGrassEdge[16];
float _HB_LayerWetGloss[16];
float4 _HB_GrassStyle;
float _HB_ShelterPass;

#define HB_GRASS_STYLE_WRAP         _HB_GrassStyle.x
#define HB_GRASS_STYLE_SOFTNESS     _HB_GrassStyle.y
#define HB_GRASS_STYLE_HALF_TONE    _HB_GrassStyle.z
#define HB_GRASS_WET_DARKEN   0.3h
#define HB_GRASS_SNOW_COLOUR  half3(0.5h, 0.54h, 0.6h)
#define HB_GRASS_SNOW_SHRINK  0.6
#define HB_GRASS_PRESS_SHORT  0.7
#define HB_GRASS_PRESS_DARK   0.5h
#define HB_GRASS_SNOW_ROOT    1.35h
#define HB_GRASS_SNOW_TIP     0.35h
#define HB_FLOWER_SNOW_FROM   0.3
#define HB_FLOWER_SNOW_GONE   0.8
#define HB_GRASS_CUTOFF       0.5h
#define HB_GRASS_DETAIL_SCALE 2.0h
#define HB_GRASS_TIP_START    0.35h
#define HB_GRASS_ROOT_SHADE   0.85h
#define HB_GRASS_ROOT_BLEND   0.4h
#define HB_GRASS_CALM_RINGS   8.0
#define HB_GRASS_FILM_SPEC    0.04h
#define HB_GRASS_FILM_GRAZE   0.89h
#define HB_GRASS_FILM_GLOSS   0.7225h
#define HB_GRASS_FILM_HORIZON 0.04h
#define HB_GRASS_IDLE         0.02
#define HB_GRASS_LEAN_QUICK   0.06
#define HB_GRASS_SWEEP        0.5h
#define HB_GRASS_SINK         0.06
#define HB_FLOWER_SWAY        0.5
#define HB_FLOWER_TILT        0.3
#define HB_FLOWER_TURN        0.55
#define HB_FLOWER_STEM        0.07
#define HB_FLOWER_LEAN        0.12
#define HB_FLOWER_LOW         0.8
#define HB_FLOWER_HIGH        1.05
#define HB_FLOWER_SMALL       0.6
#define HB_FLOWER_REACH       0.5
#define HB_FLOWER_FADE        0.2
#define HB_BLADE_VERTICES     7u
#define HB_BLADE_MOST         8.0
#define HB_BLADE_MID          5.0
#define HB_BLADE_LEAST        3.0
#define HB_BLADE_WIDTH        0.1
#define HB_BLADE_SPREAD       2.0
#define HB_BLADE_WIDEN        1.0
#define HB_BLADE_THICKEN      0.5
#define HB_BLADE_EDGE_ON      0.1
#define HB_BLADE_SHORT        0.7
#define HB_BLADE_FEWER_WIDE   0.13
#define HB_BLADE_BEND_LOW     1.0
#define HB_BLADE_BEND_HIGH    3.2
#define HB_BLADE_BEND_LEAST   0.001
#define HB_BLADE_FLOW_SIZE    8.0
#define HB_BLADE_FLOW_TURNS   9.4
#define HB_BLADE_FLOW_SPREAD  1.2
#define HB_BLADE_EVEN        float2(0.7548777, 0.5698403)
#define HB_BLADE_EVEN_SHIFT  float2(17.3, 41.7)
#define HB_BLADE_TURN         0.618034
#define HB_BLADE_TURN_LOOSE   0.25
#define HB_BLADE_RIB          0.2h
#define HB_BLADE_LEAN_LOW     0.5
#define HB_BLADE_LEAN_HIGH    0.9
#define HB_BLADE_LEAN_MOST    0.12
#define HB_BLADE_ABOVE_FROM   0.4
#define HB_BLADE_ABOVE_TO     0.85
#define HB_BLADE_ABOVE_CALM   0.6
#define HB_BLADE_FLUTTER_MOST 0.15
#define HB_BLADE_TONE         0.2
#define HB_BLADE_ROUND        0.12
#define HB_DEBUG_GRASS_CELLS  16
#define HB_DEBUG_WEATHER_GUST 17

#if defined(_HB_GRASS_FLOWERS)
#define HB_GRASS_CLIP(cover) clip(cover)
#else
#define HB_GRASS_CLIP(cover)
#endif

static const float2 HB_GRASS_CORNERS[6] =
{
    float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
    float2(0.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0)
};

struct HBGrassVaryings
{
    precise float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float4 uv         : TEXCOORD1;
    float3 normalWS   : TEXCOORD2;
    float3 ground     : TEXCOORD3;
    float2 gust       : TEXCOORD4;
    nointerpolation uint2 data : TEXCOORD5;
    float2 weather    : TEXCOORD6;
};

#if defined(_HB_GRASS_FLOWERS)

HBGrassVaryings HB_GrassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    HBGrassDrawn drawn = _HB_GrassList[instanceID];
    uint index = drawn.index;
    uint slot = index / HB_GRASS_PER_SLOT;
    uint local = index - slot * HB_GRASS_PER_SLOT;
    int4 owner = _HB_GrassSlots[slot];
    HBGrassBlade blade = HB_GrassUnpack(_HB_GrassBlades[index], owner.xy, local, (uint)owner.z);
    HBGrassMotion motion = HB_GrassMotion(drawn);

    float gap = distance(blade.root, HB_GRASS_CAMERA);
    float4 flower = _HB_LayerFlower[blade.layer];
    float reach = HB_GRASS_DISTANCE * HB_FLOWER_REACH;
    float size = flower.y * lerp(HB_FLOWER_SMALL, 1.0, blade.rand)
               * saturate((reach - gap) / (reach * HB_FLOWER_FADE))
               * (1.0 - smoothstep(HB_FLOWER_SNOW_FROM, HB_FLOWER_SNOW_GONE, motion.snow));
    float height = blade.height * lerp(HB_FLOWER_LOW, HB_FLOWER_HIGH, blade.rand)
                 * (1.0 - HB_GRASS_SNOW_SHRINK * motion.snow) * (1.0 - HB_GRASS_PRESS_SHORT * motion.press);
    uint card = vertexID / 6u;
    float2 corner = HB_GRASS_CORNERS[vertexID - card * 6u];

    float turn = blade.rand * TWO_PI * 7.0;
    float3 up = float3(0.0, 1.0, 0.0);
    float3 away = float3(cos(turn), 0.0, sin(turn));
    float3 toCamera = normalize(HB_GRASS_CAMERA - blade.root);

    float3 sway = motion.sway * (HB_FLOWER_SWAY * height);
    float3 centre = blade.root + up * height + away * (HB_FLOWER_LEAN * height) + sway;
    float3 positionWS;
    float2 uv;

    if (card == 0u)
    {
        float3 side = normalize(cross(up, toCamera) + float3(1e-4, 0.0, 0.0));
        float along = corner.y * corner.y;

        positionWS = lerp(blade.root, centre, float3(along, corner.y, along))
                   + side * ((corner.x - 0.5) * size * HB_FLOWER_STEM);
        uv = float2(-1.0, corner.y);
    }
    else
    {
        float3 across = float3(-away.z, 0.0, away.x);
        float3 face = normalize(up + away * HB_FLOWER_TILT + toCamera * HB_FLOWER_TURN);
        float3 wide = normalize(across - face * dot(across, face));
        float3 tall = cross(face, wide);

        positionWS = centre + (wide * (corner.x - 0.5) + tall * (corner.y - 0.5)) * size;
        uv = float2((float(blade.bloom - 1u) + corner.x) / max(flower.w, 1.0), corner.y);
    }

    HBGrassVaryings output;
    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.uv = float4(uv, 1.0, gap);
    output.weather = float2(motion.wet, motion.snow);
    output.normalWS = blade.normal;
    output.ground = blade.ground;
    output.gust = float2(motion.press, motion.streak);
    output.data = uint2(blade.layer, 0u);

    if (_HB_ShelterPass > 0.5)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);

    return output;
}

half4 HB_GrassTuft(HBGrassVaryings input, out half cover)
{
    cover = 1.0h;

    if (input.uv.x < 0.0)
        return half4(input.ground, 1.0h);

    float slice = _HB_LayerFlower[input.data.x].z;
    half4 head = half4(SAMPLE_TEXTURE2D_ARRAY(_HB_FlowerHeads, sampler_HB_FlowerHeads, input.uv.xy, slice));

    cover = head.a - HB_GRASS_CUTOFF;
    return head;
}

#else

static const float HB_BLADE_ROWS[7] = { 0.0, 0.0, 0.35, 0.35, 0.7, 0.7, 1.0 };

HBGrassVaryings HB_GrassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    HBGrassVaryings output = (HBGrassVaryings)0;
    HBGrassDrawn drawn = _HB_GrassList[instanceID];
    uint index = drawn.index;
    uint slot = index / HB_GRASS_PER_SLOT;
    uint local = index - slot * HB_GRASS_PER_SLOT;
    int4 owner = _HB_GrassSlots[slot];
    int2 cell = owner.xy;
    HBGrassBlade blade = HB_GrassUnpack(_HB_GrassBlades[index], cell, local, (uint)owner.z);

    uint leaf = vertexID / HB_BLADE_VERTICES;
    uint joint = vertexID - leaf * HB_BLADE_VERTICES;
    float gap = distance(blade.root, HB_GRASS_CAMERA);
    float outward = saturate((gap - HB_GRASS_CLOSE) / (HB_GRASS_RING - HB_GRASS_CLOSE));
    float leaves = gap < HB_GRASS_CLOSE ? lerp(HB_BLADE_MOST, HB_BLADE_MID, gap / HB_GRASS_CLOSE)
                                        : lerp(HB_BLADE_MID, HB_BLADE_LEAST, outward);
    float pixels = HB_GrassPixels(blade.height, gap);
    float clear = saturate((pixels - HB_GRASS_MID_PIXELS) / (HB_GRASS_CLOSE_PIXELS - HB_GRASS_MID_PIXELS));
    float sharp = pixels < HB_GRASS_CLOSE_PIXELS ? lerp(HB_BLADE_LEAST, HB_BLADE_MID, clear)
                                                 : lerp(HB_BLADE_MID, HB_BLADE_MOST,
                                                        saturate(pixels / HB_GRASS_CLOSE_PIXELS - 1.0));

    leaves = min(leaves, sharp);

    float grown = saturate(leaves - float(leaf));

    if (grown <= 0.0 || _HB_ShelterPass > 0.5)
        return output;

    uint2 dice = HB_GrassHash(uint2(index, leaf));
    float4 roll = float4(dice.x & 65535u, dice.x >> 16u, dice.y & 65535u, dice.y >> 16u) / 65535.0;
    HBGrassMotion motion = HB_GrassMotion(drawn);
    float height = blade.height * lerp(HB_BLADE_SHORT, 1.0, roll.z) * grown * motion.fade
                 * (1.0 - HB_GRASS_SNOW_SHRINK * motion.snow) * (1.0 - HB_GRASS_PRESS_SHORT * motion.press);

    float2 even = frac(blade.rand * HB_BLADE_EVEN_SHIFT + float(leaf) * HB_BLADE_EVEN);
    float2 spot = (even - 0.5) * (HB_GRASS_SPACING * HB_GrassScale(blade.level) * HB_BLADE_SPREAD);
    float3 rootWS = blade.root + float3(spot.x, -dot(spot, blade.normal.xz) / max(blade.normal.y, 0.3), spot.y);
    float3 up = float3(0.0, 1.0, 0.0);
    float yaw = (blade.rand + float(leaf) * HB_BLADE_TURN + roll.w * HB_BLADE_TURN_LOOSE) * TWO_PI;
    float3 across = float3(cos(yaw), 0.0, sin(yaw));
    float3 right = normalize(cross(up, HB_GRASS_CAMERA - rootWS) + float3(1e-4, 0.0, 0.0));
    float above = (HB_GRASS_CAMERA.y - rootWS.y) / max(gap, 1e-3);
    float over = smoothstep(HB_BLADE_ABOVE_FROM, HB_BLADE_ABOVE_TO, above);
    float facing = dot(across, right);
    float turned = facing / (abs(facing) + HB_BLADE_EDGE_ON) * (1.0 - abs(facing));
    float3 broad = across + right * (turned * HB_BLADE_THICKEN * (1.0 - over));

    float heading = HB_WindNoise(blade.root.xz / HB_BLADE_FLOW_SIZE) * HB_BLADE_FLOW_TURNS
                  + (roll.x - 0.5) * HB_BLADE_FLOW_SPREAD;
    float3 ahead = float3(cos(heading), 0.0, sin(heading));
    float3 lay = float3(-across.z, 0.0, across.x);

    lay *= dot(lay, ahead) < 0.0 ? -1.0 : 1.0;

    float lean = min(lerp(HB_BLADE_LEAN_LOW, HB_BLADE_LEAN_HIGH, roll.y) * height, HB_BLADE_LEAN_MOST);
    float rise = HB_BLADE_ROWS[joint];
    float edge = joint == HB_BLADE_VERTICES - 1u ? 0.0 : (joint & 1u) == 0u ? -1.0 : 1.0;
    float bend = max(_HB_LayerGrassEdge[blade.layer].z * lerp(HB_BLADE_BEND_LOW, HB_BLADE_BEND_HIGH, roll.z),
                     HB_BLADE_BEND_LEAST);
    float climb = sin(bend * rise) / bend;
    float reach = (1.0 - cos(bend * rise)) / bend;
    float wide = blade.width * HB_BLADE_WIDTH * (1.0 + HB_BLADE_WIDEN * gap / HB_GRASS_RING)
               * (1.0 + (HB_BLADE_MOST - leaves) * HB_BLADE_FEWER_WIDE) * (1.0 - rise * rise * rise);
    float flutter = sin(_Time.y * (2.3 + roll.w * 1.4) + roll.w * 19.0)
                  * (HB_GRASS_IDLE + HB_GRASS_LEAN_QUICK * HB_WIND)
                  * min(HB_BLADE_FLUTTER_MOST / max(height, 1e-3), 1.0);
    float3 sway = motion.sway + ahead * flutter;

    float3 positionWS = rootWS + broad * (edge * wide) + up * ((climb - HB_GRASS_SINK) * height)
                      + ahead * (reach * height) + lay * (lean * rise) + sway * (rise * rise * height);
    float tone = 0.0;

#if defined(HB_GRASS_FORWARD)
    float2 sun = normalize(_MainLightPosition.xz + 1e-4);
    float toneTurn = roll.y * TWO_PI;

    tone = (cos(toneTurn) * sun.x + sin(toneTurn) * sun.y) * HB_BLADE_TONE
         + dot(across.xz, sun) * edge * HB_BLADE_ROUND;
    tone *= saturate(pixels / HB_GRASS_MID_PIXELS) * (1.0 - over * HB_BLADE_ABOVE_CALM);
#endif

    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.uv = float4(edge, tone, rise, gap);
    output.weather = float2(motion.wet, motion.snow);
    output.normalWS = blade.normal;
    output.ground = blade.ground;
    output.gust = float2(motion.press, motion.streak);
    output.data = uint2(blade.layer, (uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663) ^ (blade.level * 83492791u));

    return output;
}

half4 HB_GrassTuft(HBGrassVaryings input, out half cover)
{
    half rib = 1.0h - HB_BLADE_RIB * half(input.uv.x * input.uv.x);

    cover = 1.0h;
    return half4(rib, rib, rib, 1.0h) * 0.5h;
}

#endif

#if defined(HB_GRASS_FORWARD)

half HB_GrassShadow(float3 positionWS)
{
    half shadow = 1.0h;

#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    shadow = MainLightRealtimeShadow(ComputeScreenPos(TransformWorldToHClip(positionWS)));
#elif defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);

    UNITY_BRANCH
    if (!BEYOND_SHADOW_FAR(shadowCoord))
    {
        shadow = half(SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare,
                                              shadowCoord.xyz));
        shadow = LerpWhiteTo(shadow, GetMainLightShadowParams().x);
    }

    shadow = lerp(shadow, 1.0h, GetMainLightShadowFade(positionWS));
#endif

    return shadow;
}

half3 HB_GrassExtraLight(Light light, half3 normalWS, half wrap, half inverseWrap)
{
    half lambert = saturate((dot(normalWS, light.direction) + wrap) * inverseWrap);
    half tone = HB_ToneBands(lambert, wrap * inverseWrap, half(HB_GRASS_STYLE_HALF_TONE),
                             half(HB_GRASS_STYLE_SOFTNESS));

    return light.color * (light.distanceAttenuation * tone);
}

half3 HB_GrassExtraLights(float3 positionWS, float2 pixel, half3 normalWS, half wrap, half inverseWrap)
{
    half3 lights = half3(0.0h, 0.0h, 0.0h);

#if USE_CLUSTER_LIGHT_LOOP
    InputData inputData = (InputData)0;

    inputData.positionWS = positionWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(pixel);

    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
                lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        lights += HB_GrassExtraLight(GetAdditionalPerObjectLight(lightIndex, positionWS), normalWS, wrap, inverseWrap);
    }

    LIGHT_LOOP_BEGIN(0)
        lights += HB_GrassExtraLight(GetAdditionalPerObjectLight(lightIndex, positionWS), normalWS, wrap, inverseWrap);
    LIGHT_LOOP_END
#elif defined(_ADDITIONAL_LIGHTS)
    uint count = (uint)_AdditionalLightsCount.x;

    for (uint lightIndex = 0u; lightIndex < count; lightIndex++)
        lights += HB_GrassExtraLight(GetAdditionalPerObjectLight(lightIndex, positionWS), normalWS, wrap, inverseWrap);
#endif

    return lights;
}

half4 HB_GrassFragment(HBGrassVaryings input) : SV_Target
{
    half cover;
    half4 tuft = HB_GrassTuft(input, cover);

    HB_GRASS_CLIP(cover);

    half height = half(input.uv.z);

#if defined(_HB_GRASS_FLOWERS)
    half3 albedo = tuft.rgb;
    half shade = 1.0h;
#else
    half calm = half(saturate(input.uv.w / (HB_GRASS_RING * HB_GRASS_CALM_RINGS)));
    half detail = lerp(1.0h, tuft.r * HB_GRASS_DETAIL_SCALE,
                       smoothstep(0.0h, HB_GRASS_ROOT_BLEND, height) * (1.0h - calm));
    float4 tip = _HB_LayerGrassTip[input.data.x];
    half tipShare = half(tip.w) * smoothstep(HB_GRASS_TIP_START, 1.0h, height);
    half3 albedo = lerp(half3(input.ground), half3(tip.rgb), tipShare) * detail * (1.0h + half(input.uv.y));

    albedo *= (1.0h + half(input.gust.y) * height * HB_GRASS_SWEEP) * (1.0h - half(input.gust.x) * HB_GRASS_PRESS_DARK);

    half shade = lerp(lerp(HB_GRASS_ROOT_SHADE, 1.0h, height), 1.0h, calm);
#endif

    half snow = half(input.weather.y);

    albedo *= 1.0h - HB_GRASS_WET_DARKEN * half(input.weather.x);
    albedo = lerp(albedo, HB_GRASS_SNOW_COLOUR * (1.0h - half(input.gust.x) * HB_SNOW_PRESS_DARK),
                  saturate(snow * (HB_GRASS_SNOW_ROOT - HB_GRASS_SNOW_TIP * height)));

    half3 normalWS = half3(normalize(input.normalWS));

    half wrap = half(HB_GRASS_STYLE_WRAP);
    half inverseWrap = rcp(1.0h + wrap);
    half lambert = saturate((dot(normalWS, half3(_MainLightPosition.xyz)) + wrap) * inverseWrap);
    half sunlit = HB_GrassShadow(input.positionWS);
    half tone = HB_ToneBands(lambert * sunlit, wrap * inverseWrap, half(HB_GRASS_STYLE_HALF_TONE),
                             half(HB_GRASS_STYLE_SOFTNESS));
    half3 direct = half3(_MainLightColor.rgb) * tone
                 + HB_GrassExtraLights(input.positionWS, input.positionCS.xy, normalWS, wrap, inverseWrap);
    half reach;
    half3 ambient = HB_ResolveAmbient(normalWS, HB_GradientAmbient(normalWS), 0.0h, 0.0h, 0.0h, reach);
    half3 colour = albedo * (direct + ambient) * shade;
    half wet = half(input.weather.x);

    UNITY_BRANCH
    if (wet > 0.0h)
    {
        half3 viewDirectionWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));
        half3 mirror = reflect(-viewDirectionWS, normalWS);
        half fresnel = Pow4(1.0h - saturate(dot(normalWS, viewDirectionWS))) * HB_GRASS_FILM_GLOSS;
        half horizon = smoothstep(-HB_GRASS_FILM_HORIZON, HB_GRASS_FILM_HORIZON, mirror.y);
        half3 sky = lerp(HB_GradientAmbient(mirror), HB_SkySample(mirror, max(mirror.y, 0.0h)), horizon);

        colour += sky * lerp(HB_GRASS_FILM_SPEC, HB_GRASS_FILM_GRAZE, fresnel) * wet
                * half(_HB_LayerWetGloss[input.data.x]);
    }

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_GRASS_CELLS)
    {
        uint hash = input.data.y * 2654435761u;
        half3 cellColour = half3((hash >> 8u) & 255u, (hash >> 16u) & 255u, (hash >> 24u) & 255u) / 255.0h;

        colour = cellColour * lerp(0.6h, 1.0h, height);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_WEATHER_GUST)
        colour = half3(half(input.gust.x), half(input.gust.y), half(input.gust.y)) * lerp(0.6h, 1.0h, height);
#endif

    return half4(colour, 1.0h);
}

#elif defined(HB_DEPTH_NORMALS)

void HB_GrassDepthNormalsFragment(
    HBGrassVaryings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    half cover;
    HB_GrassTuft(input, cover);
    HB_GRASS_CLIP(cover);

    outNormalWS = half4(half3(normalize(input.normalWS)), 0.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#else

half HB_GrassDepthFragment(HBGrassVaryings input) : SV_Target
{
    half cover;
    HB_GrassTuft(input, cover);
    HB_GRASS_CLIP(cover);

    return input.positionCS.z;
}

#endif

#endif
