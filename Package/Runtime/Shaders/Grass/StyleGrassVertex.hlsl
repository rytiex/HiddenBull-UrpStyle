#ifndef HIDDENBULL_URPSTYLE_GRASS_VERTEX_INCLUDED
#define HIDDENBULL_URPSTYLE_GRASS_VERTEX_INCLUDED

#define HB_GRASS_SNOW_SHRINK  0.6
#define HB_GRASS_GUST_SPEED   2.2
#define HB_GRASS_GUST_LENGTH  0.35
#define HB_GRASS_GUST_SPREAD  0.9
#define HB_GRASS_LEAN         1.4
#define HB_GRASS_CALM         0.45
#define HB_GRASS_IDLE         0.03
#define HB_GRASS_DROOP        0.55
#define HB_GRASS_FLUTTER      0.12
#define HB_GRASS_FRONT_LENGTH 0.09
#define HB_GRASS_FRONT_WIDTH  0.05
#define HB_GRASS_FRONT_SPEED  1.1
#define HB_GRASS_FRONT_BASE   0.35
#define HB_GRASS_SWAY_REACH   0.6
#define HB_GRASS_SWAY_FADE    0.25
#define HB_GRASS_SINK         0.06
#define HB_GRASS_SPLAY        0.2
#define HB_GRASS_SPLAY_DOWN   0.85
#define HB_GRASS_DEPTH_LIFT   0.008
#define HB_GRASS_LIFT_FROM    15.0
#define HB_GRASS_LIFT_RANGE   30.0
#define HB_GRASS_BEND_DROP    0.75
#define HB_GRASS_FAR_TURN     1.6

#define HB_GRASS_FLOWER_SWAY  0.5
#define HB_GRASS_FLOWER_TILT  0.3
#define HB_GRASS_FLOWER_TURN  0.55
#define HB_GRASS_FLOWER_STEM  0.07
#define HB_GRASS_FLOWER_LEAN  0.12
#define HB_GRASS_FLOWER_ROUND 0.4
#define HB_GRASS_FLOWER_SNOW_FROM 0.3
#define HB_GRASS_FLOWER_SNOW_GONE 0.8

struct GrassAttributes
{
    uint vertexID   : SV_VertexID;
    uint instanceID : SV_InstanceID;
};

struct GrassVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 rootWS     : TEXCOORD2;
    half3 normalWS    : TEXCOORD3;
    half3 root        : TEXCOORD4;
    half3 tip         : TEXCOORD5;
    half2 limits      : TEXCOORD6;
};

static const float2 s_GrassCorners[4] =
{
    float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0)
};

float3 HB_GrassBend(float3 rootWS)
{
    float3 push = float3(0.0, 0.0, 0.0);

    for (int index = 0; index < (int)HB_GRASS_BENDERS; index++)
    {
        float4 bender = _HB_GrassBenders[index];
        float3 away = rootWS - bender.xyz;
        float flat = length(away.xz);

        float press = saturate(1.0 - flat / bender.w) * saturate(2.0 - abs(away.y) / bender.w)
                    * _HB_GrassBenderLooks[index].x;

        push += float3(away.x, 0.0, away.z) * (press / max(flat, 1e-3));
        push.y -= press * HB_GRASS_BEND_DROP;
    }

    float most = max(length(push.xz), -push.y / HB_GRASS_BEND_DROP);

    return push / max(most, 1.0);
}

