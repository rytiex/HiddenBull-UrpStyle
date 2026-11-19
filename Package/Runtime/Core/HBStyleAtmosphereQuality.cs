using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleAtmosphereLevel
    {
        [Tooltip("Lets the fog take on the sun's colour when looking toward it.")]
        public bool sunInscatter;

        [Tooltip("Gathers the fog into the drifting patches set by Clumps in the Environment Profile.")]
        public bool clumps;

        public HBStyleAtmosphereLevel(bool sunInscatter, bool clumps)
        {
            this.sunInscatter = sunInscatter;
            this.clumps = clumps;
        }
    }

    [Serializable]
    public sealed class HBStyleAtmosphereQuality
    {
        [SerializeField]
        [Tooltip("Active atmosphere level. Off turns the fog off; Low to Ultra pick which " +
                 "optional terms run.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleAtmosphereLevel m_Low = new HBStyleAtmosphereLevel(false, false);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleAtmosphereLevel m_Medium = new HBStyleAtmosphereLevel(true, true);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleAtmosphereLevel m_High = new HBStyleAtmosphereLevel(true, true);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleAtmosphereLevel m_Ultra = new HBStyleAtmosphereLevel(true, true);

        public HBStyleQualityLevel level
        {
            get => HBStyleRenderSettings.isSet ? HBStyleRenderSettings.current.atmosphere : m_Level;
            set => m_Level = value;
        }

        internal HBStyleQualityLevel authoredLevel => m_Level;

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

        public HBStyleAtmosphereLevel current => Get(level);
    }
}
