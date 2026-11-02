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
        [InspectorName("Grass/Density")] GrassDensity = 15,
        [InspectorName("Grass/Cells")] GrassCells = 16,
        [InspectorName("Weather/Gust")] WeatherGust = 17,
        [InspectorName("Foliage/Canopy")] FoliageCanopy = 18,
        [InspectorName("Foliage/Sway")] FoliageSway = 19,
        [InspectorName("Culling/Game Camera")] CullingGameCamera = 20,
        [InspectorName("Transparency/Opacity")] TransparencyOpacity = 21,
        [InspectorName("Transparency/Tint")] TransparencyTint = 22,
        [InspectorName("Transparency/Added Light")] TransparencyLight = 23,
        [InspectorName("Transparency/Refraction")] TransparencyRefraction = 24
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

        internal static bool hidesGrass => view is StyleDebugView.WeatherShelter or StyleDebugView.WeatherWetness or
            StyleDebugView.WeatherSnow or StyleDebugView.TerrainRings or StyleDebugView.TerrainLayers or
            StyleDebugView.TerrainCurvature or StyleDebugView.GrassDensity or StyleDebugView.FoliageCanopy or
            StyleDebugView.FoliageSway;

        internal static Camera Viewer(Camera camera)
        {
            if (view != StyleDebugView.CullingGameCamera || camera.cameraType != CameraType.SceneView)
                return camera;

            var game = StyleCameraStates.lastGameCamera;

            return game != null ? game : camera;
        }

        internal static bool IsPreviewCamera(Camera camera)
        {
            return camera.cameraType == (previewGameCamera ? CameraType.Game : CameraType.SceneView);
        }
    }
}
