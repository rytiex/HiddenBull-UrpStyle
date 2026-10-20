using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleTerrainNoise
    {
        const int Size = 64;

        static Texture3D s_Texture;

        public static readonly int MapId = Shader.PropertyToID("_HB_TerrainNoiseMap");

        public static Texture3D texture
        {
            get
            {
                if (s_Texture == null)
                    s_Texture = Build();

                return s_Texture;
            }
        }

        static float Hash(int x, int y, int z)
        {
            unchecked
            {
                var hash = (uint)x * 1597334673u ^ (uint)y * 3812015801u ^ (uint)z * 2798796415u;

                hash ^= hash >> 15;
                hash *= 0x2C1B3C6Du;
                hash ^= hash >> 12;
                hash *= 0x297A2D39u;
                hash ^= hash >> 15;

                return hash * (1f / 4294967296f);
            }
        }

        static Texture3D Build()
        {
            var values = new byte[Size * Size * Size];

            for (var z = 0; z < Size; z++)
            {
                for (var y = 0; y < Size; y++)
                {
                    for (var x = 0; x < Size; x++)
                        values[x + (y + z * Size) * Size] = (byte)Mathf.RoundToInt(Hash(x, y, z) * 255f);
                }
            }

            var built = new Texture3D(Size, Size, Size, TextureFormat.R8, false)
            {
                name = "HB Terrain Noise",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            built.SetPixelData(values, 0);
            built.Apply(false, true);

            return built;
        }
    }
}
