#ifndef HB_PARTICLE_INPUT_INCLUDED
#define HB_PARTICLE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticlesInstancing.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;
    half _DiffuseSoftness;
    half _HalfTone;
    half _SoftDistance;
    half _CameraFadeNear;
    half _CameraFadeFar;
    half _BrushScale;
    half _StrokeAlpha;
CBUFFER_END

#endif
