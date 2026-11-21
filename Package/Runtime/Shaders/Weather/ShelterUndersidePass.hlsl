#ifndef HB_SHELTER_UNDERSIDE_PASS_INCLUDED
#define HB_SHELTER_UNDERSIDE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define HB_SHELTER_STATIC   1.0
#define HB_FACING_DOWN      -0.1h

float _ReceiveWeather;
float _ShelterCaster;

bool HB_ShelterCaster()
{
    return abs(_ReceiveWeather - HB_SHELTER_STATIC) < 0.5 || _ShelterCaster > 0.5;
}

#ifdef HB_SHELTER_TOP

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

float4 _BaseMap_ST;
half4 _BaseColor;
half _Cutoff;
float _AlphaClipEnabled;

struct HBShelterTopAttributes
{
    float4 positionOS : POSITION;
    float2 texcoord   : TEXCOORD0;
};

struct HBShelterTopVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
};

HBShelterTopVaryings HB_ShelterTopVertex(HBShelterTopAttributes input)
{
    HBShelterTopVaryings output;
    output.positionCS = HB_ShelterCaster() ? TransformObjectToHClip(input.positionOS.xyz) : float4(0.0, 0.0, 0.0, 0.0);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    return output;
}

half4 HB_ShelterTopFragment(HBShelterTopVaryings input) : SV_Target
{
    UNITY_BRANCH
    if (_AlphaClipEnabled > 0.5)
        clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a - _Cutoff);

    return 0.0h;
}

#endif

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

    if (!HB_ShelterCaster())
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
