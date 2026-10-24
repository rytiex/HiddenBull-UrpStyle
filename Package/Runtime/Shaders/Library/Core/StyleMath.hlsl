#ifndef HB_STYLE_MATH_INCLUDED
#define HB_STYLE_MATH_INCLUDED

#define HB_EPSILON 1e-4

float HB_Remap01(float x, float from, float to)
{
    return saturate((x - from) / (to - from));
}

uint HB_PcgHash(uint v)
{
    uint state = v * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;

    return (word >> 22u) ^ word;
}

uint HB_PcgHash(uint2 v)
{
    return HB_PcgHash(v.x ^ HB_PcgHash(v.y));
}

uint HB_PcgHash(uint3 v)
{
    return HB_PcgHash(v.x ^ HB_PcgHash(v.y ^ HB_PcgHash(v.z)));
}

float HB_HashToUnit(uint h)
{
    return float(h >> 8u) * (1.0 / 16777216.0);
}

float HB_Hash12(float2 p)
{
    return HB_HashToUnit(HB_PcgHash(asuint(int2(floor(p)))));
}

float HB_Hash13(float3 p)
{
    return HB_HashToUnit(HB_PcgHash(asuint(int3(floor(p)))));
}

float2 HB_Hash22(float2 p)
{
    uint h = HB_PcgHash(asuint(int2(floor(p))));

    return float2(HB_HashToUnit(h), HB_HashToUnit(HB_PcgHash(h)));
}

#endif
