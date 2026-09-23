using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public static class CloudAtlasBaker
    {
        public static Color[] Bake(CloudAtlasSettings settings, int resolution)
        {
            var density = BuildDensity(settings, resolution);
            var height = Blur(density, resolution, SmoothingRadius(settings, resolution));

            var pixels = new Color[resolution * resolution];

            var reliefGain = resolution * 0.03f * settings.reliefStrength;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var index = y * resolution + x;

                    var left = height[y * resolution + Wrap(x - 1, resolution)];
                    var right = height[y * resolution + Wrap(x + 1, resolution)];
                    var down = height[Wrap(y - 1, resolution) * resolution + x];
                    var up = height[Wrap(y + 1, resolution) * resolution + x];

                    var nx = Mathf.Clamp((left - right) * reliefGain, -1f, 1f);
                    var ny = Mathf.Clamp((down - up) * reliefGain, -1f, 1f);

                    pixels[index] = new Color(
                        nx * 0.5f + 0.5f,
                        ny * 0.5f + 0.5f,
                        height[index],
                        density[index]);
                }
            }

            return pixels;
        }

        public static Color[] ShadePreview(Color[] atlas, int resolution, float coverage,
                                           float softness, out float visible)
        {
            var sunDirection = new Vector3(0.62f, 0.5f, -0.6f).normalized;

            var light = new Color(1f, 0.93f, 0.84f);
            var shade = new Color(0.46f, 0.44f, 0.56f);
            var sky = new Color(0.36f, 0.58f, 0.7f);

            var threshold = 1f - coverage;
            var soft = Mathf.Max(softness * 0.5f, 1e-4f);

            var pixels = new Color[atlas.Length];
            var total = 0f;

            for (var i = 0; i < atlas.Length; i++)
            {
                var alpha = SmoothStep01((atlas[i].a - (threshold - soft)) / (2f * soft));
                total += alpha;

                var relief = new Vector3(
                    atlas[i].r * 2f - 1f,
                    0.6f,
                    atlas[i].g * 2f - 1f).normalized;

                var lit = WrappedDiffuse(Vector3.Dot(relief, sunDirection), 0.5f, 0.6f);

                pixels[i] = Color.Lerp(sky, Color.Lerp(shade, light, lit), alpha);
            }

            visible = total / atlas.Length;
            return pixels;
        }

        static float[] BuildDensity(CloudAtlasSettings settings, int resolution)
        {
            var density = new float[resolution * resolution];

            var puffScale = Mathf.Max(2, settings.puffScale);
            var shapeScale = Mathf.Max(1, settings.shapeScale);
            var erosionScale = Mathf.Max(2, settings.erosionScale);

            var minimum = float.MaxValue;
            var maximum = float.MinValue;

            for (var y = 0; y < resolution; y++)
            {
                var v = (y + 0.5f) / resolution;

                for (var x = 0; x < resolution; x++)
                {
                    var u = (x + 0.5f) / resolution;

                    var billow = 1f - Worley(u, v, puffScale, settings.seed);
                    var drift = Fbm(u, v, shapeScale, settings.octaves, settings.seed + 5501);

                    var value = Mathf.Lerp(drift, billow * drift * 2f, settings.puffiness);

                    if (settings.erosion > 0f)
                    {
                        var detail = Fbm(u, v, erosionScale, 3, settings.seed + 9173);
                        value -= detail * settings.erosion;
                    }

                    density[y * resolution + x] = value;

                    minimum = Mathf.Min(minimum, value);
                    maximum = Mathf.Max(maximum, value);
                }
            }

            var span = Mathf.Max(maximum - minimum, 1e-4f);

            for (var i = 0; i < density.Length; i++)
            {
                var normalized = (density[i] - minimum) / span;
                density[i] = Mathf.Clamp01((normalized - 0.5f) * settings.contrast + 0.5f);
            }

            return density;
        }

        static int SmoothingRadius(CloudAtlasSettings settings, int resolution)
        {
            return Mathf.RoundToInt(Mathf.Lerp(1f, resolution * 0.025f, settings.reliefSmoothing));
        }

        static float[] Blur(float[] source, int resolution, int radius)
        {
            if (radius <= 0)
                return (float[])source.Clone();

            var horizontal = new float[source.Length];
            var result = new float[source.Length];
            var window = radius * 2 + 1;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var sum = 0f;

                    for (var offset = -radius; offset <= radius; offset++)
                        sum += source[y * resolution + Wrap(x + offset, resolution)];

                    horizontal[y * resolution + x] = sum / window;
                }
            }

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var sum = 0f;

                    for (var offset = -radius; offset <= radius; offset++)
                        sum += horizontal[Wrap(y + offset, resolution) * resolution + x];

                    result[y * resolution + x] = sum / window;
                }
            }

            return result;
        }

        static float Fbm(float u, float v, int baseFrequency, int octaves, int seed)
        {
            var sum = 0f;
            var amplitude = 1f;
            var total = 0f;
            var frequency = baseFrequency;

            for (var i = 0; i < octaves; i++)
            {
                sum += ValueNoise(u * frequency, v * frequency, frequency, seed + i * 131) * amplitude;
                total += amplitude;

                amplitude *= 0.5f;
                frequency *= 2;
            }

            return sum / total;
        }

        static float ValueNoise(float x, float y, int period, int seed)
        {
            var xi = Mathf.FloorToInt(x);
            var yi = Mathf.FloorToInt(y);

            var u = Fade(x - xi);
            var v = Fade(y - yi);

            var x0 = Wrap(xi, period);
            var x1 = Wrap(xi + 1, period);
            var y0 = Wrap(yi, period);
            var y1 = Wrap(yi + 1, period);

            var a = Hash(x0, y0, seed);
            var b = Hash(x1, y0, seed);
            var c = Hash(x0, y1, seed);
            var d = Hash(x1, y1, seed);

            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        static float Worley(float u, float v, int cells, int seed)
        {
            var x = u * cells;
            var y = v * cells;

            var xi = Mathf.FloorToInt(x);
            var yi = Mathf.FloorToInt(y);

            var nearest = 8f;

            for (var oy = -1; oy <= 1; oy++)
            {
                for (var ox = -1; ox <= 1; ox++)
                {
                    var cx = xi + ox;
                    var cy = yi + oy;

                    var wx = Wrap(cx, cells);
                    var wy = Wrap(cy, cells);

                    var dx = cx + Hash(wx, wy, seed) - x;
                    var dy = cy + Hash(wx, wy, seed + 7919) - y;

                    nearest = Mathf.Min(nearest, dx * dx + dy * dy);
                }
            }

            return Mathf.Clamp01(Mathf.Sqrt(nearest));
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

        static float WrappedDiffuse(float NdotL, float wrap, float softness)
        {
            var w = Mathf.Clamp01(wrap);
            var invW = 1f / (1f + w);

            var d = Mathf.Clamp01((NdotL + w) * invW);

            var terminator = w * invW;
            var halfBand = 0.5f * Mathf.Max(softness, 1e-4f);

            var e0 = Mathf.Max(terminator - halfBand, 0f);
            var e1 = Mathf.Max(Mathf.Min(terminator + halfBand, 1f), e0 + 1e-4f);

            return SmoothStep01((d - e0) / (e1 - e0));
        }

        static float Fade(float t) => t * t * (3f - 2f * t);

        static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static int Wrap(int value, int period) => ((value % period) + period) % period;
    }
}
