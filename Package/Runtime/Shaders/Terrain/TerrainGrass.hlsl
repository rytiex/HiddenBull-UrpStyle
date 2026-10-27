#ifndef HB_TERRAIN_GRASS_INCLUDED
#define HB_TERRAIN_GRASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainInput.hlsl"

TEXTURE2D(_HB_GrassMap);

float4 _HB_LayerGrass[16];

#define HB_GRASS_MOST_HEIGHT 3.0h
#define HB_GRASS_EDGE_LOW    0.25h
#define HB_GRASS_EDGE_HIGH   0.75h
#define HB_GRASS_TAPER_LOW   0.5h
#define HB_GRASS_TAPER_HIGH  0.95h
#define HB_GRASS_EDGE_HEIGHT 0.3h
#define HB_GRASS_EDGE_DENSITY 0.5h

struct HBTerrainGrass
{
    half density;
    half height;
};

half HB_TerrainMaskHeight(float2 layerUV, int layer)
{
    float3 weights;
    float2 vertex1, vertex2, vertex3;
    HB_TerrainHexGrid(layerUV, weights, vertex1, vertex2, vertex3);

    weights /= max(max(max(weights.x, weights.y), weights.z), HB_EPSILON);
    weights = pow(saturate(weights), HB_TERRAIN_HEX_SHARPNESS);
    weights /= max(weights.x + weights.y + weights.z, HB_EPSILON);

    float height = SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_LayerMask, sampler_HB_LayerAlbedo,
                                              layerUV + HB_TerrainHexOffset(vertex1), layer, 0).b * weights.x
                 + SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_LayerMask, sampler_HB_LayerAlbedo,
                                              layerUV + HB_TerrainHexOffset(vertex2), layer, 0).b * weights.y
                 + SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_LayerMask, sampler_HB_LayerAlbedo,
                                              layerUV + HB_TerrainHexOffset(vertex3), layer, 0).b * weights.z;

    return half(height);
}

HBTerrainGrass HB_TerrainGrassPick(float2 uv, half pick, out int layer, out half3 shares, out int3 ids)
{
    half3 best;
    HB_TerrainPicks(uv, best, ids);

    float2 local = uv * HB_TERRAIN_SIZE.xz;
    half3 key;

    UNITY_UNROLL
    for (int candidate = 0; candidate < 3; candidate++)
    {
        float4 st = _HB_LayerST[ids[candidate]];
        half height = best[candidate] > 0.0h ? HB_TerrainMaskHeight(local * st.xy + st.zw, ids[candidate]) : 0.5h;

        key[candidate] = best[candidate] > 0.0h ? best[candidate] + (height - 0.5h) * HB_TERRAIN_EDGE_HEIGHT : -1.0h;
    }

    half top = max(max(key.x, key.y), key.z);
    half3 blend = max(key - top + HB_TERRAIN_EDGE_DEPTH, 0.0h);
    half total = max(blend.x + blend.y + blend.z, half(HB_EPSILON));
    half weight = max(best.x + best.y + best.z, half(HB_EPSILON));
    half density = 0.0h;
    half height = 0.0h;
    half cover = 0.0h;
    half meadow = 0.0h;
    half3 grown;

    UNITY_UNROLL
    for (int index = 0; index < 3; index++)
    {
        float4 grass = _HB_LayerGrass[ids[index]];
        half share = blend[index] / total;
        half grows = smoothstep(HB_GRASS_EDGE_LOW, HB_GRASS_EDGE_HIGH, share);

        shares[index] = share;
        meadow += grass.x > 0.0 ? best[index] / weight : 0.0h;

        grown[index] = half(grass.x) * grows;
        density += grown[index];
        height += half(grass.x * grass.y) * grows;
        cover += grass.x > 0.0 ? grows : 0.0h;
    }

    half choice = pick * max(density, half(HB_EPSILON));

    layer = choice < grown.x ? ids.x : choice < grown.x + grown.y ? ids.y : ids.z;

    half4 map = half4(SAMPLE_TEXTURE2D_LOD(_HB_GrassMap, sampler_LinearClamp, uv, 0));

    half taper = smoothstep(HB_GRASS_TAPER_LOW, HB_GRASS_TAPER_HIGH, meadow);

    HBTerrainGrass result;
    result.density = lerp(density, map.r * cover, map.b) * lerp(HB_GRASS_EDGE_DENSITY, 1.0h, taper);
    result.height = lerp(height / max(density, half(HB_EPSILON)), map.g * HB_GRASS_MOST_HEIGHT, map.a)
                  * lerp(HB_GRASS_EDGE_HEIGHT, 1.0h, taper);
    return result;
}

HBTerrainGrass HB_TerrainGrass(float2 uv)
{
    int layer;
    half3 shares;
    int3 ids;
    return HB_TerrainGrassPick(uv, 0.5h, layer, shares, ids);
}

#endif
