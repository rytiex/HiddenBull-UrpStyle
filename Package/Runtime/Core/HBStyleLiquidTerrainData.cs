using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [PreferBinarySerialization]
    public sealed class HBStyleLiquidTerrainData : ScriptableObject
    {
        public const int Size = 512;
        public const int Tiles = 8;
        public const int Blocks = 64;

        const byte Busy = 255;
        const byte Beside = 128;
        const float Flat = 0.01f;
        const float CoastFar = 64f;
        const float CoastSteps = 127f;

        [SerializeField]
        [HideInInspector]
        int m_Resolution = 512;

        [SerializeField]
        [HideInInspector]
        float[] m_Levels;

        [SerializeField]
        [HideInInspector]
        byte[] m_Weights;

        [SerializeField]
        [HideInInspector]
        float[] m_Flow;

        [SerializeField]
        [HideInInspector]
        byte[] m_Still;

        [SerializeField]
        [HideInInspector]
        float[] m_Bed;

        [SerializeField]
        [HideInInspector]
        float[] m_Calm;

        [SerializeField]
        [HideInInspector]
        sbyte[] m_Coast;

        Texture2D m_LevelMap;
        Texture2D m_WeightMap;
        float[] m_Packed;
        byte[] m_Inked;
        float m_Lowest;
        float m_Highest;
        bool m_Flowing;
        int m_Version;
        readonly float[] m_Lows = new float[Tiles * Tiles];
        readonly float[] m_Highs = new float[Tiles * Tiles];
        readonly bool[] m_Raised = new bool[Blocks * Blocks];
        readonly bool[] m_Daubed = new bool[Blocks * Blocks];
        readonly byte[] m_Sheeted = new byte[Blocks * Blocks];
        Texture2D m_BlockMap;

        public int resolution => m_Resolution;

        internal float lowest => m_Lowest;

        internal float highest => m_Highest;

        internal bool flowing => m_Flowing;

        internal int version => m_Version;

        internal ulong Sheets(int tile)
        {
            var across = Blocks / Tiles;
            var first = tile / Tiles * across * Blocks + tile % Tiles * across;
            var sheets = 0ul;

            for (var block = 0; block < across * across; block++)
            {
                if (m_Sheeted[first + block / across * Blocks + block % across] == Busy)
                    sheets |= 1ul << block;
            }

            return sheets;
        }

        internal Vector2 Range(int tile)
        {
            return new Vector2(m_Lows[tile], m_Highs[tile]);
        }

        internal float[] levels
        {
            get
            {
                if (m_Levels == null || m_Levels.Length != m_Resolution * m_Resolution)
                    m_Levels = new float[m_Resolution * m_Resolution];

                return m_Levels;
            }
        }

        internal float[] flow
        {
            get
            {
                if (m_Flow == null || m_Flow.Length != m_Resolution * m_Resolution * 2)
                    m_Flow = new float[m_Resolution * m_Resolution * 2];

                return m_Flow;
            }
        }

        internal byte[] still
        {
            get
            {
                if (m_Still == null || m_Still.Length != m_Resolution * m_Resolution)
                    m_Still = new byte[m_Resolution * m_Resolution];

                return m_Still;
            }
        }

        internal byte[] weights
        {
            get
            {
                if (m_Weights == null || m_Weights.Length != m_Resolution * m_Resolution * 4)
                    m_Weights = new byte[m_Resolution * m_Resolution * 4];

                return m_Weights;
            }
        }

        internal Texture2D weightMap
        {
            get
            {
                if (m_WeightMap == null || m_WeightMap.width != m_Resolution)
                    Upload();

                return m_WeightMap;
            }
        }

        internal Texture2D blockMap
        {
            get
            {
                if (m_BlockMap == null || m_LevelMap == null)
                    Upload();

                return m_BlockMap;
            }
        }

        internal Texture2D levelMap
        {
            get
            {
                if (m_LevelMap == null || m_LevelMap.width != m_Resolution)
                    Upload();

                return m_LevelMap;
            }
        }

        void Resize(int resolution)
        {
            if (resolution == m_Resolution)
                return;

            var resized = new float[resolution * resolution];
            var painted = new byte[resolution * resolution * 4];
            var moved = new float[resolution * resolution * 2];
            var run = flow;
            var old = weights;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    resized[y * resolution + x] = Sample((x + 0.5f) / resolution, (y + 0.5f) / resolution);

                    var from = (y * m_Resolution / resolution * m_Resolution + x * m_Resolution / resolution) * 4;

                    for (var channel = 0; channel < 4; channel++)
                        painted[(y * resolution + x) * 4 + channel] = old[from + channel];

                    moved[(y * resolution + x) * 2] = run[from / 2];
                    moved[(y * resolution + x) * 2 + 1] = run[from / 2 + 1];
                }
            }

            m_Resolution = resolution;
            m_Levels = resized;
            m_Weights = painted;
            m_Flow = moved;

            Upload();
        }

        internal float Sample(float u, float v)
        {
            var map = levels;
            var most = m_Resolution - 1;
            var x = Mathf.Clamp(u * m_Resolution - 0.5f, 0f, most);
            var y = Mathf.Clamp(v * m_Resolution - 0.5f, 0f, most);
            var column = Mathf.Min((int)x, most - 1);
            var row = Mathf.Min((int)y, most - 1);
            var first = row * m_Resolution + column;
            var second = first + m_Resolution;
            var across = x - column;

            return Mathf.Lerp(Mathf.Lerp(map[first], map[first + 1], across),
                              Mathf.Lerp(map[second], map[second + 1], across), y - row);
        }

        internal Vector3 Painted(float u, float v)
        {
            var map = weights;

            Corners(u, v, out var first, out var second, out var across, out var up);

            var painted = Vector3.zero;

            for (var channel = 0; channel < 3; channel++)
            {
                painted[channel] = Mathf.Lerp(
                    Mathf.Lerp(map[first * 4 + channel], map[(first + 1) * 4 + channel], across),
                    Mathf.Lerp(map[second * 4 + channel], map[(second + 1) * 4 + channel], across), up) / 255f;
            }

            return painted;
        }

        internal float Still(float u, float v)
        {
            var map = still;

            Corners(u, v, out var first, out var second, out var across, out var up);

            return Mathf.Lerp(Mathf.Lerp(map[first], map[first + 1], across),
                              Mathf.Lerp(map[second], map[second + 1], across), up) / 255f;
        }

        internal Vector2 Stream(float u, float v)
        {
            var map = flow;

            Corners(u, v, out var first, out var second, out var across, out var up);

            var stream = Vector2.zero;

            for (var axis = 0; axis < 2; axis++)
            {
                stream[axis] = Mathf.Lerp(
                    Mathf.Lerp(map[first * 2 + axis], map[(first + 1) * 2 + axis], across),
                    Mathf.Lerp(map[second * 2 + axis], map[(second + 1) * 2 + axis], across), up);
            }

            return stream;
        }

        void Corners(float u, float v, out int first, out int second, out float across, out float up)
        {
            var most = m_Resolution - 1;
            var x = Mathf.Clamp(u * m_Resolution - 0.5f, 0f, most);
            var y = Mathf.Clamp(v * m_Resolution - 0.5f, 0f, most);
            var column = Mathf.Min((int)x, most - 1);
            var row = Mathf.Min((int)y, most - 1);

            first = row * m_Resolution + column;
            second = first + m_Resolution;
            across = x - column;
            up = y - row;
        }

        internal bool Masked(float u, float v)
        {
            var x = Mathf.Clamp((int)(u * m_Resolution), 0, m_Resolution - 1);
            var y = Mathf.Clamp((int)(v * m_Resolution), 0, m_Resolution - 1);

            return weights[(y * m_Resolution + x) * 4 + 3] > 127;
        }

        internal bool grounded
        {
            get
            {
                var count = m_Resolution * m_Resolution;

                return m_Bed != null && m_Calm != null && m_Coast != null && m_Bed.Length == count
                    && m_Calm.Length == count && m_Coast.Length == count * 3;
            }
        }

        internal bool Ground(float u, float v, out float bed, out float calm)
        {
            bed = 0f;
            calm = 0f;

            if (u < 0f || v < 0f || u > 1f || v > 1f || !grounded)
                return false;

            Corners(u, v, out var first, out var second, out var across, out var up);

            bed = Mathf.Lerp(Mathf.Lerp(m_Bed[first], m_Bed[first + 1], across),
                             Mathf.Lerp(m_Bed[second], m_Bed[second + 1], across), up);
            calm = Mathf.Lerp(Mathf.Lerp(m_Calm[first], m_Calm[first + 1], across),
                              Mathf.Lerp(m_Calm[second], m_Calm[second + 1], across), up);

            return true;
        }

        internal bool Shore(float u, float v, out Vector3 field)
        {
            field = Vector3.zero;

            if (u < 0f || v < 0f || u > 1f || v > 1f || !grounded)
                return false;

            Corners(u, v, out var first, out var second, out var across, out var up);

            for (var channel = 0; channel < 3; channel++)
            {
                field[channel] = Mathf.Lerp(
                    Mathf.Lerp(m_Coast[first * 3 + channel], m_Coast[(first + 1) * 3 + channel], across),
                    Mathf.Lerp(m_Coast[second * 3 + channel], m_Coast[(second + 1) * 3 + channel], across), up)
                    / CoastSteps;
            }

            field.x *= CoastFar;

            return true;
        }

        internal bool Lay(float[] bed, float[] calm, float[] shore)
        {
            var coast = new sbyte[shore.Length];

            for (var index = 0; index < shore.Length; index++)
            {
                var share = index % 3 == 0 ? shore[index] / CoastFar : shore[index];

                coast[index] = (sbyte)Mathf.RoundToInt(Mathf.Clamp(share, -1f, 1f) * CoastSteps);
            }

            if (grounded && Same(m_Bed, bed) && Same(m_Calm, calm) && Same(m_Coast, coast))
                return false;

            m_Bed = bed;
            m_Calm = calm;
            m_Coast = coast;

            return true;
        }

        static bool Same<T>(T[] held, T[] given) where T : System.IEquatable<T>
        {
            if (held.Length != given.Length)
                return false;

            for (var index = 0; index < held.Length; index++)
            {
                if (!held[index].Equals(given[index]))
                    return false;
            }

            return true;
        }

        void Mark(bool[] marked, int x, int y)
        {
            var most = m_Resolution - 1;
            var left = Mathf.Max(x - 1, 0) * Blocks / m_Resolution;
            var right = Mathf.Min(x + 1, most) * Blocks / m_Resolution;

            for (var row = Mathf.Max(y - 1, 0) * Blocks / m_Resolution;
                 row <= Mathf.Min(y + 1, most) * Blocks / m_Resolution; row++)
            {
                for (var column = left; column <= right; column++)
                    marked[row * Blocks + column] = true;
            }
        }

        void Compose()
        {
            for (var block = 0; block < m_Sheeted.Length; block++)
                m_Sheeted[block] = m_Raised[block] ? Busy : (byte)0;

            for (var block = 0; block < m_Sheeted.Length; block++)
            {
                if (m_Sheeted[block] == Busy)
                    continue;

                var x = block % Blocks;
                var y = block / Blocks;

                for (var side = 0; side < 9; side++)
                {
                    var column = x + side % 3 - 1;
                    var row = y + side / 3 - 1;

                    if (column >= 0 && row >= 0 && column < Blocks && row < Blocks
                        && m_Sheeted[row * Blocks + column] == Busy)
                        m_Sheeted[block] = Beside;
                }
            }

            if (m_BlockMap == null)
            {
                m_BlockMap = new Texture2D(Blocks, Blocks, TextureFormat.R8, false, true)
                {
                    name = "Style Liquid Blocks",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
            }

            m_BlockMap.SetPixelData(m_Sheeted, 0);
            m_BlockMap.Apply(false, false);
        }

        void Measure()
        {
            var map = levels;
            var most = m_Resolution - 1;

            System.Array.Clear(m_Raised, 0, m_Raised.Length);

            m_Lowest = 0f;
            m_Highest = 0f;

            for (var tile = 0; tile < m_Lows.Length; tile++)
            {
                m_Lows[tile] = 0f;
                m_Highs[tile] = 0f;
            }

            for (var y = 0; y < m_Resolution; y++)
            {
                var bottom = Mathf.Max(y - 1, 0) * Tiles / m_Resolution;
                var top = Mathf.Min(y + 1, most) * Tiles / m_Resolution;

                for (var x = 0; x < m_Resolution; x++)
                {
                    var value = map[y * m_Resolution + x];
                    var left = Mathf.Max(x - 1, 0) * Tiles / m_Resolution;
                    var right = Mathf.Min(x + 1, most) * Tiles / m_Resolution;

                    m_Lowest = Mathf.Min(m_Lowest, value);
                    m_Highest = Mathf.Max(m_Highest, value);

                    if (Mathf.Abs(value) > Flat)
                        Mark(m_Raised, x, y);

                    for (var row = bottom; row <= top; row++)
                    {
                        for (var column = left; column <= right; column++)
                        {
                            var tile = row * Tiles + column;

                            m_Lows[tile] = Mathf.Min(m_Lows[tile], value);
                            m_Highs[tile] = Mathf.Max(m_Highs[tile], value);
                        }
                    }
                }
            }

            Compose();
            m_Version++;
        }

        internal void UploadLevels()
        {
            var map = levels;

            if (m_LevelMap == null || m_Packed == null || m_Packed.Length != map.Length * 4)
            {
                Upload();
                return;
            }

            for (var index = 0; index < map.Length; index++)
                m_Packed[index * 4] = map[index];

            Measure();
            m_LevelMap.SetPixelData(m_Packed, 0);
            m_LevelMap.Apply(false, false);
        }

        internal void Upload()
        {
            if (m_Resolution != Size)
            {
                Resize(Size);
                return;
            }

            var map = levels;
            var painted = weights;
            var run = flow;
            var calmed = still;
            var most = m_Resolution - 1;

            for (var index = 0; index < m_Resolution; index++)
            {
                map[index] = 0f;
                map[most * m_Resolution + index] = 0f;
                map[index * m_Resolution] = 0f;
                map[index * m_Resolution + most] = 0f;
                calmed[index] = 0;
                calmed[most * m_Resolution + index] = 0;
                calmed[index * m_Resolution] = 0;
                calmed[index * m_Resolution + most] = 0;

                for (var axis = 0; axis < 2; axis++)
                {
                    run[index * 2 + axis] = 0f;
                    run[(most * m_Resolution + index) * 2 + axis] = 0f;
                    run[index * m_Resolution * 2 + axis] = 0f;
                    run[(index * m_Resolution + most) * 2 + axis] = 0f;
                }

                for (var channel = 0; channel < 4; channel++)
                {
                    painted[index * 4 + channel] = 0;
                    painted[(most * m_Resolution + index) * 4 + channel] = 0;
                    painted[index * m_Resolution * 4 + channel] = 0;
                    painted[(index * m_Resolution + most) * 4 + channel] = 0;
                }
            }

            if (m_Inked == null || m_Inked.Length != painted.Length)
                m_Inked = new byte[painted.Length];

            System.Array.Copy(painted, m_Inked, painted.Length);

            for (var index = 0; index < calmed.Length; index++)
                m_Inked[index * 4 + 3] = calmed[index];

            if (m_Packed == null || m_Packed.Length != map.Length * 4)
                m_Packed = new float[map.Length * 4];

            Measure();
            m_Flowing = false;

            for (var index = 0; index < map.Length; index++)
            {
                m_Flowing |= run[index * 2] != 0f || run[index * 2 + 1] != 0f;
                m_Packed[index * 4] = map[index];
                m_Packed[index * 4 + 1] = painted[index * 4 + 3] / 255f;
                m_Packed[index * 4 + 2] = run[index * 2];
                m_Packed[index * 4 + 3] = run[index * 2 + 1];
            }

            System.Array.Clear(m_Daubed, 0, m_Daubed.Length);

            for (var index = 0; index < map.Length; index++)
            {
                if (run[index * 2] == 0f && run[index * 2 + 1] == 0f && calmed[index] == 0
                    && (painted[index * 4] | painted[index * 4 + 1] | painted[index * 4 + 2]
                        | painted[index * 4 + 3]) == 0)
                    continue;

                Mark(m_Daubed, index % m_Resolution, index / m_Resolution);
            }

            Compose();

            if (m_LevelMap != null && m_LevelMap.width != m_Resolution)
            {
                CoreUtils.Destroy(m_LevelMap);
                CoreUtils.Destroy(m_WeightMap);
                m_LevelMap = null;
                m_WeightMap = null;
            }

            if (m_LevelMap == null)
                m_LevelMap = Map("Style Liquid Level", TextureFormat.RGBAFloat);

            if (m_WeightMap == null)
                m_WeightMap = Map("Style Liquid Weights", TextureFormat.RGBA32);

            m_LevelMap.SetPixelData(m_Packed, 0);
            m_LevelMap.Apply(false, false);
            m_WeightMap.SetPixelData(m_Inked, 0);
            m_WeightMap.Apply(false, false);
        }

        Texture2D Map(string title, TextureFormat format)
        {
            return new Texture2D(m_Resolution, m_Resolution, format, false, true)
            {
                name = title,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_LevelMap);
            CoreUtils.Destroy(m_WeightMap);
            CoreUtils.Destroy(m_BlockMap);

            m_LevelMap = null;
            m_WeightMap = null;
            m_BlockMap = null;
        }
    }
}
