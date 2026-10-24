#ifndef HB_STYLE_CLOUDS_INCLUDED
#define HB_STYLE_CLOUDS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Atmosphere/StyleAtmosphere.hlsl"

TEXTURE2D(_HB_CloudAtlas);
SAMPLER(sampler_HB_CloudAtlas);

#define HB_CLOUD_PLANET_RADIUS  200.0
#define HB_CLOUD_LAYER_HEIGHT   20.0
#define HB_CLOUD_DISTANCE_SCALE 100.0
#define HB_CLOUD_RELIEF_UP      0.45h
#define HB_CLOUD_SHADE_FLOOR    0.85h
#define HB_CLOUD_SILVER_GAIN    2.0h
#define HB_CLOUD_RIM_FOCUS      12.0h
#define HB_CLOUD_BODY_FOCUS     40.0h
#define HB_CLOUD_ABSORPTION     3.0h
#define HB_CLOUD_POWDER_DEPTH   0.6h

#define HB_CLOUD_SILVER_LINING  _HB_CloudLook.x
#define HB_CLOUD_SUN_THROUGH    _HB_CloudLook.y
#define HB_CLOUD_POWDER         _HB_CloudLook.z

float HB_CloudTravel(float up, float layerHeight)
{
    float b = HB_CLOUD_PLANET_RADIUS * up;
    return -b + sqrt(b * b + layerHeight * (2.0 * HB_CLOUD_PLANET_RADIUS + layerHeight));
}

half HB_CloudWrappedDiffuse(half NdotL, half wrap, half softness)
{
    half invW = rcp(1.0h + wrap);
    half d = saturate((NdotL + wrap) * invW);
    half terminator = wrap * invW;
    half halfBand = 0.5h * max(softness, half(HB_EPSILON));
    half e0 = max(terminator - halfBand, 0.0h);
    half e1 = max(min(terminator + halfBand, 1.0h), e0 + half(HB_EPSILON));

    return smoothstep(e0, e1, d);
}

half4 HB_Clouds(half3 direction)
{
    float scale = _HB_CloudParams.y;
    float2 drift = _HB_CloudMotion.xy * _Time.y;

    int steps = max((int)_HB_CloudParams.w, 1);
    half spread = half(rsqrt((float)steps));

    half3 lightColour = lerp(half3(_HB_CloudAwayLight.rgb), half3(_HB_CloudLight.rgb), HB_SunFacing(direction));
    half3 lightDirection = half3(_HB_CloudDirection.xyz);

    half3 shade = HB_SkySample(direction, direction.y) * HB_CLOUD_SHADE_FLOOR;

    half soft = max(half(_HB_CloudParams.z), half(HB_EPSILON));
    half shading = half(_HB_CloudSlab.z);
    half absorption = shading * HB_CLOUD_ABSORPTION * spread;
    half towardSun = saturate(dot(direction, lightDirection));

    float travel = HB_CloudTravel(direction.y, HB_CLOUD_LAYER_HEIGHT);
    float2 reference = direction.xz * (travel * scale);
    float2 slab = direction.xz * (_HB_CloudSlab.x * spread);
    float2 referenceDx = ddx(reference);
    float2 referenceDy = ddy(reference);
    float2 slabDx = ddx(slab);
    float2 slabDy = ddy(slab);

    half middle = (steps - 1) * 0.5h;
    half taper = half(_HB_CloudSlab.y) * rcp(max(middle, 1.0h));
    half shellSoft = max(soft + taper * 0.6h, half(HB_EPSILON));

    half coverage = half(_HB_CloudParams.x);
    half threshold = (1.0h - coverage) - coverage * (shellSoft + taper * middle);

    half3 colour = half3(0.0h, 0.0h, 0.0h);
    half alpha = 0.0h;
    half litAccum = 0.0h;
    half transmittance = 1.0h;

    UNITY_LOOP
    for (int i = steps - 1; i >= 0; i--)
    {
        half4 atlas = SAMPLE_TEXTURE2D_GRAD(_HB_CloudAtlas, sampler_HB_CloudAtlas, reference + slab * i + drift,
                                            referenceDx + slabDx * i, referenceDy + slabDy * i);

        half cutoff = threshold + abs((half)i - middle) * taper;
        half shell = smoothstep(cutoff - shellSoft, cutoff + shellSoft, atlas.a);

        half2 relief = atlas.rg * 2.0h - 1.0h;
        half3 normal = normalize(half3(relief.x, HB_CLOUD_RELIEF_UP, relief.y));
        half shaped = HB_CloudWrappedDiffuse(dot(normal, lightDirection), 0.5h, 0.6h);
        half lit = lerp(1.0h, shaped, shading) * transmittance;
        half3 shellColour = lerp(shade, max(shade, lightColour), lit);

        colour = lerp(colour, shellColour, shell);
        litAccum = lerp(litAccum, lit, shell);
        alpha = shell + alpha * (1.0h - shell);

        transmittance *= exp(-atlas.a * absorption);
    }

    UNITY_BRANCH
    if (alpha > 0.0h)
    {
        half litAverage = litAccum * rcp(alpha);
        half edge = alpha * (1.0h - alpha) * 4.0h;
        half shadowed = 1.0h - litAverage * 0.6h;

        half silver = edge * PositivePow(towardSun, HB_CLOUD_RIM_FOCUS) * shadowed * half(HB_CLOUD_SILVER_LINING);
        half through = PositivePow(towardSun, HB_CLOUD_BODY_FOCUS) * (0.35h + 0.65h * transmittance) * shadowed
                     * half(HB_CLOUD_SUN_THROUGH);

        colour = lerp(colour, max(colour, lightColour * alpha), saturate((silver + through) * HB_CLOUD_SILVER_GAIN));
        colour *= lerp(1.0h, 1.0h - edge * HB_CLOUD_POWDER_DEPTH, half(HB_CLOUD_POWDER) * (1.0h - towardSun));

        half darkness = saturate((1.0h - litAverage) * rcp(max(shading, half(HB_EPSILON))));
        half weight = lerp(1.0h, darkness, half(_HB_CloudTint.a) * saturate(shading * 8.0h));

        colour *= lerp(half3(1.0h, 1.0h, 1.0h), half3(_HB_CloudTint.rgb), weight);

        float3 cameraPositionWS = GetCameraPositionWS();
        float3 positionWS = cameraPositionWS + float3(direction) * (travel * HB_CLOUD_DISTANCE_SCALE);
        half fogged = HB_FogAmount(positionWS, cameraPositionWS);

        colour = lerp(colour, HB_FogColour(direction) * alpha, fogged);
    }

    return half4(colour, alpha);
}

half3 HB_SkyWithClouds(half3 direction)
{
    half3 sky = HB_AtmosphereSky(HB_SkyColour(direction), direction);
    half horizon = smoothstep(0.0h, 0.03h, direction.y);

    UNITY_BRANCH
    if (_HB_CloudParams.x > 0.0 && horizon > 0.0h)
    {
        half4 clouds = HB_Clouds(direction) * horizon;

        sky = clouds.rgb + sky * (1.0h - clouds.a);
    }

    return sky;
}

#endif
