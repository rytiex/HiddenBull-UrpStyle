#ifndef HB_TERRAIN_GRASS_INCLUDED
#define HB_TERRAIN_GRASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainInput.hlsl"

TEXTURE2D(_HB_GrassMap);

float4 _HB_LayerGrass[16];

#define HB_GRASS_MOST_HEIGHT 3.0h

struct HBTerrainGrass
{
    half density;
    half height;
};

HBTerrainGrass HB_TerrainGrass(float2 uv)
{
    half3 best;
    int3 ids;
    HB_TerrainPicks(uv, best, ids);

    half total = max(best.x + best.y + best.z, half(HB_EPSILON));
    half density = 0.0h;
    half height = 0.0h;
    half cover = 0.0h;

    UNITY_UNROLL
    for (int pick = 0; pick < 3; pick++)
    {
        float4 layer = _HB_LayerGrass[ids[pick]];
        half share = best[pick] / total;

        density += half(layer.x) * share;
        height += half(layer.x * layer.y) * share;
        cover += layer.x > 0.0 ? share : 0.0h;
    }

    half4 map = half4(SAMPLE_TEXTURE2D_LOD(_HB_GrassMap, sampler_LinearClamp, uv, 0));

    HBTerrainGrass grass;
    grass.density = lerp(density, map.r * cover, map.b);
    grass.height = lerp(height / max(density, half(HB_EPSILON)), map.g * HB_GRASS_MOST_HEIGHT, map.a);
    return grass;
}

#endif
