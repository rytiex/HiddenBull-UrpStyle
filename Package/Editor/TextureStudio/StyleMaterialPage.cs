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
                "The kind of surface to generate. Picking a recipe loads its pattern and shape defaults.");

            internal static readonly GUIContent Pattern = new GUIContent("Pattern");
            internal static readonly GUIContent Shape = new GUIContent("Shape");
            internal static readonly GUIContent Surface = new GUIContent("Surface");
            internal static readonly GUIContent Light = new GUIContent("Painted Light");
            internal static readonly GUIContent Output = new GUIContent("Output");

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
            internal static readonly GUIContent Seed = new GUIContent("Seed", "Picks another random layout.");

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
                "Smoothness of the stones, saved in the mask's alpha. Dirt and moss are always rough.");
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
                "File name start. Saving writes Name_Albedo, Name_Normal and Name_Mask.");
            internal static readonly GUIContent Save = new GUIContent("Save Textures",
                "Writes the albedo, normal and mask PNGs into the folder. Files with the same names are replaced.");

            internal static readonly GUIContent Layers = new GUIContent("Layers");
            internal static readonly GUIContent Maps = new GUIContent("Maps");
            internal static readonly GUIContent ResetLayer = new GUIContent("Reset",
                "Puts the picked layer's settings back to their defaults.");

            internal static readonly GUIContent Grain = new GUIContent("Grain",
                "Fine light and dark speckles over the stones and the joints.");

            internal static readonly GUIContent Variation = new GUIContent("Brightness",
                "How much lighter or darker each stone is than the others.");
            internal static readonly GUIContent VariationColour = new GUIContent("Second Colour",
                "Some stones lean toward this colour.");
            internal static readonly GUIContent VariationShare = new GUIContent("Share",
                "Share of the stones that lean toward the second colour.");

            internal static readonly GUIContent WearColour = new GUIContent("Colour",
                "Colour of the worn, raised edges.");
            internal static readonly GUIContent Wear = new GUIContent("Amount", "How strongly the edges are worn.");

            internal static readonly GUIContent DirtColour = new GUIContent("Colour", "Colour of the dirt.");
            internal static readonly GUIContent Dirt = new GUIContent("Amount",
                "How much dirt creeps from the joints and creases onto the stones.");
            internal static readonly GUIContent DirtSpread = new GUIContent("Spread",
                "How far from the joints the dirt reaches, as a share of a stone's size.");

            internal static readonly GUIContent MossColour = new GUIContent("Colour", "Colour of the moss.");
            internal static readonly GUIContent Moss = new GUIContent("Amount", "How much moss grows.");
            internal static readonly GUIContent MossSpread = new GUIContent("Climb",
                "How high up the stones the moss climbs out of the joints.");
            internal static readonly GUIContent MossScale = new GUIContent("Patch Size",
                "Number of moss patches across the texture. More gives smaller patches.");

            internal static readonly GUIContent[] RecipeNames =
            {
                new GUIContent("Stone/Cobblestone", "Round stones of mixed sizes with earth in the joints."),
                new GUIContent("Stone/Flagstone", "Large flat slabs with chipped outlines and narrow joints."),
                new GUIContent("Stone/Pavers", "Cut rectangular stones in shifted rows."),
                new GUIContent("Brick/Bricks", "Brick wall in rows shifted by half a brick."),
                new GUIContent("Brick/Herringbone", "Bricks in a zigzag of lying and standing pairs."),
                new GUIContent("Brick/Basket Weave", "Squares of bricks, turning lying and standing in turn."),
                new GUIContent("Stone/Ashlar Wall", "Big cut blocks of mixed lengths in rows."),
                new GUIContent("Wood/Planks", "Floor or wall boards with joints at random places."),
                new GUIContent("Wood/Rough Planks", "Wide, crooked, weathered boards with open gaps."),
                new GUIContent("Wood/Parquet", "Short boards in a herringbone floor."),
                new GUIContent("Ground/Soil", "Lumpy earth with a few small stones."),
                new GUIContent("Ground/Dry Mud", "Dried mud broken into curled plates by cracks."),
                new GUIContent("Ground/Gravel", "Packed round pebbles lying over each other."),
                new GUIContent("Ground/Sand", "Wind ripples in sand with a few pebbles."),
                new GUIContent("Ground/Snow", "Soft bumpy snow with glittering crumbs and wind marks."),
                new GUIContent("Ice/Lake Ice", "Flat smooth ice with white cracks and bubbles."),
                new GUIContent("Ice/Crystals", "Clusters of sharp, chiselled crystals.")
            };

            internal static readonly GUIContent[] LayerNames =
            {
                new GUIContent("Base", "Main and joint colours. Always on."),
                new GUIContent("Variation", "Each piece a little different in brightness and colour."),
                new GUIContent("Wood Grain", "Fibres, growth rings and knots of the wood."),
                new GUIContent("Edge Wear", "Lighter, worn colour on the raised edges."),
                new GUIContent("Dirt", "Dirt creeping from the joints and creases onto the pieces."),
                new GUIContent("Moss", "Moss growing in the joints and up the low parts of the pieces."),
                new GUIContent("Specks", "Small dots all over: glitter on snow, bubbles in ice, grains in sand.")
            };

            internal static readonly GUIContent[] MainColour =
            {
                new GUIContent("Stone Colour", "Colour of the stones."),
                new GUIContent("Brick Colour", "Colour of the bricks."),
                new GUIContent("Wood Colour", "Colour of the wood."),
                new GUIContent("Pebble Colour", "Colour of the stones, pebbles or plates."),
                new GUIContent("Ice Colour", "Colour of the ice or the crystals.")
            };

            internal static readonly GUIContent[] JointColour =
            {
                new GUIContent("Gap Colour", "Colour of the ground in the joints."),
                new GUIContent("Mortar Colour", "Colour of the mortar between the bricks."),
                new GUIContent("Gap Colour", "Colour seen through the gaps between the boards."),
                new GUIContent("Ground Colour", "Colour of the soil, sand or snow around the pieces."),
                new GUIContent("Crack Colour", "Colour in the cracks and at the foot of the crystals.")
            };

            internal static readonly GUIContent Ground = new GUIContent("Ground");
            internal static readonly GUIContent Pebbles = new GUIContent("Pebbles",
                "Places across the texture where a pebble can lie. More gives smaller pebbles.");
            internal static readonly GUIContent Density = new GUIContent("Density",
                "Share of the places that get a pebble. 0 leaves bare ground.");
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
                "Lightens the high parts and darkens the low parts.");
            internal static readonly GUIContent SpecksColour = new GUIContent("Colour", "Colour of the specks.");
            internal static readonly GUIContent Specks = new GUIContent("Amount", "How many specks there are.");
            internal static readonly GUIContent SpecksScale = new GUIContent("Grid",
                "Specks across the texture. More gives smaller, closer specks.");
            internal static readonly GUIContent SpecksShine = new GUIContent("Shine",
                "Smoothness of the specks, so they glint in the light. 0 leaves the surface as it is.");

            internal static readonly GUIContent Repeats = new GUIContent("Repeats",
                "How many times the pattern repeats across the texture.");
            internal static readonly GUIContent Squares = new GUIContent("Squares",
                "Squares across the texture. Always even so the texture repeats without a seam.");
            internal static readonly GUIContent BrickLength = new GUIContent("Length",
                "Length of a piece as a multiple of its width.");
            internal static readonly GUIContent LengthVariation = new GUIContent("Length Variation",
                "How much the lengths of the pieces in a row differ.");

            internal static readonly GUIContent GrainColour = new GUIContent("Colour",
                "Colour of the dark lines of the grain and of the knots.");
            internal static readonly GUIContent Rings = new GUIContent("Rings", "Strength of the growth ring lines.");
            internal static readonly GUIContent RingDensity = new GUIContent("Ring Density",
                "Growth ring lines across the width of a board.");
            internal static readonly GUIContent Fibres = new GUIContent("Fibres",
                "Strength of the fine fibre streaks.");
            internal static readonly GUIContent Knots = new GUIContent("Knots",
                "Chance of a knot on each board; high values give some boards two.");
            internal static readonly GUIContent WoodRelief = new GUIContent("Relief",
                "How deep the grain is cut into the height and normal maps.");

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

        [SerializeField]
        StyleMaterialLayer m_Layer;

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

            var recipe = (StyleMaterialRecipe)EditorGUILayout.Popup(Styles.Recipe, (int)settings.recipe,
                                                                     Styles.RecipeNames);
            if (recipe != settings.recipe)
                settings.ApplyRecipe(recipe);

            var open = StyleSectionHeader.Draw(Styles.Pattern, "TextureStudio.Material.Pattern",
                                               () => Reset(settings.ResetPattern));
            if (open)
                DrawPattern(settings);

            StyleSectionHeader.End(open);

            if (settings.family == StyleMaterialFamily.Ground)
            {
                open = StyleSectionHeader.Draw(Styles.Ground, "TextureStudio.Material.Ground",
                                               () => Reset(settings.ResetPattern));
                if (open)
                    DrawGround(settings);

                StyleSectionHeader.End(open);
            }

            open = StyleSectionHeader.Draw(Styles.Shape, "TextureStudio.Material.Shape",
                                           () => Reset(settings.ResetShape));
            if (open)
            {
                settings.bevel = EditorGUILayout.Slider(Styles.Bevel, settings.bevel, 0.01f, 0.6f);
                settings.dome = EditorGUILayout.Slider(Styles.Dome, settings.dome, -1f, 1f);
                settings.facets = EditorGUILayout.Slider(Styles.Facets, settings.facets, 0f, 1f);
                settings.heightVariation = EditorGUILayout.Slider(Styles.HeightVariation, settings.heightVariation,
                                                                  0f, 1f);
                settings.tilt = EditorGUILayout.Slider(Styles.Tilt, settings.tilt, 0f, 1f);
                settings.roughness = EditorGUILayout.Slider(Styles.Roughness, settings.roughness, 0f, 1f);
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

        static void DrawPattern(StyleMaterialSettings settings)
        {
            switch (settings.pattern)
            {
                case StyleMaterialPattern.Courses:
                    settings.rows = EditorGUILayout.IntSlider(Styles.Rows, settings.rows, 2, 32) / 2 * 2;
                    settings.perRow = EditorGUILayout.IntSlider(Styles.PerRow, settings.perRow, 1, 16);

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
            settings.seed = EditorGUILayout.IntSlider(Styles.Seed, settings.seed, 0, 999);
        }

        static void DrawGround(StyleMaterialSettings settings)
        {
            settings.groundRelief = EditorGUILayout.Slider(Styles.GroundRelief, settings.groundRelief, 0f, 1f);
            settings.lumps = EditorGUILayout.IntSlider(Styles.Lumps, settings.lumps, 1, 16);
            settings.ripples = EditorGUILayout.Slider(Styles.Ripples, settings.ripples, 0f, 1f);
            settings.rippleCount = EditorGUILayout.IntSlider(Styles.RippleCount, settings.rippleCount, 1, 32);
            settings.rippleWarp = EditorGUILayout.Slider(Styles.RippleWarp, settings.rippleWarp, 0f, 1f);
        }

        void Restored()
        {
            m_Dirty = true;
            host?.Repaint();
        }

        internal override void DrawLayers()
        {
            var history = undo;
            var settings = m_Settings;

            EditorGUILayout.LabelField(Styles.Layers, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            if (!settings.Shows(m_Layer))
                m_Layer = StyleMaterialLayer.Base;

            for (var i = 0; i < Styles.LayerNames.Length; i++)
            {
                if (settings.Shows((StyleMaterialLayer)i))
                    DrawLayerRow(settings, (StyleMaterialLayer)i);
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Styles.LayerNames[(int)m_Layer], EditorStyles.miniBoldLabel);

                if (GUILayout.Button(Styles.ResetLayer, EditorStyles.miniButton, GUILayout.Width(48f)))
                    settings.ResetLayer(m_Layer);
            }

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.1f, 70f, 96f);

            using (new EditorGUI.DisabledScope(!settings.IsOn(m_Layer)))
                DrawLayer(settings, m_Layer);

            EditorGUIUtility.labelWidth = labelWidth;

            if (EditorGUI.EndChangeCheck())
                m_Dirty = true;

            history.Commit("Change Texture Studio Layer");

            EditorGUILayout.Space();
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

        void DrawLayerRow(StyleMaterialSettings settings, StyleMaterialLayer layer)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(layer == StyleMaterialLayer.Base))
                {
                    var on = EditorGUILayout.Toggle(settings.IsOn(layer), GUILayout.Width(16f));
                    settings.SetOn(layer, on);
                }

                if (GUILayout.Toggle(m_Layer == layer, Styles.LayerNames[(int)layer], EditorStyles.miniButton))
                    m_Layer = layer;
            }
        }

        static void DrawLayer(StyleMaterialSettings settings, StyleMaterialLayer layer)
        {
            switch (layer)
            {
                case StyleMaterialLayer.Base:
                    settings.stoneColour = EditorGUILayout.ColorField(Styles.MainColour[(int)settings.family],
                                                                      settings.stoneColour, true, false, false);
                    settings.gapColour = EditorGUILayout.ColorField(Styles.JointColour[(int)settings.family],
                                                                    settings.gapColour, true, false, false);
                    settings.grain = EditorGUILayout.Slider(Styles.Grain, settings.grain, 0f, 0.5f);
                    break;
                case StyleMaterialLayer.Variation:
                    settings.variation = EditorGUILayout.Slider(Styles.Variation, settings.variation, 0f, 0.5f);
                    settings.variationColour = EditorGUILayout.ColorField(Styles.VariationColour,
                                                                          settings.variationColour, true, false,
                                                                          false);
                    settings.variationShare = EditorGUILayout.Slider(Styles.VariationShare, settings.variationShare,
                                                                     0f, 1f);
                    settings.heightTint = EditorGUILayout.Slider(Styles.HeightTint, settings.heightTint, 0f, 1f);
                    break;
                case StyleMaterialLayer.WoodGrain:
                    settings.grainColour = EditorGUILayout.ColorField(Styles.GrainColour, settings.grainColour, true,
                                                                      false, false);
                    settings.rings = EditorGUILayout.Slider(Styles.Rings, settings.rings, 0f, 1f);
                    settings.ringDensity = EditorGUILayout.Slider(Styles.RingDensity, settings.ringDensity, 1f, 24f);
                    settings.fibres = EditorGUILayout.Slider(Styles.Fibres, settings.fibres, 0f, 1f);
                    settings.knots = EditorGUILayout.Slider(Styles.Knots, settings.knots, 0f, 1f);
                    settings.woodRelief = EditorGUILayout.Slider(Styles.WoodRelief, settings.woodRelief, 0f, 1f);
                    break;
                case StyleMaterialLayer.EdgeWear:
                    settings.wearColour = EditorGUILayout.ColorField(Styles.WearColour, settings.wearColour, true,
                                                                     false, false);
                    settings.wear = EditorGUILayout.Slider(Styles.Wear, settings.wear, 0f, 1f);
                    break;
                case StyleMaterialLayer.Dirt:
                    settings.dirtColour = EditorGUILayout.ColorField(Styles.DirtColour, settings.dirtColour, true,
                                                                     false, false);
                    settings.dirt = EditorGUILayout.Slider(Styles.Dirt, settings.dirt, 0f, 1f);
                    settings.dirtSpread = EditorGUILayout.Slider(Styles.DirtSpread, settings.dirtSpread, 0.01f, 1f);
                    break;
                case StyleMaterialLayer.Moss:
                    settings.mossColour = EditorGUILayout.ColorField(Styles.MossColour, settings.mossColour, true,
                                                                     false, false);
                    settings.moss = EditorGUILayout.Slider(Styles.Moss, settings.moss, 0f, 1f);
                    settings.mossSpread = EditorGUILayout.Slider(Styles.MossSpread, settings.mossSpread, 0.05f, 1f);
                    settings.mossScale = EditorGUILayout.IntSlider(Styles.MossScale, settings.mossScale, 1, 16);
                    break;
                case StyleMaterialLayer.Specks:
                    settings.specksColour = EditorGUILayout.ColorField(Styles.SpecksColour, settings.specksColour, true,
                                                                       false, false);
                    settings.specks = EditorGUILayout.Slider(Styles.Specks, settings.specks, 0f, 1f);
                    settings.specksScale = EditorGUILayout.IntSlider(Styles.SpecksScale, settings.specksScale, 16, 256);
                    settings.specksShine = EditorGUILayout.Slider(Styles.SpecksShine, settings.specksShine, 0f, 1f);
                    break;
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
