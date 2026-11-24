using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleBloomSettings
    {
        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("How strongly bright areas glow. 0 turns bloom off.")]
        float m_Intensity = 1f;

        [SerializeField]
        [Range(0f, 4f)]
        [Tooltip("Brightness above which pixels start to glow. Lower values make more of the image glow.")]
        float m_Threshold = 0.9f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How gently pixels near the threshold start to glow. 0 is a hard cut, 1 a long, soft start.")]
        float m_Softness = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far the glow spreads. High values give the wide, hazy glow that fills a whole window frame.")]
        float m_Spread = 0.75f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the glow. A warm white matches sunlit interiors.")]
        Color m_Tint = new Color(1f, 0.96f, 0.9f, 1f);

        public float intensity
        {
            get => m_Intensity;
            set => m_Intensity = Mathf.Clamp(value, 0f, 2f);
        }

        public float threshold
        {
            get => m_Threshold;
            set => m_Threshold = Mathf.Clamp(value, 0f, 4f);
        }

        public float softness
        {
            get => m_Softness;
            set => m_Softness = Mathf.Clamp01(value);
        }

        public float spread
        {
            get => m_Spread;
            set => m_Spread = Mathf.Clamp01(value);
        }

        public Color tint
        {
            get => m_Tint;
            set => m_Tint = value;
        }
    }
}
