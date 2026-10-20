using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleSky))]
    sealed class StyleSkyEditor : StyleVolumeEditor
    {
        const string Hint =
            "The hour of the day is set on the Style Sun of the scene's directional light, not " +
            "here. Sky, fog, clouds and the sun and moon light all follow it.";

        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(Hint, MessageType.None);

                var sun = HBStyleSun.active;

                if (GUILayout.Button(sun != null ? "Select Sun" : "Add Style Sun", GUILayout.Width(100f),
                        GUILayout.ExpandHeight(true)))
                {
                    if (sun == null)
                    {
                        var light = StyleSunInstaller.FindSun();

                        if (light != null)
                            sun = StyleSunInstaller.Add(light);
                    }

                    if (sun != null)
                        Selection.activeGameObject = sun.gameObject;
                }
            }

            Section("Sky", "daySky", "duskSky", "nightSky", "skyIntensity");
            Section("Brush", "skyBrush", "skyPaint", "skyBrushScale", "skyBrushSmoothness");
            Section("Ambient", "dayAmbient", "duskAmbient", "nightAmbient", "ambientIntensity",
                    "ambientLightBias", "sunsetFocus");
            Section("Sky Light", "skyLight", "skyLightIntensity", "moonLight", "sunDiscColor", "moonDiscColor");
            Section("Time of Day", "duskElevation", "nightElevation");
        }
    }

    [CustomEditor(typeof(HBStyleFog))]
    sealed class StyleFogEditor : StyleVolumeEditor
    {
        public override void OnInspectorGUI()
        {
            Section("Distance", "startDistance", "endDistance", "maxOpacity");
            Section("Height", "heightFalloff", "baseHeight");
            Section("Light", "sunScattering");
            Section("Brush", "paint", "silhouette");
            Section("Colour", "dayFog", "duskFog", "nightFog");
        }
    }

    [CustomEditor(typeof(HBStyleClouds))]
    sealed class StyleCloudsEditor : StyleVolumeEditor
    {
        public override void OnInspectorGUI()
        {
            Section("Shape", "coverage", "scale", "wind", "softness");
            Section("Colour", "tint", "tintShade");
            Section("Volume", "steps", "stepShading", "stepSpacing", "stepTaper");
            Section("Detail", "translucency");
        }
    }

    [CustomEditor(typeof(HBStyleCelestial))]
    sealed class StyleCelestialEditor : StyleVolumeEditor
    {
        public override void OnInspectorGUI()
        {
            Section("Sun", "sunIntensity", "sunSize", "sunGlow", "sunDuskIntensity", "sunDuskSize", "sunBrush",
                    "sunRays");
            Section("Moon", "moonIntensity", "moonSize", "moonGlow");
            Section("Stars", "stars", "starTwinkle");
        }
    }
}
