using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainBrush
    {
        const string Key = "HiddenBull.UrpStyle.Terrain.Brush.";
        const double HoldInterval = 0.05;
        const float SizeStep = 1.15f;
        const float SizeMost = 500f;
        const float StrengthStep = 0.05f;
        const float RaiseRate = 0.02f;
        const float BlendRate = 0.5f;
        const float Spacing = 0.15f;
        const int CursorSegments = 64;
        const int GridMargin = 2;
        const int GridReach = 32;
        const float ShapeExtent = 1.4143f;
        const int FootprintCells = 40;
        const float FootprintAlpha = 0.45f;

        static readonly Color s_Raise = new Color(0.3f, 0.7f, 1f, 1f);
        static readonly Color s_Lower = new Color(1f, 0.6f, 0.25f, 1f);
        static readonly Vector3[] s_Circle = new Vector3[CursorSegments + 1];
        static readonly Vector3[] s_Footprint = new Vector3[(FootprintCells + 1) * (FootprintCells + 1)];
        static readonly float[] s_FootprintWeights = new float[(FootprintCells + 1) * (FootprintCells + 1)];

        static readonly Color s_Hole = new Color(1f, 0.35f, 0.3f, 1f);
        static readonly Color s_Grid = new Color(1f, 1f, 1f, 0.3f);

        static Material s_FootprintMaterial;
        static HBStyleTerrain s_Terrain;
        static bool s_Stroking;
        static Vector3 s_Last;
        static float s_Target;
        static int s_Channel = -1;
        static bool s_Shift;
        static double s_Stamped;
        static int s_Version = -1;
        static int s_HandVersion = -1;

        internal static HBStyleTerrainLayer paintLayer;

        internal static bool armed
        {
            get => SessionState.GetBool(Key + "Armed", false);
            set => SessionState.SetBool(Key + "Armed", value);
        }

        internal static StyleTerrainBrushTool tool
        {
            get => (StyleTerrainBrushTool)EditorPrefs.GetInt(Key + "Tool", 0);
            set => EditorPrefs.SetInt(Key + "Tool", (int)value);
        }

        internal static float size
        {
            get => EditorPrefs.GetFloat(Key + "Size", 20f);
            set => EditorPrefs.SetFloat(Key + "Size", Mathf.Max(value, 0.5f));
        }

        internal static float strength
        {
            get => EditorPrefs.GetFloat(Key + "Strength", 0.5f);
            set => EditorPrefs.SetFloat(Key + "Strength", Mathf.Clamp01(value));
        }

        internal static float hardness
        {
            get => EditorPrefs.GetFloat(Key + "Hardness", 0.3f);
            set => EditorPrefs.SetFloat(Key + "Hardness", Mathf.Clamp(value, 0f, 0.99f));
        }

        internal static float height
        {
            get => EditorPrefs.GetFloat(Key + "Height", 0f);
            set => EditorPrefs.SetFloat(Key + "Height", value);
        }

        internal static float noiseSize
        {
            get => EditorPrefs.GetFloat(Key + "NoiseSize", 10f);
            set => EditorPrefs.SetFloat(Key + "NoiseSize", Mathf.Max(value, 0.5f));
        }

        internal static float terraceStep
        {
            get => EditorPrefs.GetFloat(Key + "TerraceStep", 4f);
            set => EditorPrefs.SetFloat(Key + "TerraceStep", Mathf.Max(value, 0.1f));
        }

        internal static void Begin(HBStyleTerrain terrain)
        {
            if (ReferenceEquals(s_Terrain, terrain))
                return;

            Stop();

            if (StyleTerrainPicker.terrain == terrain)
                StyleTerrainPicker.Stop();

            s_Terrain = terrain;
            s_Version = terrain.data != null ? terrain.data.version : -1;
            s_HandVersion = terrain.data != null ? terrain.data.handVersion : -1;
            SceneView.duringSceneGui += OnScene;
            Undo.undoRedoPerformed += OnUndo;
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

            SceneView.duringSceneGui -= OnScene;
            Undo.undoRedoPerformed -= OnUndo;
            s_Terrain = null;
            SceneView.RepaintAll();
        }

        static void OnUndo()
        {
            var data = s_Terrain != null ? s_Terrain.data : null;

            if (data == null || data.terrainData == null)
                return;

            if (data.version != s_Version)
            {
                data.terrainData.SyncHeightmap();
                s_Version = data.version;
            }

            if (data.hand != null && data.handVersion != s_HandVersion)
            {
                data.hand.Apply(false, false);
                s_HandVersion = data.handVersion;
            }

            data.terrainData.SyncTexture(TerrainData.HolesTextureName);
            StyleTerrainPreviewer.Restart(data);
            SceneView.RepaintAll();
        }

        static void OnScene(SceneView view)
        {
            var data = s_Terrain != null ? s_Terrain.data : null;

            if (data == null || data.terrainData == null || !data.valid)
                return;

            var current = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);

            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(id);

            if (current.type == EventType.KeyDown && Shortcut(current.keyCode))
            {
                current.Use();
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            if (current.type == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                Finish();
                current.Use();
                return;
            }

            var hit = StyleTerrainRay.Hit(s_Terrain, HandleUtility.GUIPointToWorldRay(current.mousePosition),
                                          out var point);

            switch (current.type)
            {
                case EventType.MouseMove:
                    view.Repaint();
                    break;

                case EventType.MouseDown when current.button == 0 && !current.alt && hit:
                    if (tool == StyleTerrainBrushTool.SetHeight && current.shift)
                    {
                        height = Mathf.Round((point.y - s_Terrain.transform.position.y) * 10f) * 0.1f;
                        InternalEditorUtility.RepaintAllViews();
                        current.Use();
                        break;
                    }

                    if (!Record(data))
                    {
                        current.Use();
                        break;
                    }

                    GUIUtility.hotControl = id;
                    s_Stroking = true;
                    s_Target = tool == StyleTerrainBrushTool.Flatten
                        ? point.y - s_Terrain.transform.position.y
                        : height;
                    s_Last = point;
                    s_Shift = current.shift;
                    Stamp(data, point, s_Shift);
                    EditorApplication.update -= Hold;
                    EditorApplication.update += Hold;
                    current.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    s_Shift = current.shift;

                    if (hit)
                        Follow(data, point, s_Shift);

                    current.Use();
                    break;

                case EventType.Repaint:
                    if (hit)
                        DrawCursor(data, point, current.shift);

                    break;
            }
        }

        static bool Shortcut(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftBracket:
                    size = Mathf.Max(size / SizeStep, 1f);
                    return true;

                case KeyCode.RightBracket:
                    size = Mathf.Min(size * SizeStep, SizeMost);
                    return true;

                case KeyCode.Minus:
                    strength -= StrengthStep;
                    return true;

                case KeyCode.Equals:
                    strength += StrengthStep;
                    return true;

                case KeyCode.Escape:
                    armed = false;
                    Stop();
                    return true;

                default:
                    return false;
            }
        }

        static bool Record(HBStyleTerrainData data)
        {
            switch (tool)
            {
                case StyleTerrainBrushTool.PaintLayer:
                    s_Channel = StyleTerrainPainter.Channel(data, paintLayer);

                    if (s_Channel < 0)
                        return false;

                    Undo.RegisterCompleteObjectUndo(new Object[] { data.hand, data }, "Paint HB Style Terrain");
                    return true;

                case StyleTerrainBrushTool.Holes:
                    Undo.RegisterCompleteObjectUndo(data.terrainData, "Paint HB Style Terrain Holes");
                    return true;

                default:
                    Undo.RegisterCompleteObjectUndo(new Object[] { data.terrainData, data }, "Sculpt HB Style Terrain");
                    return true;
            }
        }

        static void Follow(HBStyleTerrainData data, Vector3 point, bool shift)
        {
            var step = Mathf.Max(size * Spacing, data.size.x / (data.resolution - 1));
            var path = point - s_Last;

            path.y = 0f;

            var distance = path.magnitude;

            if (distance < step)
                return;

            var direction = path / distance;

            while (distance >= step)
            {
                s_Last += direction * step;
                distance -= step;
                Stamp(data, s_Last, shift);
            }
        }

        static void Hold()
        {
            var data = s_Terrain != null ? s_Terrain.data : null;

            if (!s_Stroking || data == null || tool == StyleTerrainBrushTool.Holes ||
                EditorApplication.timeSinceStartup - s_Stamped < HoldInterval)
                return;

            Stamp(data, s_Last, s_Shift);
            SceneView.RepaintAll();
        }

        static void Finish()
        {
            s_Stroking = false;
            EditorApplication.update -= Hold;

            var data = s_Terrain != null ? s_Terrain.data : null;

            if (data == null || data.terrainData == null)
                return;

            switch (tool)
            {
                case StyleTerrainBrushTool.PaintLayer:
                    if (data.hand != null)
                    {
                        data.HandEdited();
                        EditorUtility.SetDirty(data.hand);
                    }

                    s_HandVersion = data.handVersion;
                    break;

                case StyleTerrainBrushTool.Holes:
                    data.terrainData.SyncTexture(TerrainData.HolesTextureName);
                    data.terrainData.SyncHeightmap();
                    EditorUtility.SetDirty(data.terrainData);
                    break;

                default:
                    data.terrainData.SyncHeightmap();
                    EditorUtility.SetDirty(data.terrainData);
                    s_Version = data.version;
                    break;
            }

            EditorUtility.SetDirty(data);
        }

        static void Stamp(HBStyleTerrainData data, Vector3 point, bool shift)
        {
            s_Stamped = EditorApplication.timeSinceStartup;

            switch (tool)
            {
                case StyleTerrainBrushTool.PaintLayer:
                    StyleTerrainPainter.Stamp(data, s_Channel, point - s_Terrain.transform.position, size * 0.5f,
                                              hardness, strength, shift, StyleTerrainShapes.current);
                    StyleTerrainPreviewer.Restart(data);
                    return;

                case StyleTerrainBrushTool.Holes:
                    StampHoles(data, point, shift);
                    return;
            }

            var terrain = data.terrainData;
            var resolution = terrain.heightmapResolution;
            var bounds = terrain.size;
            var spacing = bounds.x / (resolution - 1);
            var local = point - s_Terrain.transform.position;
            var centreX = local.x / spacing;
            var centreZ = local.z / spacing;
            var shape = StyleTerrainShapes.current;
            var reach = Mathf.Max(size * 0.5f / spacing, 1f);
            var extent = reach * (shape != null ? ShapeExtent : 1f);
            var brush = tool;
            var margin = brush == StyleTerrainBrushTool.Smooth ? 1 : 0;
            var x0 = Mathf.Clamp(Mathf.FloorToInt(centreX - extent) - margin, 0, resolution - 1);
            var z0 = Mathf.Clamp(Mathf.FloorToInt(centreZ - extent) - margin, 0, resolution - 1);
            var x1 = Mathf.Clamp(Mathf.CeilToInt(centreX + extent) + margin, 0, resolution - 1);
            var z1 = Mathf.Clamp(Mathf.CeilToInt(centreZ + extent) + margin, 0, resolution - 1);
            var width = x1 - x0 + 1;
            var length = z1 - z0 + 1;

            if (width < 1 || length < 1)
                return;

            var heights = terrain.GetHeights(x0, z0, width, length);
            var source = brush == StyleTerrainBrushTool.Smooth ? (float[,])heights.Clone() : heights;
            var settings = new StyleTerrainStampSettings
            {
                tool = brush,
                shape = shape,
                offsetX = x0 - centreX,
                offsetZ = z0 - centreZ,
                reach = reach,
                hardness = hardness,
                strength = strength,
                amount = Amount(brush, shift, bounds.y),
                target = s_Target / bounds.y,
                spacing = spacing,
                cornerX = x0 * spacing,
                cornerZ = z0 * spacing,
                noiseSize = noiseSize,
                step = terraceStep / bounds.y
            };

            StyleTerrainStamp.Apply(heights, source, settings);
            terrain.SetHeightsDelayLOD(x0, z0, heights);
            data.UpdateBounds(x0, z0, width, length);
        }

        static void StampHoles(HBStyleTerrainData data, Vector3 point, bool fill)
        {
            var terrain = data.terrainData;
            var cells = terrain.holesResolution;
            var cell = data.size.x / cells;
            var local = point - s_Terrain.transform.position;
            var centreX = local.x / cell - 0.5f;
            var centreZ = local.z / cell - 0.5f;
            var reach = size * 0.5f / cell;
            var x0 = Mathf.Clamp(Mathf.FloorToInt(centreX - reach), 0, cells - 1);
            var z0 = Mathf.Clamp(Mathf.FloorToInt(centreZ - reach), 0, cells - 1);
            var x1 = Mathf.Clamp(Mathf.CeilToInt(centreX + reach), 0, cells - 1);
            var z1 = Mathf.Clamp(Mathf.CeilToInt(centreZ + reach), 0, cells - 1);
            var holes = terrain.GetHoles(x0, z0, x1 - x0 + 1, z1 - z0 + 1);

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    if (StyleTerrainStamp.Weight(null, x - centreX, z - centreZ, reach, hardness) >= 0.5f)
                        holes[z - z0, x - x0] = fill;
                }
            }

            var underX = Mathf.Clamp(Mathf.FloorToInt(local.x / cell), x0, x1);
            var underZ = Mathf.Clamp(Mathf.FloorToInt(local.z / cell), z0, z1);

            holes[underZ - z0, underX - x0] = fill;

            terrain.SetHolesDelayLOD(x0, z0, holes);
        }

        static float Amount(StyleTerrainBrushTool brush, bool shift, float range)
        {
            if (brush == StyleTerrainBrushTool.Raise || brush == StyleTerrainBrushTool.Noise)
                return size * RaiseRate / range * (shift ? -1f : 1f);

            return BlendRate;
        }

        static void Label(HBStyleTerrainData data, Vector3 point, bool shift, Color colour)
        {
            var local = point - s_Terrain.transform.position;
            var ground = $"{local.y:0.0} m";

            switch (tool)
            {
                case StyleTerrainBrushTool.Raise:
                    StyleTerrainGUI.SceneLabel(point, ground, shift ? "Lower" : "Raise", colour);
                    break;

                case StyleTerrainBrushTool.Flatten:
                    StyleTerrainGUI.SceneLabel(point, s_Stroking ? $"{ground} → {s_Target:0.0} m" : ground,
                                               "Flatten", colour);
                    break;

                case StyleTerrainBrushTool.SetHeight:
                    StyleTerrainGUI.SceneLabel(point, $"{ground} → {height:0.0} m", "Set Height", colour);
                    break;

                case StyleTerrainBrushTool.Noise:
                    StyleTerrainGUI.SceneLabel(point, ground, shift ? "Noise, turned over" : "Noise", colour);
                    break;

                case StyleTerrainBrushTool.Terrace:
                    StyleTerrainGUI.SceneLabel(point, ground, $"Steps of {terraceStep:0.#} m", colour);
                    break;

                case StyleTerrainBrushTool.Holes:
                    StyleTerrainGUI.SceneLabel(point, Solid(data, local) ? "Ground" : "Hole",
                                               shift ? "Fill" : "Cut", colour);
                    break;

                case StyleTerrainBrushTool.PaintLayer:
                    var name = paintLayer != null ? paintLayer.name : "No layer";
                    var share = $"{Painted(data, local) * 100f:0}% painted by hand";

                    StyleTerrainGUI.SceneLabel(point, name, shift ? $"Remove ({share})" : share, colour);
                    break;

                default:
                    StyleTerrainGUI.SceneLabel(point, ground, "Smooth", colour);
                    break;
            }
        }

        static bool Solid(HBStyleTerrainData data, Vector3 local)
        {
            var cells = data.terrainData.holesResolution;
            var x = Mathf.Clamp(Mathf.FloorToInt(local.x / data.size.x * cells), 0, cells - 1);
            var z = Mathf.Clamp(Mathf.FloorToInt(local.z / data.size.z * cells), 0, cells - 1);

            return data.terrainData.GetHoles(x, z, 1, 1)[0, 0];
        }

        static float Painted(HBStyleTerrainData data, Vector3 local)
        {
            var channel = data.HandChannel(paintLayer);
            var hand = data.hand;

            if (channel < 0 || hand == null)
                return 0f;

            var resolution = hand.width;
            var x = Mathf.Clamp(Mathf.FloorToInt(local.x / data.size.x * resolution), 0, resolution - 1);
            var z = Mathf.Clamp(Mathf.FloorToInt(local.z / data.size.z * resolution), 0, resolution - 1);

            return hand.GetPixelData<byte>(0, channel >> 2)[(z * resolution + x) * 4 + (channel & 3)] / 255f;
        }

        static void DrawCursor(HBStyleTerrainData data, Vector3 point, bool shift)
        {
            var colour = Colour(data, shift);

            Label(data, point, shift, colour);
            Handles.color = colour;
            Circle(data, point, size * 0.5f);
            Handles.DrawAAPolyLine(2.5f, s_Circle);

            if (hardness > 0.05f && StyleTerrainShapes.round)
            {
                Handles.color = new Color(colour.r, colour.g, colour.b, 0.45f);
                Circle(data, point, size * 0.5f * hardness);
                Handles.DrawAAPolyLine(1.5f, s_Circle);
            }

            if (tool == StyleTerrainBrushTool.Holes)
            {
                DrawGrid(data, point);
                return;
            }

            DrawFootprint(data, point, colour);

            if (!StyleTerrainShapes.round)
            {
                var angle = StyleTerrainShapes.rotation * Mathf.Deg2Rad;
                var tip = point + new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * size * 0.5f;
                var u = Mathf.Clamp01((tip.x - s_Terrain.transform.position.x) / data.size.x);
                var v = Mathf.Clamp01((tip.z - s_Terrain.transform.position.z) / data.size.z);

                tip.y = s_Terrain.transform.position.y + data.terrainData.GetInterpolatedHeight(u, v) + 0.05f;
                Handles.color = colour;
                Handles.DrawAAPolyLine(2.5f, point + Vector3.up * 0.05f, tip);
            }
        }

        static void DrawFootprint(HBStyleTerrainData data, Vector3 point, Color colour)
        {
            var shape = StyleTerrainShapes.current;
            var radius = size * 0.5f;
            var extent = radius * (shape != null ? ShapeExtent : 1f);
            var origin = s_Terrain.transform.position;
            var side = FootprintCells + 1;

            for (var z = 0; z < side; z++)
            {
                for (var x = 0; x < side; x++)
                {
                    var dx = ((float)x / FootprintCells * 2f - 1f) * extent;
                    var dz = ((float)z / FootprintCells * 2f - 1f) * extent;
                    var worldX = point.x + dx;
                    var worldZ = point.z + dz;
                    var u = Mathf.Clamp01((worldX - origin.x) / data.size.x);
                    var v = Mathf.Clamp01((worldZ - origin.z) / data.size.z);
                    var index = z * side + x;

                    s_Footprint[index] = new Vector3(worldX, origin.y + data.terrainData.GetInterpolatedHeight(u, v) +
                                                     0.05f, worldZ);
                    s_FootprintWeights[index] = StyleTerrainStamp.Weight(shape, dx, dz, radius, hardness);
                }
            }

            if (!FootprintMaterial().SetPass(0))
                return;

            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(GL.TRIANGLES);

            for (var z = 0; z < FootprintCells; z++)
            {
                for (var x = 0; x < FootprintCells; x++)
                {
                    var a = z * side + x;
                    var b = a + 1;
                    var c = a + side;
                    var d = c + 1;

                    Vertex(a, colour);
                    Vertex(c, colour);
                    Vertex(b, colour);
                    Vertex(b, colour);
                    Vertex(c, colour);
                    Vertex(d, colour);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        static Material FootprintMaterial()
        {
            if (s_FootprintMaterial != null)
                return s_FootprintMaterial;

            s_FootprintMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            s_FootprintMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            s_FootprintMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            s_FootprintMaterial.SetInt("_Cull", (int)CullMode.Off);
            s_FootprintMaterial.SetInt("_ZWrite", 0);
            s_FootprintMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            return s_FootprintMaterial;
        }

        static void Vertex(int index, Color colour)
        {
            GL.Color(new Color(colour.r, colour.g, colour.b, s_FootprintWeights[index] * FootprintAlpha));
            GL.Vertex(s_Footprint[index]);
        }

        static void DrawGrid(HBStyleTerrainData data, Vector3 point)
        {
            var terrain = data.terrainData;
            var cells = terrain.holesResolution;
            var cell = data.size.x / cells;
            var origin = s_Terrain.transform.position;
            var local = point - origin;
            var reach = Mathf.Min(Mathf.CeilToInt(size * 0.5f / cell) + GridMargin, GridReach);
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

            for (var i = 0; i <= steps; i++)
            {
                var t = steps > 0 ? (float)i / steps : 0f;
                var x = Mathf.Lerp(fromX, toX, t) * cell;
                var z = Mathf.Lerp(fromZ, toZ, t) * cell;
                var u = Mathf.Clamp01(x / data.size.x);
                var v = Mathf.Clamp01(z / data.size.z);

                line[i] = new Vector3(origin.x + x, origin.y + data.terrainData.GetInterpolatedHeight(u, v) + 0.03f,
                                      origin.z + z);
            }

            Handles.DrawPolyLine(line);
        }

        static Color Colour(HBStyleTerrainData data, bool shift)
        {
            switch (tool)
            {
                case StyleTerrainBrushTool.Raise:
                case StyleTerrainBrushTool.Noise:
                    return shift ? s_Lower : s_Raise;

                case StyleTerrainBrushTool.Holes:
                    return shift ? s_Raise : s_Hole;

                case StyleTerrainBrushTool.PaintLayer:
                    if (shift)
                        return s_Lower;

                    for (var index = 0; index < data.layerCount; index++)
                    {
                        if (paintLayer != null && data.layers[index].layer == paintLayer)
                            return StyleTerrainLayerList.Tint(index);
                    }

                    return s_Raise;

                default:
                    return s_Raise;
            }
        }

        static void Circle(HBStyleTerrainData data, Vector3 centre, float radius)
        {
            var origin = s_Terrain.transform.position;
            var bounds = data.size;

            for (var i = 0; i <= CursorSegments; i++)
            {
                var angle = i * Mathf.PI * 2f / CursorSegments;
                var x = centre.x + Mathf.Cos(angle) * radius;
                var z = centre.z + Mathf.Sin(angle) * radius;
                var u = Mathf.Clamp01((x - origin.x) / bounds.x);
                var v = Mathf.Clamp01((z - origin.z) / bounds.z);

                s_Circle[i] = new Vector3(x, origin.y + data.terrainData.GetInterpolatedHeight(u, v) + 0.05f, z);
            }
        }
    }
}
