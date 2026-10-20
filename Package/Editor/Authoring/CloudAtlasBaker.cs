using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public static class CloudAtlasBaker
    {
        public static Color[] Bake(CloudAtlasSettings settings, int resolution)
        {
            return Bake(settings, resolution, null, 0);
        }

        public static Color[] Bake(CloudAtlasSettings settings, int resolution,
                                   Color[] brush, int brushResolution)
        {
            var density = BuildDensity(settings, resolution, brush, brushResolution);
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

            if (brush != null && settings.paint > 0f)
                pixels = Paint(pixels, resolution, brush, brushResolution, settings);

            return pixels;
        }

        static Color[] Paint(Color[] source, int resolution, Color[] brush, int brushResolution,
                             CloudAtlasSettings settings)
        {
            var painted = new Color[source.Length];
            var reach = BrushAtlasBaker.SpineRange * settings.paint / Mathf.Max(settings.brushScale, 1e-3f);

            System.Threading.Tasks.Parallel.For(0, resolution, y =>
            {
                var v = (y + 0.5f) / resolution;

                for (var x = 0; x < resolution; x++)
                {
                    var u = (x + 0.5f) / resolution;
                    var stroke = SampleBrush(brush, brushResolution, u, v, settings.brushScale);

                    painted[y * resolution + x] = Bilinear(source, resolution,
                        u + (stroke.r * 2f - 1f) * reach,
                        v + (stroke.g * 2f - 1f) * reach);
                }
            });

            return painted;
        }

        static Color Bilinear(Color[] pixels, int resolution, float u, float v)
        {
            var x = u * resolution - 0.5f;
            var y = v * resolution - 0.5f;

            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);

            var fx = x - x0;
            var fy = y - y0;

            var left = Wrap(x0, resolution);
            var right = Wrap(x0 + 1, resolution);
            var bottom = Wrap(y0, resolution) * resolution;
            var top = Wrap(y0 + 1, resolution) * resolution;

            return Color.Lerp(
                Color.Lerp(pixels[bottom + left], pixels[bottom + right], fx),
                Color.Lerp(pixels[top + left], pixels[top + right], fx),
                fy);
        }

        public static Color[] ShadePreview(Color[] atlas, int resolution, float coverage,
                                           float softness, out float visible)
        {
            var sunDirection = new Vector3(0.62f, 0.5f, -0.6f).normalized;

            var light = new Color(1f, 0.93f, 0.84f);
            var shade = new Color(0.46f, 0.44f, 0.56f);
            var sky = new Color(0.36f, 0.58f, 0.7f);

            var soft = Mathf.Max(softness * 0.5f, 1e-4f);
            var threshold = (1f - coverage) - coverage * soft;

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

        static float[] BuildDensity(CloudAtlasSettings settings, int resolution,
                                    Color[] brush, int brushResolution)
        {
            var density = new float[resolution * resolution];

            var puffScale = Mathf.Max(2, settings.puffScale);
            var shapeScale = Mathf.Max(1, settings.shapeScale);
            var erosionScale = Mathf.Max(2, settings.erosionScale);

            var bend = settings.warp * 0.5f;
            var warpScale = Mathf.Max(1, settings.warpScale);

            System.Threading.Tasks.Parallel.For(0, resolution, y =>
            {
                var row = (y + 0.5f) / resolution;

                for (var x = 0; x < resolution; x++)
                {
                    var u = (x + 0.5f) / resolution;
                    var v = row;

                    if (settings.warp > 0f)
                    {
                        var du = Fbm(u, v, warpScale, 2, settings.seed + 7717) - 0.5f;
                        var dv = Fbm(u, v, warpScale, 2, settings.seed + 3313) - 0.5f;

                        u += du * bend;
                        v += dv * bend;
                    }

                    var billow = 1f - Worley(u, v, puffScale, settings.seed);
                    var drift = Fbm(u, v, shapeScale, settings.octaves, settings.seed + 5501);

                    var value = Mathf.Lerp(drift, billow * drift * 2f, settings.puffiness);

                    if (settings.erosion > 0f)
                    {
                        var detail = Fbm(u, v, erosionScale, 3, settings.seed + 9173);
                        value -= detail * settings.erosion;
                    }

                    if (brush != null && settings.brushAmount > 0f)
                        value += (SampleBrush(brush, brushResolution, u, v, settings.brushScale).b - 0.5f)
                               * settings.brushAmount;

                    density[y * resolution + x] = value;
                }
            });

            var minimum = float.MaxValue;
            var maximum = float.MinValue;

            for (var i = 0; i < density.Length; i++)
            {
                minimum = Mathf.Min(minimum, density[i]);
                maximum = Mathf.Max(maximum, density[i]);
            }

            var span = Mathf.Max(maximum - minimum, 1e-4f);

            var power = Mathf.Max(0.25f, settings.contrast);

            for (var i = 0; i < density.Length; i++)
            {
                var normalized = (density[i] - minimum) / span;

                density[i] = normalized < 0.5f
                    ? 0.5f * Mathf.Pow(2f * normalized, power)
                    : 1f - 0.5f * Mathf.Pow(2f * (1f - normalized), power);
            }

            return density;
        }

        static Color SampleBrush(Color[] brush, int resolution, float u, float v, float scale)
        {
            var x = Wrap(Mathf.FloorToInt(u * scale * resolution), resolution);
            var y = Wrap(Mathf.FloorToInt(v * scale * resolution), resolution);

            return brush[y * resolution + x];
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
