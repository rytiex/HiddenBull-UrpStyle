using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainLiquidPanel
    {
        const float PreviewSize = 60f;
        const int SwatchStrips = 8;

        static readonly StyleTerrainLiquidTool[] s_LevelTools =
        {
            StyleTerrainLiquidTool.Raise, StyleTerrainLiquidTool.SetHeight, StyleTerrainLiquidTool.Smooth,
            StyleTerrainLiquidTool.Lake, StyleTerrainLiquidTool.Ramp, StyleTerrainLiquidTool.NoLiquid
        };

        static readonly GUIContent[] s_LevelNames =
        {
            new GUIContent("Raise", "Paint to raise the liquid. Hold Shift to lower it."),
            new GUIContent("Set Height", "Paint to bring the liquid to Height."),
            new GUIContent("Smooth", "Paint to even out steps and bumps in the level."),
            new GUIContent("Lake", "Click a hollow to fill it, then drag the arrow to set how high the liquid stands."),
            new GUIContent("Ramp", "Click along a river bed to lay an even slope as wide as the brush."),
            new GUIContent("No Liquid", "Cut the liquid out and leave the ground dry. Hold Shift to bring it back.")
        };

        static readonly StyleTerrainLiquidTool[] s_FlowTools =
        {
            StyleTerrainLiquidTool.Flow, StyleTerrainLiquidTool.SmoothFlow, StyleTerrainLiquidTool.AutoFlow,
            StyleTerrainLiquidTool.Whirlpool
        };

        static readonly GUIContent[] s_FlowNames =
        {
            new GUIContent("Flow", "Drag to make the liquid flow that way. Hold Shift to stop the flow."),
            new GUIContent("Smooth Flow", "Paint to even out the painted directions and speeds."),
            new GUIContent("Auto Flow", "Paint to make the liquid flow downhill, faster where it is steeper."),
            new GUIContent("Whirlpool", "Click to stamp a whirlpool as wide as the brush.")
        };

        static readonly StyleTerrainLiquidTool[] s_WaveTools = { StyleTerrainLiquidTool.Calm };

        static readonly GUIContent[] s_WaveNames =
        {
            new GUIContent("Calm", "Paint to calm the waves. Hold Shift to bring them back.")
        };

        static readonly GUIContent[] s_ClearNames =
        {
            new GUIContent("Level", "Makes the liquid flat again everywhere."),
            new GUIContent("Liquids", "Paints the sea liquid back everywhere."),
            new GUIContent("No Liquid", "Brings the liquid back everywhere it was painted away."),
            new GUIContent("Flow", "Stops the flow everywhere."),
            new GUIContent("Calm", "Brings the waves back everywhere they were calmed.")
        };

        static readonly string[] s_Titles =
        {
            "Raise / Lower", "Set Height", "Smooth", "Lake", "Ramp", "Paint Liquid", "Flow", "Smooth Flow",
            "Auto Flow", "Whirlpool", "Calm Waves", "No Liquid"
        };

        static readonly string[] s_Help =
        {
            "Paint to raise the liquid level under the brush.",
            "Paint to bring the liquid to the height in the Height field.",
            "Paint to even out steps and bumps in the level.",
            "Click a hollow to fill it. The liquid shows the lake while you drag the arrow to set how high it " +
            "stands. Fill floods the hollow; Circle and Square stay inside their own outline. Nothing is kept " +
            "until you lay it.",
            "Click along a river bed to lay an even slope. The liquid shows the result while you place and drag " +
            "the points; nothing is kept until you lay it. Flow Speed makes the liquid run down the slope.",
            "Paint the selected liquid over the others. Painting the sea liquid paints the others away.",
            "Drag in the direction the liquid should flow. Debug > Liquid/Flow shows how fast it flows.",
            "Paint to even out the painted directions and speeds.",
            "Paint to make the liquid flow down its own slope. Flat liquid stays still and steep liquid " +
            "reaches the full Speed.",
            "Click to stamp a whirlpool: the liquid turns around the point and dips in the middle. Strength " +
            "sets how deep it dips.",
            "Paint to calm the waves under the brush, for a sheltered bay or a still lake. Floating objects " +
            "follow the calmer surface too.",
            "Cut the liquid out under the brush and leave the ground dry, with a sharp edge like a hole in the " +
            "terrain. Hold Shift to bring the liquid back."
        };

        static readonly string[][] s_Hints =
        {
            new[] { "Shift", "lower" },
            new[] { "Shift + Click", "pick ground height" },
            new string[0],
            new[] { "Click", "place", "Drag", "move, height or size", "Enter", "lay" },
            new[]
            {
                "Click", "add point", "Shift + Click", "keep liquid level", "Drag", "move, height or width",
                "Enter", "lay", "Backspace", "remove last"
            },
            new string[0],
            new[] { "Shift", "stop flow" },
            new string[0],
            new string[0],
            new[] { "Click", "stamp", "Shift + Click", "turn the other way" },
            new[] { "Shift", "bring waves back" },
            new[] { "Shift", "bring liquid back" }
        };

        static readonly string[] s_Sizing = { "[  ]", "brush size" };
        static readonly string[] s_Forcing = { "-  =", "strength" };
        static readonly string[] s_Speeding = { "-  =", "speed" };
        static readonly string[] s_Stopping = { "Esc", "stop" };

        static readonly GUIContent s_SizeLabel = new GUIContent("Brush Size", "The width of the brush, in metres.");

        static readonly GUIContent s_StrengthLabel = new GUIContent(
            "Strength", "How fast the brush works. Low values build up slowly over several strokes.");

        static readonly GUIContent s_SpeedLabel = new GUIContent(
            "Speed", "How fast the painted liquid flows, in metres per second.");

        static readonly GUIContent s_CourseLabel = new GUIContent(
            "Flow Speed", "How fast the liquid flows down the ramp, in metres per second. 0 leaves the flow as it is.");

        static readonly GUIContent s_DepthLabel = new GUIContent(
            "Depth", "How far above the ground under each clicked point the liquid sits, in metres.");

        static readonly GUIContent s_HeightLabel = new GUIContent(
            "Height", "The height Set Height brings the liquid to, in metres above the bottom of the terrain.");

        static readonly GUIContent[] s_Forms =
        {
            new GUIContent("Fill", "Floods the hollow under the point. It spills over if the level is above the rim."),
            new GUIContent("Circle", "A round lake of the size you set. It never spreads past its outline."),
            new GUIContent("Square", "A square lake of the size you set. It never spreads past its outline.")
        };

        static readonly GUIContent s_LakeLabel = new GUIContent(
            "Lay Lake", "Keeps the lake as shown. The same as pressing Enter in the Scene view.");

        static readonly GUIContent s_RampLabel = new GUIContent(
            "Lay Ramp", "Lays the slope through the clicked points. The same as pressing Enter in the Scene view.");

        static readonly GUIContent s_EmptyLabel = new GUIContent(
            "Show Empty Areas",
            "Marks in red, in the Scene view, where nothing flows while a Flow tool is active.");

        internal static void Guide(HBStyleTerrainData data)
        {
            if (!StyleTerrainLiquidBrush.armed || !data.liquidOn)
            {
                StyleTerrainGUI.GuideWith(StyleTerrainGUI.liquidTint, "Liquids",
                                          "Turn the Sea on, add a liquid layer, then pick a tool and paint in the " +
                                          "Scene view. Debug > Liquid/Depth and Liquid/Flow show the result.",
                                          null);
                return;
            }

            var tool = StyleTerrainLiquidBrush.tool;
            System.Action<Rect> glyph = rect => Glyph(tool, rect);
            var lakeFill = tool == StyleTerrainLiquidTool.Lake && !StyleTerrainLiquidBrush.sized;
            var forces = StyleTerrainLiquidBrush.rated ? s_Speeding
                       : tool == StyleTerrainLiquidTool.Lake ? null
                       : s_Forcing;

            StyleTerrainGUI.GuideWith(StyleTerrainLiquidBrush.tint, Title(data), s_Help[(int)tool],
                                      tool == StyleTerrainLiquidTool.Paint ? null : glyph,
                                      s_Hints[(int)tool], lakeFill ? null : s_Sizing, forces, s_Stopping);
        }

        internal static string Title(HBStyleTerrainData data)
        {
            var tool = StyleTerrainLiquidBrush.tool;

            if (tool != StyleTerrainLiquidTool.Paint)
                return s_Titles[(int)tool];

            return "Paint " + Name(data, StyleTerrainLiquidBrush.slot);
        }

        internal static void Draw(HBStyleTerrainData data)
        {
            EditorGUI.BeginChangeCheck();

            Palette("Level", StyleTerrainGUI.shapeTint, s_LevelNames, s_LevelTools);
            Swatches(data);
            Palette("Flow", StyleTerrainGUI.layersTint, s_FlowNames, s_FlowTools);
            Palette("Waves", StyleTerrainGUI.lookTint, s_WaveNames, s_WaveTools);

            if (StyleTerrainLiquidBrush.armed)
                Brush(data);

            if (EditorGUI.EndChangeCheck())
                StyleTerrainLiquidBrush.Changed(data);

            EditorGUILayout.Space();
            ClearRow(data);
        }

        static void Pick(StyleTerrainLiquidTool picked, bool active)
        {
            if (active)
            {
                StyleTerrainLiquidBrush.armed = false;
            }
            else
            {
                StyleTerrainLiquidBrush.tool = picked;
                StyleTerrainLiquidBrush.armed = true;
            }

            SceneView.RepaintAll();
        }

        static void Palette(string label, Color tint, GUIContent[] names, StyleTerrainLiquidTool[] tools)
        {
            StyleTerrainGUI.Header(label, tint);

            var row = StyleTerrainGUI.TileRow();

            for (var index = 0; index < names.Length; index++)
            {
                if (index > 0 && index % StyleTerrainGUI.columns == 0)
                    row = StyleTerrainGUI.TileRow();

                var rect = StyleTerrainGUI.Slot(row, index);
                var active = StyleTerrainLiquidBrush.armed && StyleTerrainLiquidBrush.tool == tools[index];

                if (StyleTerrainGUI.Tile(rect, active, tint, names[index]))
                    Pick(tools[index], active);

                if (Event.current.type != EventType.Repaint)
                    continue;

                var picture = StyleTerrainGUI.Picture(rect);
                var side = Mathf.Min(picture.width, picture.height);

                Handles.color = active ? Color.white : new Color(tint.r, tint.g, tint.b, 0.9f);
                Glyph(tools[index], new Rect(picture.center.x - side * 0.5f, picture.y, side, side));
            }
        }

        static void Swatches(HBStyleTerrainData data)
        {
            var tint = StyleTerrainGUI.surfaceTint;
            var count = data.liquidCount;

            StyleTerrainGUI.Header("Liquid", tint);

            var row = StyleTerrainGUI.TileRow();
            var painting = StyleTerrainLiquidBrush.armed &&
                           StyleTerrainLiquidBrush.tool == StyleTerrainLiquidTool.Paint;

            for (var index = 0; index < count; index++)
            {
                var rect = StyleTerrainGUI.Slot(row, index);
                var liquid = data.Liquid(index);
                var active = painting && StyleTerrainLiquidBrush.slot == index;

                using (new EditorGUI.DisabledScope(liquid == null))
                {
                    if (StyleTerrainGUI.Tile(rect, active, tint, Named(data, index)))
                    {
                        Pick(StyleTerrainLiquidTool.Paint, active);
                        StyleTerrainLiquidBrush.slot = index;
                    }
                }

                if (Event.current.type != EventType.Repaint)
                    continue;

                var swatch = StyleTerrainGUI.Picture(rect);

                if (liquid == null)
                {
                    EditorGUI.DrawRect(swatch, new Color(0f, 0f, 0f, 0.2f));
                    continue;
                }

                var gradient = liquid.depthGradient;
                var height = swatch.height / SwatchStrips;

                for (var strip = 0; strip < SwatchStrips; strip++)
                {
                    var colour = gradient.Evaluate(strip / (SwatchStrips - 1f)).gamma;

                    colour = new Color(Mathf.Min(colour.r, 1f), Mathf.Min(colour.g, 1f), Mathf.Min(colour.b, 1f), 1f);
                    EditorGUI.DrawRect(new Rect(swatch.x, swatch.y + strip * height, swatch.width, height + 0.5f),
                                       colour);
                }
            }
        }

        static string Name(HBStyleTerrainData data, int slot)
        {
            var names = new string[data.liquidCount];

            for (var index = 0; index < names.Length; index++)
            {
                var liquid = data.Liquid(index);

                names[index] = liquid != null ? liquid.name : null;
            }

            return slot >= names.Length || names[slot] == null ? "Empty" : StyleTerrainGUI.Caption(names, slot);
        }

        static GUIContent Named(HBStyleTerrainData data, int slot)
        {
            var liquid = data.Liquid(slot);

            if (liquid == null)
                return new GUIContent("Empty", "This entry has no liquid layer.");

            return new GUIContent(Name(data, slot),
                                  liquid.name + (slot == data.seaSlot
                                      ? "\nThe sea liquid. Paints the others away."
                                      : "\nPaints this liquid over the others."));
        }

        static void Brush(HBStyleTerrainData data)
        {
            var tool = StyleTerrainLiquidBrush.tool;
            var tint = StyleTerrainLiquidBrush.tint;

            StyleTerrainGUI.Header("Brush", tint);

            using (new EditorGUILayout.HorizontalScope())
            {
                var preview = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize));

                if (Event.current.type == EventType.Repaint)
                    Preview(preview, tint);

                var width = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 80f;

                using (new EditorGUILayout.VerticalScope())
                {
                    if (tool == StyleTerrainLiquidTool.Lake)
                        StyleTerrainLiquidBrush.form =
                            (StyleTerrainLakeForm)GUILayout.Toolbar((int)StyleTerrainLiquidBrush.form, s_Forms);

                    if (StyleTerrainLiquidBrush.sized)
                        StyleTerrainLiquidBrush.size = EditorGUILayout.Slider(s_SizeLabel, StyleTerrainLiquidBrush.size,
                                                                              StyleTerrainLiquidBrush.SizeLeast,
                                                                              StyleTerrainLiquidBrush.SizeMost);

                    if (StyleTerrainLiquidBrush.forced)
                        StyleTerrainLiquidBrush.strength = EditorGUILayout.Slider(s_StrengthLabel,
                                                                                  StyleTerrainLiquidBrush.strength,
                                                                                  0.01f, 1f);

                    if (tool == StyleTerrainLiquidTool.Ramp)
                        StyleTerrainLiquidBrush.speed = EditorGUILayout.Slider(s_CourseLabel,
                                                                               StyleTerrainLiquidBrush.speed, 0f,
                                                                               StyleTerrainWater.FlowMost);
                    else if (StyleTerrainLiquidBrush.rated)
                        StyleTerrainLiquidBrush.speed = EditorGUILayout.Slider(
                            s_SpeedLabel, Mathf.Max(StyleTerrainLiquidBrush.speed, StyleTerrainWater.FlowStep),
                            StyleTerrainWater.FlowStep, StyleTerrainWater.FlowMost);

                    if (tool == StyleTerrainLiquidTool.SetHeight)
                        StyleTerrainLiquidBrush.height = EditorGUILayout.FloatField(s_HeightLabel,
                                                                                    StyleTerrainLiquidBrush.height);

                    if (StyleTerrainLiquidBrush.streamed)
                        StyleTerrainLiquidBrush.marks = EditorGUILayout.ToggleLeft(s_EmptyLabel,
                                                                                   StyleTerrainLiquidBrush.marks);

                    if (StyleTerrainLiquidBrush.pinned)
                    {
                        StyleTerrainLiquidBrush.depth = EditorGUILayout.FloatField(s_DepthLabel,
                                                                                   StyleTerrainLiquidBrush.depth);

                        using (new EditorGUI.DisabledScope(StyleTerrainLiquidBrush.pins <
                                                           StyleTerrainLiquidBrush.pinsNeeded))
                        {
                            if (GUILayout.Button(tool == StyleTerrainLiquidTool.Lake ? s_LakeLabel : s_RampLabel))
                                StyleTerrainLiquidBrush.Lay(data);
                        }
                    }
                }

                EditorGUIUtility.labelWidth = width;
            }
        }

        static void Preview(Rect preview, Color tint)
        {
            var tool = StyleTerrainLiquidBrush.tool;
            var share = Mathf.InverseLerp(Mathf.Log(StyleTerrainLiquidBrush.SizeLeast),
                                          Mathf.Log(StyleTerrainLiquidBrush.SizeMost),
                                          Mathf.Log(StyleTerrainLiquidBrush.size));
            var radius = StyleTerrainLiquidBrush.sized ? Mathf.Lerp(5f, PreviewSize * 0.46f, share) : 4f;
            var strength = tool == StyleTerrainLiquidTool.Flow || tool == StyleTerrainLiquidTool.AutoFlow
                ? StyleTerrainLiquidBrush.speed / StyleTerrainWater.FlowMost
                : StyleTerrainLiquidBrush.strength;

            EditorGUI.DrawRect(preview, new Color(0f, 0f, 0f, 0.25f));
            StyleTerrainGUI.Frame(preview, new Color(1f, 1f, 1f, 0.12f));
            Handles.color = new Color(tint.r, tint.g, tint.b, 0.12f + 0.25f * strength);
            Handles.DrawSolidDisc(preview.center, Vector3.forward, radius);
            Handles.color = new Color(tint.r, tint.g, tint.b, 0.2f + 0.6f * strength);
            Handles.DrawSolidDisc(preview.center, Vector3.forward, radius * 0.5f);
            Handles.color = tint;
            Handles.DrawWireDisc(preview.center, Vector3.forward, radius);
        }

        static void ClearRow(HBStyleTerrainData data)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Clear", EditorStyles.miniBoldLabel, GUILayout.Width(40f));

                using (new EditorGUI.DisabledScope(!StyleTerrainWater.Ready(data)))
                {
                    for (var index = 0; index < s_ClearNames.Length; index++)
                    {
                        var style = index == 0 ? EditorStyles.miniButtonLeft
                                  : index == s_ClearNames.Length - 1 ? EditorStyles.miniButtonRight
                                  : EditorStyles.miniButtonMid;

                        if (!GUILayout.Button(s_ClearNames[index], style) ||
                            !EditorUtility.DisplayDialog("Clear " + s_ClearNames[index].text,
                                                         s_ClearNames[index].tooltip + " Undo brings it back.",
                                                         "Clear", "Cancel"))
                            continue;

                        StyleTerrainWater.Clear(data, (StyleTerrainWaterPart)index);
                        StyleTerrainLiquidBrush.Watch(data);
                        SceneView.RepaintAll();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        static void Dot(Rect rect, float x, float y)
        {
            Handles.DrawSolidDisc(new Vector3(rect.x + rect.width * x, rect.y + rect.height * y), Vector3.forward,
                                  2.5f);
        }

        internal static void Glyph(StyleTerrainLiquidTool tool, Rect rect)
        {
            switch (tool)
            {
                case StyleTerrainLiquidTool.Raise:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.8f, 0.3f, 0.7f, 0.5f, 0.8f, 0.7f, 0.7f, 0.9f, 0.8f);
                    StyleTerrainGUI.Ink(rect, 0.5f, 0.58f, 0.5f, 0.15f);
                    StyleTerrainGUI.Ink(rect, 0.34f, 0.32f, 0.5f, 0.15f, 0.66f, 0.32f);
                    break;

                case StyleTerrainLiquidTool.SetHeight:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.4f, 0.9f, 0.4f);
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.26f, 0.1f, 0.54f);
                    StyleTerrainGUI.Ink(rect, 0.9f, 0.26f, 0.9f, 0.54f);
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.85f, 0.35f, 0.68f, 0.6f, 0.85f, 0.9f, 0.72f);
                    break;

                case StyleTerrainLiquidTool.Smooth:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.62f, 0.2f, 0.32f, 0.3f, 0.68f, 0.4f, 0.38f, 0.5f, 0.58f, 0.65f,
                                        0.48f, 0.9f, 0.5f);
                    break;

                case StyleTerrainLiquidTool.Lake:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.22f, 0.25f, 0.82f, 0.75f, 0.82f, 0.9f, 0.22f);
                    StyleTerrainGUI.Ink(rect, 0.18f, 0.5f, 0.82f, 0.5f);
                    StyleTerrainGUI.Ink(rect, 0.22f, 0.64f, 0.78f, 0.64f);
                    break;

                case StyleTerrainLiquidTool.Ramp:
                    StyleTerrainGUI.Ink(rect, 0.12f, 0.25f, 0.5f, 0.45f, 0.88f, 0.78f);
                    Dot(rect, 0.12f, 0.25f);
                    Dot(rect, 0.5f, 0.45f);
                    Dot(rect, 0.88f, 0.78f);
                    break;

                case StyleTerrainLiquidTool.Calm:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.3f, 0.25f, 0.18f, 0.4f, 0.3f, 0.55f, 0.42f, 0.7f, 0.3f, 0.9f,
                                        0.22f);
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.56f, 0.3f, 0.5f, 0.5f, 0.56f, 0.7f, 0.62f, 0.9f, 0.56f);
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.8f, 0.9f, 0.8f);
                    break;

                case StyleTerrainLiquidTool.AutoFlow:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.25f, 0.45f, 0.3f, 0.6f, 0.7f, 0.9f, 0.75f);
                    StyleTerrainGUI.Ink(rect, 0.5f, 0.12f, 0.72f, 0.4f);
                    StyleTerrainGUI.Ink(rect, 0.56f, 0.38f, 0.72f, 0.4f, 0.7f, 0.24f);
                    break;

                case StyleTerrainLiquidTool.Whirlpool:
                    StyleTerrainGUI.Ink(rect, 0.5f, 0.5f, 0.6f, 0.42f, 0.56f, 0.3f, 0.4f, 0.28f, 0.26f, 0.42f, 0.28f,
                                        0.64f, 0.46f, 0.78f, 0.7f, 0.74f, 0.84f, 0.54f, 0.82f, 0.3f);
                    break;

                case StyleTerrainLiquidTool.Flow:
                    StyleTerrainGUI.Ink(rect, 0.12f, 0.35f, 0.85f, 0.35f);
                    StyleTerrainGUI.Ink(rect, 0.68f, 0.22f, 0.85f, 0.35f, 0.68f, 0.48f);
                    StyleTerrainGUI.Ink(rect, 0.12f, 0.7f, 0.85f, 0.7f);
                    StyleTerrainGUI.Ink(rect, 0.68f, 0.57f, 0.85f, 0.7f, 0.68f, 0.83f);
                    break;

                case StyleTerrainLiquidTool.NoLiquid:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.78f, 0.9f, 0.78f);
                    StyleTerrainGUI.Ink(rect, 0.3f, 0.15f, 0.7f, 0.6f);
                    StyleTerrainGUI.Ink(rect, 0.7f, 0.15f, 0.3f, 0.6f);
                    break;

                case StyleTerrainLiquidTool.SmoothFlow:
                    StyleTerrainGUI.Ink(rect, 0.1f, 0.5f, 0.25f, 0.36f, 0.4f, 0.5f, 0.55f, 0.64f, 0.7f, 0.5f, 0.86f,
                                        0.4f);
                    StyleTerrainGUI.Ink(rect, 0.72f, 0.3f, 0.86f, 0.4f, 0.78f, 0.56f);
                    break;

                default:
                    Handles.DrawSolidDisc(rect.center, Vector3.forward, rect.width * 0.3f);
                    break;
            }
        }
    }
}
