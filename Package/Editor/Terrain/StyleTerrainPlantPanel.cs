using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainPlantPanel
    {
        const float SizeRange = 5f;
        const float SizeLeast = 0.05f;
        const float DistanceLeast = 0.25f;
        const float DistanceMost = 50f;

        static readonly GUIContent s_AddLabel = new GUIContent("Add Plant",
            "Drop a prefab here. It needs an HB Style Plant that has been baked with Bake Plant.");

        static readonly GUIContent s_DensityLabel = new GUIContent("Density",
            "Share of the free spots this plant fills. 1 packs it as tight as Min Distance allows.");

        static readonly GUIContent s_DistanceLabel = new GUIContent("Min Distance",
            "Metres kept between plants of this kind. Next to another kind, the smaller of the two distances counts.");

        static readonly GUIContent s_SizeLabel = new GUIContent("Size",
            "Smallest and largest random size, multiplied by the prefab's own scale. Only new plants take it.");

        static readonly GUIContent s_RotationLabel = new GUIContent("Rotation",
            "Random turn around the up axis, in degrees. 0 keeps every plant facing the same way. Only new plants " +
            "take it.");

        static readonly GUIContent s_RemoveLabel = new GUIContent("Remove Plant",
            "Takes this plant off the list and removes every copy of it from the terrain. Undo brings it back.");

        static readonly GUIContent s_ClearLabel = new GUIContent("Clear Plants",
            "Removes every plant painted on this terrain and keeps the list. Undo brings them back.");

        static readonly List<string> s_Names = new List<string>();

        internal static string Caption(HBStyleTerrainData data)
        {
            var plants = data.plants;
            var index = StyleTerrainBrush.plantType;

            if (plants == null || index < 0 || index >= plants.types.Count || plants.types[index].prefab == null)
                return "Plant";

            return plants.types[index].prefab.name;
        }

        internal static void DrawPalette(HBStyleTerrainData data)
        {
            StyleTerrainGUI.Header("Plants", StyleTerrainGUI.plantTint);

            var plants = data.plants;
            var count = plants != null ? plants.types.Count : 0;

            s_Names.Clear();

            for (var index = 0; index < count; index++)
            {
                var prefab = plants.types[index].prefab;

                s_Names.Add(prefab != null ? prefab.name : null);
            }

            var row = count > 0 ? StyleTerrainGUI.TileRow() : default;

            for (var index = 0; index < count; index++)
            {
                if (index > 0 && index % StyleTerrainGUI.columns == 0)
                    row = StyleTerrainGUI.TileRow();

                DrawTile(plants, row, index);
            }

            var added = (GameObject)EditorGUILayout.ObjectField(s_AddLabel, null, typeof(GameObject), false);

            if (added == null)
                return;

            var type = StyleTerrainPlanter.Add(data, added);

            if (type >= 0)
                Pick(type);
        }

        static void DrawTile(HBStyleTerrainPlants plants, Rect row, int index)
        {
            var rect = StyleTerrainGUI.Slot(row, index);
            var type = plants.types[index];
            var active = Active(index);
            var content = new GUIContent(StyleTerrainGUI.Caption(s_Names, index),
                                         (type.prefab != null ? type.prefab.name : "Missing prefab") +
                                         "\nPaint this plant; Shift erases it.");

            if (StyleTerrainGUI.Tile(rect, active, StyleTerrainGUI.plantTint, content))
            {
                if (active)
                    StyleTerrainBrush.armed = false;
                else
                    Pick(index);

                SceneView.RepaintAll();
            }

            if (Event.current.type != EventType.Repaint)
                return;

            var picture = StyleTerrainGUI.Picture(rect);
            var preview = type.prefab != null ? AssetPreview.GetAssetPreview(type.prefab) : null;

            if (preview != null)
                GUI.DrawTexture(picture, preview, ScaleMode.ScaleAndCrop);
            else
                EditorGUI.DrawRect(picture, new Color(0.3f, 0.3f, 0.3f));
        }

        internal static void DrawSettings(HBStyleTerrainData data)
        {
            var plants = data.plants;
            var index = StyleTerrainBrush.plantType;

            if (plants == null || index < 0 || index >= plants.types.Count)
                return;

            var type = plants.types[index];

            StyleTerrainGUI.Header(Caption(data), StyleTerrainGUI.plantTint);

            if (type.asset == null || !type.asset.valid)
                EditorGUILayout.HelpBox("This prefab has no baked HB Style Plant, so its plants are not drawn. " +
                                        "Press Bake Plant on the prefab.", MessageType.Warning);

            EditorGUI.BeginChangeCheck();

            var density = EditorGUILayout.Slider(s_DensityLabel, type.density, 0f, 1f);
            var distance = EditorGUILayout.Slider(s_DistanceLabel, type.minDistance, DistanceLeast, DistanceMost);
            var least = type.sizeLeast;
            var most = type.sizeMost;

            EditorGUILayout.MinMaxSlider(s_SizeLabel, ref least, ref most, SizeLeast, SizeRange);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(" ");
                least = EditorGUILayout.FloatField(least);
                most = EditorGUILayout.FloatField(most);
            }

            var rotation = EditorGUILayout.Slider(s_RotationLabel, type.rotation, 0f, 360f);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(plants, "Change Terrain Plant");
                type.density = density;
                type.minDistance = distance;
                type.sizeMost = Mathf.Max(most, least);
                type.sizeLeast = least;
                type.rotation = rotation;
                EditorUtility.SetDirty(plants);
            }

            EditorGUILayout.LabelField("Placed", StyleTerrainPlanter.Placed(plants, index).ToString());

            if (!GUILayout.Button(s_RemoveLabel, EditorStyles.miniButton) ||
                !EditorUtility.DisplayDialog("Remove Plant",
                                             $"Remove '{Caption(data)}' and every copy of it from this terrain? Undo " +
                                             "brings it back.", "Remove", "Cancel"))
                return;

            StyleTerrainBrush.armed = false;
            StyleTerrainPlanter.Remove(plants, index);
            SceneView.RepaintAll();
            GUIUtility.ExitGUI();
        }

        internal static void DrawClear(HBStyleTerrainData data)
        {
            var plants = data.plants;

            using (new EditorGUI.DisabledScope(plants == null || plants.count == 0))
            {
                if (GUILayout.Button(s_ClearLabel, EditorStyles.miniButton) &&
                    EditorUtility.DisplayDialog("Clear Plants",
                                                "Remove every plant painted on this terrain? The list stays, and " +
                                                "Undo brings them back.", "Clear", "Cancel"))
                {
                    StyleTerrainPlanter.Clear(plants);
                    SceneView.RepaintAll();
                }
            }
        }

        static bool Active(int index)
        {
            return StyleTerrainBrush.armed && StyleTerrainBrush.plantTool && StyleTerrainBrush.plantType == index;
        }

        static void Pick(int index)
        {
            StyleTerrainBrush.tool = StyleTerrainBrushTool.PlantPaint;
            StyleTerrainBrush.plantType = index;
            StyleTerrainBrush.armed = true;
            SceneView.RepaintAll();
        }
    }
}
