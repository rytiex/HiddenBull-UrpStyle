using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleTerrainLayerList
    {
        const float GraphHeight = 220f;
        const float AxisLeft = 48f;
        const float AxisBottom = 18f;
        const float EdgeReach = 6f;
        const float RowHeight = 22f;
        const float OpenEdge = 0.005f;
        const float Open = 10000f;
        const string HighlightKey = "HiddenBull.UrpStyle.Terrain.Highlight";

        static readonly Color[] s_Palette =
        {
            new Color(0.90f, 0.30f, 0.25f), new Color(0.30f, 0.70f, 0.30f), new Color(0.25f, 0.45f, 0.90f),
            new Color(0.95f, 0.80f, 0.20f), new Color(0.70f, 0.35f, 0.85f), new Color(0.20f, 0.80f, 0.80f),
            new Color(0.95f, 0.55f, 0.15f), new Color(0.85f, 0.85f, 0.85f)
        };

        static readonly Color s_GraphBack = new Color(0.12f, 0.12f, 0.12f, 1f);
        static readonly Color s_Empty = new Color(0.3f, 0.3f, 0.3f, 1f);

        static class Styles
        {
            public static readonly GUIContent Slope = new GUIContent("Slope",
                "Steepness range, in degrees, the layer covers. 0 is flat ground, 90 a vertical cliff.");

            public static readonly GUIContent Lowest = new GUIContent("Lowest",
                "Lowest height, in metres above the terrain's base, the layer reaches. Any = no lower limit, " +
                "on every terrain that uses this set.");

            public static readonly GUIContent Highest = new GUIContent("Highest",
                "Highest height, in metres above the terrain's base, the layer reaches. Any = no upper limit, " +
                "on every terrain that uses this set.");

            public static readonly GUIContent Any = new GUIContent("Any", "No limit on this side.");

            public static readonly GUIContent PickSlope = new GUIContent("Pick Slope In Scene",
                "Drag over the terrain in the Scene view; the slopes under the drag become this layer's range.");

            public static readonly GUIContent PickHeight = new GUIContent("Pick Height In Scene",
                "Drag over the terrain in the Scene view; the heights under the drag become this layer's range.");

            public static readonly GUIContent Pick = new GUIContent("Pick in Scene",
                "Hover the terrain to see its slope, height and layer; click to select that layer here.");

            public static readonly GUIContent Highlight = new GUIContent("Highlight",
                "Tints the selected layer's ground blue in the Scene view.");

            public static readonly GUIContent LayerMap = new GUIContent("Layer Map",
                "Colours every layer in the Scene view with its colour from the list so you can see where each " +
                "one lies.");

            public static readonly GUIContent Look = new GUIContent("Look",
                "The layer asset's own settings. They are shared by every set that uses this layer.");

            public const string Base =
                "The first layer covers the whole terrain. The layers after it are laid over it, in list order, " +
                "where their rules match.";

            public const string PaintOnly =
                "Paint only: this layer is placed only where it is painted by hand.";

            public const string Graph =
                "Grey shows how much of the terrain sits at each slope and height. Drag a layer's edges to change " +
                "its range (to the border = no limit); click inside a box to select it.";
        }

        enum Edge
        {
            None,
            SlopeLow,
            SlopeHigh,
            HeightLow,
            HeightHigh
        }

        static bool s_Where = true;
        static bool s_Look;

        readonly HBStyleTerrainLayerSet m_Set;
        readonly SerializedObject m_Object;
        readonly SerializedProperty m_Layers;
        readonly ReorderableList m_List;

        HBStyleTerrain m_Terrain;
        StyleTerrainProfile m_Profile;
        UnityEditor.Editor m_LayerEditor;
        int m_Selected;
        Edge m_Edge;

        internal HBStyleTerrainLayerSet set => m_Set;

        float Low => m_Profile != null ? m_Profile.low : 0f;

        float High => m_Profile != null ? m_Profile.high : 1f;

        internal StyleTerrainLayerList(HBStyleTerrainLayerSet set)
        {
            m_Set = set;
            m_Object = new SerializedObject(set);
            m_Layers = m_Object.FindProperty("m_Layers");

            m_List = new ReorderableList(m_Object, m_Layers, true, true, true, true)
            {
                elementHeight = RowHeight,
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Layers (bottom to top)"),
                drawElementCallback = DrawRow,
                onSelectCallback = list => m_Selected = list.index,
                onReorderCallbackWithDetails = (list, from, to) => m_Selected = to,
                onAddCallback = Add,
                onCanAddCallback = list => list.count < HBStyleTerrainLayerSet.MaxLayers
            };

            StyleTerrainPicker.selected += Select;
        }

        internal static Color Tint(int index)
        {
            var colour = s_Palette[index % s_Palette.Length];
            var shade = index >= s_Palette.Length ? 0.6f : 1f;

            return new Color(colour.r * shade, colour.g * shade, colour.b * shade, 1f);
        }

        internal void Draw(HBStyleTerrain terrain, HBStyleTerrainData data)
        {
            m_Terrain = terrain;
            m_Profile = StyleTerrainProfile.Get(data);
            m_Object.Update();
            m_Selected = Mathf.Clamp(m_Selected, 0, Mathf.Max(m_Layers.arraySize - 1, 0));

            StyleTerrainGUI.Header("Where Layers Grow", StyleTerrainGUI.layersTint);
            DrawGraph();
            EditorGUILayout.Space(4f);

            m_List.index = m_Layers.arraySize > 0 ? m_Selected : -1;
            m_List.DoLayoutList();

            StyleTerrainGUI.Header("Selected Layer", StyleTerrainLayerList.Tint(m_Selected));
            DrawDetails();
            m_Object.ApplyModifiedProperties();

            DrawLook();

            StyleTerrainGUI.Header("Scene View", StyleTerrainGUI.layersTint);
            DrawTools();

            StyleTerrains.highlightTerrain = m_Terrain;
            StyleTerrains.highlightRow = EditorPrefs.GetBool(HighlightKey, true) ? m_Selected : -1;
        }

        internal void Release()
        {
            StyleTerrainPicker.selected -= Select;
            Hide();

            if (m_LayerEditor != null)
                Object.DestroyImmediate(m_LayerEditor);
        }

        internal void Hide()
        {
            if (m_Terrain == null)
                return;

            if (StyleTerrainPicker.terrain == m_Terrain)
                StyleTerrainPicker.Stop();

            if (StyleTerrains.highlightTerrain == m_Terrain)
            {
                StyleTerrains.highlightTerrain = null;
                StyleTerrains.highlightRow = -1;
                SceneView.RepaintAll();
            }
        }

        void Select(int row)
        {
            if (StyleTerrainPicker.terrain != m_Terrain)
                return;

            m_Selected = row;
            InternalEditorUtility.RepaintAllViews();
        }

        void Add(ReorderableList list)
        {
            var index = m_Layers.arraySize;

            m_Layers.arraySize++;

            var row = m_Layers.GetArrayElementAtIndex(index);

            row.FindPropertyRelative("m_Layer").objectReferenceValue = null;
            row.FindPropertyRelative("m_Automatic").boolValue = true;
            row.FindPropertyRelative("m_Slope").vector2Value = new Vector2(0f, 90f);
            row.FindPropertyRelative("m_SlopeSoftness").floatValue = 5f;
            row.FindPropertyRelative("m_Height").vector2Value = new Vector2(-Open, Open);
            row.FindPropertyRelative("m_HeightSoftness").floatValue = 2f;
            row.FindPropertyRelative("m_Opacity").floatValue = 1f;
            row.FindPropertyRelative("m_Breakup").floatValue = 0.5f;
            row.FindPropertyRelative("m_BreakupSize").floatValue = 8f;

            m_Selected = index;
        }

        void DrawRow(Rect rect, int index, bool active, bool focused)
        {
            var row = m_Layers.GetArrayElementAtIndex(index);
            var layer = row.FindPropertyRelative("m_Layer").objectReferenceValue as HBStyleTerrainLayer;
            var automatic = row.FindPropertyRelative("m_Automatic").boolValue;

            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 4f, 4f, rect.height - 8f), Tint(index));

            var thumb = new Rect(rect.x + 8f, rect.y + 2f, 18f, 18f);

            if (layer != null && layer.albedo != null)
                GUI.DrawTexture(thumb, layer.albedo, ScaleMode.ScaleAndCrop);
            else
                EditorGUI.DrawRect(thumb, layer != null ? layer.tint : s_Empty);

            var mode = index == 0 ? "Base" : automatic ? "Automatic" : "Paint only";

            EditorGUI.LabelField(new Rect(rect.x + 32f, rect.y + 2f, rect.width - 156f, 18f),
                                 layer != null ? layer.name : "(no layer)");
            EditorGUI.LabelField(new Rect(rect.xMax - 120f, rect.y + 2f, 120f, 18f), mode, EditorStyles.miniLabel);
        }

        void DrawGraph()
        {
            var rect = GUILayoutUtility.GetRect(0f, GraphHeight, GUILayout.ExpandWidth(true));
            var plot = new Rect(rect.x + AxisLeft, rect.y + 4f, rect.width - AxisLeft - 8f,
                                rect.height - AxisBottom - 8f);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(plot, s_GraphBack);

                if (m_Profile != null)
                    GUI.DrawTexture(plot, m_Profile.histogram, ScaleMode.StretchToFill);

                for (var index = 1; index < m_Layers.arraySize; index++)
                {
                    var row = m_Layers.GetArrayElementAtIndex(index);

                    if (!Automatic(row))
                        continue;

                    var tint = Tint(index);
                    var fill = tint;

                    fill.a = index == m_Selected ? 0.25f : 0.07f;
                    tint.a = index == m_Selected ? 1f : 0.6f;

                    Handles.DrawSolidRectangleWithOutline(Area(row, plot), fill, tint);
                }

                if (StyleTerrainPicker.hovering && StyleTerrainPicker.terrain == m_Terrain)
                {
                    var point = ToGraph(plot, StyleTerrainPicker.hoverSlope, StyleTerrainPicker.hoverHeight);
                    EditorGUI.DrawRect(new Rect(point.x - 3f, point.y - 3f, 6f, 6f), Color.white);
                }
            }

            var mini = EditorStyles.miniLabel;

            GUI.Label(new Rect(rect.x, plot.y - 2f, AxisLeft - 4f, 14f), $"{High:0} m", mini);
            GUI.Label(new Rect(rect.x, plot.yMax - 12f, AxisLeft - 4f, 14f), $"{Low:0} m", mini);
            GUI.Label(new Rect(plot.x - 2f, plot.yMax + 1f, 40f, 14f), "0°", mini);
            GUI.Label(new Rect(plot.center.x - 30f, plot.yMax + 1f, 60f, 14f), "Slope 45°", mini);
            GUI.Label(new Rect(plot.xMax - 22f, plot.yMax + 1f, 30f, 14f), "90°", mini);

            GraphInput(plot);

            EditorGUILayout.LabelField(Styles.Graph, EditorStyles.wordWrappedMiniLabel);
        }

        void GraphInput(Rect plot)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var current = Event.current;
            var selected = m_Selected > 0 && m_Selected < m_Layers.arraySize
                ? m_Layers.GetArrayElementAtIndex(m_Selected)
                : null;
            var active = selected != null && Automatic(selected);

            if (active && current.type == EventType.Repaint)
            {
                var area = Area(selected, plot);

                EditorGUIUtility.AddCursorRect(new Rect(area.xMin - EdgeReach, area.y, EdgeReach * 2f, area.height),
                                               MouseCursor.ResizeHorizontal);
                EditorGUIUtility.AddCursorRect(new Rect(area.xMax - EdgeReach, area.y, EdgeReach * 2f, area.height),
                                               MouseCursor.ResizeHorizontal);
                EditorGUIUtility.AddCursorRect(new Rect(area.x, area.yMin - EdgeReach, area.width, EdgeReach * 2f),
                                               MouseCursor.ResizeVertical);
                EditorGUIUtility.AddCursorRect(new Rect(area.x, area.yMax - EdgeReach, area.width, EdgeReach * 2f),
                                               MouseCursor.ResizeVertical);
            }

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                {
                    if (current.button != 0 || !plot.Contains(current.mousePosition))
                        break;

                    m_Edge = active ? HitEdge(Area(selected, plot), current.mousePosition) : Edge.None;

                    if (m_Edge != Edge.None)
                    {
                        GUIUtility.hotControl = id;
                        current.Use();
                        break;
                    }

                    for (var index = m_Layers.arraySize - 1; index > 0; index--)
                    {
                        var row = m_Layers.GetArrayElementAtIndex(index);

                        if (Automatic(row) && Area(row, plot).Contains(current.mousePosition))
                        {
                            m_Selected = index;
                            current.Use();
                            break;
                        }
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl != id || selected == null)
                        break;

                    Drag(selected, plot, current.mousePosition);
                    GUI.changed = true;
                    current.Use();
                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl != id)
                        break;

                    GUIUtility.hotControl = 0;
                    m_Edge = Edge.None;
                    current.Use();
                    break;
                }
            }
        }

        void Drag(SerializedProperty row, Rect plot, Vector2 mouse)
        {
            var slope = row.FindPropertyRelative("m_Slope");
            var height = row.FindPropertyRelative("m_Height");
            var degrees = Mathf.Round(Mathf.Clamp01((mouse.x - plot.x) / plot.width) * 180f) * 0.5f;
            var t = Mathf.Clamp01((plot.yMax - mouse.y) / plot.height);
            var metres = Mathf.Round(Mathf.Lerp(Low, High, t) * 10f) * 0.1f;
            var range = slope.vector2Value;
            var span = height.vector2Value;

            switch (m_Edge)
            {
                case Edge.SlopeLow:
                    range.x = Mathf.Min(degrees, range.y);
                    break;
                case Edge.SlopeHigh:
                    range.y = Mathf.Max(degrees, range.x);
                    break;
                case Edge.HeightLow:
                    span.x = t <= OpenEdge ? -Open : Mathf.Min(metres, span.y);
                    break;
                case Edge.HeightHigh:
                    span.y = t >= 1f - OpenEdge ? Open : Mathf.Max(metres, span.x);
                    break;
            }

            slope.vector2Value = range;
            height.vector2Value = span;
        }

        static Edge HitEdge(Rect area, Vector2 mouse)
        {
            var insideX = mouse.x > area.xMin - EdgeReach && mouse.x < area.xMax + EdgeReach;
            var insideY = mouse.y > area.yMin - EdgeReach && mouse.y < area.yMax + EdgeReach;

            if (insideY && Mathf.Abs(mouse.x - area.xMin) <= EdgeReach)
                return Edge.SlopeLow;

            if (insideY && Mathf.Abs(mouse.x - area.xMax) <= EdgeReach)
                return Edge.SlopeHigh;

            if (insideX && Mathf.Abs(mouse.y - area.yMax) <= EdgeReach)
                return Edge.HeightLow;

            if (insideX && Mathf.Abs(mouse.y - area.yMin) <= EdgeReach)
                return Edge.HeightHigh;

            return Edge.None;
        }

        static bool Automatic(SerializedProperty row)
        {
            return row.FindPropertyRelative("m_Automatic").boolValue;
        }

        Rect Area(SerializedProperty row, Rect plot)
        {
            var slope = row.FindPropertyRelative("m_Slope").vector2Value;
            var height = row.FindPropertyRelative("m_Height").vector2Value;
            var low = ToGraph(plot, slope.x, height.x);
            var high = ToGraph(plot, slope.y, height.y);

            return Rect.MinMaxRect(low.x, high.y, high.x, low.y);
        }

        Vector2 ToGraph(Rect plot, float slope, float height)
        {
            var x = plot.x + Mathf.Clamp01(slope / 90f) * plot.width;
            var t = Mathf.Clamp01((height - Low) / Mathf.Max(High - Low, 1e-3f));

            return new Vector2(x, plot.yMax - t * plot.height);
        }

        void DrawDetails()
        {
            if (m_Layers.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add a layer and give it an HB Style Terrain Layer.", MessageType.Info);
                return;
            }

            var row = m_Layers.GetArrayElementAtIndex(m_Selected);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(row.FindPropertyRelative("m_Layer"));

                s_Where = EditorGUILayout.Foldout(s_Where, "Where", true);

                if (s_Where)
                {
                    using (new EditorGUI.IndentLevelScope())
                        DrawWhere(row);
                }
            }
        }

        void DrawWhere(SerializedProperty row)
        {
            if (m_Selected == 0)
            {
                EditorGUILayout.HelpBox(Styles.Base, MessageType.None);
                return;
            }

            var automatic = row.FindPropertyRelative("m_Automatic");

            EditorGUILayout.PropertyField(automatic);

            if (!automatic.boolValue)
            {
                EditorGUILayout.HelpBox(Styles.PaintOnly, MessageType.None);
                return;
            }

            var slope = row.FindPropertyRelative("m_Slope");
            var height = row.FindPropertyRelative("m_Height");

            DrawSlope(slope);
            EditorGUILayout.PropertyField(row.FindPropertyRelative("m_SlopeSoftness"));
            DrawLimit(height, Styles.Lowest, false);
            DrawLimit(height, Styles.Highest, true);
            EditorGUILayout.PropertyField(row.FindPropertyRelative("m_HeightSoftness"));

            var breakup = row.FindPropertyRelative("m_Breakup");

            EditorGUILayout.PropertyField(breakup);

            using (new EditorGUI.DisabledScope(breakup.floatValue <= 0f))
            using (new EditorGUI.IndentLevelScope())
                EditorGUILayout.PropertyField(row.FindPropertyRelative("m_BreakupSize"));

            EditorGUILayout.PropertyField(row.FindPropertyRelative("m_Opacity"));

            var line = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            var half = (line.width - 4f) * 0.5f;

            PickButton(new Rect(line.x, line.y, half, line.height), slope, true, Styles.PickSlope);
            PickButton(new Rect(line.x + half + 4f, line.y, half, line.height), height, false, Styles.PickHeight);
        }

        void PickButton(Rect rect, SerializedProperty range, bool slope, GUIContent label)
        {
            var picking = StyleTerrainPicker.IsPicking(range);

            if (GUI.Toggle(rect, picking, label, EditorStyles.miniButton) == picking)
                return;

            if (picking)
                StyleTerrainPicker.EndRange();
            else
                StyleTerrainPicker.PickRange(m_Terrain, range, slope);
        }

        static void DrawSlope(SerializedProperty slope)
        {
            var value = slope.vector2Value;
            var low = Mathf.Min(value.x, value.y);
            var high = Mathf.Max(value.x, value.y);

            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.MinMaxSlider(Styles.Slope, ref low, ref high, 0f, 90f);
                low = Mathf.Clamp(EditorGUILayout.FloatField(low, GUILayout.Width(40f)), 0f, 90f);
                high = Mathf.Clamp(EditorGUILayout.FloatField(high, GUILayout.Width(40f)), 0f, 90f);
            }

            if (EditorGUI.EndChangeCheck())
                slope.vector2Value = new Vector2(Mathf.Round(low * 2f), Mathf.Round(Mathf.Max(low, high) * 2f)) * 0.5f;
        }

        void DrawLimit(SerializedProperty height, GUIContent label, bool upper)
        {
            var value = height.vector2Value;
            var current = upper ? value.y : value.x;
            var any = upper ? current >= Open * 0.5f : current <= -Open * 0.5f;
            var field = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);
            var toggle = new Rect(field.x, field.y, 48f, field.height);
            var number = new Rect(field.x + 52f, field.y, field.width - 52f, field.height);
            var indent = EditorGUI.indentLevel;

            EditorGUI.indentLevel = 0;

            var nowAny = EditorGUI.ToggleLeft(toggle, Styles.Any, any);

            if (nowAny != any)
                current = nowAny ? (upper ? Open : -Open) : (upper ? High : Low);

            using (new EditorGUI.DisabledScope(nowAny))
            {
                EditorGUI.BeginChangeCheck();

                var typed = EditorGUI.FloatField(number, nowAny ? 0f : (float)System.Math.Round(current, 2));

                if (EditorGUI.EndChangeCheck() && !nowAny)
                    current = typed;
            }

            EditorGUI.indentLevel = indent;

            if (upper)
                value.y = current;
            else
                value.x = current;

            if (value.x > value.y)
            {
                if (upper)
                    value.x = value.y;
                else
                    value.y = value.x;
            }

            if (value != height.vector2Value)
                height.vector2Value = value;
        }

        void DrawLook()
        {
            if (m_Layers.arraySize == 0)
                return;

            var layer = m_Layers.GetArrayElementAtIndex(m_Selected).FindPropertyRelative("m_Layer")
                                .objectReferenceValue as HBStyleTerrainLayer;

            if (layer == null)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s_Look = EditorGUILayout.Foldout(s_Look, Styles.Look, true);

                if (!s_Look)
                    return;

                UnityEditor.Editor.CreateCachedEditor(layer, typeof(StyleTerrainLayerEditor), ref m_LayerEditor);

                using (new EditorGUI.IndentLevelScope())
                    m_LayerEditor.OnInspectorGUI();
            }
        }

        void DrawTools()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var inspecting = StyleTerrainPicker.IsInspecting(m_Terrain);

                if (GUILayout.Toggle(inspecting, Styles.Pick, "Button", GUILayout.Height(22f)) != inspecting)
                {
                    if (inspecting)
                        StyleTerrainPicker.Stop();
                    else
                        StyleTerrainPicker.Inspect(m_Terrain);
                }

                var highlight = EditorPrefs.GetBool(HighlightKey, true);

                if (GUILayout.Toggle(highlight, Styles.Highlight, "Button", GUILayout.Height(22f)) != highlight)
                {
                    EditorPrefs.SetBool(HighlightKey, !highlight);
                    SceneView.RepaintAll();
                }

                var map = GUILayout.Toggle(StyleTerrains.layerMap, Styles.LayerMap, "Button", GUILayout.Height(22f));

                if (map != StyleTerrains.layerMap)
                {
                    StyleTerrains.layerMap = map;
                    SceneView.RepaintAll();
                }
            }
        }
    }
}
