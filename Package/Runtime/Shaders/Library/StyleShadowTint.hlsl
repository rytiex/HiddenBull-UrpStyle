#ifndef HIDDENBULL_URPSTYLE_SHADOW_TINT_INCLUDED
#define HIDDENBULL_URPSTYLE_SHADOW_TINT_INCLUDED

#define HB_SHADOW_TINT_CASCADES 4

TEXTURE2D(_HB_ShadowTint);
TEXTURE2D_FLOAT(_HB_ShadowTintDepth);

float4x4 _HB_ShadowTintMatrices[HB_SHADOW_TINT_CASCADES];
float4 _HB_ShadowTintSpheres[HB_SHADOW_TINT_CASCADES];
float4 _HB_ShadowTintBias;
float4 _HB_ShadowTintParams;

#define HB_SHADOW_TINT_COUNT _HB_ShadowTintParams.x

half4 HB_ShadowTint(float3 positionWS)
{
    int count = int(HB_SHADOW_TINT_COUNT);
    half4 tint = half4(1.0h, 1.0h, 1.0h, 1.0h);

    UNITY_BRANCH
    if (count > 0)
    {
        int cascade = HB_SHADOW_TINT_CASCADES;

        UNITY_UNROLL
        for (int i = HB_SHADOW_TINT_CASCADES - 1; i >= 0; i--)
        {
            float3 offset = positionWS - _HB_ShadowTintSpheres[i].xyz;

            if (i < count && dot(offset, offset) < _HB_ShadowTintSpheres[i].w)
                cascade = i;
        }

        UNITY_BRANCH
        if (cascade < count)
        {
            float3 coord = mul(_HB_ShadowTintMatrices[cascade], float4(positionWS, 1.0)).xyz;
            float bias = dot(_HB_ShadowTintBias, float4(cascade == 0, cascade == 1, cascade == 2, cascade == 3));

            float caster = SAMPLE_TEXTURE2D_LOD(_HB_ShadowTintDepth, sampler_PointClamp, coord.xy, 0).r;

        #if UNITY_REVERSED_Z
            bool behind = coord.z < caster - bias;
        #else
            bool behind = coord.z > caster + bias;
        #endif

            if (behind)
                tint = SAMPLE_TEXTURE2D_LOD(_HB_ShadowTint, sampler_LinearClamp, coord.xy, 0);
        }
    }

    return tint;
}

#endif
