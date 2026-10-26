using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainHeights
    {
        internal const int FlatResolution = 1025;

        internal static readonly Vector3 flatSize = new Vector3(1024f, 300f, 1024f);

        internal static HBStyleTerrainData NewData()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(DataPath());
            var data = ScriptableObject.CreateInstance<HBStyleTerrainData>();

            AssetDatabase.CreateAsset(data, path);
            CreateFlat(data, FlatResolution, flatSize);
            return data;
        }

        static string DataPath()
        {
            var scene = SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(scene.path))
                return "Assets/HB Style Terrain Data.asset";

            var folder = System.IO.Path.GetDirectoryName(scene.path)?.Replace('\\', '/');
            var own = $"{folder}/{scene.name}";

            if (AssetDatabase.IsValidFolder(own))
                folder = own;

            return $"{folder}/{scene.name} Terrain Data.asset";
        }

        internal static bool Square(TerrainData source)
        {
            if (Mathf.Approximately(source.size.x, source.size.z))
                return true;

            EditorUtility.DisplayDialog("Terrain Is Not Square",
                $"{source.name} is {source.size.x:0} × {source.size.z:0} m. HB Style terrains are square; " +
                "resize the Unity terrain to equal width and length first.", "OK");
            return false;
        }

        internal static bool Confirm(HBStyleTerrainData data)
        {
            return data.terrainData == null ||
                   EditorUtility.DisplayDialog("Replace Terrain", "Replace the heights and holes of this terrain?",
                                               "Replace", "Cancel");
        }

        internal static void Import(HBStyleTerrainData data, TerrainData source)
        {
            var terrain = Target(data);
            var resolution = source.heightmapResolution;
            var holes = source.holesResolution;

            terrain.heightmapResolution = resolution;
            terrain.size = source.size;
            terrain.SetHeights(0, 0, source.GetHeights(0, 0, resolution, resolution));
            terrain.SetHoles(0, 0, source.GetHoles(0, 0, holes, holes));

            Finish(data, terrain);
        }

        internal static void CreateFlat(HBStyleTerrainData data, int resolution, Vector3 size)
        {
            var terrain = Target(data);
            var holes = resolution - 1;
            var solid = new bool[holes, holes];

            for (var z = 0; z < holes; z++)
            {
                for (var x = 0; x < holes; x++)
                    solid[z, x] = true;
            }

            terrain.heightmapResolution = resolution;
            terrain.size = size;
            terrain.SetHeights(0, 0, new float[resolution, resolution]);
            terrain.SetHoles(0, 0, solid);

            Finish(data, terrain);
        }

        static TerrainData Target(HBStyleTerrainData data)
        {
            if (data.terrainData != null)
                return data.terrainData;

            var terrain = new TerrainData { name = data.name + " Collider" };

            AssetDatabase.AddObjectToAsset(terrain, data);
            return terrain;
        }

        static void Finish(HBStyleTerrainData data, TerrainData terrain)
        {
            data.Assign(terrain);

            EditorUtility.SetDirty(terrain);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);

            foreach (var owner in HBStyleTerrain.active)
            {
                if (owner.data == data)
                    owner.data = data;
            }
        }
    }
}
