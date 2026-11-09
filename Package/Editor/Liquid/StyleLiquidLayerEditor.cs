using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleLiquidLayer))]
    [CanEditMultipleObjects]
    sealed class StyleLiquidLayerEditor : UnityEditor.Editor
    {
        const string FoldKey = "HiddenBull.UrpStyle.Liquid.Layer.";

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
            "m_Tiling", "m_NormalMap", "m_NormalStrength", "m_Smoothness", "m_RefractionBlur", "m_Brush",
            "m_BrushSize", "m_RainRipples"
        };

        static readonly string[] s_ColourFields =
        {
            "m_DepthGradient", "m_Glow", "m_Caustics", "m_CausticScale", "m_CrestColor", "m_CrestSoftness"
        };

        static readonly string[] s_FoamFields =
        {
            "m_FoamColor", "m_WaterlineDepth", "m_FoamAmount", "m_ShoreFoam", "m_Bubbles", "m_Pulse", "m_PatternScale"
        };

        static readonly string[] s_CrustFields = { "m_Crust", "m_CrustColor", "m_CrustMap", "m_DiffuseWrap",
                                                   "m_DiffuseSoftness" };

        static readonly string[] s_UnderFields =
        {
            "m_LightShafts", "m_UnderBubbles", "m_Distortion", "m_UnderVisibility", "m_UnderLightDepth"
        };

        static readonly GUIContent[] s_Presets =
        {
            new GUIContent("Water", "Sets every setting to clear water. The crust map is kept."),
            new GUIContent("Acid", "Sets every setting to a glowing, bubbling acid. The crust map is kept."),
            new GUIContent("Lava", "Sets every setting to glowing melt under a drifting crust. The crust map is kept.")
        };

        public override void OnInspectorGUI()
        {
            DrawPresets();
            serializedObject.Update();

            if (StyleTerrainGUI.Section("Waves", s_WaveTint, FoldKey + "Waves", true))
                Fields(s_WaveFields);

            if (StyleTerrainGUI.Section("Surface", StyleTerrainGUI.settingsTint, FoldKey + "Surface", true))
                Fields(s_SurfaceFields);

            if (StyleTerrainGUI.Section("Colour", s_ColourTint, FoldKey + "Colour", true))
                Fields(s_ColourFields);

            if (StyleTerrainGUI.Section("Foam And Bubbles", s_FoamTint, FoldKey + "Foam", true))
                Fields(s_FoamFields);

            if (StyleTerrainGUI.Section("Crust", s_CrustTint, FoldKey + "Crust", false))
                Fields(s_CrustFields);

            if (StyleTerrainGUI.Section("Underwater", s_UnderTint, FoldKey + "Underwater", false))
                Fields(s_UnderFields);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPresets()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Preset");

                for (var index = 0; index < s_Presets.Length; index++)
                {
                    var style = index == 0 ? EditorStyles.miniButtonLeft
                              : index == s_Presets.Length - 1 ? EditorStyles.miniButtonRight
                              : EditorStyles.miniButtonMid;

                    if (!GUILayout.Button(s_Presets[index], style))
                        continue;

                    Undo.RecordObjects(targets, s_Presets[index].text + " Preset");

                    foreach (var picked in targets)
                    {
                        ((HBStyleLiquidLayer)picked).Apply((HBStyleLiquidLayer.Preset)index);
                        EditorUtility.SetDirty(picked);
                    }

                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.Space(2f);
        }

        void Fields(string[] names)
        {
            foreach (var field in names)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
        }
    }
}
