using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStylePrecipitationLevel
    {
        [Tooltip("Share of the falling rain drops and snow flakes that are drawn. 1 is the full amount.")]
        [Range(0.05f, 1f)]
        public float density;

        [Tooltip("Draws the second, far layer of rain and snow that fills the distance. Off keeps only the " +
                 "drops close to the camera.")]
        public bool far;

        public HBStylePrecipitationLevel(float density, bool far)
        {
            this.density = density;
            this.far = far;
        }
    }

    [Serializable]
    public sealed class HBStylePrecipitationQuality
    {
        [SerializeField]
        [Tooltip("How many falling rain drops and snow flakes are drawn. Off draws no falling rain or snow; wet " +
                 "and snowy surfaces stay.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.Ultra;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStylePrecipitationLevel m_Low = new HBStylePrecipitationLevel(0.3f, false);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStylePrecipitationLevel m_Medium = new HBStylePrecipitationLevel(0.5f, true);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStylePrecipitationLevel m_High = new HBStylePrecipitationLevel(0.75f, true);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStylePrecipitationLevel m_Ultra = new HBStylePrecipitationLevel(1f, true);

        public HBStyleQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStylePrecipitationLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStylePrecipitationLevel current => Get(m_Level);
    }
}
