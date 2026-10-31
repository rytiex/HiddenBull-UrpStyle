#ifndef HB_GRASS_PASS_INCLUDED
#define HB_GRASS_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassInput.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleShelter.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWeatherNoise.hlsl"

#if defined(HB_GRASS_FORWARD)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleAmbient.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSky.hlsl"
#endif

StructuredBuffer<uint3> _HB_GrassBlades;
StructuredBuffer<int4> _HB_GrassSlots;
StructuredBuffer<uint> _HB_GrassList;

TEXTURE2D_ARRAY(_HB_GrassTufts);
SAMPLER(sampler_HB_GrassTufts);

#if defined(_HB_GRASS_FLOWERS)
TEXTURE2D_ARRAY(_HB_FlowerHeads);
SAMPLER(sampler_HB_FlowerHeads);

float4 _HB_LayerFlower[16];
#endif

float4 _HB_LayerGrass[16];
float4 _HB_LayerGrassTip[16];
float4 _HB_LayerGrassEdge[16];
float4 _HB_LayerGrassShape[16];
float4 _HB_LayerSurface[16];
float4 _HB_GrassStyle;
float _HB_ShelterPass;
float4 _HB_GrassBenders[8];
float4 _HB_GrassPresses[8];

#define HB_GRASS_STYLE_WRAP         _HB_GrassStyle.x
#define HB_GRASS_STYLE_SOFTNESS     _HB_GrassStyle.y
#define HB_GRASS_STYLE_HALF_TONE    _HB_GrassStyle.z
#define HB_GRASS_BENDERS      ((int)_HB_GrassCamera.w)
#define HB_GRASS_BEND_LEAN    0.7
#define HB_GRASS_BEND_DROP    0.35
#define HB_GRASS_BEND_CORE    0.35
#define HB_GRASS_WET_DARKEN   0.3h
#define HB_GRASS_SNOW_COLOUR  half3(0.5h, 0.54h, 0.6h)
#define HB_GRASS_SNOW_SHRINK  0.6
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
#define HB_GRASS_THIN_SOFT    0.08
#define HB_GRASS_NORMAL_TILT  0.25
#define HB_GRASS_FILM_SPEC    0.04h
#define HB_GRASS_FILM_GRAZE   0.89h
#define HB_GRASS_FILM_GLOSS   0.7225h
#define HB_GRASS_FILM_HORIZON 0.04h
#define HB_GRASS_PATCH_STEP   (1.0 / 7.0)
#define HB_GRASS_PATCH_SHAKE  61.7
#define HB_GRASS_GUST_SPEED   3.0
#define HB_GRASS_GUST_RISE    7.0
#define HB_GRASS_GUST_LAG     2.5
#define HB_GRASS_GUST_WIDE    45.0
#define HB_GRASS_GUST_LONG    20.0
#define HB_GRASS_GUST_FINE    16.0
#define HB_GRASS_GUST_SHORT   8.0
#define HB_GRASS_GUST_LOW     0.45
#define HB_GRASS_GUST_HIGH    0.8
#define HB_GRASS_LEAN_BASE    0.22
#define HB_GRASS_LEAN_QUICK   0.06
#define HB_GRASS_LEAN_PRESS   0.45
#define HB_GRASS_SIDE         0.05
#define HB_GRASS_IDLE         0.02
#define HB_GRASS_SWAY_REACH   0.6
#define HB_GRASS_SWAY_FADE    0.25
#define HB_GRASS_SPLAY        0.12
#define HB_GRASS_SPLAY_DOWN   0.4
#define HB_GRASS_SWEEP        0.5h
#define HB_GRASS_SINK         0.06
#define HB_GRASS_FAR_TURN     1.6
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
#define HB_DEBUG_GRASS_CELLS  16
#define HB_DEBUG_GRASS_GUST   17

static const float2 HB_GRASS_CORNERS[6] =
{
    float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
    float2(0.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0)
};

struct HBGrassVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float4 uv         : TEXCOORD1;
    float3 normalWS   : TEXCOORD2;
    float3 ground     : TEXCOORD3;
    float2 gust       : TEXCOORD4;
    nointerpolation uint2 data : TEXCOORD5;
    float2 weather    : TEXCOORD6;
};

float HB_GrassNoise(float2 position)
{
    float2 cell = floor(position);
    float2 blend = position - cell;
    float2 step = float2(127.1, 311.7);
    float4 corners = frac(sin(float4(dot(cell, step), dot(cell + float2(1.0, 0.0), step),
                                     dot(cell + float2(0.0, 1.0), step), dot(cell + float2(1.0, 1.0), step)))
                          * 43758.5453);

    blend = blend * blend * (3.0 - 2.0 * blend);

    return lerp(lerp(corners.x, corners.y, blend.x), lerp(corners.z, corners.w, blend.x), blend.y);
}

