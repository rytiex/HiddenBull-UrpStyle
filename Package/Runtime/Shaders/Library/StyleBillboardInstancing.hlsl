#ifndef HIDDENBULL_URPSTYLE_BILLBOARD_INSTANCING_INCLUDED
#define HIDDENBULL_URPSTYLE_BILLBOARD_INSTANCING_INCLUDED

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)

#ifndef LOD_FADE_CROSSFADE
    #define LOD_FADE_CROSSFADE
#endif

struct HB_BillboardInstance
{
    float4 row0;
    float4 row1;
    float4 row2;
    float4 inverse0;
    float4 inverse1;
    float4 inverse2;
    float4 sphere;
};

struct HB_BillboardProbe
{
    float4 shAr;
    float4 shAg;
    float4 shAb;
    float4 shBr;
    float4 shBg;
    float4 shBb;
    float4 shC;
    float4 occlusion;
};

StructuredBuffer<HB_BillboardInstance> _HB_BillboardInstances;
StructuredBuffer<HB_BillboardProbe> _HB_BillboardProbes;
StructuredBuffer<uint2> _HB_BillboardVisible;

float4x4 _HB_BillboardLocal;
float4x4 _HB_BillboardLocalInverse;
float4 _HB_BillboardBoundsCenter;
float4 _HB_BillboardBoundsExtent;

void HB_BillboardInstancingSetup()
{
    uint2 visible = _HB_BillboardVisible[unity_InstanceID];
    HB_BillboardInstance instance = _HB_BillboardInstances[visible.x];

    float4x4 world = float4x4(instance.row0, instance.row1, instance.row2, float4(0.0, 0.0, 0.0, 1.0));
    float4x4 inverse = float4x4(instance.inverse0, instance.inverse1, instance.inverse2,
                                float4(0.0, 0.0, 0.0, 1.0));

    float4x4 objectToWorld = mul(world, _HB_BillboardLocal);

    unity_ObjectToWorld = objectToWorld;
    unity_WorldToObject = mul(_HB_BillboardLocalInverse, inverse);
    unity_MatrixPreviousM = unity_ObjectToWorld;
    unity_MatrixPreviousMI = unity_WorldToObject;

    float3 centre = mul(objectToWorld, float4(_HB_BillboardBoundsCenter.xyz, 1.0)).xyz;
    float3 extent = mul(abs((float3x3)objectToWorld), _HB_BillboardBoundsExtent.xyz);

    unity_RendererBounds_Min = float4(centre - extent, 0.0);
    unity_RendererBounds_Max = float4(centre + extent, 0.0);

    float fade = asfloat(visible.y);
    unity_LODFade = float4(fade, fade, 0.0, 0.0);

#if !defined(PROBE_VOLUMES_L1) && !defined(PROBE_VOLUMES_L2)
    HB_BillboardProbe probe = _HB_BillboardProbes[visible.x];

    unity_SHAr = probe.shAr;
    unity_SHAg = probe.shAg;
    unity_SHAb = probe.shAb;
    unity_SHBr = probe.shBr;
    unity_SHBg = probe.shBg;
    unity_SHBb = probe.shBb;
    unity_SHC = probe.shC;
    unity_ProbesOcclusion = probe.occlusion;
#endif
}

#endif

#endif
