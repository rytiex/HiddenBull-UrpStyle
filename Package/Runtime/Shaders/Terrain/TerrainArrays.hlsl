#ifndef HIDDENBULL_URPSTYLE_TERRAIN_ARRAYS_INCLUDED
#define HIDDENBULL_URPSTYLE_TERRAIN_ARRAYS_INCLUDED

#define HB_TERRAIN_LAYER_LIMIT  32
#define HB_TERRAIN_PICKS        3
#define HB_TERRAIN_EDGE_HEIGHT  0.6h
#define HB_TERRAIN_EDGE_JITTER  0.5h
#define HB_TERRAIN_EDGE_DEPTH   0.02h
#define HB_TERRAIN_GOLDEN       0.618034
#define HB_TERRAIN_HEX_RATE     0.3
#define HB_TERRAIN_HEX_SHARPEN  24.0
#define HB_TERRAIN_HEX_CONTRAST 10.0
#define HB_TERRAIN_HEX_SKIP     0.02
#define HB_TERRAIN_TOP_BIAS     1.4
#define HB_TERRAIN_FACE_SHARPEN 16.0
#define HB_TERRAIN_MACRO_LARGE  24.0
#define HB_TERRAIN_MACRO_SMALL  7.0
#define HB_TERRAIN_MACRO_AMOUNT 0.35h
#define HB_TERRAIN_FAR_START    0.5
#define HB_TERRAIN_FAR_MIP      16.0
#define HB_TERRAIN_PICK_SKIP    0.02h
#define HB_TERRAIN_VARY_SKIP    0.04h
#define HB_TERRAIN_VARY_FADE    0.08h

TEXTURE2D_ARRAY(_HB_TerrainControl);
SAMPLER(sampler_HB_TerrainControl);
TEXTURE2D_ARRAY(_HB_LayerAlbedo);
SAMPLER(sampler_HB_LayerAlbedo);
TEXTURE2D_ARRAY(_HB_LayerNormal);
TEXTURE2D_ARRAY(_HB_LayerMask);

float _HB_TerrainLayerCount;
float _HB_TerrainControlCount;
float4 _HB_TerrainControlTexel;
float4 _HB_LayerST[HB_TERRAIN_LAYER_LIMIT];
float4 _HB_LayerLook[HB_TERRAIN_LAYER_LIMIT];
float4 _HB_LayerWeather[HB_TERRAIN_LAYER_LIMIT];
float4 _HB_LayerVary[HB_TERRAIN_LAYER_LIMIT];
float4 _HB_LayerVaryShape[HB_TERRAIN_LAYER_LIMIT];
float4 _HB_LayerVaryExtra[HB_TERRAIN_LAYER_LIMIT];
float4 _HB_LayerMaskScale[HB_TERRAIN_LAYER_LIMIT];
float4 _HB_LayerMaskOffset[HB_TERRAIN_LAYER_LIMIT];

static const half3 s_TerrainLayerPalette[8] =
{
    half3(0.90h, 0.30h, 0.25h), half3(0.30h, 0.70h, 0.30h), half3(0.25h, 0.45h, 0.90h), half3(0.95h, 0.80h, 0.20h),
    half3(0.70h, 0.35h, 0.85h), half3(0.20h, 0.80h, 0.80h), half3(0.95h, 0.55h, 0.15h), half3(0.85h, 0.85h, 0.85h)
};

struct HiddenBullTerrainSurface
{
    half3 albedo;
    half3 normalTS;
    half3 bend;
    half smoothness;
    half metallic;
    half occlusion;
    half2 limits;
    half3 debug;
};

struct HiddenBullTerrainHex
{
    float2 offset0;
    float2 offset1;
    float2 offset2;
    float3 weight;
};

half4 HB_TerrainControlSlice(float2 uv, float slice)
{
    float2 position = uv * _Control_TexelSize.zw - 0.5;
    float2 cell = floor(position);

    half4 weightX = HB_BSplineWeights(half(position.x - cell.x));
    half4 weightY = HB_BSplineWeights(half(position.y - cell.y));

    float2 low = float2(weightX.x + weightX.y, weightY.x + weightY.y);
    float2 high = 1.0 - low;
    float2 lowUV = (cell - 0.5 + float2(weightX.y, weightY.y) / low) * _Control_TexelSize.xy;
    float2 highUV = (cell + 1.5 + float2(weightX.w, weightY.w) / high) * _Control_TexelSize.xy;

    half4 bottom = lerp(SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_TerrainControl, sampler_HB_TerrainControl,
                                                   float2(highUV.x, lowUV.y), slice, 0),
                        SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_TerrainControl, sampler_HB_TerrainControl, lowUV, slice, 0),
                        half(low.x));
    half4 top = lerp(SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_TerrainControl, sampler_HB_TerrainControl, highUV, slice, 0),
                     SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_TerrainControl, sampler_HB_TerrainControl,
                                                float2(lowUV.x, highUV.y), slice, 0),
                     half(low.x));

    return lerp(top, bottom, half(low.y));
}

