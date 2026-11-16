#define PI           3.14159265
#define TAU          6.28318531
#define AO_RINGS     4
#define AO_TAPS      8
#define CURVE_GAIN   0.5
#define AO_GAIN      2.0
#define FACETS       7
#define WRAP         64
#define WIDE         4096
#define KNOTS        2

#define KIND_VORONOI     0
#define KIND_COURSES     1
#define KIND_HERRINGBONE 2
#define KIND_BASKET      3
#define KIND_SCATTER     4
#define KIND_FILL        5
#define RAMP_STOPS       4

RWTexture2D<float4> _Cells;
RWTexture2D<float4> _Frame;
RWTexture2D<float> _Height;
RWTexture2D<float4> _Detail;
RWTexture2D<float4> _NormalPreview;
RWTexture2D<float4> _NormalExport;
RWTexture2D<float4> _Albedo;
RWTexture2D<float4> _Mask;
RWTexture2D<float4> _Display;
RWTexture2D<float4> _Encoded;
RWTexture2D<float> _StackHeight;
RWTexture2D<float> _Lift;
RWTexture2D<float4> _Emission;

Texture2D<float4> _CellsIn;
Texture2D<float4> _FrameIn;
Texture2D<float> _HeightIn;
Texture2D<float> _LiftIn;
Texture2D<float4> _DetailIn;
Texture2D<float4> _NormalIn;
Texture2D<float4> _AlbedoIn;
Texture2D<float4> _MaskIn;
Texture2D<float4> _BelowAlbedo;
Texture2D<float4> _BelowMask;
Texture2D<float> _BelowHeight;
Texture2D<float4> _EmissionIn;
Texture2D<float4> _BelowEmission;

float4 _Size;
float4 _Layout;
float4 _Joint;
float4 _Pattern;
float4 _Shape;
float4 _ShapeMore;
float4 _PieceTone;
float4 _GapColour;
float4 _Scatter;
float4 _Ground;
float4 _GroundMore;
float4 _Surface;
float4 _Light;
float4 _Blend;
float4 _BlendMore;

float4 _Ramp[RAMP_STOPS];
float4 _RampAt;
int _RampCount;
int _ShowMode;

#define HB_SIZE         ((int)_Size.x)
#define HB_KIND         ((int)_Layout.x)
#define HB_UNITS        ((int)_Layout.y)
#define HB_JITTER       _Layout.z
#define HB_PER_ROW      ((int)_Layout.w)
#define HB_GAP          _Joint.x
#define HB_ROUNDNESS    _Joint.y
#define HB_CHIPPING     _Joint.z
#define HB_ROW_OFFSET   _Joint.w
#define HB_RANDOM_SHIFT (_Pattern.x > 0.5)
#define HB_LENGTH       ((int)_Pattern.y)
#define HB_BEVEL        _Shape.x
#define HB_DOME         _Shape.y
#define HB_FACETS       _Shape.z
#define HB_RISE_SPREAD  _Shape.w
#define HB_TILT         _ShapeMore.x
#define HB_ROUGHNESS    _ShapeMore.y
#define HB_SEED         ((uint)_ShapeMore.z)
#define HB_DEPTH        _Surface.x
#define HB_SMOOTHNESS   _Surface.y
#define HB_GAP_SMOOTH   _Surface.z
#define HB_METALLIC     _Surface.w
#define HB_EDGE_LIGHT   _Light.x
#define HB_CAVITY_DARK  _Light.y
#define HB_AO_BAKE      _Light.z
#define HB_LIGHT_WIDTH  _Light.w
#define HB_ROUGH_SMOOTH 0.08
#define HB_DENSITY      _Scatter.x
#define HB_PEBBLE       _Scatter.y
#define HB_ELONGATION   _Scatter.z
#define HB_FINE_CRACKS  _Scatter.w
#define HB_ORGANIC      (_Pattern.w > 0.5)
#define HB_FILL         (HB_KIND == KIND_FILL)
struct HB_Piece
{
    float edge;
    float2 offset;
    uint2 key;
    float4 frame;
};

uint HB_Hash(uint2 cell, uint seed)
{
    uint value = cell.x * 73856093u ^ cell.y * 19349663u ^ seed * 83492791u;

    value ^= value >> 13;
    value *= 0x5bd1e995u;
    value ^= value >> 15;

    return value;
}

float HB_Random(uint2 cell, uint seed)
{
    return (HB_Hash(cell, seed) & 0xFFFFu) / 65535.0;
}

