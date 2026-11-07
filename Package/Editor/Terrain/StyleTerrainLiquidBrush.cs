using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleTerrainLiquidTool
    {
        Raise,
        SetHeight,
        Smooth,
        Lake,
        Ramp,
        Paint,
        Flow,
        SmoothFlow,
        AutoFlow,
        Whirlpool,
        Calm,
        NoLiquid
    }

    enum StyleTerrainLakeForm
    {
        Fill,
        Circle,
        Square
    }

    static class StyleTerrainLiquidBrush
    {
        const string Key = "HiddenBull.UrpStyle.Terrain.Liquid.";
        internal const float SizeLeast = 1f;
        internal const float SizeMost = 200f;
        const float SizeStep = 1.15f;
        const float ForceStep = 0.05f;
        const float ShowReach = 30f;
        const int ShowCount = 32;
        const float ShowLift = 0.3f;
        const float ShowGlow = 0.3f;
        const float FlowShown = 0.01f;
        const int CursorSegments = 64;
        const int GridMargin = 2;
        const int GridReach = 32;
        const float CursorLift = 0.05f;
        const float GridLift = 0.03f;

        static readonly List<Vector4> s_Path = new List<Vector4>();
        static readonly Vector3[] s_Quad = new Vector3[4];
        static readonly Color s_Brush = new Color(0.3f, 0.7f, 0.95f, 0.9f);
        static readonly Color s_Cut = new Color(1f, 0.35f, 0.3f, 1f);
        static readonly Color s_Restore = new Color(0.3f, 0.7f, 1f, 1f);
        static readonly Color s_Grid = new Color(1f, 1f, 1f, 0.3f);
        static readonly Vector3[] s_Circle = new Vector3[CursorSegments + 1];

        static HBStyleTerrain s_Terrain;
        static HBStyleTerrainData s_Watched;
        static int s_WaterVersion = -1;
        static bool s_Stroking;
        static Vector3 s_Last;
        static ushort[] s_Held;
        static HBStyleTerrainData s_Previewed;
        static RectInt s_Shown;

        internal static bool armed
        {
            get => SessionState.GetBool(Key + "Armed", false);
            set => SessionState.SetBool(Key + "Armed", value);
        }

        internal static StyleTerrainLiquidTool tool
        {
            get => (StyleTerrainLiquidTool)Mathf.Clamp(EditorPrefs.GetInt(Key + "Tool", 0), 0,
                                                       (int)StyleTerrainLiquidTool.NoLiquid);
            set
            {
                if (value != tool)
                    Cancel();

                EditorPrefs.SetInt(Key + "Tool", (int)value);
            }
        }

        internal static int slot
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(Key + "Slot", 0), 0, HBStyleTerrainData.LiquidSlots - 1);
            set => EditorPrefs.SetInt(Key + "Slot", Mathf.Clamp(value, 0, HBStyleTerrainData.LiquidSlots - 1));
        }

        internal static float size
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat(Key + "Size", 20f), SizeLeast, SizeMost);
            set => EditorPrefs.SetFloat(Key + "Size", Mathf.Clamp(value, SizeLeast, SizeMost));
        }

        internal static float strength
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat(Key + "Strength", 0.5f), 0.01f, 1f);
            set => EditorPrefs.SetFloat(Key + "Strength", Mathf.Clamp(value, 0.01f, 1f));
        }

        internal static float speed
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat(Key + "Speed", 3f), 0f, StyleTerrainWater.FlowMost);
            set => EditorPrefs.SetFloat(Key + "Speed", Mathf.Clamp(value, 0f, StyleTerrainWater.FlowMost));
        }

        internal static float height
        {
            get => EditorPrefs.GetFloat(Key + "Height", 0f);
            set => EditorPrefs.SetFloat(Key + "Height", value);
        }

        internal static float depth
        {
            get => EditorPrefs.GetFloat(Key + "Depth", 0.5f);
            set => EditorPrefs.SetFloat(Key + "Depth", value);
        }

        internal static bool marks
        {
            get => EditorPrefs.GetBool(Key + "Marks", true);
            set => EditorPrefs.SetBool(Key + "Marks", value);
        }

        internal static StyleTerrainLakeForm form
        {
            get => (StyleTerrainLakeForm)Mathf.Clamp(EditorPrefs.GetInt(Key + "Form", 0), 0,
                                                     (int)StyleTerrainLakeForm.Square);
            set => EditorPrefs.SetInt(Key + "Form", (int)value);
        }

        internal static bool streamed => Tint(tool) == StyleTerrainGUI.layersTint;

        internal static bool rated => tool == StyleTerrainLiquidTool.Flow || tool == StyleTerrainLiquidTool.AutoFlow ||
                                      tool == StyleTerrainLiquidTool.Whirlpool || tool == StyleTerrainLiquidTool.Ramp;

        internal static bool forced => tool != StyleTerrainLiquidTool.Lake && tool != StyleTerrainLiquidTool.Ramp &&
                                       tool != StyleTerrainLiquidTool.Flow && tool != StyleTerrainLiquidTool.AutoFlow &&
                                       tool != StyleTerrainLiquidTool.NoLiquid;

        internal static bool pinned => tool == StyleTerrainLiquidTool.Lake || tool == StyleTerrainLiquidTool.Ramp;

        internal static bool sized => tool != StyleTerrainLiquidTool.Lake || form != StyleTerrainLakeForm.Fill;

        internal static bool cutting => tool == StyleTerrainLiquidTool.NoLiquid;

        internal static int pins => s_Path.Count;

        internal static int pinsNeeded => tool == StyleTerrainLiquidTool.Lake ? 1 : 2;

        internal static Color tint => Tint(tool);

        internal static Color Tint(StyleTerrainLiquidTool brush)
        {
            switch (brush)
            {
                case StyleTerrainLiquidTool.Paint:
                    return StyleTerrainGUI.surfaceTint;

                case StyleTerrainLiquidTool.Calm:
                    return StyleTerrainGUI.lookTint;

                case StyleTerrainLiquidTool.Flow:
                case StyleTerrainLiquidTool.SmoothFlow:
                case StyleTerrainLiquidTool.AutoFlow:
                case StyleTerrainLiquidTool.Whirlpool:
                    return StyleTerrainGUI.layersTint;

                default:
                    return StyleTerrainGUI.shapeTint;
            }
        }

        [InitializeOnLoadMethod]
        static void Load()
        {
            Undo.undoRedoPerformed += OnUndo;
        }

        internal static void Watch(HBStyleTerrainData data)
        {
            s_Watched = data;
            s_WaterVersion = data != null ? data.waterVersion : -1;
        }

        internal static void Begin(HBStyleTerrain terrain)
        {
            if (ReferenceEquals(s_Terrain, terrain))
                return;

            Stop();

            if (StyleTerrainPicker.terrain == terrain)
                StyleTerrainPicker.Stop();

            s_Terrain = terrain;
            Watch(terrain.data);
            SceneView.duringSceneGui += OnScene;
            SceneView.RepaintAll();
        }

        internal static void End(HBStyleTerrain terrain)
        {
            if (ReferenceEquals(s_Terrain, terrain))
                Stop();
        }

        static void Stop()
        {
            if (s_Stroking)
                Finish();

            Cancel();
            SceneView.duringSceneGui -= OnScene;
            s_Terrain = null;
            SceneView.RepaintAll();
        }

        static void OnUndo()
        {
            var data = s_Watched;

            if (data == null)
                return;

            if (data.waterVersion == s_WaterVersion)
            {
                Cancel();
                return;
            }

            s_Previewed = null;
            s_Held = null;
            s_Path.Clear();

            if (StyleTerrainWater.Ready(data))
            {
                data.waterLevel.Apply(false, false);
                data.waterMix.Apply(false, false);
                data.waterFlow.Apply(false, false);
            }

            s_WaterVersion = data.waterVersion;
            SceneView.RepaintAll();
        }

        internal static void Changed(HBStyleTerrainData data)
        {
            if (pinned && s_Path.Count > 0)
                Preview(data);

            SceneView.RepaintAll();
        }

        static void OnScene(SceneView view)
        {
            var data = s_Terrain != null ? s_Terrain.data : null;

            if (data == null || data.terrainData == null || !data.valid || !data.liquidOn)
                return;

            var current = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var origin = s_Terrain.transform.position;

            if (pinned && Pins(origin))
                Preview(data);

            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(id);
                return;
            }

            if (current.type == EventType.KeyDown && pinned &&
                (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
            {
                Lay(data);
                current.Use();
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            if (current.type == EventType.KeyDown && Shortcut(data, current.keyCode))
            {
                current.Use();
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            if (current.rawType == EventType.MouseUp && s_Stroking)
            {
                GUIUtility.hotControl = 0;
                Finish();
                current.Use();
                return;
            }

            if (!StyleTerrainRay.Hit(s_Terrain, HandleUtility.GUIPointToWorldRay(current.mousePosition),
                                     out var point))
                return;

            var local = point - origin;

            switch (current.type)
            {
                case EventType.Repaint:
                    Draw(data, origin, local, current.shift);
                    return;

                case EventType.MouseMove:
                    view.Repaint();
                    return;
            }

            if (current.alt || current.button != 0)
                return;

            if (current.type == EventType.MouseDown && HandleUtility.nearestControl == id)
            {
                Press(data, id, local, current.shift);
                current.Use();
                return;
            }

            if (current.type != EventType.MouseDrag || !s_Stroking)
                return;

            var picking = tool == StyleTerrainLiquidTool.SetHeight && current.shift;

            if (tool != StyleTerrainLiquidTool.Whirlpool && !picking)
                Stamp(data, local, current.shift);

            current.Use();
        }

        static void Press(HBStyleTerrainData data, int id, Vector3 local, bool shift)
        {
            if (shift && tool == StyleTerrainLiquidTool.SetHeight)
            {
                height = Mathf.Round(local.y * 10f) * 0.1f;
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            if (!StyleTerrainWater.Ensure(data))
                return;

            if (pinned)
            {
                var placed = Pin(data, local, shift && tool == StyleTerrainLiquidTool.Ramp);

                if (tool == StyleTerrainLiquidTool.Lake)
                {
                    if (s_Path.Count > 0)
                        placed.w = s_Path[0].w;

                    s_Path.Clear();
                }

                s_Path.Add(placed);
                Preview(data);
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            Undo.RegisterCompleteObjectUndo(StyleTerrainWater.Recorded(data), "Paint HB Style Liquid");
            GUIUtility.hotControl = id;
            s_Stroking = true;
            s_Last = local;
            Stamp(data, local, shift);
        }

        static bool Shortcut(HBStyleTerrainData data, KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftBracket:
                    size /= SizeStep;
                    return true;

                case KeyCode.RightBracket:
                    size *= SizeStep;
                    return true;

                case KeyCode.Minus:
                case KeyCode.Equals:
                    var sign = key == KeyCode.Minus ? -1f : 1f;

                    if (rated)
                        speed = Mathf.Max(speed + sign * StyleTerrainWater.FlowStep,
                                          tool == StyleTerrainLiquidTool.Ramp ? 0f : StyleTerrainWater.FlowStep);
                    else
                        strength += sign * ForceStep;

                    return true;

                case KeyCode.Backspace when s_Path.Count > 0:
                    s_Path.RemoveAt(s_Path.Count - 1);
                    Preview(data);
                    return true;

                case KeyCode.Escape:
                    if (s_Path.Count > 0)
                    {
                        Cancel();
                        return true;
                    }

                    armed = false;
                    Stop();
                    return true;

                default:
                    return false;
            }
        }

        static void Stamp(HBStyleTerrainData data, Vector3 local, bool shift)
        {
            var radius = size * 0.5f;

            switch (tool)
            {
                case StyleTerrainLiquidTool.Raise:
                case StyleTerrainLiquidTool.SetHeight:
                case StyleTerrainLiquidTool.Smooth:
                    StyleTerrainWater.Stroke(data, tool, local, radius, strength, shift, height - data.seaLevel);
                    break;

                case StyleTerrainLiquidTool.Paint:
                    if (slot < data.liquidCount)
                        StyleTerrainWater.Paint(data, slot, local, radius, strength);

                    break;

                case StyleTerrainLiquidTool.NoLiquid:
                    StyleTerrainWater.Cut(data, local, radius, shift);
                    break;

                case StyleTerrainLiquidTool.Flow:
                    var heading = new Vector2(local.x - s_Last.x, local.z - s_Last.z);

                    s_Last = local;

                    if (!shift && heading.sqrMagnitude < 1e-4f)
                        return;

                    StyleTerrainWater.Run(data, local, radius, shift ? Vector2.zero : heading.normalized * speed);
                    break;

                case StyleTerrainLiquidTool.SmoothFlow:
                    StyleTerrainWater.Settle(data, local, radius, strength);
                    break;

                case StyleTerrainLiquidTool.AutoFlow:
                    StyleTerrainWater.Descend(data, local, radius, Mathf.Max(speed, StyleTerrainWater.FlowStep));
                    break;

                case StyleTerrainLiquidTool.Whirlpool:
                    StyleTerrainWater.Swirl(data, local, radius, Mathf.Max(speed, StyleTerrainWater.FlowStep),
                                            strength, shift);
                    break;

                case StyleTerrainLiquidTool.Calm:
                    StyleTerrainWater.Lull(data, local, radius, strength, shift);
                    break;
            }
        }

        static void Finish()
        {
            var data = s_Terrain != null ? s_Terrain.data : null;

            if (!s_Stroking || data == null)
            {
                s_Stroking = false;
                return;
            }

            s_Stroking = false;
            Edited(data);
        }

        static void Edited(HBStyleTerrainData data)
        {
            data.WaterEdited();
            EditorUtility.SetDirty(data.waterLevel);
            EditorUtility.SetDirty(data.waterMix);
            EditorUtility.SetDirty(data.waterFlow);
            EditorUtility.SetDirty(data);
            Watch(data);
        }

        static Vector4 Pin(HBStyleTerrainData data, Vector3 local, bool keep)
        {
            var level = keep ? StyleTerrainWater.Level(data, local) : local.y + depth;

            return new Vector4(local.x, level, local.z, size * 0.5f);
        }

        static RectInt Build(HBStyleTerrainData data, bool laid)
        {
            if (tool == StyleTerrainLiquidTool.Lake)
            {
                if (s_Path.Count == 0)
                    return new RectInt();

                return form == StyleTerrainLakeForm.Fill
                    ? StyleTerrainWater.Flood(data, s_Path[0])
                    : StyleTerrainWater.Pool(data, s_Path[0], form == StyleTerrainLakeForm.Circle);
            }

            return StyleTerrainWater.Ramp(data, s_Path, laid, speed);
        }

        static void Preview(HBStyleTerrainData data)
        {
            if (s_Path.Count == 0)
            {
                Cancel();
                return;
            }

            if (!StyleTerrainWater.Ready(data))
                return;

            if (s_Previewed != data || s_Held == null)
            {
                Revert();
                StyleTerrainWater.Forget();
                s_Held = StyleTerrainWater.Hold(data);
                s_Previewed = data;
                s_Shown = new RectInt();
            }

            StyleTerrainWater.Restore(data, s_Held, s_Shown);
            s_Shown = Build(data, false);
            StyleTerrainWater.Show(data, s_Shown);
            SceneView.RepaintAll();
        }

        static void Cancel()
        {
            s_Path.Clear();
            Revert();
        }

        static void Revert()
        {
            if (s_Previewed != null && s_Held != null)
                StyleTerrainWater.Restore(s_Previewed, s_Held, s_Shown);

            s_Previewed = null;
            s_Held = null;
            s_Shown = new RectInt();
            SceneView.RepaintAll();
        }

        internal static void Lay(HBStyleTerrainData data)
        {
            if (!pinned || s_Path.Count < pinsNeeded || !StyleTerrainWater.Ensure(data))
                return;

            if (s_Previewed == data && s_Held != null)
                StyleTerrainWater.Restore(data, s_Held, s_Shown);

            s_Previewed = null;
            s_Held = null;
            s_Shown = new RectInt();

            Undo.RegisterCompleteObjectUndo(StyleTerrainWater.Recorded(data),
                                            tool == StyleTerrainLiquidTool.Lake ? "Lake" : "Ramp Liquid");
            StyleTerrainWater.Show(data, Build(data, true));
            s_Path.Clear();
            Edited(data);
            SceneView.RepaintAll();
        }

        static bool Pins(Vector3 origin)
        {
            var changed = false;
            var edged = tool == StyleTerrainLiquidTool.Ramp || form != StyleTerrainLakeForm.Fill;

            for (var index = 0; index < s_Path.Count; index++)
            {
                var pin = s_Path[index];
                var place = origin + new Vector3(pin.x, pin.y, pin.z);
                var handle = HandleUtility.GetHandleSize(place);
                var wide = pin.w;

                EditorGUI.BeginChangeCheck();
                Handles.color = StyleTerrainGUI.shapeTint;

                var moved = Handles.Slider2D(place, Vector3.up, Vector3.right, Vector3.forward, handle * 0.12f,
                                             Handles.CircleHandleCap, Vector2.zero);

                Handles.color = Color.white;

                var lifted = Handles.Slider(new Vector3(moved.x, place.y, moved.z), Vector3.up, handle * 0.7f,
                                            Handles.ArrowHandleCap, 0f);

                if (edged)
                {
                    Handles.color = StyleTerrainGUI.layersTint;

                    var edge = Handles.Slider(new Vector3(moved.x + wide, lifted.y, moved.z), Vector3.right,
                                              handle * 0.06f, Handles.DotHandleCap, 0f);

                    wide = Mathf.Max(edge.x - moved.x, 0.5f);
                }

                if (!EditorGUI.EndChangeCheck())
                    continue;

                s_Path[index] = new Vector4(moved.x - origin.x, lifted.y - origin.y, moved.z - origin.z, wide);
                changed = true;
            }

            return changed;
        }

        static void Draw(HBStyleTerrainData data, Vector3 origin, Vector3 local, bool shift)
        {
            var radius = size * 0.5f;
            var point = origin + local;
            var level = StyleTerrainWater.Level(data, local);
            var surface = new Vector3(point.x, origin.y + level, point.z);

            if (streamed)
                Streams(data, origin, local);

            if (cutting)
            {
                Cutter(data, origin, local, shift);
                StyleTerrainGUI.SceneLabel(origin + Ground(data, local, CursorLift), "No Liquid",
                                           Detail(data, local, level, shift), shift ? s_Restore : s_Cut);
                return;
            }

            Handles.color = s_Brush;

            if (tool == StyleTerrainLiquidTool.Lake)
            {
                var seed = s_Path.Count > 0 ? s_Path[0] : Pin(data, local, false);

                if (form == StyleTerrainLakeForm.Fill)
                    Handles.DrawWireDisc(point, Vector3.up, 1f);
                else
                    Outline(origin, seed);

                StyleTerrainGUI.SceneLabel(origin + (Vector3)seed, "Lake", Metres(seed.y), tint);
                return;
            }

            if (tool == StyleTerrainLiquidTool.Ramp)
            {
                var next = Pin(data, local, shift);
                var nextPlace = origin + (Vector3)next;

                for (var index = 0; index < s_Path.Count; index++)
                {
                    var place = origin + (Vector3)s_Path[index];

                    Handles.DrawWireDisc(place, Vector3.up, s_Path[index].w);
                    Handles.Label(place + Vector3.up, Metres(s_Path[index].y));

                    if (index > 0)
                        Handles.DrawLine(origin + (Vector3)s_Path[index - 1], place);
                }

                if (s_Path.Count > 0)
                    Handles.DrawDottedLine(origin + (Vector3)s_Path[s_Path.Count - 1], nextPlace, 4f);

                Handles.DrawWireDisc(nextPlace, Vector3.up, radius);
                StyleTerrainGUI.SceneLabel(nextPlace, "Ramp", Metres(next.y), tint);
                return;
            }

            Handles.DrawWireDisc(point, Vector3.up, radius);
            Handles.DrawWireDisc(surface, Vector3.up, radius);
            Handles.color = new Color(s_Brush.r, s_Brush.g, s_Brush.b, 0.35f);
            Handles.DrawWireDisc(surface, Vector3.up, radius * 0.5f);

            StyleTerrainGUI.SceneLabel(surface, StyleTerrainLiquidPanel.Title(data), Detail(data, local, level, shift),
                                       tint);
        }

        static string Detail(HBStyleTerrainData data, Vector3 local, float level, bool shift)
        {
            if (streamed)
                return StyleTerrainWater.Stream(data, local).magnitude.ToString("0.0") + " m/s" + Flat(data, local);

            switch (tool)
            {
                case StyleTerrainLiquidTool.Calm:
                    return Mathf.RoundToInt((1f - StyleTerrainWater.Still(data, local)) * 100f) + "% waves" +
                           (shift ? ", bring back" : string.Empty);

                case StyleTerrainLiquidTool.Paint:
                    return null;

                case StyleTerrainLiquidTool.NoLiquid:
                    return (StyleTerrainWater.DryTexel(data, local) ? "No liquid here" : "Liquid here") +
                           (shift ? ", bring back" : string.Empty);

                case StyleTerrainLiquidTool.Raise:
                    return Metres(level) + (shift ? ", lower" : string.Empty);

                case StyleTerrainLiquidTool.SetHeight:
                    return Metres(level) + " to " + Metres(height);

                default:
                    return Metres(level);
            }
        }

        static string Flat(HBStyleTerrainData data, Vector3 local)
        {
            if (tool != StyleTerrainLiquidTool.AutoFlow)
                return string.Empty;

            return StyleTerrainWater.Fall(data, local).magnitude < StyleTerrainWater.FlatSlope
                ? ", liquid is flat here"
                : string.Empty;
        }

        static string Metres(float value)
        {
            return value.ToString("0.0") + " m";
        }

        static void Outline(Vector3 origin, Vector4 pin)
        {
            var middle = origin + new Vector3(pin.x, pin.y, pin.z);

            if (form == StyleTerrainLakeForm.Circle)
                Handles.DrawWireDisc(middle, Vector3.up, pin.w);
            else
                Handles.DrawWireCube(middle, new Vector3(pin.w * 2f, 0f, pin.w * 2f));
        }

        static Vector3 Cell(HBStyleTerrainData data, Vector3 local, float step, int column, int row)
        {
            var reach = step * ShowCount * 0.5f;
            var x = Mathf.Round((local.x - reach) / step + column) * step;
            var z = Mathf.Round((local.z - reach) / step + row) * step;
            var place = new Vector3(x, 0f, z);

            place.y = StyleTerrainWater.Level(data, place) + ShowLift;
            return place;
        }

        static bool Inside(HBStyleTerrainData data, Vector3 local)
        {
            return local.x >= 0f && local.z >= 0f && local.x <= data.size.x && local.z <= data.size.z;
        }

        static void Glow(Vector3 middle, float step, Color colour)
        {
            var half = step * 0.5f;

            s_Quad[0] = middle + new Vector3(-half, 0f, -half);
            s_Quad[1] = middle + new Vector3(-half, 0f, half);
            s_Quad[2] = middle + new Vector3(half, 0f, half);
            s_Quad[3] = middle + new Vector3(half, 0f, -half);
            colour.a = ShowGlow;
            Handles.DrawSolidRectangleWithOutline(s_Quad, colour, Color.clear);
        }

        static void Streams(HBStyleTerrainData data, Vector3 origin, Vector3 local)
        {
            var step = Mathf.Max(size, ShowReach) * 2f / ShowCount;

            for (var row = 0; row <= ShowCount; row++)
            {
                for (var column = 0; column <= ShowCount; column++)
                {
                    var cell = Cell(data, local, step, column, row);

                    if (!Inside(data, cell))
                        continue;

                    var run = StyleTerrainWater.Stream(data, cell);
                    var still = run.sqrMagnitude < FlowShown * FlowShown;

                    if (!marks && still)
                        continue;

                    var share = Mathf.Clamp01(run.magnitude / StyleTerrainWater.FlowMost);
                    var colour = share < 0.5f ? Color.Lerp(Color.red, Color.yellow, share * 2f)
                                              : Color.Lerp(Color.yellow, Color.green, share * 2f - 1f);
                    var middle = origin + cell;

                    Handles.color = Color.white;
                    Glow(middle, step, colour);
                    Handles.color = colour;

                    if (still)
                    {
                        Handles.DrawWireDisc(middle, Vector3.up, step * 0.06f);
                        continue;
                    }

                    var along = new Vector3(run.x, 0f, run.y).normalized * (step * (0.35f + 0.45f * share));
                    var side = new Vector3(-along.z, 0f, along.x) * 0.3f;
                    var tip = middle + along * 0.5f;

                    Handles.DrawLine(middle - along * 0.5f, tip);
                    Handles.DrawLine(tip, tip - along * 0.35f + side);
                    Handles.DrawLine(tip, tip - along * 0.35f - side);
                }
            }
        }

        static void Cutter(HBStyleTerrainData data, Vector3 origin, Vector3 local, bool shift)
        {
            var colour = shift ? s_Restore : s_Cut;
            var radius = size * 0.5f;

            for (var index = 0; index <= CursorSegments; index++)
            {
                var angle = index * Mathf.PI * 2f / CursorSegments;
                var place = local + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                s_Circle[index] = origin + Ground(data, place, CursorLift);
            }

            Handles.color = colour;
            Handles.DrawAAPolyLine(2.5f, s_Circle);

            if (!StyleTerrainWater.Ready(data))
                return;

            var cells = StyleTerrainWater.CutCells(data);
            var cell = data.size.x / cells;
            var reach = Mathf.Min(Mathf.CeilToInt(radius / cell) + GridMargin, GridReach);
            var centreX = Mathf.FloorToInt(local.x / cell);
            var centreZ = Mathf.FloorToInt(local.z / cell);
            var x0 = Mathf.Clamp(centreX - reach, 0, cells);
            var x1 = Mathf.Clamp(centreX + reach + 1, 0, cells);
            var z0 = Mathf.Clamp(centreZ - reach, 0, cells);
            var z1 = Mathf.Clamp(centreZ + reach + 1, 0, cells);

            Handles.color = s_Grid;

            for (var x = x0; x <= x1; x++)
                GridLine(data, origin, cell, x, z0, x, z1);

            for (var z = z0; z <= z1; z++)
                GridLine(data, origin, cell, x0, z, x1, z);
        }

        static void GridLine(HBStyleTerrainData data, Vector3 origin, float cell, int fromX, int fromZ, int toX,
                             int toZ)
        {
            var steps = Mathf.Max(Mathf.Abs(toX - fromX), Mathf.Abs(toZ - fromZ));
            var line = new Vector3[steps + 1];

            for (var index = 0; index <= steps; index++)
            {
                var along = steps > 0 ? (float)index / steps : 0f;
                var place = new Vector3(Mathf.Lerp(fromX, toX, along) * cell, 0f,
                                        Mathf.Lerp(fromZ, toZ, along) * cell);

                line[index] = origin + Ground(data, place, GridLift);
            }

            Handles.DrawPolyLine(line);
        }

        static Vector3 Ground(HBStyleTerrainData data, Vector3 local, float lift)
        {
            var u = Mathf.Clamp01(local.x / data.size.x);
            var v = Mathf.Clamp01(local.z / data.size.z);

            local.y = data.terrainData.GetInterpolatedHeight(u, v) + lift;
            return local;
        }
    }
}