void HB_TerrainPick(half weight, int layer, inout half4 best, inout int4 ids)
{
    if (weight <= best.w)
        return;

    if (weight > best.x)
    {
        best = half4(weight, best.xyz);
        ids = int4(layer, ids.xyz);
    }
    else if (weight > best.y)
    {
        best = half4(best.x, weight, best.yz);
        ids = int4(ids.x, layer, ids.yz);
    }
    else if (weight > best.z)
    {
        best = half4(best.xy, weight, best.z);
        ids = int4(ids.xy, layer, ids.z);
    }
    else
    {
        best.w = weight;
        ids.w = layer;
    }
}

void HB_TerrainPicks(float2 uv, out half4 best, out int4 ids)
{
    best = half4(0.0h, 0.0h, 0.0h, 0.0h);
    ids = int4(0, 0, 0, 0);

    int layers = int(_HB_TerrainLayerCount);
    int slices = int(_HB_TerrainControlCount);

    UNITY_LOOP
    for (int slice = 0; slice < slices; slice++)
    {
        half4 control = HB_TerrainControlSlice(uv, float(slice));
        int first = slice * 4;

        HB_TerrainPick(control.r, first, best, ids);

        if (first + 1 < layers)
            HB_TerrainPick(control.g, first + 1, best, ids);

        if (first + 2 < layers)
            HB_TerrainPick(control.b, first + 2, best, ids);

        if (first + 3 < layers)
            HB_TerrainPick(control.a, first + 3, best, ids);
    }

    best /= max(dot(best, 1.0h), HB_EPSILON);
}

float2 HB_TerrainHexHash(float2 cell)
{
    return frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), cell)) * 43758.5453);
}

HiddenBullTerrainHex HB_TerrainHex(float2 uv)
{
    float2 skewed = mul(float2x2(1.0, -0.57735027, 0.0, 1.15470054), uv * (HB_TERRAIN_HEX_RATE * 2.0 * sqrt(3.0)));
    float2 base = floor(skewed);
    float3 corner = float3(frac(skewed), 0.0);
    corner.z = 1.0 - corner.x - corner.y;

    float upper = step(0.0, -corner.z);
    float flip = 2.0 * upper - 1.0;
    float3 weight = float3(-corner.z * flip, upper - corner.y * flip, upper - corner.x * flip);

    weight = max(weight, 0.0);
    weight = pow(weight / max(max(weight.x, weight.y), weight.z), HB_TERRAIN_HEX_SHARPEN);
    weight /= max(weight.x + weight.y + weight.z, 1e-5);
    weight *= step(HB_TERRAIN_HEX_SKIP, weight);
    weight /= max(weight.x + weight.y + weight.z, 1e-5);

    HiddenBullTerrainHex hex;
    hex.offset0 = HB_TerrainHexHash(base + float2(upper, upper));
    hex.offset1 = HB_TerrainHexHash(base + float2(upper, 1.0 - upper));
    hex.offset2 = HB_TerrainHexHash(base + float2(1.0 - upper, upper));
    hex.weight = weight;

    return hex;
}

half4 HB_TerrainHexSample(TEXTURE2D_ARRAY_PARAM(map, samplerMap), float2 uv, float slice, float2 uvDx, float2 uvDy,
                          HiddenBullTerrainHex hex)
{
    half4 colour = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (hex.weight.x > 0.0)
        colour += SAMPLE_TEXTURE2D_ARRAY_GRAD(map, samplerMap, uv + hex.offset0, slice, uvDx, uvDy)
                * half(hex.weight.x);

    UNITY_BRANCH
    if (hex.weight.y > 0.0)
        colour += SAMPLE_TEXTURE2D_ARRAY_GRAD(map, samplerMap, uv + hex.offset1, slice, uvDx, uvDy)
                * half(hex.weight.y);

    UNITY_BRANCH
    if (hex.weight.z > 0.0)
        colour += SAMPLE_TEXTURE2D_ARRAY_GRAD(map, samplerMap, uv + hex.offset2, slice, uvDx, uvDy)
                * half(hex.weight.z);

    return colour;
}

