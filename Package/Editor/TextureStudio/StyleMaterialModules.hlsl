#define MASK_CRACKS      0
#define MASK_SCRATCHES   1
#define MASK_STREAKS     2
#define MASK_WOOD        3
#define MASK_CHIPS       4
#define MASK_CLUMPS      5
#define MASK_LEAVES      6
#define MASK_EDGES       7
#define MASK_CAVITIES    8
#define MASK_HEIGHT      9
#define MASK_NOISE       10
#define MASK_PIECES      11

#define COMBINE_ADD      1
#define COMBINE_SUBTRACT 2

#define HEIGHT_CUT       1
#define HEIGHT_RAISE     2
#define HEIGHT_FLATTEN   3

#define AREA_PIECES      1
#define AREA_JOINTS      2

RWTexture2D<float4> _Marks;
RWTexture2D<float2> _Anchor;
Texture2D<float4> _MarksIn;
Texture2D<float2> _AnchorIn;

float4 _MaskKind;
float4 _MaskShape;
float4 _MaskEdge;
float4 _ModuleMore;
float4 _ModuleSurface;
float4 _ModuleWhere;
float4 _ModuleGlow;

struct HB_Mark
{
    float cover;
    float profile;
    float tone;
    float shade;
    float2 anchor;
};

HB_Mark HB_MarkOf(float cover, float profile, float tone, float shade)
{
    HB_Mark mark;
    mark.cover = cover;
    mark.profile = profile;
    mark.tone = tone;
    mark.shade = shade;
    mark.anchor = float2(-1.0, -1.0);
    return mark;
}
float2 HB_ModuleCracks(float2 uv, float amount, int scale, float size, uint seed)
{
    float2 jag = float2(HB_PerlinFbm(uv, scale * 4, seed + 1u), HB_PerlinFbm(uv, scale * 4, seed + 2u)) - 0.5;
    float edge = HB_CellEdge(uv * scale + jag * 0.45, scale, seed);
    float reach = HB_PerlinFbm(uv, max((int)((uint)scale / 2u), 1), seed + 3u);
    float start = lerp(0.72, 0.3, amount);
    float open = smoothstep(start, start + 0.15, reach);
    float width = max(scale * _Size.y, (0.005 + 0.05 * size) * open);
    float crack = saturate(open * 2.0) * (1.0 - smoothstep(width * 0.4, width, edge));

    return float2(crack, crack);
}

float2 HB_ModuleScratches(float2 uv, float amount, int scale, float size, uint seed)
{
    float2 p = uv * scale;
    int2 base = (int2)floor(p);
    float2 f = p - base;
    float pixel = scale * _Size.y;
    float mark = 0.0;

    [unroll]
    for (int j = 0; j < 9; j++)
    {
        int2 cell = base + int2(j % 3 - 1, j / 3 - 1);
        uint2 wrapped = HB_WrapCell(cell, scale);

        if (HB_Random(wrapped, seed) >= amount)
            continue;

        float2 centre = cell - base + HB_Random2(wrapped, seed + 1u);
        float angle = HB_Random(wrapped, seed + 2u) * PI;
        float reach = (0.1 + 0.9 * size) * (0.4 + 0.6 * HB_Random(wrapped, seed + 3u));
        float2 direction = float2(cos(angle), sin(angle));
        float2 toward = f - centre;
        float along = clamp(dot(toward, direction), -reach, reach);
        float fade = 1.0 - abs(along) / reach;
        float width = max(pixel, (0.0015 + 0.0015 * fade) * scale);
        float stroke = 1.0 - smoothstep(width * 0.5, width, length(toward - direction * along));

        mark = max(mark, stroke * saturate(fade * 3.0) * (0.4 + 0.6 * HB_Random(wrapped, seed + 4u)));
    }

    return float2(mark, mark);
}

float2 HB_ModuleStreaks(float2 uv, float amount, int scale, float size, uint seed)
{
    int stretch = 1 + (int)round(size * 15.0);
    int2 period = int2(scale, max((int)((uint)scale / (uint)stretch), 1));
    float start = lerp(0.68, 0.35, amount);
    float streak = smoothstep(start, start + 0.2, HB_StreakNoise(uv, period, seed));

    return float2(streak, streak);
}

HB_Mark HB_ModuleWood(uint2 texel, float4 cell, float amount, int scale, float size, float knots, uint seed)
{
    float rings = 0.0;
    float fibres = 0.0;
    float knot = 0.0;

    HB_WoodGrain(_FrameIn[texel], HB_StoneKey(cell.y), scale, knots, seed, rings, fibres, knot);

    float streak = saturate(rings * amount + (fibres - 0.3) * size * 0.8);
    float profile = rings * amount * 0.6 + fibres * size * 0.4 - knot * 0.3;

    return HB_MarkOf(max(streak, knot), profile, fibres, lerp(1.0, 0.6, knot));
}

