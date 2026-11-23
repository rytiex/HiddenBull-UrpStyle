using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleAmbientOcclusionLevel
    {
        [Tooltip("Directions searched around each pixel. More directions give smoother, less noisy shading.")]
        [Range(1, 4)]
        public int directions;

        [Tooltip("Depth samples along each direction. More samples catch thin objects better.")]
        [Range(1, 6)]
        public int samples;

        [Tooltip("Computes ambient occlusion at half the screen size. About four times cheaper, slightly softer.")]
        public bool halfResolution;

        public HBStyleAmbientOcclusionLevel(int directions, int samples, bool halfResolution)
        {
            this.directions = directions;
            this.samples = samples;
            this.halfResolution = halfResolution;
        }
    }

    [Serializable]
    public sealed class HBStyleAmbientOcclusionQuality
    {
        [SerializeField]
        [Tooltip("Active ambient occlusion level. Off turns it off; Low to Ultra search more carefully.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleAmbientOcclusionLevel m_Low = new HBStyleAmbientOcclusionLevel(1, 2, true);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleAmbientOcclusionLevel m_Medium = new HBStyleAmbientOcclusionLevel(2, 2, true);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleAmbientOcclusionLevel m_High = new HBStyleAmbientOcclusionLevel(2, 3, false);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleAmbientOcclusionLevel m_Ultra = new HBStyleAmbientOcclusionLevel(4, 3, false);

        public HBStyleQualityLevel level
        {
            get => HBStyleRenderSettings.isSet ? HBStyleRenderSettings.current.ambientOcclusion : m_Level;
            set => m_Level = value;
        }

        internal HBStyleQualityLevel authoredLevel => m_Level;

        public HBStyleAmbientOcclusionLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleAmbientOcclusionLevel current => Get(level);
    }
}
