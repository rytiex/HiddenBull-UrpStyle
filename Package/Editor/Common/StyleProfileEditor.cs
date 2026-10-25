using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    abstract class StyleProfileEditor : UnityEditor.Editor
    {
        protected void DrawSection(string property, string title, IReadOnlyDictionary<string, string> groups = null)
        {
            var section = serializedObject.FindProperty(property);

            if (section == null)
                return;

            var expanded = StyleSectionHeader.Draw(new GUIContent(title), target.GetType().Name + "." + property,
                                                   () => ResetSection(property));

            if (expanded)
            {
                var child = section.Copy();
                var end = section.GetEndProperty();
                var enter = true;

                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                {
                    enter = false;

                    if (groups != null && groups.TryGetValue(child.name, out var group))
                    {
                        EditorGUILayout.Space(2f);
                        EditorGUILayout.LabelField(group, EditorStyles.miniBoldLabel);
                    }

                    EditorGUILayout.PropertyField(child, true);
                }
            }

            StyleSectionHeader.End(expanded);
        }

        void ResetSection(string property)
        {
            var defaults = CreateInstance(target.GetType());

            try
            {
                var source = new SerializedObject(defaults).FindProperty(property);

                serializedObject.Update();
                serializedObject.CopyFromSerializedProperty(source);
                serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                DestroyImmediate(defaults);
            }
        }
    }

    [CustomEditor(typeof(HBStyleEnvironmentProfile))]
    [CanEditMultipleObjects]
    sealed class StyleEnvironmentProfileEditor : StyleProfileEditor
    {
        static readonly Dictionary<string, string> s_Sky = new Dictionary<string, string>
        {
            { "m_DaySky", "Sky" },
            { "m_DayAmbient", "Ambient" },
            { "m_SkyLight", "Key Light" },
            { "m_SunDiscColor", "Discs" },
            { "m_DuskElevation", "Day and Night" }
        };

        static readonly Dictionary<string, string> s_Celestial = new Dictionary<string, string>
        {
            { "m_SunIntensity", "Sun" },
            { "m_MoonIntensity", "Moon" },
            { "m_Stars", "Stars" },
            { "m_Aurora", "Aurora" }
        };

        static readonly Dictionary<string, string> s_Clouds = new Dictionary<string, string>
        {
            { "m_Atlas", "Shape" },
            { "m_Steps", "Depth" },
            { "m_SilverLining", "Light" }
        };

        static readonly Dictionary<string, string> s_Atmosphere = new Dictionary<string, string>
        {
            { "m_FogStart", "Fog" },
            { "m_HorizonStart", "Horizon" }
        };

        static readonly Dictionary<string, string> s_Weather = new Dictionary<string, string>
        {
            { "m_RainColor", "Look" },
            { "m_Speed", "Timing" }
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("m_Brush", "Brush");
            DrawSection("m_Sky", "Sky", s_Sky);
            DrawSection("m_Celestial", "Celestial", s_Celestial);
            DrawSection("m_Clouds", "Clouds", s_Clouds);
            DrawSection("m_Atmosphere", "Atmosphere", s_Atmosphere);
            DrawSection("m_Weather", "Weather", s_Weather);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(HBStyleQualityProfile))]
    [CanEditMultipleObjects]
    sealed class StyleQualityProfileEditor : StyleProfileEditor
    {
        static readonly Dictionary<string, string> s_Levels = new Dictionary<string, string>
        {
            { "m_Low", "Levels" },
            { "m_Contact", "Shared" }
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("m_Shadows", "Shadows", s_Levels);
            DrawSection("m_Atmosphere", "Atmosphere", s_Levels);
            DrawSection("m_Weather", "Weather", s_Levels);
            DrawSection("m_Precipitation", "Precipitation", s_Levels);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
