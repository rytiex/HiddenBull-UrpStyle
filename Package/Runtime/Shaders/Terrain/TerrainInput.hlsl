#ifndef HB_TERRAIN_INPUT_INCLUDED
#define HB_TERRAIN_INPUT_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half _Smoothness;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _BakedTones;
    half _BakedSoftness;
CBUFFER_END

TEXTURE2D(_HB_TerrainHeightmap);
TEXTURE2D(_HB_TerrainHoles);
TEXTURE2D(_HB_TerrainShadowMask);

TEXTURE2D_ARRAY(_HB_TerrainControl);
TEXTURE2D_ARRAY(_HB_LayerAlbedo);
SAMPLER(sampler_HB_LayerAlbedo);
TEXTURE2D_ARRAY(_HB_LayerNormal);
TEXTURE2D_ARRAY(_HB_LayerMask);

float4 _HB_TerrainSize;
float4 _HB_TerrainOrigin;
float4 _HB_TerrainShadowMaskST;
float4 _HB_TerrainCamera;
float4 _HB_TerrainRanges[12];
float4 _HB_TerrainLayers;
float4 _HB_LayerST[16];
float4 _HB_LayerLook[16];
float4 _HB_LayerSources[16];
float4 _HB_LayerSurface[16];
float _HB_LayerWetGloss[16];
float4 _HB_LayerVary[16];
float4 _HB_LayerVaryShape[16];
float4 _HB_LayerVarySize[16];
float4 _HB_LayerGrass[16];
float4 _HB_LayerGrassEdge[16];
float4 _HB_LayerGrassTip[16];
float4 _HB_TerrainGrassFar;
float4 _HB_TerrainBrush;
float4 _HB_TerrainStyle;
float4 _HB_TerrainVariation;
float4 _HB_TerrainDetail;

#define HB_TERRAIN_FILL             0.214h
#define HB_TERRAIN_BRUSH_SIZE       _HB_TerrainBrush.x
#define HB_TERRAIN_BRUSH_PAINT      _HB_TerrainBrush.y
#define HB_TERRAIN_TONE_STROKES     _HB_TerrainBrush.z
#define HB_TERRAIN_BRUSH_RELIEF     _HB_TerrainBrush.w
#define HB_TERRAIN_SHADING_BREAKUP  _HB_TerrainStyle.x
#define HB_TERRAIN_AMBIENT_BREAKUP  _HB_TerrainStyle.y
#define HB_TERRAIN_ALBEDO_VARIATION _HB_TerrainStyle.z
#define HB_TERRAIN_CURVATURE        _HB_TerrainStyle.w
#define HB_TERRAIN_MACRO            _HB_TerrainVariation.x
#define HB_TERRAIN_DETAIL_START     _HB_TerrainDetail.x
#define HB_TERRAIN_DETAIL_FADE      _HB_TerrainDetail.y

#define HB_TERRAIN_SIZE         _HB_TerrainSize.xyz
#define HB_TERRAIN_RESOLUTION   _HB_TerrainSize.w
#define HB_TERRAIN_ORIGIN       _HB_TerrainOrigin.xyz
#define HB_TERRAIN_LEAF         _HB_TerrainOrigin.w
#define HB_TERRAIN_CAMERA       _HB_TerrainCamera.xyz
#define HB_TERRAIN_QUADS        _HB_TerrainCamera.w

#define HB_TERRAIN_LAYER_COUNT  _HB_TerrainLayers.x
#define HB_TERRAIN_SLICES       _HB_TerrainLayers.y
#define HB_TERRAIN_PAINTED      _HB_TerrainLayers.z
#define HB_TERRAIN_HIGHLIGHT    _HB_TerrainLayers.w

#define HB_TERRAIN_HEIGHT_SCALE (65535.0 / 32766.0)
#define HB_TERRAIN_HOLE         0.5
#define HB_TERRAIN_EDGE_DEPTH   0.2h
#define HB_TERRAIN_EDGE_HEIGHT  0.5h

#define HB_TERRAIN_HEX_RATE      0.3
#define HB_TERRAIN_HEX_SHARPNESS 24.0
#define HB_TERRAIN_HEX_SKIP      0.01

