#ifndef HB_SHELTER_UNDERSIDE_PASS_INCLUDED
#define HB_SHELTER_UNDERSIDE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define HB_SHELTER_MOVING   1.5
#define HB_FACING_DOWN      -0.1h

float _ReceiveWeather;

struct HBUndersideAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
};

struct HBUndersideVaryings
{
    float4 positionCS : SV_POSITION;
    half facing       : TEXCOORD0;
};

HBUndersideVaryings HB_UndersideVertex(HBUndersideAttributes input)
{
    HBUndersideVaryings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.facing = half(TransformObjectToWorldNormal(input.normalOS).y);

    if (_ReceiveWeather > HB_SHELTER_MOVING)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);

    return output;
}

#ifdef HB_UNDERSIDE_PLANT

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliagePlantData.hlsl"

StructuredBuffer<HBPlantInstance> _HB_PlantInstances;
StructuredBuffer<HBPlantVisible> _HB_PlantVisible;

float4x4 _HB_PlantLocal;

HBUndersideVaryings HB_UndersidePlantVertex(HBUndersideAttributes input, uint instance : SV_InstanceID)
{
    float4x4 world = HB_PlantWorld(_HB_PlantInstances[_HB_PlantVisible[instance].index]);
    float4x4 objectToWorld = mul(world, _HB_PlantLocal);

    HBUndersideVaryings output;
    output.positionCS = TransformWorldToHClip(mul(objectToWorld, float4(input.positionOS.xyz, 1.0)).xyz);
    output.facing = half(normalize(mul((float3x3)objectToWorld, input.normalOS)).y);

    return output;
}

#endif

half4 HB_UndersideFragment(HBUndersideVaryings input) : SV_Target
{
    clip(HB_FACING_DOWN - input.facing);

    return 0.0h;
}

#endif
