#ifndef HIDDENBULL_URPSTYLE_LIT_META_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_META_PASS_INCLUDED

#include "LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

half4 HiddenBullFragmentMeta(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    InitializeHiddenBullSurfaceData(input.uv, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular,
                       surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.Emission = surfaceData.emission;

    return UniversalFragmentMeta(input, metaInput);
}

#endif