struct HBTerrainSurface
{
    half3 albedo;
    half3 normalTS;
    half smoothness;
    half metallic;
    half occlusion;
    half wetness;
    half wetGloss;
    half snowCover;
    half height;
    half meadow;
    half vary;
    half4 tip;
    int strongest;
};

void HB_TerrainPick(half weight, int layer, inout half3 best, inout int3 ids)
{
    if (weight <= best.z)
        return;

    if (weight > best.x)
    {
        best = half3(weight, best.xy);
        ids = int3(layer, ids.xy);
    }
    else if (weight > best.y)
    {
        best = half3(best.x, weight, best.y);
        ids = int3(ids.x, layer, ids.y);
    }
    else
    {
        best.z = weight;
        ids.z = layer;
    }
}

void HB_TerrainPicks(float2 uv, out half3 best, out int3 ids)
{
    best = half3(0.0h, 0.0h, 0.0h);
    ids = int3(0, 0, 0);

    int layers = (int)HB_TERRAIN_LAYER_COUNT;

    UNITY_LOOP
    for (int slice = 0; slice < (int)HB_TERRAIN_SLICES; slice++)
    {
        half4 control = half4(SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_TerrainControl, sampler_LinearClamp, uv, slice, 0));
        int first = slice * 4;

        HB_TerrainPick(control.r, first, best, ids);

        if (first + 1 < layers)
            HB_TerrainPick(control.g, first + 1, best, ids);

        if (first + 2 < layers)
            HB_TerrainPick(control.b, first + 2, best, ids);

        if (first + 3 < layers)
            HB_TerrainPick(control.a, first + 3, best, ids);
    }
}

half HB_TerrainLayerWeight(float3 positionWS, int layer)
{
    float2 uv = (positionWS.xz - HB_TERRAIN_ORIGIN.xz) / HB_TERRAIN_SIZE.xz;
    half4 control = half4(SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_TerrainControl, sampler_LinearClamp, uv, layer >> 2, 0));

    return control[layer & 3];
}

float2 HB_TerrainHexOffset(float2 vertex)
{
    float2 r = mul(float2x2(127.1, 311.7, 269.5, 183.3), vertex);

    return frac(sin(r) * 43758.5453);
}

void HB_TerrainHexGrid(float2 uv, out float3 weights, out float2 vertex1, out float2 vertex2, out float2 vertex3)
{
    float2 grid = uv * (HB_TERRAIN_HEX_RATE * 2.0 * sqrt(3.0));
    float2 skewed = mul(float2x2(1.0, -0.57735027, 0.0, 1.15470054), grid);
    float2 base = floor(skewed);
    float3 corner = float3(frac(skewed), 0.0);
    corner.z = 1.0 - corner.x - corner.y;

    float side = step(0.0, -corner.z);
    float flip = 2.0 * side - 1.0;

    weights = float3(-corner.z * flip, side - corner.y * flip, side - corner.x * flip);
    vertex1 = base + float2(side, side);
    vertex2 = base + float2(side, 1.0 - side);
    vertex3 = base + float2(1.0 - side, side);
}

half HB_TerrainLuma(half3 colour)
{
    return dot(colour, half3(0.299h, 0.587h, 0.114h));
}

struct HBTerrainHex
{
    float2 uv1;
    float2 uv2;
    float2 uv3;
    half3 weights;
};

HBTerrainHex HB_TerrainHex(float2 uv)
{
    float3 weights;
    float2 vertex1, vertex2, vertex3;
    HB_TerrainHexGrid(uv, weights, vertex1, vertex2, vertex3);

    weights /= max(max(max(weights.x, weights.y), weights.z), HB_EPSILON);
    weights = pow(saturate(weights), HB_TERRAIN_HEX_SHARPNESS);
    weights /= max(weights.x + weights.y + weights.z, HB_EPSILON);
    weights = weights > HB_TERRAIN_HEX_SKIP ? weights : 0.0;
    weights /= max(weights.x + weights.y + weights.z, HB_EPSILON);

    HBTerrainHex hex;
    hex.uv1 = uv + HB_TerrainHexOffset(vertex1);
    hex.uv2 = uv + HB_TerrainHexOffset(vertex2);
    hex.uv3 = uv + HB_TerrainHexOffset(vertex3);
    hex.weights = half3(weights);
    return hex;
}

