using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleExportKind
    {
        Colour,
        Normal,
        Data,
        Cutout
    }

    static class StyleStudioExport
    {
        const string FolderKey = "HiddenBull.UrpStyle.TextureStudio.Folder";
        const string DefaultFolder = "Assets/Textures";
        const string Prefix = "TEX";

        static readonly char[] s_Breaks = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '_', '-', '.' }).ToArray();

        static class Styles
        {
            internal static readonly GUIContent Folder = new GUIContent("Folder",
                "Project folder the textures are saved into. Remembered between sessions.");

            internal static readonly GUIContent Pick = new GUIContent("…", "Pick the folder.");
        }

        internal static string folder
        {
            get => EditorPrefs.GetString(FolderKey, DefaultFolder);
            set => EditorPrefs.SetString(FolderKey, value);
        }

        internal static void DrawFolder()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(Styles.Folder);
                GUILayout.Label(new GUIContent(folder, folder), EditorStyles.textField, GUILayout.MinWidth(40f));

                if (GUILayout.Button(Styles.Pick, EditorStyles.miniButton, GUILayout.Width(26f)))
                {
                    var picked = Pick();

                    if (picked != null)
                        folder = picked;

                    GUIUtility.ExitGUI();
                }
            }
        }

        static string Pick()
        {
            var start = Path.GetFullPath(Directory.Exists(folder) ? folder : "Assets");
            var path = EditorUtility.OpenFolderPanel("Texture Folder", start, "");

            if (string.IsNullOrEmpty(path))
                return null;

            var assets = Path.GetFullPath("Assets");
            var relative = Path.GetRelativePath(assets, Path.GetFullPath(path));

            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                EditorUtility.DisplayDialog("Texture Folder", "Pick a folder inside the project's Assets folder.",
                                            "OK");
                return null;
            }

            return relative == "." ? "Assets" : "Assets/" + relative.Replace('\\', '/');
        }

        internal static string FileName(string name)
        {
            var words = new List<string> { Prefix };

            foreach (var word in name.Split(s_Breaks, StringSplitOptions.RemoveEmptyEntries))
            {
                if (words.Count == 1 && string.Equals(word, Prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                words.Add(char.ToUpperInvariant(word[0]) + word.Substring(1));
            }

            return string.Join("_", words);
        }

        internal static string Save(RenderTexture source, string name, StyleExportKind kind)
        {
            var target = folder;
            Directory.CreateDirectory(Path.GetFullPath(target));

            var path = $"{target}/{FileName(name)}.png";
            var width = source.width;
            var height = source.height;
            var plain = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                                                   RenderTextureReadWrite.Linear);
            var active = RenderTexture.active;
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false, true);

            try
            {
                Graphics.Blit(source, plain);
                RenderTexture.active = plain;
                pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                pixels.Apply(false);
                File.WriteAllBytes(Path.GetFullPath(path), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(plain);
                UnityEngine.Object.DestroyImmediate(pixels);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = kind == StyleExportKind.Normal
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;
                importer.sRGBTexture = kind == StyleExportKind.Colour || kind == StyleExportKind.Cutout;
                importer.alphaIsTransparency = kind == StyleExportKind.Cutout;
                importer.wrapMode = kind == StyleExportKind.Cutout ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            return path;
        }
    }
}
