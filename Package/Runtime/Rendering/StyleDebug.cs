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
        [InspectorName("Atmosphere/Fog Amount")] FogAmount = 7,
        [InspectorName("Weather/Shelter")] WeatherShelter = 8,
        [InspectorName("Weather/Wetness")] WeatherWetness = 9,
        [InspectorName("Weather/Snow")] WeatherSnow = 10,
        [InspectorName("Weather/Precipitation")] WeatherPrecipitation = 11,
        [InspectorName("Terrain/Detail Rings")] TerrainRings = 12,
        [InspectorName("Terrain/Layers")] TerrainLayers = 13,
        [InspectorName("Terrain/Curvature")] TerrainCurvature = 14,
        [InspectorName("Grass/Density")] GrassDensity = 15
    }

    static class StyleDebug
    {
        internal const string Keyword = "HB_STYLE_DEBUG";

        internal static StyleDebugView view;

        internal static bool previewShelter;
        internal static bool previewGameCamera;
        internal static Texture shelterPreview;

        internal static int rainDrops;
        internal static int snowFlakes;

        internal static int terrainPatches;
        internal static double terrainSelectMs;

        internal static bool IsPreviewCamera(Camera camera)
        {
            return camera.cameraType == (previewGameCamera ? CameraType.Game : CameraType.SceneView);
        }
    }
}
