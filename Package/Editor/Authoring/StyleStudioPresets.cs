using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class StyleStudioPresets
    {
        const string Root = "Packages/com.hiddenbull.urpstyle/Editor/Authoring/Presets";
        const string Extension = ".json";

        static class Styles
        {
            public static readonly GUIContent Preset = new GUIContent("Preset",
                "Settings stored as a file in the package. Picking one loads it.");

            public static readonly GUIContent Revert = new GUIContent("Revert",
                "Loads the picked preset again, dropping the changes made since.");

            public static readonly GUIContent Save = new GUIContent("Save",
                "Writes the current settings over the picked preset's file.");

            public static readonly GUIContent SaveAs = new GUIContent("Save As…",
                "Writes the current settings to a new preset file.");
        }

        readonly string m_Folder;
        string[] m_Names;
        GUIContent[] m_Contents;

        public StyleStudioPresets(string page)
        {
            m_Folder = $"{Root}/{page}";
        }

        public string[] names
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

        public bool Load(string name, object target)
        {
            var path = string.IsNullOrEmpty(name) ? null : PathOf(name);

            if (path == null || !File.Exists(path))
                return false;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(Activator.CreateInstance(target.GetType())), target);
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), target);
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
            return true;
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

        public void Draw(ref string selected, object settings, Action<string> load)
        {
            var list = names;
            var index = Array.IndexOf(list, selected);

            using (new EditorGUILayout.HorizontalScope())
            {
                var picked = EditorGUILayout.Popup(Styles.Preset, index, m_Contents);

                if (picked != index && picked >= 0)
                {
                    selected = list[picked];
                    load(selected);
                }

                using (new EditorGUI.DisabledScope(index < 0))
                {
                    if (GUILayout.Button(Styles.Revert, EditorStyles.miniButtonLeft, GUILayout.Width(52f)))
                        load(selected);

                    if (GUILayout.Button(Styles.Save, EditorStyles.miniButtonMid, GUILayout.Width(42f)) &&
                        EditorUtility.DisplayDialog("Save Preset", $"Overwrite the preset '{selected}'?", "Save",
                                                    "Cancel"))
                    {
                        Write(selected, settings);
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button(Styles.SaveAs, EditorStyles.miniButtonRight, GUILayout.Width(64f)))
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