HB_Mark HB_ModuleChips(float2 uv, float4 cell, float amount, int scale, float size, uint seed)
{
    if (cell.x <= 0.0)
        return HB_MarkOf(0.0, 0.0, 0.5, 1.0);

    float2 p = uv * scale;
    int2 base = (int2)floor(p);
    float2 f = p - base;
    float pixel = scale * _Size.y;
    float reach = 0.02 + 0.28 * size;
    float chip = 0.0;

    [unroll]
    for (uint j = 0u; j < 9u; j++)
    {
        int2 near = base + int2((int)(j % 3u) - 1, (int)(j / 3u) - 1);
        uint2 wrapped = HB_WrapCell(near, scale);

        if (HB_Random(wrapped, seed) >= amount)
            continue;

        float2 spot = HB_Random2(wrapped, seed + 1u);
        float4 at = _CellsIn[HB_Wrap((int2)((float2(wrapped) + spot) / scale * HB_SIZE))];

        if (abs(at.y - cell.y) > 1e-6 || at.x <= 0.0 || (!HB_FILL && at.x > reach * 0.7))
            continue;

        float2 toward = f - (near - base + spot);
        float wobble = 1.0 + 0.25 * sin(atan2(toward.y, toward.x) * 3.0 + HB_Random(wrapped, seed + 3u) * TAU);
        float radius = reach * scale / max(HB_UNITS, 1) * (0.6 + 0.5 * HB_Random(wrapped, seed + 2u)) * wobble;

        radius = min(radius, 0.9);
        chip = max(chip, smoothstep(0.0, max(radius * 0.35, pixel), radius - length(toward)));
    }

    float broken = 0.6 + 0.4 * HB_PerlinFbm(uv, scale * 4, seed + 9u);
    float slope = HB_FILL ? 1.0 : lerp(0.3, 1.0, saturate(1.0 - cell.x / max(reach, 1e-3)));
    float tone = saturate((HB_Fbm(uv, scale * 2, seed + 7u) - 0.5) * 3.0 + 0.5);

    return HB_MarkOf(chip, chip * slope * broken, tone, 1.0);
}

HB_Mark HB_ModuleClumps(float2 uv, float amount, int scale, float size, float blades, uint seed)
{
    float pixel = scale * _Size.y;
    HB_Mark mark = HB_MarkOf(0.0, 0.0, 0.5, 1.0);

    [loop]
    for (int grid = 0; grid < 2; grid++)
    {
        float shift = grid * 0.5;
        uint salt = seed + (uint)grid * 31u;
        float2 p = uv * scale + shift;
        int2 base = (int2)floor(p);
        float2 f = p - base;

        [loop]
        for (uint j = 0u; j < 9u; j++)
        {
            int2 cell = base + int2((int)(j % 3u) - 1, (int)(j / 3u) - 1);
            uint2 wrapped = HB_WrapCell(cell, scale);

            if (HB_Random(wrapped, salt) >= amount)
                continue;

            float2 spot = HB_Random2(wrapped, salt + 1u);
            float2 toward = f - (cell - base + spot);
            float radius = (0.15 + 0.8 * size) * (0.7 + 0.3 * HB_Random(wrapped, salt + 2u));
            float distance = length(toward);

            if (distance >= radius)
                continue;

            float reach = distance / radius;
            float dome = sqrt(1.0 - reach * reach) * (0.6 + 0.4 * HB_Random(wrapped, salt + 3u));
            float count = 7.0 + floor(HB_Random(wrapped, salt + 4u) * 6.0);
            float twist = HB_Random(wrapped, salt + 5u) * TAU + reach * 1.5;
            float blade = pow(abs(cos((atan2(toward.y, toward.x) + twist) * count * 0.5)), 6.0 + reach * 20.0);
            float tuft = saturate(blade * 1.5) * (1.0 - reach) * (0.5 + 0.3 * HB_Random(wrapped, salt + 6u));
            float top = lerp(dome, tuft, blades);

            if (top <= mark.profile)
                continue;

            float cover = lerp(saturate((radius - distance) / pixel), saturate(tuft * 20.0), blades);

            mark = HB_MarkOf(cover, top, HB_Random(wrapped, salt + 7u), lerp(1.0, 0.55 + tuft * 0.9, blades));
            mark.anchor = frac((float2(wrapped) + spot - shift) / scale);
        }
    }

    return mark;
}
HB_Mark HB_ModuleLeaves(float2 uv, float amount, int scale, float size, float twigs, uint seed)
{
    float2 p = uv * scale;
    int2 base = (int2)floor(p);
    float2 f = p - base;
    float pixel = scale * _Size.y;
    float order = -1.0;
    HB_Mark mark = HB_MarkOf(0.0, 0.0, 0.5, 1.0);

    [loop]
    for (uint j = 0u; j < 9u; j++)
    {
        int2 cell = base + int2((int)(j % 3u) - 1, (int)(j / 3u) - 1);
        uint2 wrapped = HB_WrapCell(cell, scale);

        if (HB_Random(wrapped, seed) >= amount)
            continue;

        float layer = HB_Random(wrapped, seed + 6u);

        if (layer <= order)
            continue;

        float angle = HB_Random(wrapped, seed + 2u) * TAU;
        float2 direction = float2(cos(angle), sin(angle));
        float2 spot = HB_Random2(wrapped, seed + 1u);
        float2 toward = f - (cell - base + spot);
        float2 local = float2(dot(toward, direction), dot(toward, float2(-direction.y, direction.x)));
        bool twig = HB_Random(wrapped, seed + 3u) < twigs;
        float span = (0.15 + 0.14 * HB_Random(wrapped, seed + 4u)) * (0.5 + size);
        float cover;
        float rib;

        if (twig)
        {
            float reach = min(span * 2.0, 0.95);
            float along = clamp(local.x, -reach, reach);
            float width = pixel * 1.2 + 0.012;

            cover = 1.0 - smoothstep(width * 0.6, width, distance(local, float2(along, 0.0)));
            rib = 0.0;
        }
        else
        {
            float x = local.x / span;
            float curl = local.y + x * x * span * (HB_Random(wrapped, seed + 5u) - 0.5) * 0.3;
            float girth = span * 0.45 * (1.0 - x * x);

            cover = abs(x) < 1.0 ? saturate((girth - abs(curl)) / pixel) : 0.0;
            rib = 1.0 - saturate(abs(curl) / (pixel * 0.8));
        }

        if (cover <= 0.0)
            continue;

        order = layer;
        mark = HB_MarkOf(cover, cover * (twig ? 1.0 : 0.7), HB_Random(wrapped, seed + 7u),
                         twig ? 0.45 : 1.0 - rib * 0.2);
        mark.anchor = frac((float2(wrapped) + spot) / scale);
    }

    return mark;
}

