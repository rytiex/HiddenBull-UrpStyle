using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    struct StyleHeaderToggle
    {
        internal bool visible;
        internal bool on;
        internal bool mixed;
    }

    static class StyleSectionHeader
    {
        const string KeyPrefix = "HiddenBull.UrpStyle.Section.";
        const float Height = 17f;
        const float IconSize = 13f;

        static readonly Dictionary<string, bool> s_Expanded = new Dictionary<string, bool>();

        static bool IsExpanded(string key)
        {
            if (!s_Expanded.TryGetValue(key, out var expanded))
            {
                expanded = EditorPrefs.GetBool(KeyPrefix + key, true);
                s_Expanded.Add(key, expanded);
            }

            return expanded;
        }

        static void SetExpanded(string key, bool expanded)
        {
            s_Expanded[key] = expanded;
            EditorPrefs.SetBool(KeyPrefix + key, expanded);
        }

        internal static bool Draw(GUIContent title, string key, Action reset)
        {
            return Draw(title, key, reset, default, out _);
        }

        internal static bool Draw(GUIContent title, string key, Action reset, StyleHeaderToggle toggle,
                                  out bool? toggled)
        {
            toggled = null;

            var expanded = IsExpanded(key);

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
            labelRect.xMin = (toggle.visible ? toggleRect.xMax : foldoutRect.xMax) + 4f;
            labelRect.xMax = menuRect.xMin - 2f;

            var nextExpanded = GUI.Toggle(foldoutRect, expanded, GUIContent.none, EditorStyles.foldout);

            var enabled = true;

            if (toggle.visible)
            {
                enabled = toggle.mixed || toggle.on;

                EditorGUI.BeginChangeCheck();

                var style = toggle.mixed ? CoreEditorStyles.smallMixedTickbox : CoreEditorStyles.smallTickbox;
                var on = GUI.Toggle(toggleRect, toggle.on, GUIContent.none, style);

                if (EditorGUI.EndChangeCheck())
                {
                    toggled = on;
                    enabled = on;
                }
            }

            using (new EditorGUI.DisabledScope(!enabled))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            var menu = reset != null &&
                       GUI.Button(menuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle);

            if (menu)
                ShowMenu(title, reset);

            var current = Event.current;
            var onToggle = toggle.visible && toggleRect.Contains(current.mousePosition);

            if (current.type == EventType.MouseDown && background.Contains(current.mousePosition) && !onToggle &&
                !menuRect.Contains(current.mousePosition))
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
                SetExpanded(key, nextExpanded);

            return nextExpanded && enabled;
        }

        internal static void End(bool expanded)
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

    static class StyleMaterialHeader
    {
        internal static bool Draw(GUIContent title, string section, MaterialEditor materialEditor,
                                  MaterialProperty toggle = null, Action reset = null)
        {
            var state = new StyleHeaderToggle
            {
                visible = toggle != null,
                on = toggle != null && toggle.floatValue >= 0.5f,
                mixed = toggle != null && toggle.hasMixedValue
            };

            var expanded = StyleSectionHeader.Draw(title, "Material." + section, reset, state, out var toggled);

            if (toggled.HasValue)
            {
                materialEditor.RegisterPropertyChangeUndo(title.text);
                toggle.floatValue = toggled.Value ? 1f : 0f;
            }

            return expanded;
        }

        internal static void End(bool expanded) => StyleSectionHeader.End(expanded);
    }
}
