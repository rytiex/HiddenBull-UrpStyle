using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    /// <summary>
    /// Material inspector for HiddenBull/URP Style/Lit.
    ///
    /// The layout follows the order the style is actually authored in (Architecture D10): colour and
    /// the shape of the light first, optional shading terms second, textures last. Texture slots
    /// stay hidden until their toggle is on, which keeps the albedo-only default visible for what it
    /// is — the primary path, not a stripped-down one (D11).
    /// </summary>
    public sealed class LitShaderGUI : ShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Shading = new GUIContent("Shading");
            public static readonly GUIContent OptionalTerms = new GUIContent("Optional Terms");
            public static readonly GUIContent Textures = new GUIContent("Textures (optional)");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap",
                "How far light wraps past the terminator. Low reads as rock, high reads as skin.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness",
                "Width of the light-to-shadow transition. Near zero gives a hard cel-like cut; " +
                "high values give a smooth gradient.");

            public static readonly GUIContent ShadowTerminator = new GUIContent(
                "Shadow Terminator Fade",
                "Fades the shadow map out near the terminator, where it would otherwise produce a " +
                "ragged band of self-shadowing acne. Raise it if that band is visible; lower it if " +
                "shadows are leaking onto surfaces that should be dark.");

            public const string AlbedoOnlyHint =
                "This material samples no textures. Gradation comes from the ambient gradient, " +
                "so a flat colour still reads in three tones.";
        }

        const string k_AlphaTestKeyword = "_ALPHATEST_ON";
        const string k_ReceiveShadowsOffKeyword = "_RECEIVE_SHADOWS_OFF";

        MaterialProperty m_BaseColor;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_ShadowTerminator;

        MaterialProperty m_RimEnabled;
        MaterialProperty m_RimColor;
        MaterialProperty m_RimPower;
        MaterialProperty m_RimIntensity;

        MaterialProperty m_SpecularEnabled;
        MaterialProperty m_Metallic;
        MaterialProperty m_Smoothness;

        MaterialProperty m_VertexColorEnabled;

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

            // Phase 1 is opaque only. Alpha clipping still moves the material into the alpha-test
            // queue so it draws after solid geometry and its cut-outs resolve correctly.
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
            m_ShadowTerminator = FindProperty("_ShadowTerminator", properties);

            m_RimEnabled = FindProperty("_RimEnabled", properties);
            m_RimColor = FindProperty("_RimColor", properties);
            m_RimPower = FindProperty("_RimPower", properties);
            m_RimIntensity = FindProperty("_RimIntensity", properties);

            m_SpecularEnabled = FindProperty("_SpecularEnabled", properties);
            m_Metallic = FindProperty("_Metallic", properties);
            m_Smoothness = FindProperty("_Smoothness", properties);

            m_VertexColorEnabled = FindProperty("_VertexColorEnabled", properties);

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
            materialEditor.ShaderProperty(m_ShadowTerminator, Styles.ShadowTerminator);
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

            materialEditor.ShaderProperty(m_VertexColorEnabled, m_VertexColorEnabled.displayName);
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
