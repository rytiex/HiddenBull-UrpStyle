#ifndef HIDDENBULL_URPSTYLE_CLOUDS_INCLUDED
#define HIDDENBULL_URPSTYLE_CLOUDS_INCLUDED

#include "StyleFog.hlsl"

TEXTURE2D(_HB_CloudAtlas);
SAMPLER(sampler_HB_CloudAtlas);

float4 _HB_CloudParams;
float4 _HB_CloudMotion;
float4 _HB_CloudLight;
float4 _HB_CloudAwayLight;
float4 _HB_CloudSlab;
float4 _HB_CloudTint;

#define HB_CLOUD_PLANET_RADIUS  200.0
#define HB_CLOUD_LAYER_HEIGHT   20.0
#define HB_CLOUD_DISTANCE_SCALE 100.0
#define HB_CLOUD_RELIEF_UP      0.45h
#define HB_CLOUD_COVERAGE_FLOOR 0.25h
#define HB_CLOUD_SHADE_FLOOR    0.85h
#define HB_CLOUD_BRUSH_EDGE     0.12h
#define HB_CLOUD_BRUSH_ALPHA    0.6h
#define HB_CLOUD_SILVER_GAIN    2.0h
#define HB_CLOUD_RIM_FOCUS      12.0h
#define HB_CLOUD_BODY_FOCUS     40.0h
#define HB_CLOUD_ABSORPTION     3.0h

float HB_CloudTravel(float up, float layerHeight)
{
    float b = HB_CLOUD_PLANET_RADIUS * up;
    return -b + sqrt(b * b + layerHeight * (2.0 * HB_CLOUD_PLANET_RADIUS + layerHeight));
}

half3 HB_SkyWithClouds(half3 direction)
{
    half3 sky = HB_SkyWithFog(direction);

    half horizon = smoothstep(0.0h, 0.03h, direction.y);

    if (_HB_CloudParams.x <= 0.0 || horizon <= 0.0h)
        return sky;

    float scale = _HB_CloudParams.y;
    float2 drift = _HB_CloudMotion.xy * _Time.y;

    int steps = max((int)_HB_CloudParams.w, 1);
    half spread = half(rsqrt((float)steps));

    half3 lightColour = lerp(_HB_CloudAwayLight.rgb, _HB_CloudLight.rgb, HB_SunFacing(direction));
    half3 lightDirection = half3(_HB_KeyDirection.xyz) * half(_HB_CloudLight.a);

    half3 shade = HB_SkySample(direction, direction.y) * HB_CLOUD_SHADE_FLOOR;

    half soft = max(half(_HB_CloudParams.z), HB_EPSILON);
    half shading = half(_HB_CloudSlab.z);
    half absorption = shading * HB_CLOUD_ABSORPTION * spread;
    half translucency = half(_HB_CloudMotion.w);
    half towardSun = saturate(dot(direction, lightDirection));

    float travel = HB_CloudTravel(direction.y, HB_CLOUD_LAYER_HEIGHT);
    float2 reference = direction.xz * (travel * scale);

    float2 slab = direction.xz * (_HB_CloudSlab.x * spread);

    half middle = (steps - 1) * 0.5h;
    half taper = half(_HB_CloudSlab.y) * rcp(max(middle, 1.0h));

    half stroke = 0.0h;
    half brush = half(_HB_CloudMotion.z);

    if (brush > 0.0h && HB_BRUSH_ATLAS_BOUND > 0.5h)
    {
        half4 painted = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, reference * 1.5);
        stroke = (painted.b * 2.0h - 1.0h) * brush;
    }

    half threshold = lerp(1.0h, HB_CLOUD_COVERAGE_FLOOR, half(_HB_CloudParams.x))
                   + stroke * HB_CLOUD_BRUSH_EDGE;

    half shellSoft = max(soft + taper * 0.6h, HB_EPSILON);

    half3 colour = half3(0.0h, 0.0h, 0.0h);
    half alpha = 0.0h;
    half litAccum = 0.0h;
    half transmittance = 1.0h;

    [loop]
    for (int i = steps - 1; i >= 0; i--)
    {
        half4 atlas = SAMPLE_TEXTURE2D(_HB_CloudAtlas, sampler_HB_CloudAtlas,
                                       reference + slab * i + drift);

        half cutoff = threshold + abs((half)i - middle) * taper;

        half shell = saturate(smoothstep(cutoff - shellSoft, cutoff + shellSoft, atlas.a)
                              * (1.0h + stroke * HB_CLOUD_BRUSH_ALPHA));

        if (shell > 0.0h)
        {
            half2 relief = atlas.rg * 2.0h - 1.0h;
            half3 normal = normalize(half3(relief.x, HB_CLOUD_RELIEF_UP, relief.y));

            half shaped = HB_WrappedDiffuse(dot(normal, lightDirection), 0.5h, 0.6h);

            half lit = lerp(1.0h, shaped, shading) * transmittance;

            half3 shellColour = lerp(shade, max(shade, lightColour), lit);

            colour = lerp(colour, shellColour, shell);
            litAccum = lerp(litAccum, lit, shell);
            alpha = shell + alpha * (1.0h - shell);
        }

        transmittance *= exp(-atlas.a * absorption);
    }

    if (alpha <= 0.0h)
        return sky;

    if (translucency > 0.0h)
    {
        half edge = alpha * (1.0h - alpha) * 4.0h;
        half litAverage = litAccum * rcp(alpha);

        half rim = edge * PositivePow(towardSun, HB_CLOUD_RIM_FOCUS);
        half body = PositivePow(towardSun, HB_CLOUD_BODY_FOCUS);

        half silver = (rim + body) * (1.0h - litAverage * 0.6h)
                    * translucency * HB_CLOUD_SILVER_GAIN;

        colour = lerp(colour, max(colour, lightColour * alpha), saturate(silver));
    }

    colour *= horizon * _HB_CloudTint.rgb;
    alpha *= horizon;

    float3 cameraPositionWS = GetCameraPositionWS();
    float3 positionWS = cameraPositionWS + float3(direction) * (travel * HB_CLOUD_DISTANCE_SCALE);

    half fogged = HB_FogAmount(positionWS, cameraPositionWS);
    colour = lerp(colour, HB_FogColour(direction, 1.0h) * alpha, fogged);

    return colour + sky * (1.0h - alpha);
}

#endif
