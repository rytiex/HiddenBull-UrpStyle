using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class StyleTerrainSampler
    {
        readonly float[,] m_Heights;
        readonly int m_Resolution;
        readonly Vector3 m_Size;
        readonly Vector3 m_Origin;
        readonly float m_Step;

        public StyleTerrainSampler(Terrain terrain)
        {
            var data = terrain.terrainData;

            m_Resolution = data.heightmapResolution;
            m_Heights = data.GetHeights(0, 0, m_Resolution, m_Resolution);
            m_Size = data.size;
            m_Origin = terrain.GetPosition();
            m_Step = 1f / (m_Resolution - 1);
        }

        public float Height(float u, float v)
        {
            var x = Mathf.Clamp01(u) * (m_Resolution - 1);
            var z = Mathf.Clamp01(v) * (m_Resolution - 1);
            var x0 = Mathf.Min((int)x, m_Resolution - 2);
            var z0 = Mathf.Min((int)z, m_Resolution - 2);
            var fx = x - x0;
            var fz = z - z0;

            var bottom = Mathf.Lerp(m_Heights[z0, x0], m_Heights[z0, x0 + 1], fx);
            var top = Mathf.Lerp(m_Heights[z0 + 1, x0], m_Heights[z0 + 1, x0 + 1], fx);

            return Mathf.Lerp(bottom, top, fz) * m_Size.y + m_Origin.y;
        }

        public float Slope(float u, float v)
        {
            var left = Mathf.Max(u - m_Step, 0f);
            var right = Mathf.Min(u + m_Step, 1f);
            var down = Mathf.Max(v - m_Step, 0f);
            var up = Mathf.Min(v + m_Step, 1f);

            var dx = (Height(right, v) - Height(left, v)) / Mathf.Max((right - left) * m_Size.x, 1e-4f);
            var dz = (Height(u, up) - Height(u, down)) / Mathf.Max((up - down) * m_Size.z, 1e-4f);

            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
        }
    }
}
