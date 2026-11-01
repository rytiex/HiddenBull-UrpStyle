using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTerrainPlants
    {
        readonly List<StylePlantSystem.Placed> m_Placed = new List<StylePlantSystem.Placed>();
        readonly List<int> m_Counts = new List<int>();
        readonly Dictionary<uint, Spot> m_Spots = new Dictionary<uint, Spot>();
        readonly HashSet<uint> m_Gone = new HashSet<uint>();
        readonly StyleTerrainColliders m_Colliders = new StyleTerrainColliders();

        struct Spot
        {
            internal int type;
            internal int index;
        }

        HBStyleTerrainPlants m_Source;
        int m_Version = -1;
        Vector3 m_Origin;

        internal IReadOnlyCollection<uint> harvested => m_Gone;

        internal void Sync(HBStyleTerrain terrain, HBStyleTerrainPlants source, Vector3 origin)
        {
            if (source == m_Source && origin == m_Origin &&
                (source == null || (source.version == m_Version && Current(source))))
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

            if (Application.isPlaying)
                m_Colliders.Build(terrain, source.types, m_Placed);
        }

        internal bool Harvest(uint id)
        {
            if (!m_Gone.Add(id))
                return false;

            if (m_Spots.TryGetValue(id, out var spot))
                StylePlantSystem.Hide(m_Placed[spot.type], spot.index, true);

            m_Colliders.Show(id, false);

            return true;
        }

        internal bool Restore(uint id)
        {
            if (!m_Gone.Remove(id))
                return false;

            if (m_Spots.TryGetValue(id, out var spot))
                StylePlantSystem.Hide(m_Placed[spot.type], spot.index, false);

            m_Colliders.Show(id, true);

            return true;
        }

        bool Current(HBStyleTerrainPlants source)
        {
            var types = source.types;

            if (types.Count != m_Placed.Count)
                return false;

            for (var index = 0; index < m_Placed.Count; index++)
            {
                if (!ReferenceEquals(m_Placed[index].asset, types[index].asset))
                    return false;
            }

            return true;
        }

        internal void Release()
        {
            foreach (var placed in m_Placed)
                StylePlantSystem.Remove(placed);

            m_Colliders.Release();
            m_Spots.Clear();
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
            m_Spots.Clear();

            for (var index = 0; index < types.Count; index++)
                Prepare(m_Placed[index], types[index], m_Counts[index]);

            foreach (var record in source.records)
            {
                if (record.type < 0 || record.type >= types.Count)
                    continue;

                var placed = m_Placed[record.type];

                if (placed.asset != null)
                    Fill(placed, record.type, types[record.type].prefab.transform, record, origin);
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

            if (!ReferenceEquals(placed.asset, asset))
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
            placed.ids = new uint[capacity];
            placed.gone = new bool[capacity];
        }

        void Fill(StylePlantSystem.Placed placed, int type, Transform root, HBStylePlantRecord record, Vector3 origin)
        {
            var turn = Quaternion.Euler(0f, record.yaw, 0f) * root.localRotation;
            var world = Matrix4x4.TRS(origin + record.position, turn, root.localScale * record.size);

            placed.worlds[placed.count] = world;
            placed.inverses[placed.count] = world.inverse;
            placed.ids[placed.count] = record.id;
            placed.gone[placed.count] = m_Gone.Contains(record.id);
            m_Spots[record.id] = new Spot { type = type, index = placed.count };
            placed.count++;
        }
    }
}
