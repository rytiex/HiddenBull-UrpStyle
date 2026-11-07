using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleGrassQualityLevel
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    public enum HBStyleGrassBladeDetail
    {
        [InspectorName("Full (5 triangles)")]
        Full = 0,

        [InspectorName("Reduced (3 triangles)")]
        Reduced = 1,

        [InspectorName("Single (1 triangle)")]
        Single = 2
    }

    [Serializable]
    public struct HBStyleGrassLevel
    {
        [Tooltip("How far from the camera grass is drawn, in metres. Far grass uses fewer tufts, so the cost " +
                 "grows slowly with distance.")]
        [Min(10f)]
        public float distance;

        [Tooltip("Gap between two tufts near the camera at Grass Density 1, in metres. 0.1 is the thickest grass; " +
                 "it doubles with each ring away from the camera.")]
        [Range(0.1f, 1f)]
        public float spacing;

        [Tooltip("Triangles per grass leaf near the camera; the farther bands use one step less. Fewer triangles " +
                 "bend the leaf in coarser steps and save vertex work. Leaf count and width stay the same.")]
        public HBStyleGrassBladeDetail bladeDetail;

        public HBStyleGrassLevel(float distance, float spacing, HBStyleGrassBladeDetail bladeDetail)
        {
            this.distance = distance;
            this.spacing = spacing;
            this.bladeDetail = bladeDetail;
        }
    }

    [Serializable]
    public sealed class HBStyleGrassQuality
    {
        [SerializeField]
        [Tooltip("Active grass level: how far grass reaches and how thick it can grow. Off draws no grass.")]
        HBStyleGrassQualityLevel m_Level = HBStyleGrassQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleGrassLevel m_Low = new HBStyleGrassLevel(45f, 0.2f, HBStyleGrassBladeDetail.Single);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleGrassLevel m_Medium = new HBStyleGrassLevel(50f, 0.15f, HBStyleGrassBladeDetail.Reduced);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleGrassLevel m_High = new HBStyleGrassLevel(75f, 0.125f, HBStyleGrassBladeDetail.Full);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleGrassLevel m_Ultra = new HBStyleGrassLevel(100f, 0.1f, HBStyleGrassBladeDetail.Full);

        public HBStyleGrassQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleGrassLevel Get(HBStyleGrassQualityLevel level)
        {
            return level switch
            {
                HBStyleGrassQualityLevel.Low => m_Low,
                HBStyleGrassQualityLevel.Medium => m_Medium,
                HBStyleGrassQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleGrassLevel current => Get(m_Level);

        public bool enabled => m_Level != HBStyleGrassQualityLevel.Off;
    }
}
