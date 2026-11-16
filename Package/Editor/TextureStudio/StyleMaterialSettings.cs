using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleMaterialRecipe
    {
        TestTiles
    }

    [Serializable]
    sealed class StyleMaterialSettings
    {
        public StyleMaterialRecipe recipe = StyleMaterialRecipe.TestTiles;

        public int tiles = 4;
        public float gap = 0.04f;
        public float bevel = 0.12f;
        public float heightVariation = 0.3f;
        public int seed = 1;

        public Color colour = new Color(0.62f, 0.58f, 0.52f, 1f);
        public Color gapColour = new Color(0.22f, 0.2f, 0.18f, 1f);
        public float variation = 0.15f;

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

        internal void ResetPattern()
        {
            var basis = new StyleMaterialSettings();

            tiles = basis.tiles;
            gap = basis.gap;
            bevel = basis.bevel;
            heightVariation = basis.heightVariation;
            seed = basis.seed;
        }

        internal void ResetColour()
        {
            var basis = new StyleMaterialSettings();

            colour = basis.colour;
            gapColour = basis.gapColour;
            variation = basis.variation;
        }

        internal void ResetSurface()
        {
            var basis = new StyleMaterialSettings();

            depth = basis.depth;
            smoothness = basis.smoothness;
            gapSmoothness = basis.gapSmoothness;
            metallic = basis.metallic;
        }

        internal void ResetLight()
        {
            var basis = new StyleMaterialSettings();

            edgeHighlight = basis.edgeHighlight;
            cavityShade = basis.cavityShade;
            occlusion = basis.occlusion;
            lightWidth = basis.lightWidth;
        }
    }
}
