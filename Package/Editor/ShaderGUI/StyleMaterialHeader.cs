using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleMaterialHeader
    {
        const string KeyPrefix = "HiddenBull.UrpStyle.MaterialSection.";
        const float Height = 17f;
        const float IconSize = 13f;

        static readonly Dictionary<string, bool> s_Expanded = new Dictionary<string, bool>();

        static bool IsExpanded(string section)
        {
            if (!s_Expanded.TryGetValue(section, out var expanded))
            {
                expanded = EditorPrefs.GetBool(KeyPrefix + section, true);
                s_Expanded.Add(section, expanded);
            }

            return expanded;
        }

        static void SetExpanded(string section, bool expanded)
        {
            s_Expanded[section] = expanded;
            EditorPrefs.SetBool(KeyPrefix + section, expanded);
        }

        public static bool Draw(GUIContent title, string section, MaterialEditor materialEditor,
                                MaterialProperty toggle = null, Action reset = null)
        {
            var expanded = IsExpanded(section);

            CoreEditorUtils.DrawSplitter();

            var rect = GUILayoutUtility.GetRect(1f, Height);

            var background = rect;
            background.xMin = 0f;
            background.width += 4f;

            var tint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(background, new Color(tint, tint, tint, 0.2f));

            var foldoutRect = new Rect(rect.x + 1f, rect.y + 2f, IconSize, IconSize);
            var toggleRect = new Rect(foldoutRect.xMax + 3f, rect.y + 2f, IconSize, IconSize);
            var menuRect = new Rect(background.xMax - 22f, rect.y + 1f, 16f, 16f);

            var labelRect = rect;
            labelRect.xMin = (toggle != null ? toggleRect.xMax : foldoutRect.xMax) + 4f;
            labelRect.xMax = menuRect.xMin - 2f;

            var nextExpanded = GUI.Toggle(foldoutRect, expanded, GUIContent.none, EditorStyles.foldout);

            var enabled = true;

            if (toggle != null)
            {
                enabled = toggle.hasMixedValue || toggle.floatValue >= 0.5f;

                EditorGUI.BeginChangeCheck();

                var style = toggle.hasMixedValue ? CoreEditorStyles.smallMixedTickbox : CoreEditorStyles.smallTickbox;
                var on = GUI.Toggle(toggleRect, toggle.floatValue >= 0.5f, GUIContent.none, style);

                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(title.text);
                    toggle.floatValue = on ? 1f : 0f;
                    enabled = on;
                }
            }

            using (new EditorGUI.DisabledScope(!enabled))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            if (reset != null && GUI.Button(menuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                ShowMenu(title, reset);

            var current = Event.current;

            if (current.type == EventType.MouseDown && background.Contains(current.mousePosition) &&
                !toggleRect.Contains(current.mousePosition) && !menuRect.Contains(current.mousePosition))
            {
                if (current.button == 0)
                {
                    nextExpanded = !nextExpanded;
                    current.Use();
                }
                else if (current.button == 1 && reset != null)
                {
                    ShowMenu(title, reset);
                    current.Use();
                }
            }

            if (nextExpanded != expanded)
                SetExpanded(section, nextExpanded);

            return nextExpanded && enabled;
        }

        public static void End(bool expanded)
        {
            if (expanded)
                EditorGUILayout.Space();
        }

        static void ShowMenu(GUIContent title, Action reset)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset " + title.text), false, () => reset());
            menu.ShowAsContext();
        }
    }
}
