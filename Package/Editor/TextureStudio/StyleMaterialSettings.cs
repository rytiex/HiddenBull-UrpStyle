using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleMaterialPattern
    {
        Cells,
        Rows,
        Herringbone,
        Basket,
        Scatter,
        None
    }

    enum StyleMaterialBlend
    {
        Normal,
        Height
    }

    [Serializable]
    sealed class StyleMaterialSettings
    {
        public StyleMaterialPattern pattern = StyleMaterialPattern.None;
        public bool randomShift;
        public bool naturalGround;
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
        public float roundness = 0.2f;
        public float chipping;
        public float density = 0.3f;
        public float pebbleSize = 0.4f;
        public float elongation = 0.3f;
        public float fineCracks;

        public float groundRelief = 0.1f;
        public int lumps = 6;
        public float ripples;
        public int rippleCount = 8;
        public float rippleWarp = 0.4f;

        public float bevel = 0.25f;
        public float dome = 0.5f;
        public float facets;
        public float heightVariation;
        public float tilt;
        public float roughness = 0.2f;

        public Color stoneColour = new Color(0.6f, 0.6f, 0.6f, 1f);
        public Color gapColour = new Color(0.3f, 0.3f, 0.3f, 1f);
        public float grain = 0.1f;

        public bool variationOn;
        public float variation = 0.15f;
        public Color variationColour = new Color(0.62f, 0.55f, 0.46f, 1f);
        public float variationShare = 0.3f;
        public float heightTint;

        public float depth = 0.02f;
        public float smoothness = 0.35f;
        public float gapSmoothness = 0.1f;
        public float metallic;

        public float edgeHighlight = 0.5f;
        public float cavityShade = 0.5f;
        public float occlusion = 0.5f;
        public float lightWidth = 2f;

        public string label;
        public bool visible = true;
        public float coverage = 1f;
        public StyleMaterialBlend blend;
        public float contrast = 0.5f;
        public float offset;
        public List<StyleMaterialModule> modules = new List<StyleMaterialModule>();

        internal bool hasPattern => pattern != StyleMaterialPattern.None;

        internal int units => pattern switch
        {
            StyleMaterialPattern.Rows => rows,
            StyleMaterialPattern.Herringbone => 2 * brickLength * repeats,
            StyleMaterialPattern.Basket => brickLength * Math.Max(repeats / 2 * 2, 2),
            StyleMaterialPattern.Scatter => stones,
            _ => stones
        };

        internal void SetPattern(StyleMaterialPattern picked)
        {
            if (label == LabelOf(pattern))
                label = LabelOf(picked);

            pattern = picked;
            ResetPattern();
        }

        internal static StyleMaterialSettings Create(StyleMaterialPattern pattern)
        {
            return new StyleMaterialSettings { pattern = pattern, label = LabelOf(pattern) };
        }

        internal static string LabelOf(StyleMaterialPattern pattern)
        {
            return pattern == StyleMaterialPattern.None ? "Empty" : pattern.ToString();
        }

        internal StyleMaterialSettings Clone()
        {
            return JsonUtility.FromJson<StyleMaterialSettings>(JsonUtility.ToJson(this));
        }

        internal void ResetPattern()
        {
            var basis = new StyleMaterialSettings();

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
            randomShift = basis.randomShift;
            naturalGround = basis.naturalGround;
            bevel = basis.bevel;
            dome = basis.dome;
            facets = basis.facets;
            heightVariation = basis.heightVariation;
            tilt = basis.tilt;
            gapColour = basis.gapColour;
            gapSmoothness = basis.gapSmoothness;
            variationOn = basis.variationOn;
            variation = basis.variation;
            variationColour = basis.variationColour;
            variationShare = basis.variationShare;
            heightTint = basis.heightTint;
        }

        internal void ResetSurface()
        {
            var basis = new StyleMaterialSettings();

            depth = basis.depth;
            smoothness = basis.smoothness;
            metallic = basis.metallic;
        }

        internal void ResetGround()
        {
            var basis = new StyleMaterialSettings();

            naturalGround = basis.naturalGround;
            groundRelief = basis.groundRelief;
            lumps = basis.lumps;
            ripples = basis.ripples;
            rippleCount = basis.rippleCount;
            rippleWarp = basis.rippleWarp;
        }

        internal void ResetLight()
        {
            var basis = new StyleMaterialSettings();

            edgeHighlight = basis.edgeHighlight;
            cavityShade = basis.cavityShade;
            occlusion = basis.occlusion;
            lightWidth = basis.lightWidth;
        }

        internal void ResetBase()
        {
            var basis = new StyleMaterialSettings();

            stoneColour = basis.stoneColour;
            grain = basis.grain;
            roughness = basis.roughness;
            seed = basis.seed;
        }
    }
}
