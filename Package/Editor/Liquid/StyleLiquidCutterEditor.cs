using UnityEditor;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleLiquidCutter))]
    [CanEditMultipleObjects]
    sealed class StyleLiquidCutterEditor : UnityEditor.Editor
    {
        SerializedProperty m_Shape;
        SerializedProperty m_Mode;

        void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("m_Shape");
            m_Mode = serializedObject.FindProperty("m_Mode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var meshShown = m_Shape.hasMultipleDifferentValues ||
                            m_Shape.enumValueIndex == (int)HBStyleLiquidCutShape.Mesh;
            var roofShown = m_Mode.hasMultipleDifferentValues ||
                            m_Mode.enumValueIndex == (int)HBStyleLiquidCutMode.CutOut;
            var property = serializedObject.GetIterator();
            var enter = true;

            while (property.NextVisible(enter))
            {
                enter = false;

                if (property.propertyPath == "m_Script" || property.propertyPath == "m_Mesh" && !meshShown ||
                    property.propertyPath == "m_Roof" && !roofShown)
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
