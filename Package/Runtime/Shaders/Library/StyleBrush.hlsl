#ifndef HIDDENBULL_URPSTYLE_BRUSH_INCLUDED
#define HIDDENBULL_URPSTYLE_BRUSH_INCLUDED

#include "StyleCommon.hlsl"

TEXTURE2D(_HB_BrushAtlas);
SAMPLER(sampler_HB_BrushAtlas);

float4 _HB_BrushParams;
float4 _HB_BrushVariation;

#define HB_BRUSH_TILES_PER_UNIT  _HB_BrushParams.x
#define HB_BRUSH_FADE_START      _HB_BrushParams.y
#define HB_BRUSH_FADE_RATE       _HB_BrushParams.z
#define HB_BRUSH_ATLAS_BOUND     _HB_BrushParams.w

#define HB_BRUSH_RANDOMIZE       _HB_BrushVariation.x
#define HB_BRUSH_GROW            _HB_BrushVariation.y
#define HB_BRUSH_GROW_START      _HB_BrushVariation.z

#define HB_BRUSH_SPINE_RANGE     0.0625
#define HB_BRUSH_SEAM_FOOTPRINT  0.25
#define HB_BRUSH_LIGHTMAP_SEAM   0.02

#define HB_BRUSH_PLANE_CUTOFF    0.02h
#define HB_BRUSH_PATCH_SIZE      1.7
#define HB_BRUSH_PATCH_JITTER    0.8
#define HB_BRUSH_PATCH_BAND      0.2
#define HB_BRUSH_PATCH_SOFT      0.15
#define HB_BRUSH_PATCH_OFFSET    8.0
#define HB_BRUSH_PATCH_TURN      0.18
#define HB_BRUSH_FIELD_ANGLE     1.57
#define HB_BRUSH_FIELD_SCALE     24.0
#define HB_BRUSH_FLOW_ANGLE      0.2
#define HB_BRUSH_FLOW_STRETCH    0.15
#define HB_BRUSH_MAX_OCTAVE      3.0
#define HB_BRUSH_OCTAVE_BLEND    0.75

struct HiddenBullBrushSample
{
    half3 spine;
    half coverage;
    float3 offset;
    half3 normalShift;
    half growth;
};

struct HiddenBullBrushTap
{
    half2 spine;
    half coverage;
};

struct HiddenBullBrushLayer
{
    half3 spine;
    half coverage;
};

struct HiddenBullBrushSpace
{
    float3 position;
    half3 normal;
    half localSpace;
};

HiddenBullBrushSample HB_NoBrush()
{
    HiddenBullBrushSample brush;
    brush.spine = half3(0.0h, 0.0h, 0.0h);
    brush.coverage = 0.0h;
    brush.offset = float3(0.0, 0.0, 0.0);
    brush.normalShift = half3(0.0h, 0.0h, 0.0h);
    brush.growth = 1.0h;
    return brush;
}

float3 HB_ObjectScale()
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();

    return float3(
        length(objectToWorld._m00_m10_m20),
        length(objectToWorld._m01_m11_m21),
        length(objectToWorld._m02_m12_m22));
}

float3 HB_ObjectSpacePosition(float3 positionWS)
{
    float3 positionOS = mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;

    return positionOS * HB_ObjectScale();
}

half3 HB_ObjectSpaceNormal(half3 normalWS)
{
    return SafeNormalize(mul(normalWS, (half3x3)GetObjectToWorldMatrix()));
}

HiddenBullBrushSpace HB_ResolveBrushSpace(float3 positionWS, half3 normalWS, half objectSpace,
                                          float3 anchorOS)
{
    HiddenBullBrushSpace space;

#if defined(HB_ANCHOR_REST)
    half local = saturate(objectSpace);

    space.position = lerp(positionWS, anchorOS * HB_ObjectScale(), local);
    space.normal = lerp(normalWS, HB_ObjectSpaceNormal(normalWS), local);
    space.localSpace = local;
#elif defined(_HB_BRUSH_ANCHOR)
    space.position = anchorOS * HB_ObjectScale();
    space.normal = HB_ObjectSpaceNormal(normalWS);
    space.localSpace = 1.0h;
#else
    space.position = lerp(positionWS, HB_ObjectSpacePosition(positionWS), objectSpace);
    space.normal = lerp(normalWS, HB_ObjectSpaceNormal(normalWS), objectSpace);
    space.localSpace = objectSpace;
#endif

    return space;
}

half3 HB_TriplanarBlend(half3 normal)
{
    half3 blend = abs(normal);
    blend *= blend;
    blend *= blend;

    return blend / max(blend.x + blend.y + blend.z, HB_EPSILON);
}

float3 HB_BrushHash(float2 cell)
{
    uint2 q = uint2(int2(cell));
    uint n = (q.x * 1597334673u) ^ (q.y * 3812015801u);
    n = (n ^ (n >> 16u)) * 2246822519u;

    uint3 v = uint3(n, n * 747796405u + 2891336453u, n * 2654435761u + 374761393u);
    v ^= v >> 15u;

    return float3(v & 0xFFFFFFu) * (1.0 / 16777215.0);
}

