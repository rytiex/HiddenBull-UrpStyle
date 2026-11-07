#ifndef HB_LIQUID_SURFACE_PASS_INCLUDED
#define HB_LIQUID_SURFACE_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparency.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparentFog.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidLevel.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidWaves.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_HB_LiquidGradient);

float4 _HB_LiquidOptics[4];
float4 _HB_LiquidShore[4];
float4 _HB_LiquidStyle[4];

float4 _HB_TerrainCamera;
float4 _HB_LiquidRanges[12];

#define HB_LIQUID_SLOTS          4
#define HB_LIQUID_SHARE_MIN      1e-3h
#define HB_LIQUID_GRADIENT_DEPTH 4.0
#define HB_LIQUID_GRADIENT_SIZE  64.0
#define HB_LIQUID_TINT_DEPTH     2.0
#define HB_LIQUID_TINT_MOST      64.0
#define HB_LIQUID_EDGE_SOFT      4.0h
#define HB_LIQUID_MURK_BLUR      0.35h
#define HB_LIQUID_SOLID          0.004h
#define HB_LIQUID_GRAIN          0.5h
#define HB_LIQUID_GLOW_BASE      0.75h
#define HB_LIQUID_GLOW_GRAIN     0.5h
#define HB_LIQUID_THROB_BASE     0.85
#define HB_LIQUID_THROB_RANGE    0.15
#define HB_LIQUID_THROB_GRAIN    9.0
#define HB_LIQUID_SUN_SCATTER    0.5h
#define HB_LIQUID_HALF_TONE      0.35h
#define HB_LIQUID_BAKED_SOFTNESS 0.5h
#define HB_LIQUID_DEBUG_RANGE    0.25h
#define HB_LIQUID_DEBUG_AMBIENT  0.35h

struct HBLiquidAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBLiquidVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float4 rest : TEXCOORD1;
    float4 wave : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct HBLiquidLook
{
    half3 tone;
    half murk;
    half3 shore;
    half smoothness;
    half blur;
    half glow;
    half wrap;
    half softness;
    half pulse;
};

float2 HB_LiquidUV(float2 xzWS)
{
    return (xzWS - _HB_TerrainOrigin.xz) / _HB_TerrainSize.xz;
}

float HB_LiquidHeight(float2 xzWS)
{
    return HB_WaterHeight(HB_LiquidUV(xzWS), _HB_TerrainOrigin.y);
}

HBLiquidVaryings HB_LiquidVertex(HBLiquidAttributes input)
{
    HBLiquidVaryings output = (HBLiquidVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float size = UNITY_MATRIX_M._m00;
    float level = UNITY_MATRIX_M._m11 - 1.0;
    float2 origin = float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m23);
    float2 xz;
    float spacing;

    UNITY_BRANCH
    if (level < 0.0)
    {
        xz = origin + input.positionOS.xz * float2(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m22);
        spacing = max(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m22) / _HB_TerrainCamera.w;
    }
    else
    {
        spacing = _HB_TerrainOrigin.w * exp2(level) / _HB_TerrainCamera.w;
        float2 grid = input.positionOS.xz * round(size / spacing);

        xz = origin + grid * spacing;

        float distance = length(float3(xz.x, HB_LiquidHeight(xz), xz.y) - _HB_TerrainCamera.xyz);
        float2 range = _HB_LiquidRanges[(int)level].xy;
        float morph = saturate((distance - range.x) / max(range.y - range.x, HB_EPSILON));

        grid -= frac(grid * 0.5) * 2.0 * morph;
        xz = origin + grid * spacing;
        spacing *= 1.0 + morph;
    }

    float2 uv = HB_LiquidUV(xz);
    HBWaterLevel water = HB_WaterLevel(uv, _HB_TerrainOrigin.y);
    HBLiquidWaveShape shape = HB_LiquidWaveShape(water.mix);
    HBLiquidSea sea = HB_LiquidSeaAt(xz, shape);
    float near = HB_WaveNear(length(float3(xz.x, water.level, xz.y) - _HB_TerrainCamera.xyz));
    float3 moved = float3(0.0, 0.0, 0.0);

    UNITY_BRANCH
    if (shape.amount * near > 0.0)
        moved = HB_WaveOffset(xz, shape, sea, spacing) * near;

    output.positionWS = float3(xz.x + moved.x, water.level + moved.y, xz.y + moved.z);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.rest = float4(xz, sea.wind, sea.shore);
    output.wave = float4(moved.y, shape.amount, shape.height, shape.length);
    return output;
}

