using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleLiquidLayer))]
    [CanEditMultipleObjects]
    sealed class StyleLiquidLayerEditor : UnityEditor.Editor
    {
        const string FoldKey = "HiddenBull.UrpStyle.Liquid.Layer.";
        const string NormalMapField = "m_NormalMap";

        static readonly Color s_WaveTint = new Color(0.35f, 0.7f, 0.95f);
        static readonly Color s_ColourTint = new Color(0.3f, 0.85f, 0.8f);
        static readonly Color s_FoamTint = new Color(0.85f, 0.9f, 0.95f);
        static readonly Color s_CrustTint = new Color(0.95f, 0.5f, 0.2f);
        static readonly Color s_UnderTint = new Color(0.3f, 0.45f, 0.85f);

        static readonly string[] s_WaveFields =
        {
            "m_WindWaves", "m_Waves", "m_WaveHeight", "m_WaveSteepness", "m_WaveLength"
        };

        static readonly string[] s_SurfaceFields =
        {
            "m_Tiling", "m_NormalMap", "m_NormalStrength", "m_RippleDrift", "m_Smoothness", "m_SurfaceDistortion",
            "m_RefractionBlur",
            "m_Brush", "m_BrushSize", "m_RainRipples"
        };

        static readonly string[] s_ColourFields =
        {
            "m_DepthGradient", "m_Glow", "m_Caustics", "m_CausticScale", "m_CrestColor", "m_CrestSoftness"
        };

        static readonly string[] s_FoamFields =
        {
            "m_FoamColor", "m_WaterlineDepth", "m_FoamAmount", "m_ShoreFoam", "m_Bubbles", "m_Pulse", "m_PatternScale"
        };

        static readonly string[] s_CrustFields = { "m_Crust", "m_CrustColor", "m_DiffuseWrap", "m_DiffuseSoftness" };

        static readonly string[] s_UnderFields =
        {
            "m_LightShafts", "m_UnderBubbles", "m_Distortion", "m_SnellWindow", "m_UnderBlur", "m_UnderVisibility",
            "m_UnderLightDepth"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawSections(serializedObject, false);
            serializedObject.ApplyModifiedProperties();
        }

        internal static void DrawSections(SerializedObject layer, bool studio)
        {
            if (StyleTerrainGUI.Section("Waves", s_WaveTint, FoldKey + "Waves", true))
                Fields(layer, s_WaveFields, studio);

            if (StyleTerrainGUI.Section("Surface", StyleTerrainGUI.settingsTint, FoldKey + "Surface", true))
                Fields(layer, s_SurfaceFields, studio);

            if (StyleTerrainGUI.Section("Colour", s_ColourTint, FoldKey + "Colour", true))
                Fields(layer, s_ColourFields, studio);

            if (StyleTerrainGUI.Section("Foam And Bubbles", s_FoamTint, FoldKey + "Foam", true))
                Fields(layer, s_FoamFields, studio);

            if (StyleTerrainGUI.Section("Crust", s_CrustTint, FoldKey + "Crust", false))
                Fields(layer, s_CrustFields, studio);

            if (StyleTerrainGUI.Section("Underwater", s_UnderTint, FoldKey + "Underwater", false))
                Fields(layer, s_UnderFields, studio);
        }

        static void Fields(SerializedObject layer, string[] names, bool studio)
        {
            foreach (var field in names)
            {
                if (studio && field == NormalMapField)
                    continue;

                EditorGUILayout.PropertyField(layer.FindProperty(field));
            }
        }
    }
}
