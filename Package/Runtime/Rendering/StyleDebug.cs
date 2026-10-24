using UnityEngine;

namespace HiddenBull.UrpStyle
{
    enum StyleDebugView
    {
        Off = 0,
        [InspectorName("Shadows/Attenuation")] ShadowAttenuation = 1,
        [InspectorName("Shadows/Unfiltered")] ShadowUnfiltered = 2,
        [InspectorName("Shadows/Blockers")] ShadowBlockers = 3,
        [InspectorName("Shadows/Cascades")] ShadowCascades = 4,
        [InspectorName("Shadows/Filter Values")] ShadowFilter = 5,
        [InspectorName("Shadows/Search Radius")] ShadowRadius = 6,
        [InspectorName("Atmosphere/Fog Amount")] FogAmount = 7
    }

    static class StyleDebug
    {
        internal const string Keyword = "HB_STYLE_DEBUG";

        internal static StyleDebugView view;
    }
}
