using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleTerrainLayer))]
    sealed class StyleTerrainLayerEditor : UnityEditor.Editor
    {
        const string GrassTexture = "m_GrassTexture";

        static readonly string[] s_GrassFields =
        {
            "m_GrassDensity", "m_GrassHeight", "m_GrassWidth", "m_GrassTip", "m_GrassTipStrength"
        };

        static readonly GUIContent s_SourceLabel = new GUIContent("Unity Terrain Layer",
            "A Unity TerrainLayer to copy the textures, tiling, normal strength, smoothness and metallic from.");

        TerrainLayer m_Source;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var property = serializedObject.GetIterator();

            for (var enter = true; property.NextVisible(enter); enter = false)
            {
                if (property.propertyPath == "m_Script" || property.propertyPath == GrassTexture ||
                    System.Array.IndexOf(s_GrassFields, property.propertyPath) >= 0)
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grass", EditorStyles.boldLabel);

            var texture = serializedObject.FindProperty(GrassTexture);

            EditorGUILayout.PropertyField(texture);

            if (texture.hasMultipleDifferentValues || texture.objectReferenceValue != null)
            {
                foreach (var field in s_GrassFields)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);

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
