using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    interface IStyleStudioBasis
    {
        void ApplyBasis();
    }

    sealed class StyleStudioPresets
    {
        const string Root = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/Presets";
        const string Extension = ".json";

        static class Styles
        {
            internal static readonly GUIContent Preset = new GUIContent("Preset",
                "Settings stored as a file in the package. Picking one loads it.");

            internal static readonly GUIContent Revert = new GUIContent("Revert",
                "Loads the picked preset again, dropping the changes made since.");

            internal static readonly GUIContent Save = new GUIContent("Save",
                "Writes the current settings over the picked preset's file.");

            internal static readonly GUIContent SaveAs = new GUIContent("Save As…",
                "Writes the current settings to a new preset file.");

            internal static readonly GUIContent Saved = new GUIContent("Saved",
                "The settings match the picked preset's file.");

            internal static readonly GUIContent Unsaved = new GUIContent("● Unsaved",
                "The settings differ from the picked preset's file. Save to keep them, Revert to drop them.");

            internal static readonly GUIContent NoPreset = new GUIContent("● Not saved",
                "No preset is picked. Use Save As to keep these settings.");

            static GUIStyle s_Warning;

            internal static GUIStyle warning => s_Warning ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(1f, 0.62f, 0.2f) }
            };
        }

        readonly string m_Folder;
        string[] m_Names;
        GUIContent[] m_Contents;
        string m_SnapshotName;
        string m_Snapshot;

        internal StyleStudioPresets(string page)
        {
            m_Folder = $"{Root}/{page}";
        }

        string[] names
        {
            get
            {
                if (m_Names == null)
                {
                    m_Names = Scan();
                    m_Contents = Array.ConvertAll(m_Names, name => new GUIContent(name));
                }

                return m_Names;
            }
        }

        string physical => Path.GetFullPath(m_Folder);

        string[] Scan()
        {
            var folder = physical;

            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            var found = new List<string>();

            foreach (var file in Directory.GetFiles(folder, "*" + Extension, SearchOption.AllDirectories))
                found.Add(Path.ChangeExtension(Path.GetRelativePath(folder, file), null).Replace('\\', '/'));

            found.Sort(StringComparer.OrdinalIgnoreCase);
            return found.ToArray();
        }

        string PathOf(string name)
        {
            return Path.Combine(physical, name + Extension);
        }

        internal bool Load(string name, object target)
        {
            var path = string.IsNullOrEmpty(name) ? null : PathOf(name);

            if (path == null || !File.Exists(path))
                return false;

            var json = File.ReadAllText(path);

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(Activator.CreateInstance(target.GetType())), target);
            JsonUtility.FromJsonOverwrite(json, target);

            if (target is IStyleStudioBasis basis)
            {
                basis.ApplyBasis();
                JsonUtility.FromJsonOverwrite(json, target);
            }

            return true;
        }

        bool Write(string name, object source)
        {
            var path = PathOf(name);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(source, true) + "\n");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                EditorUtility.DisplayDialog("Save Preset", $"Could not write {path}:\n{exception.Message}", "OK");
                return false;
            }

            AssetDatabase.ImportAsset($"{m_Folder}/{name}{Extension}");
            m_Names = null;
            m_SnapshotName = null;
            return true;
        }

        string Snapshot(string name, object settings)
        {
            if (m_SnapshotName != name)
            {
                var copy = Activator.CreateInstance(settings.GetType());

                m_Snapshot = Load(name, copy) ? JsonUtility.ToJson(copy) : null;
                m_SnapshotName = name;
            }

            return m_Snapshot;
        }

        string AskName()
        {
            var folder = physical;
            Directory.CreateDirectory(folder);

            var path = EditorUtility.SaveFilePanel("Save Preset", folder, "New Preset", Extension.Substring(1));

            if (string.IsNullOrEmpty(path))
                return null;

            var relative = Path.GetRelativePath(folder, Path.GetFullPath(path));

            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                EditorUtility.DisplayDialog("Save Preset", $"Presets must be saved inside {folder}.", "OK");
                return null;
            }

            return Path.ChangeExtension(relative, null).Replace('\\', '/');
        }

        internal void Draw(ref string selected, object settings, Action<string> load)
        {
            var list = names;
            var index = Array.IndexOf(list, selected);

            var picked = EditorGUILayout.Popup(Styles.Preset, index, m_Contents);

            if (picked != index && picked >= 0)
            {
                selected = list[picked];
                load(selected);
            }

            var changed = index < 0 || JsonUtility.ToJson(settings) != Snapshot(list[index], settings);
            var state = index < 0 ? Styles.NoPreset : changed ? Styles.Unsaved : Styles.Saved;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(state, changed ? Styles.warning : EditorStyles.miniLabel,
                                GUILayout.Width(EditorGUIUtility.labelWidth - 2f));

                using (new EditorGUI.DisabledScope(index < 0 || !changed))
                {
                    if (GUILayout.Button(Styles.Revert, EditorStyles.miniButtonLeft))
                        load(selected);

                    if (GUILayout.Button(Styles.Save, EditorStyles.miniButtonMid) &&
                        EditorUtility.DisplayDialog("Save Preset", $"Overwrite the preset '{selected}'?", "Save",
                                                    "Cancel"))
                    {
                        Write(selected, settings);
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button(Styles.SaveAs, EditorStyles.miniButtonRight))
                {
                    var name = AskName();

                    if (name != null && Write(name, settings))
                        selected = name;

                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
