using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class BrushAnchorPostprocessor : AssetPostprocessor
    {
        public const int Channel = 3;

        public override uint GetVersion() => 1;

        void OnPostprocessModel(GameObject root)
        {
            if (!BrushAnchorSettings.instance.bakeBrushAnchors)
                return;

            var done = new HashSet<Mesh>();

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;

                if (mesh == null || !done.Add(mesh))
                    continue;

                if (mesh.HasVertexAttribute(VertexAttribute.TexCoord3))
                {
                    context.LogImportWarning(
                        $"'{mesh.name}' already uses UV3, so no brush anchor was written. Brush " +
                        "strokes will slide as it animates.", root);
                    continue;
                }

                var rest = new List<Vector3>(mesh.vertexCount);
                mesh.GetVertices(rest);

                mesh.SetUVs(Channel, rest);
            }
        }

        public static string Scan()
        {
            var total = 0;
            var anchored = 0;

            foreach (var path in SkinnedModelPaths())
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is not Mesh mesh)
                        continue;

                    total++;

                    if (mesh.HasVertexAttribute(VertexAttribute.TexCoord3))
                        anchored++;
                }
            }

            return total == 0
                ? "No imported models with skinned meshes were found in this project."
                : $"{anchored} of {total} skinned mesh(es) carry a brush anchor.";
        }

        public static void ReimportSkinnedModels()
        {
            var reimported = 0;

            foreach (var path in SkinnedModelPaths())
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                reimported++;
            }

            Debug.Log(reimported > 0
                ? $"[HiddenBull URP Style] Wrote brush anchors into {reimported} model(s) with skinned meshes."
                : "[HiddenBull URP Style] Found no imported models with skinned meshes.");
        }

        static IEnumerable<string> SkinnedModelPaths()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetImporter.GetAtPath(path) is not ModelImporter)
                    continue;

                if (HasSkinnedMesh(path))
                    yield return path;
            }
        }

        static bool HasSkinnedMesh(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is GameObject root && root.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                    return true;
            }

            return false;
        }
    }
}
