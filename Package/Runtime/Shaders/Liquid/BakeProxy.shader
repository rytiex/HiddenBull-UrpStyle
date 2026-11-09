Shader "Hidden/HB URP Style/Bake Proxy"
{
    Properties
    {
        _BaseColor("Albedo", Color) = (0.0, 0.0, 0.0, 1.0)
        [HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex UniversalVertexMeta
            #pragma fragment HB_BakeProxyFragment

            #pragma shader_feature EDITOR_VISUALIZATION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

            half4 HB_BakeProxyFragment(Varyings input) : SV_Target
            {
                MetaInput metaInput = (MetaInput)0;

                metaInput.Albedo = _BaseColor.rgb;
                metaInput.Emission = _EmissionColor.rgb;

                return UniversalFragmentMeta(input, metaInput);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
