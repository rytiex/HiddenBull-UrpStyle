using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public static class StyleTerrainSetup
    {
        public static bool UsesLayerShader(Terrain terrain)
        {
            var material = terrain != null ? terrain.materialTemplate : null;
            return material != null && material.shader != null && material.shader.name == HBStyleTerrain.ShaderName;
        }

        static string Folder(Terrain terrain, Object near)
        {
            var path = near != null ? AssetDatabase.GetAssetPath(near) : null;

            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(terrain.terrainData);

            return string.IsNullOrEmpty(path) ? "Assets" : Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        }

        public static void UseLayerShader(HBStyleTerrain owner)
        {
            var terrain = owner.GetComponent<Terrain>();
            var shader = Shader.Find(HBStyleTerrain.ShaderName);

            if (terrain == null || shader == null)
                return;

            var source = terrain.materialTemplate;
            var material = source != null ? new Material(source) : new Material(shader);
            material.shader = shader;
            material.name = $"{terrain.name} HB Terrain";

            TerrainShaderGUI.Validate(material);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{Folder(terrain, owner.profile)}/{material.name}.mat");
            AssetDatabase.CreateAsset(material, path);

            Assign(owner, terrain, material, "Use HB Terrain Shader");
        }

        public static void UseMaterial(HBStyleTerrain owner, Material material)
        {
            var terrain = owner.GetComponent<Terrain>();

            if (terrain != null && material != null)
                Assign(owner, terrain, material, "Use HB Terrain Material");
        }

        static void Assign(HBStyleTerrain owner, Terrain terrain, Material material, string undo)
        {
            Undo.RecordObject(terrain, undo);
            terrain.materialTemplate = material;
            terrain.basemapDistance = HBStyleTerrain.BasemapDistance;
            EditorUtility.SetDirty(terrain);

            StyleTerrainArrays.Ensure(owner);
            owner.Refresh();
        }
    }
}
