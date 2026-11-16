using System;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleMaterialPage : StyleStudioPage
    {
        static readonly int[] s_Resolutions = { 512, 1024, 2048 };

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Material",
                "Stone, brick, plank and ground textures for terrain layers and Lit materials.");

            internal static readonly GUIContent Recipe = new GUIContent("Recipe",
                "The kind of surface to generate. Each recipe builds shape, height, normal and colour in fixed steps.");

            internal static readonly GUIContent Pattern = new GUIContent("Pattern");
            internal static readonly GUIContent Colour = new GUIContent("Colour");
            internal static readonly GUIContent Surface = new GUIContent("Surface");
            internal static readonly GUIContent Light = new GUIContent("Painted Light");
            internal static readonly GUIContent Output = new GUIContent("Output");

            internal static readonly GUIContent Tiles = new GUIContent("Tiles", "Tiles across the texture.");
            internal static readonly GUIContent Gap = new GUIContent("Gap", "Width of the joint between tiles.");
            internal static readonly GUIContent Bevel = new GUIContent("Bevel",
                "How far in from the edge each tile keeps rounding up to its full height.");
            internal static readonly GUIContent HeightVariation = new GUIContent("Height Variation",
                "How much lower some tiles sit than others.");
            internal static readonly GUIContent Seed = new GUIContent("Seed", "Picks another random layout.");

            internal static readonly GUIContent TileColour = new GUIContent("Colour", "Colour of the tiles.");
            internal static readonly GUIContent GapColour = new GUIContent("Gap Colour", "Colour of the joints.");
            internal static readonly GUIContent Variation = new GUIContent("Variation",
                "How much lighter or darker each tile is than the colour.");

            internal static readonly GUIContent Depth = new GUIContent("Depth",
                "Height of the relief as a share of the texture's width. Drives the normal map strength, the " +
                "edges and the occlusion.");
            internal static readonly GUIContent Smoothness = new GUIContent("Smoothness",
                "Smoothness of the tiles, saved in the mask's alpha.");
            internal static readonly GUIContent GapSmoothness = new GUIContent("Gap Smoothness",
                "Smoothness of the joints.");
            internal static readonly GUIContent Metallic = new GUIContent("Metallic",
                "Metallic of the tiles, saved in the mask's red channel.");

            internal static readonly GUIContent EdgeHighlight = new GUIContent("Edge Highlight",
                "Lightens raised edges in the colour map, like a painted highlight. 0 keeps the pure colour.");
            internal static readonly GUIContent CavityShade = new GUIContent("Cavity Shade",
                "Darkens creases and corners in the colour map. 0 keeps the pure colour.");
            internal static readonly GUIContent Occlusion = new GUIContent("Occlusion",
                "Bakes the soft shadow of the relief into the colour map. 0 keeps the pure colour.");
            internal static readonly GUIContent LightWidth = new GUIContent("Width",
                "Width of the edge highlight and the cavity shade, in pixels at 256.");

            internal static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of the saved textures.");
            internal static readonly GUIContent Name = new GUIContent("Name",
                "File name start. Saving writes Name_Albedo, Name_Normal and Name_Mask.");
            internal static readonly GUIContent Save = new GUIContent("Save Textures",
                "Writes the albedo, normal and mask PNGs into the folder. Files with the same names are replaced.");

            internal static readonly GUIContent Maps = new GUIContent("Maps");

            internal static readonly GUIContent[] Resolutions =
            {
                new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048")
            };

            internal static readonly GUIContent[] MapNames =
            {
                new GUIContent("Albedo", "Colour with the painted light."),
                new GUIContent("Normal", "Normal map as saved."),
                new GUIContent("Mask", "Metallic, occlusion and height as red, green and blue."),
                new GUIContent("Height", "Relief, white is high."),
                new GUIContent("Occlusion", "Soft shadow of the relief."),
                new GUIContent("Edges", "Raised edges light, creases dark.")
            };
        }

        [SerializeField]
        StyleMaterialSettings m_Settings = new StyleMaterialSettings();

        [SerializeField]
        string m_Preset;

        [SerializeField]
        StyleMaterialMap m_Map;

        [NonSerialized]
        StyleStudioPresets m_Presets;

        [NonSerialized]
        StyleMaterialGenerator m_Generator;

        [NonSerialized]
        StyleStudioPreview m_Preview;

        [NonSerialized]
        bool m_Dirty = true;

        [NonSerialized]
        StyleStudioUndoState m_Undo;

        internal override GUIContent title => Styles.Title;

        StyleStudioPresets presets => m_Presets ??= new StyleStudioPresets("Material");
        StyleMaterialGenerator generator => m_Generator ??= new StyleMaterialGenerator();
        StyleStudioPreview preview => m_Preview ??= new StyleStudioPreview();

        StyleStudioUndoState undo =>
            m_Undo != null ? m_Undo : m_Undo = StyleStudioUndoState.Create(m_Settings, Restored);

        internal override void DrawSettings()
        {
            var history = undo;

            presets.Draw(ref m_Preset, m_Settings, Load);
            EditorGUILayout.Space();

            var settings = m_Settings;

            EditorGUI.BeginChangeCheck();

            settings.recipe = (StyleMaterialRecipe)EditorGUILayout.EnumPopup(Styles.Recipe, settings.recipe);

            var open = StyleSectionHeader.Draw(Styles.Pattern, "TextureStudio.Material.Pattern",
                                               () => Reset(settings.ResetPattern));
            if (open)
            {
                settings.tiles = EditorGUILayout.IntSlider(Styles.Tiles, settings.tiles, 1, 16);
                settings.gap = EditorGUILayout.Slider(Styles.Gap, settings.gap, 0f, 0.3f);
                settings.bevel = EditorGUILayout.Slider(Styles.Bevel, settings.bevel, 0.01f, 0.5f);
                settings.heightVariation = EditorGUILayout.Slider(Styles.HeightVariation, settings.heightVariation,
                                                                  0f, 1f);
                settings.seed = EditorGUILayout.IntSlider(Styles.Seed, settings.seed, 0, 999);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Colour, "TextureStudio.Material.Colour",
                                           () => Reset(settings.ResetColour));
            if (open)
            {
                settings.colour = EditorGUILayout.ColorField(Styles.TileColour, settings.colour, true, false, false);
                settings.gapColour = EditorGUILayout.ColorField(Styles.GapColour, settings.gapColour, true, false,
                                                                false);
                settings.variation = EditorGUILayout.Slider(Styles.Variation, settings.variation, 0f, 0.5f);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Surface, "TextureStudio.Material.Surface",
                                           () => Reset(settings.ResetSurface));
            if (open)
            {
                settings.depth = EditorGUILayout.Slider(Styles.Depth, settings.depth, 0.001f, 0.1f);
                settings.smoothness = EditorGUILayout.Slider(Styles.Smoothness, settings.smoothness, 0f, 1f);
                settings.gapSmoothness = EditorGUILayout.Slider(Styles.GapSmoothness, settings.gapSmoothness, 0f,
                                                                1f);
                settings.metallic = EditorGUILayout.Slider(Styles.Metallic, settings.metallic, 0f, 1f);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Light, "TextureStudio.Material.Light",
                                           () => Reset(settings.ResetLight));
            if (open)
            {
                settings.edgeHighlight = EditorGUILayout.Slider(Styles.EdgeHighlight, settings.edgeHighlight, 0f, 2f);
                settings.cavityShade = EditorGUILayout.Slider(Styles.CavityShade, settings.cavityShade, 0f, 2f);
                settings.occlusion = EditorGUILayout.Slider(Styles.Occlusion, settings.occlusion, 0f, 1f);
                settings.lightWidth = EditorGUILayout.Slider(Styles.LightWidth, settings.lightWidth, 1f, 8f);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Output, "TextureStudio.Material.Output", null);
            if (open)
            {
                var index = Mathf.Max(Array.IndexOf(s_Resolutions, settings.resolution), 0);
                index = EditorGUILayout.Popup(Styles.Resolution, index, Styles.Resolutions);
                settings.resolution = s_Resolutions[index];
            }

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            if (open)
            {
                settings.name = EditorGUILayout.TextField(Styles.Name, settings.name);
                StyleStudioExport.DrawFolder();

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(settings.name)))
                {
                    if (GUILayout.Button(Styles.Save))
                        SaveTextures();
                }
            }

            StyleSectionHeader.End(open);
            history.Commit("Change Texture Studio");
        }

        void Restored()
        {
            m_Dirty = true;
            host?.Repaint();
        }

        internal override void DrawLayers()
        {
            EditorGUILayout.LabelField(Styles.Maps, EditorStyles.boldLabel);

            for (var i = 0; i < Styles.MapNames.Length; i++)
            {
                var picked = m_Map == (StyleMaterialMap)i && preview.view == StyleStudioView.Flat;

                if (GUILayout.Toggle(picked, Styles.MapNames[i], EditorStyles.miniButton) && !picked)
                {
                    m_Map = (StyleMaterialMap)i;
                    preview.view = StyleStudioView.Flat;
                }
            }
        }

        internal override void DrawPreview(Rect rect)
        {
            if (m_Dirty || !generator.ready)
            {
                generator.Generate(m_Settings);
                m_Dirty = false;
            }

            var changed = GUI.changed;
            GUI.changed = false;
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
            }
            else
            {
                preview.DrawModel(rect, generator.albedo, generator.normal, generator.mask);
            }

            if (GUI.changed)
                host?.Repaint();

            GUI.changed |= changed;
        }

        internal override void Release()
        {
            m_Generator?.Release();
            m_Preview?.Release();

            if (m_Undo != null)
                m_Undo.Release();

            m_Undo = null;
        }

        void Load(string name)
        {
            if (!presets.Load(name, m_Settings))
                return;

            m_Dirty = true;
            undo.Commit("Load Texture Studio Preset");
        }

        void Reset(Action reset)
        {
            reset();
            m_Dirty = true;
            undo.Commit("Reset Texture Studio Section");
            host?.Repaint();
        }

        void SaveTextures()
        {
            if (!generator.ready)
                return;

            var name = m_Settings.name.Trim();
            var albedo = StyleStudioExport.Save(generator.Export(StyleMaterialMap.Albedo), name + "_Albedo",
                                                StyleExportKind.Colour);
            StyleStudioExport.Save(generator.Export(StyleMaterialMap.Normal), name + "_Normal",
                                   StyleExportKind.Normal);
            StyleStudioExport.Save(generator.Export(StyleMaterialMap.Mask), name + "_Mask", StyleExportKind.Data);

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
            GUIUtility.ExitGUI();
        }
    }
}
