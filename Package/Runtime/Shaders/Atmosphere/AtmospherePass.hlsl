#ifndef HB_ATMOSPHERE_PASS_INCLUDED
#define HB_ATMOSPHERE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Atmosphere/StyleAtmosphere.hlsl"

struct HBAtmosphereVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
};

HBAtmosphereVaryings HB_AtmosphereVertex(uint vertexID : SV_VertexID)
{
    HBAtmosphereVaryings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    output.uv = GetFullScreenTriangleTexCoord(vertexID);
    return output;
}

float HB_AtmosphereDepth(float2 uv)
{
    return SampleSceneDepth(uv);
}

bool HB_AtmosphereSurface(float depth)
{
#if UNITY_REVERSED_Z
    return depth > 0.0;
#else
    return depth < 1.0;
#endif
}

HBAtmosphere HB_SilhouetteAtmosphere(float3 positionWS, float3 cameraPositionWS, float span, float tileAngle,
                                     HBAtmosphere atmosphere, inout half3 direction)
{
    half strength = half(HB_FOG_SILHOUETTE);

    UNITY_BRANCH
    if (HB_BRUSH_ATLAS_BOUND > 0.5h && strength > 0.0h)
    {
        half4 stroke = HB_FogWorldStroke(positionWS, HB_TriplanarBlend(direction), span / HB_FOG_SILHOUETTE_TILES,
                                         1.0);
        float2 spine = float2(stroke.rg * 2.0h - 1.0h)
                     * (HB_BRUSH_SPINE_RANGE * strength * tileAngle / HB_FOG_SILHOUETTE_TILES);
        float3 painted = float3(direction) + UNITY_MATRIX_V[0].xyz * spine.x + UNITY_MATRIX_V[1].xyz * spine.y;
        float2 paintedUV = saturate(ComputeNormalizedDeviceCoordinates(cameraPositionWS + painted, UNITY_MATRIX_VP));
        float paintedDepth = HB_AtmosphereDepth(paintedUV);

        HBAtmosphere other;
        other.horizon = 0.0h;

        if (HB_AtmosphereSurface(paintedDepth))
        {
            float3 paintedWS = ComputeWorldSpacePosition(paintedUV, paintedDepth, UNITY_MATRIX_I_VP);
            other = HB_AtmosphereAmount(paintedWS, cameraPositionWS);
        }
        else
        {
            other.fog = HB_SkyHaze(half3(normalize(painted)));
        }

        if (other.fog > atmosphere.fog)
        {
            atmosphere.fog = other.fog;
            direction = half3(normalize(painted));
        }

        atmosphere.horizon = max(atmosphere.horizon, other.horizon);
    }

    return atmosphere;
}

half4 HB_AtmosphereFragment(HBAtmosphereVaryings input) : SV_Target
{
    float depth = HB_AtmosphereDepth(input.uv);

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW != 0 && HB_DEBUG_VIEW != HB_DEBUG_FOG_AMOUNT)
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
#endif

    if (!HB_AtmosphereSurface(depth))
        return half4(0.0h, 0.0h, 0.0h, 0.0h);

    float3 positionWS = ComputeWorldSpacePosition(input.uv, depth, UNITY_MATRIX_I_VP);
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 ray = positionWS - cameraPositionWS;
    float rayLength = max(length(ray), 1e-4);
    half3 direction = half3(ray / rayLength);
    float tileAngle = HB_FogTileAngle();
    float span = rayLength * tileAngle;

    HBAtmosphere atmosphere = HB_AtmosphereAmount(positionWS, cameraPositionWS);
    half3 view = direction;

    atmosphere = HB_SilhouetteAtmosphere(positionWS, cameraPositionWS, span, tileAngle, atmosphere, direction);

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_FOG_AMOUNT)
        return half4(atmosphere.fog, atmosphere.horizon, 0.0h, 1.0h);
#endif

    half fog = atmosphere.fog;
    half horizon = atmosphere.horizon;
    half4 result = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (fog + horizon > 0.0h)
    {
        half4 atlas;
        half paint = HB_FogPaintStroke(positionWS, direction, span, fog, atlas);
        half3 fogColour = HB_FogColourPainted(direction, atlas, paint);
        half3 horizonColour = HB_SkyGradient(view);

        result.rgb = horizonColour * (horizon * (1.0h - fog)) + fogColour * fog;
        result.a = 1.0h - (1.0h - horizon) * (1.0h - fog);
    }

    return result;
}

#endif