HiddenBullBrushTap HB_BrushRead(float2 uv, float2 uvDx, float2 uvDy)
{
    half4 atlas = SAMPLE_TEXTURE2D_GRAD(_HB_BrushAtlas, sampler_HB_BrushAtlas, uv, uvDx, uvDy);

    HiddenBullBrushTap tap;
    tap.spine = atlas.rg * 2.0h - 1.0h;
    tap.coverage = atlas.b;
    return tap;
}

float HB_BrushField(float2 x)
{
    float2 i = floor(x);
    float2 f = x - i;
    f = f * f * (3.0 - 2.0 * f);

    float a = HB_BrushHash(i).x;
    float b = HB_BrushHash(i + float2(1.0, 0.0)).x;
    float c = HB_BrushHash(i + float2(0.0, 1.0)).x;
    float d = HB_BrushHash(i + float2(1.0, 1.0)).x;

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y) * 2.0 - 1.0;
}

float2 HB_BrushSite(float2 cell)
{
    return cell + 0.5 + (HB_BrushHash(cell + 7919.0).xy - 0.5) * HB_BRUSH_PATCH_JITTER;
}

HiddenBullBrushTap HB_BrushPatch(float2 p, float2 pDx, float2 pDy, float2 cell)
{
    float3 hash = HB_BrushHash(cell);
    float amount = HB_BRUSH_RANDOMIZE;

    float2 site = HB_BrushSite(cell) * HB_BRUSH_PATCH_SIZE;

    float angle = (HB_BRUSH_FIELD_ANGLE * HB_BrushField(site / HB_BRUSH_FIELD_SCALE)
                 + HB_BRUSH_PATCH_TURN * (hash.x * 2.0 - 1.0)
                 + HB_BRUSH_FLOW_ANGLE * sin(p.x * 0.21 + 1.7 * sin(p.y * 0.13))) * amount;
    float stretch = 1.0 + HB_BRUSH_FLOW_STRETCH * amount * sin(p.y * 0.17 + 1.3 * sin(p.x * 0.11));

    float s, c;
    sincos(angle, s, c);
    float2x2 rotation = float2x2(c, -s, s, c) * stretch;

    float2 uv = mul(rotation, p - site) + hash.yz * HB_BRUSH_PATCH_OFFSET;

    HiddenBullBrushTap tap = HB_BrushRead(uv, mul(rotation, pDx), mul(rotation, pDy));
    tap.spine = half2(mul(float2(tap.spine), float2x2(c, -s, s, c)));

    return tap;
}

HiddenBullBrushTap HB_BrushPlane(float2 p, float2 pDx, float2 pDy)
{
    UNITY_BRANCH
    if (HB_BRUSH_RANDOMIZE <= 0.0)
        return HB_BrushRead(p, pDx, pDy);

    float2 q = p / HB_BRUSH_PATCH_SIZE;
    float2 base = floor(q);

    float nearest = 1e6;
    float second = 1e6;
    float2 ownCell = base;
    float2 otherCell = base;

    UNITY_UNROLL
    for (int y = -1; y <= 1; y++)
    {
        UNITY_UNROLL
        for (int x = -1; x <= 1; x++)
        {
            float2 cell = base + float2(x, y);
            float2 offset = HB_BrushSite(cell) - q;
            float gap = dot(offset, offset);

            if (gap < nearest)
            {
                second = nearest;
                otherCell = ownCell;
                nearest = gap;
                ownCell = cell;
            }
            else if (gap < second)
            {
                second = gap;
                otherCell = cell;
            }
        }
    }

    HiddenBullBrushTap own = HB_BrushPatch(p, pDx, pDy, ownCell);

    float edge = sqrt(second) - sqrt(nearest);

    UNITY_BRANCH
    if (edge < HB_BRUSH_PATCH_BAND)
    {
        HiddenBullBrushTap other = HB_BrushPatch(p, pDx, pDy, otherCell);

        half inside = half(edge / HB_BRUSH_PATCH_BAND);
        half weight = saturate((other.coverage - own.coverage - inside) / HB_BRUSH_PATCH_SOFT + 0.5h);

        own.spine = lerp(own.spine, other.spine, weight);
        own.coverage = lerp(own.coverage, other.coverage, weight);
    }

    return own;
}

int HB_BrushLargest(half3 weights)
{
    return weights.x >= weights.y ? (weights.x >= weights.z ? 0 : 2) : (weights.y >= weights.z ? 1 : 2);
}

float2 HB_BrushAxis(float3 value, int axis)
{
    return axis == 0 ? value.zy : axis == 1 ? value.xz : value.xy;
}

half3 HB_BrushLift(half2 spine, int axis)
{
    return axis == 0 ? half3(0.0h, spine.y, spine.x)
         : axis == 1 ? half3(spine.x, 0.0h, spine.y)
                     : half3(spine.x, spine.y, 0.0h);
}

