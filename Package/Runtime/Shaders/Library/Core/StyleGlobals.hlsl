#ifndef HB_STYLE_GLOBALS_INCLUDED
#define HB_STYLE_GLOBALS_INCLUDED

CBUFFER_START(HBStyleFrame)
    float4 _HB_Environment;
    float4 _HB_Weather;
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
