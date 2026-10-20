using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    public struct GrassClumpSettings
    {
        public int blades;
        public float height;
        public float heightVariation;
        public float width;
        public float fan;
        public float spread;
        public float baseShade;
        public float shadeVariation;
        public int seed;

        public static GrassClumpSettings Default => new GrassClumpSettings
        {
            blades = 16,
            height = 0.95f,
            heightVariation = 0.4f,
            width = 0.035f,
            fan = 0.4f,
            spread = 0.5f,
            baseShade = 0.7f,
            shadeVariation = 0.2f,
            seed = 1
        };
    }

    public enum GrassPetalShape
    {
        Mixed,
        Round,
        Pointed,
        Heart
    }

    [Serializable]
    public struct GrassFlowerSettings
    {
        public int variants;
        public GrassPetalShape shape;
        public int petalsMin;
        public int petalsMax;
        public int rings;
        public float petalLength;
        public float petalWidth;
        public float irregularity;
        public float centreSize;
        public float shading;
        public float petalVariation;
        public Color petalA;
        public Color petalB;
        public Color petalC;
        public Color petalD;
        public Color centre;
        public int seed;

        public static GrassFlowerSettings Default => new GrassFlowerSettings
        {
            variants = 4,
            shape = GrassPetalShape.Mixed,
            petalsMin = 5,
            petalsMax = 7,
            rings = 2,
            petalLength = 0.44f,
            petalWidth = 0.55f,
            irregularity = 0.25f,
            centreSize = 0.11f,
            shading = 0.45f,
            petalVariation = 0.2f,
            petalA = new Color(0.96f, 0.95f, 0.9f),
            petalB = new Color(0.86f, 0.24f, 0.28f),
            petalC = new Color(0.56f, 0.42f, 0.86f),
            petalD = new Color(0.97f, 0.8f, 0.25f),
            centre = new Color(0.95f, 0.7f, 0.15f),
            seed = 1
        };

        public Color Petal(int variant)
        {
            switch (variant & 3)
            {
                case 0: return petalA;
                case 1: return petalB;
                case 2: return petalC;
                default: return petalD;
            }
        }
    }

    public static class GrassTextureBaker
    {
        const float k_InnerRing = 0.62f;
        const float k_InnerLift = 1.12f;
        const float k_EdgeBand = 0.35f;
        const float k_EdgeShade = 0.82f;
        const float k_CentreRim = 0.7f;
        const int k_CentreDots = 9;

        struct Petal
        {
            public float angle;
            public float length;
            public float width;
            public float tone;
        }

        public static Color[] BakeClump(GrassClumpSettings settings, int resolution)
        {
            var pixels = new Color[resolution * resolution];
            var random = new System.Random(settings.seed);
            var blades = Mathf.Max(1, settings.blades);

            for (var index = 0; index < pixels.Length; index++)
                pixels[index] = new Color(1f, 1f, 1f, 0f);

            for (var blade = 0; blade < blades; blade++)
            {
                var spread = (blade + Next(random)) / blades * 2f - 1f;
                var root = 0.5f + spread * settings.spread * 0.5f;
                var lean = spread * settings.fan + (Next(random) - 0.5f) * settings.fan * 0.5f;
                var height = settings.height * (1f - settings.heightVariation * Next(random));
                var width = settings.width * (0.7f + 0.6f * Next(random));
                var shade = 1f - settings.shadeVariation * Next(random);

                for (var y = 0; y < resolution; y++)
                {
                    var v = (y + 0.5f) / resolution;

                    if (v > height)
                        break;

                    var along = v / height;
                    var centre = root + lean * along * along;
                    var half = width * Mathf.Pow(1f - along, 0.7f);
                    var value = shade * Mathf.Lerp(settings.baseShade, 1f, along);

                    var from = Mathf.Max(Mathf.FloorToInt((centre - half) * resolution) - 1, 0);
                    var to = Mathf.Min(Mathf.CeilToInt((centre + half) * resolution) + 1, resolution - 1);

                    for (var x = from; x <= to; x++)
                    {
                        var u = (x + 0.5f) / resolution;
                        var cover = Mathf.Clamp01((half - Mathf.Abs(u - centre)) * resolution + 0.5f);

                        if (cover <= 0f)
                            continue;

                        var pixel = pixels[y * resolution + x];
                        var alpha = Mathf.Max(pixel.a, cover);
                        var tone = Mathf.Lerp(pixel.r, value, cover);

                        pixels[y * resolution + x] = new Color(tone, tone, tone, alpha);
                    }
                }
            }

            return pixels;
        }

        public static Color[] BakeFlowers(GrassFlowerSettings settings, int resolution)
        {
            var variants = Mathf.Clamp(settings.variants, 1, 8);
            var width = resolution * variants;
            var pixels = new Color[width * resolution];
            var random = new System.Random(settings.seed);
            var rings = Mathf.Clamp(settings.rings, 1, 3);

            for (var variant = 0; variant < variants; variant++)
            {
                var shape = settings.shape == GrassPetalShape.Mixed
                    ? (GrassPetalShape)(1 + random.Next(3))
                    : settings.shape;
                var count = random.Next(Mathf.Min(settings.petalsMin, settings.petalsMax),
                                        Mathf.Max(settings.petalsMin, settings.petalsMax) + 1);
                var colour = settings.Petal(variant);
                var petals = new Petal[count * rings];

                for (var ring = 0; ring < rings; ring++)
                {
                    var shrink = Mathf.Pow(k_InnerRing, ring);
                    var turn = Next(random) * Mathf.PI * 2f;
                    var step = Mathf.PI * 2f / count;

                    for (var index = 0; index < count; index++)
                    {
                        petals[ring * count + index] = new Petal
                        {
                            angle = turn + (index + (Next(random) - 0.5f) * settings.irregularity) * step,
                            length = settings.petalLength * shrink * (1f - settings.irregularity * 0.5f * Next(random)),
                            width = settings.petalWidth * (1f - settings.irregularity * 0.3f * Next(random)),
                            tone = Mathf.Pow(k_InnerLift, ring) * (1f - settings.petalVariation * Next(random))
                        };
                    }
                }

                for (var y = 0; y < resolution; y++)
                {
                    for (var x = 0; x < resolution; x++)
                    {
                        var point = new Vector2((x + 0.5f) / resolution - 0.5f, (y + 0.5f) / resolution - 0.5f);

                        pixels[y * width + variant * resolution + x] =
                            Flower(point, petals, shape, colour, settings, resolution);
                    }
                }
            }

            return pixels;
        }

        static Color Flower(Vector2 point, Petal[] petals, GrassPetalShape shape, Color colour,
                            GrassFlowerSettings settings, int resolution)
        {
            var result = new Color(colour.r, colour.g, colour.b, 0f);

            foreach (var petal in petals)
            {
                var along = point.x * Mathf.Cos(petal.angle) + point.y * Mathf.Sin(petal.angle);
                var across = -point.x * Mathf.Sin(petal.angle) + point.y * Mathf.Cos(petal.angle);
                var t = along / Mathf.Max(petal.length, 1e-4f);

                if (t <= 0f || t >= 1f)
                    continue;

                var half = petal.length * petal.width * 0.5f * Outline(shape, t);
                var cover = Mathf.Clamp01((half - Mathf.Abs(across)) * resolution + 0.5f);

                if (cover <= 0f)
                    continue;

                var edge = Mathf.Clamp01((half - Mathf.Abs(across)) / Mathf.Max(half * k_EdgeBand, 1e-4f));
                var shade = petal.tone
                          * Mathf.Lerp(1f - settings.shading, 1f, t)
                          * Mathf.Lerp(Mathf.Lerp(1f, k_EdgeShade, settings.shading), 1f, edge);
                var painted = new Color(Mathf.Clamp01(colour.r * shade), Mathf.Clamp01(colour.g * shade),
                                        Mathf.Clamp01(colour.b * shade), 1f);

                result = Color.Lerp(result, painted, cover);
                result.a = Mathf.Max(result.a, cover);
            }

            var radius = point.magnitude;
            var core = Mathf.Clamp01((settings.centreSize - radius) * resolution + 0.5f);

            if (core > 0f)
            {
                var angle = Mathf.Atan2(point.y, point.x);
                var dots = 0.5f + 0.5f * Mathf.Cos(angle * k_CentreDots);
                var rim = Mathf.Clamp01(radius / Mathf.Max(settings.centreSize, 1e-4f));
                var tone = Mathf.Lerp(1f, Mathf.Lerp(1f, k_CentreRim, dots), rim * settings.shading * 2f);
                var disc = new Color(settings.centre.r * tone, settings.centre.g * tone, settings.centre.b * tone, 1f);

                result = Color.Lerp(result, disc, core);
                result.a = Mathf.Max(result.a, core);
            }

            return result;
        }

        static float Outline(GrassPetalShape shape, float t)
        {
            switch (shape)
            {
                case GrassPetalShape.Pointed:
                    return Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f)), 0.8f) * Mathf.Lerp(1.15f, 0.55f, t);

                case GrassPetalShape.Heart:
                    return Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 1.5f)), 0.6f) * (1f - 0.35f * Mathf.Pow(t, 6f));

                default:
                    var bulge = 2f * Mathf.Pow(t, 1.3f) - 1f;

                    return Mathf.Sqrt(Mathf.Clamp01(1f - bulge * bulge));
            }
        }

        static float Next(System.Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
