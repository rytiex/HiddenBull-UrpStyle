using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTerrainPlants
    {
        readonly List<StylePlantSystem.Placed> m_Placed = new List<StylePlantSystem.Placed>();
        readonly List<int> m_Counts = new List<int>();

        HBStyleTerrainPlants m_Source;
        int m_Version = -1;
        Vector3 m_Origin;

        internal void Sync(HBStyleTerrainPlants source, Vector3 origin)
        {
            if (source == m_Source && origin == m_Origin && (source == null || source.version == m_Version))
                return;

            m_Source = source;
            m_Origin = origin;

            if (source == null)
            {
                Release();
                return;
            }

            m_Version = source.version;
            Build(source, origin);
        }

        internal void Release()
        {
            foreach (var placed in m_Placed)
                StylePlantSystem.Remove(placed);

            m_Placed.Clear();
            m_Source = null;
            m_Version = -1;
        }

        void Build(HBStyleTerrainPlants source, Vector3 origin)
        {
            var types = source.types;

            while (m_Placed.Count > types.Count)
            {
                StylePlantSystem.Remove(m_Placed[m_Placed.Count - 1]);
                m_Placed.RemoveAt(m_Placed.Count - 1);
            }

            while (m_Placed.Count < types.Count)
                m_Placed.Add(new StylePlantSystem.Placed());

            Count(source);

            for (var index = 0; index < types.Count; index++)
                Prepare(m_Placed[index], types[index], m_Counts[index]);

            foreach (var record in source.records)
            {
                if (record.type < 0 || record.type >= types.Count)
                    continue;

                var placed = m_Placed[record.type];

                if (placed.asset != null)
                    Fill(placed, types[record.type].prefab.transform, record, origin);
            }

            foreach (var placed in m_Placed)
                StylePlantSystem.Touch(placed);
        }

        void Count(HBStyleTerrainPlants source)
        {
            m_Counts.Clear();

            for (var index = 0; index < source.types.Count; index++)
                m_Counts.Add(0);

            foreach (var record in source.records)
            {
                if (record.type >= 0 && record.type < m_Counts.Count)
                    m_Counts[record.type]++;
            }
        }

        static void Prepare(StylePlantSystem.Placed placed, HBStyleTerrainPlantType type, int count)
        {
            var asset = type.asset;

            if (placed.asset != asset)
            {
                StylePlantSystem.Remove(placed);
                placed.asset = asset;
                placed.layer = type.prefab != null ? type.prefab.layer : 0;
                StylePlantSystem.Add(placed);
            }

            placed.count = 0;

            if (placed.worlds.Length >= count)
                return;

            var capacity = Mathf.NextPowerOfTwo(count);

            placed.worlds = new Matrix4x4[capacity];
            placed.inverses = new Matrix4x4[capacity];
        }

        static void Fill(StylePlantSystem.Placed placed, Transform root, HBStylePlantRecord record, Vector3 origin)
        {
            var turn = Quaternion.Euler(0f, record.yaw, 0f) * root.localRotation;
            var world = Matrix4x4.TRS(origin + record.position, turn, root.localScale * record.size);

            placed.worlds[placed.count] = world;
            placed.inverses[placed.count] = world.inverse;
            placed.count++;
        }
    }
}
