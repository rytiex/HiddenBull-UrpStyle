#ifndef HIDDENBULL_URPSTYLE_TERRAIN_SKY_DEPTH_INCLUDED
#define HIDDENBULL_URPSTYLE_TERRAIN_SKY_DEPTH_INCLUDED

#ifndef HB_SKY_PASS_FLAG
#define HB_SKY_PASS_FLAG
float _HB_SkyOcclusionPass;
#endif

VaryingsLean HiddenBullTerrainDepthVertex(AttributesLean input)
{
    VaryingsLean output = DepthOnlyVertex(input);

    if (_HB_SkyOcclusionPass > 0.5)
        output.clipPos = float4(0.0, 0.0, 0.0, 0.0);

    return output;
}

#endif