float3 HB_GrassSway(float3 rootWS, float seed, float lift)
{
    float2 wind = HB_WIND_DIRECTION;
    float2 across = float2(-wind.y, wind.x);
    float strength = HB_WIND_STRENGTH;
    float travel = dot(rootWS.xz, wind);

    float phase = _Time.y * HB_GRASS_GUST_SPEED * (0.6 + strength) - travel * HB_GRASS_GUST_LENGTH
                + seed * HB_GRASS_GUST_SPREAD;
    float fronts = sin(travel * HB_GRASS_FRONT_LENGTH - _Time.y * HB_GRASS_FRONT_SPEED)
                 * sin(dot(rootWS.xz, across) * HB_GRASS_FRONT_WIDTH + _Time.y * 0.3);
    float lean = strength * (HB_GRASS_CALM + (1.0 - HB_GRASS_CALM) * (0.5 + 0.5 * sin(phase)))
               * (HB_GRASS_FRONT_BASE + (1.0 - HB_GRASS_FRONT_BASE) * (0.5 + 0.5 * fronts));
    float flutter = sin(_Time.y * (7.0 + 6.0 * seed) + seed * 40.0) * (strength * strength * HB_GRASS_FLUTTER);

    float2 push = wind * (lean * HB_GRASS_LEAN + flutter)
                + float2(sin(phase * 0.7 + 1.3), cos(phase * 0.9)) * HB_GRASS_IDLE;

    float still = saturate((HB_GRASS_FAR * HB_GRASS_SWAY_REACH - distance(rootWS, GetCameraPositionWS()))
                         / (HB_GRASS_FAR * HB_GRASS_SWAY_FADE));

    return float3(push.x, -lean * HB_GRASS_DROOP, push.y) * (lift * still) + HB_GrassBend(rootWS) * lift;
}

#ifdef _HB_GRASS_FLOWERS

float3 HB_GrassShape(uint vertexID, HiddenBullGrassInstance blade, float3 normal, float3 up, float height,
                     float4 root, float4 tip, out float2 uv, out float3 shading)
{
    float2 corner = s_GrassCorners[vertexID & 3u];
    float turn = tip.a * TWO_PI * 7.0;
    float3 away = float3(cos(turn), 0.0, sin(turn));
    float3 toCamera = normalize(GetCameraPositionWS() - blade.position);

    float3 sway = HB_GrassSway(blade.position, tip.a, height) * HB_GRASS_FLOWER_SWAY;
    float3 centre = blade.position + float3(0.0, height, 0.0) + away * (HB_GRASS_FLOWER_LEAN * height) + sway;

    shading = normal;

    if (vertexID < 4)
    {
        float3 side = normalize(cross(float3(0.0, 1.0, 0.0), toCamera) + float3(1e-4, 0.0, 0.0));
        float along = corner.y * corner.y;

        uv = float2(-1.0, corner.y);

        return lerp(blade.position - up * (HB_GRASS_SINK * height), centre, float3(along, corner.y, along))
             + side * ((corner.x - 0.5) * blade.width * HB_GRASS_FLOWER_STEM);
    }

    float3 across = float3(-away.z, 0.0, away.x);
    float3 face = normalize(float3(0.0, 1.0, 0.0) + away * HB_GRASS_FLOWER_TILT + toCamera * HB_GRASS_FLOWER_TURN);
    float3 wide = normalize(across - face * dot(across, face));
    float3 tall = cross(face, wide);

    uv = float2((floor(root.a * 8.0) + corner.x) / max(HB_GRASS_VARIANTS, 1.0), corner.y);
    shading = normalize(normal + face * HB_GRASS_FLOWER_ROUND);

    return centre + (wide * (corner.x - 0.5) + tall * (corner.y - 0.5)) * blade.width;
}

#elif defined(_HB_GRASS_FAR)

float3 HB_GrassShape(uint vertexID, HiddenBullGrassInstance blade, float3 normal, float3 up, float height,
                     float4 root, float4 tip, out float2 uv, out float3 shading)
{
    float2 corner = s_GrassCorners[vertexID & 3u];
    float3 toCamera = GetCameraPositionWS() - blade.position;
    float3 level = toCamera - up * dot(toCamera, up);
    float turn = atan2(level.z, level.x) + (root.a - 0.5) * HB_GRASS_FAR_TURN;
    float3 face = float3(cos(turn), 0.0, sin(turn));

    face = normalize(face - up * dot(face, up));

    float3 side = normalize(cross(up, face));

    float lift = corner.y * corner.y * height;
    float overhead = saturate(dot(normalize(toCamera), up));
    float splay = lerp(HB_GRASS_SPLAY, HB_GRASS_SPLAY_DOWN, overhead * overhead);
    float3 stem = up * sqrt(1.0 - splay * splay) + face * splay;

    uv = float2(tip.a > 0.5 ? 1.0 - corner.x : corner.x, corner.y);
    shading = normal;

    return blade.position - up * (HB_GRASS_SINK * height) + side * ((corner.x - 0.5) * blade.width)
         + stem * (corner.y * height) + HB_GrassSway(blade.position, tip.a, lift);
}

