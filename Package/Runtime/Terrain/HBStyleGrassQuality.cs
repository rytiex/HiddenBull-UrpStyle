using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleGrassQualityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    [Serializable]
    public struct HBStyleGrassLevel
    {
        [Tooltip("How far from the camera grass is drawn, in metres. It thins and widens toward the end and fades " +
                 "out over the last metres, so no border shows.")]
        [Min(10f)]
        public float distance;

        [Tooltip("Out to this distance, in metres, each tuft is two crossed cards; beyond it one card that faces " +
                 "the camera. About a third of Distance.")]
        [Min(0f)]
        public float nearDistance;

        [Tooltip("Gap between two tufts at Grass Density 1, in metres. 0.2 is a tuft about every 20 cm; larger " +
                 "values draw fewer tufts.")]
        [Range(0.1f, 1f)]
        public float spacing;

        public HBStyleGrassLevel(float distance, float nearDistance, float spacing)
        {
            this.distance = distance;
            this.nearDistance = nearDistance;
            this.spacing = spacing;
        }
    }

    [Serializable]
    public sealed class HBStyleGrassQuality
    {
        [SerializeField]
        [Tooltip("Active grass level: how far grass reaches and how thick it can grow.")]
        HBStyleGrassQualityLevel m_Level = HBStyleGrassQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleGrassLevel m_Low = new HBStyleGrassLevel(30f, 10f, 0.4f);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleGrassLevel m_Medium = new HBStyleGrassLevel(45f, 15f, 0.3f);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleGrassLevel m_High = new HBStyleGrassLevel(60f, 20f, 0.25f);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleGrassLevel m_Ultra = new HBStyleGrassLevel(80f, 25f, 0.2f);

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
    }
}
