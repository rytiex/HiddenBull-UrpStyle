using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleTransparentShaderGUI : StyleLitShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Transparency = new GUIContent("Transparency");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");

            public static readonly string[] BlendModeNames =
                { "Alpha", "Premultiply", "Additive", "Tinted", "Refractive" };

            public static readonly GUIContent BlendMode = new GUIContent(
                "Blend Mode",
                "Alpha: Base Color alpha hides what is behind. Premultiply: the colour already holds its alpha, so " +
                "dark parts stay see-through. Additive: adds light, alpha sets how much. Tinted: coloured glass, " +
                "filters what is behind by Base Color, alpha sets how strongly; only highlights and rim sit on top. " +
                "Refractive: Tinted that also bends what is behind it.");

            public static readonly GUIContent Refraction = new GUIContent(
                "Refraction", "How far the surface bends what is seen through it, strongest at its edges (0 to 1).");

            public static readonly GUIContent RefractionBlur = new GUIContent(
                "Blur", "How frosted the view through the surface looks (0 to 1).");

            public static readonly GUIContent Dispersion = new GUIContent(
                "Dispersion", "How far the bent view splits into colour fringes (0 to 1).");

            public static readonly GUIContent StrokeBend = new GUIContent(
                "Stroke Bend",
                "How far brush strokes drag the bent view along them (0 to 1). Needs the Brush section on.");

            public const string TintedHint =
                "This mode ignores diffuse lighting: the surface shows its colour only through what is seen " +
                "behind it.";
        }

        const int Alpha = 0;
        const int Premultiply = 1;
        const int Additive = 2;
        const int Tinted = 3;
        const int Refractive = 4;
        const string MomentsPass = "HBStyleOITMoments";
        const string GlassPass = "HBStyleOITGlass";

        MaterialProperty m_BlendMode;
        MaterialProperty m_Refraction;
        MaterialProperty m_RefractionBlur;
        MaterialProperty m_Dispersion;
        MaterialProperty m_StrokeBend;
        MaterialProperty m_TransparentReceiveShadows;
        MaterialProperty m_TransparentCull;

        protected override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            m_BlendMode = Optional("_BlendMode", properties);
            m_Refraction = Optional("_Refraction", properties);
            m_RefractionBlur = Optional("_RefractionBlur", properties);
            m_Dispersion = Optional("_Dispersion", properties);
            m_StrokeBend = Optional("_StrokeBend", properties);
            m_TransparentReceiveShadows = Optional("_ReceiveShadows", properties);
            m_TransparentCull = Optional("_Cull", properties);
        }

        protected override void DrawSections(MaterialEditor materialEditor)
        {
            DrawTransparency(materialEditor);
            DrawSurface(materialEditor);
            DrawMaps(materialEditor);
            DrawShading(materialEditor);
            DrawBrush(materialEditor);
            DrawSpecular(materialEditor);
            DrawRim(materialEditor);
            DrawEmission(materialEditor);
            DrawTransparentAdvanced(materialEditor);
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);

            var mode = material.HasProperty("_BlendMode") ? Mathf.RoundToInt(material.GetFloat("_BlendMode")) : Alpha;

            CoreUtils.SetKeyword(material, "_HB_PREMULTIPLY", mode == Premultiply);
            CoreUtils.SetKeyword(material, "_HB_ADDITIVE", mode == Additive);
            CoreUtils.SetKeyword(material, "_HB_TINTED", mode == Tinted || mode == Refractive);

            material.SetShaderPassEnabled(MomentsPass, mode != Additive);
            material.SetShaderPassEnabled(GlassPass, mode == Refractive);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        void DrawTransparency(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BlendMode, m_Refraction, m_RefractionBlur, m_Dispersion,
                                    m_StrokeBend);
            var expanded = StyleMaterialHeader.Draw(Styles.Transparency, "Transparent.Transparency", materialEditor,
                                                    reset: reset);

            if (expanded)
            {
                DrawPopup(materialEditor, m_BlendMode, Styles.BlendMode, Styles.BlendModeNames);

                var mode = m_BlendMode.hasMixedValue ? -1 : Mathf.RoundToInt(m_BlendMode.floatValue);

                if (mode == Refractive)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.ShaderProperty(m_Refraction, Styles.Refraction);
                        materialEditor.ShaderProperty(m_RefractionBlur, Styles.RefractionBlur);
                        materialEditor.ShaderProperty(m_Dispersion, Styles.Dispersion);
                        materialEditor.ShaderProperty(m_StrokeBend, Styles.StrokeBend);
                    }
                }

                if (mode == Tinted || mode == Refractive)
                    EditorGUILayout.HelpBox(Styles.TintedHint, MessageType.None);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawTransparentAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_TransparentReceiveShadows, m_TransparentCull);
            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Transparent.Advanced", materialEditor,
                                                    reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_TransparentReceiveShadows, m_TransparentReceiveShadows.displayName);
                materialEditor.ShaderProperty(m_TransparentCull, m_TransparentCull.displayName);
                materialEditor.EnableInstancingField();
            }

            StyleMaterialHeader.End(expanded);
        }
    }
}
