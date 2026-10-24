using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    [FilePath("ProjectSettings/HiddenBullUrpStyle.asset", FilePathAttribute.Location.ProjectFolder)]
    sealed class StyleBrushAnchorSettings : ScriptableSingleton<StyleBrushAnchorSettings>
    {
        [SerializeField]
        [Tooltip("Write bind-pose positions into UV3 when a skinned mesh is imported.")]
        bool m_BakeBrushAnchors = true;

        [SerializeField]
        [Tooltip("Set once the skinned meshes already in the project have been reimported with anchors.")]
        bool m_AnchoredExistingMeshes;

        internal bool bakeBrushAnchors
        {
            get => m_BakeBrushAnchors;
            set
            {
                if (m_BakeBrushAnchors == value)
                    return;

                m_BakeBrushAnchors = value;
                Save(true);
            }
        }

        internal bool anchoredExistingMeshes
        {
            get => m_AnchoredExistingMeshes;
            set
            {
                if (m_AnchoredExistingMeshes == value)
                    return;

                m_AnchoredExistingMeshes = value;
                Save(true);
            }
        }

        [InitializeOnLoadMethod]
        static void AnchorExistingMeshes()
        {
            EditorApplication.delayCall += () =>
            {
                var settings = instance;

                if (!settings.bakeBrushAnchors || settings.anchoredExistingMeshes)
                    return;

                settings.anchoredExistingMeshes = true;
                StyleBrushAnchorPostprocessor.ReimportSkinnedModels();
            };
        }
    }

    sealed class StyleBrushAnchorPostprocessor : AssetPostprocessor
    {
        const int Channel = 3;

        public override uint GetVersion() => 1;

        void OnPostprocessModel(GameObject root)
        {
            if (!StyleBrushAnchorSettings.instance.bakeBrushAnchors)
                return;

            var done = new HashSet<Mesh>();

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;

                if (mesh == null || !done.Add(mesh))
                    continue;

                if (mesh.HasVertexAttribute(VertexAttribute.TexCoord3))
                {
                    context.LogImportWarning($"'{mesh.name}' already uses UV3, so no brush anchor was written. " +
                                             "Brush strokes will follow Object space as it animates.", root);
                    continue;
                }

                var rest = new List<Vector3>(mesh.vertexCount);
                mesh.GetVertices(rest);
                mesh.SetUVs(Channel, rest);
            }
        }

        internal static string Scan()
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

        internal static void ReimportSkinnedModels()
        {
            var reimported = 0;

            foreach (var path in SkinnedModelPaths())
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                reimported++;
            }

            Debug.Log(reimported > 0
                ? $"[HB URP Style] Wrote brush anchors into {reimported} model(s) with skinned meshes."
                : "[HB URP Style] Found no imported models with skinned meshes.");
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

    static class StyleBrushAnchorSettingsProvider
    {
        const string Explanation =
            "A skinned mesh has no stable coordinate to anchor brush strokes to: object space is tied to the " +
            "transform rather than the surface, so skinning drags every point through it and the strokes swim.\n\n" +
            "With this on, imported skinned meshes get their bind-pose positions written into UV3, which " +
            "skinning cannot move. Materials set to Rest Pose read it; a mesh without it falls back to Object " +
            "space.\n\n" +
            "It costs 12 bytes per vertex on skinned meshes only, and is skipped on any mesh that already uses UV3.";

        static string s_Status;

        [SettingsProvider]
        static SettingsProvider Create()
        {
            return new SettingsProvider("Project/HiddenBull URP Style", SettingsScope.Project)
            {
                label = "HiddenBull URP Style",
                keywords = new[] { "HiddenBull", "brush", "anchor", "skinned", "style" },
                activateHandler = (_, _) => s_Status = StyleBrushAnchorPostprocessor.Scan(),
                deactivateHandler = () => s_Status = null,
                guiHandler = _ =>
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(Explanation, MessageType.None);
                    EditorGUILayout.Space();

                    var settings = StyleBrushAnchorSettings.instance;

                    settings.bakeBrushAnchors = EditorGUILayout.Toggle(
                        new GUIContent("Bake Brush Anchors",
                                       "Write bind-pose positions into UV3 when a skinned mesh is imported."),
                        settings.bakeBrushAnchors);

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(s_Status ?? string.Empty, MessageType.Info);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("Reimport Skinned Meshes", GUILayout.Width(200f)))
                    {
                        StyleBrushAnchorPostprocessor.ReimportSkinnedModels();
                        s_Status = StyleBrushAnchorPostprocessor.Scan();
                    }
                }
            };
        }
    }
}
