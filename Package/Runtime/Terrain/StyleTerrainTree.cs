using System;
using System.Collections.Generic;

namespace HiddenBull.UrpStyle
{
    struct StyleTerrainNode
    {
        public float x;
        public float z;
        public float size;
        public int level;
    }

    sealed class StyleTerrainTree
    {
        internal const int PatchQuads = 16;
        internal const int MaxLevels = 12;
        internal const float MorphShare = 0.25f;
        internal const float Unbounded = 1e9f;

        float[][] m_Min;
        float[][] m_Max;
        int[] m_Count;
        int m_Levels;
        float m_LeafSize;
        int m_Inner;
        int m_Blocks = 1;
        int m_Block;
        float m_Outer;
        float m_Margin;
        readonly float[] m_Ranges = new float[MaxLevels];

        internal int levels => m_Levels;

        internal float leafSize => m_LeafSize;

        internal float offset => m_Block * m_LeafSize * m_Count[0];

        internal float span => m_Blocks * m_LeafSize * m_Count[0];

        internal float Range(int level) => m_Ranges[level];

        internal static float MorphStart(float range) => range * (1f - MorphShare);

        internal float Spacing(int level) => m_LeafSize * (1 << level) / PatchQuads;

        internal bool Build(float[] leafMin, float[] leafMax, int leaves, float leafSize) =>
            Build(leafMin, leafMax, leaves, leafSize, 0, 0f, 0f);

        internal bool Build(float[] leafMin, float[] leafMax, int leaves, float leafSize, int extra, float outer,
                            float margin)
        {
            m_Levels = 0;

            if (leafMin == null || leafMax == null || leaves < 1 || leafMin.Length != leaves * leaves ||
                leafMax.Length != leaves * leaves || (leaves & (leaves - 1)) != 0)
                return false;

            var levels = 1;

            while ((1 << (levels - 1)) < leaves)
                levels++;

            if (levels > MaxLevels)
                return false;

            extra = Math.Max(Math.Min(extra, MaxLevels - levels), 0);

            m_Inner = levels;
            m_Blocks = 1 << extra;
            m_Block = extra > 0 ? m_Blocks / 2 - 1 : 0;
            m_Outer = outer;
            m_Margin = margin;
            m_Levels = levels + extra;
            m_LeafSize = leafSize;
            m_Min = new float[levels][];
            m_Max = new float[levels][];
            m_Count = new int[levels];
            m_Min[0] = (float[])leafMin.Clone();
            m_Max[0] = (float[])leafMax.Clone();
            m_Count[0] = leaves;

            for (var level = 1; level < levels; level++)
            {
                var below = m_Count[level - 1];
                var count = below / 2;
                var lowMin = m_Min[level - 1];
                var lowMax = m_Max[level - 1];

                m_Count[level] = count;
                m_Min[level] = new float[count * count];
                m_Max[level] = new float[count * count];

                for (var z = 0; z < count; z++)
                {
                    for (var x = 0; x < count; x++)
                    {
                        var a = 2 * z * below + 2 * x;
                        var b = a + below;

                        m_Min[level][z * count + x] = Math.Min(Math.Min(lowMin[a], lowMin[a + 1]),
                                                               Math.Min(lowMin[b], lowMin[b + 1]));
                        m_Max[level][z * count + x] = Math.Max(Math.Max(lowMax[a], lowMax[a + 1]),
                                                               Math.Max(lowMax[b], lowMax[b + 1]));
                    }
                }
            }

            return true;
        }

        internal void SetRanges(float first)
        {
            for (var level = 0; level < MaxLevels; level++)
                m_Ranges[level] = level >= m_Levels - 1 ? Unbounded : first * (1 << level);
        }

        internal void Select(float cameraX, float cameraY, float cameraZ, float[] planes, float reach,
                             List<StyleTerrainNode> output)
        {
            output.Clear();

            if (m_Levels > 0)
                Select(m_Levels - 1, 0, 0, cameraX, cameraY, cameraZ, planes, reach, output);
        }

        bool Select(int level, int x, int z, float cameraX, float cameraY, float cameraZ, float[] planes, float reach,
                    List<StyleTerrainNode> output)
        {
            var size = m_LeafSize * (1 << level);
            var minX = x * size - offset;
            var minZ = z * size - offset;

            Height(level, x, z, out var minY, out var maxY);

            if (minY > maxY)
                return true;

            var distance = Distance(cameraX, cameraY, cameraZ, minX, minY, minZ, minX + size, maxY, minZ + size);

            if (distance > m_Ranges[level])
                return false;

            if (distance > reach)
                return true;

            if (planes != null && !Visible(planes, minX, minY, minZ, minX + size, maxY, minZ + size))
                return true;

            if (level == 0 || distance > m_Ranges[level - 1])
            {
                output.Add(new StyleTerrainNode { x = minX, z = minZ, size = size, level = level });
                return true;
            }

            var half = size * 0.5f;

            for (var child = 0; child < 4; child++)
            {
                var childX = 2 * x + (child & 1);
                var childZ = 2 * z + (child >> 1);

                if (!Select(level - 1, childX, childZ, cameraX, cameraY, cameraZ, planes, reach, output))
                {
                    output.Add(new StyleTerrainNode
                    {
                        x = minX + (child & 1) * half,
                        z = minZ + (child >> 1) * half,
                        size = half,
                        level = level
                    });
                }
            }

            return true;
        }

        void Height(int level, int x, int z, out float minY, out float maxY)
        {
            minY = m_Outer - m_Margin;
            maxY = m_Outer + m_Margin;

            if (level < m_Inner)
            {
                var count = m_Count[level];
                var first = m_Block * count;

                if (x < first || z < first || x >= first + count || z >= first + count)
                    return;

                var index = (z - first) * count + x - first;

                minY = m_Min[level][index];
                maxY = m_Max[level][index];
                return;
            }

            var blocks = 1 << (level - m_Inner + 1);
            var top = m_Inner - 1;

            if (m_Block < x * blocks || m_Block >= (x + 1) * blocks || m_Block < z * blocks ||
                m_Block >= (z + 1) * blocks || m_Min[top][0] > m_Max[top][0])
                return;

            minY = Math.Min(minY, m_Min[top][0]);
            maxY = Math.Max(maxY, m_Max[top][0]);
        }

        internal static float Distance(float px, float py, float pz, float minX, float minY, float minZ, float maxX,
                                       float maxY, float maxZ)
        {
            var dx = Math.Max(Math.Max(minX - px, 0f), px - maxX);
            var dy = Math.Max(Math.Max(minY - py, 0f), py - maxY);
            var dz = Math.Max(Math.Max(minZ - pz, 0f), pz - maxZ);

            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        internal static bool Visible(float[] planes, float minX, float minY, float minZ, float maxX, float maxY,
                                     float maxZ)
        {
            for (var i = 0; i < planes.Length; i += 4)
            {
                var nx = planes[i];
                var ny = planes[i + 1];
                var nz = planes[i + 2];
                var px = nx >= 0f ? maxX : minX;
                var py = ny >= 0f ? maxY : minY;
                var pz = nz >= 0f ? maxZ : minZ;

                if (nx * px + ny * py + nz * pz + planes[i + 3] < 0f)
                    return false;
            }

            return true;
        }
    }
}
