#ifndef HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED
#define HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED

#include "StyleCommon.hlsl"

// Three-zone gradient ambient (Architecture D12).
//
// In this style the ambient model is not decoration on top of the lighting model — it *is* the
// lighting model (D10). A flat, untextured surface gets all of its gradation from here: faces
// pointing up take the sky colour, faces pointing down take the ground colour, and faces near
// horizontal take the horizon band. One flat colour therefore reads as three tones without a
// single texture fetch.
//
// A two-colour hemisphere would be cheaper but cannot produce the warm horizon band, and that band
// is what makes a backlit silhouette read.
//
// These are global constants written once per camera by HiddenBullStyleFeature, not per-material,
// so they live outside the UnityPerMaterial CBUFFER and do not affect SRP Batcher compatibility.

float4 _HB_AmbientSky;      // rgb: sky zone colour
float4 _HB_AmbientHorizon;  // rgb: horizon band colour
float4 _HB_AmbientGround;   // rgb: ground zone colour
float4 _HB_AmbientParams;   // x: sky falloff, y: ground falloff, z: intensity, w: baked weight

#define HB_AMBIENT_SKY_FALLOFF   _HB_AmbientParams.x
#define HB_AMBIENT_GROUND_FALLOFF _HB_AmbientParams.y
#define HB_AMBIENT_INTENSITY     _HB_AmbientParams.z
#define HB_AMBIENT_BAKED_WEIGHT  _HB_AmbientParams.w

/// Evaluates the gradient along a direction — the surface normal, or once Phase 3 lands, the bent
/// normal, so that occlusion takes on the environment's colour rather than a grey multiply (D6).
half3 HB_GradientAmbient(half3 directionWS)
{
    half t = directionWS.y;

    // Only one of these is ever non-zero, so the two blends below stay mutually exclusive and the
    // whole thing remains branchless.
    half up = HB_Smooth01(saturate(HB_SafeDiv(t, HB_AMBIENT_SKY_FALLOFF)));
    half down = HB_Smooth01(saturate(HB_SafeDiv(-t, HB_AMBIENT_GROUND_FALLOFF)));

    half3 color = _HB_AmbientHorizon.rgb;
    color += (_HB_AmbientSky.rgb - _HB_AmbientHorizon.rgb) * up;
    color += (_HB_AmbientGround.rgb - _HB_AmbientHorizon.rgb) * down;

    return color * HB_AMBIENT_INTENSITY;
}

/// The pluggable ambient slot (Architecture D16).
///
/// The gradient is one provider of indirect light; Unity's baked lightmaps and probes are another.
/// Both arrive here, so whichever is in use, everything downstream — and in Phase 3 the bent-normal
/// colouring — behaves identically. Exteriors can run on the gradient alone; interiors can bake
/// their indirect bounce and blend it in.
///
/// Direct light never passes through this function. It stays realtime so it keeps the style's
/// wrapped diffuse and stylized shadows, which baked irradiance could not reproduce.
half3 HB_ResolveAmbient(half3 normalWS, half3 bakedGI)
{
    return lerp(HB_GradientAmbient(normalWS), bakedGI, HB_AMBIENT_BAKED_WEIGHT);
}

/// Sky zone colour on its own. Used to tint rim light so silhouettes pick up the environment
/// rather than glowing in an arbitrary colour.
half3 HB_AmbientSkyColor()
{
    return _HB_AmbientSky.rgb * HB_AMBIENT_INTENSITY;
}

#endif // HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED
