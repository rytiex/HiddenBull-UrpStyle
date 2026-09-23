Shader "Hidden/HiddenBull/URP Style/Brush Distortion"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "BrushDistortion"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleBrush.hlsl"

            float4 _HB_BrushDistortion;

            #define HB_DISTORT_STRENGTH _HB_BrushDistortion.x
            #define HB_DISTORT_SCALE    _HB_BrushDistortion.y
            #define HB_DISTORT_START    _HB_BrushDistortion.z
            #define HB_DISTORT_RATE     _HB_BrushDistortion.w

            #define HB_DISTORT_PIXELS 48.0

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                half strength = half(HB_DISTORT_STRENGTH);

                if (strength <= 0.0h || HB_BRUSH_ATLAS_BOUND < 0.5h)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float rawDepth = SampleSceneDepth(uv);

            #if UNITY_REVERSED_Z
                if (rawDepth <= 0.0)
            #else
                if (rawDepth >= 1.0)
            #endif
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                half reach = saturate((eyeDepth - HB_DISTORT_START) * HB_DISTORT_RATE);

                if (reach <= 0.0h)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                half3 direction = half3(SafeNormalize(positionWS - GetCameraPositionWS()));

                half4 atlas = SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas,
                                                   HB_BrushDirectionUV(direction, HB_DISTORT_SCALE), 0);

                float2 offset = (atlas.rg * 2.0 - 1.0)
                              * (strength * reach * HB_DISTORT_PIXELS)
                              * _ScreenSize.zw;

                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + offset));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
