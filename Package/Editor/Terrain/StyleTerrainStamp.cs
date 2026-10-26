using System;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleTerrainBrushTool
    {
        Raise,
        Smooth,
        Flatten,
        SetHeight,
        Holes,
        PaintLayer,
        Noise,
        Terrace
    }

    sealed class StyleTerrainShape
    {
        readonly float[] m_Mask;
        readonly int m_Size;

        float m_Cos = 1f;
        float m_Sin;

        internal StyleTerrainShape(float[] mask, int size)
        {
            m_Mask = mask;
            m_Size = size;
        }

        internal void Rotate(float degrees)
        {
            var radians = degrees * Math.PI / 180.0;

            m_Cos = (float)Math.Cos(radians);
            m_Sin = (float)Math.Sin(radians);
        }

        internal float Weight(float dx, float dz, float reach)
        {
            var x = (dx * m_Cos + dz * m_Sin) / reach * 0.5f + 0.5f;
            var z = (dz * m_Cos - dx * m_Sin) / reach * 0.5f + 0.5f;

            if (x < 0f || x > 1f || z < 0f || z > 1f)
                return 0f;

            var fx = x * (m_Size - 1);
            var fz = z * (m_Size - 1);
            var x0 = Math.Min((int)fx, m_Size - 2);
            var z0 = Math.Min((int)fz, m_Size - 2);
            var tx = fx - x0;
            var tz = fz - z0;
            var bottom = m_Mask[z0 * m_Size + x0] * (1f - tx) + m_Mask[z0 * m_Size + x0 + 1] * tx;
            var top = m_Mask[(z0 + 1) * m_Size + x0] * (1f - tx) + m_Mask[(z0 + 1) * m_Size + x0 + 1] * tx;

            return bottom * (1f - tz) + top * tz;
        }
    }

    struct StyleTerrainStampSettings
    {
        public StyleTerrainBrushTool tool;
        public StyleTerrainShape shape;
        public float offsetX;
        public float offsetZ;
        public float reach;
        public float hardness;
        public float strength;
        public float amount;
        public float target;
        public float spacing;
        public float cornerX;
        public float cornerZ;
        public float noiseSize;
        public float step;
    }

    static class StyleTerrainStamp
    {
        const float TerraceSharpness = 6f;
        const int NoiseOctaves = 3;

        internal static float Falloff(float distance, float hardness)
        {
            if (distance >= 1f)
                return 0f;

            if (distance <= hardness)
                return 1f;

            var t = (distance - hardness) / (1f - hardness);

            return 1f - t * t * (3f - 2f * t);
        }

        internal static float Weight(StyleTerrainShape shape, float dx, float dz, float reach, float hardness)
        {
            if (shape != null)
                return shape.Weight(dx, dz, reach);

            return Falloff((float)Math.Sqrt(dx * dx + dz * dz) / reach, hardness);
        }

        internal static void Apply(float[,] heights, float[,] source, StyleTerrainStampSettings settings)
        {
            var length = heights.GetLength(0);
            var width = heights.GetLength(1);

            for (var z = 0; z < length; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var weight = Weight(settings.shape, settings.offsetX + x, settings.offsetZ + z, settings.reach,
                                        settings.hardness) * settings.strength;

                    if (weight <= 0f)
                        continue;

                    var value = heights[z, x];

                    switch (settings.tool)
                    {
                        case StyleTerrainBrushTool.Raise:
                            value += settings.amount * weight;
                            break;

                        case StyleTerrainBrushTool.Noise:
                            var worldX = settings.cornerX + x * settings.spacing;
                            var worldZ = settings.cornerZ + z * settings.spacing;

                            value += settings.amount * weight * Noise(worldX / settings.noiseSize,
                                                                      worldZ / settings.noiseSize);
                            break;

                        case StyleTerrainBrushTool.Smooth:
                            if (x == 0 || z == 0 || x == width - 1 || z == length - 1)
                                continue;

                            value += (Average(source, x, z) - value) * weight * settings.amount;
                            break;

                        case StyleTerrainBrushTool.Terrace:
                            value += (Terrace(value, settings.step) - value) * weight * settings.amount;
                            break;

                        default:
                            value += (settings.target - value) * weight * settings.amount;
                            break;
                    }

                    heights[z, x] = Math.Max(0f, Math.Min(1f, value));
                }
            }
        }

        internal static float Terrace(float value, float step)
        {
            if (step <= 0f)
                return value;

            var level = value / step;
            var floor = (float)Math.Floor(level);

            return step * (floor + (float)Math.Pow(level - floor, TerraceSharpness));
        }

        internal static float Noise(float x, float z)
        {
            var sum = 0f;
            var weight = 0.5f;
            var total = 0f;

            for (var octave = 0; octave < NoiseOctaves; octave++)
            {
                sum += Value(x, z, octave) * weight;
                total += weight;
                x = x * 2.03f + 17.1f;
                z = z * 2.03f + 31.7f;
                weight *= 0.5f;
            }

            return sum / total * 2f - 1f;
        }

        static float Value(float x, float z, int seed)
        {
            var ix = (int)Math.Floor(x);
            var iz = (int)Math.Floor(z);
            var fx = x - ix;
            var fz = z - iz;

            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);

            var bottom = Hash(ix, iz, seed) * (1f - fx) + Hash(ix + 1, iz, seed) * fx;
            var top = Hash(ix, iz + 1, seed) * (1f - fx) + Hash(ix + 1, iz + 1, seed) * fx;

            return bottom * (1f - fz) + top * fz;
        }

        static float Hash(int x, int z, int seed)
        {
            unchecked
            {
                var h = (uint)x * 374761393u + (uint)z * 668265263u + (uint)seed * 1274126177u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFFu) / 16777216f;
            }
        }

        internal static void Mix(byte[] weights, int count, int channel, float amount, bool erase)
        {
            var painted = Math.Min(255.0, Math.Ceiling(weights[channel] * (1.0 - amount) + amount * 255.0));

            for (var index = 0; index < count; index++)
                weights[index] = (byte)Math.Floor(weights[index] * (1.0 - amount));

            if (!erase)
                weights[channel] = (byte)painted;
        }

        static float Average(float[,] source, int x, int z)
        {
            var sum = 0f;

            for (var j = -1; j <= 1; j++)
            {
                for (var i = -1; i <= 1; i++)
                    sum += source[z + j, x + i];
            }

            return sum / 9f;
        }
    }
}
