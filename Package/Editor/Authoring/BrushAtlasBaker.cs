using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public struct BrushAtlasStats
    {
        public float coverage;
    }

    public struct BrushAtlasResult
    {
        public Color[] pixels;
        public BrushAtlasStats stats;
    }

    public static class BrushAtlasBaker
    {
        public static BrushAtlasResult BakeResult(BrushAtlasSettings settings, int resolution)
        {
            var pixels = Bake(settings, resolution, out var stats);

            return new BrushAtlasResult { pixels = pixels, stats = stats };
        }

        public static Color[] Bake(BrushAtlasSettings settings, int resolution, out BrushAtlasStats stats)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            resolution = Mathf.Max(8, resolution);

            var coverage = BuildCoverage(settings, resolution, out var spine);
            ApplyCanvasGrain(coverage, settings, resolution);

            stats = new BrushAtlasStats { coverage = Painted(coverage) };

            CentreAndContrast(coverage, settings.contrast);

            return Encode(coverage, spine, resolution);
        }

        public static Color[] Bake(BrushAtlasSettings settings, int resolution)
        {
            return Bake(settings, resolution, out _);
        }

        const float Ground = 0.5f;

        public const float SpineRange = 0.0625f;

        struct Stroke
        {
            public Vector2 start;
            public Vector2 control;
            public Vector2 end;
            public float width;
            public float alpha;
            public float tone;
            public int seed;
            public int segments;
        }

        static float[] BuildCoverage(BrushAtlasSettings settings, int resolution, out Vector2[] spine)
        {
            var coverage = new float[resolution * resolution];
            var field = new Vector2[resolution * resolution];
            var random = new System.Random(settings.seed);

            for (var i = 0; i < coverage.Length; i++)
                coverage[i] = Ground;

            var strokeCount = Mathf.Max(1, settings.strokeCount);
            var minLength = Mathf.Min(settings.lengthRange.x, settings.lengthRange.y);
            var maxLength = Mathf.Max(settings.lengthRange.x, settings.lengthRange.y);
            var minWidth = Mathf.Min(settings.widthRange.x, settings.widthRange.y);
            var maxWidth = Mathf.Max(settings.widthRange.x, settings.widthRange.y);

            var strokes = new Stroke[strokeCount];
            var segments = 1 + Mathf.RoundToInt(settings.curvature * 7f);

            for (var s = 0; s < strokeCount; s++)
            {
                var centre = new Vector2(NextFloat(random), NextFloat(random)) * resolution;

                var angleDegrees = settings.angle + (NextFloat(random) * 2f - 1f) * settings.angleJitter;
                var angleRadians = angleDegrees * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));

                var halfLength = Mathf.Lerp(minLength, maxLength, NextFloat(random)) * resolution * 0.5f;

                var start = centre - direction * halfLength;
                var end = centre + direction * halfLength;

                var perpendicular = new Vector2(-direction.y, direction.x);
                var bend = (NextFloat(random) * 2f - 1f) * settings.curvature * halfLength;

                strokes[s] = new Stroke
                {
                    start = start,
                    control = centre + perpendicular * bend,
                    end = end,
                    width = Mathf.Max(1f, Mathf.Lerp(minWidth, maxWidth, NextFloat(random)) * resolution),
                    alpha = Mathf.Lerp(1f - settings.opacityVariation, 1f, NextFloat(random)),
                    tone = Ground + (NextFloat(random) * 2f - 1f) * settings.toneVariation * Ground,
                    seed = random.Next(),
                    segments = segments
                };
            }

            var pigment = BuildPigment(settings, resolution);
            var bands = Mathf.Clamp(Environment.ProcessorCount, 1, 16);
            var rowsPerBand = Mathf.CeilToInt(resolution / (float)bands);

            System.Threading.Tasks.Parallel.For(0, bands, band =>
            {
                var from = band * rowsPerBand;
                var to = Mathf.Min(from + rowsPerBand, resolution);
                var points = new Vector2[segments + 1];

                for (var s = 0; s < strokes.Length; s++)
                    DrawStroke(coverage, field, pigment, resolution, settings, strokes[s], from, to, points);
            });

            spine = field;

            return coverage;
        }

        static float[] BuildPigment(BrushAtlasSettings settings, int resolution)
        {
            if (settings.pigmentAmount <= 0f)
                return null;

            var pigment = new float[resolution * resolution];

            System.Threading.Tasks.Parallel.For(0, resolution, y =>
            {
                for (var x = 0; x < resolution; x++)
                {
                    pigment[y * resolution + x] = TileableNoise(x, y, resolution,
                        settings.pigmentScale, settings.seed ^ 0x27d4eb2d);
                }
            });

            return pigment;
        }

        static void DrawStroke(float[] coverage, Vector2[] field, float[] pigment, int resolution,
                               BrushAtlasSettings settings, Stroke stroke, int fromRow, int toRow,
                               Vector2[] points)
        {
            var width = stroke.width;
            var strokeSeed = stroke.seed;
            var segments = Mathf.Min(Mathf.Max(stroke.segments, 1), points.Length - 1);

            for (var i = 0; i <= segments; i++)
                points[i] = Quadratic(stroke.start, stroke.control, stroke.end, i / (float)segments);

            var maxWidth = width * (1f + settings.edgeBreakup * 0.5f);

            var lowest = points[0];
            var highest = points[0];

            for (var i = 1; i <= segments; i++)
            {
                lowest = Vector2.Min(lowest, points[i]);
                highest = Vector2.Max(highest, points[i]);
            }

            var minX = Mathf.FloorToInt(lowest.x - maxWidth) - 1;
            var maxX = Mathf.CeilToInt(highest.x + maxWidth) + 1;
            var minY = Mathf.FloorToInt(lowest.y - maxWidth) - 1;
            var maxY = Mathf.CeilToInt(highest.y + maxWidth) + 1;

            for (var y = minY; y <= maxY; y++)
            {
                var wrappedY = Wrap(y, resolution);

                if (wrappedY < fromRow || wrappedY >= toRow)
                    continue;

                for (var x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);

                    var distance = float.MaxValue;
                    var along = 0f;
                    var across = 0f;
                    var nearest = point;

                    for (var i = 0; i < segments; i++)
                    {
                        var segment = points[i + 1] - points[i];
                        var segmentLength = segment.magnitude;

                        if (segmentLength < 1e-4f)
                            continue;

                        var tangent = segment / segmentLength;
                        var offset = point - points[i];
                        var local = Mathf.Clamp(Vector2.Dot(offset, tangent), 0f, segmentLength);
                        var onSpine = points[i] + tangent * local;
                        var candidate = Vector2.Distance(point, onSpine);

                        if (candidate >= distance)
                            continue;

                        distance = candidate;
                        nearest = onSpine;
                        along = (i + local / segmentLength) / segments;
                        across = offset.x * -tangent.y + offset.y * tangent.x;
                    }

                    if (distance == float.MaxValue)
                        continue;

                    var localWidth = width;

                    if (settings.taper > 0f)
                    {
                        var fromEnd = Mathf.Min(along, 1f - along) * 2f;
                        var shape = SmoothStep01(0f, Mathf.Max(settings.taper, 1e-3f), fromEnd);
                        localWidth *= Mathf.Lerp(1f - settings.taper, 1f, shape);
                    }

                    if (settings.edgeBreakup > 0f)
                    {
                        var wander = Noise1D(along * settings.edgeBreakupScale, strokeSeed) * 2f - 1f;
                        localWidth *= 1f + wander * settings.edgeBreakup * 0.5f;
                    }

                    if (localWidth <= 0.25f || distance >= localWidth)
                        continue;

                    var inner = Mathf.Min(localWidth * (1f - settings.edgeSoftness), localWidth - 1f);
                    var alpha = 1f - SmoothStep01(inner, localWidth, distance);
                    if (alpha <= 0f)
                        continue;

                    var body = alpha;

                    if (settings.bristleAmount > 0f)
                    {
                        var bristle = Noise1D(across * settings.bristleDensity / (2f * width),
                                              strokeSeed ^ 0x5bf03635);

                        if (settings.bristleBreakup > 0f)
                        {
                            var lift = Noise1D(along * settings.bristleDensity * 0.75f,
                                               strokeSeed ^ 0x1b873593);
                            bristle = Mathf.Lerp(bristle, bristle * lift, settings.bristleBreakup);
                        }

                        alpha *= Mathf.Lerp(1f, bristle, settings.bristleAmount);
                    }

                    var index = wrappedY * resolution + Wrap(x, resolution);

                    if (pigment != null)
                        alpha *= Mathf.Lerp(1f, pigment[index], settings.pigmentAmount);

                    coverage[index] = Mathf.Lerp(coverage[index], stroke.tone, alpha * stroke.alpha);
                    field[index] = Vector2.Lerp(field[index], nearest - point, body);
                }
            }
        }

        static Vector2 Quadratic(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            var inverse = 1f - t;

            return inverse * inverse * a + 2f * inverse * t * control + t * t * b;
        }

        static float Painted(float[] coverage)
        {
            var painted = 0;

            for (var i = 0; i < coverage.Length; i++)
            {
                if (Mathf.Abs(coverage[i] - Ground) > 0.02f)
                    painted++;
            }

            return painted / (float)coverage.Length;
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

        static Color[] Encode(float[] coverage, Vector2[] spine, int resolution)
        {
            var pixels = new Color[coverage.Length];
            var scale = 0.5f / (SpineRange * resolution);

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(
                    Mathf.Clamp01(0.5f + spine[i].x * scale),
                    Mathf.Clamp01(0.5f + spine[i].y * scale),
                    Mathf.Clamp01(coverage[i] + 0.5f),
                    1f);
            }

            return pixels;
        }

        public static Color[] ExtractSpinePreview(Color[] pixels)
        {
            var preview = new Color[pixels.Length];

            for (var i = 0; i < pixels.Length; i++)
                preview[i] = new Color(pixels[i].r, pixels[i].g, 0.5f, 1f);

            return preview;
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
