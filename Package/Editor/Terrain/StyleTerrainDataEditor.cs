using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleTerrainData))]
    sealed class StyleTerrainDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var data = (HBStyleTerrainData)target;

            EditorGUILayout.HelpBox("Stores an HB Style Terrain: heights, holes, layer maps and settings. Edit it " +
                                    "on the HB Style Terrain component that uses it.", MessageType.Info);

            if (data.terrainData != null)
            {
                var size = data.size;

                EditorGUILayout.LabelField("Size", $"{size.x:0} × {size.z:0} m, {size.y:0} m high, " +
                                                   $"{data.resolution}² samples");
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Layer Set", data.layerSet, typeof(HBStyleTerrainLayerSet), false);

            StyleTerrainSelect.Buttons(terrain => terrain.data == data);
        }
    }

    static class StyleTerrainSelect
    {
        internal static void Buttons(System.Func<HBStyleTerrain, bool> uses)
        {
            foreach (var terrain in HBStyleTerrain.active)
            {
                if (terrain.data == null || !uses(terrain))
                    continue;

                if (GUILayout.Button($"Select {terrain.name}"))
                    Selection.activeObject = terrain.gameObject;
            }
        }
    }
}
