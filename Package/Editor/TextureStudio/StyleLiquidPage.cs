using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleLiquidPage : StyleStudioPage
    {
        static readonly int[] s_Resolutions = { 256, 512, 1024 };
        const string RenameControl = "TextureStudio.Liquid.Rename";
        const string LayerPrefix = "LIQ";
        const string NormalMapField = "m_NormalMap";
        const int MostLayers = 8;
        const float BarHeight = 20f;
        const float BarWidth = 160f;

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Liquid",
                "Water, acid and lava: the ripple normal map and the Liquid Layer that uses it, made together.");

            internal static readonly GUIContent Recipe = new GUIContent("Recipe",
                "Kind of liquid. Picking one sets the ripple layers and every Look setting to that recipe.");

            internal static readonly GUIContent[] RecipeNames =
            {
                new GUIContent("Water", "Clear water with crossing waves and fine ripples."),
                new GUIContent("Acid", "Glowing, bubbling acid with swollen domes."),
                new GUIContent("Lava", "Glowing melt under a drifting crust of cooled plates.")
            };

            internal static readonly GUIContent[] Tabs =
            {
                new GUIContent("Ripples", "The ripple normal map, built from the layers on the right."),
                new GUIContent("Look", "Every setting of the Liquid Layer: waves, colour, foam, crust, underwater.")
            };

            internal static readonly GUIContent Shape = new GUIContent("Shape");
            internal static readonly GUIContent Detail = new GUIContent("Detail");

            internal static readonly GUIContent Kind = new GUIContent("Kind",
                "What the picked layer draws. Changing it resets the layer's settings.");

            internal static readonly GUIContent[] KindNames =
            {
                new GUIContent("Waves", "Crossing wave trains, like wind-driven water."),
                new GUIContent("Domes", "Swollen round domes over a slow swell, like boiling acid."),
                new GUIContent("Flow", "Swirling, stretched currents with cooled streaks, like lava."),
                new GUIContent("Cells", "A net of ridges between round cells, like foam or cracked crust."),
                new GUIContent("Noise", "Soft, fine, random ripples."),
                new GUIContent("Plates", "Raised, chiselled rock plates split by narrow cracks, like crusted lava. " +
                               "With Crust on the Liquid Layer the plates turn to lit rock and the cracks glow.")
            };

            internal static readonly GUIContent Strength = new GUIContent("Strength",
                "How strongly the picked layer bends the surface.");
            internal static readonly GUIContent Scale = new GUIContent("Scale",
                "How many times the pattern repeats across the texture. Higher is smaller and finer.");
            internal static readonly GUIContent Seed = new GUIContent("Seed",
                "Picks another random layout with the same settings.");

            internal static readonly GUIContent WaveCount = new GUIContent("Count",
                "How many wave trains cross each other. More gives a busier, less regular surface.");
            internal static readonly GUIContent Direction = new GUIContent("Direction",
                "Main direction the waves travel, in degrees.");
            internal static readonly GUIContent Spread = new GUIContent("Spread",
                "How far the wave trains turn away from the main direction. 1 sends them every way.");
            internal static readonly GUIContent Sharpness = new GUIContent("Sharpness",
                "How pinched the crests are. Low gives soft rolling swell, high narrow peaks.");
            internal static readonly GUIContent LengthVariation = new GUIContent("Length Variation",
                "How much the wave trains differ in length.");

            internal static readonly GUIContent DomeSize = new GUIContent("Size",
                "How much of each cell a dome fills.");
            internal static readonly GUIContent SmallDomes = new GUIContent("Small Domes",
                "Strength of a second set of smaller domes between the big ones.");
            internal static readonly GUIContent Swell = new GUIContent("Swell",
                "Strength of the slow swell under the domes.");

            internal static readonly GUIContent Turbulence = new GUIContent("Turbulence",
                "How far the current twists and stretches.");
            internal static readonly GUIContent Streaks = new GUIContent("Streaks",
                "Strength of the thin streaks along the current.");
            internal static readonly GUIContent Cooling = new GUIContent("Cooling",
                "Where the current turns into raised plates. Higher leaves fewer, smaller plates.");

            internal static readonly GUIContent EdgeWidth = new GUIContent("Edge Width",
                "Width of the ridges between the cells.");
            internal static readonly GUIContent EdgeSharpness = new GUIContent("Edge Sharpness",
                "How sharp the ridges are. Higher gives thin, crisp lines.");

            internal static readonly GUIContent Octaves = new GUIContent("Octaves",
                "Levels of finer detail added to the noise.");

            internal static readonly GUIContent Gap = new GUIContent("Gap",
                "Width of the cracks between the plates.");
            internal static readonly GUIContent GapVariation = new GUIContent("Gap Variation",
                "How much the cracks narrow and widen along their length.");
            internal static readonly GUIContent Facets = new GUIContent("Facets",
                "How sharply each plate is chiselled into flat faces. 0 leaves smooth, flat-topped plates.");
            internal static readonly GUIContent Grain = new GUIContent("Grain",
                "Fine roughness on the rock faces.");

            internal static readonly GUIContent Layers = new GUIContent("Ripple Layers",
                "Patterns added together into the normal map, top first.");
            internal static readonly GUIContent AddLayer = new GUIContent("+",
                "Adds a layer of the picked kind above the picked layer.");
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
                "Name, size and folder of the saved texture and layer, and the Save button.");
            internal static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of the saved normal map.");
            internal static readonly GUIContent[] Resolutions =
            {
                new GUIContent("256"), new GUIContent("512"), new GUIContent("1024")
            };
            internal static readonly GUIContent Name = new GUIContent("Name",
                "Saved as TEX_Name_Normal and LIQ_Name. Saving again with the same name updates the layer in " +
                "place, so everything using it keeps it.");
            internal static readonly GUIContent Save = new GUIContent("Save Texture + Layer",
                "Writes the normal map PNG and the Liquid Layer, with the normal map already in it.");

            internal static readonly GUIContent[] Views =
            {
                new GUIContent("Liquid", "The real HB liquid in a small pool. Drag to turn, scroll to zoom."),
                new GUIContent("Flat", "The picked map repeated 2 x 2, to check that the edges join.")
            };

            internal static readonly GUIContent[] MapKeys =
            {
                new GUIContent("N", "Shows the normal map flat."),
                new GUIContent("H", "Shows the height the normal map is made from.")
            };

            internal static readonly GUIContent[] MapNames = { new GUIContent("Normal"), new GUIContent("Height") };

            internal static readonly Color selection = new Color(0.24f, 0.48f, 0.9f, 0.35f);
            internal static readonly Color badge = new Color(0f, 0f, 0f, 0.55f);
        }

        [SerializeField]
        StyleRippleStack m_Stack = new StyleRippleStack();

        [SerializeField]
        string m_LookJson;

        [SerializeField]
        int m_Selected;

        [SerializeField]
        int m_Tab;

        [SerializeField]
        bool m_Flat;

        [SerializeField]
        StyleLiquidMap m_Map;

        [NonSerialized]
        HBStyleLiquidLayer m_Working;

        [NonSerialized]
        SerializedObject m_Look;

        [NonSerialized]
        StyleLiquidGenerator m_Generator;

        [NonSerialized]
        StyleStudioPreview m_Preview;

        [NonSerialized]
        StyleStudioPool m_Pool;

        [NonSerialized]
        StyleStudioUndoState m_Undo;

        [NonSerialized]
        bool m_Dirty = true;

        [NonSerialized]
        bool m_Listening;

        [NonSerialized]
        int m_Renaming = -1;

        [NonSerialized]
        bool m_Focus;

        internal override GUIContent title => Styles.Title;

        StyleLiquidGenerator generator => m_Generator ??= new StyleLiquidGenerator();
        StyleStudioPreview preview => m_Preview ??= new StyleStudioPreview();
        StyleStudioPool pool => m_Pool ??= new StyleStudioPool();

        StyleStudioUndoState undo =>
            m_Undo != null ? m_Undo : m_Undo = StyleStudioUndoState.Create(m_Stack, Restored);

        HBStyleLiquidLayer working
        {
            get
            {
                if (!m_Listening)
                {
                    Undo.undoRedoPerformed += LookRestored;
                    m_Listening = true;
                }

                if (m_Working != null)
                    return m_Working;

                m_Working = ScriptableObject.CreateInstance<HBStyleLiquidLayer>();
                m_Working.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                m_Working.name = "HB Texture Studio Liquid";
                m_Look = new SerializedObject(m_Working);

                if (!string.IsNullOrEmpty(m_LookJson))
                {
                    EditorJsonUtility.FromJsonOverwrite(m_LookJson, m_Working);
                    m_Look.Update();
                }
                else
                {
                    StyleLiquidRecipes.Apply(m_Look, m_Stack.recipe);
                    m_Look.ApplyModifiedPropertiesWithoutUndo();
                }

                AttachNormal();
                return m_Working;
            }
        }

        StyleRippleLayer current
        {
            get
            {
                if (m_Stack.layers.Count == 0)
                    m_Stack.layers.Add(StyleRippleLayer.Create(StyleRippleKind.Waves));

                m_Selected = Mathf.Clamp(m_Selected, 0, m_Stack.layers.Count - 1);
                return m_Stack.layers[m_Selected];
            }
        }

        internal override void DrawSettings()
        {
            var history = undo;
            var layer = working;

            EditorGUI.BeginChangeCheck();

            var recipe = (StyleLiquidRecipe)EditorGUILayout.Popup(Styles.Recipe, (int)m_Stack.recipe,
                                                                  Styles.RecipeNames);

            if (recipe != m_Stack.recipe)
                ApplyRecipe(recipe);

            EditorGUILayout.Space();
            m_Tab = GUILayout.Toolbar(m_Tab, Styles.Tabs);
            EditorGUILayout.Space();

            if (m_Tab == 1)
            {
                m_Look.Update();
                EditorGUI.BeginChangeCheck();
                StyleLiquidLayerEditor.DrawSections(m_Look, true);

                if (EditorGUI.EndChangeCheck())
                {
                    m_Look.ApplyModifiedProperties();
                    layer.Touch();
                    m_LookJson = EditorJsonUtility.ToJson(layer);
                }
            }
            else
            {
                DrawRipple(current);
            }

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio Liquid");
        }

        void DrawRipple(StyleRippleLayer layer)
        {
            var open = StyleSectionHeader.Draw(Styles.Shape, "TextureStudio.Liquid.Shape",
                                               () => Reset(layer.ResetShape));
            if (open)
            {
                var kind = (StyleRippleKind)EditorGUILayout.Popup(Styles.Kind, (int)layer.kind, Styles.KindNames);

                if (kind != layer.kind)
                {
                    var named = layer.label == StyleRippleLayer.LabelOf(layer.kind);

                    layer.kind = kind;
                    layer.ResetShape();

                    if (named)
                        layer.label = StyleRippleLayer.LabelOf(kind);
                }

                layer.strength = EditorGUILayout.Slider(Styles.Strength, layer.strength, 0f, 3f);
                layer.scale = EditorGUILayout.IntSlider(Styles.Scale, layer.scale, 1, 32);
                layer.seed = EditorGUILayout.IntSlider(Styles.Seed, layer.seed, 0, 999);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Detail, "TextureStudio.Liquid.Detail",
                                           () => Reset(layer.ResetShape));
            if (open)
            {
                switch (layer.kind)
                {
                    case StyleRippleKind.Domes:
                        layer.domeSize = EditorGUILayout.Slider(Styles.DomeSize, layer.domeSize, 0.1f, 1f);
                        layer.smallDomes = EditorGUILayout.Slider(Styles.SmallDomes, layer.smallDomes, 0f, 1f);
                        layer.swell = EditorGUILayout.Slider(Styles.Swell, layer.swell, 0f, 1f);
                        break;
                    case StyleRippleKind.Flow:
                        layer.turbulence = EditorGUILayout.Slider(Styles.Turbulence, layer.turbulence, 0f, 3f);
                        layer.streaks = EditorGUILayout.Slider(Styles.Streaks, layer.streaks, 0f, 1f);
                        layer.cooling = EditorGUILayout.Slider(Styles.Cooling, layer.cooling, 0f, 1f);
                        break;
                    case StyleRippleKind.Cells:
                        layer.edgeWidth = EditorGUILayout.Slider(Styles.EdgeWidth, layer.edgeWidth, 0.02f, 0.5f);
                        layer.edgeSharpness = EditorGUILayout.Slider(Styles.EdgeSharpness, layer.edgeSharpness,
                                                                     0.25f, 4f);
                        break;
                    case StyleRippleKind.Noise:
                        layer.octaves = EditorGUILayout.IntSlider(Styles.Octaves, layer.octaves, 1, 6);
                        break;
                    case StyleRippleKind.Plates:
                        layer.gap = EditorGUILayout.Slider(Styles.Gap, layer.gap, 0f, 1f);
                        layer.gapVariation = EditorGUILayout.Slider(Styles.GapVariation, layer.gapVariation, 0f, 1f);
                        layer.facets = EditorGUILayout.Slider(Styles.Facets, layer.facets, 0f, 1f);
                        layer.grain = EditorGUILayout.Slider(Styles.Grain, layer.grain, 0f, 1f);
                        break;
                    default:
                        layer.waveCount = EditorGUILayout.IntSlider(Styles.WaveCount, layer.waveCount, 1,
                                                                    StyleRippleLayer.MostWaves);
                        layer.direction = EditorGUILayout.Slider(Styles.Direction, layer.direction, -180f, 180f);
                        layer.spread = EditorGUILayout.Slider(Styles.Spread, layer.spread, 0f, 1f);
                        layer.sharpness = EditorGUILayout.Slider(Styles.Sharpness, layer.sharpness, 0.5f, 6f);
                        layer.lengthVariation = EditorGUILayout.Slider(Styles.LengthVariation, layer.lengthVariation,
                                                                       0f, 1f);
                        break;
                }
            }

            StyleSectionHeader.End(open);
            CoreEditorUtils.DrawSplitter();
        }

        void ApplyRecipe(StyleLiquidRecipe recipe)
        {
            var layer = working;

            if (m_Stack.name == StyleRippleStack.NameOf(m_Stack.recipe))
                m_Stack.name = StyleRippleStack.NameOf(recipe);

            m_Stack.recipe = recipe;
            m_Stack.layers = StyleRippleStack.Layers(recipe);
            m_Selected = 0;
            m_Look.Update();
            StyleLiquidRecipes.Apply(m_Look, recipe);
            m_Look.ApplyModifiedProperties();
            layer.Touch();
            m_LookJson = EditorJsonUtility.ToJson(layer);
            m_Dirty = true;
        }

        void AttachNormal()
        {
            if (m_Look == null)
                return;

            var texture = m_Generator != null ? m_Generator.previewNormal : null;
            var field = m_Look.FindProperty(NormalMapField);

            if (field.objectReferenceValue == texture)
                return;

            field.objectReferenceValue = texture;
            m_Look.ApplyModifiedPropertiesWithoutUndo();
            m_Working.Touch();
        }

        void LookRestored()
        {
            if (m_Working == null)
                return;

            m_Look?.Update();
            AttachNormal();
            m_Working.Touch();
            m_LookJson = EditorJsonUtility.ToJson(m_Working);
            host?.Repaint();
        }

        internal override void DrawLayers()
        {
            var history = undo;
            var stack = m_Stack;
            var layer = current;

            EditorGUILayout.LabelField(Styles.Layers, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

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

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio Liquid Layers");
        }

        void ShowAddMenu(Rect button)
        {
            var menu = new GenericMenu();

            for (var i = 0; i < Styles.KindNames.Length; i++)
            {
                var kind = (StyleRippleKind)i;

                menu.AddItem(Styles.KindNames[i], false, () => AddLayer(kind));
            }

            menu.DropDown(button);
        }

        void AddLayer(StyleRippleKind kind)
        {
            m_Selected = Mathf.Clamp(m_Selected + 1, 0, m_Stack.layers.Count);
            m_Stack.layers.Insert(m_Selected, StyleRippleLayer.Create(kind));
            m_Tab = 0;
            m_Dirty = true;
            undo.Commit("Add Texture Studio Ripple Layer");
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
                if (index == m_Selected && m_Tab == 0)
                    EditorGUI.DrawRect(row, Styles.selection);

                var thumbnail = generator.Thumbnail(index);

                GUI.DrawTexture(picture, thumbnail != null ? thumbnail : Texture2D.normalTexture,
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
            m_Tab = 0;
            m_Renaming = Event.current.clickCount == 2 ? index : -1;
            m_Focus = m_Renaming == index;
            GUI.FocusControl(null);
            Event.current.Use();
        }

        void DrawRename(StyleRippleLayer layer, Rect text)
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

        static string Subtitle(StyleRippleLayer layer)
        {
            var kind = StyleRippleLayer.LabelOf(layer.kind);

            return $"{kind} · Scale {layer.scale} · {layer.strength:0.##}";
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
            undo.Commit("Change Texture Studio Liquid Output");
            host?.Repaint();
        }

        sealed class ExportPopup : PopupWindowContent
        {
            readonly StyleLiquidPage m_Page;

            internal ExportPopup(StyleLiquidPage page)
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
                        m_Page.SaveLiquid();
                    }
                }
            }
        }

        Rect DrawViewBar(Rect rect)
        {
            var width = Mathf.Min(rect.width, BarWidth);
            var bar = new Rect(rect.center.x - width * 0.5f, rect.y, width, BarHeight);

            m_Flat = GUI.Toolbar(bar, m_Flat ? 1 : 0, Styles.Views, EditorStyles.miniButton) == 1;
            return Rect.MinMaxRect(rect.xMin, rect.yMin + BarHeight + 4f, rect.xMax, rect.yMax);
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
                var picked = m_Map == (StyleLiquidMap)i && m_Flat;
                var style = i == 0 ? EditorStyles.miniButtonLeft : EditorStyles.miniButtonRight;

                if (GUI.Toggle(button, picked, Styles.MapKeys[i], style) && !picked)
                {
                    m_Map = (StyleLiquidMap)i;
                    m_Flat = true;
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
            var layer = working;

            if (m_Dirty || !generator.ready)
            {
                _ = current;
                generator.Generate(m_Stack);
                AttachNormal();
                m_Dirty = false;
            }

            var changed = GUI.changed;
            GUI.changed = false;
            DrawMapPicker(rect);
            rect = DrawViewBar(rect);

            if (!generator.ready)
            {
                EditorGUI.HelpBox(rect, "The studio's compute shader was not found.", MessageType.Warning);
                return;
            }

            if (m_Flat)
            {
                if (Event.current.type == EventType.Repaint)
                    generator.Show(m_Map);

                preview.DrawFlat(rect, generator.display);
                DrawMapName(rect);
            }
            else
            {
                preview.DrawLiquid(rect, pool, layer);

                if (Event.current.type == EventType.Repaint)
                    host?.Repaint();
            }

            if (GUI.changed)
                host?.Repaint();

            GUI.changed |= changed;
        }

        void Reset(Action reset)
        {
            reset();
            m_Dirty = true;
            undo.Commit("Reset Texture Studio Liquid Section");
            host?.Repaint();
        }

        void SaveLiquid()
        {
            if (!generator.ready)
                return;

            var name = m_Stack.name.Trim();
            var texturePath = StyleStudioExport.Save(generator.normal, name + "_Normal", StyleExportKind.Normal);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var path = $"{StyleStudioExport.folder}/{StyleStudioExport.FileName(name, LayerPrefix)}.asset";
            var target = AssetDatabase.LoadAssetAtPath<HBStyleLiquidLayer>(path);
            var created = target == null;

            if (created && AssetDatabase.LoadMainAssetAtPath(path) != null)
                path = AssetDatabase.GenerateUniqueAssetPath(path);

            if (created)
                target = ScriptableObject.CreateInstance<HBStyleLiquidLayer>();
            else
                Undo.RecordObject(target, "Save Liquid Layer");

            EditorUtility.CopySerialized(working, target);
            target.hideFlags = HideFlags.None;
            target.name = Path.GetFileNameWithoutExtension(path);

            var saved = new SerializedObject(target);

            saved.FindProperty(NormalMapField).objectReferenceValue = texture;
            saved.ApplyModifiedPropertiesWithoutUndo();
            target.Touch();

            if (created)
                AssetDatabase.CreateAsset(target, path);
            else
                EditorUtility.SetDirty(target);

            AssetDatabase.SaveAssetIfDirty(target);
            EditorGUIUtility.PingObject(target);
            GUIUtility.ExitGUI();
        }

        internal override void Release()
        {
            if (m_Listening)
                Undo.undoRedoPerformed -= LookRestored;

            m_Listening = false;

            if (m_Working != null)
            {
                m_LookJson = EditorJsonUtility.ToJson(m_Working);
                UnityEngine.Object.DestroyImmediate(m_Working);
            }

            m_Working = null;
            m_Look = null;
            m_Pool?.Release();
            m_Generator?.Release();
            m_Preview?.Release();
            m_Pool = null;
            m_Generator = null;
            m_Preview = null;

            if (m_Undo != null)
                m_Undo.Release();

            m_Undo = null;
        }
    }
}
