using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleMaterialPage : StyleStudioPage
    {
        static readonly int[] s_Resolutions = { 512, 1024, 2048 };
        const string RenameControl = "TextureStudio.Material.Rename";

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Material",
                "Stone, brick, plank and ground textures for terrain layers and Lit materials.");

            internal static readonly GUIContent Base = new GUIContent("Base",
                "Colour and fine relief of the whole layer, under the pattern and the modules.");
            internal static readonly GUIContent Layout = new GUIContent("Layout");
            internal static readonly GUIContent PatternGroup = new GUIContent("Pattern");
            internal static readonly GUIContent NoPattern = new GUIContent("None",
                "No pattern: one flat surface for the modules to work on.");

            internal static readonly GUIContent Shape = new GUIContent("Shape");
            internal static readonly GUIContent Surface = new GUIContent("Surface");
            internal static readonly GUIContent Colour = new GUIContent("Colour");
            internal static readonly GUIContent Modules = new GUIContent("Modules",
                "Materials painted onto the picked layer, top first. The bottom one is applied first, so the top " +
                "one covers the others. Click one to edit it on the left.");
            internal static readonly GUIContent Path = new GUIContent(" › ");

            internal static readonly GUIContent NoModules = new GUIContent("No modules. Add one with +.");
            internal static readonly GUIContent AddModule = new GUIContent("+",
                "Adds a white Fill module on top; give it shapes and masks to place it.");
            internal static readonly GUIContent DuplicateModule = new GUIContent("Duplicate",
                "Copies the picked module above itself.");
            internal static readonly GUIContent RemoveModule = new GUIContent("−", "Removes the picked module.");
            internal static readonly GUIContent RaiseModule = new GUIContent("▲", "Moves the picked module up.");
            internal static readonly GUIContent LowerModule = new GUIContent("▼", "Moves the picked module down.");
            internal static readonly GUIContent ModuleOn = EditorGUIUtility.TrIconContent("animationvisibilitytoggleon",
                "On. Click to turn the module off.");
            internal static readonly GUIContent ModuleOff = EditorGUIUtility.TrIconContent(
                "animationvisibilitytoggleoff", "Off. Click to turn the module on.");

            internal static readonly GUIContent Light = new GUIContent("Painted Light");
            internal static readonly GUIContent Output = new GUIContent("Output");
            internal static readonly GUIContent Export = new GUIContent("Export",
                "Name, size and folder of the saved textures, and the Save button.");

            internal static readonly GUIContent Stones = new GUIContent("Stones",
                "Stones across the texture. Fewer stones are bigger.");
            internal static readonly GUIContent Jitter = new GUIContent("Jitter",
                "How irregular the layout is. 0 lines the stones up in a grid, 1 scatters them freely.");
            internal static readonly GUIContent Rows = new GUIContent("Rows",
                "Rows of pavers across the texture. Always even so the texture repeats without a seam.");
            internal static readonly GUIContent PerRow = new GUIContent("Per Row", "Pavers in each row.");
            internal static readonly GUIContent RowOffset = new GUIContent("Row Offset",
                "How far every second row is shifted, as a share of a paver. 0.5 is the classic running bond.");
            internal static readonly GUIContent Gap = new GUIContent("Gap",
                "Width of the joints between stones, as a share of a stone's size.");
            internal static readonly GUIContent Roundness = new GUIContent("Roundness",
                "Rounds off the corners of the stones' outline.");
            internal static readonly GUIContent Chipping = new GUIContent("Chipping",
                "Makes the outlines wobbly and chipped instead of straight.");
            internal static readonly GUIContent Seed = new GUIContent("Seed",
                "Picks another random layout for the pattern and its colour variation.");

            internal static readonly GUIContent Bevel = new GUIContent("Bevel",
                "How far in from the edge each stone keeps rounding down into the joint.");
            internal static readonly GUIContent Dome = new GUIContent("Dome",
                "How much the stone tops bulge. 0 gives flat slabs, below 0 curls the edges up like drying mud.");
            internal static readonly GUIContent Facets = new GUIContent("Facets",
                "Cuts the stones into flat chiselled faces. 0 keeps them smooth and round.");
            internal static readonly GUIContent HeightVariation = new GUIContent("Height Variation",
                "How much lower some stones sit than others.");
            internal static readonly GUIContent Tilt = new GUIContent("Tilt",
                "How much each stone leans to a random side.");
            internal static readonly GUIContent Roughness = new GUIContent("Roughness",
                "Small bumps and pits on the stone surface.");

            internal static readonly GUIContent Depth = new GUIContent("Depth",
                "Height of the relief as a share of the texture's width. Drives the normal map strength, the " +
                "edges and the occlusion.");
            internal static readonly GUIContent Smoothness = new GUIContent("Smoothness",
                "Smoothness of the pieces, saved in the mask's alpha. Modules set their own.");
            internal static readonly GUIContent GapSmoothness = new GUIContent("Gap Smoothness",
                "Smoothness of the joints.");
            internal static readonly GUIContent Metallic = new GUIContent("Metallic",
                "Metallic of the stones, saved in the mask's red channel.");

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
                "File name start, saved as TEX_Words_Like_This. Saving writes TEX_Name_Albedo, TEX_Name_Normal, " +
                "TEX_Name_Mask and, when something glows, TEX_Name_Emission.");
            internal static readonly GUIContent Save = new GUIContent("Save Textures",
                "Writes the albedo, normal, mask and emission PNGs into the folder. Files with the same names are " +
                "replaced.");

            internal static readonly GUIContent Layers = new GUIContent("Layers",
                "The stack of materials, top first. Each layer covers the ones below it by its Coverage. " +
                "Double-click a layer to rename it.");
            internal static readonly GUIContent Blend = new GUIContent("Blend",
                "Normal lays the layer over the ones below by its Coverage. Height shows it only where its surface " +
                "is higher than the stack below, so sand fills between stones and stones poke out of sand.");
            internal static readonly GUIContent Contrast = new GUIContent("Contrast",
                "How sharp the edge between this layer and the ones below is. 0 blends softly, 1 cuts cleanly.");
            internal static readonly GUIContent Offset = new GUIContent("Offset",
                "Moves this layer's surface up or down before comparing, so more or less of it shows.");
            internal static readonly GUIContent Coverage = new GUIContent("Coverage",
                "How much the picked layer covers the layers below it. The bottom layer always covers fully.");
            internal static readonly GUIContent AddLayer = new GUIContent("+",
                "Adds an empty layer above the picked one.");
            internal static readonly GUIContent DuplicateLayer = new GUIContent("Duplicate",
                "Copies the picked layer above itself.");
            internal static readonly GUIContent RemoveLayer = new GUIContent("−", "Removes the picked layer.");
            internal static readonly GUIContent RaiseLayer = new GUIContent("▲", "Moves the picked layer up.");
            internal static readonly GUIContent LowerLayer = new GUIContent("▼", "Moves the picked layer down.");
            internal static readonly GUIContent Visible = EditorGUIUtility.TrIconContent("animationvisibilitytoggleon",
                "Shown. Click to hide the layer.");
            internal static readonly GUIContent Hidden = EditorGUIUtility.TrIconContent("animationvisibilitytoggleoff",
                "Hidden. Click to show the layer.");
            internal static readonly Color selection = new Color(0.24f, 0.48f, 0.9f, 0.35f);
            internal static readonly Color badge = new Color(0f, 0f, 0f, 0.55f);


            internal static readonly GUIContent Grain = new GUIContent("Grain",
                "Fine light and dark speckles over the stones and the joints.");

            internal static readonly GUIContent Variation = new GUIContent("Variation",
                "How far each piece, or each blotch when there is no pattern, moves along the colours.");

            internal static readonly GUIContent[] PatternNames =
            {
                new GUIContent("Cells", "Irregular pieces, as in cobblestones, flagstones, dried mud or ice."),
                new GUIContent("Rows", "Pieces in rows, as in bricks, pavers, cut blocks or boards."),
                new GUIContent("Herringbone", "Pairs of pieces in a zigzag, as in herringbone bricks or parquet."),
                new GUIContent("Basket", "Squares of pieces, turning lying and standing in turn."),
                new GUIContent("Scatter", "Loose pebbles lying on the ground, as in soil, gravel, sand or snow."),
                new GUIContent("None", "No pattern: one flat surface for the modules to work on.")
            };

            internal static readonly GUIContent FillName = new GUIContent("Fill",
                "Covers the whole area the Where settings pick. Add masks for edge wear, dirt in the creases, " +
                "noise patches of moss or damp, or water with Flatten.");

            internal static readonly GUIContent[] KindNames =
            {
                new GUIContent("Shapes/Cracks", "Thin branching cracks."),
                new GUIContent("Shapes/Scratches", "Fine straight scratches and scuff marks."),
                new GUIContent("Shapes/Streaks", "Stains running down the surface, as on old walls."),
                new GUIContent("Shapes/Wood Grain", "Growth rings, fibres and knots following the boards."),
                new GUIContent("Shapes/Chips", "Bits broken off the edges and corners of the pieces."),
                new GUIContent("Shapes/Clumps", "Pebbles or grass tufts. Masks after it keep or drop each clump " +
                                                "whole. Blades turns round pebbles into tufts."),
                new GUIContent("Shapes/Leaves", "Fallen leaves and twigs. Masks after it keep or drop each leaf " +
                                                "whole."),
                new GUIContent("Masks/Edges", "Raised edges of the relief."),
                new GUIContent("Masks/Cavities", "Creases and low corners of the relief."),
                new GUIContent("Masks/Height", "High parts of the relief."),
                new GUIContent("Masks/Noise", "Soft noise blotches."),
                new GUIContent("Masks/Random Pieces", "Some of the pattern's pieces, picked at random.")
            };

            internal static readonly GUIContent[] AmountNames =
            {
                new GUIContent("Amount", "How much of the surface is cracked."),
                new GUIContent("Density", "How many scratches there are."),
                new GUIContent("Coverage", "How much of the surface is stained."),
                new GUIContent("Rings", "Strength of the growth ring lines."),
                new GUIContent("Amount", "Share of the places near edges that get a chip."),
                new GUIContent("Density", "Share of the places that get a clump. With Size 1 it covers everything."),
                new GUIContent("Amount", "How much of the surface the leaves cover.")
            };

            internal static readonly GUIContent[] ScaleNames =
            {
                new GUIContent("Scale", "Cracks across the texture. More gives smaller, closer cracks."),
                new GUIContent("Scale", "Places across the texture where a scratch can start."),
                new GUIContent("Scale", "Streaks across the texture. More gives thinner, closer streaks."),
                new GUIContent("Ring Density", "Growth ring lines across the width of a board."),
                new GUIContent("Scale", "Places across the texture where a chip can be. More gives more, smaller " +
                                        "chances."),
                new GUIContent("Scale", "Places across the texture where a clump can be. More gives smaller clumps."),
                new GUIContent("Scale", "Leaves across the texture. More gives smaller leaves.")
            };

            internal static readonly GUIContent[] ExtraNames =
            {
                GUIContent.none, GUIContent.none, GUIContent.none,
                new GUIContent("Knots", "Chance of a knot on each board; high values give some boards two."),
                GUIContent.none,
                new GUIContent("Blades", "0 gives round pebbles, 1 gives grass tufts, between gives spiky clumps."),
                new GUIContent("Twigs", "Share of twigs among the leaves.")
            };

            internal static readonly GUIContent[] SizeNames =
            {
                new GUIContent("Width", "Width of the cracks."),
                new GUIContent("Length", "Length of the scratches."),
                new GUIContent("Length", "How long the streaks run."),
                new GUIContent("Fibres", "Strength of the fine fibre streaks."),
                new GUIContent("Size", "Size of the chips compared with the pattern's pieces."),
                new GUIContent("Size", "Size of the clumps. At 1 they touch and cover the ground."),
                new GUIContent("Size", "Size of the leaves.")
            };
            internal static readonly GUIContent ModuleColour = new GUIContent("Colours",
                "Colours of the module, up to 4 stops. Variation spreads the module across them. " +
                "Alpha is how strongly the colour covers what is below.");
            internal static readonly GUIContent ModuleVariation = new GUIContent("Variation",
                "How far the module spreads across its colours.");
            internal static readonly GUIContent ModuleSeed = new GUIContent("Seed", "Picks another random layout.");
            internal static readonly GUIContent ToneSeed = new GUIContent("Seed",
                "Picks another layout for the colour blotches where no shape sets them.");
            internal static readonly GUIContent MasksLabel = new GUIContent("Shape and Masks",
                "Shapes and masks run from top to bottom. The first sets where the module goes; each one after " +
                "it multiplies, adds to or takes from the result.");
            internal static readonly GUIContent HeightLabel = new GUIContent("Height");
            internal static readonly GUIContent HeightMode = new GUIContent("Mode",
                "Cut digs the shape into the surface, Raise lifts it, Flatten fills its low parts up to a level, " +
                "None changes only colour and surface.");
            internal static readonly GUIContent ModuleDepth = new GUIContent("Depth",
                "How deep the shape is cut or how high it is raised.");
            internal static readonly GUIContent ModuleLevel = new GUIContent("Level",
                "Height the covered low parts are filled up to; only the filled parts take the module's colour. " +
                "Pieces' tops are near 1, joints near 0 and an empty layer's surface near 0.5.");
            internal static readonly GUIContent ModuleSmoothness = new GUIContent("Smoothness",
                "Smoothness where the module covers.");
            internal static readonly GUIContent ModuleMetallic = new GUIContent("Metallic",
                "Metallic where the module covers.");
            internal static readonly GUIContent ModuleEmission = new GUIContent("Emission",
                "Glow where the module covers, saved as the Emission map. Black is off; the material's Emission " +
                "Color sets how bright it shines.");
            internal static readonly GUIContent WhereLabel = new GUIContent("Where");
            internal static readonly GUIContent Area = new GUIContent("Area",
                "Pieces keeps the module on the pattern's pieces, Joints between them, Everywhere on both.");
            internal static readonly GUIContent AddMask = new GUIContent("Add Shape or Mask",
                "Up to four; each one after the first multiplies, adds to or takes from the ones above it.");
            internal static readonly GUIContent RemoveMask = new GUIContent("−", "Removes this shape or mask.");
            internal static readonly GUIContent Warp = new GUIContent("Warp",
                "Bends the mask's edge with noise so straight, even edges turn ragged and natural.");
            internal static readonly GUIContent Threshold = new GUIContent("Threshold",
                "Where the mask starts. Higher keeps only the strongest edges, creases, the highest parts or fewer " +
                "random pieces.");
            internal static readonly GUIContent Softness = new GUIContent("Softness", "How soft the mask's edge is.");
            internal static readonly GUIContent Invert = new GUIContent("Invert", "Swaps the masked and open parts.");
            internal static readonly GUIContent MaskScale = new GUIContent("Noise Scale",
                "Noise blotches across the texture. More gives smaller blotches.");

            internal static readonly GUIContent PieceColour = new GUIContent("Colours",
                "Colours of the pieces, or of the whole surface when there is no pattern, up to 4 stops.");
            internal static readonly GUIContent JointColour = new GUIContent("Joint Colour",
                "Colour of the ground in the joints and around the pieces.");
            internal static readonly GUIContent RandomShift = new GUIContent("Random Joints",
                "Puts the joints of each row at random places instead of a fixed offset.");
            internal static readonly GUIContent NaturalGround = new GUIContent("Natural",
                "Softer, flowing ground shapes and fine noise, as in mud, sand or ice.");

            internal static readonly GUIContent Ground = new GUIContent("Ground");
            internal static readonly GUIContent Pebbles = new GUIContent("Pebbles",
                "Places across the texture where a pebble can lie. More gives smaller pebbles.");
            internal static readonly GUIContent Density = new GUIContent("Density",
                "How much of the ground the pebbles cover. 0 leaves bare ground, 1 fills every gap with " +
                "smaller pebbles.");
            internal static readonly GUIContent PebbleSize = new GUIContent("Size",
                "Size of the pebbles. Above about 0.5 they touch and lie over each other.");
            internal static readonly GUIContent FineCracks = new GUIContent("Fine Cracks",
                "Smaller cracks splitting some of the pieces. 0 keeps only the main joints.");
            internal static readonly GUIContent Elongation = new GUIContent("Elongation",
                "How long and flat some pebbles are instead of round.");
            internal static readonly GUIContent GroundRelief = new GUIContent("Relief",
                "Height of the ground's bumps and ripples compared with the pebbles.");
            internal static readonly GUIContent Lumps = new GUIContent("Lumps",
                "Bumps across the texture. More gives smaller bumps.");
            internal static readonly GUIContent Ripples = new GUIContent("Ripples",
                "Wind ripples over the ground. 0 keeps only the bumps.");
            internal static readonly GUIContent RippleCount = new GUIContent("Ripple Count",
                "Ripples across the texture.");
            internal static readonly GUIContent RippleWarp = new GUIContent("Ripple Warp",
                "How much the ripples bend and wander instead of running straight.");
            internal static readonly GUIContent HeightTint = new GUIContent("Height Tint",
                "High parts move toward the right end of the colours, low parts toward the left.");

            internal static readonly GUIContent Repeats = new GUIContent("Repeats",
                "How many times the pattern repeats across the texture.");
            internal static readonly GUIContent Squares = new GUIContent("Squares",
                "Squares across the texture. Always even so the texture repeats without a seam.");
            internal static readonly GUIContent BrickLength = new GUIContent("Length",
                "Length of a piece as a multiple of its width.");
            internal static readonly GUIContent LengthVariation = new GUIContent("Length Variation",
                "How much the lengths of the pieces in a row differ.");

            internal static readonly GUIContent[] Resolutions =
            {
                new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048")
            };

            internal static readonly GUIContent[] MapKeys =
            {
                new GUIContent("A", "Albedo: colour with the painted light."),
                new GUIContent("N", "Normal map as saved."),
                new GUIContent("M", "Mask: metallic, occlusion and height as red, green and blue."),
                new GUIContent("H", "Height: relief, white is high."),
                new GUIContent("O", "Occlusion: soft shadow of the relief."),
                new GUIContent("E", "Edges: raised edges light, creases dark."),
                new GUIContent("Em", "Emission: glow of the modules that have an Emission colour.")
            };

            internal static readonly GUIContent[] MapNames =
            {
                new GUIContent("Albedo", "Colour with the painted light."),
                new GUIContent("Normal", "Normal map as saved."),
                new GUIContent("Mask", "Metallic, occlusion and height as red, green and blue."),
                new GUIContent("Height", "Relief, white is high."),
                new GUIContent("Occlusion", "Soft shadow of the relief."),
                new GUIContent("Edges", "Raised edges light, creases dark."),
                new GUIContent("Emission", "Glow of the modules that have an Emission colour.")
            };
        }

        [SerializeField]
        StyleMaterialStack m_Stack = new StyleMaterialStack();

        [SerializeField]
        int m_Selected;

        [SerializeField]
        int m_Module = -1;

        [SerializeField]
        StyleMaterialMap m_Map;

        [SerializeField]
        bool m_EditModule;

        [SerializeField]
        string m_Preset;

        [SerializeField]
        string m_Category;

        [NonSerialized]
        StyleMaterialShelf m_Shelf;

        [NonSerialized]
        int m_Renaming = -1;

        [NonSerialized]
        bool m_Focus;

        [NonSerialized]
        StyleMaterialGenerator m_Generator;

        [NonSerialized]
        StyleStudioPreview m_Preview;

        [NonSerialized]
        bool m_Dirty = true;

        [NonSerialized]
        StyleStudioUndoState m_Undo;

        internal override GUIContent title => Styles.Title;

        StyleMaterialGenerator generator => m_Generator ??= new StyleMaterialGenerator();
        StyleStudioPreview preview => m_Preview ??= new StyleStudioPreview();
        StyleMaterialShelf shelf => m_Shelf ??= new StyleMaterialShelf();

        StyleStudioUndoState undo =>
            m_Undo != null ? m_Undo : m_Undo = StyleStudioUndoState.Create(m_Stack, Restored);

        StyleMaterialSettings current => m_Stack.Layer(m_Selected);

        internal override void DrawSettings()
        {
            var history = undo;

            var settings = current;

            EditorGUI.BeginChangeCheck();

            var module = PickedModule(settings);
            var editModule = m_EditModule && module >= 0;

            DrawPath(settings, editModule ? settings.modules[module] : null);

            if (editModule)
            {
                DrawModuleCard(settings, module);
                CoreEditorUtils.DrawSplitter();

                if (EditorGUI.EndChangeCheck())
                    m_Dirty = true;

                history.Commit("Change Texture Studio");
                return;
            }

            DrawPatternField(settings);
            EditorGUILayout.Space();

            if (settings.hasPattern)
                DrawPatternCard(settings);
            else
                DrawGroundCard(settings);

            var open = StyleSectionHeader.Draw(Styles.Base, "TextureStudio.Material.Base",
                                               () => Reset(settings.ResetBase));
            if (open)
            {
                DrawRamp(Styles.PieceColour, settings.colours);
                settings.variation = EditorGUILayout.Slider(Styles.Variation, settings.variation, 0f, 1f);
                settings.heightTint = EditorGUILayout.Slider(Styles.HeightTint, settings.heightTint, 0f, 1f);
                settings.grain = EditorGUILayout.Slider(Styles.Grain, settings.grain, 0f, 0.5f);
                settings.roughness = EditorGUILayout.Slider(Styles.Roughness, settings.roughness, 0f, 1f);
                settings.seed = EditorGUILayout.IntSlider(Styles.Seed, settings.seed, 0, 999);
            }

            StyleSectionHeader.End(open);

            open = StyleSectionHeader.Draw(Styles.Surface, "TextureStudio.Material.Surface",
                                           () => Reset(settings.ResetSurface));
            if (open)
            {
                settings.depth = EditorGUILayout.Slider(Styles.Depth, settings.depth, 0.001f, 0.1f);
                settings.smoothness = EditorGUILayout.Slider(Styles.Smoothness, settings.smoothness, 0f, 1f);
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
            CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio");
        }

        void DrawPath(StyleMaterialSettings settings, StyleMaterialModule module)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var layer = new GUIContent(settings.label, "Shows the layer's settings.");

                if (module == null)
                {
                    GUILayout.Label(layer, EditorStyles.boldLabel);
                }
                else
                {
                    if (GUILayout.Button(layer, EditorStyles.linkLabel, GUILayout.ExpandWidth(false)))
                    {
                        m_EditModule = false;
                        GUI.FocusControl(null);
                    }

                    GUILayout.Label(Styles.Path, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                    GUILayout.Label(ModuleTitle(module), EditorStyles.boldLabel);
                }
            }

            EditorGUILayout.Space(2f);
        }

        void DrawPatternCard(StyleMaterialSettings settings)
        {
            var pattern = settings.pattern;
            var title = new GUIContent(StyleMaterialSettings.LabelOf(pattern),
                                       Styles.PatternNames[(int)pattern].tooltip);
            var open = StyleSectionHeader.Draw(title, "TextureStudio.Material.Pattern",
                                               () => Reset(() => settings.SetPattern(pattern)));

            if (open)
            {
                EditorGUILayout.LabelField(Styles.Layout, EditorStyles.miniBoldLabel);
                DrawPattern(settings);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.Shape, EditorStyles.miniBoldLabel);
                settings.bevel = EditorGUILayout.Slider(Styles.Bevel, settings.bevel, 0.01f, 0.6f);
                settings.dome = EditorGUILayout.Slider(Styles.Dome, settings.dome, -1f, 1f);
                settings.facets = EditorGUILayout.Slider(Styles.Facets, settings.facets, 0f, 1f);
                settings.heightVariation = EditorGUILayout.Slider(Styles.HeightVariation, settings.heightVariation,
                                                                  0f, 1f);
                settings.tilt = EditorGUILayout.Slider(Styles.Tilt, settings.tilt, 0f, 1f);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.Ground, EditorStyles.miniBoldLabel);
                DrawGround(settings);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.Colour, EditorStyles.miniBoldLabel);
                settings.gapColour = EditorGUILayout.ColorField(Styles.JointColour,
                                                                settings.gapColour, true, false, false);
                settings.gapSmoothness = EditorGUILayout.Slider(Styles.GapSmoothness, settings.gapSmoothness, 0f,
                                                                1f);
            }

            StyleSectionHeader.End(open);
        }

        void DrawGroundCard(StyleMaterialSettings settings)
        {
            var open = StyleSectionHeader.Draw(Styles.Ground, "TextureStudio.Material.Ground",
                                               () => Reset(settings.ResetGround));

            if (open)
                DrawGround(settings);

            StyleSectionHeader.End(open);
        }

        int PickedModule(StyleMaterialSettings settings)
        {
            var count = settings.modules.Count;

            if (m_Module < 0 || m_Module >= count)
                m_Module = count - 1;

            return m_Module;
        }

        void DrawPatternField(StyleMaterialSettings settings)
        {
            var picked = Styles.PatternNames[(int)settings.pattern];
            var field = EditorGUILayout.GetControlRect();
            var button = EditorGUI.PrefixLabel(field, Styles.PatternGroup);

            if (GUI.Button(button, picked, EditorStyles.popup))
                ShowPatternMenu(settings, button);
        }

        void DrawModuleCard(StyleMaterialSettings settings, int index)
        {
            var module = settings.modules[index];
            var toggle = new StyleHeaderToggle { visible = true, on = module.on };
            var open = StyleSectionHeader.DrawWithMenu(new GUIContent(ModuleName(module)),
                                                       "TextureStudio.Material.Module",
                                                       menu => FillModuleMenu(menu, settings, index), toggle,
                                                       out var toggled);

            if (toggled.HasValue)
                module.on = toggled.Value;

            if (open)
                DrawModule(module);

            StyleSectionHeader.End(open);
        }

        void FillModuleMenu(GenericMenu menu, StyleMaterialSettings settings, int index)
        {
            var modules = settings.modules;
            var module = modules[index];

            menu.AddItem(new GUIContent("Reset"), false, () => Reset(module.Reset));
            menu.AddItem(new GUIContent("Duplicate"), false, () => Reset(() => DuplicateModule(modules, index)));
            menu.AddSeparator(string.Empty);

            if (index < modules.Count - 1)
                menu.AddItem(new GUIContent("Move Up"), false, () => Reset(() => MoveModule(modules, index, 1)));
            else
                menu.AddDisabledItem(new GUIContent("Move Up"));

            if (index > 0)
                menu.AddItem(new GUIContent("Move Down"), false, () => Reset(() => MoveModule(modules, index, -1)));
            else
                menu.AddDisabledItem(new GUIContent("Move Down"));

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Remove"), false, () => Reset(() => RemoveModule(modules, index)));
        }

        void DuplicateModule(List<StyleMaterialModule> modules, int index)
        {
            modules.Insert(index + 1, modules[index].Clone());
            m_Module = index + 1;
        }

        void MoveModule(List<StyleMaterialModule> modules, int index, int step)
        {
            (modules[index], modules[index + step]) = (modules[index + step], modules[index]);
            m_Module = index + step;
        }

        void RemoveModule(List<StyleMaterialModule> modules, int index)
        {
            modules.RemoveAt(index);
            m_Module = Mathf.Min(index, modules.Count - 1);
            m_EditModule &= modules.Count > 0;
        }

        static void DrawRamp(GUIContent label, StyleColourRamp ramp)
        {
            EditorGUI.BeginChangeCheck();
            var gradient = EditorGUILayout.GradientField(label, ramp.ToGradient());

            if (EditorGUI.EndChangeCheck())
                ramp.Set(gradient);
        }

        void DrawModule(StyleMaterialModule module)
        {
            DrawRamp(Styles.ModuleColour, module.colours);
            module.variation = EditorGUILayout.Slider(Styles.ModuleVariation, module.variation, 0f, 1f);

            if (!module.shape.HasValue)
                module.seed = EditorGUILayout.IntSlider(Styles.ToneSeed, module.seed, 0, 999);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.HeightLabel, EditorStyles.miniBoldLabel);
            module.height = (StyleModuleHeight)EditorGUILayout.EnumPopup(Styles.HeightMode, module.height);

            if (module.height == StyleModuleHeight.Flatten)
                module.depth = EditorGUILayout.Slider(Styles.ModuleLevel, module.depth, 0f, 1.2f);
            else if (module.height != StyleModuleHeight.None)
                module.depth = EditorGUILayout.Slider(Styles.ModuleDepth, module.depth, 0f, 0.5f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.Surface, EditorStyles.miniBoldLabel);
            module.smoothness = EditorGUILayout.Slider(Styles.ModuleSmoothness, module.smoothness, 0f, 1f);
            module.metallic = EditorGUILayout.Slider(Styles.ModuleMetallic, module.metallic, 0f, 1f);
            module.emission = EditorGUILayout.ColorField(Styles.ModuleEmission, module.emission, true, false, false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.WhereLabel, EditorStyles.miniBoldLabel);
            module.area = (StyleModuleArea)EditorGUILayout.EnumPopup(Styles.Area, module.area);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.MasksLabel, EditorStyles.miniBoldLabel);

            var masks = module.masks;
            var removed = -1;

            for (var i = 0; i < masks.Count; i++)
            {
                if (DrawMask(masks[i], i))
                    removed = i;
            }

            if (removed >= 0)
            {
                masks.RemoveAt(removed);
                GUI.changed = true;
            }

            using (new EditorGUI.DisabledScope(masks.Count >= StyleMaterialModule.MaxMasks))
            {
                if (GUILayout.Button(Styles.AddMask, EditorStyles.miniButton))
                    masks.Add(StyleModuleMask.Create(StyleMaskKind.Noise, masks.Count + 1));
            }
        }

        bool DrawMask(StyleModuleMask mask, int index)
        {
            var remove = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (index > 0)
                        mask.blend = (StyleMaskBlend)EditorGUILayout.EnumPopup(mask.blend, GUILayout.Width(72f));

                    var name = new GUIContent(KindName(mask.kind), Styles.KindNames[(int)mask.kind].tooltip);
                    var button = GUILayoutUtility.GetRect(name, EditorStyles.popup);

                    if (GUI.Button(button, name, EditorStyles.popup))
                        ShowKindMenu(mask, button);

                    remove = GUILayout.Button(Styles.RemoveMask, EditorStyles.miniButton, GUILayout.Width(22f));
                }

                if (mask.isShape)
                {
                    var kind = (int)mask.kind;

                    mask.amount = EditorGUILayout.Slider(Styles.AmountNames[kind], mask.amount, 0f, 1f);
                    mask.scale = EditorGUILayout.IntSlider(Styles.ScaleNames[kind], mask.scale, 1, 128);
                    mask.size = EditorGUILayout.Slider(Styles.SizeNames[kind], mask.size, 0f, 1f);

                    if (Styles.ExtraNames[kind] != GUIContent.none)
                        mask.extra = EditorGUILayout.Slider(Styles.ExtraNames[kind], mask.extra, 0f, 1f);
                }
                else
                {
                    mask.threshold = EditorGUILayout.Slider(Styles.Threshold, mask.threshold, 0f, 1f);
                    mask.softness = EditorGUILayout.Slider(Styles.Softness, mask.softness, 0f, 1f);

                    if (mask.kind == StyleMaskKind.Noise)
                        mask.scale = EditorGUILayout.IntSlider(Styles.MaskScale, mask.scale, 1, 32);
                }

                mask.invert = EditorGUILayout.Toggle(Styles.Invert, mask.invert);
                mask.warp = EditorGUILayout.Slider(Styles.Warp, mask.warp, 0f, 1f);

                if (mask.isShape || mask.kind == StyleMaskKind.Noise || mask.kind == StyleMaskKind.RandomPieces)
                    mask.seed = EditorGUILayout.IntSlider(Styles.ModuleSeed, mask.seed, 0, 999);
            }

            return remove;
        }

        void ShowKindMenu(StyleModuleMask mask, Rect button)
        {
            var menu = new GenericMenu();

            for (var i = 0; i < Styles.KindNames.Length; i++)
            {
                var kind = (StyleMaskKind)i;

                if (kind == StyleMaskKind.Edges)
                    menu.AddSeparator(string.Empty);

                menu.AddItem(Styles.KindNames[i], mask.kind == kind, () => Reset(() => mask.SetKind(kind)));
            }

            menu.DropDown(button);
        }

        static string KindName(StyleMaskKind kind)
        {
            var text = Styles.KindNames[(int)kind].text;

            return text.Substring(text.IndexOf('/') + 1);
        }

        static string ModuleName(StyleMaterialModule module)
        {
            var shape = module.shape;

            return shape.HasValue ? KindName(shape.Value) : Styles.FillName.text;
        }
        void DrawModules(StyleMaterialSettings layer)
        {
            var modules = layer.modules;
            var picked = PickedModule(layer);

            EditorGUILayout.LabelField(Styles.Modules, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (modules.Count == 0)
                    EditorGUILayout.LabelField(Styles.NoModules, EditorStyles.centeredGreyMiniLabel);

                for (var i = modules.Count - 1; i >= 0; i--)
                    DrawModuleRow(modules[i], i);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var add = GUILayoutUtility.GetRect(Styles.AddModule, EditorStyles.miniButtonLeft);

                if (GUI.Button(add, Styles.AddModule, EditorStyles.miniButtonLeft))
                    AddModule(modules);

                using (new EditorGUI.DisabledScope(picked < 0))
                {
                    if (GUILayout.Button(Styles.DuplicateModule, EditorStyles.miniButtonMid))
                        DuplicateModule(modules, picked);

                    if (GUILayout.Button(Styles.RemoveModule, EditorStyles.miniButtonMid))
                        RemoveModule(modules, picked);
                }

                using (new EditorGUI.DisabledScope(picked < 0 || picked >= modules.Count - 1))
                {
                    if (GUILayout.Button(Styles.RaiseModule, EditorStyles.miniButtonMid))
                        MoveModule(modules, picked, 1);
                }

                using (new EditorGUI.DisabledScope(picked <= 0))
                {
                    if (GUILayout.Button(Styles.LowerModule, EditorStyles.miniButtonRight))
                        MoveModule(modules, picked, -1);
                }
            }
        }

        void DrawModuleRow(StyleMaterialModule module, int index)
        {
            var row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            var eye = new Rect(row.x + 4f, row.y + 3f, 16f, 16f);
            var swatch = new Rect(eye.xMax + 6f, row.y + 4f, 14f, 14f);
            var text = new Rect(swatch.xMax + 6f, row.y + 3f, row.xMax - swatch.xMax - 10f, 16f);

            if (Event.current.type == EventType.Repaint)
            {
                if (m_EditModule && index == m_Module)
                    EditorGUI.DrawRect(row, Styles.selection);

                var faded = GUI.color;

                if (!module.on)
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);

                var colour = module.colours.Evaluate(0.5f);
                colour.a = 1f;
                EditorGUI.DrawRect(swatch, colour);
                EditorStyles.label.Draw(text, ModuleTitle(module), false, false, false, false);
                GUI.color = faded;
            }

            module.on = GUI.Toggle(eye, module.on, module.on ? Styles.ModuleOn : Styles.ModuleOff, GUIStyle.none);

            var mouse = Event.current.mousePosition;

            if (Event.current.type != EventType.MouseDown || !row.Contains(mouse) || eye.Contains(mouse))
                return;

            m_Module = index;
            m_EditModule = true;
            GUI.FocusControl(null);
            Event.current.Use();
        }

        static string ModuleTitle(StyleMaterialModule module)
        {
            var title = module.masks.Count > 0 && module.masks[0].isShape ? string.Empty : Styles.FillName.text;

            for (var i = 0; i < module.masks.Count; i++)
            {
                var mask = module.masks[i];
                var join = title.Length == 0 ? string.Empty : i == 0 ? " · " :
                    mask.blend == StyleMaskBlend.Add ? " + " :
                    mask.blend == StyleMaskBlend.Subtract ? " − " : " × ";

                title += join + (mask.invert ? "Not " : string.Empty) + KindName(mask.kind);
            }

            if (module.area != StyleModuleArea.Everywhere)
                title += " · " + module.area;

            return title;
        }

        void AddModule(List<StyleMaterialModule> modules)
        {
            Reset(() =>
            {
                modules.Add(new StyleMaterialModule());
                m_Module = modules.Count - 1;
                m_EditModule = true;
            });
        }
        void ShowPatternMenu(StyleMaterialSettings settings, Rect button)
        {
            var menu = new GenericMenu();

            menu.AddItem(Styles.NoPattern, !settings.hasPattern,
                         () => Reset(() => settings.SetPattern(StyleMaterialPattern.None)));
            menu.AddSeparator(string.Empty);

            for (var i = 0; i < Styles.PatternNames.Length; i++)
            {
                var pattern = (StyleMaterialPattern)i;

                if (pattern == StyleMaterialPattern.None)
                    continue;

                menu.AddItem(Styles.PatternNames[i], settings.pattern == pattern,
                             () => Reset(() => settings.SetPattern(pattern)));
            }

            menu.DropDown(button);
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

            var index = Mathf.Max(Array.IndexOf(s_Resolutions, m_Stack.resolution), 0);
            index = EditorGUILayout.Popup(Styles.Resolution, index, Styles.Resolutions);
            m_Stack.resolution = s_Resolutions[index];

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            m_Stack.name = EditorGUILayout.TextField(Styles.Name, m_Stack.name);
            StyleStudioExport.DrawFolder();
            undo.Commit("Change Texture Studio Output");
            host?.Repaint();
        }

        sealed class ExportPopup : PopupWindowContent
        {
            readonly StyleMaterialPage m_Page;

            internal ExportPopup(StyleMaterialPage page)
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
                        m_Page.SaveTextures();
                    }
                }
            }
        }

        static void DrawPattern(StyleMaterialSettings settings)
        {
            switch (settings.pattern)
            {
                case StyleMaterialPattern.Rows:
                    settings.rows = EditorGUILayout.IntSlider(Styles.Rows, settings.rows, 2, 32) / 2 * 2;
                    settings.perRow = EditorGUILayout.IntSlider(Styles.PerRow, settings.perRow, 1, 16);
                    settings.randomShift = EditorGUILayout.Toggle(Styles.RandomShift, settings.randomShift);

                    if (!settings.randomShift)
                        settings.rowOffset = EditorGUILayout.Slider(Styles.RowOffset, settings.rowOffset, 0f, 1f);

                    settings.lengthVariation = EditorGUILayout.Slider(Styles.LengthVariation,
                                                                      settings.lengthVariation, 0f, 1f);
                    break;
                case StyleMaterialPattern.Herringbone:
                    settings.repeats = EditorGUILayout.IntSlider(Styles.Repeats, settings.repeats, 1, 8);
                    settings.brickLength = EditorGUILayout.IntSlider(Styles.BrickLength, settings.brickLength, 2, 6);
                    break;
                case StyleMaterialPattern.Basket:
                    settings.repeats = EditorGUILayout.IntSlider(Styles.Squares, settings.repeats, 2, 16) / 2 * 2;
                    settings.brickLength = EditorGUILayout.IntSlider(Styles.BrickLength, settings.brickLength, 2, 4);
                    break;
                default:
                    settings.stones = EditorGUILayout.IntSlider(Styles.Stones, settings.stones, 2, 24);
                    settings.jitter = EditorGUILayout.Slider(Styles.Jitter, settings.jitter, 0f, 1f);
                    settings.fineCracks = EditorGUILayout.Slider(Styles.FineCracks, settings.fineCracks, 0f, 1f);
                    break;
                case StyleMaterialPattern.Scatter:
                    settings.stones = EditorGUILayout.IntSlider(Styles.Pebbles, settings.stones, 2, 48);
                    settings.density = EditorGUILayout.Slider(Styles.Density, settings.density, 0f, 1f);
                    settings.pebbleSize = EditorGUILayout.Slider(Styles.PebbleSize, settings.pebbleSize, 0.1f, 0.9f);
                    settings.elongation = EditorGUILayout.Slider(Styles.Elongation, settings.elongation, 0f, 1.5f);
                    settings.jitter = EditorGUILayout.Slider(Styles.Jitter, settings.jitter, 0f, 1f);
                    break;
            }

            settings.gap = EditorGUILayout.Slider(Styles.Gap, settings.gap, 0f, 0.3f);
            settings.roundness = EditorGUILayout.Slider(Styles.Roundness, settings.roundness, 0f, 1f);
            settings.chipping = EditorGUILayout.Slider(Styles.Chipping, settings.chipping, 0f, 1f);
        }

        static void DrawGround(StyleMaterialSettings settings)
        {
            settings.naturalGround = EditorGUILayout.Toggle(Styles.NaturalGround, settings.naturalGround);
            settings.groundRelief = EditorGUILayout.Slider(Styles.GroundRelief, settings.groundRelief, 0f, 1f);
            settings.lumps = EditorGUILayout.IntSlider(Styles.Lumps, settings.lumps, 1, 16);
            settings.ripples = EditorGUILayout.Slider(Styles.Ripples, settings.ripples, 0f, 1f);
            settings.rippleCount = EditorGUILayout.IntSlider(Styles.RippleCount, settings.rippleCount, 1, 32);
            settings.rippleWarp = EditorGUILayout.Slider(Styles.RippleWarp, settings.rippleWarp, 0f, 1f);
        }

        void Restored()
        {
            m_Selected = Mathf.Clamp(m_Selected, 0, m_Stack.layers.Count - 1);
            m_Dirty = true;
            host?.Repaint();
        }

        internal override void DrawLayers()
        {
            var history = undo;
            var stack = m_Stack;
            var layer = current;

            EditorGUILayout.LabelField(Styles.Layers, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            using (new EditorGUI.DisabledScope(m_Selected <= 0))
            {
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 64f;
                layer.coverage = EditorGUILayout.Slider(Styles.Coverage, layer.coverage, 0f, 1f);
                layer.blend = (StyleMaterialBlend)EditorGUILayout.EnumPopup(Styles.Blend, layer.blend);

                if (layer.blend == StyleMaterialBlend.Height)
                {
                    layer.contrast = EditorGUILayout.Slider(Styles.Contrast, layer.contrast, 0f, 1f);
                    layer.offset = EditorGUILayout.Slider(Styles.Offset, layer.offset, -1f, 1f);
                }
                EditorGUIUtility.labelWidth = labelWidth;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (var i = stack.layers.Count - 1; i >= 0; i--)
                    DrawStackRow(stack, i);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Styles.AddLayer, EditorStyles.miniButtonLeft))
                    AddLayer(StyleMaterialPattern.None);

                if (GUILayout.Button(Styles.DuplicateLayer, EditorStyles.miniButtonMid))
                {
                    stack.layers.Insert(m_Selected + 1, layer.Clone());
                    m_Selected++;
                    m_Module = -1;
                    m_EditModule = false;
                }

                using (new EditorGUI.DisabledScope(stack.layers.Count <= 1))
                {
                    if (GUILayout.Button(Styles.RemoveLayer, EditorStyles.miniButtonMid))
                    {
                        stack.layers.RemoveAt(m_Selected);
                        m_Selected = Mathf.Clamp(m_Selected - 1, 0, stack.layers.Count - 1);
                        m_Module = -1;
                        m_EditModule = false;
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
            DrawModules(layer);

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio Layers");
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
                var picked = m_Map == (StyleMaterialMap)i && preview.view == StyleStudioView.Flat;
                var style = i == 0 ? EditorStyles.miniButtonLeft :
                    i == count - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;

                if (GUI.Toggle(button, picked, Styles.MapKeys[i], style) && !picked)
                {
                    m_Map = (StyleMaterialMap)i;
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

        void DrawStackRow(StyleMaterialStack stack, int index)
        {
            var layer = stack.layers[index];
            var row = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            var eye = new Rect(row.x + 4f, row.y + 12f, 16f, 16f);
            var picture = new Rect(eye.xMax + 6f, row.y + 4f, 32f, 32f);
            var text = new Rect(picture.xMax + 8f, row.y + 4f, row.xMax - picture.xMax - 12f, 16f);
            var sub = new Rect(text.x, text.yMax, text.width, 16f);

            if (Event.current.type == EventType.Repaint)
            {
                if (!m_EditModule && index == m_Selected)
                    EditorGUI.DrawRect(row, Styles.selection);

                var thumbnail = generator.Thumbnail(index);

                GUI.DrawTexture(picture, thumbnail != null ? thumbnail : Texture2D.grayTexture,
                                ScaleMode.ScaleAndCrop);

                var faded = GUI.color;

                if (!layer.visible)
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);

                if (m_Renaming != index)
                    EditorStyles.boldLabel.Draw(text, layer.label, false, false, false, false);

                EditorStyles.miniLabel.Draw(sub, Subtitle(layer, index), false, false, false, false);
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

            if (m_Selected != index)
                m_Module = -1;

            m_Selected = index;
            m_EditModule = false;
            m_Renaming = Event.current.clickCount == 2 ? index : -1;
            m_Focus = m_Renaming == index;
            GUI.FocusControl(null);
            Event.current.Use();
        }

        void DrawRename(StyleMaterialSettings layer, Rect text)
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

        static string Subtitle(StyleMaterialSettings layer, int index)
        {
            var kind = StyleMaterialSettings.LabelOf(layer.pattern);

            if (index == 0)
                return $"{kind} · Base";

            return layer.coverage >= 1f ? kind : $"{kind} · {Mathf.RoundToInt(layer.coverage * 100f)}%";
        }

        void AddLayer(StyleMaterialPattern pattern)
        {
            m_Selected = Mathf.Clamp(m_Selected + 1, 0, m_Stack.layers.Count);
            m_Module = -1;
            m_EditModule = false;
            m_Stack.layers.Insert(m_Selected, StyleMaterialSettings.Create(pattern));
            m_Dirty = true;
            undo.Commit("Add Texture Studio Layer");
            host?.Repaint();
        }

        void Swap(int a, int b)
        {
            var layers = m_Stack.layers;

            (layers[a], layers[b]) = (layers[b], layers[a]);
            m_Selected = b;
        }

        internal override void DrawPreview(Rect rect)
        {
            if (m_Dirty || !generator.ready)
            {
                m_Stack.Layer(m_Selected);
                generator.Generate(m_Stack);
                m_Dirty = false;
            }

            var changed = GUI.changed;
            GUI.changed = false;
            rect = shelf.Draw(rect, m_Stack, ref m_Preset, ref m_Category, LoadPreset);
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
                preview.DrawModel(rect, generator.albedo, generator.normal, generator.mask,
                                  generator.glows ? generator.emission : null);
            }

            if (GUI.changed || shelf.busy)
                host?.Repaint();

            GUI.changed |= changed;
        }

        void LoadPreset(string name)
        {
            if (!shelf.Load(name, m_Stack))
                return;

            m_Preset = name;
            m_Selected = 0;
            m_Module = -1;
            m_EditModule = false;
            m_Dirty = true;
            GUI.FocusControl(null);
            undo.Commit("Load Texture Studio Preset");
            host?.Repaint();
        }

        internal override void Release()
        {
            m_Generator?.Release();
            m_Preview?.Release();
            m_Shelf?.Release();
            m_Shelf = null;

            if (m_Undo != null)
                m_Undo.Release();

            m_Undo = null;
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

            var name = m_Stack.name.Trim();
            var albedo = StyleStudioExport.Save(generator.Export(StyleMaterialMap.Albedo), name + "_Albedo",
                                                StyleExportKind.Colour);
            StyleStudioExport.Save(generator.Export(StyleMaterialMap.Normal), name + "_Normal",
                                   StyleExportKind.Normal);
            StyleStudioExport.Save(generator.Export(StyleMaterialMap.Mask), name + "_Mask", StyleExportKind.Data);

            if (generator.glows)
                StyleStudioExport.Save(generator.Export(StyleMaterialMap.Emission), name + "_Emission",
                                       StyleExportKind.Colour);

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
            GUIUtility.ExitGUI();
        }
    }
}
