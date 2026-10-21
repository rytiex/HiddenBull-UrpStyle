#ifndef HIDDENBULL_URPSTYLE_CANOPY_INCLUDED
#define HIDDENBULL_URPSTYLE_CANOPY_INCLUDED

#define HB_CANOPY_CENTRE 0.6

// Normal of an egg fitted to the plant's bounds, widest at the canopy centre. Unlike directions from
// a single centre point, the sides of a tall bush keep facing outwards along its whole height
// instead of turning towards the ground below the centre. Foliage and the billboard bake share it
// so a billboard lights like the plant it replaces.
float3 HB_CanopyNormal(float3 positionWS, float3 boundsMin, float3 boundsMax)
{
    float3 size = max(boundsMax - boundsMin, 1e-3);
    float3 centre = float3(boundsMin.x + 0.5 * size.x, boundsMin.y + HB_CANOPY_CENTRE * size.y,
                           boundsMin.z + 0.5 * size.z);
    float3 offset = positionWS - centre;

    // Below the centre the egg is at least as tall as it is wide, so the lower half of a long, low hedge faces
    // outwards instead of down. Plants no wider than 1.2x their height are unchanged.
    float below = max(HB_CANOPY_CENTRE * size.y, 0.5 * max(size.x, size.z));
    float3 radius = float3(0.5 * size.x, offset.y > 0.0 ? (1.0 - HB_CANOPY_CENTRE) * size.y : below, 0.5 * size.z);

    return SafeNormalize(offset / (radius * radius));
}

#endif
