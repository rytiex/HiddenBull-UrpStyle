using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public enum LiquidTextureMode
    {
        Water,
        Lava,
        Acid
    }

    public static class LiquidTextureBaker
    {
        const float TwoPi = Mathf.PI * 2f;
        const int WaveCount = 7;

        public struct Result
        {
            public Color[] normal;
            public Color[] crust;
            public float[] height;
        }

        public static int DefaultScale(LiquidTextureMode mode)
        {
            switch (mode)
            {
                case LiquidTextureMode.Lava: return 4;
                case LiquidTextureMode.Acid: return 5;
                default: return 6;
            }
        }

        public static Result Bake(LiquidTextureMode mode, int scale, float strength, int seed, int resolution)
        {
            var height = new float[resolution * resolution];
            var crust = mode == LiquidTextureMode.Lava ? new float[height.Length] : null;

            switch (mode)
            {
                case LiquidTextureMode.Lava:
                    BakeLava(height, crust, scale, seed, resolution);
                    break;
                case LiquidTextureMode.Acid:
                    BakeAcid(height, scale, seed, resolution);
                    break;
                default:
                    BakeWater(height, scale, seed, resolution);
                    break;
            }

            Normalise(height);

            var result = new Result
            {
                height = height,
                normal = BuildNormals(height, resolution, strength * 0.15f / Mathf.Max(scale, 1))
            };

            if (crust != null)
            {
                result.crust = new Color[crust.Length];

                for (var i = 0; i < crust.Length; i++)
                    result.crust[i] = new Color(crust[i], crust[i], crust[i], 1f);
            }

            return result;
        }

        static void BakeWater(float[] height, int scale, int seed, int resolution)
        {
            var waves = new Vector4[WaveCount];

            for (var i = 0; i < WaveCount; i++)
            {
                var angle = Hash(i, 0, seed) * TwoPi;
                var magnitude = scale * Mathf.Lerp(0.6f, 1.6f, Hash(i, 1, seed));

                var kx = Mathf.Round(Mathf.Cos(angle) * magnitude);
                var ky = Mathf.Round(Mathf.Sin(angle) * magnitude);

                if (kx == 0f && ky == 0f)
                    kx = 1f;

                waves[i] = new Vector4(kx, ky, Hash(i, 2, seed) * TwoPi, 1f / Mathf.Sqrt(kx * kx + ky * ky));
            }

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var u = (float)x / resolution;
                    var v = (float)y / resolution;
                    var h = 0f;

                    foreach (var wave in waves)
                    {
                        var crest = 0.5f + 0.5f * Mathf.Sin(TwoPi * (wave.x * u + wave.y * v) + wave.z);
                        h += crest * crest * wave.w;
                    }

                    h += Fbm(u, v, scale * 2, 3, seed + 11) * 0.08f;

                    height[y * resolution + x] = h;
                }
            }
        }

        static void BakeAcid(float[] height, int scale, int seed, int resolution)
        {
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var u = (float)x / resolution;
                    var v = (float)y / resolution;

                    var swell = Fbm(u, v, Mathf.Max(scale / 2, 1), 3, seed + 3);

                    Worley(u, v, scale, seed + 5, out var large, out _);
                    Worley(u, v, scale * 2, seed + 7, out var small, out _);

                    var dome = Dome(large, 0.45f) + Dome(small, 0.35f) * 0.5f;

                    height[y * resolution + x] = dome + swell * 0.6f;
                }
            }
        }

        static void BakeLava(float[] height, float[] crust, int scale, int seed, int resolution)
        {
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var u = (float)x / resolution;
                    var v = (float)y / resolution;

                    var reach = 1.5f / scale;

                    var h1 = Fbm(u, v, scale, 4, seed + 13);
                    var h2 = Fbm(u, v, scale, 4, seed + 17);

                    var g1 = Fbm(u + (h1 - 0.5f) * reach, v + (h2 - 0.5f) * reach, scale, 4, seed + 19);
                    var g2 = Fbm(u + (h2 - 0.5f) * reach, v + (h1 - 0.5f) * reach, scale, 4, seed + 23);

                    var flow = Fbm(u + (g1 - 0.5f) * reach * 1.4f, v + (g2 - 0.5f) * reach * 1.4f,
                                   scale * 2, 5, seed + 29);

                    var streak = 1f - Mathf.Abs(flow * 2f - 1f);
                    var cool = Mathf.Clamp01((flow - 0.35f) * 2f) * 0.75f + streak * streak * streak * 0.25f;

                    var index = y * resolution + x;

                    height[index] = cool + Fbm(u, v, scale * 8, 2, seed + 31) * 0.08f;
                    crust[index] = cool;
                }
            }
        }

        internal static float Dome(float distance, float radius)
        {
            var t = Mathf.Clamp01(1f - distance / radius);
            return t * t * (3f - 2f * t);
        }

        internal static Color[] BuildNormals(float[] height, int resolution, float slope)
        {
            var normals = new Color[height.Length];
            var scale = slope * resolution * 0.5f;

            for (var y = 0; y < resolution; y++)
            {
                var up = (y + 1) % resolution;
                var down = (y + resolution - 1) % resolution;

                for (var x = 0; x < resolution; x++)
                {
                    var right = (x + 1) % resolution;
                    var left = (x + resolution - 1) % resolution;

                    var dx = (height[y * resolution + right] - height[y * resolution + left]) * scale;
                    var dy = (height[up * resolution + x] - height[down * resolution + x]) * scale;

                    var normal = new Vector3(-dx, -dy, 1f).normalized;

                    normals[y * resolution + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f,
                                                            normal.z * 0.5f + 0.5f, 1f);
                }
            }

            return normals;
        }

        internal static void Normalise(float[] values)
        {
            var min = float.MaxValue;
            var max = float.MinValue;

            foreach (var value in values)
            {
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            var range = Mathf.Max(max - min, 1e-5f);

            for (var i = 0; i < values.Length; i++)
                values[i] = (values[i] - min) / range;
        }

        internal static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            var sum = 0f;
            var amplitude = 0.5f;
            var total = 0f;

            for (var i = 0; i < octaves; i++)
            {
                sum += ValueNoise(u * period, v * period, period, seed + i * 31) * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                period *= 2;
            }

            return sum / total;
        }

        internal static float ValueNoise(float x, float y, int period, int seed)
        {
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);
            var fx = x - ix;
            var fy = y - iy;

            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            var x0 = Wrap(ix, period);
            var y0 = Wrap(iy, period);
            var x1 = Wrap(ix + 1, period);
            var y1 = Wrap(iy + 1, period);

            var bottom = Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), fx);
            var top = Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), fx);

            return Mathf.Lerp(bottom, top, fy);
        }

        static void Worley(float u, float v, int period, int seed, out float first, out float second)
        {
            var x = u * period;
            var y = v * period;
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);

            first = float.MaxValue;
            second = float.MaxValue;

            for (var oy = -1; oy <= 1; oy++)
            {
                for (var ox = -1; ox <= 1; ox++)
                {
                    var cx = ix + ox;
                    var cy = iy + oy;
                    var wx = Wrap(cx, period);
                    var wy = Wrap(cy, period);

                    var px = cx + Hash(wx, wy, seed) * 0.9f + 0.05f;
                    var py = cy + Hash(wx, wy, seed + 1) * 0.9f + 0.05f;

                    var distance = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));

                    if (distance < first)
                    {
                        second = first;
                        first = distance;
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }
        }

        internal static int Wrap(int value, int period)
        {
            var wrapped = value % period;
            return wrapped < 0 ? wrapped + period : wrapped;
        }

        internal static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFF) / 16777216f;
            }
        }
    }
}
