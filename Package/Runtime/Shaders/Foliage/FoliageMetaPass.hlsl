#ifndef HB_FOLIAGE_META_PASS_INCLUDED
#define HB_FOLIAGE_META_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

half4 HB_PlantMetaFragment(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    HB_PlantSurface(input.uv, float2(0.0, 0.0), ddx(input.uv), ddy(input.uv), surfaceData);

    MetaInput metaInput;
    metaInput.Albedo = surfaceData.albedo;
    metaInput.Emission = half3(0.0h, 0.0h, 0.0h);

    return UniversalFragmentMeta(input, metaInput);
}

#endif
