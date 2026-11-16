float2 HB_Point(int2 cell, int count, uint seed)
{
    return 0.5 + (HB_Random2(HB_WrapCell(cell, count), seed) - 0.5) * HB_JITTER;
}

HB_Piece HB_Voronoi(float2 p, int count, uint seed, float soft)
{
    int2 base = (int2)floor(p);
    float2 f = p - base;
    float best = 1e9;
    int2 bestCell = 0;
    float2 bestVector = 0.0;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            int2 cell = int2(x, y);
            float2 toward = cell + HB_Point(base + cell, count, seed) - f;
            float distance = dot(toward, toward);

            if (distance < best)
            {
                best = distance;
                bestCell = cell;
                bestVector = toward;
            }
        }
    }

    float hard = 1e9;
    float distances[25];

    [unroll]
    for (int j = 0; j < 25; j++)
    {
        int2 cell = bestCell + int2(j % 5 - 2, j / 5 - 2);
        float2 toward = cell + HB_Point(base + cell, count, seed) - f;
        float2 across = toward - bestVector;

        distances[j] = dot(across, across) > 1e-6
            ? dot(0.5 * (bestVector + toward), normalize(across))
            : 1e9;
        hard = min(hard, distances[j]);
    }

    float sum = 0.0;

    [unroll]
    for (int k = 0; k < 25; k++)
        sum += exp((hard - distances[k]) / soft);

    HB_Piece piece;
    piece.edge = hard - soft * log(sum);
    piece.offset = -bestVector;
    piece.key = HB_WrapCell(base + bestCell, count);
    piece.frame = float4(-bestVector.x + 0.5, -bestVector.y + 0.5, 0.0, 1.0);
    return piece;
}

float HB_Joint(int column, uint row, int perRow, float stretch, uint seed)
{
    uint wrapped = HB_WrapCell(int2(column, 0), perRow).x;

    return (column + (HB_Random(uint2(wrapped, row), seed + 97u) - 0.5) * HB_JITTER * 0.8) * stretch;
}

HB_Piece HB_Courses(float2 p, int rows, int perRow, float soft, uint seed)
{
    float stretch = rows / (float)perRow;
    int row = (int)floor(p.y);
    uint course = HB_WrapCell(int2(row, 0), rows).x;
    float shift = HB_RANDOM_SHIFT
        ? HB_Random(uint2(course, 91u), seed) * stretch
        : ((course & 1u) != 0u ? HB_ROW_OFFSET * stretch : 0.0);
    float along = p.x + shift;
    int first = (int)floor(along / stretch);
    int column = first;
    float start = first * stretch;
    float end = start + stretch;

    [unroll]
    for (int k = -1; k <= 1; k++)
    {
        float a = HB_Joint(first + k, course, perRow, stretch, seed);
        float b = HB_Joint(first + k + 1, course, perRow, stretch, seed);

        if (along >= a && along < b)
        {
            column = first + k;
            start = a;
            end = b;
        }
    }

    float2 size = float2(end - start, 1.0);
    float2 local = float2(along - start, p.y - row);

    HB_Piece piece;
    piece.edge = HB_BoxEdge(local, size, soft);
    piece.offset = local - size * 0.5;
    piece.key = uint2(HB_WrapCell(int2(column, 0), perRow).x, course);
    piece.frame = float4(local.x, local.y, 0.0, size.x);
    return piece;
}

HB_Piece HB_Herringbone(float2 p, int length, int period, float soft)
{
    float2 guess = float2((p.x + p.y) * 0.5, (p.x - p.y) / (2.0 * length));
    int2 base = (int2)floor(guess);
    float2 local = 0.0;
    float2 size = float2(length, 1.0);
    float2 centre = 0.0;
    float upright = 0.0;

    [loop]
    for (int i = -3; i <= 3; i++)
    {
        [unroll]
        for (int j = -2; j <= 2; j++)
        {
            int2 lattice = base + int2(i, j);
            float2 origin = float2(lattice.x + length * lattice.y, lattice.x - length * lattice.y);
            float2 q = p - origin;

            if (q.x >= 0.0 && q.x < length && q.y >= 0.0 && q.y < 1.0)
            {
                local = q;
                size = float2(length, 1.0);
                centre = origin + size * 0.5;
                upright = 0.0;
            }
            else if (q.x >= 0.0 && q.x < 1.0 && q.y >= 1.0 && q.y < 1.0 + length)
            {
                local = q - float2(0.0, 1.0);
                size = float2(1.0, length);
                centre = origin + float2(0.5, 1.0 + length * 0.5);
                upright = 1.0;
            }
        }
    }

    HB_Piece piece;
    piece.edge = HB_BoxEdge(local, size, soft);
    piece.offset = local - size * 0.5;
    piece.key = HB_WrapCell((int2)floor(centre * 2.0), period * 2) + uint2((uint)upright * 65536u, 0u);
    piece.frame = upright > 0.5
        ? float4(local.y, local.x, 1.0, size.y)
        : float4(local.x, local.y, 0.0, size.x);
    return piece;
}

