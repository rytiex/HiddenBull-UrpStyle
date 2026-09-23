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

            public static readonly GUIContent BrushRelief = new GUIContent(
                "Relief",
                "How far the brush perturbs the shading normal. This is what carries the brush into " +
                "the ambient, the rim and the specular — without it a face that is entirely lit or " +
                "entirely shadowed receives nothing but a flat brightness multiply.");

            public static readonly GUIContent BrushUvWarp = new GUIContent(
                "Texture Warp",
                "How far the brush drags the texture coordinates before the base and normal maps " +
                "are read. The map itself bends along the strokes rather than being tinted by " +
                "them, so a photographic texture reads as something that was painted. Does " +
                "nothing on a material with no maps.");

            public static readonly GUIContent BrushMask = new GUIContent(
                "Mask",
                "Red channel gates the whole brush — relief, break-up, albedo and texture warp all " +
                "fall silent where it is black. Leave it off and the brush covers everything.");

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
                "Width of the light-to-shadow transition. Near zero gives a hard cel-like cut; " +
                "high values give a smooth gradient.");

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

        MaterialProperty m_RimEnabled;
        MaterialProperty m_RimColor;
        MaterialProperty m_RimPower;
        MaterialProperty m_RimIntensity;

        MaterialProperty m_SpecularEnabled;
        MaterialProperty m_Metallic;
        MaterialProperty m_Smoothness;


        MaterialProperty m_BrushEnabled;
        MaterialProperty m_BrushObjectSpace;
        MaterialProperty m_BrushRelief;
        MaterialProperty m_BrushShading;
        MaterialProperty m_BrushAmbient;
        MaterialProperty m_BrushUvWarp;
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

            m_RimEnabled = FindProperty("_RimEnabled", properties);
            m_RimColor = FindProperty("_RimColor", properties);
            m_RimPower = FindProperty("_RimPower", properties);
            m_RimIntensity = FindProperty("_RimIntensity", properties);

            m_SpecularEnabled = FindProperty("_SpecularEnabled", properties);
            m_Metallic = FindProperty("_Metallic", properties);
            m_Smoothness = FindProperty("_Smoothness", properties);


            m_BrushEnabled = FindProperty("_BrushEnabled", properties);
            m_BrushObjectSpace = FindProperty("_BrushObjectSpace", properties);
            m_BrushRelief = FindProperty("_BrushRelief", properties);
            m_BrushShading = FindProperty("_BrushShading", properties);
            m_BrushAmbient = FindProperty("_BrushAmbient", properties);
            m_BrushUvWarp = FindProperty("_BrushUvWarp", properties);
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
                    materialEditor.ShaderProperty(m_BrushRelief, Styles.BrushRelief);
                    materialEditor.ShaderProperty(m_BrushShading, Styles.BrushShading);
                    materialEditor.ShaderProperty(m_BrushAmbient, Styles.BrushAmbient);
                    materialEditor.ShaderProperty(m_BrushUvWarp, Styles.BrushUvWarp);
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