half4 HB_TerrainHexAlbedo(float2 uv, float slice, float2 uvDx, float2 uvDy, inout HiddenBullTerrainHex hex)
{
    half4 first = half4(0.0h, 0.0h, 0.0h, 0.0h);
    half4 second = half4(0.0h, 0.0h, 0.0h, 0.0h);
    half4 third = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (hex.weight.x > 0.0)
        first = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerAlbedo, sampler_HB_LayerAlbedo, uv + hex.offset0, slice, uvDx,
                                            uvDy);

    UNITY_BRANCH
    if (hex.weight.y > 0.0)
        second = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerAlbedo, sampler_HB_LayerAlbedo, uv + hex.offset1, slice, uvDx,
                                             uvDy);

    UNITY_BRANCH
    if (hex.weight.z > 0.0)
        third = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_LayerAlbedo, sampler_HB_LayerAlbedo, uv + hex.offset2, slice, uvDx,
                                            uvDy);

    half3 luma = half3(0.299h, 0.587h, 0.114h);
    float3 bright = float3(dot(first.rgb, luma), dot(second.rgb, luma), dot(third.rgb, luma));
    float3 live = step(1e-5, hex.weight);
    float most = max(max(bright.x * live.x, bright.y * live.y), bright.z * live.z);
    float3 weight = hex.weight * exp2((bright - most) * HB_TERRAIN_HEX_CONTRAST);

    hex.weight = weight / max(weight.x + weight.y + weight.z, 1e-5);

    return first * half(hex.weight.x) + second * half(hex.weight.y) + third * half(hex.weight.z);
}

half3 HB_TerrainArrayNormal(half4 packed, half scale)
{
    half2 xy = (packed.rg * 2.0h - 1.0h) * scale;
    return half3(xy, sqrt(saturate(1.0h - dot(xy, xy))));
}

void HB_TerrainProject(float2 uv, float2 uvDx, float2 uvDy, float slice, half scale, half weight,
                       inout half4 albedo, inout half4 mask, out half3 normal)
{
    HiddenBullTerrainHex hex = HB_TerrainHex(uv);

    albedo += HB_TerrainHexAlbedo(uv, slice, uvDx, uvDy, hex) * weight;
    mask += HB_TerrainHexSample(TEXTURE2D_ARRAY_ARGS(_HB_LayerMask, sampler_HB_LayerAlbedo), uv, slice, uvDx, uvDy,
                                hex) * weight;
    normal = HB_TerrainArrayNormal(HB_TerrainHexSample(TEXTURE2D_ARRAY_ARGS(_HB_LayerNormal, sampler_HB_LayerAlbedo),
                                                       uv, slice, uvDx, uvDy, hex), scale);
}

half3 HB_TerrainFaces(half3 normal)
{
    float3 faces = abs(float3(normal)) * float3(1.0, HB_TERRAIN_TOP_BIAS, 1.0);
    faces = pow(faces / max(max(faces.x, faces.y), faces.z), HB_TERRAIN_FACE_SHARPEN);
    faces /= faces.x + faces.y + faces.z;
    faces *= step(HB_TERRAIN_HEX_SKIP, faces);

    return half3(faces / (faces.x + faces.y + faces.z));
}

half4 HB_TerrainArrayStrokes(half4 best, int4 ids, HiddenBullBrushSample brush)
{
#ifdef _HB_BRUSH
    half4 table = half4(brush.coverage, brush.spine.x, brush.spine.z, -brush.coverage);
    half4 offsets = half4(table[ids.x & 3], table[ids.y & 3], table[ids.z & 3], table[ids.w & 3]);
    half4 weighted = best * exp2(offsets * (half(_LayerStrokes) * HB_TERRAIN_STROKE_RANGE));

    return weighted / max(dot(weighted, 1.0h), HB_EPSILON);
#else
    return best;
#endif
}

