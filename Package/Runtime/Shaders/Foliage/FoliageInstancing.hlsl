#ifndef HB_FOLIAGE_INSTANCING_INCLUDED
#define HB_FOLIAGE_INSTANCING_INCLUDED

static float hb_PlantLift = 0.0;

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)

#ifndef LOD_FADE_CROSSFADE
    #define LOD_FADE_CROSSFADE
#endif

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliagePlantData.hlsl"

struct HBPlantProbe
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

StructuredBuffer<HBPlantInstance> _HB_PlantInstances;
StructuredBuffer<HBPlantProbe> _HB_PlantProbes;
StructuredBuffer<HBPlantVisible> _HB_PlantVisible;

float4x4 _HB_PlantLocal;
float4x4 _HB_PlantLocalInverse;
float4 _HB_PlantBoundsCenter;
float4 _HB_PlantBoundsExtent;

static float hb_PlantStreak = 0.0;

#ifdef SHADER_STAGE_FRAGMENT
    #define HB_PLANT_EXACT
#else
    #define HB_PLANT_EXACT precise
#endif

void HB_PlantInstancingSetup()
{
    HBPlantVisible visible = _HB_PlantVisible[unity_InstanceID];
    HBPlantInstance instance = _HB_PlantInstances[visible.index];

    HB_PLANT_EXACT float4x4 objectToWorld = mul(HB_PlantWorld(instance), _HB_PlantLocal);

    unity_ObjectToWorld = objectToWorld;

#ifndef SHADER_STAGE_FRAGMENT
    float4x4 inverse = float4x4(instance.inverse0, instance.inverse1, instance.inverse2,
                                float4(0.0, 0.0, 0.0, 1.0));

    unity_WorldToObject = mul(_HB_PlantLocalInverse, inverse);
#endif

    float3 centre = mul(objectToWorld, float4(_HB_PlantBoundsCenter.xyz, 1.0)).xyz;
    float3 extent = mul(abs((float3x3)objectToWorld), _HB_PlantBoundsExtent.xyz);

    unity_RendererBounds_Min = float4(centre - extent, 0.0);
    unity_RendererBounds_Max = float4(centre + extent, 0.0);

    unity_LODFade = float4(visible.fade, visible.fade, 0.0, 0.0);
    hb_PlantStreak = visible.gust;
    hb_PlantLift = visible.lift;

#if !defined(PROBE_VOLUMES_L1) && !defined(PROBE_VOLUMES_L2)
    HBPlantProbe probe = _HB_PlantProbes[visible.index];

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
