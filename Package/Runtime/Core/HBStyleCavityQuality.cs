using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleCavityLevel
    {
        [Tooltip("How much of the Environment Profile's Cavity Distance the lines reach (0.25 to 1). A shorter " +
                 "reach leaves fewer pixels to work on.")]
        [Range(0.25f, 1f)]
        public float reach;

        public HBStyleCavityLevel(float reach)
        {
            this.reach = reach;
        }
    }

    [Serializable]
    public sealed class HBStyleCavityQuality
    {
        [SerializeField]
        [Tooltip("Active cavity level. Off draws no edge lines or corner darkening and skips the normals " +
                 "prepass they need; Low to Ultra set how far they reach.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleCavityLevel m_Low = new HBStyleCavityLevel(0.25f);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleCavityLevel m_Medium = new HBStyleCavityLevel(0.5f);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleCavityLevel m_High = new HBStyleCavityLevel(0.75f);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleCavityLevel m_Ultra = new HBStyleCavityLevel(1f);

        public HBStyleQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleCavityLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public float reach => m_Level == HBStyleQualityLevel.Off ? 0f : Mathf.Clamp(Get(m_Level).reach, 0.25f, 1f);
    }
}
