using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class ParticleShaderGUI : StyleShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Lighting = new GUIContent("Lighting");
            public static readonly GUIContent Fading = new GUIContent("Fading");
            public static readonly GUIContent Brush = new GUIContent("Brush");
            public static readonly GUIContent Emission = new GUIContent("Emission");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");

            public static readonly string[] BlendNames = { "Alpha", "Premultiply", "Additive" };

            public static readonly GUIContent Blend = new GUIContent(
                "Blend Mode",
                "Alpha for smoke, dust and anything that hides what is behind it. Premultiply for " +
                "textures whose colour is already multiplied by their alpha, so soft edges keep their " +
                "brightness. Additive for fire, sparks and glows: it only adds light, never hides " +
                "anything, and fades in fog like a light rather than turning into fog colour.\n\n" +
                "All three are blended without sorting, together with every other transparent surface.");

            public static readonly GUIContent Lit = new GUIContent(
                "Lit",
                "Lights the particle with the sun, the sky ambient and nearby lights in the same " +
                "painted tones as every other surface, and lets it fall into shadow. Off for fire, " +
                "magic and anything that gives off its own light.");

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap",
                "How far light wraps round the particle. Smoke and dust scatter light, so they look " +
                "right fairly high.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness",
                "Width of each step between the tones.");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone",
                "Width of the middle tone between light and shadow.");

            public static readonly GUIContent SoftParticles = new GUIContent(
                "Soft Particles",
                "Fades the particle where it meets a surface, so a billboard sliding into the ground " +
                "does not show a hard cut line. Needs the camera's depth texture.");

            public static readonly GUIContent SoftDistance = new GUIContent(
                "Distance",
                "Metres over which the particle fades out towards the surface behind it.");

            public static readonly GUIContent CameraFade = new GUIContent(
                "Camera Fade",
                "Fades the particle as it reaches the camera, so it never fills the screen.");

            public static readonly GUIContent CameraFadeNear = new GUIContent(
                "Near", "Distance at which the particle is fully gone.");

            public static readonly GUIContent CameraFadeFar = new GUIContent(
                "Far", "Distance at which the particle starts to fade.");

            public static readonly GUIContent FlipbookBlending = new GUIContent(
                "Flipbook Blending",
                "Blends smoothly between the frames of a Texture Sheet Animation instead of jumping. " +
                "The Particle System has to send the second frame, so press Apply Vertex Streams " +
                "afterwards.");

            public static readonly GUIContent BrushToggle = new GUIContent(
                "Brush",
                "Breaks the particle's soft edge into brush strokes, so smoke and dust read as " +
                "painted rather than as a blurred sprite.");

            public static readonly GUIContent BrushScale = new GUIContent(
                "Strokes Across",
                "How many strokes fit across one particle. The strokes travel with the particle.");

            public static readonly GUIContent StrokeAlpha = new GUIContent(
                "Stroke Alpha",
                "How far the strokes break up the partly transparent areas. Fully clear and fully " +
                "solid areas are left alone.");

            public static readonly GUIContent EmissionToggle = new GUIContent(
                "Emission",
                "Light the particle gives off by itself, added on top and never shadowed.");

            public static readonly GUIContent EmissionMap = new GUIContent(
                "Emission Map",
                "Colour and strength of the glow, multiplied by the colour. Raise the intensity above " +
                "1 for a glow strong enough to bloom later.");

            public static readonly GUIContent ApplyStreams = new GUIContent(
                "Apply Vertex Streams to Selected Particle Systems",
                "Sets the Custom Vertex Streams this material needs on every selected Particle " +
                "System that uses it.");
        }

        MaterialProperty m_BaseMap;
        MaterialProperty m_BaseColor;
        MaterialProperty m_Blend;
        MaterialProperty m_Lit;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_SoftParticles;
        MaterialProperty m_SoftDistance;
        MaterialProperty m_CameraFade;
        MaterialProperty m_CameraFadeNear;
        MaterialProperty m_CameraFadeFar;
        MaterialProperty m_FlipbookBlending;
        MaterialProperty m_BrushEnabled;
        MaterialProperty m_BrushScale;
        MaterialProperty m_StrokeAlpha;
        MaterialProperty m_EmissionEnabled;
        MaterialProperty m_EmissionColor;
        MaterialProperty m_EmissionMap;
        MaterialProperty m_Cull;
        MaterialProperty m_QueueOffset;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            EditorGUI.BeginChangeCheck();

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

        public override void ValidateMaterial(Material material) => Validate(material);

        public static void Validate(Material material)
        {
            var blend = material.HasProperty("_Blend") ? Mathf.RoundToInt(material.GetFloat("_Blend")) : 0;

            SetKeyword(material, "_HB_PREMULTIPLY", blend == 1);
            SetKeyword(material, "_HB_ADDITIVE", blend == 2);

            SyncToggle(material, "_Lit", "_HB_PARTICLE_LIT");
            SyncToggle(material, "_SoftParticles", "_HB_SOFT_PARTICLES");
            SyncToggle(material, "_CameraFade", "_HB_CAMERA_FADE");
            SyncToggle(material, "_FlipbookBlending", "_FLIPBOOKBLENDING_ON");
            SyncToggle(material, "_BrushEnabled", "_HB_BRUSH");
            SyncToggle(material, "_EmissionEnabled", "_HB_EMISSION");

            material.SetShaderPassEnabled("HiddenBullOITMoments", blend != 2);
            material.SetShaderPassEnabled("HiddenBullOITColor", true);
            material.SetOverrideTag("RenderType", "Transparent");

            var queueOffset = material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
            material.renderQueue = (int)RenderQueue.Transparent + queueOffset;
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_BaseMap = FindProperty("_BaseMap", properties);
            m_BaseColor = FindProperty("_BaseColor", properties);
            m_Blend = FindProperty("_Blend", properties);
            m_Lit = FindProperty("_Lit", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);
            m_HalfTone = FindProperty("_HalfTone", properties);
            m_SoftParticles = FindProperty("_SoftParticles", properties);
            m_SoftDistance = FindProperty("_SoftDistance", properties);
            m_CameraFade = FindProperty("_CameraFade", properties);
            m_CameraFadeNear = FindProperty("_CameraFadeNear", properties);
            m_CameraFadeFar = FindProperty("_CameraFadeFar", properties);
            m_FlipbookBlending = FindProperty("_FlipbookBlending", properties);
            m_BrushEnabled = FindProperty("_BrushEnabled", properties);
            m_BrushScale = FindProperty("_BrushScale", properties);
            m_StrokeAlpha = FindProperty("_StrokeAlpha", properties);
            m_EmissionEnabled = FindProperty("_EmissionEnabled", properties);
            m_EmissionColor = FindProperty("_EmissionColor", properties);
            m_EmissionMap = FindProperty("_EmissionMap", properties);
            m_Cull = FindProperty("_Cull", properties);
            m_QueueOffset = FindProperty("_QueueOffset", properties);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_Blend, m_BaseColor);
            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Particle.Surface", materialEditor, reset: reset);

            if (expanded)
            {
                DrawPopup(materialEditor, m_Blend, Styles.Blend, Styles.BlendNames);
                materialEditor.TexturePropertySingleLine(new GUIContent(m_BaseMap.displayName), m_BaseMap, m_BaseColor);
                materialEditor.TextureScaleOffsetProperty(m_BaseMap);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawLighting(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DiffuseWrap, m_DiffuseSoftness, m_HalfTone);
            var expanded = StyleMaterialHeader.Draw(Styles.Lighting, "Particle.Lighting", materialEditor, m_Lit, reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
                materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
                materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawFading(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_SoftParticles, m_SoftDistance, m_CameraFade, m_CameraFadeNear,
                                    m_CameraFadeFar, m_FlipbookBlending);
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

                materialEditor.ShaderProperty(m_FlipbookBlending, Styles.FlipbookBlending);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawBrush(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BrushScale, m_StrokeAlpha);
            var expanded = StyleMaterialHeader.Draw(Styles.BrushToggle, "Particle.Brush", materialEditor, m_BrushEnabled,
                                                    reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_BrushScale, Styles.BrushScale);
                materialEditor.ShaderProperty(m_StrokeAlpha, Styles.StrokeAlpha);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawEmission(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_EmissionColor);
            var expanded = StyleMaterialHeader.Draw(Styles.EmissionToggle, "Particle.Emission", materialEditor,
                                                    m_EmissionEnabled, reset);

            if (expanded)
                materialEditor.TexturePropertyWithHDRColor(Styles.EmissionMap, m_EmissionMap, m_EmissionColor, false);

            StyleMaterialHeader.End(expanded);
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_Cull, m_QueueOffset);
            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Particle.Advanced", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_Cull, m_Cull.displayName);
                materialEditor.ShaderProperty(m_QueueOffset, m_QueueOffset.displayName);
                materialEditor.EnableInstancingField();

                EditorGUILayout.Space(2f);

                if (GUILayout.Button(Styles.ApplyStreams))
                    ApplyVertexStreams(materialEditor);
            }

            StyleMaterialHeader.End(expanded);
        }

        void ApplyVertexStreams(MaterialEditor materialEditor)
        {
            var streams = new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV
            };

            if (IsEnabled(m_FlipbookBlending))
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streams.Add(ParticleSystemVertexStream.AnimBlend);
            }

            foreach (var selected in Selection.gameObjects)
            {
                foreach (var renderer in selected.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (System.Array.IndexOf(renderer.sharedMaterials, materialEditor.target as Material) < 0)
                        continue;

                    Undo.RecordObject(renderer, "Apply Vertex Streams");
                    renderer.SetActiveVertexStreams(streams);
                }
            }
        }
    }
}
