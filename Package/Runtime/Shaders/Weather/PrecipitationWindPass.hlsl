#ifndef HB_PRECIPITATION_WIND_PASS_INCLUDED
#define HB_PRECIPITATION_WIND_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl"

#define HB_WIND_SHARE           _HB_PrecipitationOffset.x
#define HB_WIND_SPEED           _HB_PrecipitationOffset.y
#define HB_WIND_DIRECTION       _HB_PrecipitationVelocity.xz

#define HB_WIND_SEGMENTS        20u
#define HB_WIND_BOX             80.0
#define HB_WIND_LIFE            3.0
#define HB_WIND_LENGTH          22.0
#define HB_WIND_WIDTH           0.3
#define HB_WIND_ALPHA           0.6h
#define HB_WIND_SWING           3.0
#define HB_WIND_SWING_RATE      0.09
#define HB_WIND_RISE            1.2
#define HB_WIND_RISE_RATE       0.07
#define HB_WIND_HEIGHT          30.0
#define HB_WIND_SPREAD          8.0
#define HB_WIND_AMBIENT         1.2h
#define HB_WIND_KEY             0.8h

float3 HB_WindPath(float along, float3 origin, float2 heading, float3 shape)
{
    float2 across = float2(-heading.y, heading.x);
    float swing = HB_WIND_SWING * (0.5 + shape.x);

    float3 position = origin;
    position.xz += heading * along + across * (sin(along * HB_WIND_SWING_RATE + shape.y * TWO_PI) * swing);
    position.y += sin(along * HB_WIND_RISE_RATE + shape.z * TWO_PI) * HB_WIND_RISE;

    return position;
}

HBDropVaryings HB_WindVertex(uint vertexID : SV_VertexID)
{
    uint ribbon = vertexID / (6u * HB_WIND_SEGMENTS);
    uint local = vertexID % (6u * HB_WIND_SEGMENTS);
    float2 corner = HB_DROP_CORNERS[local % 6u];
    float u = (float(local / 6u) + corner.x) / float(HB_WIND_SEGMENTS);

    float3 pace = HB_DropSeed(ribbon * 2u + 977u + HB_DROP_SEED);
    float life = HB_WIND_LIFE * (0.7 + 0.6 * pace.x);
    float cycle = _Time.y / life + pace.y;
    float age = frac(cycle);
    uint turn = ribbon * 7919u + uint(floor(cycle)) * 104729u;

    float3 seed = HB_DropSeed(turn + 31u);
    float3 kind = HB_DropSeed(turn + 57u);
    float3 shape = HB_DropSeed(turn + 83u);

    float3 cameraPositionWS = GetCameraPositionWS();
    float2 boxOrigin = cameraPositionWS.xz - HB_WIND_BOX * 0.5;
    float2 around = seed.xz * HB_WIND_BOX - boxOrigin;
    float3 origin = float3(0.0, cameraPositionWS.y + HB_WIND_HEIGHT + (seed.y - 0.5) * HB_WIND_SPREAD, 0.0);
    origin.xz = boxOrigin + around - floor(around / HB_WIND_BOX) * HB_WIND_BOX;

    float2 heading = HB_WIND_DIRECTION;
    float reach = HB_WIND_SPEED * life;
    float span = HB_WIND_LENGTH * (0.6 + 0.8 * kind.z);
    float along = age * (reach + span) - reach * 0.5 - u * span;

    float3 centre = HB_WindPath(along, origin, heading, shape);
    float3 ahead = HB_WindPath(along + 0.1, origin, heading, shape);

    float gap = distance(centre.xz, cameraPositionWS.xz);
    half fade = half((1.0 - smoothstep(HB_WIND_BOX * 0.35, HB_WIND_BOX * 0.5, gap))
                     * smoothstep(0.0, 0.2, age) * (1.0 - smoothstep(0.7, 1.0, age))) * half(HB_WIND_SHARE);
    half sheltered = 0.0h;

    UNITY_BRANCH
    if (fade > 0.0h)
    {
        sheltered = HB_DropSheltered(centre);
        fade *= 1.0h - HB_FogAmount(centre, cameraPositionWS);
    }

    bool marked;
    half alpha = HB_DropAlpha(HB_WIND_ALPHA, fade, sheltered, marked);

    if (alpha <= 0.0h)
        return HB_CollapsedDrop();

    float4 centreCS = TransformWorldToHClip(centre);

    if (centreCS.w < HB_NEAR_CLIP)
        return HB_CollapsedDrop();

    float thick = HB_WIND_WIDTH * (0.6 + 0.8 * shape.z);
    float width = max(thick, HB_MIN_PIXELS / HB_PixelsPerMetre(centreCS.w)) * sin(u * PI);

    float3 toCamera = normalize(cameraPositionWS - centre);
    float3 side = normalize(cross(normalize(ahead - centre), toCamera) + float3(0.0, 1e-4, 0.0));
    float3 positionWS = centre + side * ((corner.y - 0.5) * width);

    HBDropVaryings output;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.coord = float2(u, corner.y * 2.0 - 1.0);
    output.colour = half4(marked ? HB_SHELTERED_COLOUR
                                 : HB_AmbientSkyColor() * HB_WIND_AMBIENT + half3(_MainLightColor.rgb) * HB_WIND_KEY,
                          alpha);
    return output;
}

half4 HB_WindFragment(HBDropVaryings input) : SV_Target
{
    half alpha = input.colour.a * (1.0h - smoothstep(0.5h, 1.0h, half(abs(input.coord.y))));

    return half4(input.colour.rgb * alpha, alpha);
}

#endif
