using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleTerrainQualityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    [Serializable]
    public struct HBStyleTerrainLevel
    {
        [Tooltip("Distance from the camera, in metres, of the first detail ring. The terrain is drawn at the full " +
                 "heightmap resolution out to three quarters of it and each farther ring is twice as long with " +
                 "half the density. Below 96 m very steep cliffs can show hairline cracks between rings.")]
        [Min(96f)]
        public float detailRange;

        public HBStyleTerrainLevel(float detailRange)
        {
            this.detailRange = detailRange;
        }
    }

    [Serializable]
    public sealed class HBStyleTerrainQuality
    {
        [SerializeField]
        [Tooltip("Active terrain level: how fast the triangle density drops with distance. The ground near the " +
                 "camera is always drawn at full resolution, so it matches the collider on every level.")]
        HBStyleTerrainQualityLevel m_Level = HBStyleTerrainQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleTerrainLevel m_Low = new HBStyleTerrainLevel(96f);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleTerrainLevel m_Medium = new HBStyleTerrainLevel(112f);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleTerrainLevel m_High = new HBStyleTerrainLevel(128f);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleTerrainLevel m_Ultra = new HBStyleTerrainLevel(160f);

        public HBStyleTerrainQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleTerrainLevel Get(HBStyleTerrainQualityLevel level)
        {
            return level switch
            {
                HBStyleTerrainQualityLevel.Low => m_Low,
                HBStyleTerrainQualityLevel.Medium => m_Medium,
                HBStyleTerrainQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleTerrainLevel current => Get(m_Level);
    }
}