HB_Mark HB_MaskShape(int kind, float2 uv, float amount, int scale, float size, float extra, uint seed)
{
    uint2 texel = (uint2)HB_Wrap((int2)floor(uv * HB_SIZE));
    float4 cell = _CellsIn[texel];
    HB_Mark mark = HB_MarkOf(0.0, 0.0, 0.5, 1.0);

    [branch]
    if (kind == MASK_WOOD)
        mark = HB_ModuleWood(texel, cell, amount, scale, size, extra, seed);
    else if (kind == MASK_CHIPS)
        mark = HB_ModuleChips(uv, cell, amount, scale, size, seed);
    else if (kind == MASK_CLUMPS)
        mark = HB_ModuleClumps(uv, amount, scale, size, extra, seed);
    else if (kind == MASK_LEAVES)
        mark = HB_ModuleLeaves(uv, amount, scale, size, extra, seed);
    else
    {
        float2 plain = float2(0.0, 0.0);

        if (kind == MASK_CRACKS)
            plain = HB_ModuleCracks(uv, amount, scale, size, seed);
        else if (kind == MASK_SCRATCHES)
            plain = HB_ModuleScratches(uv, amount, scale, size, seed);
        else
            plain = HB_ModuleStreaks(uv, amount, scale, size, seed);

        mark = HB_MarkOf(plain.x, plain.y, saturate((HB_Fbm(uv, scale * 2, seed + 7u) - 0.5) * 3.0 + 0.5), 1.0);
    }

    return mark;
}

float HB_MaskPixel(int kind, float2 uv, int scale, uint seed)
{
    int2 texel = HB_Wrap((int2)floor(uv * HB_SIZE));
    float value = _HeightIn[texel];

    if (kind == MASK_EDGES)
        value = saturate(_DetailIn[texel].x * 3.0);
    else if (kind == MASK_CAVITIES)
        value = saturate(_DetailIn[texel].y * 3.0);
    else if (kind == MASK_NOISE)
        value = HB_PerlinFbm(uv, scale, seed);
    else if (kind == MASK_PIECES)
        value = HB_Random(uint2(HB_StoneKey(_CellsIn[texel].y), 90u), seed);

    return value;
}

uint HB_ModuleSeed(float salt)
{
    return HB_SEED * 7919u + (uint)salt * 104729u;
}