float3 HB_GrassGust(float3 rootWS, float seed, float gap, out float2 gust)
{
    float2 wind = float2(sin(HB_WIND_HEADING), cos(HB_WIND_HEADING));
    float2 side = float2(-wind.y, wind.x);
    float strength = HB_WIND;
    float along = dot(rootWS.xz, side);
    float ahead = dot(rootWS.xz, wind) + seed * HB_GRASS_GUST_LAG;
    float time = _Time.y;
    float speed = HB_GRASS_GUST_SPEED + HB_GRASS_GUST_RISE * strength;

    float wide = HB_GrassNoise(float2(along / HB_GRASS_GUST_WIDE, (ahead - time * speed) / HB_GRASS_GUST_LONG));
    float fine = HB_GrassNoise(float2(along / HB_GRASS_GUST_FINE + 7.3,
                                      (ahead - time * speed * 1.6) / HB_GRASS_GUST_SHORT));
    float streak = smoothstep(HB_GRASS_GUST_LOW, HB_GRASS_GUST_HIGH, wide * 0.65 + fine * 0.35);

    float quick = sin(time * (2.3 + seed * 1.4) + seed * 19.0);
    float lean = strength * (HB_GRASS_LEAN_BASE + HB_GRASS_LEAN_QUICK * quick + HB_GRASS_LEAN_PRESS * streak);
    float drift = (HB_GRASS_IDLE + strength * HB_GRASS_SIDE) * sin(time * (1.7 + seed) + seed * 31.0);
    float2 push = wind * (lean + HB_GRASS_IDLE * quick) + side * drift;
    float still = saturate((HB_GRASS_DISTANCE * HB_GRASS_SWAY_REACH - gap)
                         / (HB_GRASS_DISTANCE * HB_GRASS_SWAY_FADE));

    gust = float2(lean, strength * streak);

    return float3(push.x, -0.5 * lean * lean, push.y) * still;
}

float2 HB_GrassWeather(HBGrassBlade blade)
{
    float2 weather = float2(0.0, 0.0);

    UNITY_BRANCH
    if (HB_SURFACE_WET > 0.0 || HB_SURFACE_SNOW > 0.0)
    {
        float open = 1.0;

        UNITY_BRANCH
        if (HB_SHELTER_BOUND > 0.5)
        {
            float edge;
            float2 uv = HB_ShelterUV(_HB_ShelterViewProj, blade.root.xz, edge);

            if (edge > 0.0)
            {
                float depth = SAMPLE_TEXTURE2D_LOD(_HB_ShelterDepth, sampler_PointClamp, uv, 0).r;
                float roof = depth * HB_SHELTER_HEIGHT_SCALE + HB_SHELTER_HEIGHT_BIAS;
                float empty = HB_SHELTER_HEIGHT_BIAS + min(HB_SHELTER_HEIGHT_SCALE, 0.0) + HB_SHELTER_EMPTY_MARGIN;

                open = roof > empty ? 1.0 - saturate((roof - blade.root.y - HB_SHELTER_BIAS) / HB_SHELTER_RANGE) : 1.0;
            }
        }

        float4 surface = _HB_LayerSurface[blade.layer];
        float patches = blade.patches + (frac(blade.rand * HB_GRASS_PATCH_SHAKE) - 0.5) * HB_GRASS_PATCH_STEP;
        float delay = saturate(patches * HB_SNOW_PATCH_DELAY + 0.5 * HB_SNOW_DROP_DELAY);
        float settled = saturate((HB_SURFACE_SNOW * surface.w - delay * HB_SNOW_SPREAD) / (1.0 - HB_SNOW_SPREAD));
        float snow = blade.snowy * saturate(settled * HB_SNOW_FADE_IN) * open;
        float soaked = saturate((HB_SURFACE_WET - saturate(patches * HB_WET_PATCH_DELAY) * HB_WET_SPREAD)
                                / (1.0 - HB_WET_SPREAD));

        weather = float2(soaked * open * (1.0 - snow) * surface.z, snow);
    }

    return weather;
}

float3 HB_GrassBend(float3 rootWS, float height)
{
    float3 tip = rootWS + float3(0.0, height, 0.0);
    float2 push = float2(0.0, 0.0);
    float drop = 0.0;

    UNITY_LOOP
    for (int index = 0; index < HB_GRASS_BENDERS; index++)
    {
        float4 bender = _HB_GrassBenders[index];
        float3 away = tip - bender.xyz;
        float reach = length(away.xz);
        float touch = saturate(1.0 - length(away) / bender.w);
        float press = touch * touch * (3.0 - 2.0 * touch) * _HB_GrassPresses[index].x;
        float open = saturate(reach / (bender.w * HB_GRASS_BEND_CORE));

        push += away.xz / max(reach, 1e-3) * (press * open * HB_GRASS_BEND_LEAN);
        drop = max(drop, press * HB_GRASS_BEND_DROP);
    }

    return float3(push.x, 0.0, push.y) / max(length(push) / HB_GRASS_BEND_LEAN, 1.0) - float3(0.0, drop, 0.0);
}

