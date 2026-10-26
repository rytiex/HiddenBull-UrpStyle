#ifndef HB_TERRAIN_INPUT_INCLUDED
#define HB_TERRAIN_INPUT_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

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

TEXTURE2D_ARRAY(_HB_TerrainControl);
TEXTURE2D_ARRAY(_HB_LayerAlbedo);
SAMPLER(sampler_HB_LayerAlbedo);
TEXTURE2D_ARRAY(_HB_LayerNormal);
TEXTURE2D_ARRAY(_HB_LayerMask);

float4 _HB_TerrainSize;
float4 _HB_TerrainOrigin;
float4 _HB_TerrainCamera;
float4 _HB_TerrainRanges[12];
float4 _HB_TerrainLayers;
float4 _HB_LayerST[16];
float4 _HB_LayerLook[16];
float4 _HB_LayerSurface[16];
float4 _HB_TerrainBrush;
float4 _HB_TerrainStyle;
float4 _HB_TerrainVariation;

#define HB_TERRAIN_BRUSH_SIZE       _HB_TerrainBrush.x
#define HB_TERRAIN_BRUSH_PAINT      _HB_TerrainBrush.y
#define HB_TERRAIN_TONE_STROKES     _HB_TerrainBrush.z
#define HB_TERRAIN_BRUSH_RELIEF     _HB_TerrainBrush.w
#define HB_TERRAIN_SHADING_BREAKUP  _HB_TerrainStyle.x
#define HB_TERRAIN_AMBIENT_BREAKUP  _HB_TerrainStyle.y
#define HB_TERRAIN_ALBEDO_VARIATION _HB_TerrainStyle.z
#define HB_TERRAIN_CURVATURE        _HB_TerrainStyle.w
#define HB_TERRAIN_MACRO            _HB_TerrainVariation.x

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

struct HBTerrainSurface
{
    half3 albedo;
    half3 normalTS;
    half smoothness;
    half metallic;
    half occlusion;
    half wetness;
    half snowCover;
    half height;
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

void HB_TerrainHexSample(float2 uv, int layer, float2 uvDx, float2 uvDy, out half4 albedo, out half4 normal,
                         out half4 mask)
{
    float3 weights;
    float2 vertex1, vertex2, vertex3;
    HB_TerrainHexGrid(uv, weights, vertex1, vertex2, vertex3);

    float2 uv1 = uv + HB_TerrainHexOffset(vertex1);
    float2 uv2 = uv + HB_TerrainHexOffset(vertex2);
    float2 uv3 = uv + HB_TerrainHexOffset(vertex3);

    weights /= max(max(max(weights.x, weights.y), weights.z), HB_EPSILON);
    weights = pow(saturate(weights), HB_TERRAIN_HEX_SHARPNESS);
    weights /= max(weights.x + weights.y + weights.z, HB_EPSILON);

    half3 w = half3(weights);

    albedo = half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerAlbedo, sampler_HB_LayerAlbedo, uv1, layer, uvDx, uvDy)) * w.x
           + half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerAlbedo, sampler_HB_LayerAlbedo, uv2, layer, uvDx, uvDy)) * w.y
           + half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerAlbedo, sampler_HB_LayerAlbedo, uv3, layer, uvDx, uvDy)) * w.z;
    normal = half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerNormal, sampler_HB_LayerAlbedo, uv1, layer, uvDx, uvDy)) * w.x
           + half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerNormal, sampler_HB_LayerAlbedo, uv2, layer, uvDx, uvDy)) * w.y
           + half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerNormal, sampler_HB_LayerAlbedo, uv3, layer, uvDx, uvDy)) * w.z;
    mask = half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerMask, sampler_HB_LayerAlbedo, uv1, layer, uvDx, uvDy)) * w.x
         + half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerMask, sampler_HB_LayerAlbedo, uv2, layer, uvDx, uvDy)) * w.y
         + half4(SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerMask, sampler_HB_LayerAlbedo, uv3, layer, uvDx, uvDy)) * w.z;
}

HBTerrainSurface HB_TerrainLayerSurface(float3 positionWS, float2 shift)
{
    float2 positionDx = ddx(positionWS.xz);
    float2 positionDy = ddy(positionWS.xz);
    float2 local = positionWS.xz - HB_TERRAIN_ORIGIN.xz + shift;
    float2 uv = local / HB_TERRAIN_SIZE.xz;

    half3 best;
    int3 ids;
    HB_TerrainPicks(uv, best, ids);

    half4 albedo[3];
    half4 normal[3];
    half4 mask[3];
    half3 key;

    UNITY_UNROLL
    for (int pick = 0; pick < 3; pick++)
    {
        float4 st = _HB_LayerST[ids[pick]];
        float2 layerUV = local * st.xy + st.zw;
        float2 uvDx = positionDx * st.xy;
        float2 uvDy = positionDy * st.xy;

        albedo[pick] = half4(0.0h, 0.0h, 0.0h, 0.0h);
        normal[pick] = half4(0.5h, 0.5h, 1.0h, 1.0h);
        mask[pick] = half4(0.0h, 1.0h, 0.0h, 0.0h);

        UNITY_BRANCH
        if (best[pick] > 0.0h)
            HB_TerrainHexSample(layerUV, ids[pick], uvDx, uvDy, albedo[pick], normal[pick], mask[pick]);

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
    surface.snowCover = 0.0h;
    surface.height = 0.0h;

    UNITY_UNROLL
    for (int layer = 0; layer < 3; layer++)
    {
        float4 look = _HB_LayerLook[ids[layer]];
        float4 settings = _HB_LayerSurface[ids[layer]];
        half2 tilt = (normal[layer].xy * 2.0h - 1.0h) * half(look.w);
        half3 normalTS = half3(tilt, sqrt(saturate(1.0h - dot(tilt, tilt))));

        surface.albedo += albedo[layer].rgb * half3(look.rgb) * blend[layer];
        surface.normalTS += normalTS * blend[layer];
        surface.smoothness += mask[layer].a * half(settings.x) * blend[layer];
        surface.metallic += mask[layer].r * half(settings.y) * blend[layer];
        surface.occlusion += mask[layer].g * blend[layer];
        surface.wetness += half(settings.z) * blend[layer];
        surface.snowCover += half(settings.w) * blend[layer];
        surface.height += mask[layer].b * blend[layer];
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