#else

float3 HB_GrassShape(uint vertexID, HiddenBullGrassInstance blade, float3 normal, float3 up, float height,
                     float4 root, float4 tip, out float2 uv, out float3 shading)
{
    float2 corner = s_GrassCorners[vertexID & 3u];
    float angle = root.a * PI + float(vertexID / 4) * HALF_PI;
    float3 side = float3(cos(angle), 0.0, sin(angle));

    side = normalize(side - normal * dot(side, normal));

    float lift = corner.y * corner.y * height;
    float overhead = saturate(dot(normalize(GetCameraPositionWS() - blade.position), up));
    float splay = lerp(HB_GRASS_SPLAY, HB_GRASS_SPLAY_DOWN, overhead * overhead);
    float3 face = normalize(cross(side, up)) * (vertexID < 4 ? 1.0 : -1.0);
    float3 stem = up * sqrt(1.0 - splay * splay) + face * splay;

    uv = float2(tip.a > 0.5 ? 1.0 - corner.x : corner.x, corner.y);
    shading = normal;

    return blade.position - up * (HB_GRASS_SINK * height) + side * ((corner.x - 0.5) * blade.width)
         + stem * (corner.y * height) + HB_GrassSway(blade.position, tip.a, lift);
}

#endif

float3 HB_GrassLift(float3 positionWS)
{
    float3 toCamera = GetCameraPositionWS() - positionWS;
    float ramp = saturate((length(toCamera) - HB_GRASS_LIFT_FROM) / HB_GRASS_LIFT_RANGE);

    return positionWS + toCamera * (HB_GRASS_DEPTH_LIFT * ramp);
}

GrassVaryings HiddenBullGrassVertex(GrassAttributes input)
{
    GrassVaryings output = (GrassVaryings)0;

    if (input.instanceID >= (uint)HB_GRASS_CAPACITY)
        return output;

    HiddenBullGrassInstance blade = _HB_GrassBuffer[input.instanceID];

    float4 root = HB_GrassUnpackColour(blade.root);
    float4 tip = HB_GrassUnpackColour(blade.tip);
    float3 normal;
    float2 limits;

    HB_GrassUnpackGround(blade.ground, normal, limits);

    float3 up = normalize(float3(0.0, 1.0, 0.0) + normal);
    float cover = saturate(HB_SURFACE_SNOW) * limits.y * HB_GRASS_RECEIVE;

    UNITY_BRANCH
    if (cover > 0.0)
        cover *= HB_SkyExposureCoarse(blade.position, half(normal.y));

    float height = blade.height
                 * (1.0 - HB_GRASS_SNOW_SHRINK * cover);

#ifdef _HB_GRASS_FLOWERS
    float buried = 1.0 - smoothstep(HB_GRASS_FLOWER_SNOW_FROM, HB_GRASS_FLOWER_SNOW_GONE, cover);

    height *= buried;
    blade.width *= buried;
#endif

    float2 uv;
    float3 shading;
    float3 positionWS = HB_GrassShape(input.vertexID, blade, normal, up, height, root, tip, uv, shading);

    output.positionCS = TransformWorldToHClip(positionWS);

    float4 lifted = TransformWorldToHClip(HB_GrassLift(positionWS));

    output.positionCS.z = lifted.z / lifted.w * output.positionCS.w;
    output.uv = uv;
    output.positionWS = positionWS;
    output.rootWS = blade.position;
    output.normalWS = half3(shading);
    output.root = half3(root.rgb);
    output.tip = half3(tip.rgb);
    output.limits = half2(limits);

    return output;
}

#endif
