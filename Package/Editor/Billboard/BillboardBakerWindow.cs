using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class BillboardBakerWindow : EditorWindow
    {
        static class Styles
        {
            public static readonly GUIContent Target = new GUIContent("Target",
                "The plant to bake: an object in the scene or a prefab in the Project window. A prefab is baked " +
                "in place, so every copy of it updates. The plant is drawn up close as its meshes and far away " +
                "as the billboard, all copies together on the GPU.");

            public static readonly GUIContent Switch = new GUIContent("Billboard From (m)",
                "Distance from the camera where the plant turns into its billboard, for a 60 degree field of " +
                "view. Zooming in pushes it further away.");

            public static readonly GUIContent Cull = new GUIContent("Hide After (m)",
                "Distance from the camera where the billboard disappears, for a 60 degree field of view.");

            public static readonly GUIContent Folder = new GUIContent("Folder",
                "Where the billboard textures, mesh, material and data are saved.");

            public static readonly GUIContent Name = new GUIContent("Name",
                "Base name of the saved files: TEX_Billboard_<Name>_Albedo, TEX_Billboard_<Name>_Normal, " +
                "MESH_Billboard_<Name>, MAT_Billboard_<Name> and DATA_Billboard_<Name>.");
        }

        [SerializeField] GameObject m_Target;
        [SerializeField] float m_Switch;
        [SerializeField] float m_Cull;
        [SerializeField] string m_Folder;
        [SerializeField] string m_Name;

        [MenuItem("Tools/HiddenBull/URP Style/Billboard Baker")]
        static void OpenWindow()
        {
            Open(Selection.activeGameObject);
        }

        public static void Open(GameObject target)
        {
            var window = GetWindow<BillboardBakerWindow>("Billboard Baker");
            window.minSize = new Vector2(340f, 220f);

            if (target != null)
                window.SetTarget(target);
        }

        void OnSelectionChange()
        {
            var selected = Selection.activeGameObject;

            if (selected != null && selected != m_Target)
            {
                SetTarget(selected);
                Repaint();
            }
        }

        void SetTarget(GameObject target)
        {
            if (target != null)
            {
                target = EditorUtility.IsPersistent(target)
                    ? target.transform.root.gameObject
                    : PrefabUtility.GetNearestPrefabInstanceRoot(target) ?? target;
            }

            m_Target = target;

            if (m_Target == null)
                return;

            var options = BillboardBaker.DefaultOptions(m_Target);
            m_Switch = options.switchDistance;
            m_Cull = options.cullDistance;
            m_Folder = options.folder;
            m_Name = options.name;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(4f);

            var target = (GameObject)EditorGUILayout.ObjectField(Styles.Target, m_Target, typeof(GameObject), true);

            if (target != m_Target)
                SetTarget(target);

            if (m_Target == null)
            {
                EditorGUILayout.HelpBox("Pick a plant in the scene or a prefab in the Project window.",
                                        MessageType.None);
                return;
            }

            EditorGUILayout.Space(4f);
            m_Switch = Mathf.Max(0.1f, EditorGUILayout.FloatField(Styles.Switch, m_Switch));
            m_Cull = Mathf.Max(m_Switch + 0.1f, EditorGUILayout.FloatField(Styles.Cull, m_Cull));

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_Folder = EditorGUILayout.TextField(Styles.Folder, m_Folder);

                if (GUILayout.Button("…", GUILayout.Width(28f)))
                {
                    var picked = PickFolder(m_Folder);

                    if (picked != null)
                        m_Folder = picked;

                    GUIUtility.ExitGUI();
                }
            }

            m_Name = EditorGUILayout.TextField(Styles.Name, m_Name);

            var valid = IsProjectFolder(m_Folder) && !string.IsNullOrWhiteSpace(m_Name) &&
                        m_Name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

            if (!valid)
                EditorGUILayout.HelpBox("The folder must be inside Assets and the name a valid file name.",
                                        MessageType.Warning);

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button("Bake", GUILayout.Height(28f)))
                {
                    BillboardBaker.Bake(m_Target, new BillboardBaker.Options
                    {
                        switchDistance = m_Switch,
                        cullDistance = m_Cull,
                        folder = m_Folder.TrimEnd('/'),
                        name = m_Name.Trim()
                    });

                    GUIUtility.ExitGUI();
                }
            }
        }

        static bool IsProjectFolder(string folder)
        {
            return !string.IsNullOrEmpty(folder) && (folder == "Assets" || folder.StartsWith("Assets/"));
        }

        static string PickFolder(string current)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var start = IsProjectFolder(current) ? Path.Combine(root, current) : Application.dataPath;
            var picked = EditorUtility.OpenFolderPanel("Billboard Folder", start, "");

            if (string.IsNullOrEmpty(picked))
                return null;

            var relative = Path.GetRelativePath(root, Path.GetFullPath(picked)).Replace('\\', '/');

            if (IsProjectFolder(relative))
                return relative;

            EditorUtility.DisplayDialog("Billboard Folder", "Pick a folder inside the project's Assets folder.", "OK");
            return null;
        }
    }
}
