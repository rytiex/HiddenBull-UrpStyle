using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleWeatherQualityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    public enum HBStyleShelterResolution
    {
        [InspectorName("256 (0.63 m)")] Low = 256,
        [InspectorName("512 (0.31 m)")] Medium = 512,
        [InspectorName("1024 (0.16 m)")] High = 1024
    }

    [Serializable]
    public struct HBStyleWeatherLevel
    {
        [Tooltip("Size of the shelter map that decides what has open sky above it, 160 m around the camera. The " +
                 "text in brackets is the size of one texel: smaller finds narrower gaps and thinner eaves.")]
        public HBStyleShelterResolution shelterResolution;

        [Tooltip("Distance from the camera, in metres, out to which the small weather details are drawn: rain " +
                 "spots, ripples, running water, drops and flakes. Beyond it surfaces still get wet and snowed on.")]
        [Min(0f)]
        public float detailDistance;

        public HBStyleWeatherLevel(HBStyleShelterResolution shelterResolution, float detailDistance)
        {
            this.shelterResolution = shelterResolution;
            this.detailDistance = detailDistance;
        }
    }

    [Serializable]
    public sealed class HBStyleWeatherQuality
    {
        [SerializeField]
        [Tooltip("Active weather level: shelter map size and detail reach. To turn the weather off, set Rain and " +
                 "Snow to 0 on the HB Style Environment Controller; falling rain and snow have their own " +
                 "Precipitation level.")]
        HBStyleWeatherQualityLevel m_Level = HBStyleWeatherQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleWeatherLevel m_Low = new HBStyleWeatherLevel(HBStyleShelterResolution.Low, 10f);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleWeatherLevel m_Medium = new HBStyleWeatherLevel(HBStyleShelterResolution.Medium, 20f);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleWeatherLevel m_High = new HBStyleWeatherLevel(HBStyleShelterResolution.Medium, 30f);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleWeatherLevel m_Ultra = new HBStyleWeatherLevel(HBStyleShelterResolution.High, 40f);

        public HBStyleWeatherQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleWeatherLevel Get(HBStyleWeatherQualityLevel level)
        {
            return level switch
            {
                HBStyleWeatherQualityLevel.Low => m_Low,
                HBStyleWeatherQualityLevel.Medium => m_Medium,
                HBStyleWeatherQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleWeatherLevel current => Get(m_Level);
    }
}
