#ifndef HB_SKY_MAP_PASS_INCLUDED
#define HB_SKY_MAP_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleClouds.hlsl"

struct HBSkyMapVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
};

HBSkyMapVaryings HB_SkyMapVertex(uint vertexID : SV_VertexID)
{
    HBSkyMapVaryings output;

    output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    output.uv = GetFullScreenTriangleTexCoord(vertexID);

    return output;
}

half4 HB_SkyMapFragment(HBSkyMapVaryings input) : SV_Target
{
    float3 folded = UnpackNormalOctQuadEncode(input.uv * 2.0 - 1.0);

    return half4(HB_SkyWithClouds(half3(normalize(folded.xzy))), 1.0h);
}

#endif