[numthreads(8, 8, 1)]
void MaskStart(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float2 uv = HB_Uv(id.xy);
    float tone = saturate((HB_Fbm(uv, 16, HB_ModuleSeed(_ModuleWhere.y) + 7u) - 0.5) * 3.0 + 0.5);

    _Marks[id.xy] = float4(1.0, 1.0, tone, 1.0);
    _Anchor[id.xy] = float2(-1.0, -1.0);
}

[numthreads(8, 8, 1)]
void Mask(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    int kind = (int)_MaskKind.x;
    int blend = (int)_MaskKind.y;
    bool invert = _MaskKind.z > 0.5;
    uint seed = HB_ModuleSeed(_MaskKind.w);
    int scale = max((int)_MaskShape.y, 1);
    float2 uv = HB_Uv(id.xy);
    float2 anchor = _Anchor[id.xy];

    if (_MaskEdge.z > 0.0)
    {
        float2 bend = float2(HB_PerlinFbm(uv, 4, seed + 51u), HB_PerlinFbm(uv, 4, seed + 52u)) - 0.5;

        uv = frac(uv + bend * _MaskEdge.z * 0.1);
    }

    bool shaped = kind < MASK_EDGES;
    HB_Mark mark;

    if (shaped)
    {
        mark = HB_MaskShape(kind, uv, _MaskShape.x, scale, _MaskShape.z, _MaskShape.w, seed);

        if (invert)
        {
            mark.cover = 1.0 - mark.cover;
            mark.profile = 1.0 - mark.profile;
            mark.anchor = float2(-1.0, -1.0);
        }
    }
    else
    {
        float value = HB_MaskPixel(kind, anchor.x >= 0.0 ? anchor : uv, scale, seed);
        float soft = max(_MaskEdge.y, 0.001) * 0.5;
        float pick = smoothstep(_MaskEdge.x - soft, _MaskEdge.x + soft, value);

        pick = invert ? 1.0 - pick : pick;
        mark = HB_MarkOf(pick, pick, 0.5, 1.0);
    }

    float4 marks = _Marks[id.xy];

    if (blend == COMBINE_ADD)
    {
        marks.x = saturate(marks.x + mark.cover);
        marks.y = max(marks.y, mark.profile);
    }
    else if (blend == COMBINE_SUBTRACT)
    {
        marks.x = saturate(marks.x - mark.cover);
        marks.y *= 1.0 - saturate(mark.cover);
    }
    else
    {
        marks.x *= mark.cover;
        marks.y *= mark.profile;
    }

    if (shaped && blend != COMBINE_SUBTRACT)
    {
        float share = saturate(mark.cover);

        marks.zw = lerp(marks.zw, float2(mark.tone, mark.shade), share);

        if (mark.anchor.x >= 0.0 && share > 0.0)
            _Anchor[id.xy] = mark.anchor;
    }

    _Marks[id.xy] = marks;
}

[numthreads(8, 8, 1)]
void Module(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float4 marks = _MarksIn[id.xy];
    float2 anchor = _AnchorIn[id.xy];
    int2 texel = anchor.x >= 0.0 ? HB_Wrap((int2)floor(anchor * HB_SIZE)) : (int2)id.xy;
    float stone = saturate(_CellsIn[texel].x / (HB_UNITS * _Size.y) + 0.5);
    int area = (int)_ModuleWhere.x;
    float where = area == AREA_PIECES ? stone : area == AREA_JOINTS ? 1.0 - stone : 1.0;
    float cover = saturate(marks.x * where);

    if (cover <= 0.0)
        return;

    float height = _HeightIn[id.xy];
    float4 colour = _Albedo[id.xy];
    float4 material = _Mask[id.xy];
    float4 tint = HB_Ramp(0.5 + (marks.z - 0.5) * 2.0 * _ModuleMore.x);
    int mode = (int)_ModuleMore.y;
    float level = _ModuleMore.z;
    float relief = level * marks.y * where;
    float paint = cover;

    if (mode == HEIGHT_FLATTEN)
        paint *= saturate((level - height) / 0.02 + 0.5);

    colour.rgb = lerp(colour.rgb, saturate(tint.rgb * marks.w), paint * tint.a);
    material.r = lerp(material.r, _ModuleSurface.y, paint);
    material.a = lerp(material.a, _ModuleSurface.x, paint);
    _Emission[id.xy] = lerp(_Emission[id.xy], _ModuleGlow, paint * tint.a);

    if (mode == HEIGHT_CUT)
    {
        height -= relief;
    }
    else if (mode == HEIGHT_RAISE)
    {
        height += relief;
        _Lift[id.xy] += relief;
    }
    else if (mode == HEIGHT_FLATTEN)
        height = lerp(height, max(height, level), cover);

    _Albedo[id.xy] = colour;
    _Mask[id.xy] = material;
    _Height[id.xy] = height;
}
