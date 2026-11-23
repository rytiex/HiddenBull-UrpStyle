using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleBloomLevel
    {
        [Tooltip("How many times the image is halved for the glow. More steps reach wider at a small cost.")]
        [Range(3, 8)]
        public int steps;

        [Tooltip("Smooths the first step so small bright points do not flicker as the camera moves.")]
        public bool stable;

        public HBStyleBloomLevel(int steps, bool stable)
        {
            this.steps = steps;
            this.stable = stable;
        }
    }

    [Serializable]
    public sealed class HBStyleBloomQuality
    {
        [SerializeField]
        [Tooltip("Active bloom level. Off turns bloom off; Low to Ultra let the glow reach wider and flicker less.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleBloomLevel m_Low = new HBStyleBloomLevel(4, false);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleBloomLevel m_Medium = new HBStyleBloomLevel(5, false);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleBloomLevel m_High = new HBStyleBloomLevel(6, true);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleBloomLevel m_Ultra = new HBStyleBloomLevel(7, true);

        public HBStyleQualityLevel level
        {
            get => HBStyleRenderSettings.isSet ? HBStyleRenderSettings.current.bloom : m_Level;
            set => m_Level = value;
        }

        internal HBStyleQualityLevel authoredLevel => m_Level;

        public HBStyleBloomLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleBloomLevel current => Get(level);
    }
}
