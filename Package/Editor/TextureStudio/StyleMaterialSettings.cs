using System;
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
            var basis = new StyleMaterialSettings();

            switch (FamilyOf(recipe))
            {
                case StyleMaterialFamily.Brick:
                    basis.stoneColour = new Color(0.55f, 0.27f, 0.18f, 1f);
                    basis.gapColour = new Color(0.62f, 0.6f, 0.55f, 1f);
                    basis.variationColour = new Color(0.42f, 0.2f, 0.14f, 1f);
                    basis.wearColour = new Color(0.68f, 0.45f, 0.34f, 1f);
                    basis.smoothness = 0.2f;
                    basis.grain = 0.2f;
                    break;
                case StyleMaterialFamily.Wood:
                    basis.stoneColour = new Color(0.55f, 0.38f, 0.22f, 1f);
                    basis.gapColour = new Color(0.12f, 0.08f, 0.05f, 1f);
                    basis.variationColour = new Color(0.45f, 0.3f, 0.18f, 1f);
                    basis.wearColour = new Color(0.68f, 0.52f, 0.35f, 1f);
                    basis.smoothness = 0.3f;
                    basis.grain = 0.05f;
                    break;
                case StyleMaterialFamily.Ground:
                    basis.edgeHighlight = 0.25f;
                    break;
            }

            switch (recipe)
            {
                case StyleMaterialRecipe.Flagstone:
                    Set(basis, 0.04f, 0.1f, 0.5f, 0.08f, 0.15f, 0.15f, 0.15f, 0.4f);
                    basis.stones = 4;
                    basis.jitter = 1f;
                    break;
                case StyleMaterialRecipe.Pavers:
                    Set(basis, 0.08f, 0.15f, 0.15f, 0.15f, 0.25f, 0.2f, 0.2f, 0.25f);
                    basis.rows = 8;
                    basis.perRow = 4;
                    break;
                case StyleMaterialRecipe.Bricks:
                    Set(basis, 0.12f, 0.1f, 0.15f, 0.12f, 0.15f, 0.15f, 0.1f, 0.35f);
                    basis.rows = 16;
                    basis.perRow = 8;
                    break;
                case StyleMaterialRecipe.HerringboneBricks:
                    Set(basis, 0.12f, 0.1f, 0.15f, 0.12f, 0.15f, 0.15f, 0.1f, 0.35f);
                    basis.repeats = 3;
                    basis.brickLength = 2;
                    break;
                case StyleMaterialRecipe.BasketWeave:
                    Set(basis, 0.12f, 0.1f, 0.15f, 0.12f, 0.15f, 0.15f, 0.1f, 0.35f);
                    basis.repeats = 6;
                    basis.brickLength = 2;
                    break;
                case StyleMaterialRecipe.Ashlar:
                    Set(basis, 0.05f, 0.1f, 0.3f, 0.12f, 0.25f, 0.2f, 0.15f, 0.4f);
                    basis.rows = 5;
                    basis.perRow = 3;
                    basis.lengthVariation = 0.6f;
                    basis.stoneColour = new Color(0.62f, 0.57f, 0.48f, 1f);
                    basis.gapColour = new Color(0.45f, 0.42f, 0.38f, 1f);
                    break;
                case StyleMaterialRecipe.Planks:
                    Set(basis, 0.04f, 0.05f, 0.05f, 0.06f, 0.1f, 0.1f, 0.05f, 0.15f);
                    basis.rows = 6;
                    basis.perRow = 2;
                    basis.lengthVariation = 0.9f;
                    break;
                case StyleMaterialRecipe.RoughPlanks:
                    Set(basis, 0.08f, 0.1f, 0.45f, 0.1f, 0.2f, 0.3f, 0.25f, 0.3f);
                    basis.rows = 4;
                    basis.perRow = 1;
                    basis.lengthVariation = 1f;
                    basis.knots = 0.6f;
                    basis.stoneColour = new Color(0.48f, 0.37f, 0.27f, 1f);
                    break;
                case StyleMaterialRecipe.Parquet:
                    Set(basis, 0.03f, 0.05f, 0.03f, 0.05f, 0.05f, 0.05f, 0.03f, 0.1f);
                    basis.repeats = 2;
                    basis.brickLength = 4;
                    basis.stoneColour = new Color(0.6f, 0.42f, 0.25f, 1f);
                    basis.smoothness = 0.5f;
                    break;
                case StyleMaterialRecipe.Soil:
                    Set(basis, 0f, 0f, 0f, 0.3f, 0.55f, 0.4f, 0.3f, 0.2f);
                    Scatter(basis, 16, 0.25f, 0.35f, 0.4f);
                    Ground(basis, 0.35f, 6, 0f);
                    Paint(basis, Rgb(0.5f, 0.48f, 0.45f), Rgb(0.33f, 0.24f, 0.16f), 0.25f, 0.1f);
                    break;
                case StyleMaterialRecipe.DryMud:
                    Set(basis, 0.05f, 0.2f, 0.4f, 0.06f, -0.4f, 0.1f, 0.15f, 0.5f);
                    basis.stones = 7;
                    basis.jitter = 1f;
                    Ground(basis, 0.1f, 8, 0f);
                    basis.fineCracks = 0.6f;
                    Paint(basis, Rgb(0.55f, 0.45f, 0.33f), Rgb(0.22f, 0.17f, 0.12f), 0.15f, 0.1f);
                    basis.wearOn = false;
                    break;
                case StyleMaterialRecipe.Gravel:
                    Set(basis, 0f, 0f, 0f, 0.4f, 0.6f, 0.5f, 0.4f, 0.15f);
                    basis.facets = 0.15f;
                    Scatter(basis, 20, 1f, 0.85f, 0.4f);
                    Ground(basis, 0.15f, 8, 0f);
                    Paint(basis, Rgb(0.55f, 0.53f, 0.5f), Rgb(0.3f, 0.27f, 0.24f), 0.3f, 0.1f);
                    basis.variation = 0.25f;
                    break;
                case StyleMaterialRecipe.Sand:
                    Set(basis, 0f, 0f, 0f, 0.3f, 0.8f, 0.3f, 0.3f, 0.1f);
                    Scatter(basis, 12, 0.08f, 0.25f, 0.4f);
                    Ground(basis, 0.6f, 3, 1f);
                    Paint(basis, Rgb(0.5f, 0.45f, 0.4f), Rgb(0.76f, 0.66f, 0.48f), 0.25f, 0.15f);
                    basis.dirtOn = false;
                    basis.specksOn = true;
                    basis.specksColour = Rgb(0.45f, 0.38f, 0.3f);
                    break;
                case StyleMaterialRecipe.Snow:
                    Set(basis, 0f, 0f, 0f, 0.3f, 0.8f, 0.3f, 0.3f, 0.1f);
                    Scatter(basis, 8, 0f, 0.3f, 0.3f);
                    Ground(basis, 0.5f, 3, 0.3f);
                    Paint(basis, Rgb(0.9f, 0.93f, 0.97f), Rgb(0.9f, 0.93f, 0.97f), 0.4f, 0.4f);
                    basis.dirtOn = false;
                    basis.wearOn = false;
                    basis.grain = 0.03f;
                    basis.specksOn = true;
                    basis.specksShine = 0.95f;
                    break;
                case StyleMaterialRecipe.LakeIce:
                    Set(basis, 0.015f, 0f, 0.3f, 0.01f, 0f, 0.05f, 0.02f, 0.05f);
                    basis.stones = 5;
                    basis.jitter = 1f;
                    Ground(basis, 0.02f, 8, 0f);
                    basis.fineCracks = 0.7f;
                    Paint(basis, Rgb(0.45f, 0.62f, 0.75f), Rgb(0.85f, 0.92f, 0.97f), 0.9f, 0.5f);
                    basis.dirtOn = false;
                    basis.wearOn = false;
                    basis.grain = 0.08f;
                    basis.specksOn = true;
                    basis.specks = 0.08f;
                    break;
                case StyleMaterialRecipe.Crystals:
                    Set(basis, 0.04f, 0f, 0.1f, 0.04f, 1f, 0.6f, 0.4f, 0.05f);
                    basis.stones = 6;
                    basis.jitter = 1f;
                    basis.facets = 1f;
                    Ground(basis, 0.05f, 8, 0f);
                    Paint(basis, Rgb(0.45f, 0.6f, 0.85f), Rgb(0.1f, 0.12f, 0.2f), 0.85f, 0.2f);
                    basis.dirtOn = false;
                    basis.wearOn = false;
                    basis.grain = 0.02f;
                    basis.heightTint = 0.8f;
                    basis.variationColour = Rgb(0.6f, 0.45f, 0.85f);
                    break;
            }

            return basis;
        }

        static Color Rgb(float r, float g, float b) => new Color(r, g, b, 1f);

        static void Scatter(StyleMaterialSettings basis, int stones, float density, float size, float elongation)
        {
            basis.stones = stones;
            basis.density = density;
            basis.pebbleSize = size;
            basis.elongation = elongation;
            basis.jitter = 1f;
        }

        static void Ground(StyleMaterialSettings basis, float relief, int lumps, float ripples)
        {
            basis.groundRelief = relief;
            basis.lumps = lumps;
            basis.ripples = ripples;
        }

        static void Paint(StyleMaterialSettings basis, Color main, Color joint, float smoothness, float jointSmooth)
        {
            basis.stoneColour = main;
            basis.gapColour = joint;
            basis.variationColour = main * 0.85f;
            basis.wearColour = Color.Lerp(main, Color.white, 0.3f);
            basis.smoothness = smoothness;
            basis.gapSmoothness = jointSmooth;
        }

        static void Set(StyleMaterialSettings basis, float gap, float roundness, float chipping, float bevel,
                        float dome, float heightVariation, float tilt, float roughness)
        {
            basis.gap = gap;
            basis.roundness = roundness;
            basis.chipping = chipping;
            basis.bevel = bevel;
            basis.dome = dome;
            basis.heightVariation = heightVariation;
            basis.tilt = tilt;
            basis.roughness = roughness;
        }
    }
}