half4 HB_TerrainHexRead(TEXTURE2D_ARRAY_PARAM(map, sampler_map), HBTerrainHex hex, int layer, float2 uvDx,
                        float2 uvDy)
{
    half4 result = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (hex.weights.x > 0.0h)
        result += half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(map, sampler_map, hex.uv1, layer, uvDx, uvDy)) * hex.weights.x;

    UNITY_BRANCH
    if (hex.weights.y > 0.0h)
        result += half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(map, sampler_map, hex.uv2, layer, uvDx, uvDy)) * hex.weights.y;

    UNITY_BRANCH
    if (hex.weights.z > 0.0h)
        result += half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(map, sampler_map, hex.uv3, layer, uvDx, uvDy)) * hex.weights.z;

    return result;
}

half3 HB_TerrainVary(half3 colour, float2 xzWS, int layer)
{
    half4 vary = half4(_HB_LayerVary[layer]);
    half3 result = colour;

    UNITY_BRANCH
    if (vary.a > 0.0h)
    {
        float4 shape = _HB_LayerVaryShape[layer];
        float4 size = _HB_LayerVarySize[layer];
        float2 p = xzWS / max(size.x, 0.1) + float2(layer * 7.13, 3.7);

        p += float2(HB_BrushField(p * 0.9 + 3.1), HB_BrushField(p * 0.9 + 11.7)) * (0.5 * shape.x);

        half large = half(HB_BrushField(p) * 0.3 + HB_BrushField(p * 2.3 + 17.0) * 0.2);
        half fine = half(HB_BrushField(p * 6.1 + 41.0) * 0.5);
        half drift = half(HB_BrushField(p * 0.37 + 71.0) * 0.5);
        half patches = saturate((large + fine * half(shape.y)) * half(shape.z) + 0.5h);

        half3 hue = vary.rgb / max(dot(vary.rgb, half3(0.299h, 0.587h, 0.114h)), 0.05h);
        half3 varied = size.y > 0.5 ? lerp(colour, vary.rgb, patches * vary.a)
                                    : colour * lerp(half3(1.0h, 1.0h, 1.0h), hue, patches * vary.a);

        result = varied * (1.0h + (drift + fine * 0.5h) * (half(shape.w) * vary.a));
    }

    return result;
}