HBLiquidLook HB_LiquidLookAt(half4 mix, float path)
{
    HBLiquidLook look = (HBLiquidLook)0;

    float at = 1.0 - exp(-max(path, 0.0) / HB_LIQUID_GRADIENT_DEPTH);
    float u = (at * (HB_LIQUID_GRADIENT_SIZE - 1.0) + 0.5) / HB_LIQUID_GRADIENT_SIZE;
    half total = 0.0h;

    UNITY_UNROLL
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        half share = mix[slot];

        UNITY_BRANCH
        if (share > HB_LIQUID_SHARE_MIN)
        {
            float v = (slot + 0.5) / HB_LIQUID_SLOTS;
            half4 depth = half4(SAMPLE_TEXTURE2D_LOD(_HB_LiquidGradient, sampler_LinearClamp, float2(u, v), 0));
            half4 optics = half4(_HB_LiquidOptics[slot]);
            half4 style = half4(_HB_LiquidStyle[slot]);

            look.tone += depth.rgb * share;
            look.murk += depth.a * share;
            look.shore += half3(_HB_LiquidShore[slot].rgb) * share;
            look.smoothness += optics.x * share;
            look.blur += optics.z * share;
            look.glow += optics.w * share;
            look.wrap += style.x * share;
            look.softness += style.y * share;
            look.pulse += style.z * share;
            total += share;
        }
    }

    half scale = rcp(max(total, HB_LIQUID_SHARE_MIN));

    look.tone *= scale;
    look.murk = saturate(look.murk * scale);
    look.shore *= scale;
    look.smoothness *= scale;
    look.blur *= scale;
    look.glow *= scale;
    look.wrap *= scale;
    look.softness *= scale;
    look.pulse *= scale;
    return look;
}

float HB_LiquidBehind(float surfaceEye, float2 screenUV)
{
    return LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams) - surfaceEye;
}

HBStyleData HB_LiquidStyle(HBLiquidLook look)
{
    HBStyleData style = (HBStyleData)0;
    style.diffuseWrap = look.wrap;
    style.diffuseSoftness = look.softness;
    style.halfTone = HB_LIQUID_HALF_TONE;
    style.bakedSoftness = HB_LIQUID_BAKED_SOFTNESS;
    style.rimPower = 1.0h;
    return style;
}

half4 HB_LiquidFragment(HBLiquidVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(HB_STYLE_DEBUG)
    UNITY_BRANCH
    if (HB_DEBUG_VIEW == HB_DEBUG_LIQUID_DEPTH || HB_DEBUG_VIEW == HB_DEBUG_LIQUID_FLOW)
    {
        discard;
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }
#endif

    float2 restXZ = input.rest.xy;
    float2 uv = HB_LiquidUV(restXZ);
    HBWaterLevel water = HB_WaterLevel(uv, _HB_TerrainOrigin.y);

    UNITY_BRANCH
    if (!water.wet)
    {
        discard;
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }

    HBLiquidWaveShape shape = (HBLiquidWaveShape)0;
    shape.amount = input.wave.y;
    shape.height = input.wave.z;
    shape.length = input.wave.w;

    float2 restDx = ddx(restXZ);
    float2 restDy = ddy(restXZ);
    float2 slope = float2(0.0, 0.0);

    HBLiquidSea sea;
    sea.wind = input.rest.z;
    sea.shore = input.rest.w;

    UNITY_BRANCH
    if (shape.amount > 0.0)
        slope = HB_WaveSlope(restXZ, restDx, restDy, shape, sea);

    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
    half3 normalWS = half3(normalize(float3(-slope.x, 1.0, -slope.y)));
    half3 viewWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));

