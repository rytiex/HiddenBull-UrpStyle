using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleFogNoise
    {
        internal const int CellsAcross = 16;
        internal const int CellsHigh = 8;

        const int TexelsPerCell = 4;
        const float CoarseShare = 0.65f;
        const float ShapeLow = 0.3f;
        const float ShapeHigh = 0.7f;

        internal static readonly int textureId = Shader.PropertyToID("_HB_FogNoise");

        internal static Texture3D Create()
        {
            var width = CellsAcross * TexelsPerCell;
            var height = CellsHigh * TexelsPerCell;
            var texels = new byte[width * height * width];
            var index = 0;

            for (var z = 0; z < width; z++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var px = (float)x / TexelsPerCell;
                        var py = (float)y / TexelsPerCell;
                        var pz = (float)z / TexelsPerCell;

                        var coarse = Value(px, py, pz, CellsAcross, CellsHigh, 0u);
                        var fine = Value(px * 2f, py * 2f, pz * 2f, CellsAcross * 2, CellsHigh * 2, 7919u);
                        var shape = Smooth(Mathf.InverseLerp(ShapeLow, ShapeHigh,
                                                             coarse * CoarseShare + fine * (1f - CoarseShare)));

                        texels[index++] = (byte)Mathf.RoundToInt(shape * 255f);
                    }
                }
            }

            var texture = new Texture3D(width, height, width, TextureFormat.R8, false)
            {
                name = "HB Style Fog Noise",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            texture.SetPixelData(texels, 0);
            texture.Apply(false, true);

            return texture;
        }

        static float Value(float x, float y, float z, int across, int high, uint seed)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var z0 = Mathf.FloorToInt(z);
            var tx = Smooth(x - x0);
            var ty = Smooth(y - y0);
            var tz = Smooth(z - z0);

            var c000 = Corner(x0, y0, z0, across, high, seed);
            var c100 = Corner(x0 + 1, y0, z0, across, high, seed);
            var c010 = Corner(x0, y0 + 1, z0, across, high, seed);
            var c110 = Corner(x0 + 1, y0 + 1, z0, across, high, seed);
            var c001 = Corner(x0, y0, z0 + 1, across, high, seed);
            var c101 = Corner(x0 + 1, y0, z0 + 1, across, high, seed);
            var c011 = Corner(x0, y0 + 1, z0 + 1, across, high, seed);
            var c111 = Corner(x0 + 1, y0 + 1, z0 + 1, across, high, seed);

            var near = Mathf.Lerp(Mathf.Lerp(c000, c100, tx), Mathf.Lerp(c010, c110, tx), ty);
            var far = Mathf.Lerp(Mathf.Lerp(c001, c101, tx), Mathf.Lerp(c011, c111, tx), ty);

            return Mathf.Lerp(near, far, tz);
        }

        static float Corner(int x, int y, int z, int across, int high, uint seed)
        {
            var h = (uint)(x % across) * 73856093u ^ (uint)(y % high) * 19349663u ^ (uint)(z % across) * 83492791u
                    ^ seed;

            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            h *= 0x846CA68Bu;
            h ^= h >> 16;

            return (h >> 8) * (1f / 16777216f);
        }

        static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
