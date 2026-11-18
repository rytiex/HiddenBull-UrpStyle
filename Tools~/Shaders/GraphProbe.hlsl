#define SHADERPASS SHADERPASS_UNLIT
#define UNLIT_REALTIME_LIGHTING 1

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

#ifdef PROBE_BRUSH
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphBrush.hlsl"
#endif

#ifdef PROBE_WEATHER
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphWeather.hlsl"
#endif

#ifdef PROBE_WEATHER_MOVING
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphWeatherMoving.hlsl"
#endif

#ifdef PROBE_TRIPLANAR
    #define PROBE_TRIPLANAR_ON (_GraphTriplanar > 0.5)
#else
    #define PROBE_TRIPLANAR_ON false
#endif

#ifdef PROBE_MAPS
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphMaps.hlsl"
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphEmission.hlsl"
#endif

#ifdef PROBE_SPECULAR
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphSpecular.hlsl"
#endif

#ifdef PROBE_RIM
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphRim.hlsl"
#endif

#if defined(PROBE_SKIN) || defined(PROBE_CLOTH)
#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/Includes/HBGraphSubsurface.hlsl"
#endif

#include "Packages/com.hiddenbull.urpstyle/ShaderGraph/HBShaderGraph.hlsl"

TEXTURE2D(_GraphBase);
SAMPLER(sampler_GraphBase);
TEXTURE2D(_GraphNormal);
SAMPLER(sampler_GraphNormal);
TEXTURE2D(_GraphMask);
SAMPLER(sampler_GraphMask);
TEXTURE2D(_GraphEmission);
SAMPLER(sampler_GraphEmission);
TEXTURE2D(_GraphThickness);
SAMPLER(sampler_GraphThickness);
TEXTURE2D(_GraphBrushMask);
SAMPLER(sampler_GraphBrushMask);

float4 _GraphBase_ST;
float4 _GraphBrushMask_ST;
float _GraphTriplanar;

struct GraphProbeInput
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;
    float4 uv : TEXCOORD3;
    float3 anchorOS : TEXCOORD4;
    bool frontFace : SV_IsFrontFace;
};

float4 GraphBaseFragment(GraphProbeInput input) : SV_Target
{
    float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
    float4 screen = float4(input.positionCS.xy * _ScreenSize.zw, 0.0, 1.0);
    float2 uv = input.uv.xy;
    float4x4 none = (float4x4)0;

    float4x4 brush = none;
    float4x4 paint = none;
    float4x4 maps = none;
    float4x4 weather = none;
    float4x4 emission = none;
    float4x4 specular = none;
    float4x4 rim = none;
    float4x4 subsurface = none;

#ifdef PROBE_BRUSH
    HB_Brush_float(input.positionWS, input.normalWS, input.frontFace, uv, input.anchorOS, 1.0, 1.0, 0.25, 1.0, 1.0,
                   1.0, 0.235, 0.5, 0.125, 0.125, UnityBuildTexture2DStructNoTexelSize(_GraphBrushMask), brush, paint);
#endif

#ifdef PROBE_MAPS
    HB_Maps_float(float4(0.7, 0.7, 0.7, 1.0), UnityBuildTexture2DStructNoTexelSize(_GraphBase),
                  UnityBuildTexture2DStructNoScaleNoTexelSize(_GraphNormal), 1.0,
                  UnityBuildTexture2DStructNoScaleNoTexelSize(_GraphMask), PROBE_TRIPLANAR_ON, 1.0, paint,
                  input.positionWS, input.normalWS, input.frontFace, input.anchorOS, uv, maps);
    HB_Emission_float(float3(1.0, 1.0, 1.0), UnityBuildTexture2DStructNoScaleNoTexelSize(_GraphEmission), uv, emission);
#endif

#ifdef PROBE_WEATHER
    HB_WeatherStatic_float(0.5, 1.0, weather);
#endif

#ifdef PROBE_WEATHER_MOVING
    float3 moved;
    HB_WeatherMoving_float(0.5, 0.25, 0.0125, 2.0, 0.25, 0.0125, 2.0, float3(0.0, 0.0, 0.0), 2.0,
                           input.anchorOS, input.positionWS, weather, moved);
#endif

#ifdef PROBE_SPECULAR
    HB_Specular_float(0.0, 0.5, specular);
#endif

#ifdef PROBE_RIM
    HB_Rim_float(float3(1.0, 1.0, 1.0), 4.0, 1.0, rim);
#endif

#if defined(PROBE_SKIN) || defined(PROBE_CLOTH)
    HB_Subsurface_float(float3(0.85, 0.25, 0.15), 0.5, 0.6, float3(1.0, 1.0, 1.0), 0.6, 4.0, 0.0, 200.0, 0.3,
                        float3(1.0, 1.0, 1.0), 0.3, 4.0, UnityBuildTexture2DStructNoScaleNoTexelSize(_GraphThickness),
                        uv, subsurface);
#endif

    float3 color;
    float alpha;
    HB_Base_float(input.positionWS, input.normalWS, input.tangentWS.xyz, bitangentWS, screen, uv, input.uv.zw,
                  input.frontFace, 0.265, 0.75, 0.35, 0.25, 0.375, maps, brush, specular, rim, subsurface, emission,
                  weather, color, alpha);

    return float4(color, alpha);
}
