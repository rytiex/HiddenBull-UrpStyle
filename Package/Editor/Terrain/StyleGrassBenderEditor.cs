using UnityEditor;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleGrassBender))]
    [CanEditMultipleObjects]
    sealed class StyleGrassBenderEditor : UnityEditor.Editor
    {
        SerializedProperty m_Shape;
        SerializedProperty m_Radius;
        SerializedProperty m_Length;
        SerializedProperty m_Axis;
        SerializedProperty m_Size;
        SerializedProperty m_FreezeRotation;
        SerializedProperty m_Strength;
        SerializedProperty m_Trail;

        void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("m_Shape");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_Length = serializedObject.FindProperty("m_Length");
            m_Axis = serializedObject.FindProperty("m_Axis");
            m_Size = serializedObject.FindProperty("m_Size");
            m_FreezeRotation = serializedObject.FindProperty("m_FreezeRotation");
            m_Strength = serializedObject.FindProperty("m_Strength");
            m_Trail = serializedObject.FindProperty("m_Trail");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Shape);

            var mixed = m_Shape.hasMultipleDifferentValues;
            var shape = (HBStyleGrassBenderShape)m_Shape.enumValueIndex;

            EditorGUI.indentLevel++;

            if (mixed || shape != HBStyleGrassBenderShape.Box)
                EditorGUILayout.PropertyField(m_Radius);

            if (mixed || shape == HBStyleGrassBenderShape.Capsule)
            {
                EditorGUILayout.PropertyField(m_Length);
                EditorGUILayout.PropertyField(m_Axis);
            }

            if (mixed || shape == HBStyleGrassBenderShape.Box)
                EditorGUILayout.PropertyField(m_Size);

            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(m_FreezeRotation);
            EditorGUILayout.PropertyField(m_Strength);
            EditorGUILayout.PropertyField(m_Trail);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
