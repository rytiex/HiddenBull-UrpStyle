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
        ENDHLSL

        Pass
        {
            Name "Style Fog Tile Depth"

            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragTileDepth
            #pragma target 3.5

            float4 _HB_FogVolumeSize;

            float FragTileDepth(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 size = _HB_FogVolumeSize.xy;
                float2 origin = floor(input.positionCS.xy);

                float farthest = 0.0;

                UNITY_UNROLL
                for (int y = 0; y < 4; y++)
                {
                    UNITY_UNROLL
                    for (int x = 0; x < 4; x++)
                    {
                        float2 uv = (origin + (float2(x, y) + 0.5) * 0.25) / size;
                        float depth = HB_SceneDepth(uv);

                        farthest = max(farthest, HB_IsSurface(depth) ? LinearEyeDepth(depth, _ZBufferParams) : 1e6);
                    }
                }

                return farthest;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Style Fog Inject"

            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragInject
            #pragma target 3.5

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #define HB_FOG_TILES   8
            #define HB_FOG_HISTORY 0.9

            TEXTURE2D(_HB_FogVolumeHistory);
            TEXTURE2D_FLOAT(_HB_SkyOcclusion);
            TEXTURE2D_FLOAT(_HB_FogTileDepth);

            float4x4 _HB_SkyOcclusionViewProj;
            float4 _HB_SkyOcclusionParams;

            float4 _HB_FogVolumeSize;
            float4 _HB_FogVolumeFrame;
            float4 _HB_FogVolumePrevious;
            float4x4 _HB_FogVolumePrevViewProj;

            float3 HB_FroxelRay(float2 uv, float3 cameraPositionWS)
            {
                float3 far = ComputeWorldSpacePosition(uv, UNITY_RAW_FAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
                float3 ray = far - cameraPositionWS;

                return ray / dot(ray, -UNITY_MATRIX_V[2].xyz);
            }

            float HB_SliceDepth(float slice, float logNear, float rcpLogRange)
            {
                return exp(logNear + slice / (_HB_FogVolumeSize.z * rcpLogRange));
            }

            float HB_SkyVisibility(float3 positionWS, float noise)
            {
                if (_HB_SkyOcclusionParams.w < 0.5)
                    return 1.0;

                float radius = _HB_SkyOcclusionParams.x;
                float3 lifted = positionWS + float3(0.0, _HB_SkyOcclusionParams.y, 0.0);

                float visible = 0.0;

                UNITY_UNROLL
                for (int i = 0; i < 4; i++)
                {
                    float angle = (noise + i) * 2.39996323;
                    float3 offset = float3(cos(angle), 0.0, sin(angle)) * (radius * sqrt((i + 0.5) * 0.25));

                    float4 clip = mul(_HB_SkyOcclusionViewProj, float4(lifted + offset, 1.0));
                    float2 uv = clip.xy;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = -uv.y;
                #endif
                    uv = uv * 0.5 + 0.5;

                    if (any(uv < 0.0) || any(uv > 1.0))
                    {
                        visible += 1.0;
                        continue;
                    }

                    float stored = SAMPLE_TEXTURE2D_LOD(_HB_SkyOcclusion, sampler_PointClamp, uv, 0).r;
                    float depth = UNITY_NEAR_CLIP_VALUE < 0.0 ? clip.z * 0.5 + 0.5 : clip.z;

                #if UNITY_REVERSED_Z
                    visible += stored > depth ? 0.0 : 1.0;
                #else
                    visible += stored < depth ? 0.0 : 1.0;
                #endif
                }

                return visible * 0.25;
            }

            float3 HB_HistoryAt(float2 uv, float slice)
            {
                float2 size = _HB_FogVolumeSize.xy;
                float2 cell = clamp(uv * size, 0.5, size - 0.5);

                uint first = (uint)slice;
                uint second = min(first + 1u, (uint)_HB_FogVolumeSize.z - 1u);

                float2 atlas = size * HB_FOG_TILES;

                float2 a = (float2(first % HB_FOG_TILES, first / HB_FOG_TILES) * size + cell) / atlas;
                float2 b = (float2(second % HB_FOG_TILES, second / HB_FOG_TILES) * size + cell) / atlas;

                return lerp(SAMPLE_TEXTURE2D_LOD(_HB_FogVolumeHistory, sampler_LinearClamp, a, 0).rgb,
                            SAMPLE_TEXTURE2D_LOD(_HB_FogVolumeHistory, sampler_LinearClamp, b, 0).rgb,
                            frac(slice));
            }

            float4 FragInject(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                uint2 pixel = uint2(input.positionCS.xy);
                uint2 size = uint2(_HB_FogVolumeSize.xy);
                uint2 tile = pixel / size;
                uint slice = tile.x + tile.y * HB_FOG_TILES;

                if (slice >= (uint)_HB_FogVolumeSize.z)
                    return float4(0.0, 1.0, 1.0, 0.0);

                float2 cell = float2(pixel - tile * size);
                float2 centreUV = (cell + 0.5) / float2(size);

                float logNear = _HB_FogVolumeParams.x;
                float rcpLogRange = _HB_FogVolumeParams.y;

                float noise = InterleavedGradientNoise(float2(pixel), (int)_HB_FogVolumeFrame.x);

                float2 offset = frac(noise + _HB_FogVolumeFrame.yz);
                float2 uv = (cell + offset) / float2(size);

                float farthest = LOAD_TEXTURE2D(_HB_FogTileDepth, uint2(cell)).r;
                float depth = min(HB_SliceDepth(slice + noise, logNear, rcpLogRange), farthest);

                float3 cameraPositionWS = GetCameraPositionWS();
                float3 ray = HB_FroxelRay(uv, cameraPositionWS);
                float stretch = length(ray);
                float3 positionWS = cameraPositionWS + ray * depth;

                float extinction = depth * stretch > _HB_FogScatter.z
                    ? HB_FogDensity(positionWS) * stretch
                    : 0.0;

                float sky = HB_SkyVisibility(positionWS, noise);
                float sun = sky;

            #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                sun = BEYOND_SHADOW_FAR(shadowCoord) ? sky : MainLightRealtimeShadow(shadowCoord);
            #endif

                float3 current = float3(extinction, sun, sky);

                UNITY_BRANCH
                if (_HB_FogVolumePrevious.w > 0.5)
                {
                    float3 centreWS = cameraPositionWS + HB_FroxelRay(centreUV, cameraPositionWS)
                                    * HB_SliceDepth(slice + 0.5, logNear, rcpLogRange);
                    float4 previous = mul(_HB_FogVolumePrevViewProj, float4(centreWS, 1.0));

                    if (previous.w > 1e-4)
                    {
                        float2 previousUV = previous.xy / previous.w;
                    #if UNITY_UV_STARTS_AT_TOP
                        previousUV.y = -previousUV.y;
                    #endif
                        previousUV = previousUV * 0.5 + 0.5;

                        float previousSlice = (log(previous.w) - _HB_FogVolumePrevious.x)
                                            * _HB_FogVolumePrevious.y * _HB_FogVolumeSize.z - 0.5;

                        if (all(previousUV >= 0.0) && all(previousUV <= 1.0) &&
                            previousSlice >= 0.0 && previousSlice <= _HB_FogVolumeSize.z - 1.0)
                        {
                            current = lerp(current, HB_HistoryAt(previousUV, previousSlice), HB_FOG_HISTORY);
                        }
                    }
                }

                return float4(current, 0.0);
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

            float _HB_FogDebug;

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                int mode = (int)_HB_FogDebug;

                float depth = HB_SceneDepth(uv);

                float3 positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                if (!HB_IsSurface(depth))
                {
                    if (mode > 0)
                        return half4(0.0h, 0.0h, 0.0h, 1.0h);

                    HB_FogResult skyFog = HB_ResolveSkyFog(uv, half3(normalize(positionWS - GetCameraPositionWS())));

                    return half4(skyFog.colour, skyFog.amount);
                }

                HB_FogResult fog = HB_ResolveFog(positionWS, uv, true);
                half3 direction = fog.direction;

                if (mode > 0)
                {
                    half value = mode == 1 ? fog.amount
                               : mode == 2 ? fog.sun
                               : mode == 4 ? fog.sky
                                           : 0.0h;

                    if (mode == 3)
                    {
                        half towardSun = half(saturate(dot(direction, _HB_SunDirection.xyz)));

                        value = PositivePow(towardSun, HB_FOG_SCATTER_POWER)
                              * half(_HB_FogScatter.x) * half(_HB_FogScatter.w) * fog.sun;
                    }

                    return half4(value, value, value, 1.0h);
                }

                return half4(fog.colour, fog.amount);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