#if defined(HB_STYLE_DEBUG)
    UNITY_BRANCH
    if (HB_DEBUG_VIEW == HB_DEBUG_LIQUID_WAVES)
    {
        half rise = saturate(0.5h + half(input.wave.x) * HB_LIQUID_DEBUG_RANGE);
        half3 tone = lerp(half3(0.05h, 0.15h, 0.45h), half3(1.0h, 1.0h, 1.0h), rise);
        half facing = saturate(dot(normalWS, half3(_MainLightPosition.xyz)));

        return half4(tone * (HB_LIQUID_DEBUG_AMBIENT + (1.0h - HB_LIQUID_DEBUG_AMBIENT) * facing), 1.0h);
    }
#endif

    float surfaceEye = LinearEyeDepth(input.positionWS, GetWorldToViewMatrix());
    float thickness = max(HB_LiquidBehind(surfaceEye, screenUV), 0.0);

    HBLiquidLook look = HB_LiquidLookAt(water.mix, thickness);
    HBStyleData style = HB_LiquidStyle(look);

    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    HBShadowProbe probe;

    Light mainLight = GetMainLight();
    mainLight.shadowAttenuation = HB_MainLightShadow(shadowCoord, input.positionWS, half4(1.0h, 1.0h, 1.0h, 1.0h),
                                                     screenUV, probe);

    half3 sunDirection = half3(_MainLightPosition.xyz);
    half3 sunLight = half3(mainLight.color) * mainLight.shadowAttenuation;

    half skyReach;
    HB_ResolveAmbient(normalWS, SampleSH(normalWS), 0.0h, 0.0h, HB_LIQUID_BAKED_SOFTNESS, skyReach);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.smoothness = look.smoothness;
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    half3 reflectVector = reflect(-viewWS, normalWS);
    half3 environment = HB_EnvironmentReflection(reflectVector, normalWS, input.positionWS,
                                                 brdfData.perceptualRoughness, screenUV, skyReach);
    half gloss = 1.0h - brdfData.perceptualRoughness;
    half fresnelTerm = Pow4(1.0h - saturate(dot(normalWS, viewWS))) * (gloss * gloss);

    half3 reflection = EnvironmentBRDFSpecular(brdfData, fresnelTerm) * environment;

    reflection += HB_LightResponse(mainLight, normalWS, normalWS, style, 0.0h)
                * HB_DirectSpecular(brdfData, normalWS, sunDirection, viewWS, style.diffuseSoftness, 0.0h);
    reflection *= saturate(look.murk * HB_LIQUID_EDGE_SOFT);

    half3 skyLight = (HB_GradientAmbient(half3(0.0h, 1.0h, 0.0h)) + HB_GradientAmbient(half3(1.0h, 0.0h, 0.0h)))
                   * 0.5h;
    half throb = half(HB_LIQUID_THROB_BASE + HB_LIQUID_THROB_RANGE
                      * sin(_Time.y * float(look.pulse) + float(HB_LIQUID_GRAIN) * HB_LIQUID_THROB_GRAIN));
    half3 lit = skyLight + sunLight * HB_LIQUID_SUN_SCATTER
              + look.glow * (HB_LIQUID_GLOW_BASE + HB_LIQUID_GLOW_GRAIN * HB_LIQUID_GRAIN) * throb;
    half3 inscatter = look.tone * look.murk * lit;

    HBTransparentFog fog = HB_TransparentFog(input.positionWS);
    half3 colour = (reflection + inscatter) * fog.clear + fog.colour;

    half3 through = PositivePow(clamp(look.shore, 1e-3h, 1.0h),
                                half(min(thickness / HB_LIQUID_TINT_DEPTH, HB_LIQUID_TINT_MOST)))
                  * (1.0h - look.murk);
    half3 tint = lerp(half3(1.0h, 1.0h, 1.0h), through, fog.clear);
    half blur = saturate(look.blur + look.murk * HB_LIQUID_MURK_BLUR) * fog.clear
              * saturate(look.murk * HB_LIQUID_EDGE_SOFT);

    UNITY_BRANCH
    if (max(tint.r, max(tint.g, tint.b)) > HB_LIQUID_SOLID)
        colour += HB_Refracted(screenUV, float2(0.0, 0.0), blur, 0.0h) * tint;

    return half4(colour, 1.0h);
}

#endif
