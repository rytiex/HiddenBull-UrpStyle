#ifndef HB_GRAPH_MAPS_INCLUDED
#define HB_GRAPH_MAPS_INCLUDED

#ifndef HB_GRAPH_MAPS
#define HB_GRAPH_MAPS
#endif

#if !defined(HB_GRAPH_PREVIEW) && (defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN))
#define HB_GRAPH_PREVIEW
#endif

#if defined(HB_GRAPH_BRUSH) && !defined(HB_GRAPH_SIMPLE)
#define HB_GRAPH_PAINT
#endif

#define HB_GRAPH_TRIPLANAR_CUTOFF 0.02

struct HBGraphTriplanar
{
    float3 position;
    float3 positionDx;
    float3 positionDy;
    float3 blend;
    float3 normal;
    float2 offset;
    float scale;
    float localSpace;
};

float3 HB_GraphObjectScale()
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();

    return float3(length(objectToWorld._m00_m10_m20), length(objectToWorld._m01_m11_m21),
                  length(objectToWorld._m02_m12_m22));
}

HBGraphTriplanar HB_GraphSetupTriplanar(float3 positionWS, float3 positionDx, float3 positionDy, float3 normalWS,
                                        float space, float3 anchorOS, float3 anchorDx, float3 anchorDy,
                                        float4 scaleTranslate)
{
    float3 scale = HB_GraphObjectScale();
    float3 objectNormal = SafeNormalize(mul(normalWS, (float3x3)GetObjectToWorldMatrix()));
    float3 position;
    float3 dx;
    float3 dy;
    float3 normal;
    float local;

    if (space > 1.5 && any(anchorOS != 0.0))
    {
        position = anchorOS * scale;
        dx = anchorDx * scale;
        dy = anchorDy * scale;
        normal = objectNormal;
        local = 1.0;
    }
    else
    {
        float3x3 worldToObject = (float3x3)GetWorldToObjectMatrix();

        local = saturate(space);
        position = lerp(positionWS, mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz * scale, local);
        dx = lerp(positionDx, mul(worldToObject, positionDx) * scale, local);
        dy = lerp(positionDy, mul(worldToObject, positionDy) * scale, local);
        normal = lerp(normalWS, objectNormal, local);
    }

    float3 blend = abs(normal);
    blend *= blend;
    blend *= blend;

    HBGraphTriplanar triplanar;
    triplanar.position = position * scaleTranslate.x;
    triplanar.positionDx = dx * scaleTranslate.x;
    triplanar.positionDy = dy * scaleTranslate.x;
    triplanar.blend = blend / max(blend.x + blend.y + blend.z, 1e-5);
    triplanar.normal = normal;
    triplanar.offset = scaleTranslate.zw;
    triplanar.scale = scaleTranslate.x;
    triplanar.localSpace = local;
    return triplanar;
}

float3 HB_GraphTriplanarShift(HBGraphTriplanar triplanar, float3 offsetWS)
{
    float3 local = mul((float3x3)GetWorldToObjectMatrix(), offsetWS) * HB_GraphObjectScale();

    return lerp(offsetWS, local, triplanar.localSpace) * triplanar.scale;
}

float4 HB_GraphSampleTriplanar(UnityTexture2D map, HBGraphTriplanar triplanar, float3 shift)
{
    float3 p = triplanar.position + shift;
    float4 sum = float4(0.0, 0.0, 0.0, 0.0);

    UNITY_BRANCH
    if (triplanar.blend.x > HB_GRAPH_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(map.tex, map.samplerstate, p.zy + triplanar.offset,
                                     triplanar.positionDx.zy, triplanar.positionDy.zy) * triplanar.blend.x;
    }

    UNITY_BRANCH
    if (triplanar.blend.y > HB_GRAPH_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(map.tex, map.samplerstate, p.xz + triplanar.offset,
                                     triplanar.positionDx.xz, triplanar.positionDy.xz) * triplanar.blend.y;
    }

    UNITY_BRANCH
    if (triplanar.blend.z > HB_GRAPH_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(map.tex, map.samplerstate, p.xy + triplanar.offset,
                                     triplanar.positionDx.xy, triplanar.positionDy.xy) * triplanar.blend.z;
    }

    return sum;
}

float3 HB_GraphSampleTriplanarNormal(UnityTexture2D map, float scale, HBGraphTriplanar triplanar, float3 shift)
{
    float3 p = triplanar.position + shift;
    float3 normal = triplanar.normal;
    float3 sum = float3(0.0, 0.0, 0.0);

    UNITY_BRANCH
    if (triplanar.blend.x > HB_GRAPH_TRIPLANAR_CUTOFF)
    {
        float3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(map.tex, map.samplerstate, p.zy + triplanar.offset,
                                           triplanar.positionDx.zy, triplanar.positionDy.zy), scale);
        tangent = float3(tangent.xy + normal.zy, abs(tangent.z) * normal.x);
        sum += tangent.zyx * triplanar.blend.x;
    }

    UNITY_BRANCH
    if (triplanar.blend.y > HB_GRAPH_TRIPLANAR_CUTOFF)
    {
        float3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(map.tex, map.samplerstate, p.xz + triplanar.offset,
                                           triplanar.positionDx.xz, triplanar.positionDy.xz), scale);
        tangent = float3(tangent.xy + normal.xz, abs(tangent.z) * normal.y);
        sum += tangent.xzy * triplanar.blend.y;
    }

    UNITY_BRANCH
    if (triplanar.blend.z > HB_GRAPH_TRIPLANAR_CUTOFF)
    {
        float3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(map.tex, map.samplerstate, p.xy + triplanar.offset,
                                           triplanar.positionDx.xy, triplanar.positionDy.xy), scale);
        tangent = float3(tangent.xy + normal.xy, abs(tangent.z) * normal.z);
        sum += tangent * triplanar.blend.z;
    }

    float3 mapped = SafeNormalize(sum);

    return triplanar.localSpace > 0.5 ? TransformObjectToWorldNormal(mapped) : mapped;
}

