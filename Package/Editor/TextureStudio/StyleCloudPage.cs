using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleCloudPage : StyleStudioPage
    {
        static readonly int[] s_Resolutions = { 256, 512, 1024 };
        const float BarHeight = 20f;
        const float BarWidth = 160f;

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Cloud",
                "The cloud atlas the HB sky reads for its painted clouds.");

            internal static readonly GUIContent Recipe = new GUIContent("Recipe",
                "Kind of cloud. Picking one sets every value below to that recipe.");

            internal static readonly GUIContent[] RecipeNames =
            {
                new GUIContent("Cumulus", "Big, stacked, round billows with clear lit and shaded sides."),
                new GUIContent("Stratus", "Flat, even sheets with soft gaps."),
                new GUIContent("Cirrus", "Thin, stretched, wispy streaks high in the sky."),
                new GUIContent("Altocumulus", "Many small puffs in loose rows.")
            };

            internal static readonly GUIContent Shape = new GUIContent("Shape");
            internal static readonly GUIContent Breakup = new GUIContent("Breakup");
            internal static readonly GUIContent Relief = new GUIContent("Relief",
                "The slopes the sun lights. They decide how solid a cloud looks.");
            internal static readonly GUIContent BrushGroup = new GUIContent("Brush",
                "Brush strokes baked into the cloud's shape.");
            internal static readonly GUIContent Preview = new GUIContent("Preview",
                "Preview only, not saved. Mirror the Environment profile's cloud coverage and softness here.");

            internal static readonly GUIContent PuffScale = new GUIContent("Puff Scale",
                "How many puffs fit across the texture. This is the size of the billows themselves.");
            internal static readonly GUIContent Puffiness = new GUIContent("Puffiness",
                "Blends drifting noise into round billows. Low gives flat sheets, high stacked cauliflower shapes.");
            internal static readonly GUIContent ShapeScale = new GUIContent("Shape Scale",
                "Scale of the drift that decides where clouds gather and where the sky stays open.");
            internal static readonly GUIContent Octaves = new GUIContent("Octaves",
                "How many levels of detail go into that drift.");
            internal static readonly GUIContent Stretch = new GUIContent("Stretch",
                "Draws the drift and the erosion out into long streaks. 0 keeps them round.");

            internal static readonly GUIContent Erosion = new GUIContent("Erosion",
                "How far fine noise eats into the cloud, giving torn edges and holes.");
            internal static readonly GUIContent ErosionScale = new GUIContent("Erosion Scale",
                "Size of the erosion noise. Higher eats smaller bites.");
            internal static readonly GUIContent Contrast = new GUIContent("Contrast",
                "Separates cloud from sky more sharply and leaves less wispy transition. It bends the density " +
                "rather than clipping it, so the sky can still fill in fully as coverage rises.");
            internal static readonly GUIContent Warp = new GUIContent("Warp",
                "Bends the whole field with a slow drift so the puffs never line up in a grid.");
            internal static readonly GUIContent WarpScale = new GUIContent("Warp Scale",
                "How quickly the drift changes. Low moves whole regions together, high churns the shapes.");
            internal static readonly GUIContent Seed = new GUIContent("Seed",
                "Picks another random sky with the same settings.");

            internal static readonly GUIContent ReliefStrength = new GUIContent("Strength",
                "How strongly the slopes are lit. Higher reads as a solid form rather than a flat patch.");
            internal static readonly GUIContent ReliefSmoothing = new GUIContent("Smoothing",
                "Smooths the relief before it is lit. Low follows every speck and sparkles, high gives broad " +
                "billows with a clear lit and shaded side.");

            internal static readonly GUIContent Brush = new GUIContent("Brush Atlas",
                "A brush atlas saved from the Brush tab. Brush cuts its strokes into the density, Paint rebuilds " +
                "the cloud out of them. Leave it empty for clouds without strokes.");
            internal static readonly GUIContent BrushAmount = new GUIContent("Amount",
                "How far the strokes cut into the cloud's silhouette and body.");
            internal static readonly GUIContent BrushScale = new GUIContent("Scale",
                "How many times the brush atlas repeats across the cloud texture. Low gives broad sweeps, high a " +
                "finer tooth on the edge.");
            internal static readonly GUIContent Paint = new GUIContent("Paint",
                "How far the strokes repaint the cloud, so each stroke is lit as one dab. Above 1 a stroke " +
                "pulls from past its own middle.");

            internal static readonly GUIContent Coverage = new GUIContent("Coverage",
                "How much of the sky the preview fills with cloud. Not saved.");
            internal static readonly GUIContent Softness = new GUIContent("Softness",
                "How soft the cloud edges are in the preview. Not saved.");

            internal static readonly GUIContent Output = new GUIContent("Output");
            internal static readonly GUIContent Export = new GUIContent("Export",
                "Name, size and folder of the saved atlas, and the Save button.");
            internal static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of the saved atlas. 512 is usually enough: the sky repeats it at two scales.");
            internal static readonly GUIContent[] Resolutions =
            {
                new GUIContent("256"), new GUIContent("512"), new GUIContent("1024")
            };
            internal static readonly GUIContent Name = new GUIContent("Name",
                "File name, saved as TEX_Words_Like_This. Put the saved texture in the Environment profile's " +
                "Clouds › Atlas slot.");
            internal static readonly GUIContent Save = new GUIContent("Save Atlas",
                "Writes the cloud atlas PNG into the folder as linear data. A file with the same name is replaced.");

            internal static readonly GUIContent[] Views =
            {
                new GUIContent("Sky", "The real HB sky with this atlas. Drag to look around, scroll to zoom."),
                new GUIContent("Flat", "The picked map repeated 2 x 2, to check that the edges join.")
            };

            internal static readonly GUIContent[] MapKeys =
            {
                new GUIContent("S", "Shows the clouds flat with simple lighting."),
                new GUIContent("R", "Shows the relief channels flat."),
                new GUIContent("H", "Shows the height channel flat."),
                new GUIContent("D", "Shows the density channel flat.")
            };

            internal static readonly GUIContent[] MapNames =
            {
                new GUIContent("Shaded"), new GUIContent("Relief"), new GUIContent("Height"), new GUIContent("Density")
            };

            internal static readonly Color badge = new Color(0f, 0f, 0f, 0.55f);
        }

        [SerializeField]
        StyleCloudSettings m_Settings = new StyleCloudSettings();

        [SerializeField]
        StyleCloudMap m_Map;

        [SerializeField]
        bool m_Flat;

        [NonSerialized]
        StyleCloudGenerator m_Generator;

        [NonSerialized]
        StyleStudioPreview m_Preview;

        [NonSerialized]
        bool m_Dirty = true;

        [NonSerialized]
        StyleStudioUndoState m_Undo;

        internal override GUIContent title => Styles.Title;

        internal override bool hasLayers => false;

        StyleCloudGenerator generator => m_Generator ??= new StyleCloudGenerator();
        StyleStudioPreview preview => m_Preview ??= new StyleStudioPreview();

        StyleStudioUndoState undo =>
            m_Undo != null ? m_Undo : m_Undo = StyleStudioUndoState.Create(m_Settings, Restored);

        internal override void DrawSettings()
        {
            var history = undo;
            var settings = m_Settings;

            EditorGUI.BeginChangeCheck();

            var kind = (StyleCloudKind)EditorGUILayout.Popup(Styles.Recipe, (int)settings.kind, Styles.RecipeNames);

            if (kind != settings.kind)
                settings.Apply(kind);

            EditorGUILayout.Space();

            var open = StyleSectionHeader.Draw(Styles.Shape, "TextureStudio.Cloud.Shape",
                                               () => Reset(settings.ResetShape));
            if (open)
            {
                settings.puffScale = EditorGUILayout.IntSlider(Styles.PuffScale, settings.puffScale, 2, 16);
                settings.puffiness = EditorGUILayout.Slider(Styles.Puffiness, settings.puffiness, 0f, 1f);
                settings.shapeScale = EditorGUILayout.IntSlider(Styles.ShapeScale, settings.shapeScale, 1, 12);
                settings.octaves = EditorGUILayout.IntSlider(Styles.Octaves, settings.octaves, 1, 6);
                settings.stretch = EditorGUILayout.Slider(Styles.Stretch, settings.stretch, 0f, 1f);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Breakup, "TextureStudio.Cloud.Breakup",
                                           () => Reset(settings.ResetBreakup));
            if (open)
            {
                settings.erosion = EditorGUILayout.Slider(Styles.Erosion, settings.erosion, 0f, 1f);

                using (new EditorGUI.DisabledScope(settings.erosion <= 0f))
                {
                    settings.erosionScale = EditorGUILayout.IntSlider(Styles.ErosionScale, settings.erosionScale, 2,
                                                                      48);
                }

                settings.contrast = EditorGUILayout.Slider(Styles.Contrast, settings.contrast, 0.25f, 4f);
                settings.warp = EditorGUILayout.Slider(Styles.Warp, settings.warp, 0f, 1f);

                using (new EditorGUI.DisabledScope(settings.warp <= 0f))
                {
                    settings.warpScale = EditorGUILayout.IntSlider(Styles.WarpScale, settings.warpScale, 1, 8);
                }

                settings.seed = EditorGUILayout.IntSlider(Styles.Seed, settings.seed, 0, 999);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Relief, "TextureStudio.Cloud.Relief",
                                           () => Reset(settings.ResetRelief));
            if (open)
            {
                settings.reliefStrength = EditorGUILayout.Slider(Styles.ReliefStrength, settings.reliefStrength, 0f,
                                                                 1f);
                settings.reliefSmoothing = EditorGUILayout.Slider(Styles.ReliefSmoothing, settings.reliefSmoothing,
                                                                  0f, 1f);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.BrushGroup, "TextureStudio.Cloud.Brush",
                                           () => Reset(settings.ResetBrush));
            if (open)
            {
                settings.brush = (Texture2D)EditorGUILayout.ObjectField(Styles.Brush, settings.brush,
                                                                       typeof(Texture2D), false);

                using (new EditorGUI.DisabledScope(settings.brush == null))
                {
                    settings.brushAmount = EditorGUILayout.Slider(Styles.BrushAmount, settings.brushAmount, 0f, 1f);
                    settings.brushScale = EditorGUILayout.Slider(Styles.BrushScale, settings.brushScale, 0.25f, 8f);
                    settings.paint = EditorGUILayout.Slider(Styles.Paint, settings.paint, 0f, 3f);
                }
            }

            StyleSectionHeader.End(open);

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            open = StyleSectionHeader.Draw(Styles.Preview, "TextureStudio.Cloud.Preview",
                                           () => Reset(settings.ResetPreview));
            if (open)
            {
                settings.previewCoverage = EditorGUILayout.Slider(Styles.Coverage, settings.previewCoverage, 0f, 1f);
                settings.previewSoftness = EditorGUILayout.Slider(Styles.Softness, settings.previewSoftness, 0.01f,
                                                                  1f);

                var bar = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true)));
                var visible = generator.ready ? generator.visible : 0f;

                EditorGUI.ProgressBar(bar, visible, $"Sky covered {visible * 100f:0}%");
            }

            StyleSectionHeader.End(open);
            CoreEditorUtils.DrawSplitter();
            history.Commit("Change Texture Studio Cloud");
        }

        internal override void DrawLayers()
        {
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

            var index = Array.IndexOf(s_Resolutions, m_Settings.resolution);

            if (index < 0)
                index = 1;

            index = EditorGUILayout.Popup(Styles.Resolution, index, Styles.Resolutions);
            m_Settings.resolution = s_Resolutions[index];

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            m_Settings.name = EditorGUILayout.TextField(Styles.Name, m_Settings.name);
            StyleStudioExport.DrawFolder();
            undo.Commit("Change Texture Studio Cloud Output");
            host?.Repaint();
        }

        sealed class ExportPopup : PopupWindowContent
        {
            readonly StyleCloudPage m_Page;

            internal ExportPopup(StyleCloudPage page)
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

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_Page.m_Settings.name)))
                {
                    if (GUILayout.Button(Styles.Save))
                    {
                        editorWindow.Close();
                        m_Page.SaveAtlas();
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
                var picked = m_Map == (StyleCloudMap)i && m_Flat;
                var style = i == 0 ? EditorStyles.miniButtonLeft :
                    i == count - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;

                if (GUI.Toggle(button, picked, Styles.MapKeys[i], style) && !picked)
                {
                    m_Map = (StyleCloudMap)i;
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
            m_Dirty = true;
            host?.Repaint();
        }

        internal override void DrawPreview(Rect rect)
        {
            var settings = m_Settings;

            if (m_Dirty || !generator.ready)
            {
                generator.Generate(settings);
                m_Dirty = false;
            }

            if (generator.ready && Event.current.type == EventType.Repaint)
                generator.Show(m_Map, settings.previewCoverage, settings.previewSoftness);

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
                preview.DrawFlat(rect, generator.display);
                DrawMapName(rect);
            }
            else
            {
                preview.DrawSky(rect, generator.atlas, settings.previewCoverage, settings.previewSoftness);
            }

            if (GUI.changed)
                host?.Repaint();

            GUI.changed |= changed;
        }

        void Reset(Action reset)
        {
            reset();
            m_Dirty = true;
            undo.Commit("Reset Texture Studio Cloud Section");
            host?.Repaint();
        }

        void SaveAtlas()
        {
            if (!generator.ready)
                return;

            var path = StyleStudioExport.Save(generator.atlas, m_Settings.name.Trim(), StyleExportKind.Data);

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
