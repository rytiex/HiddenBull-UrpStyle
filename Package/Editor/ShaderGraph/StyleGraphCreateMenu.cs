using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleGraphCreateMenu
    {
        const string Folder = "Packages/com.hiddenbull.urpstyle/Editor/ShaderGraph/";
        const int Priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 2;

        [MenuItem("Assets/Create/Shader Graph/HB URP Style Graph", priority = Priority)]
        static void CreateGraph()
        {
            Create("HBGraphTemplate.json", "New HB URP Style Graph.shadergraph");
        }

        [MenuItem("Assets/Create/Shader Graph/HB URP Style Decal Graph", priority = Priority + 1)]
        static void CreateDecalGraph()
        {
            Create("HBDecalGraphTemplate.json", "New HB URP Style Decal Graph.shadergraph");
        }

        static void Create(string templateName, string fileName)
        {
            var template = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + templateName);

            if (template == null)
            {
                Debug.LogError("HB URP Style Graph template is missing: " + Folder + templateName);
                return;
            }

            ProjectWindowUtil.CreateAssetWithTextContent(fileName, template.text);
        }
    }
}
