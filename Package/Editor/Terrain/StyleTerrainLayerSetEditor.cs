using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleTerrainLayerSet))]
    sealed class StyleTerrainLayerSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var set = (HBStyleTerrainLayerSet)target;

            EditorGUILayout.HelpBox("Ground layers and their rules, shared by every terrain that uses this set. " +
                                    "Edit them on an HB Style Terrain component's Layers tab.", MessageType.Info);

            for (var index = 0; index < set.layers.Count; index++)
            {
                var layer = set.layers[index].layer;

                using (new EditorGUILayout.HorizontalScope())
                {
                    var swatch = GUILayoutUtility.GetRect(4f, 18f, GUILayout.Width(4f));

                    EditorGUI.DrawRect(swatch, StyleTerrainLayerList.Tint(index));
                    EditorGUILayout.LabelField(layer != null ? layer.name : "(no layer)");
                }
            }

            StyleTerrainSelect.Buttons(terrain => terrain.data.layerSet == set);
        }
    }
}
