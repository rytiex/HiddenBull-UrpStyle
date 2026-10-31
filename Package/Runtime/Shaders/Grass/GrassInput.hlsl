#ifndef HB_GRASS_INPUT_INCLUDED
#define HB_GRASS_INPUT_INCLUDED

float4 _HB_GrassGrid;
float4 _HB_GrassReach;
float4 _HB_GrassCamera;

#define HB_GRASS_SPACING     _HB_GrassGrid.x
#define HB_GRASS_SIDE        ((uint)_HB_GrassGrid.y)
#define HB_GRASS_PER_SLOT    ((uint)_HB_GrassGrid.z)
#define HB_GRASS_DISTANCE    _HB_GrassReach.x
#define HB_GRASS_RING        _HB_GrassReach.y
#define HB_GRASS_BAND        _HB_GrassReach.z
#define HB_GRASS_RINGS       ((uint)_HB_GrassReach.w)
#define HB_GRASS_CAMERA      _HB_GrassCamera.xyz

#define HB_GRASS_HEIGHT_MOST 3.0
#define HB_GRASS_HEIGHT_BITS 1023.0
#define HB_GRASS_WIDTH_MOST  2.0
#define HB_GRASS_WIDTH_BITS  255.0
#define HB_GRASS_RAND_BITS   1023.0

struct HBGrassBlade
{
    float3 root;
    float height;
    float width;
    uint layer;
    float rand;
    float3 ground;
    uint level;
    uint bloom;
    float patches;
    float snowy;
};

uint2 HB_GrassHash(uint2 v)
{
    v = v * 1664525u + 1013904223u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    v.x += v.y * 1664525u;
    v.y += v.x * 1664525u;
    v ^= v >> 16u;
    return v;
}

float2 HB_GrassRandom(int2 grid, uint salt)
{
    return float2(HB_GrassHash(asuint(grid) + uint2(salt, salt * 7u)) >> 8u) * (1.0 / 16777216.0);
}

int2 HB_GrassGrid(int2 cell, uint local)
{
    uint side = HB_GRASS_SIDE;

    return cell * (int)side + int2(local % side, local / side);
}

float HB_GrassScale(uint level)
{
    return float(1u << level);
}

float2 HB_GrassSpot(int2 grid, uint level)
{
    return (float2(grid) + HB_GrassRandom(grid, level * 8u)) * (HB_GRASS_SPACING * HB_GrassScale(level));
}

uint HB_GrassPack(float height, float width, uint layer, float rand)
{
    uint tall = clamp((uint)round(height / HB_GRASS_HEIGHT_MOST * HB_GRASS_HEIGHT_BITS), 1u, 1023u);
    uint wide = clamp((uint)round(width / HB_GRASS_WIDTH_MOST * HB_GRASS_WIDTH_BITS), 1u, 255u);
    uint seed = (uint)(saturate(rand) * HB_GRASS_RAND_BITS);

    return tall | (wide << 10u) | ((layer & 15u) << 18u) | (seed << 22u);
}

uint HB_GrassPackColour(float3 colour)
{
    uint3 bytes = (uint3)round(sqrt(saturate(colour)) * 255.0);

    return bytes.r | (bytes.g << 8u) | (bytes.b << 16u);
}

HBGrassBlade HB_GrassUnpack(uint3 data, int2 cell, uint local, uint level)
{
    HBGrassBlade blade;

    blade.root = float3(0.0, asfloat(data.x), 0.0);
    blade.root.xz = HB_GrassSpot(HB_GrassGrid(cell, local), level);
    blade.level = level;
    blade.bloom = (data.z >> 24u) & 15u;
    blade.patches = float((data.z >> 28u) & 7u) / 7.0;
    blade.snowy = float(data.z >> 31u);
    blade.height = float(data.y & 1023u) / HB_GRASS_HEIGHT_BITS * HB_GRASS_HEIGHT_MOST;
    blade.width = float((data.y >> 10u) & 255u) / HB_GRASS_WIDTH_BITS * HB_GRASS_WIDTH_MOST;
    blade.layer = (data.y >> 18u) & 15u;
    blade.rand = float(data.y >> 22u) / HB_GRASS_RAND_BITS;

    float3 colour = float3(data.z & 255u, (data.z >> 8u) & 255u, (data.z >> 16u) & 255u) / 255.0;

    blade.ground = colour * colour;
    return blade;
}

float HB_GrassReachShare(float gap)
{
    return max(gap / (HB_GRASS_RING * 0.5), 1.0);
}

float HB_GrassOuter(uint level)
{
    return level + 1u >= HB_GRASS_RINGS ? HB_GRASS_DISTANCE * (1.0 - HB_GRASS_BAND * 0.5)
                                        : HB_GRASS_RING * HB_GrassScale(level);
}

float HB_GrassKeep(float gap, uint level)
{
    float share = HB_GrassScale(level) / HB_GrassReachShare(gap);
    float outer = HB_GrassOuter(level);
    float outerBand = max(outer * HB_GRASS_BAND, 1e-3);
    float keep = saturate(share * share) * saturate((outer + outerBand * 0.5 - gap) / outerBand);

    if (level > 0u)
    {
        float inner = HB_GRASS_RING * HB_GrassScale(level - 1u);
        float innerBand = max(inner * HB_GRASS_BAND, 1e-3);

        keep *= saturate((gap - inner + innerBand * 0.5) / innerBand);
    }

    return keep;
}

#endif
