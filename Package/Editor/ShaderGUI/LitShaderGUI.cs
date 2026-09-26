using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class LitShaderGUI : ShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Shading = new GUIContent("Shading");
            public static readonly GUIContent OptionalTerms = new GUIContent("Optional Terms");
            public static readonly GUIContent Textures = new GUIContent("Textures (optional)");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");
            public static readonly GUIContent Brush = new GUIContent("Brush");

            public static readonly GUIContent BrushSpace = new GUIContent(
                "Brush Space",
                "World for static geometry. Object for anything that moves — a world-anchored " +
                "brush slides across a carried object. Both use the same stroke size, so the two " +
                "can sit side by side.\n\n" +
                "Rest Pose is for skinned meshes: object space is tied to the transform rather " +
                "than the surface, so skinning drags every point through it and the strokes swim " +
                "across the character. Rest Pose reads a bind-pose position written into the mesh " +
                "at import instead, which skinning cannot move. Use it only on skinned meshes — " +
                "give a shared material its own copy for the character. See Project Settings > " +
                "HiddenBull URP Style.");

            public static readonly GUIContent BrushSize = new GUIContent(
                "Size",
                "Stroke size relative to the project's, which is set once on the renderer feature. " +
                "1 matches every other material; 2 paints this one with strokes twice as large. " +
                "Keep it close to 1 on surfaces that sit next to each other, or the seam between " +
                "them reads as two different brushes.");

            public static readonly GUIContent BrushEdgeKeep = new GUIContent(
                "Edge Keep",
                "How firmly Paint stops at hard edges in the texture. At 1 a stroke that would drag " +
                "in a colour very unlike the one underneath is held back, so regions are painted " +
                "from the inside and a logo, a seam line or a stripe stays crisp. At 0 strokes run " +
                "straight across them and break them up like everything else.\n\n" +
                "Colour is all the shader can go on, so it cannot tell a logo's edge from a " +
                "neighbouring UV island packed close by. If lowering this lets a foreign colour " +
                "creep in somewhere, that is the island showing through.");

            public static readonly GUIContent ToneStrokes = new GUIContent(
                "Tone Strokes",
                "How far light and shade are decided stroke by stroke. Light is read at the middle " +
                "of each stroke instead of at every point, so a whole stroke falls in the light, " +
                "the half tone or the shadow together — the edge between them becomes a run of " +
                "strokes rather than a line, the way a painter lays it. It moves the edge of the " +
                "object's own shadow side and of every shadow cast onto it, along the same strokes " +
                "that repaint the texture.\n\n" +
                "Above 1 a stroke reaches past its own middle and the edge breaks up further. On a " +
                "brushed material this replaces the renderer feature's shadow brush.");

            public static readonly GUIContent BakedStrokes = new GUIContent(
                "Baked Strokes (Bake Only)",
                "The same for lightmapped light: the lightmap is read at the middle of each stroke, " +
                "so the steps of Baked Tones break along strokes too. Turn it down if a lightmap " +
                "chart edge shows through as a stray line.");

            public static readonly GUIContent BrushPaint = new GUIContent(
                "Paint",
                "How far the texture is repainted by the strokes. Each point reads its colour from " +
                "the middle of the stroke it sits under, so a stroke carries one colour across its " +
                "width and drags it along its length, the way a loaded brush does. Nothing slides: " +
                "the texture is resampled, not moved.\n\n" +
                "Strokes keep the same size in the world whatever the UV layout — a densely " +
                "unwrapped face and a stretched one paint with the same brush.\n\n" +
                "0 leaves the texture as it is. Does nothing on a material with no maps, or on " +
                "faces whose UVs were collapsed onto a palette swatch; the relief and break-up " +
                "still act there.");

            public static readonly GUIContent BrushRelief = new GUIContent(
                "Relief",
                "How far each stroke raises the surface along its middle, the way thick paint " +
                "stands proud of the canvas. This is what carries the brush into the ambient, the " +
                "rim and the specular — without it a face that is entirely lit or entirely " +
                "shadowed receives nothing but a flat brightness multiply.");


            public static readonly GUIContent BrushMask = new GUIContent(
                "Mask",
                "Red channel gates the whole brush — paint, relief, break-up and albedo all fall " +
                "silent where it is black. Leave it off and the brush covers everything.");

            public static readonly GUIContent BrushAmbient = new GUIContent(
                "Ambient Break-up",
                "How far the brush shifts what the ambient gradient reads. This is the only brush " +
                "term that still acts where the direct light has saturated — Relief and Shading " +
                "Break-up both work on the terminator, so they fall silent on a face that is " +
                "entirely lit or entirely in shadow.");

            public static readonly GUIContent BrushShading = new GUIContent(
                "Brush Shading Break-up",
                "How far the brush offsets the terminator, breaking the light-to-shadow transition " +
                "into painted patches.");

            public static readonly GUIContent BrushAlbedo = new GUIContent(
                "Brush Albedo Variation",
                "How far the gaps between strokes darken the surface. Only the gaps act, so the " +
                "brush reads as bare patches in the paint rather than as pale strokes laid over " +
                "the top — which is what made it look like scribble.");

            public const string BrushAtlasHint =
                "The atlas and its scale are set once on the HiddenBull Style renderer feature, not " +
                "per material — stroke size has to match across the whole project.";

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap",
                "How far light wraps past the terminator. Low reads as rock, high reads as skin.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness",
                "Width of each step between the tones of a realtime light. Near zero gives a hard " +
                "cel-like cut; high values blend the tones into a smooth gradient. Lightmapped " +
                "light has its own softness under Baked Tones.");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone (Realtime Only)",
                "Width of the middle tone between light and shadow. A painter lays the lit side, " +
                "a half tone and the shadow as flat values rather than as a gradient, and this is " +
                "that half tone. 0 leaves two tones.\n\n" +
                "Cast shadows land on the same tones as the form's own shadow side, since a painter " +
                "does not tell them apart: the soft edge of a shadow becomes the half tone band, " +
                "narrow where the caster touches the ground and wide where it lifts away.\n\n" +
                "It follows realtime light — Realtime and Mixed — whether the object is static or " +
                "not. A fully Baked light puts no realtime light on anything, so it does nothing " +
                "there; Baked Tones covers that case.");

            public static readonly GUIContent BakedTones = new GUIContent(
                "Baked Tones (Bake Only)",
                "How far lightmapped light is stepped into tones as well. A fully Baked light lives " +
                "inside the lightmap, so without this its light and shadow are Unity's smooth " +
                "falloff and none of the tones above apply.\n\n" +
                "The steps are a stop apart — each tone half as bright as the one above — so a " +
                "dark interior stays dark rather than being lifted onto a step. 0 leaves the " +
                "lightmap as it was baked.");

            public static readonly GUIContent BakedSoftness = new GUIContent(
                "Softness",
                "Width of each step between lightmapped tones. Near zero gives crisp flat steps; " +
                "1 blends them back into the lightmap's own falloff.");

            public const string AlbedoOnlyHint =
                "This material samples no textures. Gradation comes from the ambient gradient, " +
                "so a flat colour still reads in three tones.";
        }

        const string k_AlphaTestKeyword = "_ALPHATEST_ON";
        const string k_ReceiveShadowsOffKeyword = "_RECEIVE_SHADOWS_OFF";
        const string k_BrushAnchorKeyword = "_HB_BRUSH_ANCHOR";

        MaterialProperty m_BaseColor;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_BakedTones;
        MaterialProperty m_BakedSoftness;

        MaterialProperty m_RimEnabled;
        MaterialProperty m_RimColor;
        MaterialProperty m_RimPower;
        MaterialProperty m_RimIntensity;

        MaterialProperty m_SpecularEnabled;
        MaterialProperty m_Metallic;
        MaterialProperty m_Smoothness;


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
        MaterialProperty m_BrushMaskEnabled;
        MaterialProperty m_BrushMask;
        MaterialProperty m_BrushAlbedo;

        MaterialProperty m_BaseMapEnabled;
        MaterialProperty m_BaseMap;
        MaterialProperty m_NormalMapEnabled;
        MaterialProperty m_BumpMap;
        MaterialProperty m_BumpScale;

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
            DrawShading(materialEditor);
            DrawOptionalTerms(materialEditor);
            DrawTextures(materialEditor);
            DrawAdvanced(materialEditor);

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var target in materialEditor.targets)
                {
                    if (target is Material material)
                        ValidateMaterial(material);
                }
            }
        }

        public override void ValidateMaterial(Material material)
        {
            var alphaClip = material.HasProperty("_AlphaClipEnabled") &&
                            material.GetFloat("_AlphaClipEnabled") >= 0.5f;
            CoreUtils.SetKeyword(material, k_AlphaTestKeyword, alphaClip);

            var receiveShadows = !material.HasProperty("_ReceiveShadows") ||
                                 material.GetFloat("_ReceiveShadows") >= 0.5f;
            CoreUtils.SetKeyword(material, k_ReceiveShadowsOffKeyword, !receiveShadows);

            var restPose = material.HasProperty("_BrushObjectSpace") &&
                           material.GetFloat("_BrushObjectSpace") >= 1.5f;
            CoreUtils.SetKeyword(material, k_BrushAnchorKeyword, restPose);

            material.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");

            var queueOffset = material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
            var baseQueue = alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            material.renderQueue = baseQueue + queueOffset;

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_BaseColor = FindProperty("_BaseColor", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);
            m_HalfTone = FindProperty("_HalfTone", properties);
            m_BakedTones = FindProperty("_BakedTones", properties);
            m_BakedSoftness = FindProperty("_BakedSoftness", properties);

            m_RimEnabled = FindProperty("_RimEnabled", properties);
            m_RimColor = FindProperty("_RimColor", properties);
            m_RimPower = FindProperty("_RimPower", properties);
            m_RimIntensity = FindProperty("_RimIntensity", properties);

            m_SpecularEnabled = FindProperty("_SpecularEnabled", properties);
            m_Metallic = FindProperty("_Metallic", properties);
            m_Smoothness = FindProperty("_Smoothness", properties);


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
            m_BrushMaskEnabled = FindProperty("_BrushMaskEnabled", properties);
            m_BrushMask = FindProperty("_BrushMask", properties);
            m_BrushAlbedo = FindProperty("_BrushAlbedo", properties);

            m_BaseMapEnabled = FindProperty("_BaseMapEnabled", properties);
            m_BaseMap = FindProperty("_BaseMap", properties);
            m_NormalMapEnabled = FindProperty("_NormalMapEnabled", properties);
            m_BumpMap = FindProperty("_BumpMap", properties);
            m_BumpScale = FindProperty("_BumpScale", properties);

            m_AlphaClipEnabled = FindProperty("_AlphaClipEnabled", properties);
            m_Cutoff = FindProperty("_Cutoff", properties);
            m_ReceiveShadows = FindProperty("_ReceiveShadows", properties);
            m_Cull = FindProperty("_Cull", properties);
            m_QueueOffset = FindProperty("_QueueOffset", properties);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            EditorGUILayout.LabelField(Styles.Surface, EditorStyles.boldLabel);
            materialEditor.ShaderProperty(m_BaseColor, m_BaseColor.displayName);

            if (!IsEnabled(m_BaseMapEnabled) && !IsEnabled(m_NormalMapEnabled))
                EditorGUILayout.HelpBox(Styles.AlbedoOnlyHint, MessageType.None);

            EditorGUILayout.Space();
        }

        void DrawShading(MaterialEditor materialEditor)
        {
            EditorGUILayout.LabelField(Styles.Shading, EditorStyles.boldLabel);
            materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
            materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
            materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
            materialEditor.ShaderProperty(m_BakedTones, Styles.BakedTones);

            if (m_BakedTones.floatValue > 0f)
            {
                using (new EditorGUI.IndentLevelScope())
                    materialEditor.ShaderProperty(m_BakedSoftness, Styles.BakedSoftness);
            }

            EditorGUILayout.Space();
        }

        void DrawOptionalTerms(MaterialEditor materialEditor)
        {
            EditorGUILayout.LabelField(Styles.OptionalTerms, EditorStyles.boldLabel);

            materialEditor.ShaderProperty(m_RimEnabled, m_RimEnabled.displayName);
            if (IsEnabled(m_RimEnabled))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    materialEditor.ShaderProperty(m_RimColor, m_RimColor.displayName);
                    materialEditor.ShaderProperty(m_RimPower, m_RimPower.displayName);
                    materialEditor.ShaderProperty(m_RimIntensity, m_RimIntensity.displayName);
                }
            }

            materialEditor.ShaderProperty(m_SpecularEnabled, m_SpecularEnabled.displayName);
            if (IsEnabled(m_SpecularEnabled))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    materialEditor.ShaderProperty(m_Metallic, m_Metallic.displayName);
                    materialEditor.ShaderProperty(m_Smoothness, m_Smoothness.displayName);
                }
            }

            EditorGUILayout.Space();

            DrawBrush(materialEditor);
        }

        void DrawBrush(MaterialEditor materialEditor)
        {
            EditorGUILayout.LabelField(Styles.Brush, EditorStyles.boldLabel);
            materialEditor.ShaderProperty(m_BrushEnabled, m_BrushEnabled.displayName);

            if (IsEnabled(m_BrushEnabled))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    materialEditor.ShaderProperty(m_BrushObjectSpace, Styles.BrushSpace);
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
                }

                EditorGUILayout.HelpBox(Styles.BrushAtlasHint, MessageType.None);
            }

            EditorGUILayout.Space();
        }

        void DrawTextures(MaterialEditor materialEditor)
        {
            EditorGUILayout.LabelField(Styles.Textures, EditorStyles.boldLabel);

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
                {
                    materialEditor.TexturePropertySingleLine(
                        new GUIContent(m_BumpMap.displayName), m_BumpMap, m_BumpScale);
                }
            }

            EditorGUILayout.Space();
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            EditorGUILayout.LabelField(Styles.Advanced, EditorStyles.boldLabel);

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

        static bool IsEnabled(MaterialProperty property) => property.floatValue >= 0.5f;
    }
}
