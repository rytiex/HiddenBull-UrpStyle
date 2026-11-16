using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleStudioPresets
    {
        const string Root = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/Presets";
        const string Extension = ".json";

        readonly string m_Folder;
        List<string> m_Names;
        List<string> m_Categories;

        internal StyleStudioPresets(string page)
        {
            m_Folder = $"{Root}/{page}";
        }

        string physical => Path.GetFullPath(m_Folder);

        internal List<string> categories
        {
            get
            {
                Scan();
                return m_Categories;
            }
        }

        internal List<string> In(string category)
        {
            Scan();
            return m_Names.FindAll(name => CategoryOf(name) == category);
        }

        internal static string CategoryOf(string name)
        {
            var slash = name.IndexOf('/');

            return slash < 0 ? string.Empty : name.Substring(0, slash);
        }

        internal static string TitleOf(string name)
        {
            return name.Substring(name.LastIndexOf('/') + 1);
        }

        void Scan()
        {
            if (m_Names != null)
                return;

            m_Names = new List<string>();
            m_Categories = new List<string>();

            var folder = physical;

            if (!Directory.Exists(folder))
                return;

            foreach (var file in Directory.GetFiles(folder, "*" + Extension, SearchOption.AllDirectories))
                m_Names.Add(Path.ChangeExtension(Path.GetRelativePath(folder, file), null).Replace('\\', '/'));

            m_Names.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (var name in m_Names)
            {
                var category = CategoryOf(name);

                if (!m_Categories.Contains(category))
                    m_Categories.Add(category);
            }
        }

        string PathOf(string name)
        {
            return Path.Combine(physical, name + Extension);
        }

        internal string Read(string name)
        {
            var path = string.IsNullOrEmpty(name) ? null : PathOf(name);

            return path != null && File.Exists(path) ? File.ReadAllText(path) : null;
        }

        internal bool Write(string name, object source)
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

        internal void Delete(string name)
        {
            AssetDatabase.DeleteAsset($"{m_Folder}/{name}{Extension}");

            var category = CategoryOf(name);
            var folder = Path.Combine(physical, category);

            if (category.Length > 0 && Directory.Exists(folder) &&
                Directory.GetFiles(folder, "*" + Extension, SearchOption.AllDirectories).Length == 0)
                AssetDatabase.DeleteAsset($"{m_Folder}/{category}");

            m_Names = null;
        }

        internal string AskName(string category)
        {
            var folder = Path.Combine(physical, category);
            Directory.CreateDirectory(folder);

            var path = EditorUtility.SaveFilePanel("Save Preset", folder, "New Preset", Extension.Substring(1));

            if (string.IsNullOrEmpty(path))
                return null;

            var relative = Path.GetRelativePath(physical, Path.GetFullPath(path));

            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                EditorUtility.DisplayDialog("Save Preset", $"Presets must be saved inside {physical}.", "OK");
                return null;
            }

            return Path.ChangeExtension(relative, null).Replace('\\', '/');
        }
    }
}