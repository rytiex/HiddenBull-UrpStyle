#ifndef HIDDENBULL_URPSTYLE_GRASS_INSTANCE_INCLUDED
#define HIDDENBULL_URPSTYLE_GRASS_INSTANCE_INCLUDED

struct HiddenBullGrassInstance
{
    float3 position;
    float height;
    float width;
    uint root;
    uint tip;
    uint ground;
};

uint HB_GrassPackColour(float3 colour, float extra)
{
    uint3 bits = (uint3)round(sqrt(saturate(colour)) * 255.0);

    return bits.r | (bits.g << 8) | (bits.b << 16) | ((uint)round(saturate(extra) * 255.0) << 24);
}

float4 HB_GrassUnpackColour(uint bits)
{
    float4 unpacked = float4(bits & 255u, (bits >> 8) & 255u, (bits >> 16) & 255u, bits >> 24) / 255.0;

    return float4(unpacked.rgb * unpacked.rgb, unpacked.a);
}

uint HB_GrassPackGround(float3 normal, float2 limits)
{
    uint2 slope = (uint2)round(saturate(normal.xz * 0.5 + 0.5) * 1023.0);
    uint2 weather = (uint2)round(saturate(limits) * 63.0);

    return slope.x | (slope.y << 10) | (weather.x << 20) | (weather.y << 26);
}

void HB_GrassUnpackGround(uint bits, out float3 normal, out float2 limits)
{
    float2 xz = float2(bits & 1023u, (bits >> 10) & 1023u) / 1023.0 * 2.0 - 1.0;

    normal = float3(xz.x, sqrt(saturate(1.0 - dot(xz, xz))), xz.y);
    limits = float2((bits >> 20) & 63u, bits >> 26) / 63.0;
}

#endif
