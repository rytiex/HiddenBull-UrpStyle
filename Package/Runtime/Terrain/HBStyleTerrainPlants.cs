using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleTerrainPlantType
    {
        const float SizeLeast = 0.05f;

        [SerializeField]
        GameObject m_Prefab;

        [SerializeField]
        float m_Density = 0.5f;

        [SerializeField]
        float m_MinDistance = 4f;

        [SerializeField]
        float m_SizeLeast = 0.9f;

        [SerializeField]
        float m_SizeMost = 1.2f;

        [SerializeField]
        float m_Rotation = 360f;

        HBStyleTerrainPlantType()
        {
        }

        internal HBStyleTerrainPlantType(GameObject prefab)
        {
            m_Prefab = prefab;
        }

        public GameObject prefab
        {
            get => m_Prefab;
            internal set => m_Prefab = value;
        }

        public float density
        {
            get => m_Density;
            set => m_Density = Mathf.Clamp01(value);
        }

        public float minDistance
        {
            get => m_MinDistance;
            set => m_MinDistance = Mathf.Max(value, 0.1f);
        }

        public float sizeLeast
        {
            get => m_SizeLeast;
            set => m_SizeLeast = Mathf.Clamp(value, SizeLeast, m_SizeMost);
        }

        public float sizeMost
        {
            get => m_SizeMost;
            set => m_SizeMost = Mathf.Max(value, m_SizeLeast);
        }

        public float rotation
        {
            get => m_Rotation;
            set => m_Rotation = Mathf.Clamp(value, 0f, 360f);
        }

        public HBStylePlantAsset asset =>
            m_Prefab != null && m_Prefab.TryGetComponent<HBStylePlant>(out var plant) ? plant.asset : null;
    }

    [Serializable]
    internal struct HBStylePlantRecord
    {
        public uint id;
        public int type;
        public Vector3 position;
        public float yaw;
        public float size;
    }

    public sealed class HBStyleTerrainPlants : ScriptableObject
    {
        [SerializeField]
        List<HBStyleTerrainPlantType> m_Types = new List<HBStyleTerrainPlantType>();

        [SerializeField]
        List<HBStylePlantRecord> m_Records = new List<HBStylePlantRecord>();

        [SerializeField]
        uint m_NextId = 1u;

        [SerializeField]
        int m_Version;

        public IReadOnlyList<HBStyleTerrainPlantType> types => m_Types;

        public int count => m_Records.Count;

        internal List<HBStylePlantRecord> records => m_Records;

        internal int version => m_Version;

        internal void AddType(HBStyleTerrainPlantType type)
        {
            m_Types.Add(type);
        }

        internal void RemoveType(int index)
        {
            var kept = 0;

            for (var slot = 0; slot < m_Records.Count; slot++)
            {
                var record = m_Records[slot];

                if (record.type == index)
                    continue;

                if (record.type > index)
                    record.type--;

                m_Records[kept++] = record;
            }

            m_Records.RemoveRange(kept, m_Records.Count - kept);
            m_Types.RemoveAt(index);
        }

        internal void Plant(int type, Vector3 position, float yaw, float size)
        {
            m_Records.Add(new HBStylePlantRecord
            {
                id = m_NextId++,
                type = type,
                position = position,
                yaw = yaw,
                size = size
            });
        }

        internal void Edited()
        {
            m_Version++;
        }
    }
}
