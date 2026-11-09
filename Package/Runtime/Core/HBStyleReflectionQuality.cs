using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleReflectionLevel
    {
        [Tooltip("How many steps each pixel takes along its reflection to find what it mirrors. More steps reach " +
                 "farther and miss thin objects less often, at a higher cost.")]
        [Range(4, 48)]
        public int steps;

        public HBStyleReflectionLevel(int steps)
        {
            this.steps = steps;
        }
    }

    [Serializable]
    public sealed class HBStyleReflectionQuality
    {
        [SerializeField]
        [Tooltip("Active screen space reflection level for liquid surfaces. Off mirrors only the sky and the " +
                 "reflection probes; Low to Ultra also mirror the scene seen on screen.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleReflectionLevel m_Low = new HBStyleReflectionLevel(6);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleReflectionLevel m_Medium = new HBStyleReflectionLevel(10);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleReflectionLevel m_High = new HBStyleReflectionLevel(16);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleReflectionLevel m_Ultra = new HBStyleReflectionLevel(24);

        public HBStyleQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleReflectionLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public int steps => m_Level == HBStyleQualityLevel.Off ? 0 : Get(m_Level).steps;

        public int skyMapSize => m_Level >= HBStyleQualityLevel.High ? 512 : 256;
    }
}
