Shader "HB URP Style/Particle"
{
    Properties
    {
        [Enum(Alpha, 0, Premultiply, 1, Additive, 2)] _BlendMode("Blend Mode", Float) = 0.0
        [ToggleUI] _FlipbookBlending("Flipbook Blending", Float) = 0.0

        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor][HDR] _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)

        [ToggleUI] _Lit("Lit", Float) = 1.0
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 1.0
        _HalfTone("Half Tone", Range(0.0, 1.0)) = 0.35

        [ToggleUI] _SoftParticles("Soft Particles", Float) = 1.0
        _SoftDistance("Soft Distance", Range(0.01, 5.0)) = 0.5
        [ToggleUI] _CameraFade("Camera Fade", Float) = 1.0
        _CameraFadeNear("Near", Float) = 0.3
        _CameraFadeFar("Far", Float) = 1.5

        [ToggleUI] _BrushEnabled("Brush", Float) = 1.0
        _BrushScale("Strokes Across", Range(0.5, 8.0)) = 2.0
        _StrokeAlpha("Stroke Alpha", Range(0.0, 1.0)) = 0.5

        [ToggleUI] _EmissionEnabled("Emission", Float) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        [Enum(Front, 2, Back, 1, Both, 0)] _Cull("Render Face", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "OIT Moments"
            Tags
            {
                "LightMode" = "HBStyleOITMoments"
            }

            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Blend One One

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_ParticleVertex
            #pragma fragment HB_ParticleMomentsFragment

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
            Name "OIT Color"
            Tags
            {
                "LightMode" = "HBStyleOITColor"
            }

            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Blend 0 One One
            Blend 1 DstColor Zero
            Blend 2 One One
            Blend 3 One One

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_ParticleVertex
            #pragma fragment HB_ParticleColorFragment

            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _HB_PARTICLE_LIT
            #pragma shader_feature_local_fragment _ _HB_PREMULTIPLY _HB_ADDITIVE
            #pragma shader_feature_local_fragment _HB_SOFT_PARTICLES
            #pragma shader_feature_local_fragment _HB_CAMERA_FADE
            #pragma shader_feature_local_fragment _HB_BRUSH
            #pragma shader_feature_local_fragment _HB_EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Particle/ParticlePass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "HiddenBull.UrpStyle.Editor.StyleParticleShaderGUI"
}
