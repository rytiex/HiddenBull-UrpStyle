using UnityEditor;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleLiquidCutter))]
    [CanEditMultipleObjects]
    sealed class StyleLiquidCutterEditor : UnityEditor.Editor
    {
        SerializedProperty m_Shape;

        void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("m_Shape");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var meshShown = m_Shape.hasMultipleDifferentValues ||
                            m_Shape.enumValueIndex == (int)HBStyleLiquidCutShape.Mesh;
            var property = serializedObject.GetIterator();
            var enter = true;

            while (property.NextVisible(enter))
            {
                enter = false;

                if (property.propertyPath == "m_Script" || property.propertyPath == "m_Mesh" && !meshShown)
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
