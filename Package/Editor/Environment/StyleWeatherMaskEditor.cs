using UnityEditor;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleWeatherMask))]
    [CanEditMultipleObjects]
    sealed class StyleWeatherMaskEditor : UnityEditor.Editor
    {
        SerializedProperty m_Shape;
        SerializedProperty m_Radius;
        SerializedProperty m_Size;
        SerializedProperty m_BlockPrecipitation;
        SerializedProperty m_Affects;
        SerializedProperty m_Transition;
        SerializedProperty m_ClearTime;
        SerializedProperty m_CoverTime;

        void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("m_Shape");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_Size = serializedObject.FindProperty("m_Size");
            m_BlockPrecipitation = serializedObject.FindProperty("m_BlockPrecipitation");
            m_Affects = serializedObject.FindProperty("m_Affects");
            m_Transition = serializedObject.FindProperty("m_Transition");
            m_ClearTime = serializedObject.FindProperty("m_ClearTime");
            m_CoverTime = serializedObject.FindProperty("m_CoverTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Shape);

            var box = m_Shape.enumValueIndex == (int)HBStyleWeatherMaskShape.Box;

            if (m_Shape.hasMultipleDifferentValues || !box)
                EditorGUILayout.PropertyField(m_Radius);

            if (m_Shape.hasMultipleDifferentValues || box)
                EditorGUILayout.PropertyField(m_Size);

            EditorGUILayout.PropertyField(m_Affects);
            EditorGUILayout.PropertyField(m_BlockPrecipitation);
            EditorGUILayout.PropertyField(m_Transition);

            if (m_Transition.hasMultipleDifferentValues ||
                m_Transition.enumValueIndex == (int)HBStyleWeatherMaskTransition.Gradual)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ClearTime);
                EditorGUILayout.PropertyField(m_CoverTime);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
