using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleBrushPage : StyleStudioPage
    {
        static readonly int[] s_Resolutions = { 256, 512, 1024 };
        const string RenameControl = "TextureStudio.Brush.Rename";
        const int MostLayers = 8;

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Brush",
                "The painted stroke atlas the HB shaders read for brush shading.");

            internal static readonly GUIContent Style = new GUIContent("Style",
                "Painting recipe of the picked layer. Picking one sets every value below to that recipe.");

            internal static readonly GUIContent[] StyleNames =
            {
                new GUIContent("Oil", "Long, tapered strokes with visible bristles and rich pigment."),
                new GUIContent("Gouache", "Broad, flat, opaque strokes with clean edges."),
                new GUIContent("Dry Brush", "Scratchy strokes that skip and break into bristle lines."),
                new GUIContent("Hatching", "Many short, thin, parallel lines."),
                new GUIContent("Watercolour", "Few wide, soft washes with ragged edges.")
            };

            internal static readonly GUIContent Strokes = new GUIContent("Strokes");
            internal static readonly GUIContent Edge = new GUIContent("Edge");
            internal static readonly GUIContent Bristles = new GUIContent("Bristles");
            internal static readonly GUIContent Paint = new GUIContent("Paint");
            internal static readonly GUIContent Finish = new GUIContent("Finish",
                "Applied once to the whole atlas, after every layer is painted.");

            internal static readonly GUIContent Count = new GUIContent("Count",
                "Strokes painted across the texture. More strokes cover it more fully.");
            internal static readonly GUIContent Length = new GUIContent("Length",
                "Shortest and longest stroke, as a share of the texture's width.");
            internal static readonly GUIContent Width = new GUIContent("Width",
                "Thinnest and thickest stroke, as a share of the texture's width.");
            internal static readonly GUIContent Angle = new GUIContent("Angle",
                "Main direction of the strokes in degrees. 0 runs left to right.");
            internal static readonly GUIContent AngleJitter = new GUIContent("Angle Jitter",
                "How far each stroke turns away from the main direction, in degrees.");
            internal static readonly GUIContent Curvature = new GUIContent("Curvature",
                "How much the strokes bend. 0 keeps them straight.");

            internal static readonly GUIContent Taper = new GUIContent("Taper",
                "How thin the stroke ends get. 0 keeps the full width to the ends.");
            internal static readonly GUIContent Softness = new GUIContent("Softness",
                "Width of the soft fade at the stroke's sides. 0 gives a hard edge.");
            internal static readonly GUIContent EdgeBreakup = new GUIContent("Breakup",
                "How much the stroke's width wanders along its length.");
            internal static readonly GUIContent EdgeBreakupScale = new GUIContent("Breakup Scale",
                "How often the width wanders along one stroke.");

            internal static readonly GUIContent BristleAmount = new GUIContent("Amount",
                "How strongly the bristle lines show inside each stroke.");
            internal static readonly GUIContent BristleBreakup = new GUIContent("Breakup",
                "How much the bristle lines skip along the stroke, like a dry brush.");
            internal static readonly GUIContent BristleDensity = new GUIContent("Density",
                "Bristle lines across one stroke.");

            internal static readonly GUIContent OpacityVariation = new GUIContent("Opacity Variation",
                "How much the strokes differ in opacity. 0 paints every stroke fully.");
            internal static readonly GUIContent ToneVariation = new GUIContent("Tone Variation",
                "How much the strokes differ from each other in lightness.");
            internal static readonly GUIContent PigmentAmount = new GUIContent("Pigment",
                "Blotchy pigment breakup inside the strokes.");
            internal static readonly GUIContent PigmentScale = new GUIContent("Pigment Scale",
                "Pigment blotches across the texture. More are smaller.");
            internal static readonly GUIContent Seed = new GUIContent("Seed",
                "Picks another random layout of the same strokes.");

            internal static readonly GUIContent Strength = new GUIContent("Strength",
                "How much the picked layer covers the layers under it.");

            internal static readonly GUIContent Contrast = new GUIContent("Contrast",
                "Steepens the light and dark of the strokes. 1 leaves them as painted.");
            internal static readonly GUIContent CanvasAmount = new GUIContent("Canvas",
                "Grain of the canvas showing through the paint.");
            internal static readonly GUIContent CanvasScale = new GUIContent("Canvas Scale",
                "Canvas grain cells across the texture. More are finer.");
            internal static readonly GUIContent CanvasSeed = new GUIContent("Canvas Seed",
                "Picks another random canvas grain.");

            internal static readonly GUIContent Layers = new GUIContent("Layers",
                "Stroke layers, top first. The bottom one is painted first, so the top one covers the others.");
            internal static readonly GUIContent AddLayer = new GUIContent("+",
                "Adds a layer of the picked style above the picked layer.");
            internal static readonly GUIContent DuplicateLayer = new GUIContent("Duplicate",
                "Copies the picked layer above itself with another seed.");
            internal static readonly GUIContent RemoveLayer = new GUIContent("−", "Removes the picked layer.");
            internal static readonly GUIContent RaiseLayer = new GUIContent("▲", "Moves the picked layer up.");
            internal static readonly GUIContent LowerLayer = new GUIContent("▼", "Moves the picked layer down.");
            internal static readonly GUIContent Visible = EditorGUIUtility.TrIconContent("animationvisibilitytoggleon",
                "Shown. Click to hide the layer.");
            internal static readonly GUIContent Hidden = EditorGUIUtility.TrIconContent("animationvisibilitytoggleoff",
                "Hidden. Click to show the layer.");

            internal static readonly GUIContent Output = new GUIContent("Output");
            internal static readonly GUIContent Export = new GUIContent("Export",
                "Name, size and folder of the saved atlas, and the Save button.");
            internal static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of the saved atlas.");
            internal static readonly GUIContent[] Resolutions =
            {
                new GUIContent("256"), new GUIContent("512"), new GUIContent("1024")
            };
            internal static readonly GUIContent Name = new GUIContent("Name",
                "File name, saved as TEX_Words_Like_This. Put the saved texture in the Environment profile's " +
                "Brush › Atlas slot.");
            internal static readonly GUIContent Save = new GUIContent("Save Atlas",
                "Writes the brush atlas PNG into the folder as linear data. A file with the same name is replaced.");

            internal static readonly GUIContent[] MapKeys =
            {
                new GUIContent("D", "Shows the stroke direction channels flat."),
                new GUIContent("C", "Shows the stroke coverage channel flat.")
            };

            internal static readonly GUIContent[] MapNames =
            {
                new GUIContent("Direction"), new GUIContent("Coverage")
            };

            internal static readonly Color selection = new Color(0.24f, 0.48f, 0.9f, 0.35f);
            internal static readonly Color badge = new Color(0f, 0f, 0f, 0.55f);
            internal static readonly GUIContent PreviewColour = new GUIContent("Colour",
                "Colour of the sphere and the plane in the preview. Only the preview uses it; the saved atlas " +
                "carries no colour.");
        }

        [SerializeField]
        StyleBrushStack m_Stack = new StyleBrushStack();

        [SerializeField]
        int m_Selected;

        [SerializeField]
        StyleBrushMap m_Map;

        [NonSerialized]
        int m_Renaming = -1;

        [NonSerialized]
        bool m_Focus;

        [NonSerialized]
        StyleBrushGenerator m_Generator;

        [NonSerialized]
        StyleStudioPreview m_Preview;

        [NonSerialized]
        bool m_Dirty = true;

        [NonSerialized]
        StyleStudioUndoState m_Undo;

        internal override GUIContent title => Styles.Title;

        StyleBrushGenerator generator => m_Generator ??= new StyleBrushGenerator();
        StyleStudioPreview preview => m_Preview ??= new StyleStudioPreview();

        StyleStudioUndoState undo =>
            m_Undo != null ? m_Undo : m_Undo = StyleStudioUndoState.Create(m_Stack, Restored);

        StyleBrushLayer current
        {
            get
            {
                if (m_Stack.layers.Count == 0)
                    m_Stack.layers.Add(StyleBrushLayer.Create(StyleBrushStyle.Oil));

                m_Selected = Mathf.Clamp(m_Selected, 0, m_Stack.layers.Count - 1);
                return m_Stack.layers[m_Selected];
            }
        }

        internal override void DrawSettings()
        {
            var history = undo;
            var layer = current;

            EditorGUI.BeginChangeCheck();

            var style = (StyleBrushStyle)EditorGUILayout.Popup(Styles.Style, (int)layer.style, Styles.StyleNames);

            if (style != layer.style)
            {
                var named = layer.label == StyleBrushLayer.LabelOf(layer.style);

                layer.Apply(style);

                if (named)
                    layer.label = StyleBrushLayer.LabelOf(style);
            }

            EditorGUILayout.Space();

            var open = StyleSectionHeader.Draw(Styles.Strokes, "TextureStudio.Brush.Strokes",
                                               () => Reset(layer.ResetStrokes));
            if (open)
            {
                layer.strokeCount = EditorGUILayout.IntSlider(Styles.Count, layer.strokeCount, 1,
                                                              StyleBrushLayer.MostStrokes);
                layer.lengthRange = Range(Styles.Length, layer.lengthRange, 0.01f, 0.6f);
                layer.widthRange = Range(Styles.Width, layer.widthRange, 0.001f, 0.15f);
                layer.angle = EditorGUILayout.Slider(Styles.Angle, layer.angle, -90f, 90f);
                layer.angleJitter = EditorGUILayout.Slider(Styles.AngleJitter, layer.angleJitter, 0f, 90f);
                layer.curvature = EditorGUILayout.Slider(Styles.Curvature, layer.curvature, 0f, 1f);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Edge, "TextureStudio.Brush.Edge", () => Reset(layer.ResetEdge));

            if (open)
            {
                layer.taper = EditorGUILayout.Slider(Styles.Taper, layer.taper, 0f, 1f);
                layer.edgeSoftness = EditorGUILayout.Slider(Styles.Softness, layer.edgeSoftness, 0f, 1f);
                layer.edgeBreakup = EditorGUILayout.Slider(Styles.EdgeBreakup, layer.edgeBreakup, 0f, 1f);

                using (new EditorGUI.DisabledScope(layer.edgeBreakup <= 0f))
                {
                    layer.edgeBreakupScale = EditorGUILayout.Slider(Styles.EdgeBreakupScale, layer.edgeBreakupScale,
                                                                    1f, 64f);
                }
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Bristles, "TextureStudio.Brush.Bristles",
                                           () => Reset(layer.ResetBristles));
            if (open)
            {
                layer.bristleAmount = EditorGUILayout.Slider(Styles.BristleAmount, layer.bristleAmount, 0f, 1f);

                using (new EditorGUI.DisabledScope(layer.bristleAmount <= 0f))
                {
                    layer.bristleBreakup = EditorGUILayout.Slider(Styles.BristleBreakup, layer.bristleBreakup, 0f,
                                                                  1f);
                    layer.bristleDensity = EditorGUILayout.Slider(Styles.BristleDensity, layer.bristleDensity, 1f,
                                                                  96f);
                }
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Paint, "TextureStudio.Brush.Paint", () => Reset(layer.ResetPaint));

            if (open)
            {
                layer.opacityVariation = EditorGUILayout.Slider(Styles.OpacityVariation, layer.opacityVariation, 0f,
                                                                1f);
                layer.toneVariation = EditorGUILayout.Slider(Styles.ToneVariation, layer.toneVariation, 0f, 1f);
                layer.pigmentAmount = EditorGUILayout.Slider(Styles.PigmentAmount, layer.pigmentAmount, 0f, 1f);

                using (new EditorGUI.DisabledScope(layer.pigmentAmount <= 0f))
                {
                    layer.pigmentScale = EditorGUILayout.IntSlider(Styles.PigmentScale, layer.pigmentScale, 1, 128);
                }

                layer.seed = EditorGUILayout.IntSlider(Styles.Seed, layer.seed, 0, 999);
            }

            StyleSectionHeader.End(open);
            CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio Brush");
        }

        static Vector2 Range(GUIContent label, Vector2 range, float least, float most)
        {
            var low = Mathf.Min(range.x, range.y);
            var high = Mathf.Max(range.x, range.y);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);

                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                low = EditorGUILayout.FloatField(low, GUILayout.Width(52f));
                EditorGUILayout.MinMaxSlider(ref low, ref high, least, most);
                high = EditorGUILayout.FloatField(high, GUILayout.Width(52f));
                EditorGUI.indentLevel = indent;
            }

            low = Mathf.Clamp(low, least, most);
            high = Mathf.Clamp(high, low, most);
            return new Vector2(low, high);
        }

        internal override void DrawLayers()
        {
            var history = undo;
            var stack = m_Stack;
            var layer = current;

            EditorGUILayout.LabelField(Styles.Layers, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 64f;
            layer.strength = EditorGUILayout.Slider(Styles.Strength, layer.strength, 0f, 1f);
            EditorGUIUtility.labelWidth = labelWidth;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (var i = stack.layers.Count - 1; i >= 0; i--)
                    DrawStackRow(i);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(stack.layers.Count >= MostLayers))
                {
                    var add = GUILayoutUtility.GetRect(Styles.AddLayer, EditorStyles.miniButtonLeft);

                    if (GUI.Button(add, Styles.AddLayer, EditorStyles.miniButtonLeft))
                        ShowAddMenu(add);

                    if (GUILayout.Button(Styles.DuplicateLayer, EditorStyles.miniButtonMid))
                    {
                        var copy = layer.Clone();

                        copy.seed = (layer.seed + 1) % 1000;
                        stack.layers.Insert(m_Selected + 1, copy);
                        m_Selected++;
                    }
                }

                using (new EditorGUI.DisabledScope(stack.layers.Count <= 1))
                {
                    if (GUILayout.Button(Styles.RemoveLayer, EditorStyles.miniButtonMid))
                    {
                        stack.layers.RemoveAt(m_Selected);
                        m_Selected = Mathf.Clamp(m_Selected - 1, 0, stack.layers.Count - 1);
                    }
                }

                using (new EditorGUI.DisabledScope(m_Selected >= stack.layers.Count - 1))
                {
                    if (GUILayout.Button(Styles.RaiseLayer, EditorStyles.miniButtonMid))
                        Swap(m_Selected, m_Selected + 1);
                }

                using (new EditorGUI.DisabledScope(m_Selected <= 0))
                {
                    if (GUILayout.Button(Styles.LowerLayer, EditorStyles.miniButtonRight))
                        Swap(m_Selected, m_Selected - 1);
                }
            }

            EditorGUILayout.Space();

            var open = StyleSectionHeader.Draw(Styles.Finish, "TextureStudio.Brush.Finish",
                                               () => Reset(stack.ResetFinish));
            if (open)
            {
                EditorGUIUtility.labelWidth = 86f;
                stack.contrast = EditorGUILayout.Slider(Styles.Contrast, stack.contrast, 0.25f, 4f);
                stack.canvasAmount = EditorGUILayout.Slider(Styles.CanvasAmount, stack.canvasAmount, 0f, 1f);

                using (new EditorGUI.DisabledScope(stack.canvasAmount <= 0f))
                {
                    stack.canvasScale = EditorGUILayout.IntSlider(Styles.CanvasScale, stack.canvasScale, 1, 512);
                    stack.canvasSeed = EditorGUILayout.IntSlider(Styles.CanvasSeed, stack.canvasSeed, 0, 999);
                }

                EditorGUIUtility.labelWidth = labelWidth;
            }

            StyleSectionHeader.End(open);

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio Brush Layers");
        }

        void ShowAddMenu(Rect button)
        {
            var menu = new GenericMenu();

            for (var i = 0; i < Styles.StyleNames.Length; i++)
            {
                var style = (StyleBrushStyle)i;

                menu.AddItem(Styles.StyleNames[i], false, () => AddLayer(style));
            }

            menu.DropDown(button);
        }

        void AddLayer(StyleBrushStyle style)
        {
            m_Selected = Mathf.Clamp(m_Selected + 1, 0, m_Stack.layers.Count);
            m_Stack.layers.Insert(m_Selected, StyleBrushLayer.Create(style));
            m_Dirty = true;
            undo.Commit("Add Texture Studio Brush Layer");
            host?.Repaint();
        }

        void Swap(int a, int b)
        {
            var layers = m_Stack.layers;

            (layers[a], layers[b]) = (layers[b], layers[a]);
            m_Selected = b;
        }

        void DrawStackRow(int index)
        {
            var layer = m_Stack.layers[index];
            var row = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            var eye = new Rect(row.x + 4f, row.y + 12f, 16f, 16f);
            var picture = new Rect(eye.xMax + 6f, row.y + 4f, 32f, 32f);
            var text = new Rect(picture.xMax + 8f, row.y + 4f, row.xMax - picture.xMax - 12f, 16f);
            var sub = new Rect(text.x, text.yMax, text.width, 16f);

            if (Event.current.type == EventType.Repaint)
            {
                if (index == m_Selected)
                    EditorGUI.DrawRect(row, Styles.selection);

                var thumbnail = generator.Thumbnail(index);

                GUI.DrawTexture(picture, thumbnail != null ? thumbnail : Texture2D.grayTexture,
                                ScaleMode.ScaleAndCrop);

                var faded = GUI.color;

                if (!layer.visible)
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);

                if (m_Renaming != index)
                    EditorStyles.boldLabel.Draw(text, layer.label, false, false, false, false);

                EditorStyles.miniLabel.Draw(sub, Subtitle(layer), false, false, false, false);
                GUI.color = faded;
            }

            layer.visible = GUI.Toggle(eye, layer.visible, layer.visible ? Styles.Visible : Styles.Hidden,
                                       GUIStyle.none);

            if (m_Renaming == index)
                DrawRename(layer, text);

            var mouse = Event.current.mousePosition;

            if (Event.current.type != EventType.MouseDown || !row.Contains(mouse) || eye.Contains(mouse) ||
                (m_Renaming == index && text.Contains(mouse)))
                return;

            m_Selected = index;
            m_Renaming = Event.current.clickCount == 2 ? index : -1;
            m_Focus = m_Renaming == index;
            GUI.FocusControl(null);
            Event.current.Use();
        }

        void DrawRename(StyleBrushLayer layer, Rect text)
        {
            GUI.SetNextControlName(RenameControl);
            EditorGUI.BeginChangeCheck();

            var label = EditorGUI.DelayedTextField(text, layer.label);

            if (EditorGUI.EndChangeCheck())
            {
                layer.label = label;
                m_Renaming = -1;
            }
            else if (GUI.GetNameOfFocusedControl() == RenameControl)
            {
                m_Focus = false;
            }
            else if (m_Focus)
            {
                EditorGUI.FocusTextInControl(RenameControl);
            }
            else if (Event.current.type == EventType.Repaint)
            {
                m_Renaming = -1;
            }
        }

        static string Subtitle(StyleBrushLayer layer)
        {
            var kind = StyleBrushLayer.LabelOf(layer.style);

            return layer.strength >= 1f ? kind : $"{kind} · {Mathf.RoundToInt(layer.strength * 100f)}%";
        }

        internal override void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var button = GUILayoutUtility.GetRect(Styles.Export, EditorStyles.toolbarDropDown,
                                                      GUILayout.Width(72f));

                if (GUI.Button(button, Styles.Export, EditorStyles.toolbarDropDown))
                    PopupWindow.Show(button, new ExportPopup(this));
            }

            GUILayout.EndArea();
        }

        void DrawOutput()
        {
            EditorGUI.BeginChangeCheck();

            var index = Array.IndexOf(s_Resolutions, m_Stack.resolution);

            if (index < 0)
                index = 1;

            index = EditorGUILayout.Popup(Styles.Resolution, index, Styles.Resolutions);
            m_Stack.resolution = s_Resolutions[index];

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            m_Stack.name = EditorGUILayout.TextField(Styles.Name, m_Stack.name);
            StyleStudioExport.DrawFolder();
            undo.Commit("Change Texture Studio Brush Output");
            host?.Repaint();
        }

        sealed class ExportPopup : PopupWindowContent
        {
            readonly StyleBrushPage m_Page;

            internal ExportPopup(StyleBrushPage page)
            {
                m_Page = page;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(320f, 112f);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUIUtility.labelWidth = 80f;
                EditorGUILayout.LabelField(Styles.Output, EditorStyles.boldLabel);
                m_Page.DrawOutput();

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_Page.m_Stack.name)))
                {
                    if (GUILayout.Button(Styles.Save))
                    {
                        editorWindow.Close();
                        m_Page.SaveAtlas();
                    }
                }
            }
        }

        void DrawMapPicker(Rect bar)
        {
            const float side = 20f;
            var count = Styles.MapKeys.Length;
            var x = bar.xMax;

            for (var i = 0; i < count; i++)
                x -= Mathf.Max(side, EditorStyles.miniButtonMid.CalcSize(Styles.MapKeys[i]).x);

            for (var i = 0; i < count; i++)
            {
                var width = Mathf.Max(side, EditorStyles.miniButtonMid.CalcSize(Styles.MapKeys[i]).x);
                var button = new Rect(x, bar.y, width, side);
                x += width;
                var picked = m_Map == (StyleBrushMap)i && preview.view == StyleStudioView.Flat;
                var style = i == 0 ? EditorStyles.miniButtonLeft : EditorStyles.miniButtonRight;

                if (GUI.Toggle(button, picked, Styles.MapKeys[i], style) && !picked)
                {
                    m_Map = (StyleBrushMap)i;
                    preview.view = StyleStudioView.Flat;
                }
            }
        }

        void DrawMapName(Rect rect)
        {
            var name = Styles.MapNames[(int)m_Map];
            var size = EditorStyles.boldLabel.CalcSize(name);
            var label = new Rect(rect.x + 6f, rect.y + 6f, size.x + 10f, size.y + 4f);

            EditorGUI.DrawRect(label, Styles.badge);
            GUI.Label(new Rect(label.x + 5f, label.y + 2f, size.x, size.y), name, EditorStyles.boldLabel);
        }

        void Restored()
        {
            m_Selected = Mathf.Clamp(m_Selected, 0, Mathf.Max(m_Stack.layers.Count - 1, 0));
            m_Dirty = true;
            host?.Repaint();
        }

        internal override void DrawPreview(Rect rect)
        {
            if (m_Dirty || !generator.ready)
            {
                _ = current;
                generator.Generate(m_Stack);
                m_Dirty = false;
            }

            var changed = GUI.changed;
            GUI.changed = false;
            DrawMapPicker(rect);

            if (preview.view != StyleStudioView.Flat)
                DrawPreviewColour(rect);

            rect = preview.DrawBar(rect);

            if (!generator.ready)
            {
                EditorGUI.HelpBox(rect, "The studio's compute shader was not found.", MessageType.Warning);
                return;
            }

            if (preview.view == StyleStudioView.Flat)
            {
                if (Event.current.type == EventType.Repaint)
                    generator.Show(m_Map);

                preview.DrawFlat(rect, generator.display);
                DrawMapName(rect);
            }
            else
            {
                preview.DrawBrush(rect, generator.atlas, m_Stack.previewColour);
            }

            if (GUI.changed)
                host?.Repaint();

            GUI.changed |= changed;
        }

        void DrawPreviewColour(Rect bar)
        {
            var room = (bar.width - Mathf.Min(bar.width, 240f)) * 0.5f - 12f;

            if (room < 80f)
                return;

            var field = new Rect(bar.x + 4f, bar.y + 1f, Mathf.Min(120f, room), 18f);
            var labelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = 44f;
            m_Stack.previewColour = EditorGUI.ColorField(field, Styles.PreviewColour, m_Stack.previewColour, true,
                                                         false, false);
            EditorGUIUtility.labelWidth = labelWidth;
            undo.Commit("Change Texture Studio Brush Preview Colour");
        }

        void Reset(Action reset)
        {
            reset();
            m_Dirty = true;
            undo.Commit("Reset Texture Studio Brush Section");
            host?.Repaint();
        }

        void SaveAtlas()
        {
            if (!generator.ready)
                return;

            var path = StyleStudioExport.Save(generator.atlas, m_Stack.name.Trim(), StyleExportKind.Data);

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            GUIUtility.ExitGUI();
        }

        internal override void Release()
        {
            m_Generator?.Release();
            m_Preview?.Release();
            m_Generator = null;
            m_Preview = null;

            if (m_Undo != null)
                m_Undo.Release();

            m_Undo = null;
        }
    }
}
