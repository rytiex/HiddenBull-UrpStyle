using System;
using System.Collections.Generic;
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
        const string MaskMapField = "m_MaskMap";
        const int MostLayers = 8;
        const int LookRow = -1;
        const float BarHeight = 20f;
        const float BarWidth = 160f;

        static readonly StyleLiquidChannel[] s_Channels =
        {
            StyleLiquidChannel.Tone, StyleLiquidChannel.Solid, StyleLiquidChannel.Lines
        };

        static readonly StyleLiquidMap[] s_Maps =
        {
            StyleLiquidMap.Colour, StyleLiquidMap.Normal, StyleLiquidMap.Height, StyleLiquidMap.Tone,
            StyleLiquidMap.Solid, StyleLiquidMap.Lines
        };

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Liquid",
                "Water, acid and lava: the liquid's layers, its masks and normal map, and its Look, saved together " +
                "into one Liquid Layer.");

            internal static readonly GUIContent Liquid = new GUIContent("Liquid",
                "The Look of the liquid on top, then the layers that paint its masks and bumps, top first.");
            internal static readonly GUIContent Look = new GUIContent("Look",
                "Every setting of the Liquid Layer: colour, rocks, lines, motion, waves, foam and underwater.");
            internal static readonly string LookSubtitle = "Colour · Rocks · Lines · Motion · Waves";

            internal static readonly GUIContent Shape = new GUIContent("Shape");
            internal static readonly GUIContent Detail = new GUIContent("Detail");
            internal static readonly GUIContent Effects = new GUIContent("Effects");
            internal static readonly GUIContent Targets = new GUIContent("Targets");

            internal static readonly GUIContent Bend = new GUIContent("Warp",
                "Bends the layer's shape with soft noise, so straight edges and round shapes turn lumpy and uneven.");
            internal static readonly GUIContent BendScale = new GUIContent("Warp Scale",
                "How many times the bending noise repeats across the texture. Higher gives smaller, busier bends.");
            internal static readonly GUIContent Blur = new GUIContent("Blur",
                "Softens the layer's shape. Its edges spread into each other.");
            internal static readonly GUIContent Black = new GUIContent("Black",
                "Level of the shape that turns fully black. Higher darkens more of the shape.");
            internal static readonly GUIContent White = new GUIContent("White",
                "Level of the shape that turns fully white. Lower brightens more of the shape.");
            internal static readonly GUIContent Gamma = new GUIContent("Gamma",
                "Brightens the middle greys above 1 and darkens them below 1, keeping black and white.");

            internal static readonly GUIContent Kind = new GUIContent("Kind",
                "What the picked layer draws. Changing it resets the layer's shape settings.");

            internal static readonly GUIContent[] KindNames =
            {
                new GUIContent("Waves", "Crossing wave trains, like wind-driven water."),
                new GUIContent("Domes", "Swollen round domes over a slow swell, like boiling acid."),
                new GUIContent("Flow", "Swirling, stretched currents with cooled streaks."),
                new GUIContent("Cells", "A net of ridges between round cells, like foam."),
                new GUIContent("Noise", "Soft, random clouds."),
                new GUIContent("Plates", "Raised, chiselled rock plates split by narrow cracks."),
                new GUIContent("Blobs", "Rounded blobs or cells with gaps between them, like cartoon water or " +
                               "veined acid."),
                new GUIContent("Swirl", "Rings that curl around scattered centres, like stirred, glowing melt."),
                new GUIContent("Rocks", "Scattered rounded rocks with open liquid between them.")
            };

            internal static readonly GUIContent[] ChannelNames =
            {
                new GUIContent("Tone",
                               "Paints where along the Look's Depth Gradient each spot sits. Dark takes the " +
                               "gradient's start, bright its end."),
                new GUIContent("Solid", "Paints floating rocks or crust in the Look's Rock Color. White is solid."),
                new GUIContent("Lines", "Paints lines, rims, veins or foam in the Look's Line Color.")
            };

            internal static readonly GUIContent Height = new GUIContent("Height",
                "Adds the shape to the bumps of the surface, which the normal map is made from.");
            internal static readonly GUIContent Blend = new GUIContent("Blend",
                "How the layer joins the layers under it in this mask. Add brightens, Multiply darkens where the " +
                "layer is dark, Subtract cuts the layer out, Max keeps the brighter of the two.");
            internal static readonly GUIContent Opacity = new GUIContent("Opacity",
                "How strongly the layer blends into this mask.");
            internal static readonly GUIContent Threshold = new GUIContent("Threshold",
                "Level of the shape where the mask turns from black to white. Higher keeps only the shape's " +
                "brightest parts.");
            internal static readonly GUIContent Feather = new GUIContent("Feather",
                "How soft the turn from black to white is. 1 keeps every grey of the shape, 0 cuts it sharply.");
            internal static readonly GUIContent TargetInvert = new GUIContent("Invert",
                "Uses the shape upside down in this mask only, so gaps become the white parts.");
            internal static readonly GUIContent Strength = new GUIContent("Strength",
                "How strongly the layer bends the surface.");
            internal static readonly GUIContent Scale = new GUIContent("Scale",
                "How many times the pattern repeats across the texture. Higher is smaller and finer.");
            internal static readonly GUIContent Seed = new GUIContent("Seed",
                "Picks another random layout with the same settings.");
            internal static readonly GUIContent Invert = new GUIContent("Invert",
                "Turns the shape upside down for every target, so gaps become ridges and domes become dimples.");

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
                "Width of the cracks or gaps between the plates, blobs or rocks.");
            internal static readonly GUIContent GapVariation = new GUIContent("Gap Variation",
                "How much the cracks narrow and widen along their length.");
            internal static readonly GUIContent Facets = new GUIContent("Facets",
                "How sharply each plate is chiselled into flat faces. 0 leaves smooth, flat-topped plates.");
            internal static readonly GUIContent Grain = new GUIContent("Grain",
                "Fine roughness on the rock faces.");
            internal static readonly GUIContent Roundness = new GUIContent("Roundness",
                "Rounds each plate, blob or rock into a soft pillow that falls away towards its edges.");
            internal static readonly GUIContent BlobSize = new GUIContent("Size",
                "Largest size of a blob or rock. 1 lets them fill their cells, leaving only the gaps between them.");
            internal static readonly GUIContent SizeVariation = new GUIContent("Size Variation",
                "How much smaller some blobs or rocks are than others.");
            internal static readonly GUIContent Corners = new GUIContent("Corners",
                "How round the corners are. Rounder corners widen the gaps where they meet.");
            internal static readonly GUIContent Warp = new GUIContent("Warp",
                "How much the shapes bend into lumpy, uneven forms.");
            internal static readonly GUIContent Softness = new GUIContent("Softness",
                "How softly the blobs or rocks fade into the gaps.");
            internal static readonly GUIContent Density = new GUIContent("Density",
                "Share of the cells that hold a rock. Lower leaves fewer rocks and more open liquid.");
            internal static readonly GUIContent Rings = new GUIContent("Rings",
                "How many rings circle each centre.");
            internal static readonly GUIContent Twist = new GUIContent("Twist",
                "How many arms the rings wind into. 0 gives round rings, more gives curling spirals.");
            internal static readonly GUIContent Reach = new GUIContent("Reach",
                "How far each swirl reaches from its centre before fading into its neighbours.");

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
                "Name, size and folder of the saved Liquid Layer, and the Save button.");
            internal static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of the normal map and mask map saved inside the layer.");
            internal static readonly GUIContent[] Resolutions =
            {
                new GUIContent("256"), new GUIContent("512"), new GUIContent("1024")
            };
            internal static readonly GUIContent Name = new GUIContent("Name",
                "Saved as LIQ_Name. Saving again with the same name updates the layer and its maps in place, so " +
                "everything using it keeps it.");
            internal static readonly GUIContent Save = new GUIContent("Save Layer",
                "Writes the Liquid Layer with its normal map and mask map saved inside it.");

            internal static readonly GUIContent[] Views =
            {
                new GUIContent("Liquid", "The real HB liquid in a small pool. Drag to turn, scroll to zoom."),
                new GUIContent("Flat", "The picked map repeated 2 x 2, to check that the edges join.")
            };

            internal static readonly GUIContent[] MapKeys =
            {
                new GUIContent("C", "Shows the masks painted in the Look's colours."),
                new GUIContent("N", "Shows the normal map flat."),
                new GUIContent("H", "Shows the height the normal map is made from."),
                new GUIContent("T", "Shows the Tone mask."),
                new GUIContent("S", "Shows the Solid mask."),
                new GUIContent("L", "Shows the Lines mask.")
            };

            internal static readonly GUIContent[] MapNames =
            {
                new GUIContent("Normal"), new GUIContent("Height"), new GUIContent("Tone"), new GUIContent("Solid"),
                new GUIContent("Lines"), new GUIContent("Colour")
            };

            internal static readonly Color selection = new Color(0.24f, 0.48f, 0.9f, 0.35f);
            internal static readonly Color badge = new Color(0f, 0f, 0f, 0.55f);
            internal static readonly Color separator = new Color(0f, 0f, 0f, 0.3f);
        }

        [SerializeField]
        StyleRippleStack m_Stack = new StyleRippleStack();

        [SerializeField]
        string m_LookJson;

        [SerializeField]
        int m_Selected = LookRow;

        [SerializeField]
        bool m_Flat;

        [SerializeField]
        StyleLiquidMap m_Map = StyleLiquidMap.Colour;

        [SerializeField]
        string m_Preset = StyleLiquidShelf.Starter + "/" + nameof(StyleLiquidRecipe.Water);

        [SerializeField]
        string m_Category = StyleLiquidShelf.Starter;

        [NonSerialized]
        HBStyleLiquidLayer m_Working;

        [NonSerialized]
        SerializedObject m_Look;

        [NonSerialized]
        StyleLiquidGenerator m_Generator;

        [NonSerialized]
        StyleLiquidShelf m_Shelf;

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
        int m_Renaming = -2;

        [NonSerialized]
        bool m_Focus;

        internal override GUIContent title => Styles.Title;

        StyleLiquidGenerator generator => m_Generator ??= new StyleLiquidGenerator();
        StyleLiquidShelf shelf => m_Shelf ??= new StyleLiquidShelf();
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
                    Start();
                }

                AttachMaps();
                return m_Working;
            }
        }

        StyleRippleLayer current
        {
            get
            {
                if (m_Stack.layers == null)
                    m_Stack.layers = new List<StyleRippleLayer>();

                if (m_Stack.layers.Count == 0)
                    m_Stack.layers.Add(StyleRippleLayer.Create(StyleRippleKind.Waves));

                if (m_Selected == LookRow)
                    return null;

                m_Selected = Mathf.Clamp(m_Selected, 0, m_Stack.layers.Count - 1);
                return m_Stack.layers[m_Selected];
            }
        }

        StyleLiquidPreset snapshot => new StyleLiquidPreset { stack = m_Stack, look = m_LookJson };

        internal override void DrawSettings()
        {
            var history = undo;
            var layer = working;
            var picked = current;

            EditorGUI.BeginChangeCheck();

            if (picked == null)
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
                DrawRipple(picked);
            }

            if (EditorGUI.EndChangeCheck() && picked != null)
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

                layer.scale = EditorGUILayout.IntSlider(Styles.Scale, layer.scale, 1, 32);
                layer.seed = EditorGUILayout.IntSlider(Styles.Seed, layer.seed, 0, 999);
                layer.invert = EditorGUILayout.Toggle(Styles.Invert, layer.invert);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Detail, "TextureStudio.Liquid.Detail",
                                           () => Reset(layer.ResetShape));
            if (open)
                DrawDetail(layer);

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Effects, "TextureStudio.Liquid.Effects",
                                           () => Reset(layer.ResetEffects));
            if (open)
                DrawEffects(layer);

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Targets, "TextureStudio.Liquid.Targets",
                                           () => Reset(() => ResetTargets(layer)));
            if (open)
                DrawTargets(layer);

            StyleSectionHeader.End(open);
            CoreEditorUtils.DrawSplitter();
        }

        static void DrawEffects(StyleRippleLayer layer)
        {
            layer.bend = EditorGUILayout.Slider(Styles.Bend, layer.bend, 0f, 1f);

            using (new EditorGUI.DisabledScope(layer.bend <= 0f))
                layer.bendScale = EditorGUILayout.IntSlider(Styles.BendScale, layer.bendScale, 1, 16);

            layer.blur = EditorGUILayout.Slider(Styles.Blur, layer.blur, 0f, 1f);
            layer.black = EditorGUILayout.Slider(Styles.Black, layer.black, 0f, 1f);
            layer.white = EditorGUILayout.Slider(Styles.White, layer.white, 0f, 1f);
            layer.gamma = EditorGUILayout.Slider(Styles.Gamma, layer.gamma, 0.2f, 5f);
        }

        static void ResetTargets(StyleRippleLayer layer)
        {
            foreach (var channel in s_Channels)
                layer.Target(channel).Reset();

            layer.strength = 1f;
        }

        static void DrawTargets(StyleRippleLayer layer)
        {
            foreach (var channel in s_Channels)
            {
                var target = layer.Target(channel);

                target.on = EditorGUILayout.ToggleLeft(Styles.ChannelNames[(int)channel], target.on,
                                                       EditorStyles.boldLabel);

                if (!target.on)
                    continue;

                using (new EditorGUI.IndentLevelScope())
                {
                    target.blend = (StyleLiquidBlend)EditorGUILayout.EnumPopup(Styles.Blend, target.blend);
                    target.opacity = EditorGUILayout.Slider(Styles.Opacity, target.opacity, 0f, 1f);
                    target.threshold = EditorGUILayout.Slider(Styles.Threshold, target.threshold, 0f, 1f);
                    target.feather = EditorGUILayout.Slider(Styles.Feather, target.feather, 0f, 1f);
                    target.invert = EditorGUILayout.Toggle(Styles.TargetInvert, target.invert);
                }
            }

            layer.height = EditorGUILayout.ToggleLeft(Styles.Height, layer.height, EditorStyles.boldLabel);

            if (!layer.height)
                return;

            using (new EditorGUI.IndentLevelScope())
                layer.strength = EditorGUILayout.Slider(Styles.Strength, layer.strength, 0f, 3f);
        }

        static void DrawDetail(StyleRippleLayer layer)
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
                    layer.edgeSharpness = EditorGUILayout.Slider(Styles.EdgeSharpness, layer.edgeSharpness, 0.25f,
                                                                 4f);
                    break;
                case StyleRippleKind.Noise:
                    layer.octaves = EditorGUILayout.IntSlider(Styles.Octaves, layer.octaves, 1, 6);
                    break;
                case StyleRippleKind.Plates:
                    layer.gap = EditorGUILayout.Slider(Styles.Gap, layer.gap, 0f, 1f);
                    layer.gapVariation = EditorGUILayout.Slider(Styles.GapVariation, layer.gapVariation, 0f, 1f);
                    layer.facets = EditorGUILayout.Slider(Styles.Facets, layer.facets, 0f, 1f);
                    layer.grain = EditorGUILayout.Slider(Styles.Grain, layer.grain, 0f, 1f);
                    layer.roundness = EditorGUILayout.Slider(Styles.Roundness, layer.roundness, 0f, 1f);
                    break;
                case StyleRippleKind.Rocks:
                    layer.density = EditorGUILayout.Slider(Styles.Density, layer.density, 0f, 1f);
                    DrawBlob(layer);
                    break;
                case StyleRippleKind.Blobs:
                    DrawBlob(layer);
                    break;
                case StyleRippleKind.Swirl:
                    layer.rings = EditorGUILayout.Slider(Styles.Rings, layer.rings, 0.5f, 8f);
                    layer.twist = EditorGUILayout.IntSlider(Styles.Twist, layer.twist, 0, 6);
                    layer.reach = EditorGUILayout.Slider(Styles.Reach, layer.reach, 0.3f, 1.5f);
                    layer.warp = EditorGUILayout.Slider(Styles.Warp, layer.warp, 0f, 1f);
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

        static void DrawBlob(StyleRippleLayer layer)
        {
            layer.blobSize = EditorGUILayout.Slider(Styles.BlobSize, layer.blobSize, 0f, 1f);
            layer.sizeVariation = EditorGUILayout.Slider(Styles.SizeVariation, layer.sizeVariation, 0f, 1f);
            layer.gap = EditorGUILayout.Slider(Styles.Gap, layer.gap, 0f, 1f);
            layer.gapVariation = EditorGUILayout.Slider(Styles.GapVariation, layer.gapVariation, 0f, 1f);
            layer.corners = EditorGUILayout.Slider(Styles.Corners, layer.corners, 0f, 1f);
            layer.warp = EditorGUILayout.Slider(Styles.Warp, layer.warp, 0f, 1f);
            layer.softness = EditorGUILayout.Slider(Styles.Softness, layer.softness, 0f, 1f);
            layer.roundness = EditorGUILayout.Slider(Styles.Roundness, layer.roundness, 0f, 1f);
        }

        void Start()
        {
            shelf.Seed();

            var loaded = shelf.Parse(m_Preset, m_Stack.resolution);

            if (loaded != null)
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(loaded.stack), m_Stack);
                EditorJsonUtility.FromJsonOverwrite(loaded.look, m_Working);
                m_Working.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                m_Working.name = "HB Texture Studio Liquid";
                m_Look.Update();
            }
            else
            {
                StyleLiquidRecipes.Apply(m_Look, StyleLiquidRecipe.Water);
                m_Look.ApplyModifiedPropertiesWithoutUndo();
            }

            m_Dirty = true;
            m_LookJson = EditorJsonUtility.ToJson(m_Working);
        }

        void LoadPreset(string name)
        {
            var loaded = shelf.Parse(name, m_Stack.resolution);

            if (loaded == null)
                return;

            var layer = working;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(loaded.stack), m_Stack);
            Undo.RecordObject(layer, "Load Texture Studio Liquid Preset");
            EditorJsonUtility.FromJsonOverwrite(loaded.look, layer);
            layer.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            layer.name = "HB Texture Studio Liquid";
            m_Look.Update();
            m_Preset = name;
            m_Selected = LookRow;
            m_Renaming = -2;
            m_Dirty = true;
            AttachMaps();
            layer.Touch();
            m_LookJson = EditorJsonUtility.ToJson(layer);
            undo.Commit("Load Texture Studio Liquid Preset");
            host?.Repaint();
        }

        void AttachMaps()
        {
            if (m_Look == null)
                return;

            var normal = m_Generator != null ? m_Generator.previewNormal : null;
            var mask = m_Generator != null ? m_Generator.previewMask : null;
            var normalField = m_Look.FindProperty(NormalMapField);
            var maskField = m_Look.FindProperty(MaskMapField);

            if (normalField.objectReferenceValue == normal && maskField.objectReferenceValue == mask)
                return;

            normalField.objectReferenceValue = normal;
            maskField.objectReferenceValue = mask;
            m_Look.ApplyModifiedPropertiesWithoutUndo();
            m_Working.Touch();
        }

        void LookRestored()
        {
            if (m_Working == null)
                return;

            m_Look?.Update();
            AttachMaps();
            m_Working.Touch();
            m_LookJson = EditorJsonUtility.ToJson(m_Working);
            host?.Repaint();
        }

        internal override void DrawLayers()
        {
            var history = undo;
            var picked = current;
            var layers = m_Stack.layers;

            EditorGUILayout.LabelField(Styles.Liquid, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawLookRow();

                var line = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));

                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(line, Styles.separator);

                for (var i = layers.Count - 1; i >= 0; i--)
                    DrawStackRow(layers, i);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(layers.Count >= MostLayers))
                {
                    var add = GUILayoutUtility.GetRect(Styles.AddLayer, EditorStyles.miniButtonLeft);

                    if (GUI.Button(add, Styles.AddLayer, EditorStyles.miniButtonLeft))
                        ShowAddMenu(add);

                    using (new EditorGUI.DisabledScope(picked == null))
                    {
                        if (GUILayout.Button(Styles.DuplicateLayer, EditorStyles.miniButtonMid))
                        {
                            var copy = picked.Clone();

                            copy.seed = (picked.seed + 1) % 1000;
                            layers.Insert(m_Selected + 1, copy);
                            m_Selected++;
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(picked == null || layers.Count <= 1))
                {
                    if (GUILayout.Button(Styles.RemoveLayer, EditorStyles.miniButtonMid))
                    {
                        layers.RemoveAt(m_Selected);
                        m_Selected = Mathf.Clamp(m_Selected, 0, layers.Count - 1);
                    }
                }

                using (new EditorGUI.DisabledScope(picked == null || m_Selected >= layers.Count - 1))
                {
                    if (GUILayout.Button(Styles.RaiseLayer, EditorStyles.miniButtonMid))
                        Swap(layers, m_Selected, m_Selected + 1);
                }

                using (new EditorGUI.DisabledScope(picked == null || m_Selected <= 0))
                {
                    if (GUILayout.Button(Styles.LowerLayer, EditorStyles.miniButtonRight))
                        Swap(layers, m_Selected, m_Selected - 1);
                }
            }

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio Liquid Layers");
        }

        void DrawLookRow()
        {
            var row = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            var picture = new Rect(row.x + 26f, row.y + 4f, 32f, 32f);
            var text = new Rect(picture.xMax + 8f, row.y + 4f, row.xMax - picture.xMax - 12f, 16f);
            var sub = new Rect(text.x, text.yMax, text.width, 16f);

            if (Event.current.type == EventType.Repaint)
            {
                if (m_Selected == LookRow)
                    EditorGUI.DrawRect(row, Styles.selection);

                var gradient = working.depthGradient;
                var half = picture.height * 0.5f;

                EditorGUI.DrawRect(new Rect(picture.x, picture.y, picture.width, half), gradient.Evaluate(0.25f));
                EditorGUI.DrawRect(new Rect(picture.x, picture.y + half, picture.width, half),
                                   gradient.Evaluate(0.75f));
                EditorStyles.boldLabel.Draw(text, Styles.Look, false, false, false, false);
                EditorStyles.miniLabel.Draw(sub, Styles.LookSubtitle, false, false, false, false);
            }

            if (Event.current.type != EventType.MouseDown || !row.Contains(Event.current.mousePosition))
                return;

            m_Selected = LookRow;
            m_Renaming = -2;
            GUI.FocusControl(null);
            Event.current.Use();
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
            var layers = m_Stack.layers;

            m_Selected = m_Selected == LookRow ? layers.Count : Mathf.Clamp(m_Selected + 1, 0, layers.Count);
            layers.Insert(m_Selected, StyleRippleLayer.Create(kind));
            m_Dirty = true;
            undo.Commit("Add Texture Studio Liquid Layer");
            host?.Repaint();
        }

        void Swap(List<StyleRippleLayer> layers, int a, int b)
        {
            (layers[a], layers[b]) = (layers[b], layers[a]);
            m_Selected = b;
        }

        void DrawStackRow(List<StyleRippleLayer> layers, int index)
        {
            var layer = layers[index];
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

                GUI.DrawTexture(picture, thumbnail != null ? thumbnail : Texture2D.blackTexture,
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
            m_Renaming = Event.current.clickCount == 2 ? index : -2;
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
                m_Renaming = -2;
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
                m_Renaming = -2;
            }
        }

        static string Subtitle(StyleRippleLayer layer)
        {
            var uses = new List<string>();

            foreach (var channel in s_Channels)
            {
                if (layer.Target(channel).on)
                    uses.Add(channel.ToString());
            }

            if (layer.height && layer.strength > 0f)
                uses.Add("Height");

            var kind = StyleRippleLayer.LabelOf(layer.kind);
            return uses.Count == 0 ? $"{kind} · Unused" : $"{kind} · {string.Join(", ", uses)}";
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
                var picked = m_Map == s_Maps[i] && m_Flat;
                var style = i == 0 ? EditorStyles.miniButtonLeft :
                    i == count - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;

                if (GUI.Toggle(button, picked, Styles.MapKeys[i], style) && !picked)
                {
                    m_Map = s_Maps[i];
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
            if (m_Selected != LookRow)
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
                AttachMaps();
                m_Dirty = false;
            }

            var changed = GUI.changed;
            GUI.changed = false;

            var picked = m_Preset;
            var category = m_Category;

            rect = shelf.Draw(rect, snapshot, ref picked, ref category, LoadPreset);
            m_Preset = picked;
            m_Category = category;

            if (shelf.busy)
                host?.Repaint();

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
                {
                    generator.Paint(layer);
                    generator.Show(m_Map);
                }

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
            var folder = StyleStudioExport.folder;
            var path = $"{folder}/{StyleStudioExport.FileName(name, LayerPrefix)}.asset";
            var target = AssetDatabase.LoadAssetAtPath<HBStyleLiquidLayer>(path);
            var created = target == null;

            Directory.CreateDirectory(Path.GetFullPath(folder));

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

            saved.FindProperty(NormalMapField).objectReferenceValue = null;
            saved.FindProperty(MaskMapField).objectReferenceValue = null;
            saved.ApplyModifiedPropertiesWithoutUndo();

            if (created)
                AssetDatabase.CreateAsset(target, path);

            var normal = Embed(target, path, generator.normal, target.name + "_Normal");
            var mask = Embed(target, path, generator.mask, target.name + "_Mask");

            saved.Update();
            saved.FindProperty(NormalMapField).objectReferenceValue = normal;
            saved.FindProperty(MaskMapField).objectReferenceValue = mask;
            saved.ApplyModifiedPropertiesWithoutUndo();
            target.Touch();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
            EditorGUIUtility.PingObject(target);
            GUIUtility.ExitGUI();
        }

        static Texture2D Embed(HBStyleLiquidLayer owner, string path, RenderTexture source, string name)
        {
            var texture = StyleLiquidGenerator.Capture(source, name);

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is Texture2D old) || old.name != name)
                    continue;

                EditorUtility.CopySerialized(texture, old);
                old.name = name;
                UnityEngine.Object.DestroyImmediate(texture);
                EditorUtility.SetDirty(old);
                return old;
            }

            AssetDatabase.AddObjectToAsset(texture, owner);
            return texture;
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
            m_Shelf?.Release();
            m_Pool = null;
            m_Generator = null;
            m_Preview = null;
            m_Shelf = null;

            if (m_Undo != null)
                m_Undo.Release();

            m_Undo = null;
        }
    }
}
