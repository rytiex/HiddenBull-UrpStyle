using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleWeatherBody))]
    [CanEditMultipleObjects]
    sealed class StyleWeatherBodyEditor : UnityEditor.Editor
    {
        SerializedProperty m_ProbeOffset;
        SerializedProperty m_FollowRotation;

        void OnEnable()
        {
            m_ProbeOffset = serializedObject.FindProperty("m_ProbeOffset");
            m_FollowRotation = serializedObject.FindProperty("m_FollowRotation");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ProbeOffset);
            EditorGUILayout.PropertyField(m_FollowRotation);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Works in Play mode. In Edit mode Moving surfaces follow cover at once.",
                                        MessageType.Info);
                return;
            }

            if (targets.Length > 1)
                return;

            var body = (HBStyleWeatherBody)target;

            EditorGUILayout.LabelField("Shelter Map", StyleWeatherBodies.status);
            EditorGUILayout.LabelField("Renderers", body.rendererCount.ToString());
            EditorGUILayout.Space(2f);

            Bar("Open to the sky", body.exposure);
            Bar("Wet", body.wetness);
            Bar("Snow", body.snow);

            Repaint();
        }

        static void Bar(string label, float value)
        {
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), value, $"{label}  {value:P0}");
        }
    }
}
