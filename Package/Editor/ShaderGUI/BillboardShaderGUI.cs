using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class BillboardShaderGUI : StyleShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Shading = new GUIContent("Shading");
            public static readonly GUIContent Motion = new GUIContent("Wind & Weather");
            public static readonly GUIContent Baked = new GUIContent("Baked");

            public const string Setup =
                "Made by Tools > HiddenBull > URP Style > Bake Billboard, or Bake Billboard on a LOD Group. " +
                "Baking again refreshes the atlases and the values below.";

            public static readonly GUIContent BaseMap = new GUIContent(
                "Albedo Atlas", "The tree seen from each baked direction, side by side.");

            public static readonly GUIContent NormalAtlas = new GUIContent(
                "Normal Atlas", "The tree's normals for each baked direction, so the billboard is lit like the mesh.");

            public static readonly GUIContent Cutoff = new GUIContent(
                "Alpha Cutoff", "Pixels less opaque than this are cut away.");

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap", "How softly light falls off toward the shadow side.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness", "Width of each step between tones.");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone", "Width of the middle tone between light and shadow.");

            public static readonly GUIContent WindStrength = new GUIContent(
                "Wind Strength", "How far the billboard sways with the weather's wind. Match it to the tree's leaves.");

            public static readonly GUIContent Wetness = new GUIContent(
                "Wetness",
                "How dark and glossy this surface gets at its wettest. Where and when it gets wet does not " +
                "change.");

            public static readonly GUIContent SnowCover = new GUIContent(
                "Snow Cover",
                "How much of this surface the snow can hide at its deepest. 1 turns it fully white; lower " +
                "values let the surface show through.");

            public static readonly GUIContent ReceiveWeather = new GUIContent(
                "Receive Weather", "Lets rain darken and snow cover the billboard like the tree itself.");

            public static readonly GUIContent Frames = new GUIContent(
                "Frames", "How many directions the tree was baked from.");

            public static readonly GUIContent Center = new GUIContent(
                "Center", "Middle of the tree in the object's own space.");

            public static readonly GUIContent Size = new GUIContent(
                "Size", "Width and height of the billboard in the object's own space.");
        }

        MaterialProperty m_BaseMap;
        MaterialProperty m_NormalAtlas;
        MaterialProperty m_BaseColor;
        MaterialProperty m_Cutoff;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_WindStrength;
        MaterialProperty m_ReceiveWeather;
        MaterialProperty m_Wetness;
        MaterialProperty m_SnowCover;
        MaterialProperty m_Frames;
        MaterialProperty m_Center;
        MaterialProperty m_Size;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            EditorGUILayout.HelpBox(Styles.Setup, MessageType.None);

            EditorGUI.BeginChangeCheck();

            DrawSurface(materialEditor);
            DrawShading(materialEditor);
            DrawMotion(materialEditor);
            DrawBaked(materialEditor);

            CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                ValidateTargets(materialEditor);
        }

        public override void ValidateMaterial(Material material)
        {
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_BaseMap = FindProperty("_BaseMap", properties);
            m_NormalAtlas = FindProperty("_NormalAtlas", properties);
            m_BaseColor = FindProperty("_BaseColor", properties);
            m_Cutoff = FindProperty("_Cutoff", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);
            m_HalfTone = FindProperty("_HalfTone", properties);
            m_WindStrength = FindProperty("_WindStrength", properties);
            m_ReceiveWeather = FindProperty("_ReceiveWeather", properties);
            m_Wetness = FindProperty("_Wetness", properties);
            m_SnowCover = FindProperty("_SnowCover", properties);
            m_Frames = FindProperty("_Frames", properties);
            m_Center = FindProperty("_BillboardCenter", properties);
            m_Size = FindProperty("_BillboardSize", properties);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BaseColor, m_Cutoff);
            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Billboard.Surface", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.TexturePropertySingleLine(Styles.BaseMap, m_BaseMap, m_BaseColor);
                materialEditor.TexturePropertySingleLine(Styles.NormalAtlas, m_NormalAtlas);
                materialEditor.ShaderProperty(m_Cutoff, Styles.Cutoff);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawShading(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DiffuseWrap, m_DiffuseSoftness, m_HalfTone);
            var expanded = StyleMaterialHeader.Draw(Styles.Shading, "Billboard.Shading", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
                materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
                materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawMotion(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_WindStrength, m_ReceiveWeather, m_Wetness, m_SnowCover);
            var expanded = StyleMaterialHeader.Draw(Styles.Motion, "Billboard.Motion", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_WindStrength, Styles.WindStrength);
                materialEditor.ShaderProperty(m_ReceiveWeather, Styles.ReceiveWeather);

                var off = !m_ReceiveWeather.hasMixedValue && m_ReceiveWeather.floatValue < 0.5f;

                using (new EditorGUI.DisabledScope(off))
                {
                    materialEditor.ShaderProperty(m_Wetness, Styles.Wetness);
                    materialEditor.ShaderProperty(m_SnowCover, Styles.SnowCover);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawBaked(MaterialEditor materialEditor)
        {
            var expanded = StyleMaterialHeader.Draw(Styles.Baked, "Billboard.Baked", materialEditor);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_Frames, Styles.Frames);
                materialEditor.ShaderProperty(m_Center, Styles.Center);
                materialEditor.ShaderProperty(m_Size, Styles.Size);
                materialEditor.EnableInstancingField();
            }

            StyleMaterialHeader.End(expanded);
        }
    }
}
