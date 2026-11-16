float HB_Ring(int2 texel, float radius)
{
    float sum = 0.0;

    [unroll]
    for (int tap = 0; tap < AO_TAPS; tap++)
    {
        float angle = tap * (TAU / AO_TAPS);
        int2 offset = (int2)round(float2(cos(angle), sin(angle)) * radius);

        sum += HB_GroundAt(texel + offset);
    }

    return sum / AO_TAPS;
}

[numthreads(8, 8, 1)]
void Derive(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    int2 texel = (int2)id.xy;
    float size = _Size.x;
    float scale = size / 256.0;
    float height = HB_HeightAt(texel);

    float left = HB_HeightAt(texel + int2(-1, 0));
    float right = HB_HeightAt(texel + int2(1, 0));
    float down = HB_HeightAt(texel + int2(0, -1));
    float up = HB_HeightAt(texel + int2(0, 1));

    float slope = HB_DEPTH * size * 0.5;
    float3 normal = normalize(float3((left - right) * slope, (down - up) * slope, 1.0));

    float reach = max(HB_LIGHT_WIDTH * scale, 1.0);
    float around = HB_Ring(texel, reach);
    float curve = (around - height) * HB_DEPTH * size / reach * CURVE_GAIN;
    float edge = saturate(-curve);
    float cavity = saturate(curve);

    float occlusion = 0.0;
    float radius = 2.0 * scale;

    [unroll]
    for (int ring = 0; ring < AO_RINGS; ring++)
    {
        occlusion += saturate((HB_Ring(texel, radius) - height) * HB_DEPTH * size / radius);
        radius *= 2.0;
    }

    float ao = saturate(1.0 - occlusion / AO_RINGS * AO_GAIN);

    _Detail[id.xy] = float4(edge, cavity, ao, 0.0);
    _NormalPreview[id.xy] = float4(1.0, normal.y * 0.5 + 0.5, normal.z * 0.5 + 0.5, normal.x * 0.5 + 0.5);
    _NormalExport[id.xy] = float4(normal * 0.5 + 0.5, 1.0);
}

[numthreads(8, 8, 1)]
void Compose(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float2 uv = HB_Uv(id.xy);
    int units = HB_UNITS;
    float4 cell = _CellsIn[id.xy];
    float4 detail = _DetailIn[id.xy];
    float height = _HeightIn[id.xy];
    uint key = HB_StoneKey(cell.y);
    float pixel = units * _Size.y;
    float stone = saturate(cell.x / pixel + 0.5);

    float grain = (HB_Fbm(uv, units * 16, HB_SEED + 61u) - 0.5) * _PieceTone.x * 2.0;
    float blotch = saturate((HB_Fbm(uv, 3, HB_SEED + 95u) - 0.5) * 3.0 + 0.5);
    float piece = HB_FILL ? blotch : HB_StoneRandom(key, 4u);
    float spot = 0.5 + (piece - 0.5) * 2.0 * _PieceTone.y + (height - 0.5) * 2.0 * _PieceTone.z;
    float3 rock = HB_Ramp(spot).rgb * (1.0 + grain);
    float3 ground = _GapColour.rgb * (1.0 + grain * 0.5);

    ground *= 1.0 + (blotch - 0.5) * _PieceTone.y + (height - 0.5) * _PieceTone.z * 0.5;

    float3 albedo = saturate(lerp(ground, rock, stone));
    float smoothness = lerp(HB_GAP_SMOOTH, HB_SMOOTHNESS, stone);

    _Albedo[id.xy] = float4(albedo, 1.0);
    _Mask[id.xy] = float4(HB_METALLIC * stone, detail.z, height, smoothness);
}

[numthreads(8, 8, 1)]
void Light(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float4 detail = _DetailIn[id.xy];
    float4 colour = _Albedo[id.xy];
    float4 material = _Mask[id.xy];
    float light = 1.0 + detail.x * HB_EDGE_LIGHT - detail.y * HB_CAVITY_DARK;

    _Albedo[id.xy] = float4(saturate(colour.rgb * light * lerp(1.0, detail.z, HB_AO_BAKE)), 1.0);
    _Mask[id.xy] = float4(material.r, detail.z, _HeightIn[id.xy], material.a);
}

float3 HB_ToDisplay(float3 value)
{
    return pow(saturate(value), 2.2);
}

[numthreads(8, 8, 1)]
void Show(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float4 detail = _DetailIn[id.xy];
    float3 colour;

    if (_ShowMode == 0)
        colour = _AlbedoIn[id.xy].rgb;
    else if (_ShowMode == 1)
        colour = HB_ToDisplay(_NormalIn[id.xy].rgb);
    else if (_ShowMode == 2)
        colour = HB_ToDisplay(_MaskIn[id.xy].rgb);
    else if (_ShowMode == 3)
        colour = HB_ToDisplay(_HeightIn[id.xy].xxx);
    else if (_ShowMode == 4)
        colour = HB_ToDisplay(detail.zzz);
    else if (_ShowMode == 5)
        colour = HB_ToDisplay(0.5 + (detail.x - detail.y) * 0.5).xxx;
    else
        colour = _EmissionIn[id.xy].rgb;

    _Display[id.xy] = float4(colour, 1.0);
}

float3 HB_LinearToSrgb(float3 value)
{
    value = saturate(value);

    return value <= 0.0031308 ? value * 12.92 : 1.055 * pow(value, 1.0 / 2.4) - 0.055;
}

[numthreads(8, 8, 1)]
void Encode(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    _Encoded[id.xy] = float4(HB_LinearToSrgb(_AlbedoIn[id.xy].rgb), 1.0);
}

[numthreads(8, 8, 1)]
void Composite(uint3 id : SV_DispatchThreadID)
{
    if (any((int2)id.xy >= HB_SIZE))
        return;

    float4 colour = _AlbedoIn[id.xy];
    float4 material = _MaskIn[id.xy];
    float4 glow = _EmissionIn[id.xy];
    float height = _HeightIn[id.xy];

    if (_Blend.y < 0.5)
    {
        float below = _BelowHeight[id.xy];
        float cover = saturate(_Blend.x);

        if (_Blend.z > 0.5)
        {
            height += _BlendMore.x;
            cover *= saturate((height - below) * lerp(2.0, 60.0, _Blend.w) + 0.5);
        }

        colour = lerp(_BelowAlbedo[id.xy], colour, cover);
        material = lerp(_BelowMask[id.xy], material, cover);
        glow = lerp(_BelowEmission[id.xy], glow, cover);
        height = lerp(below, height, cover);
        material.z = height;
    }

    _Albedo[id.xy] = colour;
    _Mask[id.xy] = material;
    _Emission[id.xy] = glow;
    _StackHeight[id.xy] = height;
}
