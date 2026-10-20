using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [FilePath("ProjectSettings/HiddenBullUrpStyle.asset", FilePathAttribute.Location.ProjectFolder)]
    sealed class BrushAnchorSettings : ScriptableSingleton<BrushAnchorSettings>
    {
        [SerializeField]
        bool m_BakeBrushAnchors = true;

        [SerializeField]
        bool m_AnchoredExistingMeshes;

        public bool bakeBrushAnchors
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

        public bool anchoredExistingMeshes
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
                BrushAnchorPostprocessor.ReimportSkinnedModels();
            };
        }
    }

    static class BrushAnchorSettingsProvider
    {
        const string k_Explanation =
            "A skinned mesh has no stable coordinate to anchor brush strokes to: object space is " +
            "tied to the transform rather than the surface, so skinning drags every point through " +
            "it and the strokes swim across the character.\n\n" +
            "With this on, imported skinned meshes get their bind-pose positions written into UV3, " +
            "which skinning cannot move. Materials need no setting — the Lit shader uses the " +
            "anchor when the mesh carries one and falls back to object space when it does not, so " +
            "a material shared between a character and a crate keeps working on both.\n\n" +
            "It costs 12 bytes per vertex on skinned meshes only, and is skipped on any mesh that " +
            "already uses UV3.";

        static string s_Status;

        [SettingsProvider]
        static SettingsProvider Create()
        {
            return new SettingsProvider("Project/HiddenBull URP Style", SettingsScope.Project)
            {
                label = "HiddenBull URP Style",
                keywords = new[] { "HiddenBull", "brush", "anchor", "skinned", "style" },

                activateHandler = (_, _) => s_Status = BrushAnchorPostprocessor.Scan(),
                deactivateHandler = () => s_Status = null,

                guiHandler = _ =>
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(k_Explanation, MessageType.None);
                    EditorGUILayout.Space();

                    var settings = BrushAnchorSettings.instance;

                    settings.bakeBrushAnchors = EditorGUILayout.Toggle(
                        new GUIContent("Bake Brush Anchors",
                            "Write bind-pose positions into UV3 when a skinned mesh is imported."),
                        settings.bakeBrushAnchors);

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(s_Status ?? string.Empty, MessageType.Info);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("Reimport Skinned Meshes", GUILayout.Width(200f)))
                    {
                        BrushAnchorPostprocessor.ReimportSkinnedModels();
                        s_Status = BrushAnchorPostprocessor.Scan();
                    }
                }
            };
        }
    }
}
