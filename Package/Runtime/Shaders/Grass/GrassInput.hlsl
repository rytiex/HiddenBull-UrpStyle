#ifndef HB_GRASS_INPUT_INCLUDED
#define HB_GRASS_INPUT_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassNoise.hlsl"

float4 _HB_GrassGrid;
float4 _HB_GrassReach;
float4 _HB_GrassCamera;
float4 _HB_GrassRoots;

#define HB_GRASS_SPACING     _HB_GrassGrid.x
#define HB_GRASS_SIDE        ((uint)_HB_GrassGrid.y)
#define HB_GRASS_PER_SLOT    ((uint)_HB_GrassGrid.z)
#define HB_GRASS_PIXEL       _HB_GrassGrid.w
#define HB_GRASS_DISTANCE    _HB_GrassReach.x
#define HB_GRASS_RING        _HB_GrassReach.y
#define HB_GRASS_BAND        _HB_GrassReach.z
#define HB_GRASS_RINGS       ((uint)_HB_GrassReach.w)
#define HB_GRASS_CAMERA      _HB_GrassCamera.xyz
#define HB_GRASS_ROOT_LOW    _HB_GrassRoots.x
#define HB_GRASS_ROOT_RANGE  _HB_GrassRoots.y
#define HB_GRASS_CLOSE       (HB_GRASS_RING * 0.3)
#define HB_GRASS_CLOSE_PIXELS 12.0
#define HB_GRASS_MID_PIXELS  5.0

#define HB_GRASS_HEIGHT_MOST 3.0
#define HB_GRASS_HEIGHT_BITS 1023.0
#define HB_GRASS_WIDTH_MOST  2.0
#define HB_GRASS_WIDTH_BITS  255.0
#define HB_GRASS_RAND_BITS   1023.0
#define HB_GRASS_ROOT_BITS   65535.0
#define HB_GRASS_LEAN_BITS   255.0
#define HB_GRASS_SWAY_MOST   1.5
#define HB_GRASS_SWAY_BITS   1023.0
#define HB_GRASS_DROP_BITS   63.0
#define HB_GRASS_STATE_BITS  255.0
#define HB_GRASS_THIN_SOFT   0.08

struct HBGrassBlade
{
    float3 root;
    float3 normal;
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

struct HBGrassDrawn
{
    uint index;
    uint motion;
    uint state;
};

struct HBGrassMotion
{
    float3 sway;
    float streak;
    float wet;
    float snow;
    float press;
    float fade;
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

uint HB_GrassPackRoot(float rootHeight, float3 normal)
{
    float share = saturate((rootHeight - HB_GRASS_ROOT_LOW) / HB_GRASS_ROOT_RANGE);
    uint2 lean = (uint2)round(saturate(normal.xz * 0.5 + 0.5) * HB_GRASS_LEAN_BITS);

    return (uint)round(share * HB_GRASS_ROOT_BITS) | (lean.x << 16u) | (lean.y << 24u);
}

uint HB_GrassPackColour(float3 colour)
{
    uint3 bytes = (uint3)round(sqrt(saturate(colour)) * 255.0);

    return bytes.r | (bytes.g << 8u) | (bytes.b << 16u);
}

HBGrassBlade HB_GrassUnpack(uint3 data, int2 cell, uint local, uint level)
{
    HBGrassBlade blade;

    float2 lean = float2((data.x >> 16u) & 255u, data.x >> 24u) / HB_GRASS_LEAN_BITS * 2.0 - 1.0;

    blade.normal = float3(lean.x, sqrt(saturate(1.0 - dot(lean, lean))), lean.y);
    blade.root = float3(0.0, HB_GRASS_ROOT_LOW + float(data.x & 65535u) / HB_GRASS_ROOT_BITS * HB_GRASS_ROOT_RANGE, 0.0);
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

uint HB_GrassPackMotion(float3 sway, float streak)
{
    uint2 side = (uint2)round(saturate(sway.xz / (HB_GRASS_SWAY_MOST * 2.0) + 0.5) * HB_GRASS_SWAY_BITS);
    uint drop = (uint)round(saturate(-sway.y) * HB_GRASS_DROP_BITS);
    uint gust = (uint)round(saturate(streak) * HB_GRASS_DROP_BITS);

    return side.x | (side.y << 10u) | (drop << 20u) | (gust << 26u);
}

uint HB_GrassPackState(float wet, float snow, float press, float fade)
{
    uint4 bytes = (uint4)round(saturate(float4(wet, snow, press, fade)) * HB_GRASS_STATE_BITS);

    return bytes.x | (bytes.y << 8u) | (bytes.z << 16u) | (bytes.w << 24u);
}

HBGrassMotion HB_GrassMotion(HBGrassDrawn drawn)
{
    HBGrassMotion motion;
    float2 side = float2(drawn.motion & 1023u, (drawn.motion >> 10u) & 1023u) / HB_GRASS_SWAY_BITS - 0.5;

    motion.sway = float3(side.x, 0.0, side.y) * (HB_GRASS_SWAY_MOST * 2.0);
    motion.sway.y = -float((drawn.motion >> 20u) & 63u) / HB_GRASS_DROP_BITS;
    motion.streak = float(drawn.motion >> 26u) / HB_GRASS_DROP_BITS;
    motion.wet = float(drawn.state & 255u) / HB_GRASS_STATE_BITS;
    motion.snow = float((drawn.state >> 8u) & 255u) / HB_GRASS_STATE_BITS;
    motion.press = float((drawn.state >> 16u) & 255u) / HB_GRASS_STATE_BITS;
    motion.fade = float(drawn.state >> 24u) / HB_GRASS_STATE_BITS;
    return motion;
}

float HB_GrassPixels(float height, float gap)
{
    return height / max(gap * HB_GRASS_PIXEL, 1e-5);
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
