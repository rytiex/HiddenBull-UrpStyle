using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStylePrecipitationQuality
    {
        Off,
        Low,
        Medium,
        High,
        Ultra
    }

    [Serializable]
    public struct HBStylePrecipitationLevel
    {
        [Tooltip("Share of the falling rain drops and snow flakes that are drawn. 1 is the full amount.")]
        [Range(0.05f, 1f)]
        public float density;

        [Tooltip("Draws the second, far layer of rain and snow that fills the distance. Off keeps only the drops " +
                 "close to the camera.")]
        public bool far;

        public HBStylePrecipitationLevel(float density, bool far)
        {
            this.density = density;
            this.far = far;
        }
    }

    [Serializable]
    public class HBStylePrecipitationQualitySettings
    {
        [SerializeField]
        [Tooltip("How many falling rain drops and snow flakes are drawn. Each level below carries its own numbers, " +
                 "so a build can offer the choice to its players. Off draws no falling rain or snow; wet and snowy " +
                 "surfaces stay.")]
        HBStylePrecipitationQuality m_Quality = HBStylePrecipitationQuality.Ultra;

        [SerializeField]
        HBStylePrecipitationLevel m_Low = new HBStylePrecipitationLevel(0.3f, false);

        [SerializeField]
        HBStylePrecipitationLevel m_Medium = new HBStylePrecipitationLevel(0.5f, true);

        [SerializeField]
        HBStylePrecipitationLevel m_High = new HBStylePrecipitationLevel(0.75f, true);

        [SerializeField]
        HBStylePrecipitationLevel m_Ultra = new HBStylePrecipitationLevel(1f, true);

        public HBStylePrecipitationQuality quality
        {
            get => m_Quality;
            set => m_Quality = value;
        }

        public bool TryGetLevel(out HBStylePrecipitationLevel level)
        {
            switch (m_Quality)
            {
                case HBStylePrecipitationQuality.Low:
                    level = m_Low;
                    return true;

                case HBStylePrecipitationQuality.Medium:
                    level = m_Medium;
                    return true;

                case HBStylePrecipitationQuality.High:
                    level = m_High;
                    return true;

                case HBStylePrecipitationQuality.Ultra:
                    level = m_Ultra;
                    return true;

                default:
                    level = default;
                    return false;
            }
        }
    }
}
