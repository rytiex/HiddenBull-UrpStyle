Shader "Hidden/HiddenBull/Style Precipitation"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleFog.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleTransparency.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleSkyExposure.hlsl"

    TEXTURE2D_FLOAT(_HB_SkyOcclusion);

    float4 _HB_PrecipitationSplit;

    #define HB_SPLIT                 _HB_PrecipitationSplit.x
    #define HB_SKY_RANGE             _HB_PrecipitationSplit.y
    #define HB_SPLIT_TRANSMITTANCE   0.98

    float4x4 _HB_SkyOcclusionViewProj;
    float4 _HB_WeatherParams;
    float4 _HB_RainVelocity;
    float4 _HB_DropBox;
    float4 _HB_DropOffset;
    float4 _HB_RainColor;
    float4 _HB_WindStreak;
    float4 _HB_WeatherMasks[16];
    float _HB_WeatherMaskCount;

    #define HB_WEATHER_TIME     _HB_WeatherParams.w
    #define HB_BOX              _HB_DropBox.x
    #define HB_INNER_START      _HB_DropBox.y
    #define HB_INNER_END        _HB_DropBox.z
    #define HB_SIZE_SCALE       _HB_DropBox.w
    #define HB_SEED             uint(_HB_DropOffset.w)

    #define HB_EXPOSURE_BIAS    0.02
    #define HB_EDGE_SCATTER     0.6
    #define HB_SKY_TEXEL        _HB_WeatherParams.z
    #define HB_EDGE_FADE        0.75
    #define HB_MIN_PIXELS       1.5
    #define HB_MIN_COVERAGE     0.3
    #define HB_DEPTH_FADE       0.2
    #define HB_LANDING_FADE     0.75

    #define HB_RAIN_SHUTTER     0.04
    #define HB_RAIN_WIDTH       0.003
    #define HB_RAIN_MAX_PIXELS  4.0
    #define HB_RAIN_ALPHA       0.55

    #define HB_SNOW_SIZE        0.012
    #define HB_SNOW_SIZE_RANGE  0.01
    #define HB_SNOW_SWAY        0.25
    #define HB_SNOW_ALPHA       0.95

    #define HB_WIND_SEGMENTS    20u
    #define HB_WIND_LIFE        3.0
    #define HB_WIND_LENGTH      22.0
    #define HB_WIND_WIDTH       0.3
    #define HB_WIND_ALPHA       0.6
    #define HB_WIND_SWING       3.0
    #define HB_WIND_SWING_RATE  0.09
    #define HB_WIND_RISE        1.2
    #define HB_WIND_RISE_RATE   0.07
    #define HB_WIND_HEIGHT      30.0
    #define HB_WIND_SPREAD      8.0
    #define HB_WIND_CLEAR       5.0

    struct DropVaryings
    {
        float4 positionCS : SV_POSITION;
        float2 coord      : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
        half4 colour      : COLOR;
    };

    static const float2 HB_CORNERS[6] =
    {
        float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
        float2(0.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0)
    };

    float3 HB_DropSeed(uint drop)
    {
        uint3 v = uint3(drop, drop * 1664525u + 1013904223u, drop ^ 0x9E3779B9u);

        v = v * 1664525u + 1013904223u;
        v.x += v.y * v.z;
        v.y += v.z * v.x;
        v.z += v.x * v.y;
        v ^= v >> 16u;
        v.x += v.y * v.z;
        v.y += v.z * v.x;
        v.z += v.x * v.y;

        return float3(v) * (1.0 / 4294967295.0);
    }

    float3 HB_WrapAround(float3 seed, float3 offset, float box, float3 centre)
    {
        float3 origin = centre - box * 0.5;
        float3 local = seed * box + offset - origin;

        return origin + local - floor(local / box) * box;
    }

    float HB_Exposed(float3 positionWS, float landing)
    {
        if (HB_SKY_TEXEL <= 0.0)
            return 1.0;

        float4 clip = mul(_HB_SkyOcclusionViewProj, float4(positionWS + float3(0.0, HB_EXPOSURE_BIAS, 0.0), 1.0));
        float2 uv = clip.xy;
    #if UNITY_UV_STARTS_AT_TOP
        uv.y = -uv.y;
    #endif
        uv = uv * 0.5 + 0.5;

        if (any(uv < 0.0) || any(uv > 1.0))
            return 1.0;

        float depth = UNITY_NEAR_CLIP_VALUE < 0.0 ? clip.z * 0.5 + 0.5 : clip.z;
        float clear = 1.0;

        UNITY_UNROLL
        for (int i = 0; i < 4; i++)
        {
            float2 offset = float2((i & 1) ? 0.5 : -0.5, (i & 2) ? 0.5 : -0.5) * HB_SKY_TEXEL;
            float stored = SAMPLE_TEXTURE2D_LOD(_HB_SkyOcclusion, sampler_PointClamp, uv + offset, 0).r;

        #if UNITY_REVERSED_Z
            clear = min(clear, (depth - stored) * HB_SKY_RANGE / landing);
        #else
            clear = min(clear, (stored - depth) * HB_SKY_RANGE / landing);
        #endif
        }

        clear = saturate(clear);

        if (clear < 1.0)
            clear = max(clear, HB_ShelterBelowReach(positionWS.xz, positionWS.y));

        return clear;
    }

    float HB_Unmasked(float3 positionWS)
    {
        float clear = 1.0;

        for (int index = 0; index < (int)_HB_WeatherMaskCount; index++)
        {
            float4 mask = _HB_WeatherMasks[index];

            clear = min(clear, (distance(positionWS, mask.xyz) - mask.w) / HB_LANDING_FADE + 1.0);
        }

        return saturate(clear);
    }

    float HB_DropFade(float3 positionWS, float3 seed, float landing)
    {
        float radius = HB_BOX * 0.5;
        float gap = distance(positionWS, GetCameraPositionWS());

        float fade = (1.0 - smoothstep(radius * HB_EDGE_FADE, radius, gap))
                   * smoothstep(HB_INNER_START, HB_INNER_END, gap);

        if (fade <= 0.0)
            return 0.0;

        float2 scatter = (frac(seed.yz * 7.31 + seed.x) - 0.5) * HB_EDGE_SCATTER;

        fade *= HB_Exposed(positionWS + float3(scatter.x, 0.0, scatter.y), landing) * HB_Unmasked(positionWS);

        if (fade <= 0.0)
            return 0.0;

        float2 screenUV = ComputeNormalizedDeviceCoordinates(positionWS, UNITY_MATRIX_VP);

        return fade * (1.0 - HB_FogAmountAt(positionWS, screenUV));
    }

    float HB_PixelsPerMetre(float w)
    {
        return abs(UNITY_MATRIX_P[1][1]) * 0.5 * _ScreenParams.y / max(w, 1e-4);
    }

    half HB_SceneFade(float4 positionCS, float range)
    {
        float2 uv = GetNormalizedScreenSpaceUV(positionCS);
        float sceneEye = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);

        return half(saturate((sceneEye - positionCS.w) / range));
    }

    half HB_SplitMask(float4 positionCS, float3 positionWS)
    {
        UNITY_BRANCH
        if (HB_SPLIT < 0.5)
            return 1.0h;

        uint2 pixel = uint2(positionCS.xy);
        float depth = HB_OITDepth(positionWS);

        bool behind = HB_OITBehindRefraction(pixel, depth) ||
                      HB_OITTransmittanceAt(pixel, depth) < HB_SPLIT_TRANSMITTANCE;

        return (HB_SPLIT < 1.5) == behind ? 1.0h : 0.0h;
    }

    DropVaryings HB_Collapsed()
    {
        DropVaryings output = (DropVaryings)0;
        output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
        return output;
    }

    DropVaryings RainVertex(uint vertexID : SV_VertexID)
    {
        uint drop = vertexID / 6u;
        float2 corner = HB_CORNERS[vertexID % 6u];

        float3 seed = HB_DropSeed(drop + HB_SEED);
        float3 cameraPositionWS = GetCameraPositionWS();
        float3 head = HB_WrapAround(seed, _HB_DropOffset.xyz, HB_BOX, cameraPositionWS);

        float fade = HB_DropFade(head, seed, HB_LANDING_FADE);

        if (fade <= 0.0)
            return HB_Collapsed();

        float3 tail = head - _HB_RainVelocity.xyz * HB_RAIN_SHUTTER;

        float4 headCS = TransformWorldToHClip(head);
        float4 tailCS = TransformWorldToHClip(tail);

        if (headCS.w < 0.05 || tailCS.w < 0.05)
            return HB_Collapsed();

        float2 halfScreen = _ScreenParams.xy * 0.5;
        float2 headPixels = headCS.xy / headCS.w * halfScreen;
        float2 tailPixels = tailCS.xy / tailCS.w * halfScreen;

        float2 axis = tailPixels - headPixels;
        float span = length(axis);
        axis = span > 1e-3 ? axis / span : float2(0.0, 1.0);

        float projected = HB_RAIN_WIDTH * HB_SIZE_SCALE * HB_PixelsPerMetre(headCS.w);
        float width = clamp(projected, HB_MIN_PIXELS, HB_RAIN_MAX_PIXELS);

        float4 positionCS = corner.x < 0.5 ? headCS : tailCS;
        float2 side = float2(-axis.y, axis.x) * ((corner.y * 2.0 - 1.0) * width * 0.5);
        positionCS.xy += side / halfScreen * positionCS.w;

        half3 colour = (HB_AmbientSkyColor() * 1.2h + half3(_HB_KeyColor.rgb) * 0.5h) * half3(_HB_RainColor.rgb);
        half alpha = half(fade * HB_RAIN_ALPHA * max(saturate(projected / width), HB_MIN_COVERAGE));

        DropVaryings output;
        output.positionCS = positionCS;
        output.coord = float2(corner.x, corner.y * 2.0 - 1.0);
        output.positionWS = corner.x < 0.5 ? head : tail;
        output.colour = half4(colour, alpha);
        return output;
    }

    half4 RainFragment(DropVaryings input) : SV_Target
    {
        half across = 1.0h - smoothstep(0.3h, 1.0h, half(abs(input.coord.y)));
        half along = smoothstep(0.0h, 0.15h, half(input.coord.x)) * (1.0h - smoothstep(0.5h, 1.0h, half(input.coord.x)));

        half alpha = input.colour.a * across * along * HB_SceneFade(input.positionCS, HB_DEPTH_FADE)
                   * HB_SplitMask(input.positionCS, input.positionWS);

        return half4(input.colour.rgb * alpha, alpha);
    }

    DropVaryings SnowVertex(uint vertexID : SV_VertexID)
    {
        uint drop = vertexID / 6u;
        float2 corner = HB_CORNERS[vertexID % 6u];

        float3 seed = HB_DropSeed(drop + HB_SEED);
        float3 cameraPositionWS = GetCameraPositionWS();

        float3 centre = HB_WrapAround(seed, _HB_DropOffset.xyz, HB_BOX, cameraPositionWS);
        centre += float3(sin(HB_WEATHER_TIME * 0.9 + seed.y * TWO_PI), 0.0,
                         cos(HB_WEATHER_TIME * 0.7 + seed.z * TWO_PI)) * HB_SNOW_SWAY;

        float fade = HB_DropFade(centre, seed, HB_LANDING_FADE);

        if (fade <= 0.0)
            return HB_Collapsed();

        float4 positionCS = TransformWorldToHClip(centre);

        if (positionCS.w < 0.05)
            return HB_Collapsed();

        float projected = (HB_SNOW_SIZE + HB_SNOW_SIZE_RANGE * seed.x) * HB_SIZE_SCALE
                        * HB_PixelsPerMetre(positionCS.w);
        float size = max(projected, HB_MIN_PIXELS);

        float2 halfScreen = _ScreenParams.xy * 0.5;
        float2 offset = (corner * 2.0 - 1.0) * (size * 0.5);
        positionCS.xy += offset / halfScreen * positionCS.w;

        half3 colour = HB_AmbientSkyColor() * 1.2h + half3(_HB_KeyColor.rgb) * 0.8h;
        half coverage = half(max(saturate(projected / size), HB_MIN_COVERAGE));
        half alpha = half(fade * HB_SNOW_ALPHA) * coverage * coverage;

        DropVaryings output;
        output.positionCS = positionCS;
        output.coord = corner * 2.0 - 1.0;
        output.positionWS = centre;
        output.colour = half4(colour, alpha);
        return output;
    }

    half4 SnowFragment(DropVaryings input) : SV_Target
    {
        half alpha = input.colour.a * (1.0h - smoothstep(0.4h, 1.0h, half(length(input.coord))))
                   * HB_SceneFade(input.positionCS, HB_DEPTH_FADE) * HB_SplitMask(input.positionCS, input.positionWS);

        return half4(input.colour.rgb * alpha, alpha);
    }

    float3 HB_WindPath(float along, float3 origin, float2 heading, float3 shape)
    {
        float2 across = float2(-heading.y, heading.x);
        float swing = HB_WIND_SWING * (0.5 + shape.x);

        float3 position = origin;
        position.xz += heading * along + across * (sin(along * HB_WIND_SWING_RATE + shape.y * TWO_PI) * swing);
        position.y += sin(along * HB_WIND_RISE_RATE + shape.z * TWO_PI) * HB_WIND_RISE;

        return position;
    }

    DropVaryings WindVertex(uint vertexID : SV_VertexID)
    {
        uint ribbon = vertexID / (6u * HB_WIND_SEGMENTS);
        uint local = vertexID % (6u * HB_WIND_SEGMENTS);
        float2 corner = HB_CORNERS[local % 6u];
        float u = (float(local / 6u) + corner.x) / float(HB_WIND_SEGMENTS);

        float strength = _HB_WindStreak.w;
        float3 pace = HB_DropSeed(ribbon * 2u + 977u);
        float life = HB_WIND_LIFE * (0.7 + 0.6 * pace.x);
        float cycle = HB_WEATHER_TIME / life + pace.y;
        float age = frac(cycle);
        uint turn = ribbon * 7919u + uint(floor(cycle)) * 104729u;

        float3 seed = HB_DropSeed(turn + 31u);
        float3 kind = HB_DropSeed(turn + 57u);
        float3 shape = HB_DropSeed(turn + 83u);

        float3 cameraPositionWS = GetCameraPositionWS();
        float3 origin = HB_WrapAround(seed, float3(0.0, 0.0, 0.0), HB_BOX, cameraPositionWS);
        float2 heading = _HB_WindStreak.xy;

        float reach = _HB_WindStreak.z * life;
        float span = HB_WIND_LENGTH * (0.6 + 0.8 * kind.z);
        float along = age * (reach + span) - reach * 0.5 - u * span;

        origin.y = cameraPositionWS.y + HB_WIND_HEIGHT + (seed.y - 0.5) * HB_WIND_SPREAD;

        float3 centre = HB_WindPath(along, origin, heading, shape);
        float3 ahead = HB_WindPath(along + 0.1, origin, heading, shape);

        float fade = HB_DropFade(centre, seed, HB_WIND_CLEAR)
                   * smoothstep(0.0, 0.2, age) * (1.0 - smoothstep(0.7, 1.0, age));

        if (fade <= 0.0)
            return HB_Collapsed();

        float4 centreCS = TransformWorldToHClip(centre);

        if (centreCS.w < 0.05)
            return HB_Collapsed();

        float taper = sin(u * PI);
        float thick = HB_WIND_WIDTH * (0.6 + 0.8 * shape.z);
        float width = max(thick, HB_MIN_PIXELS / HB_PixelsPerMetre(centreCS.w)) * taper;

        float3 toCamera = normalize(cameraPositionWS - centre);
        float3 side = normalize(cross(normalize(ahead - centre), toCamera) + float3(0.0, 1e-4, 0.0));
        float3 positionWS = centre + side * ((corner.y - 0.5) * width);

        half3 colour = HB_AmbientSkyColor() * 1.2h + half3(_HB_KeyColor.rgb) * 0.8h;

        DropVaryings output;
        output.positionCS = TransformWorldToHClip(positionWS);
        output.coord = float2(u, corner.y * 2.0 - 1.0);
        output.positionWS = positionWS;
        output.colour = half4(colour, half(fade * HB_WIND_ALPHA * saturate(0.4 + strength)));
        return output;
    }

    half4 WindFragment(DropVaryings input) : SV_Target
    {
        half alpha = input.colour.a * (1.0h - smoothstep(0.5h, 1.0h, half(abs(input.coord.y))))
                   * HB_SceneFade(input.positionCS, HB_WIND_CLEAR) * HB_SplitMask(input.positionCS, input.positionWS);

        return half4(input.colour.rgb * alpha, alpha);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Style Rain"

            HLSLPROGRAM
            #pragma vertex RainVertex
            #pragma fragment RainFragment
            #pragma target 3.5
            ENDHLSL
        }

        Pass
        {
            Name "Style Snow"

            HLSLPROGRAM
            #pragma vertex SnowVertex
            #pragma fragment SnowFragment
            #pragma target 3.5
            ENDHLSL
        }

        Pass
        {
            Name "Style Wind"

            HLSLPROGRAM
            #pragma vertex WindVertex
            #pragma fragment WindFragment
            #pragma target 3.5
            ENDHLSL
        }
    }

    Fallback Off
}
