using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleDecalShaderGUI : StyleShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Brush = new GUIContent("Brush");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");

            public const string Setup =
                "Place with a URP Decal Projector. The renderer needs URP's Decal renderer feature with its " +
                "Technique set to DBuffer. The decal is painted into the surfaces under it and lit by them, so " +
                "it takes their brush strokes, shadows, snow and wetness.";

            public static readonly GUIContent BaseMap = new GUIContent("Base Map");

            public static readonly GUIContent AffectNormal = new GUIContent(
                "Affect Normal", "Lets the decal's normal map reshape the light on the surface.");

            public static readonly GUIContent AffectMAOS = new GUIContent(
                "Affect Surface",
                "Replaces the surface's metallic and smoothness with the decal's: a wet patch or a worn, " +
                "polished spot.");

            public static readonly GUIContent NormalMap = new GUIContent("Normal Map");

            public static readonly GUIContent NormalBlend = new GUIContent(
                "Normal Opacity", "How far the decal's normals replace the surface's.");

            public static readonly GUIContent Metallic = new GUIContent("Metallic");
            public static readonly GUIContent Smoothness = new GUIContent("Smoothness");

            public static readonly GUIContent Wear = new GUIContent(
                "Edge Wear",
                "Eats the decal's border away along the brush strokes of the surfaces around it, so it looks " +
                "painted on, not stuck on. 0 keeps the border straight.");

            public static readonly GUIContent BrushScale = new GUIContent(
                "Brush Size",
                "Stroke size relative to the project's. Near 1 the decal's wear follows the same strokes as " +
                "the surface under it.");

            public static readonly GUIContent StrokeAlpha = new GUIContent(
                "Stroke Alpha", "How far the strokes break up the decal's soft, partly transparent areas.");

            public static readonly GUIContent DrawOrder = new GUIContent(
                "Draw Order", "Decals with a higher draw order are painted over those with a lower one.");
        }

        MaterialProperty m_BaseMap;
        MaterialProperty m_BaseColor;
        MaterialProperty m_BumpMap;
        MaterialProperty m_BumpScale;
        MaterialProperty m_NormalBlend;
        MaterialProperty m_Metallic;
        MaterialProperty m_Smoothness;
        MaterialProperty m_AffectNormal;
        MaterialProperty m_AffectMAOS;
        MaterialProperty m_Wear;
        MaterialProperty m_BrushScale;
        MaterialProperty m_StrokeAlpha;
        MaterialProperty m_DrawOrder;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            EditorGUILayout.HelpBox(Styles.Setup, MessageType.None);

            DrawSurface(materialEditor);
            DrawBrush(materialEditor);
            DrawAdvanced(materialEditor);

            CoreEditorUtils.DrawSplitter();
        }

        public override void ValidateMaterial(Material material)
        {
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_BaseMap = FindProperty("_BaseMap", properties);
            m_BaseColor = FindProperty("_BaseColor", properties);
            m_BumpMap = FindProperty("_BumpMap", properties);
            m_BumpScale = FindProperty("_BumpScale", properties);
            m_NormalBlend = FindProperty("_NormalBlend", properties);
            m_Metallic = FindProperty("_Metallic", properties);
            m_Smoothness = FindProperty("_Smoothness", properties);
            m_AffectNormal = FindProperty("_AffectNormal", properties);
            m_AffectMAOS = FindProperty("_AffectMAOS", properties);
            m_Wear = FindProperty("_Wear", properties);
            m_BrushScale = FindProperty("_BrushScale", properties);
            m_StrokeAlpha = FindProperty("_StrokeAlpha", properties);
            m_DrawOrder = FindProperty("_DrawOrder", properties);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BaseColor, m_BumpScale, m_NormalBlend, m_Metallic, m_Smoothness,
                                    m_AffectNormal, m_AffectMAOS);
            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Decal.Surface", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.TexturePropertySingleLine(Styles.BaseMap, m_BaseMap, m_BaseColor);
                materialEditor.TextureScaleOffsetProperty(m_BaseMap);

                materialEditor.ShaderProperty(m_AffectNormal, Styles.AffectNormal);

                if (IsEnabled(m_AffectNormal))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.TexturePropertySingleLine(Styles.NormalMap, m_BumpMap, m_BumpScale);
                        materialEditor.ShaderProperty(m_NormalBlend, Styles.NormalBlend);
                    }
                }

                materialEditor.ShaderProperty(m_AffectMAOS, Styles.AffectMAOS);

                if (IsEnabled(m_AffectMAOS))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.ShaderProperty(m_Metallic, Styles.Metallic);
                        materialEditor.ShaderProperty(m_Smoothness, Styles.Smoothness);
                    }
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawBrush(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_Wear, m_BrushScale, m_StrokeAlpha);
            var expanded = StyleMaterialHeader.Draw(Styles.Brush, "Decal.Brush", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_Wear, Styles.Wear);
                materialEditor.ShaderProperty(m_BrushScale, Styles.BrushScale);
                materialEditor.ShaderProperty(m_StrokeAlpha, Styles.StrokeAlpha);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DrawOrder);
            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Decal.Advanced", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_DrawOrder, Styles.DrawOrder);
                materialEditor.EnableInstancingField();
            }

            StyleMaterialHeader.End(expanded);
        }
    }
}
