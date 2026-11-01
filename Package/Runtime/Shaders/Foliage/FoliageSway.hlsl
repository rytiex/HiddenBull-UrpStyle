#ifndef HB_FOLIAGE_SWAY_INCLUDED
#define HB_FOLIAGE_SWAY_INCLUDED

#define HB_PLANT_LEAN_BASE  0.3
#define HB_PLANT_LEAN_PRESS 0.7
#define HB_PLANT_ROCK_SPEED 1.3
#define HB_PLANT_ROCK       0.15
#define HB_PLANT_DROP       0.3
#define HB_PLANT_IDLE       0.2
#define HB_PLANT_FLUTTER    7.0

#define HB_PLANT_OWN_SHELTER 1.5

float HB_PlantGust(float3 restWS, float2 wind, float strength, float time)
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    return hb_PlantStreak;
#else
    return HB_GustStreak(restWS.xz, 0.0, wind, strength, time);
#endif
}

float3 HB_PlantSway(float3 restWS, float share, float swayMetres)
{
    float strength = HB_WIND;
    float2 wind = float2(sin(HB_WIND_HEADING), cos(HB_WIND_HEADING));
    float2 side = float2(-wind.y, wind.x);
    float time = _Time.y;
    float wave = dot(restWS.xz, float2(0.37, 0.23));
    float moving = HB_PLANT_IDLE + (1.0 - HB_PLANT_IDLE) * strength;
    float reach = max(share, 0.0);

    float streak = HB_PlantGust(restWS, wind, strength, time);
    float lean = strength * (HB_PLANT_LEAN_BASE + HB_PLANT_LEAN_PRESS * streak);
    float rock = moving * HB_PLANT_ROCK * sin(time * HB_PLANT_ROCK_SPEED + wave);
    float2 push = (wind * (lean + rock) + side * rock) * (reach * reach * swayMetres);

    float3 sway = float3(push.x, -length(push) * HB_PLANT_DROP * reach, push.y);

#ifndef HB_PLANT_BODY
    float flutter = sin(time * HB_PLANT_FLUTTER + dot(restWS, float3(3.1, 2.3, 4.7))) * reach * moving * _WindFlutter;

    sway += float3(side.x, 0.6, side.y) * flutter;
#endif

    return sway;
}

float4 HB_PlantClipPosition(float3 positionWS)
{
    precise float4 positionCS = mul(GetWorldToHClipMatrix(), float4(positionWS, 1.0));

    return positionCS;
}

float3 HB_PlantPosition(float3 restWS, float share, float swayMetres)
{
    precise float3 positionWS = restWS + HB_PlantSway(restWS, share, swayMetres);

    return positionWS;
}

#endif
