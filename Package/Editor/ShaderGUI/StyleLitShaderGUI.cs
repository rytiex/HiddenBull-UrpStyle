using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleLitShaderGUI : StyleShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Maps = new GUIContent("Maps");
            public static readonly GUIContent Shading = new GUIContent("Shading");
            public static readonly GUIContent Brush = new GUIContent("Brush");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");
            public static readonly GUIContent Subsurface = new GUIContent("Subsurface");

            public static readonly GUIContent Emission = new GUIContent(
                "Emission",
                "Light the surface gives off by itself: lamps, screens, embers. It is added on top of the lit " +
                "colour, so it shows the same in the dark as in daylight, and it follows the brush strokes like " +
                "the Base Map does.");

            public static readonly GUIContent EmissionMap = new GUIContent(
                "Emission Map",
                "Colour and strength of the glow, read with the Base Map's layout (UVs or Triplanar) and " +
                "multiplied by the colour. Raise the colour's intensity above 1 for a glow strong enough to " +
                "bloom. Set Global Illumination to Baked for it to light its surroundings in the lightmap.");

            public static readonly GUIContent Rim = new GUIContent(
                "Rim Light",
                "A glow along the silhouette where the surface turns away from the view, tinted by the " +
                "ambient sky colour of the Environment Profile.");

            public static readonly GUIContent Specular = new GUIContent(
                "Specular",
                "Highlights and reflections. Reflections come from the Reflection Probes in the scene and fall " +
                "back to the live sky of the Environment Profile wherever no probe reaches, so they follow the " +
                "time of day. Highlights are laid in two flat tones, a sheen and a core, broken up by the brush.");

            public const string RestPoseMissing =
                "Rest Pose needs a skinned mesh whose rest pose was written at import (Project Settings > " +
                "HiddenBull URP Style). A renderer in this selection has none, so it falls back to Object space.";

            public static readonly GUIContent BrushSpace = new GUIContent(
                "Brush Space",
                "World for static geometry. Object for anything that moves, since a world-anchored brush " +
                "slides across a carried object. Rest Pose is for skinned meshes: it reads a bind-pose " +
                "position written into the mesh at import, which skinning cannot move. A mesh without it " +
                "falls back to Object.");

            public static readonly GUIContent BrushSize = new GUIContent(
                "Size",
                "Stroke size relative to the project's, which is set once in the Environment Profile. 1 " +
                "matches every other material; 2 paints this one with strokes twice as large.");

            public static readonly GUIContent BrushPaint = new GUIContent(
                "Paint",
                "How far the texture is repainted by the strokes (0 to 1). Each point reads its colour from the " +
                "middle of the stroke it sits under, so a stroke carries one colour across its width. The " +
                "texture is resampled, not moved. Does nothing on a material with no maps.");

            public static readonly GUIContent BrushEdgeKeep = new GUIContent(
                "Edge Keep",
                "How firmly Paint stops at hard edges in the texture (0 to 1). At 1 a stroke that would drag in " +
                "a very different colour is held back, so a logo or seam stays crisp.");

            public static readonly GUIContent ToneStrokes = new GUIContent(
                "Tone Strokes",
                "How far light and shade are decided stroke by stroke (0 to 2). Light is read at the middle of " +
                "each stroke, so the edge between light and shadow becomes a run of strokes. On a brushed " +
                "material this replaces the shadow edge brush of the Quality Profile.");

            public static readonly GUIContent BakedStrokes = new GUIContent(
                "Baked Strokes",
                "The same for lightmapped light (0 to 1): the lightmap is read at the middle of each stroke. " +
                "Turn it down if a lightmap chart edge shows as a stray line.");

            public static readonly GUIContent BrushRelief = new GUIContent(
                "Relief",
                "How far each stroke raises the surface along its middle (0 to 1). This carries the brush into " +
                "the ambient, the rim and the specular.");

            public static readonly GUIContent BrushShading = new GUIContent(
                "Shading Break-up",
                "How far the brush offsets the terminator, breaking the light-to-shadow transition into painted " +
                "patches (0 to 0.5).");

            public static readonly GUIContent BrushAmbient = new GUIContent(
                "Ambient Break-up",
                "How far the brush shifts what the ambient gradient reads (0 to 0.5). The only brush term that " +
                "still acts where the direct light has saturated.");

            public static readonly GUIContent BrushAlbedo = new GUIContent(
                "Albedo Variation",
                "How far the gaps between strokes darken the surface (0 to 1).");

            public static readonly GUIContent BrushMask = new GUIContent(
                "Mask",
                "Red channel gates the whole brush; it falls silent where the mask is black.");

            public const string BrushAtlasHint =
                "The atlas and its world scale are set once in the Environment Profile, not per material, so " +
                "stroke size matches across the whole project.";

            public static readonly GUIContent Triplanar = new GUIContent(
                "Triplanar Mapping",
                "Lays the Base, Normal and Mask maps on from three sides in space instead of by the mesh's UVs. " +
                "Tiling X on the Base Map is how many times the texture repeats per metre; Offset slides it. " +
                "The Brush Mask still follows the UVs.");

            public static readonly GUIContent TriplanarSpace = new GUIContent(
                "Mapping Space",
                "World for static geometry, Object for anything that moves, Rest Pose for skinned characters. " +
                "It only places the maps; the brush has its own Brush Space.");

            public static readonly GUIContent MaskMap = new GUIContent(
                "Mask Map",
                "Red is Metallic, green Occlusion, alpha Smoothness; blue is reserved for terrain height. " +
                "Smoothness scales its alpha. Import it with sRGB off.");

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap",
                "How softly light falls off toward the terminator (0 to 1). Low reads as rock, high as skin.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness",
                "Width of each step between the tones of a realtime light (0 to 2). Near zero gives a hard cut; " +
                "high values blend the tones into a gradient.");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone",
                "Width of the middle tone between light and shadow for realtime light (0 to 1). Cast shadows " +
                "land on the same tones as the form's own shadow side. 0 leaves two tones.");

            public static readonly GUIContent BakedTones = new GUIContent(
                "Baked Tones",
                "How far lightmapped light is stepped into tones a stop apart (0 to 1). 0 leaves the lightmap " +
                "as it was baked.");

            public static readonly GUIContent BakedSoftness = new GUIContent(
                "Softness",
                "Width of each step between lightmapped tones (0 to 1).");

            public static readonly GUIContent Metallic = new GUIContent(
                "Metallic",
                "At 1 the surface has no diffuse colour of its own; all it shows is reflection tinted by Base " +
                "Color.");

            public static readonly GUIContent Smoothness = new GUIContent(
                "Smoothness",
                "How sharp the highlight and reflections are (0 to 1). With a Mask Map this scales its alpha.");

            public static readonly string[] SubsurfaceModeNames = { "Skin", "Cloth" };

            public static readonly GUIContent SubsurfaceMode = new GUIContent(
                "Material",
                "Skin warms and softens the edge between light and shadow and lets thin parts glow when lit " +
                "from behind. Cloth catches a soft sheen along its edges.");

            public static readonly GUIContent SubsurfaceColor = new GUIContent(
                "Subsurface Color", "Colour the light takes on inside the skin.");

            public static readonly GUIContent SubsurfaceStrength = new GUIContent(
                "Strength", "How strongly that colour shows along the edge between light and shadow (0 to 1).");

            public static readonly GUIContent SubsurfaceWidth = new GUIContent(
                "Width", "How far the coloured edge reaches into the shadow side (0 to 1).");

            public static readonly GUIContent Transmission = new GUIContent(
                "Transmission",
                "Glow of thin parts when a light is behind them (0 to 1). For the sun, thinness comes from its " +
                "shadow map, so it needs shadows on.");

            public static readonly GUIContent TransmissionBend = new GUIContent(
                "Distortion", "How far the surface's shape bends the light passing through (0 to 1).");

            public static readonly GUIContent TransmissionFocus = new GUIContent(
                "Concentration", "How tight the glow is (1 to 16). High keeps it to a small spot.");

            public static readonly GUIContent ClothTransmissionColor = new GUIContent(
                "Transmission Color", "Colour of the light that passes through the cloth.");

            public static readonly GUIContent ThicknessMapToggle = new GUIContent(
                "Use Translucency Map", "Takes the thinness from a painted map instead of measuring it.");

            public static readonly GUIContent ThicknessMap = new GUIContent(
                "Translucency Map", "Red channel: white where the skin is thin, black where it is thick.");

            public static readonly GUIContent SheenColor = new GUIContent(
                "Sheen Color", "Colour of the soft highlight along the cloth's edges.");

            public static readonly GUIContent SheenStrength = new GUIContent("Strength", "Sheen strength (0 to 2).");

            public static readonly GUIContent SheenPower = new GUIContent(
                "Width", "Higher keeps the sheen to a thin line at the very edge (1 to 8).");

            public static readonly GUIContent Weave = new GUIContent(
                "Weave",
                "A fine woven pattern in the light, laid along the UVs or the triplanar space (0 to 1). Fades " +
                "out where threads would be smaller than a pixel.");

            public static readonly GUIContent WeaveScale = new GUIContent(
                "Threads", "How many threads fit across one UV unit or one triplanar tile.");

            public const string AlbedoOnlyHint =
                "This material samples no textures. Gradation comes from the ambient gradient, so a flat " +
                "colour still reads in three tones.";
        }

        static readonly (string property, string keyword)[] s_ToggleKeywords =
        {
            ("_BrushEnabled", "_HB_BRUSH"),
            ("_BrushMaskEnabled", "_HB_BRUSH_MASK"),
            ("_RimEnabled", "_HB_RIM"),
            ("_SpecularEnabled", "_HB_SPECULAR"),
            ("_BaseMapEnabled", "_HB_BASE_MAP"),
            ("_NormalMapEnabled", "_NORMALMAP"),
            ("_MaskMapEnabled", "_HB_MASK_MAP"),
            ("_TriplanarEnabled", "_HB_TRIPLANAR"),
            ("_EmissionEnabled", "_HB_EMISSION"),
            ("_SubsurfaceEnabled", "_HB_SUBSURFACE")
        };

        MaterialProperty m_BaseColor;
        MaterialProperty m_TriplanarEnabled;
        MaterialProperty m_TriplanarSpace;
        MaterialProperty m_BaseMapEnabled;
        MaterialProperty m_BaseMap;
        MaterialProperty m_NormalMapEnabled;
        MaterialProperty m_BumpMap;
        MaterialProperty m_BumpScale;
        MaterialProperty m_MaskMapEnabled;
        MaterialProperty m_MaskMap;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_BakedTones;
        MaterialProperty m_BakedSoftness;
        MaterialProperty m_BrushEnabled;
        MaterialProperty m_BrushObjectSpace;
        MaterialProperty m_BrushScale;
        MaterialProperty m_BrushPaint;
        MaterialProperty m_BrushEdgeKeep;
        MaterialProperty m_ToneStrokes;
        MaterialProperty m_BakedStrokes;
        MaterialProperty m_BrushRelief;
        MaterialProperty m_BrushShading;
        MaterialProperty m_BrushAmbient;
        MaterialProperty m_BrushAlbedo;
        MaterialProperty m_BrushMaskEnabled;
        MaterialProperty m_BrushMask;
        MaterialProperty m_SpecularEnabled;
        MaterialProperty m_Metallic;
        MaterialProperty m_Smoothness;
        MaterialProperty m_RimEnabled;
        MaterialProperty m_RimColor;
        MaterialProperty m_RimPower;
        MaterialProperty m_RimIntensity;
        MaterialProperty m_SubsurfaceEnabled;
        MaterialProperty m_SkinMode;
        MaterialProperty m_SubsurfaceColor;
        MaterialProperty m_SubsurfaceStrength;
        MaterialProperty m_SubsurfaceWidth;
        MaterialProperty m_Transmission;
        MaterialProperty m_TransmissionBend;
        MaterialProperty m_TransmissionFocus;
        MaterialProperty m_TransmissionColor;
        MaterialProperty m_ThicknessMapEnabled;
        MaterialProperty m_ThicknessMap;
        MaterialProperty m_SheenColor;
        MaterialProperty m_SheenStrength;
        MaterialProperty m_SheenPower;
        MaterialProperty m_Weave;
        MaterialProperty m_WeaveScale;
        MaterialProperty m_EmissionEnabled;
        MaterialProperty m_EmissionColor;
        MaterialProperty m_EmissionMap;
        MaterialProperty m_AlphaClipEnabled;
        MaterialProperty m_Cutoff;
        MaterialProperty m_ReceiveShadows;
        MaterialProperty m_Cull;
        MaterialProperty m_QueueOffset;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            EditorGUI.BeginChangeCheck();

            DrawSurface(materialEditor);
            DrawMaps(materialEditor);
            DrawShading(materialEditor);
            DrawBrush(materialEditor);
            DrawSpecular(materialEditor);
            DrawRim(materialEditor);
            DrawSubsurface(materialEditor);
            DrawEmission(materialEditor);
            DrawAdvanced(materialEditor);

            CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                ValidateTargets(materialEditor);
        }

        public override void ValidateMaterial(Material material)
        {
            var alphaClip = GetToggle(material, "_AlphaClipEnabled");
            CoreUtils.SetKeyword(material, "_ALPHATEST_ON", alphaClip);

            var receiveShadows = !material.HasProperty("_ReceiveShadows") || GetToggle(material, "_ReceiveShadows");
            CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", !receiveShadows);

            foreach (var (property, keyword) in s_ToggleKeywords)
                CoreUtils.SetKeyword(material, keyword, GetToggle(material, property));

            var triplanarRest = GetToggle(material, "_TriplanarEnabled") &&
                                material.GetFloat("_TriplanarSpace") >= 1.5f;
            var brushRest = GetToggle(material, "_BrushEnabled") && material.GetFloat("_BrushObjectSpace") >= 1.5f;
            CoreUtils.SetKeyword(material, "_HB_BRUSH_ANCHOR", triplanarRest || brushRest);

            var subsurface = GetToggle(material, "_SubsurfaceEnabled");
            var cloth = subsurface && GetToggle(material, "_SkinMode");

            CoreUtils.SetKeyword(material, "_HB_CLOTH", cloth);
            CoreUtils.SetKeyword(material, "_HB_THICKNESS_MAP",
                                 subsurface && !cloth && GetToggle(material, "_ThicknessMapEnabled"));

            var flags = material.globalIlluminationFlags;

            flags = GetToggle(material, "_EmissionEnabled")
                ? MaterialEditor.FixupEmissiveFlag(material.GetColor("_EmissionColor"), flags)
                : flags | MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            if (material.globalIlluminationFlags != flags)
                material.globalIlluminationFlags = flags;

            material.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");

            var queueOffset = material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
            material.renderQueue = (alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry) + queueOffset;
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_BaseColor = FindProperty("_BaseColor", properties);
            m_TriplanarEnabled = FindProperty("_TriplanarEnabled", properties);
            m_TriplanarSpace = FindProperty("_TriplanarSpace", properties);
            m_BaseMapEnabled = FindProperty("_BaseMapEnabled", properties);
            m_BaseMap = FindProperty("_BaseMap", properties);
            m_NormalMapEnabled = FindProperty("_NormalMapEnabled", properties);
            m_BumpMap = FindProperty("_BumpMap", properties);
            m_BumpScale = FindProperty("_BumpScale", properties);
            m_MaskMapEnabled = FindProperty("_MaskMapEnabled", properties);
            m_MaskMap = FindProperty("_MaskMap", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);
            m_HalfTone = FindProperty("_HalfTone", properties);
            m_BakedTones = FindProperty("_BakedTones", properties);
            m_BakedSoftness = FindProperty("_BakedSoftness", properties);
            m_BrushEnabled = FindProperty("_BrushEnabled", properties);
            m_BrushObjectSpace = FindProperty("_BrushObjectSpace", properties);
            m_BrushScale = FindProperty("_BrushScale", properties);
            m_BrushPaint = FindProperty("_BrushPaint", properties);
            m_BrushEdgeKeep = FindProperty("_BrushEdgeKeep", properties);
            m_ToneStrokes = FindProperty("_ToneStrokes", properties);
            m_BakedStrokes = FindProperty("_BakedStrokes", properties);
            m_BrushRelief = FindProperty("_BrushRelief", properties);
            m_BrushShading = FindProperty("_BrushShading", properties);
            m_BrushAmbient = FindProperty("_BrushAmbient", properties);
            m_BrushAlbedo = FindProperty("_BrushAlbedo", properties);
            m_BrushMaskEnabled = FindProperty("_BrushMaskEnabled", properties);
            m_BrushMask = FindProperty("_BrushMask", properties);
            m_SpecularEnabled = FindProperty("_SpecularEnabled", properties);
            m_Metallic = FindProperty("_Metallic", properties);
            m_Smoothness = FindProperty("_Smoothness", properties);
            m_RimEnabled = FindProperty("_RimEnabled", properties);
            m_RimColor = FindProperty("_RimColor", properties);
            m_RimPower = FindProperty("_RimPower", properties);
            m_RimIntensity = FindProperty("_RimIntensity", properties);
            m_SubsurfaceEnabled = FindProperty("_SubsurfaceEnabled", properties);
            m_SkinMode = FindProperty("_SkinMode", properties);
            m_SubsurfaceColor = FindProperty("_SubsurfaceColor", properties);
            m_SubsurfaceStrength = FindProperty("_SubsurfaceStrength", properties);
            m_SubsurfaceWidth = FindProperty("_SubsurfaceWidth", properties);
            m_Transmission = FindProperty("_Transmission", properties);
            m_TransmissionBend = FindProperty("_TransmissionBend", properties);
            m_TransmissionFocus = FindProperty("_TransmissionFocus", properties);
            m_TransmissionColor = FindProperty("_TransmissionColor", properties);
            m_ThicknessMapEnabled = FindProperty("_ThicknessMapEnabled", properties);
            m_ThicknessMap = FindProperty("_ThicknessMap", properties);
            m_SheenColor = FindProperty("_SheenColor", properties);
            m_SheenStrength = FindProperty("_SheenStrength", properties);
            m_SheenPower = FindProperty("_SheenPower", properties);
            m_Weave = FindProperty("_Weave", properties);
            m_WeaveScale = FindProperty("_WeaveScale", properties);
            m_EmissionEnabled = FindProperty("_EmissionEnabled", properties);
            m_EmissionColor = FindProperty("_EmissionColor", properties);
            m_EmissionMap = FindProperty("_EmissionMap", properties);
            m_AlphaClipEnabled = FindProperty("_AlphaClipEnabled", properties);
            m_Cutoff = FindProperty("_Cutoff", properties);
            m_ReceiveShadows = FindProperty("_ReceiveShadows", properties);
            m_Cull = FindProperty("_Cull", properties);
            m_QueueOffset = FindProperty("_QueueOffset", properties);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BaseColor);
            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Surface", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_BaseColor, m_BaseColor.displayName);

                if (!IsEnabled(m_BaseMapEnabled) && !IsEnabled(m_NormalMapEnabled) && !IsEnabled(m_MaskMapEnabled))
                    EditorGUILayout.HelpBox(Styles.AlbedoOnlyHint, MessageType.None);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawMaps(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_TriplanarEnabled, m_TriplanarSpace, m_BaseMapEnabled,
                                    m_NormalMapEnabled, m_BumpScale, m_MaskMapEnabled);
            var expanded = StyleMaterialHeader.Draw(Styles.Maps, "Maps", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_TriplanarEnabled, Styles.Triplanar);

                if (IsEnabled(m_TriplanarEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.ShaderProperty(m_TriplanarSpace, Styles.TriplanarSpace);

                        if (!IsEnabled(m_BaseMapEnabled))
                            materialEditor.TextureScaleOffsetProperty(m_BaseMap);

                        DrawRestPoseWarning(materialEditor, m_TriplanarSpace);
                    }
                }

                materialEditor.ShaderProperty(m_BaseMapEnabled, m_BaseMapEnabled.displayName);

                if (IsEnabled(m_BaseMapEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.TexturePropertySingleLine(new GUIContent(m_BaseMap.displayName), m_BaseMap);
                        materialEditor.TextureScaleOffsetProperty(m_BaseMap);
                    }
                }

                materialEditor.ShaderProperty(m_NormalMapEnabled, m_NormalMapEnabled.displayName);

                if (IsEnabled(m_NormalMapEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.TexturePropertySingleLine(new GUIContent(m_BumpMap.displayName), m_BumpMap,
                                                                 m_BumpScale);
                }

                materialEditor.ShaderProperty(m_MaskMapEnabled, m_MaskMapEnabled.displayName);

                if (IsEnabled(m_MaskMapEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.TexturePropertySingleLine(Styles.MaskMap, m_MaskMap);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawShading(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DiffuseWrap, m_DiffuseSoftness, m_HalfTone, m_BakedTones,
                                    m_BakedSoftness);
            var expanded = StyleMaterialHeader.Draw(Styles.Shading, "Shading", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
                materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
                materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
                materialEditor.ShaderProperty(m_BakedTones, Styles.BakedTones);

                if (m_BakedTones.floatValue > 0f)
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_BakedSoftness, Styles.BakedSoftness);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawBrush(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BrushObjectSpace, m_BrushScale, m_BrushPaint, m_BrushEdgeKeep,
                                    m_ToneStrokes, m_BakedStrokes, m_BrushRelief, m_BrushShading, m_BrushAmbient,
                                    m_BrushAlbedo, m_BrushMaskEnabled);
            var expanded = StyleMaterialHeader.Draw(Styles.Brush, "Brush", materialEditor, m_BrushEnabled, reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_BrushObjectSpace, Styles.BrushSpace);
                DrawRestPoseWarning(materialEditor, m_BrushObjectSpace);

                materialEditor.ShaderProperty(m_BrushScale, Styles.BrushSize);
                materialEditor.ShaderProperty(m_BrushPaint, Styles.BrushPaint);

                if (m_BrushPaint.floatValue > 0f)
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_BrushEdgeKeep, Styles.BrushEdgeKeep);
                }

                materialEditor.ShaderProperty(m_ToneStrokes, Styles.ToneStrokes);

                if (m_ToneStrokes.floatValue > 0f)
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_BakedStrokes, Styles.BakedStrokes);
                }

                materialEditor.ShaderProperty(m_BrushRelief, Styles.BrushRelief);
                materialEditor.ShaderProperty(m_BrushShading, Styles.BrushShading);
                materialEditor.ShaderProperty(m_BrushAmbient, Styles.BrushAmbient);
                materialEditor.ShaderProperty(m_BrushAlbedo, Styles.BrushAlbedo);
                materialEditor.ShaderProperty(m_BrushMaskEnabled, m_BrushMaskEnabled.displayName);

                if (IsEnabled(m_BrushMaskEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.TexturePropertySingleLine(Styles.BrushMask, m_BrushMask);
                }

                EditorGUILayout.HelpBox(Styles.BrushAtlasHint, MessageType.None);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawSpecular(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_Metallic, m_Smoothness);
            var expanded = StyleMaterialHeader.Draw(Styles.Specular, "Specular", materialEditor, m_SpecularEnabled,
                                                    reset);

            if (expanded)
            {
                if (!IsEnabled(m_MaskMapEnabled))
                    materialEditor.ShaderProperty(m_Metallic, Styles.Metallic);

                materialEditor.ShaderProperty(m_Smoothness, Styles.Smoothness);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawRim(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_RimColor, m_RimPower, m_RimIntensity);
            var expanded = StyleMaterialHeader.Draw(Styles.Rim, "Rim", materialEditor, m_RimEnabled, reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_RimColor, m_RimColor.displayName);
                materialEditor.ShaderProperty(m_RimPower, m_RimPower.displayName);
                materialEditor.ShaderProperty(m_RimIntensity, m_RimIntensity.displayName);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawSubsurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_SkinMode, m_SubsurfaceColor, m_SubsurfaceStrength,
                                    m_SubsurfaceWidth, m_Transmission, m_TransmissionBend, m_TransmissionFocus,
                                    m_TransmissionColor, m_SheenColor, m_SheenStrength, m_SheenPower, m_Weave,
                                    m_WeaveScale);
            var expanded = StyleMaterialHeader.Draw(Styles.Subsurface, "Subsurface", materialEditor,
                                                    m_SubsurfaceEnabled, reset);

            if (expanded)
            {
                DrawPopup(materialEditor, m_SkinMode, Styles.SubsurfaceMode, Styles.SubsurfaceModeNames);

                if (IsEnabled(m_SkinMode))
                {
                    materialEditor.ShaderProperty(m_SheenColor, Styles.SheenColor);
                    materialEditor.ShaderProperty(m_SheenStrength, Styles.SheenStrength);
                    materialEditor.ShaderProperty(m_SheenPower, Styles.SheenPower);
                    materialEditor.ShaderProperty(m_Weave, Styles.Weave);

                    if (m_Weave.floatValue > 0f)
                    {
                        using (new EditorGUI.IndentLevelScope())
                            materialEditor.ShaderProperty(m_WeaveScale, Styles.WeaveScale);
                    }

                    materialEditor.ShaderProperty(m_TransmissionColor, Styles.ClothTransmissionColor);
                    materialEditor.ShaderProperty(m_Transmission, Styles.Transmission);
                    DrawTransmissionShape(materialEditor);
                }
                else
                {
                    materialEditor.ShaderProperty(m_SubsurfaceColor, Styles.SubsurfaceColor);
                    materialEditor.ShaderProperty(m_SubsurfaceStrength, Styles.SubsurfaceStrength);
                    materialEditor.ShaderProperty(m_SubsurfaceWidth, Styles.SubsurfaceWidth);
                    materialEditor.ShaderProperty(m_Transmission, Styles.Transmission);
                    DrawTransmissionShape(materialEditor);
                    materialEditor.ShaderProperty(m_ThicknessMapEnabled, Styles.ThicknessMapToggle);

                    if (IsEnabled(m_ThicknessMapEnabled))
                    {
                        using (new EditorGUI.IndentLevelScope())
                            materialEditor.TexturePropertySingleLine(Styles.ThicknessMap, m_ThicknessMap);
                    }
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawTransmissionShape(MaterialEditor materialEditor)
        {
            if (m_Transmission.floatValue <= 0f)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.ShaderProperty(m_TransmissionBend, Styles.TransmissionBend);
                materialEditor.ShaderProperty(m_TransmissionFocus, Styles.TransmissionFocus);
            }
        }

        void DrawEmission(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_EmissionColor);
            var expanded = StyleMaterialHeader.Draw(Styles.Emission, "Emission", materialEditor, m_EmissionEnabled,
                                                    reset);

            if (expanded)
            {
                materialEditor.TexturePropertyWithHDRColor(Styles.EmissionMap, m_EmissionMap, m_EmissionColor, false);
                materialEditor.LightmapEmissionFlagsProperty(0, true, true);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_AlphaClipEnabled, m_Cutoff, m_ReceiveShadows, m_Cull,
                                    m_QueueOffset);
            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Advanced", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_AlphaClipEnabled, m_AlphaClipEnabled.displayName);

                if (IsEnabled(m_AlphaClipEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_Cutoff, m_Cutoff.displayName);
                }

                materialEditor.ShaderProperty(m_ReceiveShadows, m_ReceiveShadows.displayName);
                materialEditor.ShaderProperty(m_Cull, m_Cull.displayName);
                materialEditor.ShaderProperty(m_QueueOffset, m_QueueOffset.displayName);

                materialEditor.EnableInstancingField();
                materialEditor.DoubleSidedGIField();
            }

            StyleMaterialHeader.End(expanded);
        }

        static void DrawRestPoseWarning(MaterialEditor materialEditor, MaterialProperty space)
        {
            if (space.hasMixedValue || space.floatValue < 1.5f || materialEditor.target is not Material material)
                return;

            if (SelectionLacksRestPose(material))
                EditorGUILayout.HelpBox(Styles.RestPoseMissing, MessageType.Warning);
        }

        static bool SelectionLacksRestPose(Material material)
        {
            foreach (var selected in Selection.gameObjects)
            {
                foreach (var renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    if (Array.IndexOf(renderer.sharedMaterials, material) < 0)
                        continue;

                    if (renderer is not SkinnedMeshRenderer skinned || skinned.sharedMesh == null ||
                        !skinned.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord3))
                        return true;
                }
            }

            return false;
        }
    }
}
