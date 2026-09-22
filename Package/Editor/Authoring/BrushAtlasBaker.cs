using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public struct BrushAtlasStats
    {
        public float coverage;
    }

    public static class BrushAtlasBaker
    {
        public static Color[] Bake(BrushAtlasSettings settings, int resolution, out BrushAtlasStats stats)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            resolution = Mathf.Max(8, resolution);

            var coverage = BuildCoverage(settings, resolution);
            ApplyCanvasGrain(coverage, settings, resolution);

            stats = new BrushAtlasStats { coverage = Mean(coverage) };

            CentreAndContrast(coverage, settings.contrast);

            var warpRadius = Mathf.RoundToInt(settings.warpSpread * resolution);
            var smoothed = warpRadius > 0 ? BoxBlur(coverage, resolution, warpRadius) : coverage;

            return Encode(coverage, smoothed, resolution);
        }

        public static Color[] Bake(BrushAtlasSettings settings, int resolution)
        {
            return Bake(settings, resolution, out _);
        }

        static float[] BuildCoverage(BrushAtlasSettings settings, int resolution)
        {
            var coverage = new float[resolution * resolution];
            var random = new System.Random(settings.seed);

            var strokeCount = Mathf.Max(1, settings.strokeCount);
            var minLength = Mathf.Min(settings.lengthRange.x, settings.lengthRange.y);
            var maxLength = Mathf.Max(settings.lengthRange.x, settings.lengthRange.y);
            var minWidth = Mathf.Min(settings.widthRange.x, settings.widthRange.y);
            var maxWidth = Mathf.Max(settings.widthRange.x, settings.widthRange.y);

            for (var s = 0; s < strokeCount; s++)
            {
                var centre = new Vector2(NextFloat(random), NextFloat(random)) * resolution;

                var angleDegrees = settings.angle + (NextFloat(random) * 2f - 1f) * settings.angleJitter;
                var angleRadians = angleDegrees * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));

                var halfLength = Mathf.Lerp(minLength, maxLength, NextFloat(random)) * resolution * 0.5f;
                var width = Mathf.Max(1f, Mathf.Lerp(minWidth, maxWidth, NextFloat(random)) * resolution);
                var opacity = Mathf.Lerp(1f - settings.opacityVariation, 1f, NextFloat(random));

                DrawStroke(coverage, resolution, settings,
                    centre - direction * halfLength,
                    centre + direction * halfLength,
                    width, opacity, random.Next());
            }

            return coverage;
        }

        static void DrawStroke(float[] coverage, int resolution, BrushAtlasSettings settings,
                               Vector2 a, Vector2 b, float width, float opacity, int strokeSeed)
        {
            var ab = b - a;
            var length = ab.magnitude;
            if (length < 1e-3f)
                return;

            var direction = ab / length;
            var perpendicular = new Vector2(-direction.y, direction.x);

            var maxWidth = width * (1f + settings.edgeBreakup * 0.5f);

            var minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - maxWidth) - 1;
            var maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + maxWidth) + 1;
            var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - maxWidth) - 1;
            var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + maxWidth) + 1;

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var offset = point - a;

                    var along = Mathf.Clamp(Vector2.Dot(offset, direction), 0f, length);
                    var distance = Vector2.Distance(point, a + direction * along);

                    var localWidth = width;

                    if (settings.taper > 0f)
                    {
                        var fromEnd = Mathf.Min(along, length - along) / (length * 0.5f);
                        var shape = SmoothStep01(0f, Mathf.Max(settings.taper, 1e-3f), fromEnd);
                        localWidth *= Mathf.Lerp(1f - settings.taper, 1f, shape);
                    }

                    if (settings.edgeBreakup > 0f)
                    {
                        var wander = Noise1D(along / length * settings.edgeBreakupScale, strokeSeed) * 2f - 1f;
                        localWidth *= 1f + wander * settings.edgeBreakup * 0.5f;
                    }

                    if (localWidth <= 0.25f || distance >= localWidth)
                        continue;

                    var inner = Mathf.Min(localWidth * (1f - settings.edgeSoftness), localWidth - 1f);
                    var value = 1f - SmoothStep01(inner, localWidth, distance);
                    if (value <= 0f)
                        continue;

                    if (settings.bristleAmount > 0f)
                    {
                        var across = Vector2.Dot(offset, perpendicular);
                        var bristle = Noise1D(across * settings.bristleDensity / (2f * width),
                                              strokeSeed ^ 0x5bf03635);
                        value *= Mathf.Lerp(1f, bristle, settings.bristleAmount);
                    }

                    var wrappedX = Wrap(x, resolution);
                    var wrappedY = Wrap(y, resolution);

                    if (settings.pigmentAmount > 0f)
                    {
                        var pigment = TileableNoise(wrappedX, wrappedY, resolution,
                                                    settings.pigmentScale, settings.seed ^ 0x27d4eb2d);
                        value *= Mathf.Lerp(1f, pigment, settings.pigmentAmount);
                    }

                    var index = wrappedY * resolution + wrappedX;
                    coverage[index] = Mathf.Max(coverage[index], value * opacity);
                }
            }
        }

        static void ApplyCanvasGrain(float[] coverage, BrushAtlasSettings settings, int resolution)
        {
            if (settings.canvasAmount <= 0f)
                return;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var grain = TileableNoise(x, y, resolution, settings.canvasScale,
                                              settings.seed ^ 0x165667b1);

                    coverage[y * resolution + x] *= Mathf.Lerp(1f, grain, settings.canvasAmount);
                }
            }
        }

        static float Mean(float[] values)
        {
            var total = 0.0;
            for (var i = 0; i < values.Length; i++)
                total += values[i];

            return (float)(total / values.Length);
        }

        static void CentreAndContrast(float[] coverage, float contrast)
        {
            var total = 0.0;

            for (var i = 0; i < coverage.Length; i++)
            {
                var value = Mathf.Pow(Mathf.Clamp01(coverage[i]), Mathf.Max(0.25f, contrast));
                coverage[i] = value;
                total += value;
            }

            var mean = (float)(total / coverage.Length);
            var maxDeviation = 0f;

            for (var i = 0; i < coverage.Length; i++)
            {
                coverage[i] -= mean;
                maxDeviation = Mathf.Max(maxDeviation, Mathf.Abs(coverage[i]));
            }

            if (maxDeviation <= 1e-5f)
                return;

            var scale = 0.5f / maxDeviation;
            for (var i = 0; i < coverage.Length; i++)
                coverage[i] *= scale;
        }

        static float[] BoxBlur(float[] source, int resolution, int radius)
        {
            radius = Mathf.Clamp(radius, 1, resolution / 2 - 1);

            var window = radius * 2 + 1;
            var inverseWindow = 1f / window;

            var horizontal = new float[source.Length];
            var result = new float[source.Length];

            for (var y = 0; y < resolution; y++)
            {
                var row = y * resolution;
                var sum = 0f;

                for (var k = -radius; k <= radius; k++)
                    sum += source[row + Wrap(k, resolution)];

                for (var x = 0; x < resolution; x++)
                {
                    horizontal[row + x] = sum * inverseWindow;
                    sum += source[row + Wrap(x + radius + 1, resolution)]
                         - source[row + Wrap(x - radius, resolution)];
                }
            }

            for (var x = 0; x < resolution; x++)
            {
                var sum = 0f;

                for (var k = -radius; k <= radius; k++)
                    sum += horizontal[Wrap(k, resolution) * resolution + x];

                for (var y = 0; y < resolution; y++)
                {
                    result[y * resolution + x] = sum * inverseWindow;
                    sum += horizontal[Wrap(y + radius + 1, resolution) * resolution + x]
                         - horizontal[Wrap(y - radius, resolution) * resolution + x];
                }
            }

            return result;
        }

        static Color[] Encode(float[] coverage, float[] smoothed, int resolution)
        {
            var pixels = new Color[coverage.Length];
            var gradients = new Vector2[coverage.Length];
            var maxMagnitude = 0f;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var left = smoothed[y * resolution + Wrap(x - 1, resolution)];
                    var right = smoothed[y * resolution + Wrap(x + 1, resolution)];
                    var down = smoothed[Wrap(y - 1, resolution) * resolution + x];
                    var up = smoothed[Wrap(y + 1, resolution) * resolution + x];

                    var gradient = new Vector2(right - left, up - down) * 0.5f;
                    gradients[y * resolution + x] = gradient;
                    maxMagnitude = Mathf.Max(maxMagnitude, gradient.magnitude);
                }
            }

            var normalise = maxMagnitude > 1e-5f ? 1f / maxMagnitude : 0f;

            for (var i = 0; i < pixels.Length; i++)
            {
                var gradient = gradients[i] * normalise;

                pixels[i] = new Color(
                    gradient.x * 0.5f + 0.5f,
                    gradient.y * 0.5f + 0.5f,
                    Mathf.Clamp01(coverage[i] + 0.5f),
                    1f);
            }

            return pixels;
        }

        public static Color[] ExtractCoveragePreview(Color[] pixels)
        {
            var preview = new Color[pixels.Length];

            for (var i = 0; i < pixels.Length; i++)
            {
                var value = pixels[i].b;
                preview[i] = new Color(value, value, value, 1f);
            }

            return preview;
        }

        static float TileableNoise(int x, int y, int resolution, int cells, int seed)
        {
            cells = Mathf.Clamp(cells, 1, resolution);

            var amplitude = 1f;
            var total = 0f;
            var normalisation = 0f;

            for (var octave = 0; octave < 3; octave++)
            {
                var octaveCells = Mathf.Min(cells << octave, resolution);
                var scale = octaveCells / (float)resolution;

                total += LatticeNoise(x * scale, y * scale, octaveCells, seed + octave * 7919) * amplitude;
                normalisation += amplitude;
                amplitude *= 0.5f;
            }

            return total / normalisation;
        }

        static float LatticeNoise(float x, float y, int period, int seed)
        {
            var xi = Mathf.FloorToInt(x);
            var yi = Mathf.FloorToInt(y);
            var xf = x - xi;
            var yf = y - yi;

            var u = xf * xf * (3f - 2f * xf);
            var v = yf * yf * (3f - 2f * yf);

            var x0 = Wrap(xi, period);
            var x1 = Wrap(xi + 1, period);
            var y0 = Wrap(yi, period);
            var y1 = Wrap(yi + 1, period);

            var bottom = Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), u);
            var top = Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), u);

            return Mathf.Lerp(bottom, top, v);
        }

        static float Noise1D(float x, int seed)
        {
            var xi = Mathf.FloorToInt(x);
            var xf = x - xi;
            var u = xf * xf * (3f - 2f * xf);

            return Mathf.Lerp(Hash(xi, 0, seed), Hash(xi + 1, 0, seed), u);
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = x * 374761393 + y * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;

                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        static float SmoothStep01(float edge0, float edge1, float x)
        {
            var t = Mathf.Clamp01((x - edge0) / Mathf.Max(edge1 - edge0, 1e-5f));
            return t * t * (3f - 2f * t);
        }

        static int Wrap(int value, int size)
        {
            value %= size;
            return value < 0 ? value + size : value;
        }

        static float NextFloat(System.Random random) => (float)random.NextDouble();
    }
}
