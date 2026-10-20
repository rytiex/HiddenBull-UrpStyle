Shader "Hidden/HB URP Style/Ground Height"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "GroundHeight"

            Cull Off
            ZWrite Off
            ZTest Always
            BlendOp Max
            Blend One One

            HLSLPROGRAM
            #pragma vertex GroundVertex
            #pragma fragment GroundFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _HB_GroundFrame;

            struct GroundVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            GroundVaryings GroundVertex(float4 positionOS : POSITION)
            {
                GroundVaryings output;

                output.positionWS = TransformObjectToWorld(positionOS.xyz);
                output.positionCS = float4((output.positionWS.xz - _HB_GroundFrame.xy) * _HB_GroundFrame.zw, 0.5, 1.0);

                return output;
            }

            float4 GroundFragment(GroundVaryings input) : SV_Target
            {
                return float4(input.positionWS.y, input.positionWS.x, input.positionWS.z, 1.0);
            }
            ENDHLSL
        }
    }
}
