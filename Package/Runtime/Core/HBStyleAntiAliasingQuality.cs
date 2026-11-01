using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleAntiAliasingLevel
    {
        [Tooltip("How far along an edge the filter looks for its end. More steps smooth long, shallow edges better.")]
        [Range(2, 12)]
        public int edgeSteps;

        [Tooltip("How much pixel-sized details such as far grass and thin branches are softened.")]
        [Range(0f, 1f)]
        public float softness;

        public HBStyleAntiAliasingLevel(int edgeSteps, float softness)
        {
            this.edgeSteps = edgeSteps;
            this.softness = softness;
        }
    }

    [Serializable]
    public sealed class HBStyleAntiAliasingQuality
    {
        [SerializeField]
        [Tooltip("Active anti aliasing level. Off turns it off; Low to Ultra smooth edges with more care.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleAntiAliasingLevel m_Low = new HBStyleAntiAliasingLevel(4, 0.5f);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleAntiAliasingLevel m_Medium = new HBStyleAntiAliasingLevel(6, 0.6f);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleAntiAliasingLevel m_High = new HBStyleAntiAliasingLevel(8, 0.75f);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleAntiAliasingLevel m_Ultra = new HBStyleAntiAliasingLevel(12, 0.75f);

        public HBStyleQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleAntiAliasingLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleAntiAliasingLevel current => Get(m_Level);
    }
}
