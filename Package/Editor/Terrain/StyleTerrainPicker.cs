using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleTerrainPickMode
    {
        None,
        Inspect,
        Slope,
        Height
    }

    static class StyleTerrainPicker
    {
        const float HoverPad = 6f;
        const float HoverStrip = 4f;
        const float HoverOffset = 14f;
        const float SlopeStep = 0.5f;
        const float HeightStep = 0.1f;

        static readonly Color s_HoverBack = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        static GUIStyle s_Title;
        static GUIStyle s_Text;

        static StyleTerrainPickMode s_Mode;
        static StyleTerrainPickMode s_Return;
        static HBStyleTerrain s_Terrain;
        static SerializedObject s_Object;
        static string s_Path;
        static bool s_Dragging;
        static float s_Low;
        static float s_High;
        static Vector3 s_Point;
        static Vector3 s_Normal;
        static int s_Row = -1;

        internal static bool hovering;
        internal static float hoverSlope;
        internal static float hoverHeight;

        internal static event System.Action<int> selected;

        internal static HBStyleTerrain terrain => s_Terrain;

        internal static bool IsInspecting(HBStyleTerrain owner)
        {
            return s_Terrain == owner && owner != null &&
                   (s_Mode == StyleTerrainPickMode.Inspect || s_Return == StyleTerrainPickMode.Inspect);
        }

        internal static bool IsPicking(SerializedProperty range)
        {
            return (s_Mode == StyleTerrainPickMode.Slope || s_Mode == StyleTerrainPickMode.Height) &&
                   s_Object != null && s_Object.targetObject == range.serializedObject.targetObject &&
                   s_Path == range.propertyPath;
        }

        internal static void Inspect(HBStyleTerrain owner)
        {
            Start(owner, StyleTerrainPickMode.Inspect);
            s_Return = StyleTerrainPickMode.None;
        }

        internal static void PickRange(HBStyleTerrain owner, SerializedProperty range, bool slope)
        {
            var back = IsInspecting(owner) ? StyleTerrainPickMode.Inspect : StyleTerrainPickMode.None;

            Start(owner, slope ? StyleTerrainPickMode.Slope : StyleTerrainPickMode.Height);
            s_Return = back;
            s_Object = range.serializedObject;
            s_Path = range.propertyPath;
        }

        internal static void EndRange()
        {
            if (s_Return != StyleTerrainPickMode.Inspect)
            {
                Stop();
                return;
            }

            s_Mode = StyleTerrainPickMode.Inspect;
            s_Return = StyleTerrainPickMode.None;
            s_Object = null;
            s_Path = null;
            s_Dragging = false;
            Repaint();
        }

        internal static void Stop()
        {
            SceneView.duringSceneGui -= OnScene;
            s_Mode = StyleTerrainPickMode.None;
            s_Return = StyleTerrainPickMode.None;
            s_Terrain = null;
            s_Object = null;
            s_Path = null;
            s_Dragging = false;
            hovering = false;
            Repaint();
        }

        static void Start(HBStyleTerrain owner, StyleTerrainPickMode mode)
        {
            SceneView.duringSceneGui -= OnScene;
            SceneView.duringSceneGui += OnScene;
            s_Terrain = owner;
            s_Mode = mode;
            s_Object = null;
            s_Path = null;
            s_Dragging = false;
            hovering = false;
            Repaint();
        }

        static void Repaint()
        {
            SceneView.RepaintAll();
            InternalEditorUtility.RepaintAllViews();
        }

        static bool ranging => s_Mode == StyleTerrainPickMode.Slope || s_Mode == StyleTerrainPickMode.Height;

        static void OnScene(SceneView view)
        {
            if (s_Terrain == null || s_Mode == StyleTerrainPickMode.None)
            {
                Stop();
                return;
            }

            var current = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);

            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(id);

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                current.Use();

                if (ranging)
                    EndRange();
                else
                    Stop();

                return;
            }

            if (current.type == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                current.Use();

                if (s_Dragging)
                    Apply();

                EndRange();
                return;
            }

            switch (current.type)
            {
                case EventType.MouseMove:
                    Hover(current.mousePosition);
                    Repaint();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    Hover(current.mousePosition);

                    if (hovering)
                    {
                        var value = s_Mode == StyleTerrainPickMode.Slope ? hoverSlope : hoverHeight;

                        s_Low = Mathf.Min(s_Low, value);
                        s_High = Mathf.Max(s_High, value);
                    }

                    current.Use();
                    Repaint();
                    break;

                case EventType.MouseDown when current.button == 0 && !current.alt:
                    Hover(current.mousePosition);

                    if (!hovering)
                        break;

                    if (ranging)
                    {
                        var value = s_Mode == StyleTerrainPickMode.Slope ? hoverSlope : hoverHeight;

                        GUIUtility.hotControl = id;
                        s_Dragging = true;
                        s_Low = value;
                        s_High = value;
                    }
                    else if (s_Row >= 0)
                    {
                        selected?.Invoke(s_Row);
                    }

                    current.Use();
                    Repaint();
                    break;

                case EventType.Repaint:
                    DrawHover();
                    break;
            }
        }

        static void Apply()
        {
            s_Object.Update();

            var range = s_Object.FindProperty(s_Path);

            if (range == null)
                return;

            var step = s_Mode == StyleTerrainPickMode.Slope ? SlopeStep : HeightStep;

            range.vector2Value = new Vector2(Mathf.Floor(s_Low / step) * step, Mathf.Ceil(s_High / step) * step);
            s_Object.ApplyModifiedProperties();
        }

        static void Hover(Vector2 mouse)
        {
            hovering = false;
            s_Row = -1;

            var data = s_Terrain.data;

            if (data == null || data.terrainData == null ||
                !StyleTerrainRay.Hit(s_Terrain, HandleUtility.GUIPointToWorldRay(mouse), out var point))
                return;

            var local = point - s_Terrain.transform.position;
            var size = data.size;
            var u = local.x / size.x;
            var v = local.z / size.z;

            s_Point = point;
            s_Normal = data.terrainData.GetInterpolatedNormal(u, v);
            hoverSlope = data.terrainData.GetSteepness(u, v);
            hoverHeight = local.y;
            s_Row = Painted(data, u, v, out var row) ? row : Match(data, hoverSlope, hoverHeight);
            hovering = true;
        }

        static bool Painted(HBStyleTerrainData data, float u, float v, out int row)
        {
            row = -1;

            Texture control = null;
            var count = 0;
            var live = StyleTerrains.TryGetPreview(data, out var preview) && preview.control != null;

            if (live)
            {
                control = preview.control;
                count = preview.layers.Length;
            }
            else if (data.control != null)
            {
                control = data.control;
                count = data.controlLayers;
            }

            if (control == null || count == 0)
                return false;

            var resolution = control.width;
            var x = Mathf.Clamp(Mathf.FloorToInt(u * resolution), 0, resolution - 1);
            var z = Mathf.Clamp(Mathf.FloorToInt(v * resolution), 0, resolution - 1);
            var slices = (count + 3) / 4;
            var request = AsyncGPUReadback.Request(control, 0, x, 1, z, 1, 0, slices);

            request.WaitForCompletion();

            if (request.hasError)
                return false;

            var best = -1;
            var strongest = 0;

            for (var slice = 0; slice < slices; slice++)
            {
                var texel = request.GetData<Color32>(slice)[0];

                for (var channel = 0; channel < 4 && slice * 4 + channel < count; channel++)
                {
                    if (texel[channel] <= strongest)
                        continue;

                    strongest = texel[channel];
                    best = slice * 4 + channel;
                }
            }

            if (best < 0)
                return true;

            if (live)
            {
                row = best < data.layerCount ? best : -1;
                return true;
            }

            var layer = data.Layer(best);

            for (var index = 0; index < data.layerCount; index++)
            {
                if (data.layers[index].layer == layer)
                {
                    row = index;
                    break;
                }
            }

            return true;
        }

        static int Match(HBStyleTerrainData data, float slope, float height)
        {
            var layers = data.layers;

            for (var index = data.layerCount - 1; index > 0; index--)
            {
                var rule = layers[index];

                if (rule.automatic && rule.opacity > 0f && slope >= rule.slope.x && slope <= rule.slope.y &&
                    height >= rule.height.x && height <= rule.height.y)
                    return index;
            }

            return data.layerCount > 0 ? 0 : -1;
        }

        static void DrawHover()
        {
            if (!hovering)
                return;

            var size = HandleUtility.GetHandleSize(s_Point) * 0.2f;
            var tint = s_Row >= 0 ? StyleTerrainLayerList.Tint(s_Row) : Color.white;

            Handles.color = tint;
            Handles.DrawWireDisc(s_Point, s_Normal, size);

            if (s_Title == null)
            {
                s_Title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
                s_Title.normal.textColor = Color.white;
                s_Text = new GUIStyle(EditorStyles.label) { fontSize = 11 };
                s_Text.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            }

            var title = new GUIContent(Title());
            var detail = new GUIContent(Detail());
            var titleSize = s_Title.CalcSize(title);
            var detailSize = s_Text.CalcSize(detail);
            var width = Mathf.Max(titleSize.x, detailSize.x) + HoverPad * 2f + HoverStrip;
            var height = titleSize.y + detailSize.y + HoverPad * 2f;

            Handles.BeginGUI();

            var anchor = HandleUtility.WorldToGUIPoint(s_Point);
            var box = new Rect(anchor.x + HoverOffset, anchor.y - height - HoverOffset, width, height);
            var view = SceneView.currentDrawingSceneView != null
                ? SceneView.currentDrawingSceneView.position
                : new Rect(0f, 0f, float.MaxValue, float.MaxValue);

            box.x = Mathf.Min(box.x, view.width - width - 4f);
            box.y = Mathf.Max(box.y, 4f);

            EditorGUI.DrawRect(box, s_HoverBack);
            EditorGUI.DrawRect(new Rect(box.x, box.y, HoverStrip, box.height), tint);

            var left = box.x + HoverStrip + HoverPad;

            GUI.Label(new Rect(left, box.y + HoverPad, titleSize.x, titleSize.y), title, s_Title);
            GUI.Label(new Rect(left, box.y + HoverPad + titleSize.y, detailSize.x, detailSize.y), detail, s_Text);

            Handles.EndGUI();
        }

        static string Title()
        {
            if (s_Mode == StyleTerrainPickMode.Slope)
                return s_Dragging ? $"Slope {s_Low:0.#}° – {s_High:0.#}°" : "Drag to set the slope range";

            if (s_Mode == StyleTerrainPickMode.Height)
                return s_Dragging ? $"Height {s_Low:0.#} – {s_High:0.#} m" : "Drag to set the height range";

            var data = s_Terrain.data;
            var layer = s_Row >= 0 && data != null ? data.layers[s_Row].layer : null;

            if (s_Row < 0)
                return "No layer";

            return layer != null ? layer.name : $"Layer {s_Row}";
        }

        static string Detail()
        {
            return $"Slope {hoverSlope:0.0}°   Height {hoverHeight:0.0} m";
        }
    }
}
