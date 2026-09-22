#ifndef HIDDENBULL_URPSTYLE_LIT_META_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_META_PASS_INCLUDED

#include "LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

// Required by Architecture D16: indirect light may be baked, so the shader has to tell the
// lightmapper what its surfaces actually look like.
//
// Vertex colour is not applied here — the meta pass has no vertex colour stream, so a material
// relying on _HB_VERTEX_COLOR bounces light as if it were untinted. Worth knowing before tinting
// large static surfaces that contribute a lot of bounce.
half4 HiddenBullFragmentMeta(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    InitializeHiddenBullSurfaceData(input.uv, half4(1.0h, 1.0h, 1.0h, 1.0h), surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular,
                       surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.Emission = surfaceData.emission;

    return UniversalFragmentMeta(input, metaInput);
}

#endif // HIDDENBULL_URPSTYLE_LIT_META_PASS_INCLUDED
