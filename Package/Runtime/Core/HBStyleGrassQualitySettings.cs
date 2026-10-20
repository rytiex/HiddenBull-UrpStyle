using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleGrassQuality
    {
        Off,
        Low,
        Medium,
        High,
        Ultra
    }

    [Serializable]
    public struct HBStyleGrassLevel
    {
        [Tooltip("How far from the camera grass is drawn, in metres. It thins out toward the end and fades away.")]
        [Range(10f, 300f)]
        public float distance;

        [Tooltip("How closely the tufts stand. 1 is a tuft about every 20 cm; 4 halves that spacing.")]
        [Range(0.25f, 8f)]
        public float density;

        [Tooltip("Lets a low sun glow through the tips of the grass, in the colours set on the terrain material.")]
        public bool translucency;

        public HBStyleGrassLevel(float distance, float density, bool translucency)
        {
            this.distance = distance;
            this.density = density;
            this.translucency = translucency;
        }
    }

    [Serializable]
    public class HBStyleGrassQualitySettings
    {
        [SerializeField]
        [Tooltip("How much grass every terrain grows and how far it reaches. Each level below carries its own " +
                 "numbers, so a build can offer the choice to its players. Off draws no grass at all.")]
        HBStyleGrassQuality m_Quality = HBStyleGrassQuality.High;

        [SerializeField]
        HBStyleGrassLevel m_Low = new HBStyleGrassLevel(40f, 0.5f, false);

        [SerializeField]
        HBStyleGrassLevel m_Medium = new HBStyleGrassLevel(60f, 1f, true);

        [SerializeField]
        HBStyleGrassLevel m_High = new HBStyleGrassLevel(90f, 2f, true);

        [SerializeField]
        HBStyleGrassLevel m_Ultra = new HBStyleGrassLevel(130f, 3.5f, true);

        public HBStyleGrassQuality quality
        {
            get => m_Quality;
            set => m_Quality = value;
        }

        public bool translucency => TryGetLevel(out var level) && level.translucency;

        public bool TryGetLevel(out HBStyleGrassLevel level)
        {
            switch (m_Quality)
            {
                case HBStyleGrassQuality.Low:
                    level = m_Low;
                    return true;

                case HBStyleGrassQuality.Medium:
                    level = m_Medium;
                    return true;

                case HBStyleGrassQuality.High:
                    level = m_High;
                    return true;

                case HBStyleGrassQuality.Ultra:
                    level = m_Ultra;
                    return true;

                default:
                    level = default;
                    return false;
            }
        }
    }
}
