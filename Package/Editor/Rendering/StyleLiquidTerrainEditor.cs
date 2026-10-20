using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleLiquidTerrain))]
    sealed class StyleLiquidTerrainEditor : UnityEditor.Editor
    {
        const int Off = 0;
        const int Raise = 1;
        const int SetHeight = 2;
        const int Smooth = 3;
        const int Lake = 4;
        const int Ramp = 5;
        const int Paint = 6;
        const int Flow = 7;
        const int Calm = 8;
        const int Downhill = 9;
        const int Whirl = 10;
        const int Hush = 11;
        const float SizeLeast = 1f;
        const float SizeMost = 200f;
        const float SizeStep = 1.15f;
        const float ForceStep = 0.05f;
        const float FlowMost = 6f;
        const float FlowStep = 0.25f;
        const float FlowBlend = 0.5f;
        const float FlowLeast = 0.01f;
        const float FallFull = 0.1f;
        const float WhirlPull = 0.35f;
        const float WhirlDip = 0.2f;
        const float ShowReach = 30f;
        const int ShowCount = 16;
        const float ShowLift = 0.3f;
        const float ShowGlow = 0.3f;
        const byte Unknown = 0;
        const byte Hollow = 1;
        const byte Solid = 2;
        const byte Flooded = 3;
        const int FillFringe = 2;
        const float RaiseStep = 0.25f;
        const float Farthest = 100000f;
        const string TabKey = "HiddenBull.LiquidTerrain.Tab";
        const float RampCore = 0.7f;
        const float GuideIcon = 34f;
        const float HeaderHeight = 22f;
        const int TileColumns = 5;
        const float TileHeight = 58f;
        const float TileGap = 3f;
        const float CaptionHeight = 16f;
        const float PreviewSize = 60f;
        const int SwatchStrips = 8;

        static readonly GUIContent s_Distance = new GUIContent(
            "Distance",
            "How far the liquid is drawn from the camera, in kilometres. Shorter draws fewer vertices; let fog " +
            "or the shape of the land hide where it ends.");

        static readonly GUIContent s_New = new GUIContent(
            "New Data", "Creates the asset that stores what is sculpted and painted on this liquid.");

        static readonly GUIContent s_Fit = new GUIContent(
            "Fit To Terrains",
            "Moves and sizes the area so it covers every Terrain in the scene. The height is left alone.");

        static readonly GUIContent[] s_Tabs =
        {
            new GUIContent("Liquid", "The materials and how far the liquid is drawn."),
            new GUIContent("Area", "Where the liquid can be sculpted and painted, and the asset that stores it."),
            new GUIContent("Tools", "Sculpt the level, paint materials and paint the flow.")
        };

        static readonly GUIContent s_Level = new GUIContent("Level");
        static readonly GUIContent s_Material = new GUIContent("Material");
        static readonly GUIContent s_Flowing = new GUIContent("Flow");
        static readonly GUIContent s_Waving = new GUIContent("Waves");
        static readonly GUIContent s_Clear = new GUIContent("Clear");

        static readonly int[] s_LevelTools = { Raise, SetHeight, Smooth, Lake, Ramp };

        static readonly GUIContent[] s_LevelNames =
        {
            new GUIContent("Raise", "Paint to raise the liquid. Hold Shift to lower it."),
            new GUIContent("Set Height", "Paint to bring the liquid to Height."),
            new GUIContent("Smooth", "Paint to even out steps and bumps in the level."),
            new GUIContent("Lake", "Click a hollow to fill it, then drag the arrow to set how high the liquid stands."),
            new GUIContent("Ramp", "Click along a river bed to lay an even slope as wide as the brush.")
        };

        static readonly int[] s_FlowTools = { Flow, Calm, Downhill, Whirl };

        static readonly GUIContent[] s_FlowNames =
        {
            new GUIContent("Flow", "Drag to make the liquid flow that way. Hold Shift to stop the flow."),
            new GUIContent("Smooth Flow", "Paint to even out the painted directions and speeds."),
            new GUIContent("Auto Flow", "Paint to make the liquid flow downhill, faster where it is steeper."),
            new GUIContent("Whirlpool", "Click to stamp a whirlpool as wide as the brush.")
        };

        static readonly int[] s_WaveTools = { Hush };

        static readonly GUIContent[] s_WaveNames =
        {
            new GUIContent("Calm", "Paint to calm the waves. Hold Shift to bring them back.")
        };

        static readonly GUIContent[] s_ClearNames =
        {
            new GUIContent("Level", "Makes the liquid flat again everywhere."),
            new GUIContent("Materials", "Paints the first material back everywhere."),
            new GUIContent("No Liquid", "Brings the liquid back everywhere it was painted away."),
            new GUIContent("Flow", "Stops the flow everywhere."),
            new GUIContent("Calm", "Brings the waves back everywhere they were calmed.")
        };

        static readonly string[] s_Titles =
        {
            "Liquid Tools", "Raise / Lower", "Set Height", "Smooth", "Lake", "Ramp", "Paint Material", "Flow",
            "Smooth Flow", "Auto Flow", "Whirlpool", "Calm Waves"
        };

        static readonly string[] s_Help =
        {
            "Open the Tools tab, pick a tool, then paint in the Scene view.",
            "Paint to raise the liquid level under the brush.",
            "Paint to bring the liquid to the height in the Height field.",
            "Paint to even out steps and bumps in the level.",
            "Click a hollow to fill it. The liquid shows the lake while you drag the arrow to set how high it " +
            "stands. Fill floods the hollow; Circle and Square stay inside their own outline. Nothing is kept " +
            "until you lay it.",
            "Click along a river bed to lay an even slope. The liquid shows the result while you place and drag " +
            "the points; nothing is kept until you lay it. Flow Speed makes the liquid run down the slope.",
            "Paint the selected material over the liquid. The first material paints the others away; No Liquid " +
            "removes the liquid and leaves the ground dry.",
            "Drag in the direction the liquid should flow. The surface glows green where it flows fast.",
            "Paint to even out the painted directions and speeds.",
            "Paint to make the liquid flow down its own slope. Flat liquid stays still and steep liquid " +
            "reaches the full Speed.",
            "Click to stamp a whirlpool: the liquid turns around the point and dips in the middle. Strength " +
            "sets how deep it dips.",
            "Paint to calm the waves under the brush, for a sheltered bay or a still lake. Floating objects " +
            "follow the calmer surface too."
        };

        static readonly string[][] s_Hints =
        {
            new string[0],
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
            new[] { "Shift", "bring waves back" }
        };

        static readonly string[] s_Sizing = { "[  ]", "brush size" };
        static readonly string[] s_Forcing = { "-  =", "strength" };
        static readonly string[] s_Speeding = { "-  =", "speed" };
        static readonly string[] s_Stopping = { "Esc", "stop" };
        static readonly char[] s_Breaks = { '_', ' ', '-', '.' };

        static readonly GUIContent s_BrushSize = new GUIContent("Brush Size", "The width of the brush, in metres.");

        static readonly GUIContent s_Strength = new GUIContent(
            "Strength", "How fast the brush works. Low values build up slowly over several strokes.");

        static readonly GUIContent s_Speed = new GUIContent(
            "Speed", "How fast the painted liquid flows, in metres per second.");

        static readonly GUIContent s_Course = new GUIContent(
            "Flow Speed", "How fast the liquid flows down the ramp, in metres per second. 0 leaves the flow " +
            "as it is.");

        static readonly GUIContent s_Depth = new GUIContent(
            "Depth", "How far above the ground under each clicked point the liquid sits, in metres.");

        static readonly GUIContent[] s_Forms =
        {
            new GUIContent("Fill", "Floods the hollow under the point. It spills over if the level is above the rim."),
            new GUIContent("Circle", "A round lake of the size you set. It never spreads past its outline."),
            new GUIContent("Square", "A square lake of the size you set. It never spreads past its outline.")
        };

        static readonly GUIContent s_Fill = new GUIContent(
            "Lay Lake", "Keeps the lake as shown. The same as pressing Enter in the Scene view.");

        static readonly GUIContent s_Lay = new GUIContent(
            "Lay Ramp", "Lays the slope through the clicked points. The same as pressing Enter in the Scene view.");

        static readonly GUIContent s_Height = new GUIContent(
            "Height", "The world height Set Height brings the liquid to.");

        static readonly Vector3[] s_Quad = new Vector3[4];
        static readonly int s_ShallowId = Shader.PropertyToID("_DepthColor0");
        static readonly int s_DeepId = Shader.PropertyToID("_DepthColor5");
        static readonly Color s_LevelTint = new Color(0.35f, 0.65f, 0.95f);
        static readonly Color s_MaterialTint = new Color(0.95f, 0.6f, 0.25f);
        static readonly Color s_FlowTint = new Color(0.4f, 0.85f, 0.45f);
        static readonly Color s_WaveTint = new Color(0.7f, 0.55f, 0.95f);

        static readonly GUIContent s_NoLiquid = new GUIContent(
            "No Liquid", "Paints the liquid away and leaves the ground there dry. Paint any material to " +
                         "bring it back.");

        static GUIStyle s_Caption;
        static GUIStyle s_Title;
        static GUIStyle s_Text;
        static GUIStyle s_Chip;
        static GUIStyle s_Heading;
        static float[] s_Held;
        static HBStyleLiquidTerrainData s_Previewed;

        static readonly GUIContent s_Empty = new GUIContent(
            "Show Empty Areas",
            "Marks in red, in the Scene view, where nothing flows while a Flow tool is active and where the " +
            "liquid is painted away while No Liquid is active.");

        static int s_Tool;
        static float s_Size = 20f;
        static float s_Force = 0.5f;
        static float s_Rate = 3f;
        static float s_Target;
        static float[] s_Before;
        static int s_Layer;
        static Vector3 s_Last;
        static float s_Above = 0.5f;
        static bool s_Marks = true;
        static int s_Form;
        static readonly List<Vector4> s_Path = new List<Vector4>();
        static readonly List<int> s_Filled = new List<int>();
        static byte[] s_State;
        static float[] s_Soil;

        SerializedProperty m_Layers;
        SerializedProperty m_Distance;
        SerializedProperty m_Size;
        SerializedProperty m_Data;

        struct Patch
        {
            public Vector2 at;
            public Vector2 texel;
            public float radius;
            public int resolution;
            public int left;
            public int right;
            public int bottom;
            public int top;

            public bool empty => left > right || bottom > top;

            public float Weight(int x, int y)
            {
                var offset = new Vector2((x - at.x) * texel.x, (y - at.y) * texel.y);
                var weight = 1f - offset.magnitude / radius;

                return weight <= 0f ? 0f : weight * weight * (3f - 2f * weight);
            }
        }

        void OnEnable()
        {
            m_Layers = serializedObject.FindProperty("m_Layers");
            m_Distance = serializedObject.FindProperty("m_Distance");
            m_Size = serializedObject.FindProperty("m_Extent");
            m_Data = serializedObject.FindProperty("m_Data");

            Undo.undoRedoPerformed += Restore;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= Restore;
            Cancel();
        }

        void Restore()
        {
            var terrain = target as HBStyleLiquidTerrain;

            s_Previewed = null;
            s_Path.Clear();

            if (terrain != null && terrain.data != null)
                terrain.data.Upload();

            SceneView.RepaintAll();
        }

        void Guide(HBStyleLiquidTerrain terrain)
        {
            var tint = s_Tool == Off ? new Color(0.6f, 0.6f, 0.6f) : Tint();
            var title = s_Titles[s_Tool];

            if (s_Tool == Paint)
            {
                var count = Mathf.Min(terrain.layers.Count, StyleLiquidLayers.Most);

                title = s_Layer >= count ? s_NoLiquid.text : "Paint " + Caption(terrain, s_Layer);
            }

            s_Title ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            s_Text ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel) { richText = false };
            s_Chip ??= new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 1, 2),
                margin = new RectOffset(2, 2, 2, 2)
            };

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

                        if (s_Tool == Off || s_Tool == Paint)
                            Handles.DrawSolidDisc(icon.center, Vector3.forward, GuideIcon * 0.22f);
                        else
                            Glyph(s_Tool, new Rect(icon.x + 5f, icon.y + 5f, icon.width - 10f, icon.height - 10f));
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Label(title, s_Title);
                        GUILayout.Label(s_Help[s_Tool], s_Text);
                    }
                }

                if (s_Tool != Off)
                {
                    EditorGUILayout.Space(2f);
                    Chips(s_Hints[s_Tool], s_Tool == Lake && s_Form == 0 ? null : s_Sizing, Forces(), s_Stopping);
                }

                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(box.rect.x, box.rect.y, 3f, box.rect.height), tint);
            }
        }

        static string[] Forces()
        {
            if (Rated())
                return s_Speeding;

            return s_Tool == Lake ? null : s_Forcing;
        }

        static void Chips(params string[][] groups)
        {
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

        public override void OnInspectorGUI()
        {
            var terrain = (HBStyleLiquidTerrain)target;
            var data = terrain.data;

            if (data == null)
                s_Tool = Off;

            serializedObject.Update();

            Guide(terrain);

            if (FindObjectsByType<HBStyleLiquidTerrain>().Length > 1)
                EditorGUILayout.HelpBox("There is more than one Liquid Terrain in the scene. Shore foam, wet " +
                                        "shores and calm shallows follow only one of them.", MessageType.Warning);

            var tab = GUILayout.Toolbar(SessionState.GetInt(TabKey, 2), s_Tabs);
            var create = false;

            SessionState.SetInt(TabKey, tab);
            EditorGUILayout.Space();

            if (tab == 0)
            {
                EditorGUILayout.PropertyField(m_Layers, true);
                Distance();
            }
            else if (tab == 1)
            {
                EditorGUILayout.PropertyField(m_Size, new GUIContent("Size", m_Size.tooltip));
                EditorGUILayout.PropertyField(m_Data);

                create = m_Data.objectReferenceValue == null && GUILayout.Button(s_New);
            }

            serializedObject.ApplyModifiedProperties();

            if (create)
            {
                Create(terrain);
                GUIUtility.ExitGUI();
            }

            if (tab == 1)
            {
                using (new EditorGUI.DisabledScope(Terrain.activeTerrains.Length == 0))
                {
                    if (GUILayout.Button(s_Fit))
                        Fit(terrain);
                }
            }

            if (tab != 2)
                return;

            if (data == null)
                EditorGUILayout.HelpBox("Create or assign Data to sculpt and paint the liquid.", MessageType.Info);

            using (new EditorGUI.DisabledScope(data == null))
                Tools(terrain, data);
        }

        void Tools(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data)
        {
            var before = s_Tool;

            EditorGUI.BeginChangeCheck();

            Palette(s_Level, s_LevelTint, s_LevelNames, s_LevelTools);
            Swatches(terrain);
            Palette(s_Flowing, s_FlowTint, s_FlowNames, s_FlowTools);
            Palette(s_Waving, s_WaveTint, s_WaveNames, s_WaveTools);

            if (s_Tool != before)
                Cancel();

            EditorGUILayout.Space();

            if (s_Tool != Off)
                Brush(terrain, data);

            if (EditorGUI.EndChangeCheck())
            {
                if ((s_Tool == Ramp || s_Tool == Lake) && data != null)
                    Preview(terrain, data);

                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(s_Clear, EditorStyles.miniBoldLabel, GUILayout.Width(40f));

                for (var index = 0; index < s_ClearNames.Length; index++)
                {
                    if (GUILayout.Button(s_ClearNames[index], Style(index, s_ClearNames.Length)) && data != null)
                        Clear(data, index);
                }
            }
        }

        static bool Streamed()
        {
            return s_Tool == Flow || s_Tool == Calm || s_Tool == Downhill || s_Tool == Whirl;
        }

        static bool Rated()
        {
            return s_Tool == Flow || s_Tool == Downhill || s_Tool == Whirl || s_Tool == Ramp;
        }

        static Color Tint()
        {
            if (s_Tool == Paint)
                return s_MaterialTint;

            if (s_Tool == Hush)
                return s_WaveTint;

            return Streamed() ? s_FlowTint : s_LevelTint;
        }

        static void Brush(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data)
        {
            var tint = Tint();
            var width = EditorGUIUtility.labelWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                var preview = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize));

                if (Event.current.type == EventType.Repaint)
                {
                    var share = Mathf.InverseLerp(Mathf.Log(SizeLeast), Mathf.Log(SizeMost), Mathf.Log(s_Size));
                    var radius = s_Tool == Lake && s_Form == 0 ? 4f : Mathf.Lerp(5f, PreviewSize * 0.46f, share);
                    var strength = s_Tool == Flow || s_Tool == Downhill ? s_Rate / FlowMost : s_Force;

                    EditorGUI.DrawRect(preview, new Color(0f, 0f, 0f, 0.25f));
                    Frame(preview, new Color(1f, 1f, 1f, 0.12f));

                    Handles.color = new Color(tint.r, tint.g, tint.b, 0.12f + 0.25f * strength);
                    Handles.DrawSolidDisc(preview.center, Vector3.forward, radius);
                    Handles.color = new Color(tint.r, tint.g, tint.b, 0.2f + 0.6f * strength);
                    Handles.DrawSolidDisc(preview.center, Vector3.forward, radius * 0.5f);
                    Handles.color = tint;
                    Handles.DrawWireDisc(preview.center, Vector3.forward, radius);
                }

                EditorGUIUtility.labelWidth = 80f;

                using (new EditorGUILayout.VerticalScope())
                {
                    if (s_Tool == Lake)
                        s_Form = GUILayout.Toolbar(s_Form, s_Forms);

                    if (s_Tool != Lake || s_Form != 0)
                        s_Size = EditorGUILayout.Slider(s_BrushSize, s_Size, SizeLeast, SizeMost);

                    if (s_Tool == Raise || s_Tool == SetHeight || s_Tool == Smooth || s_Tool == Paint
                        || s_Tool == Calm || s_Tool == Whirl || s_Tool == Hush)
                        s_Force = EditorGUILayout.Slider(s_Strength, s_Force, 0.01f, 1f);

                    if (s_Tool == Ramp)
                        s_Rate = EditorGUILayout.Slider(s_Course, s_Rate, 0f, FlowMost);
                    else if (Rated())
                        s_Rate = EditorGUILayout.Slider(s_Speed, Mathf.Max(s_Rate, FlowStep), FlowStep, FlowMost);

                    if (s_Tool == SetHeight)
                        s_Target = EditorGUILayout.FloatField(s_Height, s_Target);

                    if (Streamed()
                        || (s_Tool == Paint && s_Layer >= Mathf.Min(terrain.layers.Count, StyleLiquidLayers.Most)))
                        s_Marks = EditorGUILayout.ToggleLeft(s_Empty, s_Marks);

                    if (s_Tool == Ramp || s_Tool == Lake)
                    {
                        s_Above = EditorGUILayout.FloatField(s_Depth, s_Above);

                        using (new EditorGUI.DisabledScope(s_Path.Count < (s_Tool == Lake ? 1 : 2)))
                        {
                            if (GUILayout.Button(s_Tool == Lake ? s_Fill : s_Lay))
                                Lay(terrain, data);
                        }
                    }
                }

                EditorGUIUtility.labelWidth = width;
            }
        }

        static GUIStyle Style(int index, int count)
        {
            if (count == 1)
                return EditorStyles.miniButton;

            if (index == 0)
                return EditorStyles.miniButtonLeft;

            return index == count - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
        }

        static void Header(GUIContent label, Color tint)
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

            s_Heading ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            rect.xMin += 10f;

            GUI.Label(rect, label, s_Heading);
            EditorGUILayout.Space(2f);
        }

        static void Frame(Rect rect, Color colour)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), colour);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), colour);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), colour);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), colour);
        }

        static Rect Slot(Rect row, int index)
        {
            var width = (row.width - TileGap * (TileColumns - 1)) / TileColumns;

            return new Rect(row.x + index * (width + TileGap), row.y, width, row.height);
        }

        static bool Tile(Rect rect, bool active, Color tint, GUIContent content)
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

            if (!GUI.enabled || current.type != EventType.MouseDown || current.button != 0
                || !rect.Contains(current.mousePosition))
                return false;

            current.Use();
            GUI.changed = true;

            return true;
        }

        static void Palette(GUIContent label, Color tint, GUIContent[] names, int[] tools)
        {
            Header(label, tint);

            var row = EditorGUILayout.GetControlRect(false, TileHeight);

            for (var index = 0; index < names.Length; index++)
            {
                var rect = Slot(row, index);
                var active = s_Tool == tools[index];

                if (Tile(rect, active, tint, names[index]))
                    s_Tool = active ? Off : tools[index];

                if (Event.current.type != EventType.Repaint)
                    continue;

                var side = Mathf.Min(rect.width - 8f, rect.height - CaptionHeight - 6f);

                Handles.color = active ? Color.white : new Color(tint.r, tint.g, tint.b, 0.9f);
                Glyph(tools[index], new Rect(rect.center.x - side * 0.5f, rect.y + 4f, side, side));
            }
        }

        static void Swatches(HBStyleLiquidTerrain terrain)
        {
            var layers = terrain.layers;
            var count = Mathf.Min(layers.Count, StyleLiquidLayers.Most);

            Header(s_Material, s_MaterialTint);

            var row = EditorGUILayout.GetControlRect(false, TileHeight);

            for (var index = 0; index <= count; index++)
            {
                var rect = Slot(row, index);
                var active = s_Tool == Paint && s_Layer == index;
                var material = index < count ? layers[index] : null;
                var content = index == count ? s_NoLiquid : Named(terrain, index);

                if (Tile(rect, active, s_MaterialTint, content))
                {
                    s_Tool = active ? Off : Paint;
                    s_Layer = index;
                }

                if (Event.current.type != EventType.Repaint)
                    continue;

                var swatch = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - CaptionHeight - 8f);

                if (index == count)
                {
                    EditorGUI.DrawRect(swatch, new Color(0f, 0f, 0f, 0.35f));
                    Handles.color = new Color(0.95f, 0.3f, 0.25f);
                    Ink(swatch, 0.3f, 0.25f, 0.7f, 0.75f);
                    Ink(swatch, 0.7f, 0.25f, 0.3f, 0.75f);
                    continue;
                }

                var shallow = Shade(material, s_ShallowId, new Color(0.3f, 0.3f, 0.3f));
                var deep = Shade(material, s_DeepId, new Color(0.15f, 0.15f, 0.15f));

                for (var strip = 0; strip < SwatchStrips; strip++)
                {
                    var height = swatch.height / SwatchStrips;

                    EditorGUI.DrawRect(new Rect(swatch.x, swatch.y + strip * height, swatch.width, height + 0.5f),
                                       Color.Lerp(shallow, deep, strip / (SwatchStrips - 1f)));
                }
            }

            if (s_Tool == Paint)
                s_Layer = Mathf.Min(s_Layer, count);
        }

        static string Caption(HBStyleLiquidTerrain terrain, int index)
        {
            var layers = terrain.layers;
            var count = Mathf.Min(layers.Count, StyleLiquidLayers.Most);

            if (index >= count || layers[index] == null)
                return "None";

            var title = layers[index].name;
            string shared = null;
            var named = 0;

            for (var other = 0; other < count; other++)
            {
                if (layers[other] == null)
                    continue;

                var name = layers[other].name;

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

            var cut = named > 1 && shared != null ? shared.LastIndexOfAny(s_Breaks) + 1 : 0;

            return cut > 0 && cut < title.Length ? title.Substring(cut) : title;
        }

        static GUIContent Named(HBStyleLiquidTerrain terrain, int index)
        {
            var layers = terrain.layers;
            var title = index < layers.Count && layers[index] != null ? layers[index].name : "None";

            return new GUIContent(Caption(terrain, index),
                                  title + (index == 0 ? "\nPaints the first material back, removing the others."
                                                      : "\nPaints this material over the liquid."));
        }

        static Color Shade(Material material, int id, Color fallback)
        {
            if (material == null || !material.HasColor(id))
                return fallback;

            var colour = material.GetColor(id).gamma;

            return new Color(Mathf.Min(colour.r, 1f), Mathf.Min(colour.g, 1f), Mathf.Min(colour.b, 1f), 1f);
        }

        static void Ink(Rect rect, params float[] points)
        {
            var line = new Vector3[points.Length / 2];

            for (var index = 0; index < line.Length; index++)
            {
                line[index] = new Vector3(rect.x + points[index * 2] * rect.width,
                                          rect.y + points[index * 2 + 1] * rect.height, 0f);
            }

            Handles.DrawAAPolyLine(2f, line);
        }

        static void Dot(Rect rect, float x, float y)
        {
            Handles.DrawSolidDisc(new Vector3(rect.x + rect.width * x, rect.y + rect.height * y), Vector3.forward,
                                  2.5f);
        }

        static void Glyph(int tool, Rect rect)
        {
            if (tool == Raise)
            {
                Ink(rect, 0.1f, 0.8f, 0.3f, 0.7f, 0.5f, 0.8f, 0.7f, 0.7f, 0.9f, 0.8f);
                Ink(rect, 0.5f, 0.58f, 0.5f, 0.15f);
                Ink(rect, 0.34f, 0.32f, 0.5f, 0.15f, 0.66f, 0.32f);
            }
            else if (tool == SetHeight)
            {
                Ink(rect, 0.1f, 0.4f, 0.9f, 0.4f);
                Ink(rect, 0.1f, 0.26f, 0.1f, 0.54f);
                Ink(rect, 0.9f, 0.26f, 0.9f, 0.54f);
                Ink(rect, 0.1f, 0.85f, 0.35f, 0.68f, 0.6f, 0.85f, 0.9f, 0.72f);
            }
            else if (tool == Smooth)
            {
                Ink(rect, 0.1f, 0.62f, 0.2f, 0.32f, 0.3f, 0.68f, 0.4f, 0.38f, 0.5f, 0.58f, 0.65f, 0.48f, 0.9f, 0.5f);
            }
            else if (tool == Lake)
            {
                Ink(rect, 0.1f, 0.22f, 0.25f, 0.82f, 0.75f, 0.82f, 0.9f, 0.22f);
                Ink(rect, 0.18f, 0.5f, 0.82f, 0.5f);
                Ink(rect, 0.22f, 0.64f, 0.78f, 0.64f);
            }
            else if (tool == Ramp)
            {
                Ink(rect, 0.12f, 0.25f, 0.5f, 0.45f, 0.88f, 0.78f);
                Dot(rect, 0.12f, 0.25f);
                Dot(rect, 0.5f, 0.45f);
                Dot(rect, 0.88f, 0.78f);
            }
            else if (tool == Hush)
            {
                Ink(rect, 0.1f, 0.3f, 0.25f, 0.18f, 0.4f, 0.3f, 0.55f, 0.42f, 0.7f, 0.3f, 0.9f, 0.22f);
                Ink(rect, 0.1f, 0.56f, 0.3f, 0.5f, 0.5f, 0.56f, 0.7f, 0.62f, 0.9f, 0.56f);
                Ink(rect, 0.1f, 0.8f, 0.9f, 0.8f);
            }
            else if (tool == Downhill)
            {
                Ink(rect, 0.1f, 0.25f, 0.45f, 0.3f, 0.6f, 0.7f, 0.9f, 0.75f);
                Ink(rect, 0.5f, 0.12f, 0.72f, 0.4f);
                Ink(rect, 0.56f, 0.38f, 0.72f, 0.4f, 0.7f, 0.24f);
            }
            else if (tool == Whirl)
            {
                Ink(rect, 0.5f, 0.5f, 0.6f, 0.42f, 0.56f, 0.3f, 0.4f, 0.28f, 0.26f, 0.42f, 0.28f, 0.64f, 0.46f, 0.78f,
                    0.7f, 0.74f, 0.84f, 0.54f, 0.82f, 0.3f);
            }
            else if (tool == Flow)
            {
                Ink(rect, 0.12f, 0.35f, 0.85f, 0.35f);
                Ink(rect, 0.68f, 0.22f, 0.85f, 0.35f, 0.68f, 0.48f);
                Ink(rect, 0.12f, 0.7f, 0.85f, 0.7f);
                Ink(rect, 0.68f, 0.57f, 0.85f, 0.7f, 0.68f, 0.83f);
            }
            else
            {
                Ink(rect, 0.1f, 0.5f, 0.25f, 0.36f, 0.4f, 0.5f, 0.55f, 0.64f, 0.7f, 0.5f, 0.86f, 0.4f);
                Ink(rect, 0.72f, 0.3f, 0.86f, 0.4f, 0.78f, 0.56f);
            }
        }

        static void Clear(HBStyleLiquidTerrainData data, int what)
        {
            if (!EditorUtility.DisplayDialog("Clear " + s_ClearNames[what].text, s_ClearNames[what].tooltip,
                                             "Clear", "Cancel"))
                return;

            Undo.RegisterCompleteObjectUndo(data, "Clear Liquid " + s_ClearNames[what].text);

            if (what == 0)
            {
                System.Array.Clear(data.levels, 0, data.levels.Length);
            }
            else if (what == 4)
            {
                System.Array.Clear(data.still, 0, data.still.Length);
            }
            else if (what == 3)
            {
                System.Array.Clear(data.flow, 0, data.flow.Length);
            }
            else
            {
                var map = data.weights;

                for (var index = 0; index < map.Length; index++)
                {
                    if ((index % 4 == 3) == (what == 2))
                        map[index] = 0;
                }
            }

            data.Upload();
            EditorUtility.SetDirty(data);
            SceneView.RepaintAll();
            GUIUtility.ExitGUI();
        }

        void Distance()
        {
            var steps = HBStyleLiquidTerrain.Distances;
            var current = 0;

            for (var index = 0; index < steps.Length; index++)
            {
                if (Mathf.Abs(steps[index] - m_Distance.floatValue) < Mathf.Abs(steps[current] - m_Distance.floatValue))
                    current = index;
            }

            var rect = EditorGUILayout.GetControlRect();
            var label = new Rect(rect.xMax - 56f, rect.y, 56f, rect.height);

            rect.xMax -= 60f;

            EditorGUI.BeginProperty(rect, s_Distance, m_Distance);
            EditorGUI.BeginChangeCheck();

            var picked = Mathf.RoundToInt(GUI.HorizontalSlider(EditorGUI.PrefixLabel(rect, s_Distance), current, 0f,
                                                               steps.Length - 1));

            if (EditorGUI.EndChangeCheck())
                m_Distance.floatValue = steps[picked];

            EditorGUI.EndProperty();
            EditorGUI.LabelField(label, steps[picked].ToString("0.#") + " KM");
        }

        static void Create(HBStyleLiquidTerrain terrain)
        {
            var scene = terrain.gameObject.scene.path;
            var folder = string.IsNullOrEmpty(scene) ? "Assets" : Path.GetDirectoryName(scene);
            var path = EditorUtility.SaveFilePanelInProject("New Liquid Terrain Data", "Liquid Terrain Data", "asset",
                                                            "Choose where to save what is sculpted on this liquid.",
                                                            folder);

            if (string.IsNullOrEmpty(path))
                return;

            var data = CreateInstance<HBStyleLiquidTerrainData>();

            AssetDatabase.CreateAsset(data, path);

            Undo.RecordObject(terrain, "New Liquid Terrain Data");
            terrain.data = data;
            EditorUtility.SetDirty(terrain);
        }

        static void Fit(HBStyleLiquidTerrain liquid)
        {
            var found = false;
            var bounds = new Bounds();

            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain.terrainData == null)
                    continue;

                var size = terrain.terrainData.size;
                var area = new Bounds(terrain.transform.position + size * 0.5f, size);

                if (found)
                    bounds.Encapsulate(area);
                else
                    bounds = area;

                found = true;
            }

            if (!found)
                return;

            var own = liquid.transform;

            Undo.RecordObjects(new Object[] { liquid, own }, "Fit Liquid Terrain");

            own.position = new Vector3(bounds.center.x, own.position.y, bounds.center.z);
            liquid.size = new Vector2(bounds.size.x, bounds.size.z);

            EditorUtility.SetDirty(liquid);
        }

        void OnSceneGUI()
        {
            var terrain = (HBStyleLiquidTerrain)target;
            var data = terrain.data;

            if (s_Tool == Off || data == null)
                return;

            var current = Event.current;
            var control = GUIUtility.GetControlID(FocusType.Passive);

            if ((s_Tool == Ramp || s_Tool == Lake) && Pins())
                Preview(terrain, data);

            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(control);
                return;
            }

            if (current.type == EventType.KeyDown && (s_Tool == Ramp || s_Tool == Lake)
                && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
            {
                Lay(terrain, data);
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.KeyDown && Key(current.keyCode))
            {
                if (s_Tool == Ramp || s_Previewed != null)
                    Preview(terrain, data);

                current.Use();
                Repaint();
                SceneView.RepaintAll();
                return;
            }

            if (current.type == EventType.MouseUp && GUIUtility.hotControl == control)
            {
                GUIUtility.hotControl = 0;
                current.Use();
                return;
            }

            var ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);

            if (!Aim(terrain, ray, out var point))
                return;

            if (current.type == EventType.Repaint)
            {
                Draw(terrain, data, point);
                return;
            }

            if (current.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
                return;
            }

            if (current.alt || current.button != 0)
                return;

            if (current.type == EventType.MouseDown && HandleUtility.nearestControl == control)
            {
                GUIUtility.hotControl = control;
                s_Last = point;

                if (current.shift && s_Tool == SetHeight)
                {
                    s_Target = point.y;
                    Repaint();
                    current.Use();
                    return;
                }

                if (s_Tool == Ramp || s_Tool == Lake)
                {
                    var placed = Pin(terrain, point, current.shift && s_Tool == Ramp);

                    if (s_Tool == Lake)
                    {
                        if (s_Path.Count > 0)
                            placed.w = s_Path[0].w;

                        s_Path.Clear();
                    }

                    s_Path.Add(placed);
                    Preview(terrain, data);
                    Repaint();
                    current.Use();
                    return;
                }

                Undo.RegisterCompleteObjectUndo(data, "Sculpt Liquid");
            }
            else if (current.type != EventType.MouseDrag || GUIUtility.hotControl != control || s_Tool == Lake
                     || s_Tool == Ramp || (current.shift && s_Tool == SetHeight))
            {
                return;
            }

            if (s_Tool == Paint)
                Daub(terrain, data, point);
            else if (s_Tool == Flow)
                Run(terrain, data, point, current.shift);
            else if (s_Tool == Calm)
                Settle(terrain, data, point);
            else if (s_Tool == Hush)
                Lull(terrain, data, point, current.shift);
            else if (s_Tool == Downhill)
                Descend(terrain, data, point);
            else if (s_Tool == Whirl)
            {
                if (current.type == EventType.MouseDown)
                    Swirl(terrain, data, point, current.shift);
            }
            else
                Stroke(terrain, data, point, current.shift);

            EditorUtility.SetDirty(data);
            current.Use();
        }

        static bool Key(KeyCode key)
        {
            if (key == KeyCode.LeftBracket || key == KeyCode.RightBracket)
            {
                s_Size = Mathf.Clamp(key == KeyCode.LeftBracket ? s_Size / SizeStep : s_Size * SizeStep, SizeLeast,
                                     SizeMost);
                return true;
            }

            if (key == KeyCode.Minus || key == KeyCode.Equals)
            {
                var sign = key == KeyCode.Minus ? -1f : 1f;

                if (Rated())
                    s_Rate = Mathf.Clamp(s_Rate + sign * FlowStep, s_Tool == Ramp ? 0f : FlowStep, FlowMost);
                else
                    s_Force = Mathf.Clamp(s_Force + sign * ForceStep, 0.01f, 1f);

                return true;
            }

            if (key == KeyCode.Backspace && s_Path.Count > 0)
            {
                s_Path.RemoveAt(s_Path.Count - 1);
                return true;
            }

            if (key != KeyCode.Escape)
                return false;

            if (s_Path.Count > 0)
                s_Path.Clear();
            else
                s_Tool = Off;

            return true;
        }

        static Vector4 Pin(HBStyleLiquidTerrain terrain, Vector3 point, bool keep)
        {
            var height = keep ? terrain.LevelAt(new Vector2(point.x, point.z)) : point.y + s_Above;

            return new Vector4(point.x, height, point.z, s_Size * 0.5f);
        }

        static void Draw(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point)
        {
            var here = new Vector2(point.x, point.z);
            var surface = new Vector3(point.x, terrain.LevelAt(here), point.z);
            var radius = s_Size * 0.5f;
            var brush = new Color(0.3f, 0.7f, 0.95f, 0.9f);

            if (Streamed())
                Streams(terrain, data, point);

            if (s_Marks && s_Tool == Paint && s_Layer >= Mathf.Min(terrain.layers.Count, StyleLiquidLayers.Most))
                Holes(terrain, data, point);

            Handles.color = brush;

            if (s_Tool == Lake)
            {
                Vector3 seed = s_Path.Count > 0 ? (Vector3)s_Path[0] : point + Vector3.up * s_Above;

                if (s_Form == 0)
                    Handles.DrawWireDisc(point, Vector3.up, 1f);
                else
                    Outline(s_Path.Count > 0 ? s_Path[0] : Pin(terrain, point, false));

                Handles.Label(seed + Vector3.up, seed.y.ToString("0.0") + " m");
                return;
            }

            if (s_Tool == Ramp)
            {
                Vector3 next = Pin(terrain, point, Event.current.shift);

                for (var index = 0; index < s_Path.Count; index++)
                {
                    Vector3 pin = s_Path[index];

                    Handles.DrawWireDisc(pin, Vector3.up, s_Path[index].w);
                    Handles.Label(pin + Vector3.up, pin.y.ToString("0.0") + " m");

                    if (index > 0)
                        Handles.DrawLine((Vector3)s_Path[index - 1], pin);
                }

                if (s_Path.Count > 0)
                    Handles.DrawDottedLine((Vector3)s_Path[s_Path.Count - 1], next, 4f);

                Handles.DrawWireDisc(next, Vector3.up, radius);
                Handles.Label(next + Vector3.up, next.y.ToString("0.0") + " m");
                return;
            }

            Handles.DrawWireDisc(point, Vector3.up, radius);
            Handles.DrawWireDisc(surface, Vector3.up, radius);

            Handles.color = new Color(brush.r, brush.g, brush.b, 0.35f);
            Handles.DrawWireDisc(surface, Vector3.up, radius * 0.5f);

            if (Streamed())
                Handles.Label(surface + Vector3.up, terrain.StreamAt(here).magnitude.ToString("0.0") + " m/s");
            else if (s_Tool == Hush)
                Handles.Label(surface + Vector3.up,
                              Mathf.RoundToInt((1f - terrain.StillAt(here)) * 100f) + "% waves");
            else if (s_Tool != Paint)
                Handles.Label(surface + Vector3.up, surface.y.ToString("0.0") + " m");
        }

        static Vector3 Cell(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point, float step,
                            int column, int row, out int index)
        {
            var size = terrain.size;
            var centre = terrain.transform.position;
            var resolution = data.resolution;
            var reach = step * ShowCount * 0.5f;
            var place = new Vector2(Mathf.Round((point.x - reach) / step + column) * step,
                                    Mathf.Round((point.z - reach) / step + row) * step);
            var x = Mathf.FloorToInt(((place.x - centre.x) / size.x + 0.5f) * resolution);
            var y = Mathf.FloorToInt(((place.y - centre.z) / size.y + 0.5f) * resolution);

            index = x < 0 || y < 0 || x >= resolution || y >= resolution ? -1 : y * resolution + x;

            return new Vector3(place.x, terrain.LevelAt(place) + ShowLift, place.y);
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

        static void Streams(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point)
        {
            var map = data.flow;
            var step = Mathf.Max(s_Size, ShowReach) * 2f / ShowCount;

            for (var row = 0; row <= ShowCount; row++)
            {
                for (var column = 0; column <= ShowCount; column++)
                {
                    var middle = Cell(terrain, data, point, step, column, row, out var index);

                    if (index < 0)
                        continue;

                    var run = new Vector2(map[index * 2], map[index * 2 + 1]);
                    var share = Mathf.Clamp01(run.magnitude / FlowMost);
                    var colour = share < 0.5f ? Color.Lerp(Color.red, Color.yellow, share * 2f)
                                              : Color.Lerp(Color.yellow, Color.green, share * 2f - 1f);

                    if (!s_Marks && run.sqrMagnitude < FlowLeast * FlowLeast)
                        continue;

                    Handles.color = Color.white;
                    Glow(middle, step, colour);
                    Handles.color = colour;

                    if (run.sqrMagnitude < FlowLeast * FlowLeast)
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

        static void Holes(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point)
        {
            var map = data.weights;
            var step = Mathf.Max(s_Size, ShowReach) * 2f / ShowCount;

            Handles.color = Color.white;

            for (var row = 0; row <= ShowCount; row++)
            {
                for (var column = 0; column <= ShowCount; column++)
                {
                    var middle = Cell(terrain, data, point, step, column, row, out var index);

                    if (index >= 0 && map[index * 4 + 3] > 127)
                        Glow(middle, step, Color.red);
                }
            }
        }

        static Patch Cover(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point, float reach)
        {
            var size = terrain.size;
            var centre = terrain.transform.position;
            var patch = new Patch
            {
                resolution = data.resolution,
                radius = s_Size * 0.5f
            };

            patch.texel = new Vector2(size.x / patch.resolution, size.y / patch.resolution);
            patch.at = new Vector2(((point.x - centre.x) / size.x + 0.5f) * patch.resolution - 0.5f,
                                   ((point.z - centre.z) / size.y + 0.5f) * patch.resolution - 0.5f);
            patch.left = Mathf.Max(Mathf.FloorToInt(patch.at.x - reach / patch.texel.x), 0);
            patch.right = Mathf.Min(Mathf.CeilToInt(patch.at.x + reach / patch.texel.x), patch.resolution - 1);
            patch.bottom = Mathf.Max(Mathf.FloorToInt(patch.at.y - reach / patch.texel.y), 0);
            patch.top = Mathf.Min(Mathf.CeilToInt(patch.at.y + reach / patch.texel.y), patch.resolution - 1);

            return patch;
        }

        static void Keep(float[] map, Patch patch, int width)
        {
            if (s_Before == null || s_Before.Length != map.Length)
                s_Before = new float[map.Length];

            var first = Mathf.Max(patch.left - 1, 0);
            var last = Mathf.Min(patch.right + 1, patch.resolution - 1);

            for (var y = Mathf.Max(patch.bottom - 1, 0); y <= Mathf.Min(patch.top + 1, patch.resolution - 1); y++)
            {
                var from = (y * patch.resolution + first) * width;

                System.Array.Copy(map, from, s_Before, from, (last - first + 1) * width);
            }
        }

        static float Around(Patch patch, int x, int y, int width, int channel)
        {
            var most = patch.resolution - 1;
            var west = s_Before[(y * patch.resolution + Mathf.Max(x - 1, 0)) * width + channel];
            var east = s_Before[(y * patch.resolution + Mathf.Min(x + 1, most)) * width + channel];
            var south = s_Before[(Mathf.Max(y - 1, 0) * patch.resolution + x) * width + channel];
            var north = s_Before[(Mathf.Min(y + 1, most) * patch.resolution + x) * width + channel];

            return (west + east + south + north) * 0.25f;
        }

        static float Ground(Terrain[] terrains, float x, float z)
        {
            var ground = float.NegativeInfinity;

            foreach (var terrain in terrains)
            {
                var shape = terrain.terrainData;

                if (shape == null)
                    continue;

                var origin = terrain.transform.position;

                if (x < origin.x || z < origin.z || x > origin.x + shape.size.x || z > origin.z + shape.size.z)
                    continue;

                ground = Mathf.Max(ground, terrain.SampleHeight(new Vector3(x, 0f, z)) + origin.y);
            }

            if (!float.IsNegativeInfinity(ground))
                return ground;

            return Physics.Raycast(new Vector3(x, Farthest * 0.5f, z), Vector3.down, out var hit, Farthest)
                ? hit.point.y
                : ground;
        }

        static void Flood(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector4 seed)
        {
            var size = terrain.size;
            var centre = terrain.transform.position;
            var resolution = data.resolution;
            var map = data.levels;
            var terrains = Terrain.activeTerrains;
            var filled = s_Filled;
            var column = Mathf.FloorToInt(((seed.x - centre.x) / size.x + 0.5f) * resolution);
            var row = Mathf.FloorToInt(((seed.z - centre.z) / size.y + 0.5f) * resolution);

            if (column < 0 || row < 0 || column >= resolution || row >= resolution)
                return;

            if (s_State == null || s_State.Length != map.Length)
                s_State = new byte[map.Length];
            else
                System.Array.Clear(s_State, 0, s_State.Length);

            if (s_Soil == null || s_Soil.Length != map.Length)
            {
                s_Soil = new float[map.Length];

                for (var index = 0; index < s_Soil.Length; index++)
                    s_Soil[index] = float.NaN;
            }

            var state = s_State;

            filled.Clear();

            bool Open(int x, int y)
            {
                if (x < 0 || y < 0 || x >= resolution || y >= resolution)
                    return false;

                var index = y * resolution + x;

                if (state[index] == Unknown)
                {
                    if (float.IsNaN(s_Soil[index]))
                    {
                        s_Soil[index] = Ground(terrains, centre.x + ((x + 0.5f) / resolution - 0.5f) * size.x,
                                               centre.z + ((y + 0.5f) / resolution - 0.5f) * size.y);
                    }

                    state[index] = s_Soil[index] < seed.y ? Hollow : Solid;
                }

                if (state[index] != Hollow)
                    return false;

                state[index] = Flooded;
                filled.Add(index);

                return true;
            }

            if (!Open(column, row))
                return;

            for (var next = 0; next < filled.Count; next++)
            {
                var x = filled[next] % resolution;
                var y = filled[next] / resolution;

                Open(x - 1, y);
                Open(x + 1, y);
                Open(x, y - 1);
                Open(x, y + 1);
            }

            var first = 0;

            for (var pass = 0; pass < FillFringe; pass++)
            {
                var last = filled.Count;

                for (var next = first; next < last; next++)
                {
                    var x = filled[next] % resolution;
                    var y = filled[next] / resolution;

                    for (var side = 0; side < 9; side++)
                    {
                        var besideX = x + side % 3 - 1;
                        var besideY = y + side / 3 - 1;

                        if (besideX < 0 || besideY < 0 || besideX >= resolution || besideY >= resolution)
                            continue;

                        var beside = besideY * resolution + besideX;

                        if (state[beside] == Flooded)
                            continue;

                        state[beside] = Flooded;
                        filled.Add(beside);
                    }
                }

                first = last;
            }

            var wanted = seed.y - terrain.level;

            foreach (var index in filled)
                map[index] = wanted;
        }

        static bool Aim(HBStyleLiquidTerrain terrain, Ray ray, out Vector3 point)
        {
            if (Physics.Raycast(ray, out var hit, Farthest))
            {
                point = hit.point;
                return true;
            }

            point = Vector3.zero;

            if (!new Plane(Vector3.up, new Vector3(0f, terrain.level, 0f)).Raycast(ray, out var along))
                return false;

            point = ray.GetPoint(along);

            return true;
        }

        static void Shape(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, bool course)
        {
            var size = terrain.size;
            var centre = terrain.transform.position;
            var resolution = data.resolution;
            var map = data.levels;
            var run = data.flow;
            var low = new Vector2(float.MaxValue, float.MaxValue);
            var high = new Vector2(float.MinValue, float.MinValue);

            foreach (var pin in s_Path)
            {
                low = Vector2.Min(low, new Vector2(pin.x - pin.w, pin.z - pin.w));
                high = Vector2.Max(high, new Vector2(pin.x + pin.w, pin.z + pin.w));
            }

            var left = Mathf.Max(Mathf.FloorToInt(((low.x - centre.x) / size.x + 0.5f) * resolution), 0);
            var right = Mathf.Min(Mathf.CeilToInt(((high.x - centre.x) / size.x + 0.5f) * resolution), resolution - 1);
            var bottom = Mathf.Max(Mathf.FloorToInt(((low.y - centre.z) / size.y + 0.5f) * resolution), 0);
            var top = Mathf.Min(Mathf.CeilToInt(((high.y - centre.z) / size.y + 0.5f) * resolution), resolution - 1);

            for (var y = bottom; y <= top; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    var place = new Vector2(centre.x + ((x + 0.5f) / resolution - 0.5f) * size.x,
                                            centre.z + ((y + 0.5f) / resolution - 0.5f) * size.y);
                    var nearest = 1f;
                    var height = 0f;
                    var heading = Vector2.zero;

                    for (var index = 0; index + 1 < s_Path.Count; index++)
                    {
                        var from = s_Path[index];
                        var to = s_Path[index + 1];
                        var start = new Vector2(from.x, from.z);
                        var span = new Vector2(to.x, to.z) - start;
                        var along = Mathf.Clamp01(Vector2.Dot(place - start, span)
                                                  / Mathf.Max(span.sqrMagnitude, 1e-4f));
                        var apart = (place - start - span * along).magnitude
                                  / Mathf.Max(Mathf.Lerp(from.w, to.w, along), 0.1f);

                        if (apart >= nearest)
                            continue;

                        nearest = apart;
                        height = Mathf.Lerp(from.y, to.y, along);
                        heading = (from.y >= to.y ? span : -span).normalized;
                    }

                    var weight = Mathf.Clamp01((1f - nearest) / (1f - RampCore));

                    if (weight <= 0f)
                        continue;

                    var blend = weight * weight * (3f - 2f * weight);

                    map[y * resolution + x] = Mathf.Lerp(map[y * resolution + x], height - terrain.level, blend);

                    if (!course || s_Rate <= 0f)
                        continue;

                    var first = (y * resolution + x) * 2;

                    run[first] = Mathf.Lerp(run[first], heading.x * s_Rate, blend);
                    run[first + 1] = Mathf.Lerp(run[first + 1], heading.y * s_Rate, blend);
                }
            }
        }

        static void Outline(Vector4 pin)
        {
            var middle = new Vector3(pin.x, pin.y, pin.z);

            if (s_Form == 1)
            {
                Handles.DrawWireDisc(middle, Vector3.up, pin.w);
                return;
            }

            Handles.DrawWireCube(middle, new Vector3(pin.w * 2f, 0f, pin.w * 2f));
        }

        static void Pool(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector4 pin)
        {
            var size = terrain.size;
            var centre = terrain.transform.position;
            var resolution = data.resolution;
            var map = data.levels;
            var wide = Mathf.Max(pin.w, 0.5f);
            var left = Mathf.Max(Mathf.FloorToInt(((pin.x - wide - centre.x) / size.x + 0.5f) * resolution), 0);
            var right = Mathf.Min(Mathf.CeilToInt(((pin.x + wide - centre.x) / size.x + 0.5f) * resolution),
                                  resolution - 1);
            var bottom = Mathf.Max(Mathf.FloorToInt(((pin.z - wide - centre.z) / size.y + 0.5f) * resolution), 0);
            var top = Mathf.Min(Mathf.CeilToInt(((pin.z + wide - centre.z) / size.y + 0.5f) * resolution),
                                resolution - 1);

            for (var y = bottom; y <= top; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    var offset = new Vector2(centre.x + ((x + 0.5f) / resolution - 0.5f) * size.x - pin.x,
                                             centre.z + ((y + 0.5f) / resolution - 0.5f) * size.y - pin.z);
                    var apart = s_Form == 1 ? offset.magnitude : Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                    var weight = Mathf.Clamp01((1f - apart / wide) / (1f - RampCore));

                    if (weight <= 0f)
                        continue;

                    map[y * resolution + x] = Mathf.Lerp(map[y * resolution + x], pin.y - terrain.level,
                                                         weight * weight * (3f - 2f * weight));
                }
            }
        }

        static void Build(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, bool laid)
        {
            if (s_Tool == Lake)
            {
                if (s_Form == 0)
                    Flood(terrain, data, s_Path[0]);
                else
                    Pool(terrain, data, s_Path[0]);
            }
            else if (s_Path.Count > 1)
                Shape(terrain, data, laid);
        }

        static void Preview(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data)
        {
            if (s_Path.Count == 0)
            {
                Cancel();
                return;
            }

            if (s_Previewed != data || s_Held == null || s_Held.Length != data.levels.Length)
            {
                Revert();
                s_Held = (float[])data.levels.Clone();
                s_Previewed = data;
                s_Soil = null;
            }

            System.Array.Copy(s_Held, data.levels, s_Held.Length);
            Build(terrain, data, false);

            data.UploadLevels();
            SceneView.RepaintAll();
        }

        static void Cancel()
        {
            s_Path.Clear();
            Revert();
        }

        static void Revert()
        {
            if (s_Previewed == null)
                return;

            if (s_Held != null && s_Held.Length == s_Previewed.levels.Length)
            {
                System.Array.Copy(s_Held, s_Previewed.levels, s_Held.Length);
                s_Previewed.Upload();
            }

            s_Previewed = null;
            SceneView.RepaintAll();
        }

        static void Lay(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data)
        {
            if (s_Path.Count < (s_Tool == Lake ? 1 : 2))
                return;

            if (s_Previewed == data && s_Held != null && s_Held.Length == data.levels.Length)
                System.Array.Copy(s_Held, data.levels, s_Held.Length);

            s_Previewed = null;

            Undo.RegisterCompleteObjectUndo(data, s_Tool == Lake ? "Lake" : "Ramp Liquid");
            Build(terrain, data, true);

            s_Path.Clear();
            data.Upload();
            EditorUtility.SetDirty(data);
            SceneView.RepaintAll();
        }

        static bool Pins()
        {
            var changed = false;

            for (var index = 0; index < s_Path.Count; index++)
            {
                var pin = s_Path[index];
                var place = new Vector3(pin.x, pin.y, pin.z);
                var size = HandleUtility.GetHandleSize(place);
                var wide = pin.w;

                EditorGUI.BeginChangeCheck();

                Handles.color = s_LevelTint;

                var moved = Handles.Slider2D(place, Vector3.up, Vector3.right, Vector3.forward, size * 0.12f,
                                             Handles.CircleHandleCap, Vector2.zero);

                Handles.color = Color.white;

                var lifted = Handles.Slider(new Vector3(moved.x, pin.y, moved.z), Vector3.up, size * 0.7f,
                                            Handles.ArrowHandleCap, 0f);

                if (s_Tool == Ramp || s_Form != 0)
                {
                    Handles.color = s_FlowTint;

                    var edge = Handles.Slider(new Vector3(moved.x + wide, lifted.y, moved.z), Vector3.right,
                                              size * 0.06f, Handles.DotHandleCap, 0f);

                    wide = Mathf.Max(edge.x - moved.x, 0.5f);
                }

                if (!EditorGUI.EndChangeCheck())
                    continue;

                s_Path[index] = new Vector4(moved.x, lifted.y, moved.z, wide);
                changed = true;
            }

            return changed;
        }

        static void Run(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point, bool still)
        {
            var heading = new Vector2(point.x - s_Last.x, point.z - s_Last.z);

            s_Last = point;

            if (!still && heading.sqrMagnitude < 1e-4f)
                return;

            var wanted = still ? Vector2.zero : heading.normalized * s_Rate;
            var map = data.flow;
            var patch = Cover(terrain, data, point, s_Size * 0.5f);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * FlowBlend;

                    if (weight <= 0f)
                        continue;

                    var first = (y * patch.resolution + x) * 2;

                    for (var axis = 0; axis < 2; axis++)
                    {
                        var value = Mathf.Lerp(map[first + axis], wanted[axis], weight);

                        map[first + axis] = Mathf.Abs(value) < FlowLeast ? 0f : value;
                    }
                }
            }

            data.Upload();
        }

        static void Lull(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point, bool lower)
        {
            var map = data.still;
            var patch = Cover(terrain, data, point, s_Size * 0.5f);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * s_Force;

                    if (weight <= 0f)
                        continue;

                    var index = y * patch.resolution + x;
                    var value = Mathf.Lerp(map[index] / 255f, lower ? 0f : 1f, weight);

                    map[index] = (byte)(lower ? Mathf.FloorToInt(value * 255f) : Mathf.CeilToInt(value * 255f));
                }
            }

            data.Upload();
        }

        static void Descend(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point)
        {
            var levels = data.levels;
            var map = data.flow;
            var patch = Cover(terrain, data, point, s_Size * 0.5f);
            var most = patch.resolution - 1;

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * FlowBlend;

                    if (weight <= 0f)
                        continue;

                    var row = y * patch.resolution;
                    var east = levels[row + Mathf.Min(x + 1, most)] - levels[row + Mathf.Max(x - 1, 0)];
                    var north = levels[Mathf.Min(y + 1, most) * patch.resolution + x]
                              - levels[Mathf.Max(y - 1, 0) * patch.resolution + x];
                    var fall = new Vector2(-east / (patch.texel.x * 2f), -north / (patch.texel.y * 2f));
                    var steep = fall.magnitude;
                    var wanted = steep > 1e-5f
                        ? fall / steep * (s_Rate * Mathf.Clamp01(steep / FallFull))
                        : Vector2.zero;
                    var first = (row + x) * 2;

                    for (var axis = 0; axis < 2; axis++)
                    {
                        var value = Mathf.Lerp(map[first + axis], wanted[axis], weight);

                        map[first + axis] = Mathf.Abs(value) < FlowLeast ? 0f : value;
                    }
                }
            }

            data.Upload();
        }

        static void Swirl(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point, bool reverse)
        {
            var levels = data.levels;
            var map = data.flow;
            var patch = Cover(terrain, data, point, s_Size * 0.5f);
            var turn = reverse ? -1f : 1f;

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var offset = new Vector2((x - patch.at.x) * patch.texel.x, (y - patch.at.y) * patch.texel.y);
                    var away = offset.magnitude;
                    var share = away / patch.radius;

                    if (share >= 1f)
                        continue;

                    var index = y * patch.resolution + x;
                    var wanted = Vector2.zero;

                    if (away > 1e-4f)
                    {
                        wanted = (new Vector2(-offset.y, offset.x) * turn - offset * WhirlPull)
                               * (s_Rate * 4f * share * (1f - share) / away);
                    }

                    for (var axis = 0; axis < 2; axis++)
                    {
                        var value = Mathf.Lerp(map[index * 2 + axis], wanted[axis], 1f - share * share);

                        map[index * 2 + axis] = Mathf.Abs(value) < FlowLeast ? 0f : value;
                    }

                    levels[index] -= s_Force * patch.radius * WhirlDip * (1f - share) * (1f - share);
                }
            }

            data.Upload();
        }

        static void Settle(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point)
        {
            var map = data.flow;
            var patch = Cover(terrain, data, point, s_Size * 0.5f);

            if (patch.empty)
                return;

            Keep(map, patch, 2);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * s_Force;

                    if (weight <= 0f)
                        continue;

                    var first = (y * patch.resolution + x) * 2;

                    for (var axis = 0; axis < 2; axis++)
                    {
                        var value = Mathf.Lerp(map[first + axis], Around(patch, x, y, 2, axis), weight);

                        map[first + axis] = Mathf.Abs(value) < FlowLeast ? 0f : value;
                    }
                }
            }

            data.Upload();
        }

        static void Daub(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point)
        {
            var map = data.weights;
            var hole = s_Layer >= Mathf.Min(terrain.layers.Count, StyleLiquidLayers.Most);
            var target = hole ? 3 : s_Layer - 1;
            var patch = Cover(terrain, data, point, s_Size * 0.5f);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * s_Force;

                    if (weight <= 0f)
                        continue;

                    var first = (y * patch.resolution + x) * 4;

                    for (var channel = hole ? 3 : 0; channel < 4; channel++)
                    {
                        var value = map[first + channel] / 255f;

                        value = channel == target ? Mathf.Lerp(value, 1f, weight) : value * (1f - weight);
                        map[first + channel] = (byte)(channel == target ? Mathf.CeilToInt(value * 255f)
                                                                         : Mathf.FloorToInt(value * 255f));
                    }
                }
            }

            data.Upload();
        }

        static void Stroke(HBStyleLiquidTerrain terrain, HBStyleLiquidTerrainData data, Vector3 point, bool lower)
        {
            var map = data.levels;
            var patch = Cover(terrain, data, point, s_Size * 0.5f);

            if (patch.empty)
                return;

            if (s_Tool == Smooth)
                Keep(map, patch, 1);

            var wanted = s_Target - terrain.level;

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * s_Force;

                    if (weight <= 0f)
                        continue;

                    var index = y * patch.resolution + x;

                    if (s_Tool == Raise)
                        map[index] += (lower ? -RaiseStep : RaiseStep) * weight;
                    else if (s_Tool == SetHeight)
                        map[index] = Mathf.Lerp(map[index], wanted, weight);
                    else
                        map[index] = Mathf.Lerp(map[index], Around(patch, x, y, 1, 0), weight);
                }
            }

            data.Upload();
        }
    }
}