float2 HB_Random2(uint2 cell, uint seed)
{
    return float2(HB_Random(cell, seed), HB_Random(cell, seed + 7919u));
}

uint2 HB_WrapCell(int2 cell, int count)
{
    return (uint2)(cell + count * WRAP) % (uint)count;
}

int2 HB_Wrap(int2 texel)
{
    int size = HB_SIZE;

    return (int2)((uint2)(texel + size * 4) % (uint)size);
}

float HB_HeightAt(int2 texel)
{
    return _HeightIn[HB_Wrap(texel)];
}

float HB_GroundAt(int2 texel)
{
    int2 wrapped = HB_Wrap(texel);

    return _HeightIn[wrapped] - _LiftIn[wrapped];
}

float2 HB_Uv(uint2 id)
{
    return (id + 0.5) * _Size.y;
}

float HB_Noise(float2 p, int period, uint seed)
{
    float2 lattice = floor(p);
    float2 f = p - lattice;
    int2 c = (int2)lattice;

    f = f * f * (3.0 - 2.0 * f);

    float a = HB_Random(HB_WrapCell(c, period), seed);
    float b = HB_Random(HB_WrapCell(c + int2(1, 0), period), seed);
    float d = HB_Random(HB_WrapCell(c + int2(0, 1), period), seed);
    float e = HB_Random(HB_WrapCell(c + int2(1, 1), period), seed);

    return lerp(lerp(a, b, f.x), lerp(d, e, f.x), f.y);
}

float HB_Fbm(float2 uv, int period, uint seed)
{
    float sum = 0.0;
    float amplitude = 0.5;
    float total = 0.0;

    period = max(period, 1);

    [unroll]
    for (int octave = 0; octave < 4; octave++)
    {
        sum += HB_Noise(uv * period, period, seed + (uint)octave * 101u) * amplitude;
        total += amplitude;
        amplitude *= 0.5;
        period *= 2;
    }

    return sum / total;
}

float2 HB_Gradient(int2 cell, int period, uint seed)
{
    float angle = HB_Random(HB_WrapCell(cell, period), seed) * TAU;

    return float2(cos(angle), sin(angle));
}

float HB_Perlin(float2 p, int period, uint seed)
{
    float2 lattice = floor(p);
    float2 f = p - lattice;
    int2 c = (int2)lattice;
    float2 w = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float a = dot(HB_Gradient(c, period, seed), f);
    float b = dot(HB_Gradient(c + int2(1, 0), period, seed), f - float2(1.0, 0.0));
    float d = dot(HB_Gradient(c + int2(0, 1), period, seed), f - float2(0.0, 1.0));
    float e = dot(HB_Gradient(c + int2(1, 1), period, seed), f - 1.0);

    return lerp(lerp(a, b, w.x), lerp(d, e, w.x), w.y) * 0.7 + 0.5;
}

float HB_PerlinFbm(float2 uv, int period, uint seed)
{
    float sum = 0.0;
    float amplitude = 0.5;
    float total = 0.0;

    period = max(period, 1);

    [unroll]
    for (int octave = 0; octave < 5; octave++)
    {
        sum += HB_Perlin(uv * period, period, seed + (uint)octave * 101u) * amplitude;
        total += amplitude;
        amplitude *= 0.5;
        period *= 2;
    }

    return sum / total;
}

float4 HB_Ramp(float t)
{
    float4 colour = _Ramp[0];

    [unroll]
    for (int i = 1; i < RAMP_STOPS; i++)
    {
        float span = max(_RampAt[i] - _RampAt[i - 1], 1e-4);

        if (i < _RampCount)
            colour = lerp(colour, _Ramp[i], saturate((t - _RampAt[i - 1]) / span));
    }

    return colour;
}

float HB_Organic(float2 uv, int period, uint seed)
{
    float2 warp = float2(HB_PerlinFbm(uv, period, seed + 1u), HB_PerlinFbm(uv, period, seed + 2u)) - 0.5;

    return HB_PerlinFbm(uv + warp * 0.5 / max(period, 1), period, seed);
}

float HB_SoftMin(float a, float b, float k)
{
    float low = min(a, b);

    return low - k * log(exp((low - a) / k) + exp((low - b) / k));
}

float HB_BoxEdge(float2 local, float2 size, float soft)
{
    return HB_SoftMin(min(local.x, size.x - local.x), min(local.y, size.y - local.y), soft);
}
