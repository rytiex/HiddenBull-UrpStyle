using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleTerrain))]
    sealed class StyleTerrainEditor : UnityEditor.Editor
    {
        SerializedProperty m_Profile;
        SerializedProperty m_AutoBake;

        void OnEnable()
        {
            m_Profile = serializedObject.FindProperty("m_Profile");
            m_AutoBake = serializedObject.FindProperty("m_AutoBake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Profile);
            EditorGUILayout.PropertyField(m_AutoBake);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Terrain Layers", GUILayout.Height(26f)))
                StyleTerrainWindow.Show(((HBStyleTerrain)target).GetComponent<Terrain>());

            DrawTrees((HBStyleTerrain)target);
        }

        static void DrawTrees(HBStyleTerrain owner)
        {
            var terrain = owner.GetComponent<Terrain>();
            var prototypes = terrain != null && terrain.terrainData != null
                ? terrain.terrainData.treePrototypes
                : null;

            if (prototypes == null || prototypes.Length == 0)
                return;

            var baked = 0;

            foreach (var prototype in prototypes)
            {
                if (Baked(prototype.prefab))
                    baked++;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Trees", EditorStyles.boldLabel);

            if (baked > 0 && baked < prototypes.Length)
                EditorGUILayout.HelpBox("Baked trees are drawn by the GPU billboard system, which turns off the " +
                                        "terrain's own tree drawing. Trees that are not baked are not drawn until " +
                                        "you bake them.", MessageType.Warning);
            else if (baked == 0)
                EditorGUILayout.HelpBox("Bake a tree to draw this terrain's trees with the GPU billboard system: " +
                                        "meshes up close, billboards far away.", MessageType.None);

            foreach (var prototype in prototypes)
            {
                var prefab = prototype.prefab;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);

                    var ready = Baked(prefab);

                    GUILayout.Label(ready ? "GPU" : "Not baked", GUILayout.Width(70f));

                    using (new EditorGUI.DisabledScope(prefab == null))
                    {
                        if (GUILayout.Button(ready ? "Rebake" : "Bake", GUILayout.Width(60f)))
                            BillboardBakerWindow.Open(prefab);
                    }
                }
            }
        }

        static bool Baked(GameObject prefab)
        {
            return prefab != null && prefab.TryGetComponent<HBStyleBillboard>(out var billboard) &&
                   billboard.asset != null && billboard.asset.valid;
        }
    }
}