HiddenBullBrushLayer HB_BrushLayer(float3 position, float3 positionDx, float3 positionDy, half3 blend,
                                   float frequency)
{
    float3 uvw = position * frequency;
    float3 uvwDx = positionDx * frequency;
    float3 uvwDy = positionDy * frequency;

    int first = HB_BrushLargest(blend);
    half3 rest = blend * half3(first != 0, first != 1, first != 2);
    int second = HB_BrushLargest(rest);

    half firstWeight = max(blend.x, max(blend.y, blend.z));
    half secondWeight = max(rest.x, max(rest.y, rest.z));
    int planes = secondWeight > HB_BRUSH_PLANE_CUTOFF ? 2 : 1;
    half total = firstWeight + (planes > 1 ? secondWeight : 0.0h);

    HiddenBullBrushLayer layer;
    layer.spine = half3(0.0h, 0.0h, 0.0h);
    layer.coverage = 0.0h;

    UNITY_LOOP
    for (int plane = 0; plane < planes; plane++)
    {
        int axis = plane == 0 ? first : second;
        half weight = (plane == 0 ? firstWeight : secondWeight) / total;

        HiddenBullBrushTap tap = HB_BrushPlane(HB_BrushAxis(uvw, axis), HB_BrushAxis(uvwDx, axis),
                                               HB_BrushAxis(uvwDy, axis));

        layer.spine += HB_BrushLift(tap.spine, axis) * weight;
        layer.coverage += tap.coverage * weight;
    }

    return layer;
}

HiddenBullBrushSample HB_SampleBrush(float3 positionWS, half3 normalWS, half objectSpace,
                                     float3 anchorOS, half scale, half mask)
{
    HiddenBullBrushSample brush = HB_NoBrush();

    HiddenBullBrushSpace space = HB_ResolveBrushSpace(positionWS, normalWS, objectSpace, anchorOS);

    float3 positionDx = ddx(space.position);
    float3 positionDy = ddy(space.position);

    if (HB_BRUSH_ATLAS_BOUND < 0.5 || mask <= 0.0h)
        return brush;

    float distanceToCamera = distance(positionWS, GetCameraPositionWS());
    half fade = half(saturate(1.0 - (distanceToCamera - HB_BRUSH_FADE_START) * HB_BRUSH_FADE_RATE));

    if (fade > 0.0h)
    {
        half3 blend = HB_TriplanarBlend(space.normal);
        float frequency = HB_BRUSH_TILES_PER_UNIT / scale;

        float growth = 1.0;
        float coarser = 0.0;

        UNITY_BRANCH
        if (HB_BRUSH_GROW > 0.5)
        {
            float octave = clamp(log2(max(distanceToCamera / HB_BRUSH_GROW_START, 1.0)), 0.0, HB_BRUSH_MAX_OCTAVE);
            float level = floor(octave);

            growth = exp2(level);
            coarser = level < HB_BRUSH_MAX_OCTAVE ? smoothstep(HB_BRUSH_OCTAVE_BLEND, 1.0, octave - level) : 0.0;
        }

        HiddenBullBrushLayer layer;
        layer.spine = half3(0.0h, 0.0h, 0.0h);
        layer.coverage = 0.0h;

        int octaves = coarser > 0.0 ? 2 : 1;

        UNITY_LOOP
        for (int octave = 0; octave < octaves; octave++)
        {
            half share = half(octave == 0 ? 1.0 - coarser : coarser);
            HiddenBullBrushLayer part = HB_BrushLayer(space.position, positionDx, positionDy, blend,
                                                      frequency / (growth * (octave + 1.0)));

            layer.spine += part.spine * share;
            layer.coverage += part.coverage * share;
        }

        growth *= 1.0 + coarser;

        half3 rotated = SafeNormalize(mul((half3x3)GetObjectToWorldMatrix(), layer.spine)) * length(layer.spine);

        brush.spine = lerp(layer.spine, rotated, space.localSpace) * (fade * mask);
        brush.coverage = (layer.coverage * 2.0h - 1.0h) * (fade * mask);
        brush.growth = half(growth);
    }

    return brush;
}

float3 HB_BrushStrokeOffset(half3 spine, half3 normalWS, half scale)
{
    float3 normal = float3(normalWS);
    float3 offset = float3(spine) * (HB_BRUSH_SPINE_RANGE * scale / max(HB_BRUSH_TILES_PER_UNIT, 1e-4));

    return offset - normal * dot(offset, normal);
}

float2 HB_BrushScreenOffset(float3 offset, float3 positionWS, half3 normalWS)
{
    float3 dpdx = ddx(positionWS);
    float3 dpdy = ddy(positionWS);

    float3 edgeY = cross(dpdy, float3(normalWS));
    float3 edgeX = cross(float3(normalWS), dpdx);

    float determinant = dot(dpdx, edgeY);

    if (abs(determinant) < 1e-18)
        return float2(0.0, 0.0);

    return float2(dot(offset, edgeY), dot(offset, edgeX)) / determinant;
}

float2 HB_BrushUvShift(float2 screen, float2 uvDdx, float2 uvDdy, float seam)
{
    float2 footprint = max(abs(uvDdx), abs(uvDdy));

    if (max(footprint.x, footprint.y) > seam)
        return float2(0.0, 0.0);

    return screen.x * uvDdx + screen.y * uvDdy;
}

#endif
