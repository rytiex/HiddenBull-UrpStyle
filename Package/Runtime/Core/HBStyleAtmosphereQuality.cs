using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleAtmosphereLevel
    {
        [Tooltip("Steps the far distance into the Horizon Layers of the Environment Profile.")]
        public bool horizonLayers;

        [Tooltip("Lets the fog take on the sun's colour when looking toward it.")]
        public bool sunInscatter;

        public HBStyleAtmosphereLevel(bool horizonLayers, bool sunInscatter)
        {
            this.horizonLayers = horizonLayers;
            this.sunInscatter = sunInscatter;
        }
    }

    [Serializable]
    public sealed class HBStyleAtmosphereQuality
    {
        [SerializeField]
        [Tooltip("Active atmosphere level. Off turns the fog and aerial perspective off; Low to Ultra pick " +
                 "which optional terms run.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleAtmosphereLevel m_Low = new HBStyleAtmosphereLevel(false, false);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleAtmosphereLevel m_Medium = new HBStyleAtmosphereLevel(false, true);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleAtmosphereLevel m_High = new HBStyleAtmosphereLevel(true, true);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleAtmosphereLevel m_Ultra = new HBStyleAtmosphereLevel(true, true);

        public HBStyleQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleAtmosphereLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleAtmosphereLevel current => Get(m_Level);
    }
}
