#ifndef HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED
#define HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "StyleCommon.hlsl"
#include "StyleAmbient.hlsl"

// The HiddenBull lighting model (Architecture D2).
//
// This is the only place the lighting model is implemented. Every shader family in the package —
// lit, unlit, particle, vegetation, terrain — routes through HiddenBullFragmentLit below, so the
// style cannot drift between them and changing the look is a single-file edit. A shader that
// reimplements any of this is a bug.

// HiddenBullStyleData itself lives in StyleCommon.hlsl, so passes that need the struct but do no
// shading do not have to include this file.

/// Analytic wrapped diffuse (Architecture D14).
///
/// Two numbers shape the whole light-to-shadow transition, with no texture fetch and no keyword:
///
///   wrap     — pushes the terminator past 90°, widening the lit area and softening the falloff.
///              Wide reads as skin, narrow reads as rock.
///   softness — the width of the transition band around the terminator. Near zero gives a hard
///              cel-like cut; large values give a smooth gradient across the whole range.
///
/// The lit side flattening out at high wrap is intentional, not a limitation: in this style the
/// gradation on a lit surface comes from the ambient gradient (D10), not from the cosine falloff.
half HB_WrappedDiffuse(half NdotL, half wrap, half softness)
{
    half w = saturate(wrap);
    half invW = rcp(1.0h + w);

    // Zero where light no longer reaches (NdotL <= -wrap), one facing the light.
    half d = saturate((NdotL + w) * invW);

    // Where NdotL == 0 lands after that remap — the point the transition band centres on.
    half terminator = w * invW;

    half halfBand = 0.5h * max(softness, HB_EPSILON);

    // Clamped to the range d can actually take. Without the clamp a band reaching below zero
    // leaves the unlit side sitting at a grey floor instead of going dark, and — because most of
    // the band then falls outside the reachable range — moving `wrap` barely changes anything.
    half e0 = max(terminator - halfBand, 0.0h);
    half e1 = max(min(terminator + halfBand, 1.0h), e0 + HB_EPSILON);

    return smoothstep(e0, e1, d);
}

/// Shaped diffuse response for one light, including its attenuation and shadowing.
///
/// Additional lights go through the same function as the main light, so a torch and the sun shade
/// a surface with the same character.
half3 HB_LightResponse(Light light, half3 normalWS, HiddenBullStyleData style)
{
    half NdotL = dot(normalWS, light.direction);
    half shaped = HB_WrappedDiffuse(NdotL, style.diffuseWrap, style.diffuseSoftness);

    // Shadow maps produce acne where a surface is nearly edge-on to the light — which is exactly
    // where the terminator sits. Wrapped diffuse makes it worse, because it lights geometry the
    // shadow map already considers self-shadowed, so the two disagree in a noisy band.
    //
    // Fading the shadow term in as the surface turns toward the light hides that band behind the
    // diffuse falloff, which is already dark there. Phase 2 replaces this with the screen-space
    // mask, which resolves the terminator properly instead of hiding it.
    half shadowFade = smoothstep(0.0h, max(style.shadowTerminator, HB_EPSILON), NdotL);
    half shadowAttenuation = lerp(1.0h, light.shadowAttenuation, shadowFade);

    return light.color * (light.distanceAttenuation * shadowAttenuation * shaped);
}

/// Fresnel rim tinted by the sky (Architecture D15).
///
/// Multiplied by the sky colour rather than used raw, so edges pick up the environment and sit in
/// the scene instead of glowing. A white rimColor therefore means "pure sky colour"; any other
/// value tints away from it.
half3 HB_RimLight(half3 normalWS, half3 viewDirectionWS, HiddenBullStyleData style, half occlusion)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
    half rim = PositivePow(fresnel, max(style.rimPower, HB_EPSILON));
    return rim * style.rimIntensity * style.rimColor * HB_AmbientSkyColor() * occlusion;
}

/// The style's fragment lighting entry point. Mirrors the role of UniversalFragmentPBR.
half4 HiddenBullFragmentLit(InputData inputData, SurfaceData surfaceData, HiddenBullStyleData style)
{
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();

    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    half occlusion = surfaceData.occlusion * aoFactor.indirectAmbientOcclusion;

    // --- Indirect ------------------------------------------------------------------------------
    // The ambient slot: gradient, baked, or a blend of the two (D16).
    half3 ambient = HB_ResolveAmbient(inputData.normalWS, inputData.bakedGI) * occlusion;

#ifdef _HB_SPECULAR
    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    // Environment specular reuses the same gradient, sampled along the reflection vector, instead
    // of a reflection probe. Zero texture cost, and reflections cannot disagree with the ambient
    // they sit next to — which is the whole point of D10.
    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half3 environmentSpecular = HB_GradientAmbient(reflectVector) * occlusion;
    half fresnelTerm = Pow4(1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS)));

    half3 color = EnvironmentBRDF(brdfData, ambient, environmentSpecular, fresnelTerm);
#else
    half3 color = ambient * surfaceData.albedo;
#endif

    // --- Direct --------------------------------------------------------------------------------
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        half3 radiance = HB_LightResponse(mainLight, inputData.normalWS, style);

        color += radiance * surfaceData.albedo;
#ifdef _HB_SPECULAR
        // Specular is driven by the shaped term too, so highlights cannot spill past the stylized
        // terminator and break the silhouette the diffuse just established.
        color += radiance * brdfData.specular *
                 DirectBRDFSpecular(brdfData, inputData.normalWS, mainLight.direction, inputData.viewDirectionWS);
#endif
    }

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    // Forward+ keeps directional lights outside the cluster list, so they need their own sweep.
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            half3 radiance = HB_LightResponse(light, inputData.normalWS, style);

            color += radiance * surfaceData.albedo;
    #ifdef _HB_SPECULAR
            color += radiance * brdfData.specular *
                     DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);
    #endif
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            half3 radiance = HB_LightResponse(light, inputData.normalWS, style);

            color += radiance * surfaceData.albedo;
    #ifdef _HB_SPECULAR
            color += radiance * brdfData.specular *
                     DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);
    #endif
        }
    LIGHT_LOOP_END
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    color += inputData.vertexLighting * surfaceData.albedo;
#endif

    // --- Style terms ---------------------------------------------------------------------------
#ifdef _HB_RIM
    color += HB_RimLight(inputData.normalWS, inputData.viewDirectionWS, style, occlusion);
#endif

    color += surfaceData.emission;

    return half4(color, surfaceData.alpha);
}

#endif // HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED
