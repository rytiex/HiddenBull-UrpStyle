using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleMaterialRecipe
    {
        Cobblestone,
        Flagstone,
        Pavers,
        Bricks,
        HerringboneBricks,
        BasketWeave,
        Ashlar,
        Planks,
        RoughPlanks,
        Parquet,
        Soil,
        DryMud,
        Gravel,
        Sand,
        Snow,
        LakeIce,
        Crystals
    }

    enum StyleMaterialPattern
    {
        Voronoi,
        Courses,
        Herringbone,
        Basket,
        Scatter
    }

    enum StyleMaterialFamily
    {
        Stone,
        Brick,
        Wood,
        Ground,
        Ice
    }

    enum StyleMaterialLayer
    {
        Base,
        Variation,
        WoodGrain,
        EdgeWear,
        Dirt,
        Moss,
        Specks
    }

    [Serializable]
    sealed class StyleMaterialSettings : IStyleStudioBasis
    {
        const string Defaults = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/Defaults/Material";

        static readonly Dictionary<StyleMaterialRecipe, string> s_Defaults =
            new Dictionary<StyleMaterialRecipe, string>();

        public StyleMaterialRecipe recipe = StyleMaterialRecipe.Cobblestone;
        public int seed = 1;

        public int stones = 8;
        public float jitter = 0.9f;
        public int rows = 8;
        public int perRow = 4;
        public float rowOffset = 0.5f;
        public float lengthVariation;
        public int repeats = 3;
        public int brickLength = 2;
        public float gap = 0.06f;
        public float roundness = 0.4f;
        public float chipping = 0.3f;
        public float density = 0.3f;
        public float pebbleSize = 0.4f;
        public float elongation = 0.3f;
        public float fineCracks;

        public float groundRelief = 0.3f;
        public int lumps = 6;
        public float ripples;
        public int rippleCount = 8;
        public float rippleWarp = 0.4f;

        public float bevel = 0.25f;
        public float dome = 0.7f;
        public float facets;
        public float heightVariation = 0.3f;
        public float tilt = 0.3f;
        public float roughness = 0.3f;

        public Color stoneColour = new Color(0.55f, 0.53f, 0.5f, 1f);
        public Color gapColour = new Color(0.25f, 0.21f, 0.17f, 1f);
        public float grain = 0.15f;

        public bool variationOn = true;
        public float variation = 0.15f;
        public Color variationColour = new Color(0.62f, 0.55f, 0.46f, 1f);
        public float variationShare = 0.3f;
        public float heightTint;

        public bool woodOn = true;
        public Color grainColour = new Color(0.3f, 0.18f, 0.1f, 1f);
        public float rings = 0.6f;
        public float ringDensity = 8f;
        public float fibres = 0.5f;
        public float knots = 0.3f;
        public float woodRelief = 0.5f;

        public bool wearOn = true;
        public Color wearColour = new Color(0.72f, 0.7f, 0.66f, 1f);
        public float wear = 0.5f;

        public bool dirtOn = true;
        public Color dirtColour = new Color(0.3f, 0.25f, 0.19f, 1f);
        public float dirt = 0.4f;
        public float dirtSpread = 0.2f;

        public bool mossOn;
        public Color mossColour = new Color(0.27f, 0.36f, 0.14f, 1f);
        public float moss = 0.4f;
        public float mossSpread = 0.4f;
        public int mossScale = 4;

        public bool specksOn;
        public Color specksColour = new Color(0.95f, 0.97f, 1f, 1f);
        public float specks = 0.15f;
        public int specksScale = 96;
        public float specksShine;

        public float depth = 0.02f;
        public float smoothness = 0.35f;
        public float gapSmoothness = 0.1f;
        public float metallic;

        public float edgeHighlight = 0.5f;
        public float cavityShade = 0.5f;
        public float occlusion = 0.5f;
        public float lightWidth = 2f;

        public int resolution = 1024;
        public string name = "New Material";

        internal StyleMaterialPattern pattern => PatternOf(recipe);

        internal StyleMaterialFamily family => FamilyOf(recipe);

        internal bool randomShift =>
            recipe == StyleMaterialRecipe.Ashlar || recipe == StyleMaterialRecipe.Planks ||
            recipe == StyleMaterialRecipe.RoughPlanks;

        internal int units => pattern switch
        {
            StyleMaterialPattern.Courses => rows,
            StyleMaterialPattern.Herringbone => 2 * brickLength * repeats,
            StyleMaterialPattern.Basket => brickLength * repeats,
            StyleMaterialPattern.Scatter => stones,
            _ => stones
        };

        internal static StyleMaterialPattern PatternOf(StyleMaterialRecipe recipe)
        {
            return recipe switch
            {
                StyleMaterialRecipe.Cobblestone => StyleMaterialPattern.Voronoi,
                StyleMaterialRecipe.Flagstone => StyleMaterialPattern.Voronoi,
                StyleMaterialRecipe.HerringboneBricks => StyleMaterialPattern.Herringbone,
                StyleMaterialRecipe.Parquet => StyleMaterialPattern.Herringbone,
                StyleMaterialRecipe.BasketWeave => StyleMaterialPattern.Basket,
                StyleMaterialRecipe.Soil => StyleMaterialPattern.Scatter,
                StyleMaterialRecipe.Gravel => StyleMaterialPattern.Scatter,
                StyleMaterialRecipe.Sand => StyleMaterialPattern.Scatter,
                StyleMaterialRecipe.Snow => StyleMaterialPattern.Scatter,
                StyleMaterialRecipe.DryMud => StyleMaterialPattern.Voronoi,
                StyleMaterialRecipe.LakeIce => StyleMaterialPattern.Voronoi,
                StyleMaterialRecipe.Crystals => StyleMaterialPattern.Voronoi,
                _ => StyleMaterialPattern.Courses
            };
        }

        internal static StyleMaterialFamily FamilyOf(StyleMaterialRecipe recipe)
        {
            return recipe switch
            {
                StyleMaterialRecipe.Bricks => StyleMaterialFamily.Brick,
                StyleMaterialRecipe.HerringboneBricks => StyleMaterialFamily.Brick,
                StyleMaterialRecipe.BasketWeave => StyleMaterialFamily.Brick,
                StyleMaterialRecipe.Planks => StyleMaterialFamily.Wood,
                StyleMaterialRecipe.RoughPlanks => StyleMaterialFamily.Wood,
                StyleMaterialRecipe.Parquet => StyleMaterialFamily.Wood,
                StyleMaterialRecipe.Soil => StyleMaterialFamily.Ground,
                StyleMaterialRecipe.DryMud => StyleMaterialFamily.Ground,
                StyleMaterialRecipe.Gravel => StyleMaterialFamily.Ground,
                StyleMaterialRecipe.Sand => StyleMaterialFamily.Ground,
                StyleMaterialRecipe.Snow => StyleMaterialFamily.Ground,
                StyleMaterialRecipe.LakeIce => StyleMaterialFamily.Ice,
                StyleMaterialRecipe.Crystals => StyleMaterialFamily.Ice,
                _ => StyleMaterialFamily.Stone
            };
        }

        internal bool Shows(StyleMaterialLayer layer)
        {
            return layer != StyleMaterialLayer.WoodGrain || family == StyleMaterialFamily.Wood;
        }

        internal bool IsOn(StyleMaterialLayer layer)
        {
            return layer switch
            {
                StyleMaterialLayer.Variation => variationOn,
                StyleMaterialLayer.WoodGrain => woodOn,
                StyleMaterialLayer.EdgeWear => wearOn,
                StyleMaterialLayer.Dirt => dirtOn,
                StyleMaterialLayer.Moss => mossOn,
                StyleMaterialLayer.Specks => specksOn,
                _ => true
            };
        }

        internal void SetOn(StyleMaterialLayer layer, bool on)
        {
            switch (layer)
            {
                case StyleMaterialLayer.Variation:
                    variationOn = on;
                    break;
                case StyleMaterialLayer.WoodGrain:
                    woodOn = on;
                    break;
                case StyleMaterialLayer.EdgeWear:
                    wearOn = on;
                    break;
                case StyleMaterialLayer.Dirt:
                    dirtOn = on;
                    break;
                case StyleMaterialLayer.Moss:
                    mossOn = on;
                    break;
                case StyleMaterialLayer.Specks:
                    specksOn = on;
                    break;
            }
        }

        internal void ApplyRecipe(StyleMaterialRecipe picked)
        {
            var before = family;

            recipe = picked;
            ResetPattern();
            ResetShape();

            if (family == before && family != StyleMaterialFamily.Ground && family != StyleMaterialFamily.Ice)
                return;

            var basis = Recipe(picked);

            stoneColour = basis.stoneColour;
            gapColour = basis.gapColour;
            variationColour = basis.variationColour;
            wearColour = basis.wearColour;
            smoothness = basis.smoothness;
            gapSmoothness = basis.gapSmoothness;
            grain = basis.grain;
            specksOn = basis.specksOn;
            specksColour = basis.specksColour;
            specksShine = basis.specksShine;
            specks = basis.specks;
            heightTint = basis.heightTint;
            variation = basis.variation;
            mossOn = basis.mossOn;
            dirtOn = basis.dirtOn;
            wearOn = basis.wearOn;
            edgeHighlight = basis.edgeHighlight;
        }

        public void ApplyBasis()
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(Recipe(recipe)), this);
        }

        internal void ResetPattern()
        {
            var basis = Recipe(recipe);

            stones = basis.stones;
            jitter = basis.jitter;
            rows = basis.rows;
            perRow = basis.perRow;
            rowOffset = basis.rowOffset;
            lengthVariation = basis.lengthVariation;
            repeats = basis.repeats;
            brickLength = basis.brickLength;
            gap = basis.gap;
            roundness = basis.roundness;
            chipping = basis.chipping;
            density = basis.density;
            pebbleSize = basis.pebbleSize;
            elongation = basis.elongation;
            fineCracks = basis.fineCracks;
            groundRelief = basis.groundRelief;
            lumps = basis.lumps;
            ripples = basis.ripples;
            rippleCount = basis.rippleCount;
            rippleWarp = basis.rippleWarp;
            seed = basis.seed;
        }

        internal void ResetShape()
        {
            var basis = Recipe(recipe);

            bevel = basis.bevel;
            dome = basis.dome;
            facets = basis.facets;
            heightVariation = basis.heightVariation;
            tilt = basis.tilt;
            roughness = basis.roughness;
        }

        internal void ResetSurface()
        {
            var basis = Recipe(recipe);

            depth = basis.depth;
            smoothness = basis.smoothness;
            gapSmoothness = basis.gapSmoothness;
            metallic = basis.metallic;
        }

        internal void ResetLight()
        {
            var basis = Recipe(recipe);

            edgeHighlight = basis.edgeHighlight;
            cavityShade = basis.cavityShade;
            occlusion = basis.occlusion;
            lightWidth = basis.lightWidth;
        }

        internal void ResetLayer(StyleMaterialLayer layer)
        {
            var basis = Recipe(recipe);

            switch (layer)
            {
                case StyleMaterialLayer.Base:
                    stoneColour = basis.stoneColour;
                    gapColour = basis.gapColour;
                    grain = basis.grain;
                    break;
                case StyleMaterialLayer.Variation:
                    variation = basis.variation;
                    variationColour = basis.variationColour;
                    variationShare = basis.variationShare;
                    heightTint = basis.heightTint;
                    break;
                case StyleMaterialLayer.WoodGrain:
                    grainColour = basis.grainColour;
                    rings = basis.rings;
                    ringDensity = basis.ringDensity;
                    fibres = basis.fibres;
                    knots = basis.knots;
                    woodRelief = basis.woodRelief;
                    break;
                case StyleMaterialLayer.EdgeWear:
                    wearColour = basis.wearColour;
                    wear = basis.wear;
                    break;
                case StyleMaterialLayer.Dirt:
                    dirtColour = basis.dirtColour;
                    dirt = basis.dirt;
                    dirtSpread = basis.dirtSpread;
                    break;
                case StyleMaterialLayer.Moss:
                    mossColour = basis.mossColour;
                    moss = basis.moss;
                    mossSpread = basis.mossSpread;
                    mossScale = basis.mossScale;
                    break;
                case StyleMaterialLayer.Specks:
                    specksColour = basis.specksColour;
                    specks = basis.specks;
                    specksScale = basis.specksScale;
                    specksShine = basis.specksShine;
                    break;
            }
        }

        static StyleMaterialSettings Recipe(StyleMaterialRecipe recipe)
        {
            if (!s_Defaults.TryGetValue(recipe, out var json))
            {
                var path = Path.GetFullPath($"{Defaults}/{recipe}.json");

                json = File.Exists(path) ? File.ReadAllText(path) : null;
                s_Defaults[recipe] = json;
            }

            var basis = new StyleMaterialSettings();

            if (json != null)
                JsonUtility.FromJsonOverwrite(json, basis);

            basis.recipe = recipe;
            return basis;
        }
    }
}
