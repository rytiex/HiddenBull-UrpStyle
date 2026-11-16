using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleModuleHeight
    {
        None,
        Cut,
        Raise,
        Flatten
    }

    enum StyleModuleArea
    {
        Everywhere,
        Pieces,
        Joints
    }

    enum StyleMaskKind
    {
        Cracks,
        Scratches,
        Streaks,
        WoodGrain,
        Chips,
        Clumps,
        Leaves,
        Edges,
        Cavities,
        Height,
        Noise,
        RandomPieces
    }

    enum StyleMaskBlend
    {
        Multiply,
        Add,
        Subtract
    }

    [Serializable]
    sealed class StyleModuleMask
    {
        public StyleMaskKind kind = StyleMaskKind.Noise;
        public StyleMaskBlend blend;
        public bool invert;
        public float threshold = 0.5f;
        public float softness = 0.2f;
        public float warp;
        public float amount = 0.5f;
        public int scale = 4;
        public float size = 0.5f;
        public float extra = 0.3f;
        public int seed = 1;

        internal bool isShape => IsShape(kind);

        internal static bool IsShape(StyleMaskKind kind)
        {
            return kind < StyleMaskKind.Edges;
        }

        internal static StyleModuleMask Create(StyleMaskKind kind, int seed)
        {
            var mask = new StyleModuleMask { seed = seed };

            mask.SetKind(kind);
            return mask;
        }

        internal void SetKind(StyleMaskKind picked)
        {
            var basis = new StyleModuleMask();

            kind = picked;
            threshold = basis.threshold;
            softness = basis.softness;
            amount = basis.amount;
            scale = IsShape(picked) ? 8 : basis.scale;
            size = basis.size;
            extra = basis.extra;

            switch (picked)
            {
                case StyleMaskKind.Cracks:
                    scale = 6;
                    break;
                case StyleMaskKind.Scratches:
                    scale = 32;
                    break;
                case StyleMaskKind.Streaks:
                    scale = 24;
                    break;
                case StyleMaskKind.WoodGrain:
                    amount = 0.6f;
                    break;
                case StyleMaskKind.Chips:
                    amount = 0.4f;
                    scale = 24;
                    size = 0.4f;
                    break;
                case StyleMaskKind.Clumps:
                    amount = 0.6f;
                    scale = 32;
                    extra = 0f;
                    break;
                case StyleMaskKind.Leaves:
                    amount = 0.4f;
                    scale = 16;
                    extra = 0.15f;
                    break;
            }
        }
    }
    [Serializable]
    sealed class StyleColourStop
    {
        public Color colour = Color.white;
        public float at;
    }

    [Serializable]
    sealed class StyleColourRamp
    {
        public List<StyleColourStop> stops = new List<StyleColourStop>();

        internal const int MaxStops = 4;

        internal static StyleColourRamp Of(Color colour)
        {
            var ramp = new StyleColourRamp();

            ramp.stops.Add(new StyleColourStop { colour = Scaled(colour, 0.5f), at = 0f });
            ramp.stops.Add(new StyleColourStop { colour = colour, at = 0.5f });
            ramp.stops.Add(new StyleColourStop { colour = Scaled(colour, 1.5f), at = 1f });
            return ramp;
        }

        internal static StyleColourRamp Between(Color start, Color end)
        {
            var ramp = new StyleColourRamp();

            ramp.stops.Add(new StyleColourStop { colour = start, at = 0f });
            ramp.stops.Add(new StyleColourStop { colour = end, at = 1f });
            return ramp;
        }

        static Color Scaled(Color colour, float factor)
        {
            return new Color(Mathf.Clamp01(colour.r * factor), Mathf.Clamp01(colour.g * factor),
                             Mathf.Clamp01(colour.b * factor), colour.a);
        }

        internal Color Evaluate(float time)
        {
            if (stops.Count == 0)
                return Color.white;

            var colour = stops[0].colour;

            for (var i = 1; i < stops.Count; i++)
            {
                var span = Mathf.Max(stops[i].at - stops[i - 1].at, 1e-4f);

                colour = Color.Lerp(colour, stops[i].colour, Mathf.Clamp01((time - stops[i - 1].at) / span));
            }

            return colour;
        }

        internal int Write(Vector4[] colours, out Vector4 at)
        {
            var count = Mathf.Min(stops.Count, MaxStops);

            at = Vector4.zero;

            for (var i = 0; i < colours.Length; i++)
            {
                var stop = count > 0 ? stops[Mathf.Min(i, count - 1)] : new StyleColourStop();
                var linear = stop.colour.linear;

                colours[i] = new Vector4(linear.r, linear.g, linear.b, stop.colour.a);
                at[i] = stop.at;
            }

            return count;
        }

        internal Gradient ToGradient()
        {
            var count = Mathf.Max(stops.Count, 1);
            var colours = new GradientColorKey[count];
            var alphas = new GradientAlphaKey[count];

            for (var i = 0; i < count; i++)
            {
                var stop = i < stops.Count ? stops[i] : new StyleColourStop();

                colours[i] = new GradientColorKey(stop.colour, stop.at);
                alphas[i] = new GradientAlphaKey(stop.colour.a, stop.at);
            }

            var gradient = new Gradient();

            gradient.SetKeys(colours, alphas);
            return gradient;
        }

        internal void Set(Gradient gradient)
        {
            var keys = gradient.colorKeys;

            stops.Clear();

            for (var i = 0; i < keys.Length && i < MaxStops; i++)
            {
                var colour = keys[i].color;

                colour.a = gradient.Evaluate(keys[i].time).a;
                stops.Add(new StyleColourStop { colour = colour, at = keys[i].time });
            }

            stops.Sort((a, b) => a.at.CompareTo(b.at));
        }
    }

    [Serializable]
    sealed class StyleMaterialModule
    {
        public bool on = true;

        public StyleColourRamp colours = StyleColourRamp.Between(Color.black, Color.white);
        public float variation = 0.2f;
        public int seed = 1;

        public StyleModuleHeight height;
        public float depth = 0.1f;

        public float smoothness = 0.3f;
        public float metallic;
        public Color emission = Color.black;

        public StyleModuleArea area;
        public List<StyleModuleMask> masks = new List<StyleModuleMask>();

        internal const int MaxMasks = 4;

        internal StyleMaskKind? shape
        {
            get
            {
                foreach (var mask in masks)
                {
                    if (mask.isShape)
                        return mask.kind;
                }

                return null;
            }
        }

        internal StyleMaterialModule Clone()
        {
            return JsonUtility.FromJson<StyleMaterialModule>(JsonUtility.ToJson(this));
        }

        internal void Reset()
        {
            var keptOn = on;
            var keptMasks = masks;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new StyleMaterialModule()), this);
            on = keptOn;
            masks = keptMasks;
        }
    }
}