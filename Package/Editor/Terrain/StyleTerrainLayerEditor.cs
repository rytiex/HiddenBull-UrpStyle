using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleTerrainLayer))]
    sealed class StyleTerrainLayerEditor : UnityEditor.Editor
    {
        const string FlowerTexture = "m_FlowerTexture";
        const string CardTexture = "m_GrassCardTexture";
        const string Variation = "m_Variation";
        const string FoldKey = "HiddenBull.UrpStyle.Terrain.Layer.";
        const float Gap = 6f;
        const float GrassLabel = 84f;
        const float ShortLabel = 60f;
        const float SliderGap = 5f;

        static readonly Color s_VariationTint = new Color(0.95f, 0.8f, 0.3f);
        static readonly Color s_FlowerTint = new Color(0.95f, 0.5f, 0.75f);

        static readonly string[] s_TextureFields = { "m_Albedo", "m_Normal", "m_Mask", "m_Tint", "m_Size", "m_Offset" };

        static readonly string[] s_SurfaceFields =
        {
            "m_NormalStrength", "m_Smoothness", "m_Metallic", "m_Wetness", "m_WetGloss", "m_SnowCover"
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
                DrawGrass();

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

        void DrawGrass()
        {
            var labelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = GrassLabel;
            Row("m_GrassDensity", "Density");
            Pair("m_GrassHeight", "Height", "m_GrassWidth", "Width");
            Pair("m_GrassCurl", "Curl", "m_GrassVariation", "Variation");
            Pair("m_GrassTip", "Tip", "m_GrassTipStrength", "Strength");
            Row("m_GrassTransition", "Transition");
            DrawCards();
            EditorGUIUtility.labelWidth = labelWidth;
        }

        void DrawCards()
        {
            var card = serializedObject.FindProperty(CardTexture);

            Split(new GUIContent("Cards", card.tooltip), out var first, out var second);
            Plain(first, card);

            using (new EditorGUI.DisabledScope(!card.hasMultipleDifferentValues && card.objectReferenceValue == null))
            {
                Labelled(second, serializedObject.FindProperty("m_GrassCardShare"), "Share");
                Row("m_GrassCardSpan", "Card Width");
            }
        }

        void Row(string name, string label)
        {
            var property = serializedObject.FindProperty(name);

            EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip));
        }

        void Pair(string left, string leftLabel, string right, string rightLabel)
        {
            var first = serializedObject.FindProperty(left);

            Split(new GUIContent(leftLabel, first.tooltip), out var firstRect, out var secondRect);
            Plain(firstRect, first);
            Labelled(secondRect, serializedObject.FindProperty(right), rightLabel);
        }

        static void Split(GUIContent label, out Rect first, out Rect second)
        {
            var field = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);
            var half = (field.width - Gap) * 0.5f;

            first = new Rect(field.x, field.y, half, field.height);
            second = new Rect(field.xMax - half, field.y, half, field.height);
        }

        static void Plain(Rect rect, SerializedProperty property)
        {
            var indent = EditorGUI.indentLevel;
            var range = property.propertyType == SerializedPropertyType.Float ? Range(property) : null;

            EditorGUI.indentLevel = 0;

            if (range != null)
                Slider(rect, property, range);
            else
                EditorGUI.PropertyField(rect, property, GUIContent.none);

            EditorGUI.indentLevel = indent;
        }

        static void Slider(Rect rect, SerializedProperty property, RangeAttribute range)
        {
            var numberWidth = EditorGUIUtility.fieldWidth;
            var number = new Rect(rect.xMax - numberWidth, rect.y, numberWidth, rect.height);
            var bar = new Rect(rect.x, rect.y, Mathf.Max(rect.width - numberWidth - SliderGap, 0f), rect.height);

            EditorGUI.BeginProperty(rect, GUIContent.none, property);
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            var value = GUI.HorizontalSlider(bar, property.floatValue, range.min, range.max);

            value = EditorGUI.FloatField(number, value);

            if (EditorGUI.EndChangeCheck())
                property.floatValue = Mathf.Clamp(value, range.min, range.max);

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }

        static RangeAttribute Range(SerializedProperty property)
        {
            var field = typeof(HBStyleTerrainLayer).GetField(property.propertyPath,
                                                             BindingFlags.Instance | BindingFlags.NonPublic);

            return field?.GetCustomAttribute<RangeAttribute>();
        }

        static void Labelled(Rect rect, SerializedProperty property, string label)
        {
            var indent = EditorGUI.indentLevel;
            var caption = new Rect(rect.x, rect.y, ShortLabel, rect.height);

            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(caption, new GUIContent(label, property.tooltip));
            EditorGUI.indentLevel = indent;
            Plain(new Rect(caption.xMax, rect.y, rect.width - ShortLabel, rect.height), property);
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
