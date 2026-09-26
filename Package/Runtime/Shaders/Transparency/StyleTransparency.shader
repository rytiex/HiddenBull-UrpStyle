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

            Blend One SrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleTransparency.hlsl"

            TEXTURE2D(_HB_OITAccum);

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                uint2 pixel = uint2(input.positionCS.xy);

                float b0 = LOAD_TEXTURE2D(_HB_OITB0, pixel).r;

                if (b0 < HB_OIT_EMPTY)
                {
                    float4 additive = LOAD_TEXTURE2D(_HB_OITAccum, pixel);

                    return half4(additive.rgb, 1.0h);
                }

                float4 accum = LOAD_TEXTURE2D(_HB_OITAccum, pixel);
                float transmittance = exp(-b0);

                float scale = (1.0 - transmittance) / max(accum.a, HB_OIT_EMPTY);

                return half4(accum.rgb * scale, transmittance);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
