#ifndef HIDDENBULL_URPSTYLE_TERRAIN_NOISE_INCLUDED
#define HIDDENBULL_URPSTYLE_TERRAIN_NOISE_INCLUDED

#define HB_TERRAIN_EDGE_STROKES 0.3h
#define HB_TERRAIN_EDGE_SCALE   0.35
#define HB_TERRAIN_NOISE_SIZE   64.0

TEXTURE3D(_HB_TerrainNoiseMap);
SAMPLER(s_hb_noise_linear_repeat);

float HB_TerrainHash(int3 cell)
{
    uint3 scrambled = (uint3)cell * uint3(1597334673u, 3812015801u, 2798796415u);
    uint hash = scrambled.x ^ scrambled.y ^ scrambled.z;

    hash ^= hash >> 15;
    hash *= 0x2C1B3C6Du;
    hash ^= hash >> 12;
    hash *= 0x297A2D39u;
    hash ^= hash >> 15;

    return float(hash) * (1.0 / 4294967296.0);
}

half HB_TerrainNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = p - cell;

    f = f * f * (3.0 - 2.0 * f);

    float3 uvw = (cell + f + 0.5) / HB_TERRAIN_NOISE_SIZE;

    return half(SAMPLE_TEXTURE3D_LOD(_HB_TerrainNoiseMap, s_hb_noise_linear_repeat, uvw, 0).r);
}

half HB_TerrainEdge(float3 positionWS, half strokes, half layerStrokes)
{
    float3 edge = positionWS * HB_TERRAIN_EDGE_SCALE;
    half wander = HB_TerrainNoise(edge) * 0.55h + HB_TerrainNoise(edge * 2.7 + 31.0) * 0.3h
                + HB_TerrainNoise(edge * 7.3 + 57.0) * 0.15h - 0.5h;

    return (wander + strokes * HB_TERRAIN_EDGE_STROKES) * layerStrokes;
}

half3 HB_TerrainVariationNoise(float3 positionWS, float4 range, float varySize, float4 varyShape, half broken)
{
    float3 p = positionWS / max(varySize, 0.1) + float3(range.xy, 0.0);

    p += (float3(HB_TerrainNoise(p * 0.9 + 3.1), HB_TerrainNoise(p * 0.9 + 11.7),
                 HB_TerrainNoise(p * 0.9 + 23.9)) - 0.5) * varyShape.x;

    half large = HB_TerrainNoise(p) * 0.6h + HB_TerrainNoise(p * 2.3 + 17.0) * 0.4h - 0.5h;
    half fine = HB_TerrainNoise(p * 6.1 + 41.0) * 0.6h + HB_TerrainNoise(p * 13.7 + 5.0) * 0.4h - 0.5h;
    half drift = HB_TerrainNoise(p * 0.37 + 71.0) - 0.5h;

    half patches = saturate((large + fine * half(varyShape.y) + broken * 0.5h) * half(varyShape.z) + 0.5h);

    return half3(patches, fine, drift);
}

half3 HB_TerrainVariationApply(half3 colour, half4 vary, float4 varyShape, half varyCover, half3 noise)
{
    half patches = noise.x;

    half3 hue = vary.rgb / max(dot(vary.rgb, half3(0.299h, 0.587h, 0.114h)), 0.05h);
    half3 tinted = colour * lerp(half3(1.0h, 1.0h, 1.0h), hue, patches * vary.a);
    half3 painted = lerp(colour, vary.rgb, patches * vary.a);

    colour = lerp(tinted, painted, varyCover);

    return colour * (1.0h + (noise.z + noise.y * 0.5h) * (half(varyShape.w) * vary.a));
}

half3 HB_TerrainVariation(half3 colour, float3 positionWS, float4 range, half4 vary, float varySize,
                          float4 varyShape, half varyCover, half broken)
{
    return HB_TerrainVariationApply(colour, vary, varyShape, varyCover,
                                    HB_TerrainVariationNoise(positionWS, range, varySize, varyShape, broken));
}

#endif
