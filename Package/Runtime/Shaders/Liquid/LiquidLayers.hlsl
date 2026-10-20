#ifndef HIDDENBULL_URPSTYLE_LIQUID_LAYERS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIQUID_LAYERS_INCLUDED

#define HB_LAYER_SLOTS          3
#define HB_LAYER_ROUGH          0.5
#define HB_LAYER_GRAIN          6.0
#define HB_LAYER_SECOND         0.53
#define HB_LAYER_DRIFT          0.012
#define HB_LAYER_BLUR           2.0
#define HB_LAYER_FLOOR          1e-3
#define HB_LAYER_BUBBLES        4.0
#define HB_LAYER_OWN            1e-3

float4 _OtherWave[HB_LAYER_SLOTS];
float4 _OtherShape[HB_LAYER_SLOTS];
float4 _OtherFoam[HB_LAYER_SLOTS];
float4 _OtherSurface[HB_LAYER_SLOTS];
float4 _OtherFoamColor[HB_LAYER_SLOTS];
float4 _OtherCrestColor[HB_LAYER_SLOTS];
float4 _OtherCrustColor[HB_LAYER_SLOTS];
float4 _OtherOptics[HB_LAYER_SLOTS];
float4 _OtherUnder[HB_LAYER_SLOTS];
float4 _OtherDepthColor0[HB_LAYER_SLOTS];
float4 _OtherDepthColor1[HB_LAYER_SLOTS];
float4 _OtherDepthColor2[HB_LAYER_SLOTS];
float4 _OtherDepthColor3[HB_LAYER_SLOTS];
float4 _OtherDepthColor4[HB_LAYER_SLOTS];
float4 _OtherDepthColor5[HB_LAYER_SLOTS];
float4 _OtherDepthTimes0[HB_LAYER_SLOTS];
float4 _OtherDepthTimes1[HB_LAYER_SLOTS];
float4 _OtherDepthAlpha0[HB_LAYER_SLOTS];
float4 _OtherDepthAlpha1[HB_LAYER_SLOTS];
float4 _OtherDepthAlphaTimes0[HB_LAYER_SLOTS];
float4 _OtherDepthAlphaTimes1[HB_LAYER_SLOTS];

TEXTURE2D(_OtherBumpMap0);
TEXTURE2D(_OtherBumpMap1);
TEXTURE2D(_OtherBumpMap2);
TEXTURE2D(_OtherBaseMap0);
TEXTURE2D(_OtherBaseMap1);
TEXTURE2D(_OtherBaseMap2);
TEXTURE2D(_WaterWeightMap);

static float3 s_Mix = float3(0.0, 0.0, 0.0);

bool HB_LiquidLayered()
{
    return _WaterLayers > 0.5;
}

float HB_LiquidOwnShare(float3 weights)
{
    float own = 1.0 - weights.x - weights.y - weights.z;

    return own > HB_LAYER_OWN ? own : 0.0;
}

float HB_LiquidSourceShare(int source, float own, float3 weights)
{
    return source == 0 ? own : source == 1 ? weights.x : source == 2 ? weights.y : weights.z;
}

float3 HB_LiquidSourceMix(int source)
{
    return float3(source == 1 ? 1.0 : 0.0, source == 2 ? 1.0 : 0.0, source == 3 ? 1.0 : 0.0);
}

float HB_Mix(float own, float first, float second, float third)
{
    return own + (first - own) * s_Mix.x + (second - own) * s_Mix.y + (third - own) * s_Mix.z;
}

float3 HB_Mix3(float3 own, float3 first, float3 second, float3 third)
{
    return own + (first - own) * s_Mix.x + (second - own) * s_Mix.y + (third - own) * s_Mix.z;
}

float HB_LiquidLayerRough(float2 positionXZ, float2 positionDx, float2 positionDy)
{
    float scale = 1.0 / HB_LAYER_GRAIN;
    float2 uv = positionXZ * scale;
    float2 drift = _HB_WaveFrame.xy * (HB_LiquidTime() * HB_LAYER_DRIFT);
    float2 uvDx = positionDx * (scale * HB_LAYER_BLUR);
    float2 uvDy = positionDy * (scale * HB_LAYER_BLUR);
    float coarse = SAMPLE_TEXTURE2D_GRAD(_HB_FoamPattern, sampler_HB_FoamPattern, uv + drift, uvDx, uvDy).r;
    float fine = SAMPLE_TEXTURE2D_GRAD(_HB_FoamPattern, sampler_HB_FoamPattern,
                                       HB_WaveTurn(uv, 1.0) * HB_LAYER_SECOND - drift * 0.7, uvDx, uvDy).r;

    return (_HB_CausticParams.z > 0.5 ? (coarse + fine) * 0.5 : 0.5) * HB_LAYER_ROUGH;
}

float3 HB_LiquidLayerPaint(float2 positionXZ)
{
    return HB_LevelRead(_WaterWeightMap, _WaterArea, positionXZ).rgb;
}

float3 HB_LiquidLayerBlend(float3 painted, float rough)
{
    float4 share = float4(saturate(1.0 - painted.x - painted.y - painted.z), painted);
    float4 weights = 1.0 - smoothstep(rough, rough + 1.0 - HB_LAYER_ROUGH, 1.0 - share) + share * HB_LAYER_FLOOR;

    return weights.yzw / (weights.x + weights.y + weights.z + weights.w);
}

float3 HB_LiquidLayerWeights(float2 positionXZ, float rough)
{
    return HB_LiquidLayerBlend(HB_LiquidLayerPaint(positionXZ), rough);
}

float HB_LiquidHeight()
{
    return HB_Mix(float(_WaveHeight), _OtherWave[0].z, _OtherWave[1].z, _OtherWave[2].z);
}

