using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainGUI
    {
        const float HeaderHeight = 22f;
        const float GuideIcon = 34f;
        const int TileColumns = 5;
        const float TileHeight = 58f;
        const float TileGap = 3f;
        const float CaptionHeight = 16f;
        const float LabelPad = 6f;
        const float LabelStrip = 4f;
        const float LabelOffset = 14f;

        internal static readonly Color shapeTint = new Color(0.35f, 0.65f, 0.95f);
        internal static readonly Color surfaceTint = new Color(0.95f, 0.6f, 0.25f);
        internal static readonly Color layersTint = new Color(0.4f, 0.85f, 0.45f);
        internal static readonly Color lookTint = new Color(0.7f, 0.55f, 0.95f);
        internal static readonly Color settingsTint = new Color(0.6f, 0.6f, 0.6f);
        internal static readonly Color grassTint = new Color(0.6f, 0.85f, 0.25f);

        static readonly Color s_LabelBack = new Color(0.1f, 0.1f, 0.1f, 0.92f);
        static readonly char[] s_Breaks = { '_', ' ', '-', '.' };

        static GUIStyle s_Heading;
        static GUIStyle s_Caption;
        static GUIStyle s_Title;
        static GUIStyle s_Text;
        static GUIStyle s_Chip;
        static GUIStyle s_LabelTitle;
        static GUIStyle s_LabelText;

        internal static void Header(string label, Color tint)
        {
            EditorGUILayout.Space(4f);

            var rect = EditorGUILayout.GetControlRect(false, HeaderHeight);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(tint.r, tint.g, tint.b, 0.12f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), tint);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                                   new Color(tint.r, tint.g, tint.b, 0.5f));
            }

            s_Heading ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, alignment = TextAnchor.MiddleLeft };

            rect.xMin += 10f;
            GUI.Label(rect, label, s_Heading);
            EditorGUILayout.Space(2f);
        }

        internal static void Frame(Rect rect, Color colour)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), colour);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), colour);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), colour);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), colour);
        }

        internal static Rect TileRow()
        {
            return EditorGUILayout.GetControlRect(false, TileHeight);
        }

        internal static Rect Slot(Rect row, int index)
        {
            var width = (row.width - TileGap * (TileColumns - 1)) / TileColumns;

            return new Rect(row.x + (index % TileColumns) * (width + TileGap), row.y, width, row.height);
        }

        internal static int columns => TileColumns;

        internal static Rect Picture(Rect tile)
        {
            return new Rect(tile.x + 5f, tile.y + 5f, tile.width - 10f, tile.height - CaptionHeight - 8f);
        }

        internal static bool Tile(Rect rect, bool active, Color tint, GUIContent content)
        {
            var current = Event.current;

            if (current.type == EventType.Repaint)
            {
                s_Caption ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip
                };

                EditorGUI.DrawRect(rect, active ? new Color(tint.r, tint.g, tint.b, 0.3f)
                                                : new Color(1f, 1f, 1f, 0.06f));
                Frame(rect, active ? tint : new Color(1f, 1f, 1f, 0.1f));

                s_Caption.normal.textColor = active ? Color.white : EditorStyles.miniLabel.normal.textColor;
                s_Caption.Draw(new Rect(rect.x, rect.yMax - CaptionHeight, rect.width, CaptionHeight), content.text,
                               false, false, false, false);
            }

            GUI.Label(rect, new GUIContent(string.Empty, content.tooltip), GUIStyle.none);

            if (!GUI.enabled || current.type != EventType.MouseDown || current.button != 0 ||
                !rect.Contains(current.mousePosition))
                return false;

            current.Use();
            GUI.changed = true;
            return true;
        }

        internal static void Ink(Rect rect, params float[] points)
        {
            var line = new Vector3[points.Length / 2];

            for (var index = 0; index < line.Length; index++)
            {
                line[index] = new Vector3(rect.x + points[index * 2] * rect.width,
                                          rect.y + points[index * 2 + 1] * rect.height, 0f);
            }

            Handles.DrawAAPolyLine(2f, line);
        }

        internal static void Glyph(StyleTerrainBrushTool tool, Rect rect)
        {
            switch (tool)
            {
                case StyleTerrainBrushTool.Raise:
                    Ink(rect, 0.1f, 0.8f, 0.3f, 0.7f, 0.5f, 0.8f, 0.7f, 0.7f, 0.9f, 0.8f);
                    Ink(rect, 0.5f, 0.58f, 0.5f, 0.15f);
                    Ink(rect, 0.34f, 0.32f, 0.5f, 0.15f, 0.66f, 0.32f);
                    break;

                case StyleTerrainBrushTool.Smooth:
                    Ink(rect, 0.1f, 0.62f, 0.2f, 0.32f, 0.3f, 0.68f, 0.4f, 0.38f, 0.5f, 0.58f, 0.65f, 0.48f, 0.9f,
                        0.5f);
                    break;

                case StyleTerrainBrushTool.Flatten:
                    Ink(rect, 0.1f, 0.78f, 0.3f, 0.62f, 0.5f, 0.78f, 0.7f, 0.62f, 0.9f, 0.78f);
                    Ink(rect, 0.1f, 0.45f, 0.9f, 0.45f);
                    Ink(rect, 0.5f, 0.1f, 0.5f, 0.38f);
                    Ink(rect, 0.38f, 0.26f, 0.5f, 0.38f, 0.62f, 0.26f);
                    break;

                case StyleTerrainBrushTool.SetHeight:
                    Ink(rect, 0.1f, 0.4f, 0.9f, 0.4f);
                    Ink(rect, 0.1f, 0.26f, 0.1f, 0.54f);
                    Ink(rect, 0.9f, 0.26f, 0.9f, 0.54f);
                    Ink(rect, 0.1f, 0.85f, 0.35f, 0.68f, 0.6f, 0.85f, 0.9f, 0.72f);
                    break;

                case StyleTerrainBrushTool.Holes:
                    Ink(rect, 0.05f, 0.5f, 0.3f, 0.5f);
                    Ink(rect, 0.7f, 0.5f, 0.95f, 0.5f);
                    Ink(rect, 0.3f, 0.5f, 0.32f, 0.72f, 0.5f, 0.82f, 0.68f, 0.72f, 0.7f, 0.5f);
                    break;

                case StyleTerrainBrushTool.Noise:
                    Ink(rect, 0.1f, 0.6f, 0.2f, 0.4f, 0.3f, 0.65f, 0.42f, 0.3f, 0.55f, 0.62f, 0.65f, 0.38f, 0.78f,
                        0.66f, 0.9f, 0.45f);
                    break;

                case StyleTerrainBrushTool.Terrace:
                    Ink(rect, 0.1f, 0.85f, 0.3f, 0.85f, 0.3f, 0.65f, 0.5f, 0.65f, 0.5f, 0.45f, 0.7f, 0.45f, 0.7f,
                        0.25f, 0.9f, 0.25f);
                    break;

                case StyleTerrainBrushTool.GrassDensity:
                    Ink(rect, 0.1f, 0.85f, 0.9f, 0.85f);
                    Ink(rect, 0.2f, 0.85f, 0.15f, 0.55f);
                    Ink(rect, 0.35f, 0.85f, 0.38f, 0.45f);
                    Ink(rect, 0.5f, 0.85f, 0.48f, 0.5f);
                    Ink(rect, 0.65f, 0.85f, 0.68f, 0.45f);
                    Ink(rect, 0.8f, 0.85f, 0.85f, 0.55f);
                    Ink(rect, 0.5f, 0.32f, 0.5f, 0.08f);
                    Ink(rect, 0.38f, 0.2f, 0.62f, 0.2f);
                    break;

                case StyleTerrainBrushTool.GrassHeight:
                    Ink(rect, 0.1f, 0.85f, 0.9f, 0.85f);
                    Ink(rect, 0.3f, 0.85f, 0.25f, 0.6f);
                    Ink(rect, 0.45f, 0.85f, 0.45f, 0.25f);
                    Ink(rect, 0.6f, 0.85f, 0.62f, 0.4f);
                    Ink(rect, 0.8f, 0.6f, 0.8f, 0.1f);
                    Ink(rect, 0.7f, 0.22f, 0.8f, 0.1f, 0.9f, 0.22f);
                    break;

                case StyleTerrainBrushTool.GrassSetDensity:
                    Ink(rect, 0.1f, 0.85f, 0.9f, 0.85f);
                    Ink(rect, 0.2f, 0.85f, 0.2f, 0.5f);
                    Ink(rect, 0.4f, 0.85f, 0.4f, 0.5f);
                    Ink(rect, 0.6f, 0.85f, 0.6f, 0.5f);
                    Ink(rect, 0.8f, 0.85f, 0.8f, 0.5f);
                    Ink(rect, 0.15f, 0.3f, 0.85f, 0.3f);
                    Ink(rect, 0.15f, 0.18f, 0.85f, 0.18f);
                    break;

                case StyleTerrainBrushTool.GrassSetHeight:
                    Ink(rect, 0.1f, 0.85f, 0.9f, 0.85f);
                    Ink(rect, 0.25f, 0.85f, 0.25f, 0.4f);
                    Ink(rect, 0.45f, 0.85f, 0.45f, 0.4f);
                    Ink(rect, 0.65f, 0.85f, 0.65f, 0.4f);
                    Ink(rect, 0.1f, 0.3f, 0.9f, 0.3f);
                    Ink(rect, 0.1f, 0.18f, 0.1f, 0.42f);
                    Ink(rect, 0.9f, 0.18f, 0.9f, 0.42f);
                    break;

                case StyleTerrainBrushTool.GrassReset:
                    Ink(rect, 0.1f, 0.85f, 0.9f, 0.85f);
                    Ink(rect, 0.3f, 0.85f, 0.28f, 0.55f);
                    Ink(rect, 0.5f, 0.85f, 0.5f, 0.5f);
                    Ink(rect, 0.7f, 0.85f, 0.72f, 0.55f);
                    Ink(rect, 0.75f, 0.3f, 0.65f, 0.15f, 0.45f, 0.12f, 0.3f, 0.22f, 0.25f, 0.38f);
                    Ink(rect, 0.18f, 0.28f, 0.25f, 0.38f, 0.36f, 0.33f);
                    break;

                default:
                    Handles.DrawSolidDisc(rect.center, Vector3.forward, rect.width * 0.3f);
                    break;
            }
        }

        internal static void Guide(Color tint, string title, string help, StyleTerrainBrushTool? glyph,
                                   params string[][] chips)
        {
            s_Title ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            s_Text ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel) { richText = false };

            using (var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var icon = GUILayoutUtility.GetRect(GuideIcon, GuideIcon, GUILayout.Width(GuideIcon));

                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(icon, new Color(tint.r, tint.g, tint.b, 0.18f));
                        Frame(icon, new Color(tint.r, tint.g, tint.b, 0.6f));
                        Handles.color = tint;

                        var inner = new Rect(icon.x + 5f, icon.y + 5f, icon.width - 10f, icon.height - 10f);

                        if (glyph.HasValue)
                            Glyph(glyph.Value, inner);
                        else
                            Handles.DrawSolidDisc(icon.center, Vector3.forward, GuideIcon * 0.22f);
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Label(title, s_Title);
                        GUILayout.Label(help, s_Text);
                    }
                }

                if (chips.Length > 0)
                {
                    EditorGUILayout.Space(2f);
                    Chips(chips);
                }

                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(box.rect.x, box.rect.y, 3f, box.rect.height), tint);
            }
        }

        static void Chips(string[][] groups)
        {
            s_Chip ??= new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 1, 2),
                margin = new RectOffset(2, 2, 2, 2)
            };

            var room = EditorGUIUtility.currentViewWidth - 50f;
            var used = 0f;

            EditorGUILayout.BeginHorizontal();

            foreach (var group in groups)
            {
                for (var index = 0; group != null && index + 1 < group.Length; index += 2)
                {
                    var key = new GUIContent(group[index]);
                    var meaning = new GUIContent(group[index + 1]);
                    var width = s_Chip.CalcSize(key).x + EditorStyles.miniLabel.CalcSize(meaning).x + 12f;

                    if (used > 0f && used + width > room)
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        used = 0f;
                    }

                    GUILayout.Label(key, s_Chip, GUILayout.ExpandWidth(false));
                    GUILayout.Label(meaning, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                    GUILayout.Space(4f);
                    used += width;
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        internal static string Caption(IReadOnlyList<string> names, int index)
        {
            var title = names[index];
            string shared = null;
            var named = 0;

            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                    continue;

                named++;

                if (shared == null)
                {
                    shared = name;
                    continue;
                }

                var same = 0;

                while (same < shared.Length && same < name.Length && shared[same] == name[same])
                    same++;

                shared = shared.Substring(0, same);
            }

            if (string.IsNullOrEmpty(title))
                return "None";

            var cut = named > 1 && shared != null ? shared.LastIndexOfAny(s_Breaks) + 1 : 0;

            return cut > 0 && cut < title.Length ? title.Substring(cut) : title;
        }

        internal static void SceneLabel(Vector3 point, string title, string detail, Color tint)
        {
            if (s_LabelTitle == null)
            {
                s_LabelTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
                s_LabelTitle.normal.textColor = Color.white;
                s_LabelText = new GUIStyle(EditorStyles.label) { fontSize = 11 };
                s_LabelText.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            }

            var head = new GUIContent(title);
            var body = new GUIContent(detail ?? string.Empty);
            var headSize = s_LabelTitle.CalcSize(head);
            var bodySize = string.IsNullOrEmpty(detail) ? Vector2.zero : s_LabelText.CalcSize(body);
            var width = Mathf.Max(headSize.x, bodySize.x) + LabelPad * 2f + LabelStrip;
            var height = headSize.y + bodySize.y + LabelPad * 2f;

            Handles.BeginGUI();

            var anchor = HandleUtility.WorldToGUIPoint(point);
            var box = new Rect(anchor.x + LabelOffset, anchor.y - height - LabelOffset, width, height);
            var view = SceneView.currentDrawingSceneView != null
                ? SceneView.currentDrawingSceneView.position
                : new Rect(0f, 0f, float.MaxValue, float.MaxValue);

            box.x = Mathf.Min(box.x, view.width - width - 4f);
            box.y = Mathf.Max(box.y, 4f);

            EditorGUI.DrawRect(box, s_LabelBack);
            EditorGUI.DrawRect(new Rect(box.x, box.y, LabelStrip, box.height), tint);

            var left = box.x + LabelStrip + LabelPad;

            GUI.Label(new Rect(left, box.y + LabelPad, headSize.x, headSize.y), head, s_LabelTitle);

            if (!string.IsNullOrEmpty(detail))
                GUI.Label(new Rect(left, box.y + LabelPad + headSize.y, bodySize.x, bodySize.y), body, s_LabelText);

            Handles.EndGUI();
        }
    }
}
