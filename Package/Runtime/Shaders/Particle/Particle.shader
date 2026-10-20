Shader "HB URP Style/Particle"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor][HDR] _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Enum(Alpha, 0, Premultiply, 1, Additive, 2)] _Blend("Blend Mode", Float) = 0.0

        [ToggleUI] _Lit("Lit", Float) = 1.0
        _DiffuseWrap("Diffuse Wrap", Range(0.0, 1.0)) = 0.5
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 1.0
        _HalfTone("Half Tone", Range(0.0, 1.0)) = 0.35

        [ToggleUI] _SoftParticles("Soft Particles", Float) = 1.0
        _SoftDistance("Soft Distance", Range(0.01, 5.0)) = 0.5
        [ToggleUI] _CameraFade("Camera Fade", Float) = 1.0
        _CameraFadeNear("Near", Float) = 0.3
        _CameraFadeFar("Far", Float) = 1.5
        [ToggleUI] _FlipbookBlending("Flipbook Blending", Float) = 0.0

        [ToggleUI] _BrushEnabled("Brush", Float) = 1.0
        _BrushScale("Strokes Across", Range(0.5, 8.0)) = 2.0
        _StrokeAlpha("Stroke Alpha", Range(0.0, 1.0)) = 0.5

        [ToggleUI] _EmissionEnabled("Emission", Float) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        [Enum(Front, 2, Back, 1, Both, 0)] _Cull("Render Face", Float) = 0.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "PerformanceChecks" = "False"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "OITMoments"
            Tags
            {
                "LightMode" = "HiddenBullOITMoments"
            }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullParticleVertex
            #pragma fragment HiddenBullParticleMomentsFragment

            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _HB_PREMULTIPLY
            #pragma shader_feature_local_fragment _HB_SOFT_PARTICLES
            #pragma shader_feature_local_fragment _HB_CAMERA_FADE
            #pragma shader_feature_local_fragment _HB_BRUSH

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Particle/ParticlePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "OITColor"
            Tags
            {
                "LightMode" = "HiddenBullOITColor"
            }

            Blend 0 One One
            Blend 1 One One
            Blend 2 One One
            Blend 3 DstColor Zero
            Blend 4 DstColor Zero
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullParticleVertex
            #pragma fragment HiddenBullParticleColorFragment

            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _HB_PREMULTIPLY
            #pragma shader_feature_local_fragment _HB_ADDITIVE
            #pragma shader_feature_local_fragment _HB_SOFT_PARTICLES
            #pragma shader_feature_local_fragment _HB_CAMERA_FADE
            #pragma shader_feature_local_fragment _HB_BRUSH
            #pragma shader_feature_local_fragment _HB_EMISSION
            #pragma shader_feature_local_fragment _HB_PARTICLE_LIT

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #define _SURFACE_TYPE_TRANSPARENT
            #define HB_PARTICLE_COLOR
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Particle/ParticlePass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "HiddenBull.UrpStyle.Editor.ParticleShaderGUI"
    Fallback Off
}
