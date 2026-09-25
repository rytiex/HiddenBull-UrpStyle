Shader "Hidden/HiddenBull/Style Fog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleFog.hlsl"

        float HB_SceneDepth(float2 uv)
        {
            return SampleSceneDepth(uv);
        }

        bool HB_ScenePosition(float2 uv, out float3 positionWS)
        {
            float depth = HB_SceneDepth(uv);

            positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

        #if UNITY_REVERSED_Z
            return depth > 0.0;
        #else
            return depth < 1.0;
        #endif
        }
        ENDHLSL

        Pass
        {
            Name "Style Fog Reach"

            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragReach
            #pragma target 3.5

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            half FragReach(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                float3 positionWS;
                if (!HB_ScenePosition(uv, positionWS))
                    return 1.0h;

                float3 cameraPositionWS = GetCameraPositionWS();

                float3 ray = positionWS - cameraPositionWS;
                float rayLength = length(ray);

                if (rayLength < 1e-4)
                    return 1.0h;

                return HB_FogAirReach(1.0h, cameraPositionWS, half3(ray / rayLength), rayLength, uv);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Style Fog Composite"

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            #pragma target 3.5

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            TEXTURE2D(_HB_FogReach);
            float4 _HB_FogReachSize;

            void HB_AccumulateReach(float2 uv, float eye, inout float sum, inout float total)
            {
                float tap = LinearEyeDepth(HB_SceneDepth(uv), _ZBufferParams);
                float weight = rcp(0.01 + abs(tap - eye));

                sum += SAMPLE_TEXTURE2D(_HB_FogReach, sampler_LinearClamp, uv).r * weight;
                total += weight;
            }

            half HB_GatherReach(float2 uv, float eye)
            {
                float2 offset = _HB_FogReachSize.xy;

                float sum = 0.0;
                float total = 0.0;

                HB_AccumulateReach(uv + float2(-offset.x, -offset.y), eye, sum, total);
                HB_AccumulateReach(uv + float2( offset.x, -offset.y), eye, sum, total);
                HB_AccumulateReach(uv + float2(-offset.x,  offset.y), eye, sum, total);
                HB_AccumulateReach(uv + float2( offset.x,  offset.y), eye, sum, total);

                return half(sum / total);
            }

            half FragShadow(float3 positionWS)
            {
            #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                float4 coord = TransformWorldToShadowCoord(positionWS);

                return BEYOND_SHADOW_FAR(coord) ? 1.0h : half(MainLightRealtimeShadow(coord));
            #else
                return 1.0h;
            #endif
            }

            float _HB_FogDebug;

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                int mode = (int)_HB_FogDebug;

                float3 positionWS;
                if (!HB_ScenePosition(uv, positionWS))
                    return mode > 0 ? half4(0.0h, 0.0h, 0.0h, 1.0h) : half4(0.0h, 0.0h, 0.0h, 0.0h);

                float3 cameraPositionWS = GetCameraPositionWS();

                half amount = HB_FogAmount(positionWS, cameraPositionWS);

                float3 ray = positionWS - cameraPositionWS;
                half3 direction = half3(ray / max(length(ray), 1e-4));

                half sunlit = FragShadow(positionWS);
                half raw = SAMPLE_TEXTURE2D(_HB_FogReach, sampler_LinearClamp, uv).r;
                half reach = HB_GatherReach(uv, LinearEyeDepth(HB_SceneDepth(uv), _ZBufferParams));

                if (mode > 0)
                {
                    half value = mode == 1 ? reach
                               : mode == 2 ? raw
                               : mode == 3 ? amount
                               : mode == 4 ? sunlit
                                           : 0.0h;

                    if (mode == 5)
                    {
                        half towardSun = half(saturate(dot(direction, _HB_SunDirection.xyz)));

                        value = PositivePow(towardSun, HB_FOG_SCATTER_POWER)
                              * half(_HB_FogScatter.x) * half(_HB_FogScatter.w) * sunlit * reach;
                    }

                    return half4(value, value, value, 1.0h);
                }

                if (amount <= 0.0h)
                    return half4(0.0h, 0.0h, 0.0h, 0.0h);

                return half4(HB_FogColour(direction, sunlit, reach), amount);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
