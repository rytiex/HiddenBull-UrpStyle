using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public static class StyleTextureNames
    {
        const string FolderKey = "HiddenBull.UrpStyle.TextureStudio.Folder";

        public static string PickFolder(string title)
        {
            var start = EditorPrefs.GetString(FolderKey, "Assets");
            var chosen = EditorUtility.OpenFolderPanel(title, start, string.Empty);

            if (string.IsNullOrEmpty(chosen))
                return null;

            var folder = FileUtil.GetProjectRelativePath(chosen.Replace('\\', '/'));

            if (string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets"))
            {
                EditorUtility.DisplayDialog(title, "Choose a folder inside the project's Assets folder.", "OK");
                return null;
            }

            EditorPrefs.SetString(FolderKey, folder);
            return folder;
        }

        public static int FreeIndex(string folder, params string[] patterns)
        {
            for (var index = 0; ; index++)
            {
                var free = true;

                foreach (var pattern in patterns)
                    free &= !File.Exists($"{folder}/{string.Format(pattern, index)}");

                if (free)
                    return index;
            }
        }

        public static string Path(string folder, string pattern, int index)
        {
            return $"{folder}/{string.Format(pattern, index)}";
        }
    }
}