#if defined(_HB_GRASS_FLOWERS)

HBGrassVaryings HB_GrassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    uint index = _HB_GrassList[instanceID];
    uint slot = index / HB_GRASS_PER_SLOT;
    uint local = index - slot * HB_GRASS_PER_SLOT;
    int4 owner = _HB_GrassSlots[slot];
    HBGrassBlade blade = HB_GrassUnpack(_HB_GrassBlades[index], owner.xy, local, (uint)owner.z);

    float gap = distance(blade.root, HB_GRASS_CAMERA);
    float4 flower = _HB_LayerFlower[blade.layer];
    float2 weather = HB_GrassWeather(blade);
    float reach = HB_GRASS_DISTANCE * HB_FLOWER_REACH;
    float size = flower.y * lerp(HB_FLOWER_SMALL, 1.0, blade.rand)
               * saturate((reach - gap) / (reach * HB_FLOWER_FADE))
               * (1.0 - smoothstep(HB_FLOWER_SNOW_FROM, HB_FLOWER_SNOW_GONE, weather.y));
    float height = blade.height * lerp(HB_FLOWER_LOW, HB_FLOWER_HIGH, blade.rand)
                 * (1.0 - HB_GRASS_SNOW_SHRINK * weather.y);
    uint card = vertexID / 6u;
    float2 corner = HB_GRASS_CORNERS[vertexID - card * 6u];

    float turn = blade.rand * TWO_PI * 7.0;
    float3 up = float3(0.0, 1.0, 0.0);
    float3 away = float3(cos(turn), 0.0, sin(turn));
    float3 toCamera = normalize(HB_GRASS_CAMERA - blade.root);

    float2 gust;
    float3 sway = (HB_GrassGust(blade.root, blade.rand, gap, gust) * HB_FLOWER_SWAY
                   + HB_GrassBend(blade.root, height)) * height;
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
    output.weather = weather;
    output.normalWS = up;
    output.ground = blade.ground;
    output.gust = gust;
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

HBGrassVaryings HB_GrassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    uint index = _HB_GrassList[instanceID];
    uint slot = index / HB_GRASS_PER_SLOT;
    uint local = index - slot * HB_GRASS_PER_SLOT;
    int4 owner = _HB_GrassSlots[slot];
    int2 cell = owner.xy;
    HBGrassBlade blade = HB_GrassUnpack(_HB_GrassBlades[index], cell, local, (uint)owner.z);

    float gap = distance(blade.root, HB_GRASS_CAMERA);
    float width = blade.width;

    float2 weather = HB_GrassWeather(blade);

    blade.height *= saturate((HB_GrassKeep(gap, blade.level) - blade.rand) / HB_GRASS_THIN_SOFT);
    blade.height *= 1.0 - HB_GRASS_SNOW_SHRINK * weather.y;

    uint card = vertexID / 6u;
    float2 corner = HB_GRASS_CORNERS[vertexID - card * 6u];
    float4 shape = _HB_LayerGrassShape[blade.layer];

    corner = float2(lerp(lerp(shape.x, shape.y, corner.x), lerp(shape.z, shape.w, corner.x), corner.y),
                    corner.y * _HB_LayerGrassEdge[blade.layer].y);

    float3 toCamera = HB_GRASS_CAMERA - blade.root;
    float3 across;
    float3 face;

    if (blade.level > 0u)
    {
        float2 flat = normalize(toCamera.xz + float2(1e-5, 0.0));
        float turn = (blade.rand - 0.5) * HB_GRASS_FAR_TURN;
        float turnSin = sin(turn);
        float turnCos = cos(turn);

        face = float3(flat.x * turnCos - flat.y * turnSin, 0.0, flat.x * turnSin + flat.y * turnCos);
        across = float3(face.z, 0.0, -face.x);
    }
    else
    {
        float yaw = blade.rand * TWO_PI + card * HALF_PI;

        across = float3(cos(yaw), 0.0, sin(yaw));
        face = float3(-across.z, 0.0, across.x) * (card == 0u ? 1.0 : -1.0);
    }

    float overhead = saturate(toCamera.y / max(length(toCamera), 1e-3));
    float splay = lerp(HB_GRASS_SPLAY, HB_GRASS_SPLAY_DOWN, overhead * overhead);
    float3 stem = float3(face.x * splay, sqrt(1.0 - splay * splay), face.z * splay);

    float2 gust = float2(0.0, 0.0);
    float3 sway = float3(0.0, 0.0, 0.0);

    UNITY_BRANCH
    if (corner.y > 0.0)
        sway = HB_GrassGust(blade.root, blade.rand, gap, gust) + HB_GrassBend(blade.root, blade.height);

    float3 positionWS = blade.root + across * ((corner.x - 0.5) * (blade.rand > 0.5 ? -width : width))
                     + stem * (corner.y * blade.height);
    positionWS.y -= HB_GRASS_SINK * blade.height;
    positionWS += sway * (corner.y * corner.y * blade.height);

    HBGrassVaryings output;
    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.uv = float4(corner, corner.y, gap);
    output.weather = weather;
    output.normalWS = normalize(float3(cos(blade.rand * TWO_PI * 3.0) * HB_GRASS_NORMAL_TILT, 1.0,
                                       sin(blade.rand * TWO_PI * 3.0) * HB_GRASS_NORMAL_TILT));
    output.ground = blade.ground;
    output.gust = gust;
    output.data = uint2(blade.layer, (uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663) ^ (blade.level * 83492791u));

    if (_HB_ShelterPass > 0.5)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);

    return output;
}

