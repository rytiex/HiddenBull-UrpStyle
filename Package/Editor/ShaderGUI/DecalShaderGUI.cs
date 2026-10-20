using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class DecalShaderGUI : StyleShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Brush = new GUIContent("Brush");
            public static readonly GUIContent Shading = new GUIContent("Shading (Screen Space)");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");

            public const string Setup =
                "Place with a URP Decal Projector. The renderer needs the Decal renderer feature. " +
                "With its DBuffer technique the decal is painted into the surfaces under it and lit by " +
                "them, strokes and tones included; with Screen Space it lights itself the same way.";

            public static readonly GUIContent BaseMap = new GUIContent("Base Map");

            public static readonly GUIContent AffectAlbedo = new GUIContent(
                "Affect Color", "Paints the decal's colour over the surface.");

            public static readonly GUIContent AffectNormal = new GUIContent(
                "Affect Normal", "Lets the decal's normal map reshape the light on the surface.");

            public static readonly GUIContent AffectMAOS = new GUIContent(
                "Affect Surface",
                "Replaces the surface's metallic and smoothness with the decal's — a wet patch or a " +
                "worn, polished spot. DBuffer only.");

            public static readonly GUIContent NormalMap = new GUIContent("Normal Map");

            public static readonly GUIContent NormalBlend = new GUIContent(
                "Normal Opacity", "How far the decal's normals replace the surface's.");

            public static readonly GUIContent Metallic = new GUIContent("Metallic");
            public static readonly GUIContent Smoothness = new GUIContent("Smoothness");

            public static readonly GUIContent MAOSOpacity = new GUIContent(
                "Surface Opacity", "How far metallic and smoothness replace the surface's own.");

            public static readonly GUIContent Wear = new GUIContent(
                "Edge Wear",
                "Eats the decal's border away along the brush strokes of the surfaces around it, so " +
                "it looks painted on rather than stuck on. 0 keeps the border straight.");

            public static readonly GUIContent BrushScale = new GUIContent(
                "Brush Size", "Stroke size relative to the project's. Near 1 the decal's wear follows " +
                              "the same strokes as the surface under it.");

            public static readonly GUIContent StrokeAlpha = new GUIContent(
                "Stroke Alpha", "How far the strokes break up the decal's soft, partly transparent areas.");

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap", "How softly light falls off toward the shadow side.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness", "Width of each step between tones.");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone", "Width of the middle tone between light and shadow.");

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
        MaterialProperty m_MAOSOpacity;
        MaterialProperty m_AffectAlbedo;
        MaterialProperty m_AffectNormal;
        MaterialProperty m_AffectMAOS;
        MaterialProperty m_Wear;
        MaterialProperty m_BrushScale;
        MaterialProperty m_StrokeAlpha;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_DrawOrder;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            EditorGUILayout.HelpBox(Styles.Setup, MessageType.None);

            EditorGUI.BeginChangeCheck();

            DrawSurface(materialEditor);
            DrawBrush(materialEditor);
            DrawShading(materialEditor);
            DrawAdvanced(materialEditor);

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
            m_BaseColor = FindProperty("_BaseColor", properties);
            m_BumpMap = FindProperty("_BumpMap", properties);
            m_BumpScale = FindProperty("_BumpScale", properties);
            m_NormalBlend = FindProperty("_NormalBlend", properties);
            m_Metallic = FindProperty("_Metallic", properties);
            m_Smoothness = FindProperty("_Smoothness", properties);
            m_MAOSOpacity = FindProperty("_MAOSOpacity", properties);
            m_AffectAlbedo = FindProperty("_AffectAlbedo", properties);
            m_AffectNormal = FindProperty("_AffectNormal", properties);
            m_AffectMAOS = FindProperty("_AffectMAOS", properties);
            m_Wear = FindProperty("_Wear", properties);
            m_BrushScale = FindProperty("_BrushScale", properties);
            m_StrokeAlpha = FindProperty("_StrokeAlpha", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);
            m_HalfTone = FindProperty("_HalfTone", properties);
            m_DrawOrder = FindProperty("_DrawOrder", properties);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BaseColor, m_BumpScale, m_NormalBlend, m_Metallic, m_Smoothness,
                                    m_MAOSOpacity, m_AffectAlbedo, m_AffectNormal, m_AffectMAOS);
            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Decal.Surface", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_AffectAlbedo, Styles.AffectAlbedo);

                if (IsEnabled(m_AffectAlbedo))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.TexturePropertySingleLine(Styles.BaseMap, m_BaseMap, m_BaseColor);
                        materialEditor.TextureScaleOffsetProperty(m_BaseMap);
                    }
                }

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
                        materialEditor.ShaderProperty(m_MAOSOpacity, Styles.MAOSOpacity);
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

        void DrawShading(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DiffuseWrap, m_DiffuseSoftness, m_HalfTone);
            var expanded = StyleMaterialHeader.Draw(Styles.Shading, "Decal.Shading", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
                materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
                materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
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