HB_Piece HB_Basket(float2 p, int length, int squares, float soft)
{
    int2 square = (int2)floor(p / length);
    uint2 wrapped = HB_WrapCell(square, squares);
    float2 inside = p - square * length;
    bool lying = ((wrapped.x + wrapped.y) & 1u) == 0u;
    float brick = floor(lying ? inside.y : inside.x);
    float2 local = lying ? float2(inside.x, inside.y - brick) : float2(inside.x - brick, inside.y);
    float2 size = lying ? float2(length, 1.0) : float2(1.0, length);

    HB_Piece piece;
    piece.edge = HB_BoxEdge(local, size, soft);
    piece.offset = local - size * 0.5;
    piece.key = uint2(wrapped.x * 16u + (uint)brick, wrapped.y);
    piece.frame = lying ? float4(local.x, local.y, 0.0, length) : float4(local.y, local.x, 1.0, length);
    return piece;
}

struct HB_Pebble
{
    bool on;
    uint2 wrapped;
    float2 centre;
    float2 turned;
    float distance;
    float radius;
    float power;
};

HB_Pebble HB_PebbleAt(int2 cell, int2 base, float2 f, int count, float size, uint seed, float pack)
{
    HB_Pebble pebble;
    uint2 wrapped = HB_WrapCell(cell, count);

    pebble.wrapped = wrapped;
    pebble.on = HB_Random(wrapped, seed + 5u) < HB_DENSITY;
    pebble.centre = cell - base + HB_Point(cell, count, seed);

    float turn = HB_Random(wrapped, seed + 6u) * TAU;
    float stretch = 1.0 + HB_Random(wrapped, seed + 8u) * HB_ELONGATION;
    float pick = HB_Random(wrapped, seed + 9u);
    float radius = size * (0.4 + 0.6 * pick * pick);
    float grown = radius + pack * 1.5;
    float2 toward = f - pebble.centre;

    pebble.turned = float2(dot(toward, float2(cos(turn), sin(turn))),
                           dot(toward, float2(-sin(turn), cos(turn))) * stretch);
    pebble.distance = length(pebble.turned);

    float angle = atan2(pebble.turned.y, pebble.turned.x);
    float lobes = sin(angle * 2.0 + HB_Random(wrapped, seed + 12u) * TAU) * 0.5 +
                  sin(angle * 3.0 + HB_Random(wrapped, seed + 13u) * TAU) * 0.3 +
                  sin(angle * 5.0 + HB_Random(wrapped, seed + 14u) * TAU) * 0.2;

    pebble.radius = radius * (1.0 + (0.15 + HB_CHIPPING * 0.3) * lobes) + pack * 1.5;
    pebble.power = dot(toward, toward) - grown * grown;
    return pebble;
}

float HB_RoundMin(float a, float b, float k)
{
    float h = max(k - abs(a - b), 0.0) / k;

    return min(a, b) - h * h * k * 0.25;
}

