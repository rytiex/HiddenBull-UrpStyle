Shader "Hidden/HiddenBull/Style Transparency"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Style Transparency Composite"

            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleTransparency.hlsl"

            TEXTURE2D(_HB_OITFront);
            TEXTURE2D(_HB_OITBehind);
            TEXTURE2D(_HB_OITOffset);
            TEXTURE2D(_HB_OITTint);
            TEXTURE2D(_HB_OITBehindTint);
            TEXTURE2D(_HB_OITBackground);
            TEXTURE2D_FLOAT(_HB_OITSceneDepth);

            float4 _HB_OITSize;

            #define HB_REFRACTION_BLUR       0.03
            #define HB_REFRACTION_DISPERSION 0.35
            #define HB_REFRACTION_TAPS       6

            float3 HB_Resolve(float4 accumulated, float absorbance)
            {
                if (absorbance < HB_OIT_EMPTY || accumulated.a < HB_OIT_EMPTY)
                    return accumulated.rgb;

                return accumulated.rgb * ((1.0 - exp(-absorbance)) / accumulated.a);
            }

            float3 HB_BehindImage(float2 uv)
            {
                uv = saturate(uv);

                uint2 pixel = min(uint2(uv * _HB_OITSize.xy), uint2(_HB_OITSize.xy) - 1);

                float absorbance = LOAD_TEXTURE2D(_HB_OITB0, pixel).g;
                float2 bend = LOAD_TEXTURE2D(_HB_OITOffset, pixel).ba;
                float3 tint = LOAD_TEXTURE2D(_HB_OITBehindTint, pixel).rgb;

                float3 background = SAMPLE_TEXTURE2D_LOD(_HB_OITBackground, sampler_LinearClamp,
                                                         saturate(uv + bend), 0).rgb;

                return HB_Resolve(LOAD_TEXTURE2D(_HB_OITBehind, pixel), absorbance)
                     + background * exp(-absorbance) * tint;
            }

            float3 HB_BehindChroma(float2 uv, float2 chroma)
            {
                return float3(HB_BehindImage(uv + chroma).r, HB_BehindImage(uv).g, HB_BehindImage(uv - chroma).b);
            }

            bool HB_InFrontOfSurface(float2 uv, float stored)
            {
                uint2 pixel = min(uint2(saturate(uv) * _HB_OITSize.xy), uint2(_HB_OITSize.xy) - 1);

                float scene = LinearEyeDepth(LOAD_TEXTURE2D(_HB_OITSceneDepth, pixel).r, _ZBufferParams);
                float surface = exp((1.0 - stored) / HB_OIT_RCP_LOG_RANGE + HB_OIT_LOG_NEAR);

                return scene < surface;
            }

            float2 HB_GuardedTap(float2 uv, float2 tap, float guard)
            {
                return guard > 0.0 && HB_InFrontOfSurface(tap, guard) ? uv : tap;
            }

            float3 HB_Refracted(float2 uv, float2 offset, float blur, float dispersion, uint2 pixel, float guard)
            {
                float2 chroma = offset * (dispersion * HB_REFRACTION_DISPERSION);
                bool split = dispersion > 0.0;

                if (blur <= 0.0)
                {
                    if (split)
                        return HB_BehindChroma(uv, chroma);

                    return HB_BehindImage(uv);
                }

                float2 radius = blur * HB_REFRACTION_BLUR * float2(_HB_OITSize.y * _HB_OITSize.z, 1.0);
                float rotation = InterleavedGradientNoise(float2(pixel), 0) * TWO_PI;

                float3 sum = float3(0.0, 0.0, 0.0);

                UNITY_UNROLL
                for (int i = 0; i < HB_REFRACTION_TAPS; i++)
                {
                    float angle = rotation + i * (TWO_PI / HB_REFRACTION_TAPS);
                    float2 disc = float2(cos(angle), sin(angle)) * radius * sqrt((i + 0.5) / HB_REFRACTION_TAPS);

                    if (split)
                    {
                        int channel = i % 3;
                        float3 mask = float3(channel == 0, channel == 1, channel == 2);

                        sum += HB_BehindImage(HB_GuardedTap(uv, uv + disc + chroma * (1 - channel), guard)) * mask;
                    }
                    else
                    {
                        sum += HB_BehindImage(HB_GuardedTap(uv, uv + disc, guard));
                    }
                }

                return sum * (split ? 3.0 / HB_REFRACTION_TAPS : 1.0 / HB_REFRACTION_TAPS);
            }

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                uint2 pixel = uint2(input.positionCS.xy);
                float4 background = LOAD_TEXTURE2D(_HB_OITBackground, pixel);
                float4 info = LOAD_TEXTURE2D(_HB_OITInfo, pixel);

                float absorbance = LOAD_TEXTURE2D(_HB_OITB0, pixel).r;
                float3 front = HB_Resolve(LOAD_TEXTURE2D(_HB_OITFront, pixel), absorbance);
                float clear = exp(-absorbance);

                UNITY_BRANCH
                if (info.r <= 0.0)
                    return half4(front + background.rgb * clear, background.a);

                float2 offset = LOAD_TEXTURE2D(_HB_OITOffset, pixel).rg;

                if (info.a > 0.5 && HB_InFrontOfSurface(input.texcoord + offset, info.r))
                    offset = float2(0.0, 0.0);

                float3 behind = HB_Refracted(input.texcoord + offset, offset, info.g, info.b, pixel,
                                             info.a > 0.5 ? info.r : 0.0)
                              * LOAD_TEXTURE2D(_HB_OITTint, pixel).rgb;

                return half4(front + clear * behind, background.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