HBTerrainSurface HB_TerrainLayerSurface(float3 positionWS, float2 shift, half detail)
{
    float2 positionDx = ddx(positionWS.xz);
    float2 positionDy = ddy(positionWS.xz);
    float2 local = positionWS.xz - HB_TERRAIN_ORIGIN.xz + shift;
    float2 uv = local / HB_TERRAIN_SIZE.xz;

    half3 best;
    int3 ids;
    HB_TerrainPicks(uv, best, ids);
    best.z *= detail;

    half4 mask[3];
    half3 key;

    UNITY_UNROLL
    for (int pick = 0; pick < 3; pick++)
    {
        int id = ids[pick];

        mask[pick] = half4(0.0h, 1.0h, 0.0h, 0.0h);

        UNITY_BRANCH
        if (best[pick] > 0.0h)
        {
            float4 st = _HB_LayerST[id];
            HBTerrainHex hex = HB_TerrainHex(local * st.xy + st.zw);
            float2 uvDx = positionDx * st.xy;
            float2 uvDy = positionDy * st.xy;
            half3 sources = half3(_HB_LayerSources[id].xyz);

            UNITY_BRANCH
            if (sources.z > 0.5h)
            {
                mask[pick] = HB_TerrainHexRead(TEXTURE2D_ARRAY_ARGS(_HB_LayerMask, sampler_HB_LayerAlbedo), hex, id,
                                               uvDx, uvDy);
            }
            else
            {
                half3 colour = half3(HB_TERRAIN_FILL, HB_TERRAIN_FILL, HB_TERRAIN_FILL);

                UNITY_BRANCH
                if (sources.x > 0.5h)
                    colour = HB_TerrainHexRead(TEXTURE2D_ARRAY_ARGS(_HB_LayerAlbedo, sampler_HB_LayerAlbedo), hex, id,
                                               uvDx, uvDy).rgb;

                mask[pick] = half4(1.0h, 1.0h, saturate(HB_TerrainLuma(colour)), 1.0h);
            }
        }

        key[pick] = best[pick] > 0.0h ? best[pick] + (mask[pick].b - 0.5h) * HB_TERRAIN_EDGE_HEIGHT : -1.0h;
    }

    half top = max(max(key.x, key.y), key.z);
    half3 blend = max(key - top + HB_TERRAIN_EDGE_DEPTH, 0.0h);
    blend /= max(blend.x + blend.y + blend.z, half(HB_EPSILON));

    HBTerrainSurface surface;
    surface.albedo = half3(0.0h, 0.0h, 0.0h);
    surface.normalTS = half3(0.0h, 0.0h, 0.0h);
    surface.smoothness = 0.0h;
    surface.metallic = 0.0h;
    surface.occlusion = 0.0h;
    surface.wetness = 0.0h;
    surface.wetGloss = 0.0h;
    surface.snowCover = 0.0h;
    surface.height = 0.0h;
    surface.meadow = 0.0h;
    surface.vary = 0.0h;
    surface.tip = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_UNROLL
    for (int layer = 0; layer < 3; layer++)
    {
        int id = ids[layer];
        half share = blend[layer];

        UNITY_BRANCH
        if (share > 0.0h)
        {
            float4 st = _HB_LayerST[id];
            float4 look = _HB_LayerLook[id];
            HBTerrainHex hex = HB_TerrainHex(local * st.xy + st.zw);
            float2 uvDx = positionDx * st.xy;
            float2 uvDy = positionDy * st.xy;
            half3 sources = half3(_HB_LayerSources[id].xyz);
            half3 colour = half3(HB_TERRAIN_FILL, HB_TERRAIN_FILL, HB_TERRAIN_FILL);
            half3 normalTS = half3(0.0h, 0.0h, 1.0h);

            UNITY_BRANCH
            if (sources.x > 0.5h)
                colour = HB_TerrainHexRead(TEXTURE2D_ARRAY_ARGS(_HB_LayerAlbedo, sampler_HB_LayerAlbedo), hex, id,
                                           uvDx, uvDy).rgb;

            surface.albedo += HB_TerrainVary(colour * half3(look.rgb), positionWS.xz, id) * share;

            UNITY_BRANCH
            if (detail > 0.0h && sources.y > 0.5h)
            {
                half4 normal = HB_TerrainHexRead(TEXTURE2D_ARRAY_ARGS(_HB_LayerNormal, sampler_HB_LayerAlbedo), hex,
                                                 id, uvDx, uvDy);
                half2 tilt = (half2(normal.a * normal.r, normal.g) * 2.0h - 1.0h) * (half(look.w) * detail);

                normalTS = half3(tilt, sqrt(saturate(1.0h - dot(tilt, tilt))));
            }

            surface.normalTS += normalTS * share;
        }

        float4 settings = _HB_LayerSurface[id];

        surface.smoothness += mask[layer].a * half(settings.x) * share;
        surface.metallic += mask[layer].r * half(settings.y) * share;
        surface.occlusion += mask[layer].g * share;
        surface.wetness += half(settings.z) * share;
        surface.wetGloss += half(_HB_LayerWetGloss[id]) * share;
        surface.snowCover += half(settings.w) * share;
        surface.height += mask[layer].b * share;

        half grows = saturate(half(_HB_LayerGrass[id].x)) * share;

        surface.meadow += grows;
        surface.vary += half(_HB_LayerGrassEdge[id].y) * grows;
        surface.tip += half4(_HB_LayerGrassTip[id]) * grows;
    }

    surface.normalTS = normalize(surface.normalTS);
    surface.strongest = blend.y > blend.x ? (blend.z > blend.y ? ids.z : ids.y) : (blend.z > blend.x ? ids.z : ids.x);
    return surface;
}

half3 HB_TerrainNormalToWorld(half3 normalTS, half3 normalWS)
{
    half3 tangentWS = SafeNormalize(half3(normalWS.y, -normalWS.x, 0.0h));
    half3 bitangentWS = cross(tangentWS, normalWS);

    return normalize(tangentWS * normalTS.x + bitangentWS * normalTS.y + normalWS * normalTS.z);
}

