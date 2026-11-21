using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleMaskPage : StyleStudioPage
    {
        static readonly int[] s_Resolutions = { 0, 512, 1024, 2048, 4096 };

        static readonly Regex s_MapWords = new Regex(
            @"(^TEX_)|([ _-](Albedo|BaseColou?r|BaseMap|Base|Colou?r|Diffuse|Diff|Col|D)$)",
            RegexOptions.IgnoreCase);

        static readonly StyleMaskMap[] s_Maps =
            { StyleMaskMap.Metallic, StyleMaskMap.Occlusion, StyleMaskMap.Height, StyleMaskMap.Smoothness };

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Mask",
                "Packs a mask map for Lit materials and terrain layers from the textures you have: R metallic, " +
                "G occlusion, B height, A smoothness.");

            internal static readonly GUIContent Inputs = new GUIContent("Inputs",
                "The colour and normal maps the automatic channels read, also shown in the preview.");
            internal static readonly GUIContent Albedo = new GUIContent("Albedo",
                "Colour map of the material. Automatic height, occlusion and smoothness come from its local " +
                "brightness: brighter than the area around it reads as raised.");
            internal static readonly GUIContent Normal = new GUIContent("Normal",
                "Optional normal map. With it, automatic occlusion also darkens the hollows the normals curve " +
                "into, which is more exact than brightness alone.");

            internal static readonly GUIContent[] Channels =
            {
                new GUIContent("Metallic (R)", "Red channel: 0 for stone, wood, cloth; 1 for bare metal."),
                new GUIContent("Occlusion (G)", "Green channel: darkens cracks and hollows. 1 is fully lit."),
                new GUIContent("Height (B)",
                               "Blue channel: the terrain uses it to decide which layer shows where two meet. " +
                               "Lit materials ignore it."),
                new GUIContent("Smoothness (A)", "Alpha channel: 0 is rough and matte, 1 is polished.")
            };

            internal static readonly GUIContent Source = new GUIContent("Source",
                "Constant fills the channel with one value, Texture reads it from a texture you have, Auto " +
                "estimates it from the Inputs.");
            internal static readonly GUIContent[] Sources =
            {
                new GUIContent("Constant"), new GUIContent("Texture"), new GUIContent("Auto")
            };
            internal static readonly GUIContent[] FixedSources =
            {
                new GUIContent("Constant"), new GUIContent("Texture")
            };
            internal static readonly GUIContent Texture = new GUIContent("Texture",
                "The texture to read. Its size does not have to match the others.");
            internal static readonly GUIContent Pick = new GUIContent("Channel",
                "Which channel of the texture to read. Gray mixes red, green and blue by brightness.");
            internal static readonly GUIContent Invert = new GUIContent("Invert",
                "Turns the values over. Turn it on for a Roughness map in the Smoothness channel.");
            internal static readonly GUIContent Value = new GUIContent("Value", "The value the whole channel takes.");
            internal static readonly GUIContent AutoNote = new GUIContent(
                "Estimated from the Inputs; tune it under Estimate.");

            internal static readonly GUIContent Estimate = new GUIContent("Estimate",
                "How the Auto channels are worked out from the Inputs.");
            internal static readonly GUIContent DetailSize = new GUIContent("Detail Size",
                "Size of the area each pixel is compared with, as a share of the texture. Small picks out fine " +
                "grain, large picks out whole stones and planks.");
            internal static readonly GUIContent HeightContrast = new GUIContent("Height Contrast",
                "How far brightness differences push the height up and down.");
            internal static readonly GUIContent OcclusionStrength = new GUIContent("Occlusion Strength",
                "How dark the hollows get.");
            internal static readonly GUIContent OcclusionRadius = new GUIContent("Occlusion Radius",
                "How far around each pixel the normal map is read for hollows, as a share of the texture. Only " +
                "used with a Normal input.");
            internal static readonly GUIContent CavitySmoothness = new GUIContent("Smoothness Low",
                "Smoothness in the hollows, where dirt and dust gather.");
            internal static readonly GUIContent TopSmoothness = new GUIContent("Smoothness High",
                "Smoothness on the raised parts, which wear smooth.");

            internal static readonly GUIContent Output = new GUIContent("Output");
            internal static readonly GUIContent Export = new GUIContent("Export",
                "Name, size and folder of the saved mask map, and the Save button.");
            internal static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of the saved mask map. Match Source takes the size of the largest input, rounded to a power " +
                "of two. The preview works at up to 1024.");
            internal static readonly GUIContent[] Resolutions =
            {
                new GUIContent("Match Source"), new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048"),
                new GUIContent("4096")
            };
            internal static readonly GUIContent Name = new GUIContent("Name",
                "File name, saved as TEX_Words_Like_This. Picking an Albedo names it after that texture.");
            internal static readonly GUIContent Save = new GUIContent("Save Mask",
                "Writes the mask map PNG into the folder with sRGB off. A file with the same name is replaced.");

            internal static readonly GUIContent[] MapKeys =
            {
                new GUIContent("M", "Shows the metallic channel flat."),
                new GUIContent("O", "Shows the occlusion channel flat."),
                new GUIContent("H", "Shows the height channel flat."),
                new GUIContent("S", "Shows the smoothness channel flat.")
            };

            internal static readonly GUIContent[] MapNames =
            {
                new GUIContent("Metallic"), new GUIContent("Occlusion"), new GUIContent("Height"),
                new GUIContent("Smoothness")
            };

            internal static readonly Color badge = new Color(0f, 0f, 0f, 0.55f);
        }

        [SerializeField]
        StyleMaskSettings m_Settings = new StyleMaskSettings();

        [SerializeField]
        StyleMaskMap m_Map = StyleMaskMap.Occlusion;

        [NonSerialized]
        StyleMaskGenerator m_Generator;

        [NonSerialized]
        StyleStudioPreview m_Preview;

        [NonSerialized]
        bool m_Dirty = true;

        [NonSerialized]
        StyleStudioUndoState m_Undo;

        internal override GUIContent title => Styles.Title;

        internal override bool hasLayers => false;

        StyleMaskGenerator generator => m_Generator ??= new StyleMaskGenerator();
        StyleStudioPreview preview => m_Preview ??= new StyleStudioPreview();

        StyleStudioUndoState undo =>
            m_Undo != null ? m_Undo : m_Undo = StyleStudioUndoState.Create(m_Settings, Restored);

        int outputSize => m_Settings.resolution > 0 ? m_Settings.resolution : m_Settings.sourceSize;

        int previewSize => Mathf.Min(outputSize, StyleMaskGenerator.PreviewSize);

        internal override void DrawSettings()
        {
            var history = undo;
            var settings = m_Settings;

            EditorGUI.BeginChangeCheck();

            var open = StyleSectionHeader.Draw(Styles.Inputs, "TextureStudio.Mask.Inputs", null);

            if (open)
            {
                var albedo = (Texture2D)EditorGUILayout.ObjectField(Styles.Albedo, settings.albedo, typeof(Texture2D),
                                                                    false);

                if (albedo != settings.albedo)
                {
                    settings.albedo = albedo;

                    if (albedo != null)
                        settings.name = NameFrom(albedo.name);
                }

                settings.normal = (Texture2D)EditorGUILayout.ObjectField(Styles.Normal, settings.normal,
                                                                         typeof(Texture2D), false);

                if (settings.albedo == null && settings.usesAuto)
                    EditorGUILayout.HelpBox("Auto channels need an Albedo; without one they stay flat.",
                                            MessageType.Info);
            }

            StyleSectionHeader.End(open);

            foreach (var map in s_Maps)
                DrawChannel(map);

            open = StyleSectionHeader.Draw(Styles.Estimate, "TextureStudio.Mask.Estimate",
                                           () => Reset(settings.ResetEstimate));
            if (open)
            {
                using (new EditorGUI.DisabledScope(!settings.usesAuto))
                {
                    settings.detailSize = EditorGUILayout.Slider(Styles.DetailSize, settings.detailSize, 0.002f,
                                                                 0.03f);
                    settings.heightContrast = EditorGUILayout.Slider(Styles.HeightContrast, settings.heightContrast,
                                                                     0.5f, 16f);
                    settings.occlusionStrength = EditorGUILayout.Slider(Styles.OcclusionStrength,
                                                                        settings.occlusionStrength, 0f, 2f);

                    using (new EditorGUI.DisabledScope(settings.normal == null))
                    {
                        settings.occlusionRadius = EditorGUILayout.Slider(Styles.OcclusionRadius,
                                                                          settings.occlusionRadius, 0.002f, 0.03f);
                    }

                    settings.cavitySmoothness = EditorGUILayout.Slider(Styles.CavitySmoothness,
                                                                       settings.cavitySmoothness, 0f, 1f);
                    settings.topSmoothness = EditorGUILayout.Slider(Styles.TopSmoothness, settings.topSmoothness, 0f,
                                                                    1f);
                }
            }

            StyleSectionHeader.End(open);

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            CoreEditorUtils.DrawSplitter();
            history.Commit("Change Texture Studio Mask");
        }

        void DrawChannel(StyleMaskMap map)
        {
            var settings = m_Settings;
            var channel = settings[map];
            var key = "TextureStudio.Mask." + map;
            var open = StyleSectionHeader.Draw(Styles.Channels[(int)map], key,
                                               () => Reset(() => settings.ResetChannel(map)));

            if (open)
            {
                var choices = map == StyleMaskMap.Metallic ? Styles.FixedSources : Styles.Sources;
                var index = Mathf.Min((int)channel.source, choices.Length - 1);

                channel.source = (StyleMaskSource)EditorGUILayout.Popup(Styles.Source, index, choices);

                switch (channel.source)
                {
                    case StyleMaskSource.Texture:
                        channel.texture = (Texture2D)EditorGUILayout.ObjectField(Styles.Texture, channel.texture,
                                                                                 typeof(Texture2D), false);
                        channel.pick = (StyleMaskPick)EditorGUILayout.EnumPopup(Styles.Pick, channel.pick);
                        channel.invert = EditorGUILayout.Toggle(Styles.Invert, channel.invert);

                        if (channel.texture == null)
                            channel.value = EditorGUILayout.Slider(Styles.Value, channel.value, 0f, 1f);

                        break;
                    case StyleMaskSource.Auto:
                        EditorGUILayout.LabelField(GUIContent.none, Styles.AutoNote, EditorStyles.wordWrappedMiniLabel);
                        break;
                    default:
                        channel.value = EditorGUILayout.Slider(Styles.Value, channel.value, 0f, 1f);
                        break;
                }
            }

            StyleSectionHeader.End(open);
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

            var index = Mathf.Max(Array.IndexOf(s_Resolutions, m_Settings.resolution), 0);

            index = EditorGUILayout.Popup(Styles.Resolution, index, Styles.Resolutions);
            m_Settings.resolution = s_Resolutions[index];

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            m_Settings.name = EditorGUILayout.TextField(Styles.Name, m_Settings.name);
            StyleStudioExport.DrawFolder();
            undo.Commit("Change Texture Studio Mask Output");
            host?.Repaint();
        }

        sealed class ExportPopup : PopupWindowContent
        {
            readonly StyleMaskPage m_Page;

            internal ExportPopup(StyleMaskPage page)
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
                        m_Page.SaveMask();
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
                var flat = preview.view == StyleStudioView.Flat;
                var picked = m_Map == (StyleMaskMap)i && flat;
                var style = i == 0 ? EditorStyles.miniButtonLeft :
                    i == count - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;

                if (GUI.Toggle(button, picked, Styles.MapKeys[i], style) && !picked)
                {
                    m_Map = (StyleMaskMap)i;
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
            m_Dirty = true;
            host?.Repaint();
        }

        internal override void DrawPreview(Rect rect)
        {
            if (m_Dirty || !generator.ready || generator.mask.width != previewSize)
            {
                generator.Generate(m_Settings, previewSize);
                m_Dirty = false;
            }

            var changed = GUI.changed;
            GUI.changed = false;
            DrawMapPicker(rect);
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
                preview.DrawModel(rect, m_Settings.albedo, m_Settings.normal, generator.mask);
            }

            if (GUI.changed)
                host?.Repaint();

            GUI.changed |= changed;
        }

        void Reset(Action reset)
        {
            reset();
            m_Dirty = true;
            undo.Commit("Reset Texture Studio Mask Section");
            host?.Repaint();
        }

        static string NameFrom(string texture)
        {
            var name = s_MapWords.Replace(texture, "").Trim(' ', '_', '-');

            return string.IsNullOrEmpty(name) ? "Mask" : name + " Mask";
        }

        void SaveMask()
        {
            if (!generator.Generate(m_Settings, outputSize))
                return;

            var path = StyleStudioExport.Save(generator.mask, m_Settings.name.Trim(), StyleExportKind.Data);

            m_Dirty = true;
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
