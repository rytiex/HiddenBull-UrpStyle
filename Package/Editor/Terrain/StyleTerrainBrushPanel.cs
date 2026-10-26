using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainBrushPanel
    {
        const float PreviewSize = 60f;
        const float ShapeCell = 36f;
        const float SizeLeast = 1f;
        const float SizeMost = 500f;

        static readonly StyleTerrainBrushTool[] s_ShapeTools =
        {
            StyleTerrainBrushTool.Raise, StyleTerrainBrushTool.Smooth, StyleTerrainBrushTool.Flatten,
            StyleTerrainBrushTool.SetHeight, StyleTerrainBrushTool.Noise, StyleTerrainBrushTool.Terrace
        };

        static readonly string[] s_Titles =
        {
            "Raise / Lower", "Smooth", "Flatten", "Set Height", "Holes", "Paint Layer", "Noise", "Terrace"
        };

        static readonly string[] s_Help =
        {
            "Drag over the terrain to raise it.",
            "Drag over the terrain to soften bumps, ridges and steps.",
            "Press on the ground to take its height, then drag to flatten the ground toward it.",
            "Drag to pull the ground toward Height.",
            "Drag to cut holes in the ground.",
            "Drag to paint this layer over the rules. The preview shows it at once; Bake keeps it.",
            "Drag to add natural bumps and dips; going over the same place again grows the same shapes.",
            "Drag to pull the ground into flat steps of Step Height with steep but unbroken edges."
        };

        static readonly string[][] s_Hints =
        {
            new[] { "Shift", "lower" },
            new string[0],
            new[] { "Click", "take height" },
            new[] { "Shift + Click", "take ground height" },
            new[] { "Shift", "fill" },
            new[] { "Shift", "remove hand paint" },
            new[] { "Shift", "turn over" },
            new string[0]
        };

        static readonly string[] s_Sizing = { "[  ]", "brush size" };
        static readonly string[] s_Forcing = { "-  =", "strength" };
        static readonly string[] s_Stopping = { "Esc", "put away" };

        static readonly GUIContent[] s_ShapeNames =
        {
            new GUIContent("Raise", "Raise the ground; hold Shift to lower it."),
            new GUIContent("Smooth", "Soften bumps, ridges and steps."),
            new GUIContent("Flatten", "Flatten toward the height where you press."),
            new GUIContent("Set Height", "Pull the ground toward Height."),
            new GUIContent("Noise", "Add natural bumps and dips."),
            new GUIContent("Terrace", "Pull the ground into flat steps.")
        };

        static readonly GUIContent s_HolesName = new GUIContent("Holes", "Cut holes in the ground; Shift fills them.");

        static readonly GUIContent s_SizeLabel = new GUIContent("Size", "Brush diameter, in metres. [ and ] in the " +
                                                                        "Scene view change it.");

        static readonly GUIContent s_StrengthLabel = new GUIContent("Strength",
            "How much each touch of the brush changes the ground. - and = in the Scene view change it.");

        static readonly GUIContent s_HardnessLabel = new GUIContent("Hardness",
            "Share of the brush that works at full strength; the rest fades out to the edge.");

        static readonly GUIContent s_RotationLabel = new GUIContent("Rotation",
            "Turns the brush shape, in degrees. The line on the cursor shows the shape's top.");

        static readonly GUIContent s_TargetLabel = new GUIContent("Height",
            "Height Set Height pulls the ground to, in metres above the terrain's base.");

        static readonly GUIContent s_NoiseSizeLabel = new GUIContent("Noise Size",
            "Size of the bumps the Noise brush makes, in metres.");

        static readonly GUIContent s_StepLabel = new GUIContent("Step Height",
            "Height of each terrace step, in metres.");

        static readonly GUIContent s_RefreshLabel = new GUIContent("Refresh",
            "Looks for Brush assets added to the project since the list was built.");

        static readonly GUIContent s_ClearHandLabel = new GUIContent("Clear Hand Painting",
            "Removes everything painted by hand on this terrain, so only the rules place the layers. Undo brings " +
            "it back.");

        static readonly List<string> s_Names = new List<string>();

        static Color tint => surface ? StyleTerrainGUI.surfaceTint : StyleTerrainGUI.shapeTint;

        static bool surface => StyleTerrainBrush.tool == StyleTerrainBrushTool.Holes ||
                               StyleTerrainBrush.tool == StyleTerrainBrushTool.PaintLayer;

        internal static void Guide(HBStyleTerrainData data)
        {
            if (!StyleTerrainBrush.armed)
            {
                StyleTerrainGUI.Guide(StyleTerrainGUI.settingsTint, "Brushes",
                                      "Pick a tool below, then work in the Scene view. Click the tool again or " +
                                      "press Esc to put it away and select objects again.", null);
                return;
            }

            var tool = StyleTerrainBrush.tool;
            var index = (int)tool;
            var title = s_Titles[index];

            if (tool == StyleTerrainBrushTool.PaintLayer)
                title = "Paint " + Caption(data, StyleTerrainBrush.paintLayer);

            StyleTerrainGUI.Guide(tint, title, s_Help[index], tool, s_Hints[index], s_Sizing,
                                  tool == StyleTerrainBrushTool.Holes ? null : s_Forcing, s_Stopping);
        }

        internal static void Draw(HBStyleTerrainData data)
        {
            DrawShapePalette();
            DrawSurfacePalette(data);

            if (StyleTerrainBrush.armed)
            {
                DrawBrush();

                if (StyleTerrainBrush.tool != StyleTerrainBrushTool.Holes)
                    DrawShapes();
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(data.hand == null))
            {
                if (GUILayout.Button(s_ClearHandLabel, EditorStyles.miniButton) &&
                    EditorUtility.DisplayDialog("Clear Hand Painting",
                                                "Remove everything painted by hand on this terrain? Undo brings it " +
                                                "back.", "Clear", "Cancel"))
                {
                    StyleTerrainPainter.Clear(data);
                    StyleTerrainPreviewer.Restart(data);
                }
            }
        }

        static void DrawShapePalette()
        {
            StyleTerrainGUI.Header("Shape", StyleTerrainGUI.shapeTint);

            var row = StyleTerrainGUI.TileRow();

            for (var index = 0; index < s_ShapeTools.Length; index++)
            {
                if (index > 0 && index % StyleTerrainGUI.columns == 0)
                    row = StyleTerrainGUI.TileRow();

                var rect = StyleTerrainGUI.Slot(row, index);
                var picked = s_ShapeTools[index];
                var active = StyleTerrainBrush.armed && StyleTerrainBrush.tool == picked;

                if (StyleTerrainGUI.Tile(rect, active, StyleTerrainGUI.shapeTint, s_ShapeNames[index]))
                    Pick(picked, null, active);

                if (Event.current.type != EventType.Repaint)
                    continue;

                var side = Mathf.Min(rect.width - 8f, StyleTerrainGUI.Picture(rect).height);

                Handles.color = active ? Color.white : StyleTerrainGUI.shapeTint;
                StyleTerrainGUI.Glyph(picked, new Rect(rect.center.x - side * 0.5f, rect.y + 4f, side, side));
            }
        }

        static void DrawSurfacePalette(HBStyleTerrainData data)
        {
            StyleTerrainGUI.Header("Surface", StyleTerrainGUI.surfaceTint);

            s_Names.Clear();

            for (var index = 0; index < data.layerCount; index++)
            {
                var layer = data.layers[index].layer;
                s_Names.Add(layer != null ? layer.name : null);
            }

            var row = StyleTerrainGUI.TileRow();
            var holes = StyleTerrainBrush.armed && StyleTerrainBrush.tool == StyleTerrainBrushTool.Holes;
            var holesRect = StyleTerrainGUI.Slot(row, 0);

            if (StyleTerrainGUI.Tile(holesRect, holes, StyleTerrainGUI.surfaceTint, s_HolesName))
                Pick(StyleTerrainBrushTool.Holes, null, holes);

            if (Event.current.type == EventType.Repaint)
            {
                var side = Mathf.Min(holesRect.width - 8f, StyleTerrainGUI.Picture(holesRect).height);

                Handles.color = holes ? Color.white : StyleTerrainGUI.surfaceTint;
                StyleTerrainGUI.Glyph(StyleTerrainBrushTool.Holes,
                                      new Rect(holesRect.center.x - side * 0.5f, holesRect.y + 4f, side, side));
            }

            for (var index = 0; index < data.layerCount; index++)
            {
                var slot = index + 1;

                if (slot % StyleTerrainGUI.columns == 0)
                    row = StyleTerrainGUI.TileRow();

                var rect = StyleTerrainGUI.Slot(row, slot);
                var layer = data.layers[index].layer;
                var active = StyleTerrainBrush.armed && StyleTerrainBrush.tool == StyleTerrainBrushTool.PaintLayer &&
                             layer != null && StyleTerrainBrush.paintLayer == layer;
                var content = new GUIContent(StyleTerrainGUI.Caption(s_Names, index),
                                             (layer != null ? layer.name : "No layer") +
                                             "\nPaint this layer over the rules; Shift removes hand painting.");

                using (new EditorGUI.DisabledScope(layer == null))
                {
                    if (StyleTerrainGUI.Tile(rect, active, StyleTerrainGUI.surfaceTint, content))
                        Pick(StyleTerrainBrushTool.PaintLayer, layer, active);
                }

                if (Event.current.type != EventType.Repaint)
                    continue;

                var picture = StyleTerrainGUI.Picture(rect);

                if (layer != null && layer.albedo != null)
                    GUI.DrawTexture(picture, layer.albedo, ScaleMode.ScaleAndCrop);
                else
                    EditorGUI.DrawRect(picture, layer != null ? layer.tint : new Color(0.3f, 0.3f, 0.3f));

                EditorGUI.DrawRect(new Rect(picture.x, picture.y, picture.width, 3f),
                                   StyleTerrainLayerList.Tint(index));
            }

            if (data.layerCount == 0)
                EditorGUILayout.HelpBox("Add layers on the Layers tab to paint them here.", MessageType.None);
        }

        static void Pick(StyleTerrainBrushTool picked, HBStyleTerrainLayer layer, bool active)
        {
            if (active)
            {
                StyleTerrainBrush.armed = false;
            }
            else
            {
                StyleTerrainBrush.tool = picked;
                StyleTerrainBrush.armed = true;

                if (layer != null)
                    StyleTerrainBrush.paintLayer = layer;
            }

            SceneView.RepaintAll();
        }

        static void DrawBrush()
        {
            var tool = StyleTerrainBrush.tool;
            var holes = tool == StyleTerrainBrushTool.Holes;
            var round = holes || StyleTerrainShapes.round;

            StyleTerrainGUI.Header("Brush", tint);

            using (new EditorGUILayout.HorizontalScope())
            {
                var preview = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize));

                if (Event.current.type == EventType.Repaint)
                    DrawPreview(preview, round);

                var width = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 80f;

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    var size = EditorGUILayout.Slider(s_SizeLabel, StyleTerrainBrush.size, SizeLeast, SizeMost);
                    var strength = StyleTerrainBrush.strength;
                    var hardness = StyleTerrainBrush.hardness;
                    var rotation = StyleTerrainShapes.rotation;
                    var height = StyleTerrainBrush.height;
                    var noise = StyleTerrainBrush.noiseSize;
                    var step = StyleTerrainBrush.terraceStep;

                    if (!holes)
                        strength = EditorGUILayout.Slider(s_StrengthLabel, strength, 0f, 1f);

                    if (round)
                        hardness = EditorGUILayout.Slider(s_HardnessLabel, hardness, 0f, 0.99f);
                    else
                        rotation = EditorGUILayout.Slider(s_RotationLabel, rotation, 0f, 360f);

                    switch (tool)
                    {
                        case StyleTerrainBrushTool.SetHeight:
                            height = EditorGUILayout.FloatField(s_TargetLabel, height);
                            break;

                        case StyleTerrainBrushTool.Noise:
                            noise = EditorGUILayout.Slider(s_NoiseSizeLabel, noise, 0.5f, 200f);
                            break;

                        case StyleTerrainBrushTool.Terrace:
                            step = EditorGUILayout.FloatField(s_StepLabel, step);
                            break;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        StyleTerrainBrush.size = size;
                        StyleTerrainBrush.strength = strength;
                        StyleTerrainBrush.hardness = hardness;
                        StyleTerrainBrush.height = height;
                        StyleTerrainBrush.noiseSize = noise;
                        StyleTerrainBrush.terraceStep = step;
                        StyleTerrainShapes.rotation = rotation;
                        SceneView.RepaintAll();
                    }
                }

                EditorGUIUtility.labelWidth = width;
            }
        }

        static void DrawPreview(Rect rect, bool round)
        {
            var colour = tint;
            var share = Mathf.InverseLerp(Mathf.Log(SizeLeast), Mathf.Log(SizeMost), Mathf.Log(StyleTerrainBrush.size));
            var radius = Mathf.Lerp(5f, PreviewSize * 0.46f, share);
            var strength = StyleTerrainBrush.tool == StyleTerrainBrushTool.Holes ? 1f : StyleTerrainBrush.strength;

            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
            StyleTerrainGUI.Frame(rect, new Color(1f, 1f, 1f, 0.12f));

            if (round)
            {
                Handles.color = new Color(colour.r, colour.g, colour.b, 0.12f + 0.25f * strength);
                Handles.DrawSolidDisc(rect.center, Vector3.forward, radius);
                Handles.color = new Color(colour.r, colour.g, colour.b, 0.2f + 0.6f * strength);
                Handles.DrawSolidDisc(rect.center, Vector3.forward, radius * StyleTerrainBrush.hardness);
                Handles.color = colour;
                Handles.DrawWireDisc(rect.center, Vector3.forward, radius);
                return;
            }

            var shape = StyleTerrainShapes.contents[StyleTerrainShapes.selectedIndex].image;
            var area = new Rect(rect.center.x - radius, rect.center.y - radius, radius * 2f, radius * 2f);
            var matrix = GUI.matrix;
            var previous = GUI.color;

            GUIUtility.RotateAroundPivot(-StyleTerrainShapes.rotation, rect.center);
            GUI.color = new Color(colour.r, colour.g, colour.b, 0.35f + 0.65f * strength);
            GUI.DrawTexture(area, shape, ScaleMode.StretchToFill, true);
            GUI.color = previous;
            GUI.matrix = matrix;
        }

        static void DrawShapes()
        {
            StyleTerrainGUI.Header("Brush Shape", tint);

            var contents = StyleTerrainShapes.contents;
            var columns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 48f) / ShapeCell));
            var rows = Mathf.CeilToInt(contents.Length / (float)columns);
            var selected = StyleTerrainShapes.selectedIndex;
            var picked = GUILayout.SelectionGrid(selected, contents, columns, GUILayout.Height(rows * ShapeCell));

            if (picked != selected)
            {
                StyleTerrainShapes.selectedIndex = picked;
                SceneView.RepaintAll();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(s_RefreshLabel, EditorStyles.miniButton, GUILayout.Width(60f)))
                    StyleTerrainShapes.Refresh();
            }
        }

        static string Caption(HBStyleTerrainData data, HBStyleTerrainLayer layer)
        {
            s_Names.Clear();

            var found = -1;

            for (var index = 0; index < data.layerCount; index++)
            {
                var other = data.layers[index].layer;

                s_Names.Add(other != null ? other.name : null);

                if (other != null && other == layer)
                    found = index;
            }

            return found >= 0 ? StyleTerrainGUI.Caption(s_Names, found) : "Layer";
        }
    }
}
