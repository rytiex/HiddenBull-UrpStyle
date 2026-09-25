using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomPropertyDrawer(typeof(ShadowQualitySettings))]
    sealed class ShadowQualitySettingsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lines = property.isExpanded ? 8 : 1;

            return lines * EditorGUIUtility.singleLineHeight
                   + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var line = position;
            line.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

            if (!property.isExpanded)
                return;

            var step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            using (new EditorGUI.IndentLevelScope())
            {
                var quality = property.FindPropertyRelative("m_Quality");
                var distance = property.FindPropertyRelative("m_Distance");

                line.y += step;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(line, quality);

                var selected = (StyleShadowQuality)quality.enumValueIndex;

                if (EditorGUI.EndChangeCheck())
                    distance.floatValue = ShadowQualitySettings.DefaultDistance(selected);

                var range = ShadowQualitySettings.DistanceRange(selected);

                line.y += step;

                distance.floatValue = EditorGUI.Slider(
                    line,
                    new GUIContent(distance.displayName, distance.tooltip),
                    Mathf.Clamp(distance.floatValue, range.x, range.y),
                    range.x,
                    range.y);

                line.y += step;
                EditorGUI.PropertyField(line, property.FindPropertyRelative("m_Softness"));

                line.y += step;
                EditorGUI.PropertyField(line, property.FindPropertyRelative("m_Contact"));

                line.y += step;
                EditorGUI.PropertyField(line, property.FindPropertyRelative("m_Brush"));

                line.y += step;
                EditorGUI.PropertyField(line, property.FindPropertyRelative("m_BrushSize"));

                line.y += step;
                EditorGUI.PropertyField(line, property.FindPropertyRelative("m_Debug"));
            }
        }
    }
}