float HB_LiquidSteep()
{
    return HB_Mix(float(_WaveSteepness), _OtherWave[0].w, _OtherWave[1].w, _OtherWave[2].w);
}

float HB_LiquidLength()
{
    return max(HB_Mix(float(_WaveLength), _OtherShape[0].x, _OtherShape[1].x, _OtherShape[2].x),
               HB_WAVE_LENGTH_MIN);
}

float HB_LiquidFoamAmount()
{
    return HB_Mix(float(_FoamAmount), _OtherFoam[0].x, _OtherFoam[1].x, _OtherFoam[2].x);
}

float HB_LiquidShoreFoam()
{
    return HB_Mix(float(_ShoreFoam), _OtherFoam[0].y, _OtherFoam[1].y, _OtherFoam[2].y);
}

float HB_LiquidFoamDistance()
{
    return max(HB_Mix(float(_FoamDistance), _OtherFoam[0].z, _OtherFoam[1].z, _OtherFoam[2].z), 1e-3);
}

half HB_LiquidCrustAmount()
{
    return half(HB_Mix(float(_CrustAmount), _OtherFoam[0].w, _OtherFoam[1].w, _OtherFoam[2].w));
}

half HB_LiquidBubbleAmount()
{
    return half(HB_Mix(float(_BubbleAmount), _OtherShape[0].z, _OtherShape[1].z, _OtherShape[2].z));
}

half HB_LiquidGlow()
{
    return half(HB_Mix(float(_Glow), _OtherShape[0].w, _OtherShape[1].w, _OtherShape[2].w));
}

half HB_LiquidSmoothness()
{
    return half(HB_Mix(float(_Smoothness), _OtherSurface[0].w, _OtherSurface[1].w, _OtherSurface[2].w));
}

float HB_LiquidPatternFit()
{
    float tiling = max(float(_Tiling), 1e-4);

    return HB_Mix(float(_PatternScale), _OtherSurface[0].z * _OtherSurface[0].x / tiling,
                  _OtherSurface[1].z * _OtherSurface[1].x / tiling, _OtherSurface[2].z * _OtherSurface[2].x / tiling);
}

float HB_LiquidTilingFit()
{
    float tiling = max(float(_Tiling), 1e-4);

    return HB_Mix(1.0, _OtherSurface[0].x / tiling, _OtherSurface[1].x / tiling, _OtherSurface[2].x / tiling);
}

half3 HB_LiquidFoamColour()
{
    return half3(HB_Mix3(_FoamColor.rgb, _OtherFoamColor[0].rgb, _OtherFoamColor[1].rgb, _OtherFoamColor[2].rgb));
}

half3 HB_LiquidCrestColour()
{
    return half3(HB_Mix3(_CrestColor.rgb, _OtherCrestColor[0].rgb, _OtherCrestColor[1].rgb,
                         _OtherCrestColor[2].rgb));
}

half3 HB_LiquidCrustColour()
{
    return half3(HB_Mix3(_CrustColor.rgb, _OtherCrustColor[0].rgb, _OtherCrustColor[1].rgb,
                         _OtherCrustColor[2].rgb));
}

half3 HB_LiquidShoreColour()
{
    return half3(HB_Mix3(_DepthColor0.rgb, _OtherDepthColor0[0].rgb, _OtherDepthColor0[1].rgb,
                         _OtherDepthColor0[2].rgb));
}

half HB_LiquidRefraction()
{
    return half(HB_Mix(float(_Refraction), _OtherOptics[0].x, _OtherOptics[1].x, _OtherOptics[2].x));
}

half HB_LiquidRefractionBlur()
{
    return half(HB_Mix(float(_RefractionBlur), _OtherOptics[0].y, _OtherOptics[1].y, _OtherOptics[2].y));
}

half HB_LiquidCausticAmount()
{
    return half(HB_Mix(float(_Caustics), _OtherOptics[0].z, _OtherOptics[1].z, _OtherOptics[2].z));
}

half HB_LiquidCrestSoftness()
{
    return half(HB_Mix(float(_CrestSoftness), _OtherOptics[0].w, _OtherOptics[1].w, _OtherOptics[2].w));
}

half HB_LiquidUnderShafts()
{
    return half(HB_Mix(float(_UnderShafts), _OtherUnder[0].x, _OtherUnder[1].x, _OtherUnder[2].x));
}

half HB_LiquidUnderBubbles()
{
    return half(HB_Mix(float(_UnderBubbles), _OtherUnder[0].y, _OtherUnder[1].y, _OtherUnder[2].y));
}

half HB_LiquidUnderDistortion()
{
    return half(HB_Mix(float(_UnderDistortion), _OtherUnder[0].z, _OtherUnder[1].z, _OtherUnder[2].z));
}

bool HB_LiquidCrusted()
{
    return _CrustAmount > 0.0h
        || (HB_LiquidLayered() && dot(s_Mix, float3(_OtherFoam[0].w, _OtherFoam[1].w, _OtherFoam[2].w)) > 0.0);
}

half HB_LiquidBubbleShare()
{
    float3 others = step(1e-4, float3(_OtherShape[0].z, _OtherShape[1].z, _OtherShape[2].z));
    float own = _BubbleAmount > 0.0h ? 1.0 - saturate(s_Mix.x + s_Mix.y + s_Mix.z) : 0.0;

    return half(saturate((own + dot(s_Mix, others)) * HB_LAYER_BUBBLES));
}

bool HB_LiquidBubbled()
{
    return _BubbleAmount > 0.0h
        || (HB_LiquidLayered() && dot(s_Mix, float3(_OtherShape[0].z, _OtherShape[1].z, _OtherShape[2].z)) > 0.0);
}

#endif