half4 HB_TerrainArrayEdges(half4 best, int4 ids, half4 heights, half broken)
{
    half4 live = step(HB_EPSILON, best);
    half4 phase = half4(frac(float4(ids) * HB_TERRAIN_GOLDEN)) * 2.0h - 1.0h;
    half4 key = best + (heights - 0.5h) * HB_TERRAIN_EDGE_HEIGHT + phase * (broken * HB_TERRAIN_EDGE_JITTER);
    key = lerp(-1.0h, key, live);

    half top = max(max(key.x, key.y), max(key.z, key.w));
    half depth = max(half(_HeightTransition), HB_TERRAIN_EDGE_DEPTH);
    half4 shaped = max(key - top + depth, 0.0h) * live;

    return shaped / max(dot(shaped, 1.0h), HB_EPSILON);
}

half HB_TerrainMacro(float3 positionWS)
{
    float3 large = float3(positionWS.x, 0.5, positionWS.z) / HB_TERRAIN_MACRO_LARGE;
    float3 small = float3(positionWS.x, 0.5, positionWS.z) / HB_TERRAIN_MACRO_SMALL + 17.0;

    return HB_TerrainNoise(large) * 0.6h + HB_TerrainNoise(small) * 0.4h - 0.5h;
}

HiddenBullTerrainSurface HB_TerrainArraySurface(Varyings IN, half3 geometryNormal, HiddenBullBrushSample brush)
{
#ifdef HB_TERRAIN_FAR_ONLY
    float2 splatUV = (IN.uvMainAndLM.xy * (_HB_TerrainControlTexel.zw - 1.0f) + 0.5f) * _HB_TerrainControlTexel.xy;
#else
    float2 splatUV = (IN.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
#endif

    half4 best;
    int4 ids;
    HB_TerrainPicks(splatUV, best, ids);
    best.w = 0.0h;
    best = HB_TerrainArrayStrokes(best, ids, brush);
    best *= step(HB_TERRAIN_PICK_SKIP, best);
    best /= max(dot(best, 1.0h), HB_EPSILON);

    float3 positionDx = ddx(IN.positionWS);
    float3 positionDy = ddy(IN.positionWS);
    float3 painted = IN.positionWS;

#ifdef _HB_BRUSH
    painted += HB_BrushStrokeOffset(brush.spine, geometryNormal, max(half(_BrushScale), HB_EPSILON));
#endif

    half3 faces = half3(0.0h, 1.0h, 0.0h);

    UNITY_BRANCH
    if (_SteepMapping > 0.5)
        faces = HB_TerrainFaces(geometryNormal);

#ifdef HB_TERRAIN_FAR_ONLY
    half far = 1.0h;
#else
    float reach = max(float(_DetailDistance), 1.0);
    float away = distance(IN.positionWS, GetCameraPositionWS()) / reach;
    half far = half(saturate((away - HB_TERRAIN_FAR_START) / (1.0 - HB_TERRAIN_FAR_START)));
#endif
    half3 detail = faces * (1.0h - far);

    half4 albedo[HB_TERRAIN_PICKS];
    half4 masks[HB_TERRAIN_PICKS];
    half3 normals[HB_TERRAIN_PICKS];
    half3 bends[HB_TERRAIN_PICKS];
    half4 heights = half4(0.5h, 0.5h, 0.5h, 0.5h);

    UNITY_UNROLL
    for (int pick = 0; pick < HB_TERRAIN_PICKS; pick++)
    {
        albedo[pick] = half4(0.0h, 0.0h, 0.0h, 0.0h);
        masks[pick] = half4(0.0h, 1.0h, 0.5h, 0.0h);
        normals[pick] = half3(0.0h, 0.0h, 1.0h);
        bends[pick] = half3(0.0h, 0.0h, 0.0h);

        UNITY_BRANCH
        if (best[pick] > 0.0h)
        {
            int layer = ids[pick];
            float slice = float(layer);
            float4 st = _HB_LayerST[layer];
            half scale = half(_HB_LayerLook[layer].a);
            masks[pick] = half4(0.0h, 0.0h, 0.0h, 0.0h);

            UNITY_LOOP
            for (int face = 0; face < 3; face++)
            {
                half share = face == 0 ? detail.y : face == 1 ? detail.x : detail.z;

                if (share <= 0.0h)
                    continue;

                float2 plane = face == 0 ? painted.xz : face == 1 ? painted.zy : painted.xy;
                float2 planeDx = face == 0 ? positionDx.xz : face == 1 ? positionDx.zy : positionDx.xy;
                float2 planeDy = face == 0 ? positionDy.xz : face == 1 ? positionDy.zy : positionDy.xy;
                float2 offset = face == 0 ? st.zw : float2(0.0, 0.0);
                half3 normal;

                HB_TerrainProject(plane * st.xy + offset, planeDx * st.xy, planeDy * st.xy, slice, scale, share,
                                  albedo[pick], masks[pick], normal);

                if (face == 0)
                    normals[pick] = lerp(half3(0.0h, 0.0h, 1.0h), normal, 1.0h - far);
                else
                    bends[pick] += (face == 1 ? half3(0.0h, normal.y, normal.x) : half3(normal.x, normal.y, 0.0h))
                                 * share;
            }

            UNITY_BRANCH
            if (far > 0.0h)
            {
                albedo[pick] += SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_LayerAlbedo, sampler_HB_LayerAlbedo, float2(0.5, 0.5),
                                                           slice, HB_TERRAIN_FAR_MIP) * far;
                masks[pick] += SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_LayerMask, sampler_HB_LayerAlbedo, float2(0.5, 0.5),
                                                          slice, HB_TERRAIN_FAR_MIP) * far;
            }

            masks[pick] = half4(_HB_LayerMaskOffset[layer]) + masks[pick] * half4(_HB_LayerMaskScale[layer]);
            heights[pick] = masks[pick].b;
        }
    }

    half broken = HB_TerrainEdge(IN.positionWS, brush.coverage, half(_LayerStrokes));
    best = HB_TerrainArrayEdges(best, ids, heights, broken);

    HiddenBullTerrainSurface surface;
    surface.albedo = half3(0.0h, 0.0h, 0.0h);
    surface.bend = half3(0.0h, 0.0h, 0.0h);
    surface.smoothness = 0.0h;
    surface.metallic = 0.0h;
    surface.occlusion = 0.0h;
    surface.limits = half2(0.0h, 0.0h);
    surface.debug = half3(0.0h, 0.0h, 0.0h);

    half4 patches = half4(0.0h, 0.0h, 0.0h, 0.0h);
    half4 fine = half4(0.0h, 0.0h, 0.0h, 0.0h);
    half4 drift = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_LOOP
    for (int noise = 0; noise < HB_TERRAIN_PICKS; noise++)
    {
        int layer = ids[noise];
        half fade = saturate((best[noise] - HB_TERRAIN_VARY_SKIP) / HB_TERRAIN_VARY_FADE);
        half amount = half(_HB_LayerVary[layer].a) * fade;

        if (amount <= 0.0h)
            continue;

        half4 slot = half4(noise == 0, noise == 1, noise == 2, noise == 3);
        half3 value = HB_TerrainVariationNoise(IN.positionWS, float4(float(layer) * 7.13, 3.7, 0.0, 0.0),
                                               _HB_LayerVaryExtra[layer].x, _HB_LayerVaryShape[layer], broken);

        patches += slot * value.x;
        fine += slot * value.y;
        drift += slot * value.z;
    }

    half3 normal = half3(0.0h, 0.0h, 1e-3h);

    UNITY_UNROLL
    for (int mix = 0; mix < HB_TERRAIN_PICKS; mix++)
    {
        half weight = best[mix];
        int layer = ids[mix];

        UNITY_BRANCH
        if (weight > 0.0h)
        {
            half3 colour = albedo[mix].rgb * half3(_HB_LayerLook[layer].rgb);
            half4 vary = half4(_HB_LayerVary[layer]);
            vary.a *= saturate((weight - HB_TERRAIN_VARY_SKIP) / HB_TERRAIN_VARY_FADE);

            if (vary.a > 0.0h)
                colour = HB_TerrainVariationApply(colour, vary, _HB_LayerVaryShape[layer],
                                                  half(_HB_LayerVaryExtra[layer].y),
                                                  half3(patches[mix], fine[mix], drift[mix]));

            surface.albedo += colour * weight;
            surface.bend += bends[mix] * weight;
            surface.smoothness += masks[mix].a * weight;
            surface.metallic += masks[mix].r * weight;
            surface.occlusion += masks[mix].g * weight;
            surface.limits += half2(_HB_LayerWeather[layer].xy) * weight;
            surface.debug += s_TerrainLayerPalette[layer & 7] * weight;
            normal += normals[mix] * weight;
        }
    }

    UNITY_BRANCH
    if (_MacroVariation > 0.0h)
        surface.albedo *= 1.0h + HB_TerrainMacro(IN.positionWS) * half(_MacroVariation) * HB_TERRAIN_MACRO_AMOUNT;

    surface.normalTS = normalize(lerp(half3(0.0h, 0.0h, 1.0h), normalize(normal), detail.y));

    return surface;
}

#endif
