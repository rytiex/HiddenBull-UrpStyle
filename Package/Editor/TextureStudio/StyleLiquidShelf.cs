using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleLiquidPreset
    {
        public StyleRippleStack stack = new StyleRippleStack();
        public string look;
    }

    sealed class StyleLiquidShelf
    {
        internal const string Starter = "Liquid";

        const string SeedKey = "HiddenBull.UrpStyle.TextureStudio.Liquid.Seeded";

        const float Height = 132f;
        const float Tile = 76f;
        const int ThumbnailSize = 128;
        const string NormalMapField = "m_NormalMap";
        const string MaskMapField = "m_MaskMap";

        static readonly StyleLiquidRecipe[] s_Recipes =
        {
            StyleLiquidRecipe.Water, StyleLiquidRecipe.Acid, StyleLiquidRecipe.Lava
        };

        static class Styles
        {
            internal static readonly GUIContent Revert = new GUIContent("Revert",
                "Loads the picked preset again, dropping the changes made since.");
            internal static readonly GUIContent Save = new GUIContent("Save",
                "Writes the current liquid over the picked preset.");
            internal static readonly GUIContent SaveAs = new GUIContent("Save As…",
                "Writes the current liquid, its layers and its Look, to a new preset in the open category.");
            internal static readonly GUIContent Saved = new GUIContent("Saved",
                "The liquid matches the picked preset.");
            internal static readonly GUIContent Unsaved = new GUIContent("● Unsaved",
                "The liquid differs from the picked preset. Save to keep the changes, Revert to drop them.");
            internal static readonly GUIContent NoPreset = new GUIContent("● Not saved",
                "No preset is picked. Use Save As to keep this liquid.");
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

        readonly StyleStudioPresets m_Presets = new StyleStudioPresets("Liquid");
        readonly Dictionary<string, RenderTexture> m_Thumbnails = new Dictionary<string, RenderTexture>();
        readonly List<string> m_Categories = new List<string>();

        StyleLiquidGenerator m_Generator;
        HBStyleLiquidLayer m_Paint;
        Vector2 m_Scroll;
        string m_SnapshotName;
        string m_Snapshot;
        string m_LookSource;
        string m_LookClean;

        internal bool busy { get; private set; }

        List<string> categories
        {
            get
            {
                m_Categories.Clear();
                m_Categories.AddRange(m_Presets.categories);

                if (m_Categories.Count == 0)
                    m_Categories.Add(Starter);

                return m_Categories;
            }
        }

        List<string> In(string category)
        {
            return m_Presets.In(category);
        }

        internal Rect Draw(Rect rect, StyleLiquidPreset current, ref string picked, ref string category,
                           Action<string> load)
        {
            Seed();

            var shelf = new Rect(rect.x, rect.yMax - Height, rect.width, Height);
            var all = categories;

            if (!all.Contains(category))
                category = all[0];

            var names = In(category);

            GUILayout.BeginArea(shelf, EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                foreach (var name in all)
                {
                    var open = name == category;

                    if (GUILayout.Toggle(open, name.Length == 0 ? "Other" : name, EditorStyles.toolbarButton,
                                         GUILayout.ExpandWidth(false)) && !open)
                        category = name;
                }

                GUILayout.FlexibleSpace();
                DrawState(current, ref picked, category, load);
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, true, false, GUILayout.ExpandHeight(true));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (names.Count == 0)
                    GUILayout.Label(Styles.Empty, EditorStyles.centeredGreyMiniLabel);

                foreach (var name in names)
                {
                    if (!DrawTile(name, name == picked))
                        continue;

                    picked = name;
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

        void DrawState(StyleLiquidPreset current, ref string picked, string category, Action<string> load)
        {
            var snapshot = Snapshot(picked, current.stack.resolution);
            var known = snapshot != null;
            var changed = !known || Json(current) != snapshot;
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
                    Store(picked, current);
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

                if (name != null && Store(name, current))
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

                var preset = Parse(name, ThumbnailSize);

                if (preset == null)
                    continue;

                m_Generator ??= new StyleLiquidGenerator();

                if (!m_Generator.Generate(preset.stack, false))
                    return false;

                m_Generator.Paint(Painted(preset.look));
                m_Generator.Show(StyleLiquidMap.Colour);

                var thumbnail = new RenderTexture(ThumbnailSize, ThumbnailSize, 0, RenderTextureFormat.ARGB32,
                                                  RenderTextureReadWrite.Linear)
                {
                    name = "HB Texture Studio Liquid Preset",
                    hideFlags = HideFlags.HideAndDontSave
                };

                thumbnail.Create();

                var active = RenderTexture.active;
                Graphics.Blit(m_Generator.display, thumbnail);
                RenderTexture.active = active;

                m_Thumbnails[name] = thumbnail;
                return true;
            }

            return false;
        }

        HBStyleLiquidLayer Painted(string look)
        {
            if (m_Paint == null)
            {
                m_Paint = ScriptableObject.CreateInstance<HBStyleLiquidLayer>();
                m_Paint.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!string.IsNullOrEmpty(look))
                EditorJsonUtility.FromJsonOverwrite(look, m_Paint);

            return m_Paint;
        }

        internal StyleLiquidPreset Parse(string name, int resolution)
        {
            var json = m_Presets.Read(name);

            if (json == null)
                return null;

            var preset = new StyleLiquidPreset();

            JsonUtility.FromJsonOverwrite(json, preset);

            preset.stack ??= new StyleRippleStack();
            preset.stack.resolution = resolution;
            preset.look = Clean(preset.look);
            return preset;
        }

        internal void Seed()
        {
            if (EditorPrefs.GetBool(SeedKey))
                return;

            EditorPrefs.SetBool(SeedKey, true);

            foreach (var recipe in s_Recipes)
            {
                var name = $"{Starter}/{recipe}";

                if (m_Presets.Read(name) == null)
                    Store(name, StarterPreset(recipe));
            }
        }

        static StyleLiquidPreset StarterPreset(StyleLiquidRecipe recipe)
        {
            var look = ScriptableObject.CreateInstance<HBStyleLiquidLayer>();

            look.hideFlags = HideFlags.HideAndDontSave;

            var serialized = new SerializedObject(look);

            StyleLiquidRecipes.Apply(serialized, recipe);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var json = EditorJsonUtility.ToJson(look);

            UnityEngine.Object.DestroyImmediate(look);

            return new StyleLiquidPreset
            {
                stack = new StyleRippleStack
                {
                    name = StyleRippleStack.NameOf(recipe), layers = StyleRippleStack.Layers(recipe)
                },
                look = json
            };
        }

        internal static string Clean(string look)
        {
            var layer = ScriptableObject.CreateInstance<HBStyleLiquidLayer>();

            if (!string.IsNullOrEmpty(look))
                EditorJsonUtility.FromJsonOverwrite(look, layer);

            layer.hideFlags = HideFlags.HideAndDontSave;
            layer.name = "Liquid";

            var serialized = new SerializedObject(layer);

            serialized.FindProperty(NormalMapField).objectReferenceValue = null;
            serialized.FindProperty(MaskMapField).objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var json = EditorJsonUtility.ToJson(layer);

            UnityEngine.Object.DestroyImmediate(layer);
            return json;
        }

        string Json(StyleLiquidPreset current)
        {
            if (current.look != m_LookSource)
            {
                m_LookSource = current.look;
                m_LookClean = Clean(current.look);
            }

            return JsonUtility.ToJson(new StyleLiquidPreset { stack = current.stack, look = m_LookClean });
        }

        string Snapshot(string name, int resolution)
        {
            var key = name + "@" + resolution;

            if (m_SnapshotName != key)
            {
                var preset = string.IsNullOrEmpty(name) ? null : Parse(name, resolution);

                m_Snapshot = preset != null ? JsonUtility.ToJson(preset) : null;
                m_SnapshotName = key;
            }

            return m_Snapshot;
        }

        bool Store(string name, StyleLiquidPreset current)
        {
            var preset = new StyleLiquidPreset { stack = current.stack, look = Clean(current.look) };

            if (!m_Presets.Write(name, preset))
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

            if (m_Paint != null)
                UnityEngine.Object.DestroyImmediate(m_Paint);

            m_Paint = null;
        }
    }
}