void HB_Maps_float(float4 BaseColor, UnityTexture2D BaseMap, UnityTexture2D NormalMap, float NormalScale,
                   UnityTexture2D MaskMap, bool Triplanar, float Space, float4x4 Paint, float3 PositionWS,
                   float3 GeometryNormalWS, bool FrontFace, float3 AnchorOS, float2 UV, out float4x4 Out)
{
    Out = (float4x4)0;
    Out[0] = BaseColor;
    Out[1] = float4(0.0, 0.0, 1.0, 0.0);
    Out[2] = float4(0.0, 1.0, 0.0, 1.0);

    float2 uv = UV * BaseMap.scaleTranslate.xy + BaseMap.scaleTranslate.zw;
    float2 uvDx = ddx(uv);
    float2 uvDy = ddy(uv);
    float3 positionDx = ddx(PositionWS);
    float3 positionDy = ddy(PositionWS);
    float3 anchorDx = ddx(AnchorOS);
    float3 anchorDy = ddy(AnchorOS);

#ifdef HB_GRAPH_PAINT
    bool painting = Paint[0].w > 0.5;
#endif

    UNITY_BRANCH
    if (Triplanar)
    {
        float3 facingWS = normalize(FrontFace ? GeometryNormalWS : -GeometryNormalWS);
        HBGraphTriplanar triplanar = HB_GraphSetupTriplanar(PositionWS, positionDx, positionDy, facingWS, round(Space),
                                                            AnchorOS, anchorDx, anchorDy, BaseMap.scaleTranslate);
        float3 shift = float3(0.0, 0.0, 0.0);

#ifdef HB_GRAPH_PAINT
        shift = painting ? HB_GraphTriplanarShift(triplanar, Paint[1].xyz) : float3(0.0, 0.0, 0.0);
#endif

        float3 detail = shift;

        Out[1] = float4(facingWS, 1.0);

#if defined(_HB_BASE_MAP) || defined(HB_GRAPH_PREVIEW)
        float4 painted = HB_GraphSampleTriplanar(BaseMap, triplanar, shift);

    #ifdef HB_GRAPH_PAINT
        UNITY_BRANCH
        if (painting)
        {
            float4 original = HB_GraphSampleTriplanar(BaseMap, triplanar, float3(0.0, 0.0, 0.0));
            half keep = HB_PaintKeep(half4(painted), half4(original), half(Paint[0].z));

            painted = float4(lerp(original.rgb, painted.rgb, keep), original.a);
            detail = shift * keep;
        }
    #endif

        Out[0] *= painted;
#endif

#if defined(_HB_NORMAL_MAP) || defined(HB_GRAPH_PREVIEW)
        Out[1].xyz = HB_GraphSampleTriplanarNormal(NormalMap, NormalScale, triplanar, detail);
#endif

#if defined(_HB_MASK_MAP) || defined(HB_GRAPH_PREVIEW)
        Out[2] = HB_GraphSampleTriplanar(MaskMap, triplanar, detail);
#endif
    }
    else
    {
        float2 detail = uv;

#ifdef HB_GRAPH_PAINT
        float2 shift = painting ? HB_BrushUvShift(Paint[0].xy, uvDx, uvDy, HB_BRUSH_SEAM_FOOTPRINT)
                                : float2(0.0, 0.0);

        detail = uv + shift;
#endif

#if defined(_HB_BASE_MAP) || defined(HB_GRAPH_PREVIEW)
        half4 painted = SAMPLE_TEXTURE2D_GRAD(BaseMap.tex, BaseMap.samplerstate, detail, uvDx, uvDy);

    #ifdef HB_GRAPH_PAINT
        UNITY_BRANCH
        if (painting)
        {
            half4 original = SAMPLE_TEXTURE2D_GRAD(BaseMap.tex, BaseMap.samplerstate, uv, uvDx, uvDy);
            half keep = HB_PaintKeep(painted, original, half(Paint[0].z));

            painted = half4(lerp(original.rgb, painted.rgb, keep), original.a);
            detail = uv + shift * keep;
        }
    #endif

        Out[0] *= painted;
#endif

#if defined(_HB_NORMAL_MAP) || defined(HB_GRAPH_PREVIEW)
        half4 normal = SAMPLE_TEXTURE2D_GRAD(NormalMap.tex, NormalMap.samplerstate, detail, uvDx, uvDy);

        Out[1].xyz = UnpackNormalScale(normal, NormalScale);
#endif

#if defined(_HB_MASK_MAP) || defined(HB_GRAPH_PREVIEW)
        Out[2] = SAMPLE_TEXTURE2D_GRAD(MaskMap.tex, MaskMap.samplerstate, detail, uvDx, uvDy);
#endif
    }
}

#endif
