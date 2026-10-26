using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainMenus
    {
        const string ConvertPath = "Assets/HiddenBull/Convert To HB Style Terrain Layer";

        [MenuItem("GameObject/HiddenBull/HB Style Terrain", false, 10)]
        static void CreateTerrain(MenuCommand command)
        {
            var data = StyleTerrainHeights.NewData();
            var terrain = new GameObject("HB Style Terrain", typeof(TerrainCollider), typeof(HBStyleTerrain));

            GameObjectUtility.SetParentAndAlign(terrain, command.context as GameObject);
            terrain.GetComponent<HBStyleTerrain>().data = data;

            Undo.RegisterCreatedObjectUndo(terrain, "Create HB Style Terrain");
            Selection.activeObject = terrain;
        }

        [MenuItem(ConvertPath, true)]
        static bool CanConvert()
        {
            return Selection.GetFiltered<TerrainLayer>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem(ConvertPath, false, 2000)]
        static void Convert()
        {
            var created = new List<Object>();

            foreach (var source in Selection.GetFiltered<TerrainLayer>(SelectionMode.Assets))
            {
                var folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(source))?.Replace('\\', '/');
                var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{source.name}.asset");
                var layer = ScriptableObject.CreateInstance<HBStyleTerrainLayer>();

                StyleTerrainLayerEditor.Copy(source, new SerializedObject(layer));
                AssetDatabase.CreateAsset(layer, path);
                created.Add(layer);
            }

            AssetDatabase.SaveAssets();
            Selection.objects = created.ToArray();
        }
    }
}
