using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleShaderGraphMenu
    {
        const string TemplatePath = "Packages/com.hiddenbull.urpstyle/ShaderGraph/HB Lit.shadergraph";

        [MenuItem("Assets/Create/Shader Graph/HiddenBull Style/Lit Shader Graph", priority = 80)]
        static void CreateLit()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(SelectedFolder() + "/New HB Lit Shader Graph.shadergraph");

            File.WriteAllText(path, File.ReadAllText(Path.GetFullPath(TemplatePath)));
            AssetDatabase.ImportAsset(path);

            var created = AssetDatabase.LoadMainAssetAtPath(path);
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        static string SelectedFolder()
        {
            foreach (var selected in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(selected);

                if (string.IsNullOrEmpty(path))
                    continue;

                return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path).Replace('\\', '/');
            }

            return "Assets";
        }
    }
}