struct HBTerrainVertex
{
    float3 positionWS;
    float level;
    float morph;
};

float2 HB_TerrainLocal(float2 xzWS)
{
    return xzWS - HB_TERRAIN_ORIGIN.xz;
}

float HB_TerrainHeight(float2 xzWS)
{
    float2 uv = (HB_TerrainLocal(xzWS) / HB_TERRAIN_SIZE.xz * (HB_TERRAIN_RESOLUTION - 1.0) + 0.5)
              / HB_TERRAIN_RESOLUTION;
    float height = SAMPLE_TEXTURE2D_LOD(_HB_TerrainHeightmap, sampler_LinearClamp, uv, 0).r;

    return HB_TERRAIN_ORIGIN.y + height * HB_TERRAIN_HEIGHT_SCALE * HB_TERRAIN_SIZE.y;
}

float3 HB_TerrainNormal(float2 xzWS)
{
    float2 texel = HB_TERRAIN_SIZE.xz / (HB_TERRAIN_RESOLUTION - 1.0);

    float left = HB_TerrainHeight(xzWS - float2(texel.x, 0.0));
    float right = HB_TerrainHeight(xzWS + float2(texel.x, 0.0));
    float down = HB_TerrainHeight(xzWS - float2(0.0, texel.y));
    float up = HB_TerrainHeight(xzWS + float2(0.0, texel.y));

    return normalize(float3((left - right) / (2.0 * texel.x), 1.0, (down - up) / (2.0 * texel.y)));
}

float3 HB_TerrainShape(float2 xzWS, float footprint, out float curvature)
{
    float step = max(HB_TERRAIN_SIZE.x / (HB_TERRAIN_RESOLUTION - 1.0), footprint);

    float left = HB_TerrainHeight(xzWS - float2(step, 0.0));
    float right = HB_TerrainHeight(xzWS + float2(step, 0.0));
    float down = HB_TerrainHeight(xzWS - float2(0.0, step));
    float up = HB_TerrainHeight(xzWS + float2(0.0, step));
    float centre = HB_TerrainHeight(xzWS);

    curvature = (left + right + down + up - 4.0 * centre) / (step * step);

    return normalize(float3((left - right) / (2.0 * step), 1.0, (down - up) / (2.0 * step)));
}

half4 HB_TerrainShadowMask(float3 positionWS)
{
#if defined(SHADOWS_SHADOWMASK)
    float2 uv = HB_TerrainLocal(positionWS.xz) / HB_TERRAIN_SIZE.xz;

    return SAMPLE_TEXTURE2D_LOD(_HB_TerrainShadowMask, sampler_LinearClamp,
                                uv * _HB_TerrainShadowMaskST.xy + _HB_TerrainShadowMaskST.zw, 0);
#else
    return half4(1.0h, 1.0h, 1.0h, 1.0h);
#endif
}

void HB_TerrainClipHoles(float3 positionWS)
{
    float2 uv = HB_TerrainLocal(positionWS.xz) / HB_TERRAIN_SIZE.xz;

    clip(SAMPLE_TEXTURE2D_LOD(_HB_TerrainHoles, sampler_PointClamp, uv, 0).r - HB_TERRAIN_HOLE);
}

HBTerrainVertex HB_TerrainVertex(float3 positionOS)
{
    float size = UNITY_MATRIX_M._m00;
    float level = UNITY_MATRIX_M._m11 - 1.0;
    float2 origin = float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m23);

    float spacing = HB_TERRAIN_LEAF * exp2(level) / HB_TERRAIN_QUADS;
    float2 grid = positionOS.xz * round(size / spacing);
    float2 xz = origin + grid * spacing;

    float distance = length(float3(xz.x, HB_TerrainHeight(xz), xz.y) - HB_TERRAIN_CAMERA);
    float2 range = _HB_TerrainRanges[(int)level].xy;
    float morph = saturate((distance - range.x) / max(range.y - range.x, HB_EPSILON));

    grid -= frac(grid * 0.5) * 2.0 * morph;
    xz = origin + grid * spacing;

    HBTerrainVertex vertex;
    vertex.positionWS = float3(xz.x, HB_TerrainHeight(xz), xz.y);
    vertex.level = level;
    vertex.morph = morph;
    return vertex;
}

#endif
