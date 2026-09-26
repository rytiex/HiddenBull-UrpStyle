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
            uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraDepthTexture_TexelSize.xy);

            return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0).r;
        }

        bool HB_IsSurface(float depth)
        {
        #if UNITY_REVERSED_Z
            return depth > 0.0;
        #else
            return depth < 1.0;
        #endif
        }

        bool HB_ScenePosition(float2 uv, out float3 positionWS)
        {
            float depth = HB_SceneDepth(uv);

            positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

            return HB_IsSurface(depth);
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

                sum += SAMPLE_TEXTURE2D_LOD(_HB_FogReach, sampler_LinearClamp, uv, 0).r * weight;
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

            #define HB_FOG_SILHOUETTE_TILES 8.0

            half4 HB_FogStrokePlanes(float3 position, half3 blend, float lod)
            {
                half4 sum = half4(0.0h, 0.0h, 0.0h, 0.0h);
                half total = 0.0h;

                UNITY_BRANCH
                if (blend.x > 0.02h)
                {
                    sum += SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas, position.zy, lod) * blend.x;
                    total += blend.x;
                }

                UNITY_BRANCH
                if (blend.y > 0.02h)
                {
                    sum += SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas, position.xz, lod) * blend.y;
                    total += blend.y;
                }

                UNITY_BRANCH
                if (blend.z > 0.02h)
                {
                    sum += SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas, position.xy, lod) * blend.z;
                    total += blend.z;
                }

                return sum * rcp(max(total, HB_EPSILON));
            }

            half4 HB_FogWorldStroke(float3 positionWS, half3 blend, float tile, float lod)
            {
                float level = log2(max(tile, 1e-3));
                float base = floor(level);
                float density = exp2(-base);

                half4 fine = HB_FogStrokePlanes(positionWS * density, blend, lod);
                half4 coarse = HB_FogStrokePlanes(positionWS * (density * 0.5), blend, lod);

                return lerp(fine, coarse, half(level - base));
            }

            float _HB_FogDebug;

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                int mode = (int)_HB_FogDebug;

                float depth = HB_SceneDepth(uv);

                float3 cameraPositionWS = GetCameraPositionWS();
                float3 positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 facing = cross(ddy(positionWS), ddx(positionWS));

                if (!HB_IsSurface(depth))
                    return mode > 0 ? half4(0.0h, 0.0h, 0.0h, 1.0h) : half4(0.0h, 0.0h, 0.0h, 0.0h);

                float3 anchorWS = positionWS;
                float3 ray = positionWS - cameraPositionWS;
                float rayLength = max(length(ray), 1e-4);
                half3 direction = half3(ray / rayLength);

                half amount = HB_FogAmount(positionWS, cameraPositionWS);
                bool surface = true;

                bool brush = HB_BRUSH_ATLAS_BOUND > 0.5h;
                half3 blend = HB_TriplanarBlend(half3(SafeNormalize(facing)));
                float tileAngle = TWO_PI / HB_SkyBrushTurns(max(HB_SKY_BRUSH_SCALE, 1e-3));
                float span = rayLength * tileAngle;

                half silhouette = half(HB_FOG_SILHOUETTE);

                UNITY_BRANCH
                if (brush && silhouette > 0.0h)
                {
                    half4 stroke = HB_FogWorldStroke(anchorWS, blend, span / HB_FOG_SILHOUETTE_TILES, 1.0);

                    float2 spine = float2(stroke.rg * 2.0h - 1.0h)
                                 * (HB_BRUSH_SPINE_RANGE * silhouette * tileAngle / HB_FOG_SILHOUETTE_TILES);

                    float3 painted = float3(direction) + UNITY_MATRIX_V[0].xyz * spine.x
                                                       + UNITY_MATRIX_V[1].xyz * spine.y;

                    float2 paintedUV = saturate(ComputeNormalizedDeviceCoordinates(
                        cameraPositionWS + painted, UNITY_MATRIX_VP));

                    float paintedDepth = HB_SceneDepth(paintedUV);
                    bool paintedSurface = HB_IsSurface(paintedDepth);

                    float3 paintedWS = ComputeWorldSpacePosition(paintedUV, paintedDepth, UNITY_MATRIX_I_VP);
                    float3 paintedRay = paintedWS - cameraPositionWS;
                    half3 paintedDirection = half3(paintedRay / max(length(paintedRay), 1e-4));

                    half paintedAmount = paintedSurface
                        ? HB_FogAmount(paintedWS, cameraPositionWS)
                        : HB_SkyHaze(paintedDirection);

                    if (paintedAmount > amount)
                    {
                        amount = paintedAmount;
                        uv = paintedUV;
                        depth = paintedDepth;
                        positionWS = paintedWS;
                        direction = paintedDirection;
                        surface = paintedSurface;
                    }
                }

                if (mode == 0 && amount <= 0.0h)
                    return half4(0.0h, 0.0h, 0.0h, 0.0h);

                half sunlit = 1.0h;
                half reach = 1.0h;

                UNITY_BRANCH
                if (surface)
                {
                    sunlit = FragShadow(positionWS);

                    if (_HB_FogReachSize.z > 0.5)
                        reach = HB_GatherReach(uv, LinearEyeDepth(depth, _ZBufferParams));
                }

                if (mode > 0)
                {
                    half value = mode == 1 ? reach
                               : mode == 2 ? (_HB_FogReachSize.z > 0.5
                                              ? SAMPLE_TEXTURE2D_LOD(_HB_FogReach, sampler_LinearClamp, uv, 0).r
                                              : 1.0h)
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

                half paint = 0.0h;
                half4 atlas = half4(0.5h, 0.5h, 0.5h, 1.0h);

                UNITY_BRANCH
                if (brush && HB_FOG_PAINT > 0.0)
                {
                    paint = half(HB_FOG_PAINT);
                    atlas = HB_FogWorldStroke(anchorWS, blend, span, HB_SKY_BRUSH_SMOOTH * 4.0);

                    half far = smoothstep(0.5h, 1.0h, amount * rcp(max(half(_HB_FogParams.w), HB_EPSILON)));

                    if (far > 0.0h)
                    {
                        half domePaint = HB_FogDomePaint(direction);

                        if (domePaint > 0.0h)
                            atlas = lerp(atlas, HB_SkyStroke(direction), far);

                        paint = lerp(paint, domePaint, far);
                    }
                }

                return half4(HB_FogColourPainted(direction, atlas, paint, sunlit, reach), amount);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
