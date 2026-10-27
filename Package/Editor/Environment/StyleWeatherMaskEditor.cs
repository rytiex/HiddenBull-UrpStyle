using UnityEditor;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleWeatherMask))]
    [CanEditMultipleObjects]
    sealed class StyleWeatherMaskEditor : UnityEditor.Editor
    {
        SerializedProperty m_Radius;
        SerializedProperty m_BlockPrecipitation;
        SerializedProperty m_Transition;
        SerializedProperty m_ClearTime;
        SerializedProperty m_CoverTime;

        void OnEnable()
        {
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_BlockPrecipitation = serializedObject.FindProperty("m_BlockPrecipitation");
            m_Transition = serializedObject.FindProperty("m_Transition");
            m_ClearTime = serializedObject.FindProperty("m_ClearTime");
            m_CoverTime = serializedObject.FindProperty("m_CoverTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Radius);
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
