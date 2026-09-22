#ifndef HIDDENBULL_URPSTYLE_COMMON_INCLUDED
#define HIDDENBULL_URPSTYLE_COMMON_INCLUDED

// Shared constants and small helpers for the HiddenBull style library.
// Everything here is used by more than one of Ambient.hlsl / Lighting.hlsl; single-use helpers
// belong in the file that uses them.

#define HB_EPSILON 1e-4h

// Safe divide that never returns inf for a zero or negative denominator.
half HB_SafeDiv(half a, half b)
{
    return a * rcp(max(b, HB_EPSILON));
}

// Hermite smoothing of an already-normalised [0,1] value. Cheaper than smoothstep() when the
// caller has done the remap itself, which is the common case in this library.
half HB_Smooth01(half x)
{
    return x * x * (3.0h - 2.0h * x);
}

/// Per-material style parameters that sit alongside URP's SurfaceData.
///
/// Kept separate from SurfaceData rather than bolted onto it, so URP's own helpers (decals, meta
/// pass, BRDF initialisation) keep working with the struct they expect.
///
/// Declared here rather than in StyleLighting.hlsl so a shader's material input header can describe
/// its style parameters without dragging in the whole lighting library — the shadow, depth and meta
/// passes need the struct but never shade anything.
struct HiddenBullStyleData
{
    half diffuseWrap;      // How far light wraps past the terminator. 0 = Lambert.
    half diffuseSoftness;  // Width of the transition band. Near 0 = hard cut, high = smooth ramp.
    half shadowTerminator; // How far the shadow map is faded out near the terminator, to hide acne.
    half3 rimColor;        // Tint applied on top of the sky colour.
    half rimPower;         // Fresnel exponent. Higher = tighter edge.
    half rimIntensity;     // 0 disables the term's contribution.
};

HiddenBullStyleData HB_DefaultStyleData()
{
    HiddenBullStyleData style;
    style.diffuseWrap = 0.0h;
    style.diffuseSoftness = 1.0h;
    style.shadowTerminator = 0.15h;
    style.rimColor = half3(1.0h, 1.0h, 1.0h);
    style.rimPower = 4.0h;
    style.rimIntensity = 0.0h;
    return style;
}

#endif // HIDDENBULL_URPSTYLE_COMMON_INCLUDED