void HB_ScatterPass(float2 p, int count, float size, uint seed, float unit, uint tag, float pack,
                    inout float best, inout float nearest, inout HB_Piece piece)
{
    int2 base = (int2)floor(p);
    float2 f = p - base;
    int reach = pack > 0.0 ? 2 : 1;
    uint side = (uint)(reach * 2 + 1);
    float winnerScore = -1e9;
    HB_Pebble winner = (HB_Pebble)0;
    uint winnerIndex = 0u;

    [loop]
    for (uint j = 0u; j < side * side; j++)
    {
        int2 cell = base + int2((int)(j % side) - reach, (int)(j / side) - reach);
        HB_Pebble pebble = HB_PebbleAt(cell, base, f, count, size, seed, pack);

        if (!pebble.on)
            continue;

        float disc = pebble.radius - pebble.distance;

        nearest = max(nearest, disc * unit);

        float score = -pebble.power;

        if (pack <= 0.0)
        {
            if (disc <= 0.0)
                continue;

            uint stone = HB_Hash(pebble.wrapped + uint2(0u, tag), HB_SEED + 3u) & 0xFFFFFFu;
            float rise = 1.0 - HB_RISE_SPREAD * HB_Random(uint2(stone, 2u), HB_SEED);

            score = rise * (1.0 - pebble.distance * pebble.distance / (pebble.radius * pebble.radius));
        }

        if (score > winnerScore)
        {
            winnerScore = score;
            winner = pebble;
            winnerIndex = j;
        }
    }

    if (winnerScore <= -1e8)
        return;

    float edge = winner.radius - winner.distance;

    if (edge <= 0.0)
        return;

    if (pack > 0.0)
    {
        float round = 0.3 * size;

        [loop]
        for (uint k = 0u; k < side * side; k++)
        {
            if (k == winnerIndex)
                continue;

            int2 cell = base + int2((int)(k % side) - reach, (int)(k / side) - reach);
            HB_Pebble other = HB_PebbleAt(cell, base, f, count, size, seed, pack);

            if (!other.on)
                continue;

            float apart = max(length(other.centre - winner.centre), 1e-4);

            edge = HB_RoundMin(edge, (other.power - winner.power) / (2.0 * apart), round);
        }

        if (edge <= 0.0)
            return;
    }

    best = 0.0;
    piece.edge = edge * unit;
    piece.offset = winner.turned * unit;
    piece.key = winner.wrapped + uint2(0u, tag);
    piece.frame = float4((winner.turned + winner.radius) * unit, 0.0, winner.radius * 2.0 * unit);
}
HB_Piece HB_Scatter(float2 p, int count, uint seed)
{
    float best = -1.0;
    float nearest = -1e9;
    float pack = smoothstep(0.6, 1.0, HB_DENSITY);

    HB_Piece piece;
    piece.edge = -1.0;
    piece.offset = 0.0;
    piece.key = 0u;
    piece.frame = float4(0.0, 0.0, 0.0, 1.0);

    HB_ScatterPass(p, count, HB_PEBBLE, seed, 1.0, 0u, pack, best, nearest, piece);

    if (best < 0.0)
        HB_ScatterPass(p * 3.0, count * 3, HB_PEBBLE * 0.45, seed + 200u, 1.0 / 3.0, 0x10000u, 0.0, best,
                       nearest, piece);

    if (best < 0.0)
        piece.edge = nearest;

    return piece;
}

[numthreads(8, 8, 1)]
void Cells(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float2 uv = HB_Uv(id.xy);
    int units = HB_UNITS;
    uint seed = HB_SEED;
    float soft = max(HB_ROUNDNESS * 0.15, 0.002);

    float2 warp = float2(HB_Fbm(uv, units * 2, seed + 11u), HB_Fbm(uv, units * 2, seed + 23u)) - 0.5;
    float2 p = uv * units + warp * HB_CHIPPING * 0.6;

    HB_Piece piece;

    if (HB_KIND == KIND_COURSES)
        piece = HB_Courses(p, units, HB_PER_ROW, soft, seed);
    else if (HB_KIND == KIND_HERRINGBONE)
        piece = HB_Herringbone(p, HB_LENGTH, units, soft);
    else if (HB_KIND == KIND_SCATTER)
        piece = HB_Scatter(p, units, seed);
    else if (HB_KIND == KIND_BASKET)
        piece = HB_Basket(p, HB_LENGTH, (int)((uint)units / (uint)max(HB_LENGTH, 1)), soft);
    else if (HB_KIND == KIND_FILL)
    {
        piece.edge = 4.0;
        piece.offset = 0.0;
        piece.key = 0u;

        float span = max((int)((uint)units / 2u), 1);

        piece.frame = float4(uv.x * 20.0 * span, uv.y * span, 2.0, 20.0 * span);
    }
    else
    {
        piece = HB_Voronoi(p, units, seed, soft);

        if (HB_FINE_CRACKS > 0.0)
        {
            int finePeriod = units * 8;
            float2 jag = float2(HB_PerlinFbm(uv, finePeriod, seed + 501u), HB_PerlinFbm(uv, finePeriod, seed + 502u));
            HB_Piece fine = HB_Voronoi(p * 2.0 + (jag - 0.5) * 0.5, units * 2, seed + 500u, 0.002);
            float reach = HB_PerlinFbm(uv, units * 3, seed + 503u);
            float start = lerp(0.72, 0.3, HB_FINE_CRACKS);
            float open = smoothstep(start, start + 0.12, reach);
            float fade = (1.0 - open) * (HB_BEVEL + 0.02) * 2.0;

            piece.edge = min(piece.edge, fine.edge * 0.5 + HB_GAP * 0.5 - HB_GAP * 0.3 * open + fade);
        }
    }

    float edge = piece.edge + (HB_Fbm(uv, units * 8, seed + 31u) - 0.5) * HB_CHIPPING * 0.08;
    float2 shape = piece.offset / (length(piece.offset) + max(edge, 1e-4));
    float stone = (HB_Hash(piece.key, seed + 3u) & 0xFFFFFFu) / 16777215.0;

    _Cells[id.xy] = float4(edge - HB_GAP * 0.5, stone, shape);
    _Frame[id.xy] = piece.frame;
}

uint HB_StoneKey(float stone)
{
    return (uint)(stone * 16777215.0 + 0.5);
}

float HB_StoneRandom(uint key, uint salt)
{
    return HB_Random(uint2(key, salt), HB_SEED);
}

void HB_WoodGrain(float4 frame, uint key, float ringDensity, float knots, uint seed, out float rings,
                  out float fibres, out float knot)
{
    float along = frame.x;
    float across = frame.y;
    float span = frame.z > 1.5 ? frame.w / 20.0 : 0.0;
    int bendPeriod = span > 0.0 ? (int)round(span * 7.0) : WIDE;
    int wobblePeriod = span > 0.0 ? (int)round(span * 3.0) : WIDE;
    int fibrePeriod = span > 0.0 ? (int)round(span * 40.0) : WIDE;
    float density = span > 0.0 ? max(round(ringDensity * span), 1.0) / span : ringDensity;
    float rows = span > 0.0 ? span : 1.0;
    float phase = HB_StoneRandom(key, 30u) * 300.0;

    float bend = (HB_Noise(float2(along * 0.35 + phase, 0.5), bendPeriod, seed) - 0.5) * 0.8;
    float wobble = (HB_Noise(float2(along * 1.5 + phase, across * 3.0), wobblePeriod, seed + 1u) - 0.5) * 0.15;
    float wave = across + bend + wobble;

    knot = 0.0;

    [unroll]
    for (int k = 0; k < KNOTS; k++)
    {
        uint salt = 32u + (uint)k * 5u;
        float chance = knots * (k == 0 ? 1.0 : 0.4);

        if (HB_StoneRandom(key, salt) >= chance)
            continue;

        float2 centre = float2(HB_StoneRandom(key, salt + 1u) * frame.w,
                               (0.2 + 0.6 * HB_StoneRandom(key, salt + 2u)) * rows);
        float radius = 0.06 + 0.08 * HB_StoneRandom(key, salt + 3u);
        float2 reach = float2((along - centre.x) / (radius * 3.0), (across - centre.y) / (radius * 1.6));

        wave += (across - centre.y) * exp(-dot(reach, reach)) * 1.5;
        knot = max(knot, saturate(1.0 - length(float2(along - centre.x, across - centre.y)) / radius));
    }

    float ring = frac(wave * density);

    rings = smoothstep(0.6, 1.0, 1.0 - abs(ring * 2.0 - 1.0));
    fibres = HB_Noise(float2(along * 2.0 + phase, across * 40.0), fibrePeriod, seed + 2u);
}

float HB_Ground(float2 uv)
{
    int period = (int)_Ground.y;
    float lumps = HB_Fbm(uv, period, HB_SEED + 41u);

    if (HB_ORGANIC)
    {
        float grit = HB_PerlinFbm(uv, period * 8, HB_SEED + 47u) - 0.5;

        lumps = saturate(HB_Organic(uv, period, HB_SEED + 41u) + grit * 0.12);
    }

    if (_Ground.z <= 0.0)
        return _Ground.x * lumps;

    int count = max((int)_Ground.w, 1);
    float bend = (HB_PerlinFbm(uv, 2, HB_SEED + 43u) - 0.5) * 3.0 + (HB_PerlinFbm(uv, 5, HB_SEED + 44u) - 0.5) +
                 (lumps - 0.5) * 0.5;
    float wave = frac(uv.x * count + bend * _GroundMore.x * 2.0);
    float ripple = smoothstep(0.0, 1.0, wave < 0.75 ? wave / 0.75 : (1.0 - wave) / 0.25);
    float strength = smoothstep(0.25, 0.75, HB_PerlinFbm(uv, 3, HB_SEED + 45u)) * 0.7 + 0.3;
    float fineBend = (HB_PerlinFbm(uv, 4, HB_SEED + 46u) - 0.5) * 2.0 + bend * 0.5;
    float fine = frac((uv.x * 3.0 + uv.y) * count + fineBend * _GroundMore.x * 2.0);
    float shaped = ripple * strength * 0.75 + (1.0 - abs(fine * 2.0 - 1.0)) * 0.1 + lumps * 0.25;

    return _Ground.x * lerp(lumps, shaped, saturate(_Ground.z));
}

float HB_CellEdge(float2 p, int count, uint seed)
{
    int2 base = (int2)floor(p);
    float2 f = p - base;
    float best = 1e9;
    float2 closest = 0.0;

    [unroll]
    for (int j = 0; j < 9; j++)
    {
        int2 cell = base + int2(j % 3 - 1, j / 3 - 1);
        float2 toward = cell - base + HB_Random2(HB_WrapCell(cell, count), seed) - f;
        float distance = dot(toward, toward);

        if (distance < best)
        {
            best = distance;
            closest = toward;
        }
    }

    float edge = 1e9;

    [unroll]
    for (int k = 0; k < 9; k++)
    {
        int2 cell = base + int2(k % 3 - 1, k / 3 - 1);
        float2 toward = cell - base + HB_Random2(HB_WrapCell(cell, count), seed) - f;
        float2 split = toward - closest;

        if (dot(split, split) > 1e-6)
            edge = min(edge, dot((closest + toward) * 0.5, normalize(split)));
    }

    return edge;
}

float HB_StreakNoise(float2 uv, int2 period, uint seed)
{
    float sum = 0.0;
    float amplitude = 0.5;
    float total = 0.0;

    [unroll]
    for (int octave = 0; octave < 3; octave++)
    {
        float2 p = uv * period;
        float2 lattice = floor(p);
        float2 t = p - lattice;
        int2 c = (int2)lattice;
        float2 w = t * t * (3.0 - 2.0 * t);
        uint s = seed + (uint)octave * 101u;

        float a = HB_Random((uint2)(c + period * WRAP) % (uint2)period, s);
        float b = HB_Random((uint2)(c + int2(1, 0) + period * WRAP) % (uint2)period, s);
        float d = HB_Random((uint2)(c + int2(0, 1) + period * WRAP) % (uint2)period, s);
        float e = HB_Random((uint2)(c + int2(1, 1) + period * WRAP) % (uint2)period, s);

        sum += lerp(lerp(a, b, w.x), lerp(d, e, w.x), w.y) * amplitude;
        total += amplitude;
        amplitude *= 0.5;
        period *= 2;
    }

    return sum / total;
}

float HB_StoneHeight(float2 uv, float4 cell, float floorHeight)
{
    int units = HB_UNITS;
    float edge = cell.x;
    float2 shape = cell.zw;
    uint key = HB_StoneKey(cell.y);
    float pixel = units * _Size.y;

    float rim = sin(saturate(edge / max(HB_BEVEL, pixel)) * PI * 0.5);
    float dome = 1.0 - saturate(dot(shape, shape));
    float facet = 1.0;
    float turn = HB_StoneRandom(key, 1u) * TAU;

    [unroll]
    for (int i = 0; i < FACETS; i++)
    {
        float angle = turn + i * (TAU / FACETS) + (HB_StoneRandom(key, 10u + i) - 0.5) * 0.6;
        float steep = 0.6 + 0.6 * HB_StoneRandom(key, 20u + i);

        facet = min(facet, 1.0 - steep * dot(shape, float2(cos(angle), sin(angle))));
    }

    float surface = lerp(dome, saturate(facet), HB_FACETS);
    float curve = HB_DOME < 0.0 ? HB_DOME : max(HB_DOME, HB_FACETS);
    float top = lerp(1.0, surface, curve) / (1.0 - min(curve, 0.0));
    float rise = 1.0 - HB_RISE_SPREAD * HB_StoneRandom(key, 2u);
    float lean = HB_StoneRandom(key, 3u) * TAU;
    float tilt = HB_TILT * 0.35 * dot(shape, float2(cos(lean), sin(lean)));
    float grain = (HB_Fbm(uv, units * 8, HB_SEED + 51u) - 0.5) * HB_ROUGHNESS * 0.3;

    return max(rim * saturate(rise * top + tilt + grain), floorHeight);
}
[numthreads(8, 8, 1)]
void Shape(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float2 uv = HB_Uv(id.xy);
    float4 cell = _CellsIn[id.xy];
    float floorHeight = HB_Ground(uv);

    float grain = (HB_Fbm(uv, HB_UNITS * 8, HB_SEED + 51u) - 0.5) * HB_ROUGHNESS * 0.3;

    if (HB_FILL)
        _Height[id.xy] = 0.5 + floorHeight - _Ground.x * 0.5 + grain;
    else
        _Height[id.xy] = cell.x > 0.0 ? HB_StoneHeight(uv, cell, floorHeight) : floorHeight;
}
