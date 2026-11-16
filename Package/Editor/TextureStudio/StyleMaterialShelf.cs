using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleMaterialShelf
    {
        const float Height = 132f;
        const float Tile = 76f;
        const int ThumbnailSize = 128;

        static class Styles
        {
            internal static readonly GUIContent Revert = new GUIContent("Revert",
                "Loads the picked preset again, dropping the changes made since.");
            internal static readonly GUIContent Save = new GUIContent("Save",
                "Writes the current material over the picked preset.");
            internal static readonly GUIContent SaveAs = new GUIContent("Save As…",
                "Writes the current material to a new preset in the open category.");
            internal static readonly GUIContent Saved = new GUIContent("Saved",
                "The material matches the picked preset.");
            internal static readonly GUIContent Unsaved = new GUIContent("● Unsaved",
                "The material differs from the picked preset. Save to keep the changes, Revert to drop them.");
            internal static readonly GUIContent NoPreset = new GUIContent("● Not saved",
                "No preset is picked. Use Save As to keep this material.");
            internal static readonly GUIContent Delete = new GUIContent("Delete",
                "Deletes the picked preset. An emptied category folder goes with it.");
            internal static readonly GUIContent Empty = new GUIContent("No presets yet. Use Save As.");

            static GUIStyle s_Warning;
            static GUIStyle s_Name;

            internal static GUIStyle warning => s_Warning ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(1f, 0.62f, 0.2f) }
            };

            internal static GUIStyle name => s_Name ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                clipping = TextClipping.Clip
            };

            internal static readonly Color frame = new Color(0.24f, 0.48f, 0.9f, 1f);
            internal static readonly Color blank = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        readonly StyleStudioPresets m_Presets = new StyleStudioPresets("Material");
        readonly Dictionary<string, RenderTexture> m_Thumbnails = new Dictionary<string, RenderTexture>();

        StyleMaterialGenerator m_Generator;
        Vector2 m_Scroll;
        string m_SnapshotName;
        string m_Snapshot;

        internal bool busy { get; private set; }

        internal Rect Draw(Rect rect, StyleMaterialStack stack, ref string picked, ref string category,
                           Action<string> load)
        {
            var shelf = new Rect(rect.x, rect.yMax - Height, rect.width, Height);
            var categories = m_Presets.categories;

            if (!categories.Contains(category))
                category = categories.Count > 0 ? categories[0] : string.Empty;

            var names = m_Presets.In(category);

            GUILayout.BeginArea(shelf, EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                foreach (var name in categories)
                {
                    var open = name == category;

                    if (GUILayout.Toggle(open, name.Length == 0 ? "Other" : name, EditorStyles.toolbarButton,
                                         GUILayout.ExpandWidth(false)) && !open)
                        category = name;
                }

                GUILayout.FlexibleSpace();
                DrawState(stack, ref picked, category, load);
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, true, false, GUILayout.ExpandHeight(true));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (names.Count == 0)
                    GUILayout.Label(Styles.Empty, EditorStyles.centeredGreyMiniLabel);

                foreach (var name in names)
                {
                    if (DrawTile(name, name == picked))
                        load(name);
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            busy = false;

            if (Event.current.type == EventType.Repaint)
                busy = BuildNext(names);

            return Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax, shelf.yMin - 4f);
        }

        void DrawState(StyleMaterialStack stack, ref string picked, string category, Action<string> load)
        {
            var snapshot = Snapshot(picked, stack.resolution);
            var known = snapshot != null;
            var changed = !known || JsonUtility.ToJson(stack) != snapshot;
            var state = !known ? Styles.NoPreset : changed ? Styles.Unsaved : Styles.Saved;

            GUILayout.Label(state, changed ? Styles.warning : EditorStyles.miniLabel, GUILayout.ExpandWidth(false));

            using (new EditorGUI.DisabledScope(!known || !changed))
            {
                if (GUILayout.Button(Styles.Revert, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    load(picked);

                if (GUILayout.Button(Styles.Save, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) &&
                    EditorUtility.DisplayDialog("Save Preset", $"Overwrite the preset '{picked}'?", "Save",
                                                "Cancel"))
                {
                    Store(picked, stack);
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUI.DisabledScope(!known))
            {
                if (GUILayout.Button(Styles.Delete, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) &&
                    EditorUtility.DisplayDialog("Delete Preset", $"Delete the preset '{picked}'? This cannot be " +
                                                "undone.", "Delete", "Cancel"))
                {
                    m_Presets.Delete(picked);
                    Forget(picked);
                    m_SnapshotName = null;
                    picked = null;
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button(Styles.SaveAs, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                var name = m_Presets.AskName(category);

                if (name != null && Store(name, stack))
                    picked = name;

                GUIUtility.ExitGUI();
            }
        }

        bool DrawTile(string name, bool picked)
        {
            var rect = GUILayoutUtility.GetRect(Tile, Tile + 16f, GUILayout.Width(Tile), GUILayout.Height(Tile + 16f));
            var image = new Rect(rect.x + 2f, rect.y + 2f, Tile - 4f, Tile - 4f);
            var label = new Rect(rect.x, image.yMax + 1f, Tile, 14f);
            var title = StyleStudioPresets.TitleOf(name);

            if (Event.current.type == EventType.Repaint)
            {
                if (picked)
                    EditorGUI.DrawRect(rect, Styles.frame);

                if (m_Thumbnails.TryGetValue(name, out var thumbnail))
                    GUI.DrawTexture(image, thumbnail, ScaleMode.ScaleAndCrop, false);
                else
                    EditorGUI.DrawRect(image, Styles.blank);

                Styles.name.Draw(label, new GUIContent(title), false, false, false, false);
            }

            GUI.Label(rect, new GUIContent(string.Empty, title));

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 ||
                !rect.Contains(Event.current.mousePosition))
                return false;

            Event.current.Use();
            return true;
        }

        bool BuildNext(List<string> names)
        {
            foreach (var name in names)
            {
                if (m_Thumbnails.ContainsKey(name))
                    continue;

                var stack = Parse(name, ThumbnailSize);

                if (stack == null)
                    continue;

                m_Generator ??= new StyleMaterialGenerator();
                m_Generator.Generate(stack);

                if (!m_Generator.ready)
                    return false;

                var thumbnail = new RenderTexture(ThumbnailSize, ThumbnailSize, 0, RenderTextureFormat.ARGB32,
                                                  RenderTextureReadWrite.sRGB)
                {
                    name = "HB Texture Studio Preset",
                    hideFlags = HideFlags.HideAndDontSave
                };

                thumbnail.Create();

                var active = RenderTexture.active;
                Graphics.Blit(m_Generator.albedo, thumbnail);
                RenderTexture.active = active;

                m_Thumbnails[name] = thumbnail;
                return true;
            }

            return false;
        }

        StyleMaterialStack Parse(string name, int resolution)
        {
            var json = m_Presets.Read(name);

            if (json == null)
                return null;

            var stack = new StyleMaterialStack();

            JsonUtility.FromJsonOverwrite(json, stack);
            stack.resolution = resolution;
            return stack;
        }

        internal bool Load(string name, StyleMaterialStack target)
        {
            var stack = Parse(name, target.resolution);

            if (stack == null)
                return false;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(stack), target);
            return true;
        }

        string Snapshot(string name, int resolution)
        {
            var key = name + "@" + resolution;

            if (m_SnapshotName != key)
            {
                var stack = Parse(name, resolution);

                m_Snapshot = stack != null ? JsonUtility.ToJson(stack) : null;
                m_SnapshotName = key;
            }

            return m_Snapshot;
        }

        bool Store(string name, StyleMaterialStack stack)
        {
            if (!m_Presets.Write(name, stack))
                return false;

            m_SnapshotName = null;
            Forget(name);
            return true;
        }

        void Forget(string name)
        {
            if (!m_Thumbnails.TryGetValue(name, out var thumbnail))
                return;

            thumbnail.Release();
            UnityEngine.Object.DestroyImmediate(thumbnail);
            m_Thumbnails.Remove(name);
        }

        internal void Release()
        {
            foreach (var thumbnail in m_Thumbnails.Values)
            {
                thumbnail.Release();
                UnityEngine.Object.DestroyImmediate(thumbnail);
            }

            m_Thumbnails.Clear();
            m_Generator?.Release();
            m_Generator = null;
        }
    }
}