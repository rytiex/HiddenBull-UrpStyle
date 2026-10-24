#ifndef HB_STYLE_GLOBALS_INCLUDED
#define HB_STYLE_GLOBALS_INCLUDED

CBUFFER_START(HBStyleFrame)
    float4 _HB_Environment;
    float4 _HB_AmbientFloor;
    float4 _HB_Weather;
    float4 _HB_Brush;
    float4 _HB_BrushVariation;
    float4 _HB_ShadowFilter;
    float4 _HB_ShadowBrush;
    float4 _HB_Sky;
    float4 _HB_SkyBrush;
    float4 _HB_SunDirection;
    float4 _HB_SunColor;
    float4 _HB_SunDisc;
    float4 _HB_SunGlow;
    float4 _HB_MoonColor;
    float4 _HB_MoonDisc;
    float4 _HB_MoonGlow;
    float4 _HB_Stars;
    float4 _HB_StarLook;
    float4 _HB_Aurora;
    float4 _HB_AuroraLow;
    float4 _HB_AuroraMid;
    float4 _HB_AuroraHigh;
    float4 _HB_CloudParams;
    float4 _HB_CloudMotion;
    float4 _HB_CloudLight;
    float4 _HB_CloudAwayLight;
    float4 _HB_CloudDirection;
    float4 _HB_CloudSlab;
    float4 _HB_CloudTint;
    float4 _HB_CloudLook;
    float4 _HB_Fog;
    float4 _HB_FogScatter;
    float4 _HB_Horizon;
    float4 _HB_Debug;
CBUFFER_END

#define HB_TIME_OF_DAY _HB_Environment.x
#define HB_RAIN _HB_Weather.x
#define HB_SNOW _HB_Weather.y
#define HB_WIND _HB_Weather.z

#if defined(HB_STYLE_DEBUG)
    #define HB_DEBUG_VIEW ((int)_HB_Debug.x)
#else
    #define HB_DEBUG_VIEW 0
#endif

#endif
