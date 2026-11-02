using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleParticleShaderGUI : StyleShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Particle = new GUIContent("Particle");
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Lighting = new GUIContent("Lighting");
            public static readonly GUIContent Fading = new GUIContent("Fading");
            public static readonly GUIContent Brush = new GUIContent("Brush");
            public static readonly GUIContent Emission = new GUIContent("Emission");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");

            public static readonly string[] BlendModeNames = { "Alpha", "Premultiply", "Additive" };

            public static readonly GUIContent BlendMode = new GUIContent(
                "Blend Mode",
                "Alpha for smoke, dust and anything that hides what is behind it. Premultiply for textures whose " +
                "colour is already multiplied by their alpha, so soft edges keep their brightness. Additive for " +
                "fire, sparks and glows: it only adds light, never hides anything, and fades in fog like a light " +
                "rather than turning into fog colour.");

            public static readonly GUIContent FlipbookBlending = new GUIContent(
                "Flipbook Blending",
                "Blends smoothly between the frames of a Texture Sheet Animation instead of jumping. The Particle " +
                "System has to send the second frame, so press Apply Vertex Streams afterwards.");

            public static readonly GUIContent BaseMap = new GUIContent(
                "Base Map", "Colour and alpha of the particle, multiplied by Base Color and the particle colour.");

            public static readonly GUIContent Lit = new GUIContent(
                "Lighting",
                "Lights the particle like a puff of smoke: the sun and nearby lights reach it from every viewing " +
                "angle, the sky fills it from above, and it darkens clearly in shadow, in the same painted tones as " +
                "every other surface. Off for fire, magic and anything that gives off its own light.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness", "Width of each step between the tones (0 to 2).");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone", "Width of the middle tone between light and shadow (0 to 1).");

            public static readonly GUIContent SoftParticles = new GUIContent(
                "Soft Particles",
                "Fades the particle where it meets a surface, so a billboard sliding into the ground does not " +
                "show a hard cut line.");

            public static readonly GUIContent SoftDistance = new GUIContent(
                "Distance", "Metres over which the particle fades out towards the surface behind it.");

            public static readonly GUIContent CameraFade = new GUIContent(
                "Camera Fade", "Fades the particle as it reaches the camera, so it never fills the screen.");

            public static readonly GUIContent CameraFadeNear = new GUIContent(
                "Near", "Distance in metres at which the particle is fully gone.");

            public static readonly GUIContent CameraFadeFar = new GUIContent(
                "Far", "Distance in metres at which the particle starts to fade.");

            public static readonly GUIContent BrushToggle = new GUIContent(
                "Brush",
                "Breaks the particle's soft edge into brush strokes, so smoke and dust read as painted rather " +
                "than as a blurred sprite.");

            public static readonly GUIContent BrushScale = new GUIContent(
                "Strokes Across", "How many strokes fit across one particle. The strokes travel with the particle.");

            public static readonly GUIContent StrokeAlpha = new GUIContent(
                "Stroke Alpha",
                "How far the strokes break up the partly transparent areas (0 to 1). Fully clear and fully solid " +
                "areas are left alone.");

            public static readonly GUIContent EmissionToggle = new GUIContent(
                "Emission", "Light the particle gives off by itself, added on top and never shadowed.");

            public static readonly GUIContent EmissionMap = new GUIContent(
                "Emission Map",
                "Colour and strength of the glow, multiplied by the colour. Raise the intensity above 1 for a " +
                "glow strong enough to bloom.");

            public static readonly GUIContent ApplyStreams = new GUIContent(
                "Apply Vertex Streams to Selected Particle Systems",
                "Sets the Custom Vertex Streams this material needs on every selected Particle System that uses " +
                "it.");
        }

        const int Premultiply = 1;
        const int Additive = 2;
        const string MomentsPass = "HBStyleOITMoments";

        static readonly (string property, string keyword)[] s_ToggleKeywords =
        {
            ("_FlipbookBlending", "_FLIPBOOKBLENDING_ON"),
            ("_Lit", "_HB_PARTICLE_LIT"),
            ("_SoftParticles", "_HB_SOFT_PARTICLES"),
            ("_CameraFade", "_HB_CAMERA_FADE"),
            ("_BrushEnabled", "_HB_BRUSH"),
            ("_EmissionEnabled", "_HB_EMISSION")
        };

        MaterialProperty m_BlendMode;
        MaterialProperty m_FlipbookBlending;
        MaterialProperty m_BaseMap;
        MaterialProperty m_BaseColor;
        MaterialProperty m_Lit;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_SoftParticles;
        MaterialProperty m_SoftDistance;
        MaterialProperty m_CameraFade;
        MaterialProperty m_CameraFadeNear;
        MaterialProperty m_CameraFadeFar;
        MaterialProperty m_BrushEnabled;
        MaterialProperty m_BrushScale;
        MaterialProperty m_StrokeAlpha;
        MaterialProperty m_EmissionEnabled;
        MaterialProperty m_EmissionColor;
        MaterialProperty m_EmissionMap;
        MaterialProperty m_Cull;

        bool m_Opened;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            if (!m_Opened)
            {
                m_Opened = true;
                ValidateTargets(materialEditor);
            }

            EditorGUI.BeginChangeCheck();

            DrawParticle(materialEditor);
            DrawSurface(materialEditor);
            DrawLighting(materialEditor);
            DrawFading(materialEditor);
            DrawBrush(materialEditor);
            DrawEmission(materialEditor);
            DrawAdvanced(materialEditor);

            CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                ValidateTargets(materialEditor);
        }

        public override void ValidateMaterial(Material material)
        {
            var mode = material.HasProperty("_BlendMode") ? Mathf.RoundToInt(material.GetFloat("_BlendMode")) : 0;

            CoreUtils.SetKeyword(material, "_HB_PREMULTIPLY", mode == Premultiply);
            CoreUtils.SetKeyword(material, "_HB_ADDITIVE", mode == Additive);

            foreach (var (property, keyword) in s_ToggleKeywords)
                CoreUtils.SetKeyword(material, keyword, GetToggle(material, property));

            material.SetShaderPassEnabled(MomentsPass, mode != Additive);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_BlendMode = Optional("_BlendMode", properties);
            m_FlipbookBlending = Optional("_FlipbookBlending", properties);
            m_BaseMap = Optional("_BaseMap", properties);
            m_BaseColor = Optional("_BaseColor", properties);
            m_Lit = Optional("_Lit", properties);
            m_DiffuseSoftness = Optional("_DiffuseSoftness", properties);
            m_HalfTone = Optional("_HalfTone", properties);
            m_SoftParticles = Optional("_SoftParticles", properties);
            m_SoftDistance = Optional("_SoftDistance", properties);
            m_CameraFade = Optional("_CameraFade", properties);
            m_CameraFadeNear = Optional("_CameraFadeNear", properties);
            m_CameraFadeFar = Optional("_CameraFadeFar", properties);
            m_BrushEnabled = Optional("_BrushEnabled", properties);
            m_BrushScale = Optional("_BrushScale", properties);
            m_StrokeAlpha = Optional("_StrokeAlpha", properties);
            m_EmissionEnabled = Optional("_EmissionEnabled", properties);
            m_EmissionColor = Optional("_EmissionColor", properties);
            m_EmissionMap = Optional("_EmissionMap", properties);
            m_Cull = Optional("_Cull", properties);
        }

        void DrawParticle(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BlendMode, m_FlipbookBlending);
            var expanded = StyleMaterialHeader.Draw(Styles.Particle, "Particle.Particle", materialEditor,
                                                    reset: reset);

            if (expanded)
            {
                DrawPopup(materialEditor, m_BlendMode, Styles.BlendMode, Styles.BlendModeNames);
                materialEditor.ShaderProperty(m_FlipbookBlending, Styles.FlipbookBlending);

                if (GUILayout.Button(Styles.ApplyStreams))
                    ApplyVertexStreams(materialEditor);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BaseColor);
            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Particle.Surface", materialEditor,
                                                    reset: reset);

            if (expanded)
            {
                materialEditor.TexturePropertySingleLine(Styles.BaseMap, m_BaseMap, m_BaseColor);
                materialEditor.TextureScaleOffsetProperty(m_BaseMap);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawLighting(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DiffuseSoftness, m_HalfTone);
            var expanded = StyleMaterialHeader.Draw(Styles.Lit, "Particle.Lighting", materialEditor, m_Lit, reset);

            if (expanded)
            {
                using (new EditorGUI.DisabledScope(!IsEnabled(m_Lit)))
                {
                    materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
                    materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawFading(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_SoftParticles, m_SoftDistance, m_CameraFade, m_CameraFadeNear,
                                    m_CameraFadeFar);
            var expanded = StyleMaterialHeader.Draw(Styles.Fading, "Particle.Fading", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_SoftParticles, Styles.SoftParticles);

                if (IsEnabled(m_SoftParticles))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_SoftDistance, Styles.SoftDistance);
                }

                materialEditor.ShaderProperty(m_CameraFade, Styles.CameraFade);

                if (IsEnabled(m_CameraFade))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.ShaderProperty(m_CameraFadeNear, Styles.CameraFadeNear);
                        materialEditor.ShaderProperty(m_CameraFadeFar, Styles.CameraFadeFar);
                    }
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawBrush(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BrushScale, m_StrokeAlpha);
            var expanded = StyleMaterialHeader.Draw(Styles.BrushToggle, "Particle.Brush", materialEditor,
                                                    m_BrushEnabled, reset);

            if (expanded)
            {
                using (new EditorGUI.DisabledScope(!IsEnabled(m_BrushEnabled)))
                {
                    materialEditor.ShaderProperty(m_BrushScale, Styles.BrushScale);
                    materialEditor.ShaderProperty(m_StrokeAlpha, Styles.StrokeAlpha);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawEmission(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_EmissionColor);
            var expanded = StyleMaterialHeader.Draw(Styles.EmissionToggle, "Particle.Emission", materialEditor,
                                                    m_EmissionEnabled, reset);

            if (expanded)
            {
                using (new EditorGUI.DisabledScope(!IsEnabled(m_EmissionEnabled)))
                    materialEditor.TexturePropertyWithHDRColor(Styles.EmissionMap, m_EmissionMap, m_EmissionColor,
                                                               false);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_Cull);
            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Particle.Advanced", materialEditor,
                                                    reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_Cull, m_Cull.displayName);
                materialEditor.EnableInstancingField();
            }

            StyleMaterialHeader.End(expanded);
        }

        void ApplyVertexStreams(MaterialEditor materialEditor)
        {
            var streams = new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV
            };

            if (IsEnabled(m_FlipbookBlending))
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streams.Add(ParticleSystemVertexStream.AnimBlend);
            }

            var material = materialEditor.target as Material;

            foreach (var selected in Selection.gameObjects)
            {
                foreach (var renderer in selected.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (Array.IndexOf(renderer.sharedMaterials, material) < 0)
                        continue;

                    Undo.RecordObject(renderer, "Apply Vertex Streams");
                    renderer.SetActiveVertexStreams(streams);
                }
            }
        }
    }
}
