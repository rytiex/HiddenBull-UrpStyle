using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class FoliageShaderGUI : LitShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Leaves = new GUIContent("Leaves");
            public static readonly GUIContent Wind = new GUIContent("Wind");

            public static readonly string[] SourceNames = { "Height", "Vertex Color Red" };

            public static readonly GUIContent TranslucencyColor = new GUIContent(
                "Translucency Color",
                "Colour of sunlight coming through the leaves, mixed with the leaf's own colour. Like the " +
                "grass glow it shows once the sun sits low, strongest when looking towards it.");

            public static readonly GUIContent WindStrength = new GUIContent(
                "Strength",
                "How much this plant answers the Style Weather volume's wind. The direction and " +
                "force of the wind come from the volume, so everything sways together.");

            public static readonly GUIContent WindHeight = new GUIContent(
                "Plant Height",
                "Height of the plant in metres, measured up from its pivot. The base stays put and " +
                "the top moves the most, so the pivot should sit at the root.");

            public static readonly GUIContent WindBend = new GUIContent(
                "Sway", "How far the whole plant leans and rocks with the wind.");

            public static readonly GUIContent WindFlutter = new GUIContent(
                "Flutter", "Fast shimmer of the leaves.");

            public static readonly GUIContent WindSource = new GUIContent(
                "Flutter Mask",
                "Height makes the upper part flutter more. Vertex Color Red lets the mesh say which " +
                "parts are loose leaves (red) and which are trunk and branches (black).");
        }

        MaterialProperty m_TranslucencyColor;
        MaterialProperty m_WindStrength;
        MaterialProperty m_WindHeight;
        MaterialProperty m_WindBend;
        MaterialProperty m_WindFlutter;
        MaterialProperty m_WindSource;

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            material.enableInstancing = true;
        }

        protected override void FindExtraProperties(MaterialProperty[] properties)
        {
            m_TranslucencyColor = FindProperty("_TranslucencyColor", properties);
            m_WindStrength = FindProperty("_WindStrength", properties);
            m_WindHeight = FindProperty("_WindHeight", properties);
            m_WindBend = FindProperty("_WindBend", properties);
            m_WindFlutter = FindProperty("_WindFlutter", properties);
            m_WindSource = FindProperty("_WindSource", properties);
        }

        protected override void DrawExtraSections(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_TranslucencyColor);
            var expanded = StyleMaterialHeader.Draw(Styles.Leaves, "Foliage.Leaves", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_TranslucencyColor, Styles.TranslucencyColor);
            }

            StyleMaterialHeader.End(expanded);

            reset = ResetAction(materialEditor, m_WindStrength, m_WindHeight, m_WindBend, m_WindFlutter,
                                m_WindSource);
            expanded = StyleMaterialHeader.Draw(Styles.Wind, "Foliage.Wind", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_WindStrength, Styles.WindStrength);
                materialEditor.ShaderProperty(m_WindHeight, Styles.WindHeight);
                materialEditor.ShaderProperty(m_WindBend, Styles.WindBend);
                materialEditor.ShaderProperty(m_WindFlutter, Styles.WindFlutter);
                DrawPopup(materialEditor, m_WindSource, Styles.WindSource, Styles.SourceNames);
            }

            StyleMaterialHeader.End(expanded);
        }
    }
}
