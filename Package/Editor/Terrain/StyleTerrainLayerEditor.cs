using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleTerrainLayer))]
    sealed class StyleTerrainLayerEditor : UnityEditor.Editor
    {
        const string FlowerTexture = "m_FlowerTexture";
        const string Variation = "m_Variation";
        const string FoldKey = "HiddenBull.UrpStyle.Terrain.Layer.";

        static readonly Color s_VariationTint = new Color(0.95f, 0.8f, 0.3f);
        static readonly Color s_FlowerTint = new Color(0.95f, 0.5f, 0.75f);

        static readonly string[] s_TextureFields = { "m_Albedo", "m_Normal", "m_Mask", "m_Tint", "m_Size", "m_Offset" };

        static readonly string[] s_SurfaceFields =
        {
            "m_NormalStrength", "m_Smoothness", "m_Metallic", "m_Wetness", "m_SnowCover"
        };

        static readonly string[] s_GrassFields =
        {
            "m_GrassDensity", "m_GrassHeight", "m_GrassWidth", "m_GrassCurl", "m_GrassVariation", "m_GrassTip",
            "m_GrassTipStrength", "m_GrassTransition"
        };

        static readonly string[] s_FlowerFields = { "m_FlowerVariants", "m_FlowerShare", "m_FlowerSize" };

        static readonly string[] s_VariationFields =
        {
            "m_VariationColor", "m_VariationSize", "m_VariationWarp", "m_VariationDetail", "m_VariationContrast",
            "m_VariationShading", "m_VariationPaint"
        };

        static readonly GUIContent s_SourceLabel = new GUIContent("Unity Terrain Layer",
            "A Unity TerrainLayer to copy the textures, tiling, normal strength, smoothness and metallic from.");

        TerrainLayer m_Source;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (StyleTerrainGUI.Section("Textures", StyleTerrainGUI.shapeTint, FoldKey + "Textures", true))
                Fields(s_TextureFields);

            if (StyleTerrainGUI.Section("Surface", StyleTerrainGUI.settingsTint, FoldKey + "Surface", true))
                Fields(s_SurfaceFields);

            if (StyleTerrainGUI.Section("Variation", s_VariationTint, FoldKey + "Variation", false))
            {
                var variation = serializedObject.FindProperty(Variation);

                EditorGUILayout.PropertyField(variation);

                if (variation.hasMultipleDifferentValues || variation.boolValue)
                    Fields(s_VariationFields);
            }

            if (StyleTerrainGUI.Section("Grass", StyleTerrainGUI.grassTint, FoldKey + "Grass", true))
                Fields(s_GrassFields);

            if (StyleTerrainGUI.Section("Flowers", s_FlowerTint, FoldKey + "Flowers", false))
            {
                var flower = serializedObject.FindProperty(FlowerTexture);

                EditorGUILayout.PropertyField(flower);

                if (flower.hasMultipleDifferentValues || flower.objectReferenceValue != null)
                    Fields(s_FlowerFields);
            }

            serializedObject.ApplyModifiedProperties();

            if (!StyleTerrainGUI.Section("Import", StyleTerrainGUI.settingsTint, FoldKey + "Import", false))
                return;

            m_Source = (TerrainLayer)EditorGUILayout.ObjectField(s_SourceLabel, m_Source, typeof(TerrainLayer), false);

            using (new EditorGUI.DisabledScope(m_Source == null))
            {
                if (GUILayout.Button("Import From Unity Terrain Layer"))
                {
                    Undo.RecordObject(target, "Import Unity Terrain Layer");
                    Copy(m_Source, serializedObject);
                }
            }
        }

        void Fields(string[] names)
        {
            foreach (var field in names)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
        }

        internal static void Copy(TerrainLayer source, SerializedObject target)
        {
            target.Update();

            var masked = source.maskMapTexture != null;

            target.FindProperty("m_Albedo").objectReferenceValue = source.diffuseTexture;
            target.FindProperty("m_Normal").objectReferenceValue = source.normalMapTexture;
            target.FindProperty("m_Mask").objectReferenceValue = source.maskMapTexture;
            target.FindProperty("m_Size").vector2Value = Vector2.Max(source.tileSize, Vector2.one * 0.01f);
            target.FindProperty("m_Offset").vector2Value = source.tileOffset;
            target.FindProperty("m_NormalStrength").floatValue = Mathf.Clamp(source.normalScale, 0f, 2f);
            target.FindProperty("m_Smoothness").floatValue =
                Mathf.Clamp01(masked ? source.maskMapRemapMax.w : source.smoothness);
            target.FindProperty("m_Metallic").floatValue =
                Mathf.Clamp01(masked ? source.maskMapRemapMax.x : source.metallic);

            target.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
