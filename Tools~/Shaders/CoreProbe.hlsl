#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

float4 CoreProbeFragment(float4 positionCS : SV_POSITION, float3 positionWS : TEXCOORD0) : SV_Target
{
    float pixels = HB_PixelsPerMetre(HB_ViewDepth(positionWS));
    float noise = HB_Hash12(positionWS.xz) + HB_Hash13(positionWS) + HB_Hash22(positionWS.xy).y;

    return float4(HB_TIME_OF_DAY, HB_RAIN + HB_SNOW + HB_WIND, pixels, noise + HB_DEBUG_VIEW);
}