half4 HB_GrassTuft(HBGrassVaryings input, out half cover)
{
    float slice = _HB_LayerGrass[input.data.x].w;
    half4 tuft = half4(SAMPLE_TEXTURE2D_ARRAY(_HB_GrassTufts, sampler_HB_GrassTufts, input.uv.xy, slice));

    cover = tuft.a - HB_GRASS_CUTOFF;
    return tuft;
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

half4 HB_GrassFragment(HBGrassVaryings input) : SV_Target
{
    half cover;
    half4 tuft = HB_GrassTuft(input, cover);

    clip(cover);

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
    half3 albedo = lerp(half3(input.ground), half3(tip.rgb), tipShare) * detail;

    albedo *= 1.0h + half(input.gust.y) * height * HB_GRASS_SWEEP;

    half shade = lerp(lerp(HB_GRASS_ROOT_SHADE, 1.0h, height), 1.0h, calm);
#endif

    half snow = half(input.weather.y);

    albedo *= 1.0h - HB_GRASS_WET_DARKEN * half(input.weather.x);
    albedo = lerp(albedo, HB_GRASS_SNOW_COLOUR, saturate(snow * (HB_GRASS_SNOW_ROOT - HB_GRASS_SNOW_TIP * height)));

    half3 normalWS = half3(normalize(input.normalWS));

    half wrap = half(HB_GRASS_STYLE_WRAP);
    half inverseWrap = rcp(1.0h + wrap);
    half lambert = saturate((dot(normalWS, half3(_MainLightPosition.xyz)) + wrap) * inverseWrap);
    half sunlit = HB_GrassShadow(input.positionWS);
    half tone = HB_ToneBands(lambert * sunlit, wrap * inverseWrap, half(HB_GRASS_STYLE_HALF_TONE),
                             half(HB_GRASS_STYLE_SOFTNESS));
    half3 direct = half3(_MainLightColor.rgb) * tone;
    half reach;
    half3 ambient = HB_ResolveAmbient(normalWS, HB_GradientAmbient(normalWS), 0.0h, 0.0h, 0.0h, reach);
    half3 colour = albedo * (direct + ambient) * shade;
    half wet = half(input.weather.x);

    UNITY_BRANCH
    if (wet > 0.0h)
    {
        half3 viewDirectionWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));
        half3 mirror = half3(-viewDirectionWS.x, viewDirectionWS.y, -viewDirectionWS.z);
        half fresnel = Pow4(1.0h - saturate(viewDirectionWS.y)) * HB_GRASS_FILM_GLOSS;
        half horizon = smoothstep(-HB_GRASS_FILM_HORIZON, HB_GRASS_FILM_HORIZON, mirror.y);
        half3 sky = lerp(HB_GradientAmbient(mirror), HB_SkySample(mirror, max(mirror.y, 0.0h)), horizon);

        colour += sky * lerp(HB_GRASS_FILM_SPEC, HB_GRASS_FILM_GRAZE, fresnel) * wet;
    }

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_GRASS_CELLS)
    {
        uint hash = input.data.y * 2654435761u;
        half3 cellColour = half3((hash >> 8u) & 255u, (hash >> 16u) & 255u, (hash >> 24u) & 255u) / 255.0h;

        colour = cellColour * lerp(0.6h, 1.0h, height);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_GRASS_GUST)
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
    clip(cover);

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
    clip(cover);

    return input.positionCS.z;
}

#endif

#endif
