using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    abstract class StyleVolumeEditor : VolumeComponentEditor
    {
        const string KeyPrefix = "HiddenBull.UrpStyle.VolumeSection.";

        static readonly Dictionary<(System.Type, string), bool> s_Expanded = new Dictionary<(System.Type, string), bool>();

        readonly Dictionary<string, SerializedDataParameter> m_Parameters = new Dictionary<string, SerializedDataParameter>();

        public override void OnEnable()
        {
            base.OnEnable();
            m_Parameters.Clear();
        }

        protected SerializedDataParameter Parameter(string name)
        {
            if (!m_Parameters.TryGetValue(name, out var parameter))
            {
                parameter = Unpack(serializedObject.FindProperty(name));
                m_Parameters.Add(name, parameter);
            }

            return parameter;
        }

        protected void Section(string title, params string[] names)
        {
            if (!AnyVisible(names))
                return;

            var section = (GetType(), title);

            if (!s_Expanded.TryGetValue(section, out var expanded))
            {
                expanded = EditorPrefs.GetBool(KeyPrefix + GetType().Name + "." + title, true);
                s_Expanded.Add(section, expanded);
            }

            EditorGUILayout.Space(2f);

            var next = CoreEditorUtils.DrawSubHeaderFoldout(EditorGUIUtility.TrTextContent(title), expanded, false);

            if (next != expanded)
            {
                s_Expanded[section] = next;
                EditorPrefs.SetBool(KeyPrefix + GetType().Name + "." + title, next);
            }

            if (!next)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var name in names)
                    PropertyField(Parameter(name));
            }
        }

        protected void SetValue(string name, float value)
        {
            var parameter = Parameter(name);

            parameter.overrideState.boolValue = true;
            parameter.value.floatValue = value;
        }

        bool AnyVisible(string[] names)
        {
            if (showAdditionalProperties)
                return true;

            foreach (var name in names)
            {
                if (Parameter(name).GetAttribute<AdditionalPropertyAttribute>() == null)
                    return true;
            }

            return false;
        }
    }
}
