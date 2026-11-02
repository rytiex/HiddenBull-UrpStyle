using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleTransparencyQualityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    public enum HBStyleGlassShadowResolution
    {
        Off,
        Quarter,
        Half
    }

    [Serializable]
    public struct HBStyleTransparencyLevel
    {
        [Tooltip("Size of the scene copy that Refractive glass bends and blurs, as a share of the screen. 1 is " +
                 "sharpest; lower values are cheaper and look softer at low Blur.")]
        [Range(0.25f, 1f)]
        public float refractionResolution;

        [Tooltip("Size of the map that holds the shadows glass casts, compared with the main shadow map. Off: " +
                 "glass casts no shadow.")]
        public HBStyleGlassShadowResolution glassShadowResolution;

        [Tooltip("Transparent surfaces and particles skip point and spot lights and the soft shadow filter: " +
                 "lit by the main light and ambient only, with plain shadow edges. Much cheaper for each layer.")]
        public bool simpleLighting;

        public HBStyleTransparencyLevel(float refractionResolution, HBStyleGlassShadowResolution glassShadowResolution,
                                        bool simpleLighting)
        {
            this.refractionResolution = refractionResolution;
            this.glassShadowResolution = glassShadowResolution;
            this.simpleLighting = simpleLighting;
        }
    }

    [Serializable]
    public sealed class HBStyleTransparencyQuality
    {
        [SerializeField]
        [Tooltip("Active transparency level: how sharp refraction is and how glass shadows are drawn.")]
        HBStyleTransparencyQualityLevel m_Level = HBStyleTransparencyQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleTransparencyLevel m_Low = new HBStyleTransparencyLevel(0.5f, HBStyleGlassShadowResolution.Off, true);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleTransparencyLevel m_Medium =
            new HBStyleTransparencyLevel(0.5f, HBStyleGlassShadowResolution.Quarter, false);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleTransparencyLevel m_High = new HBStyleTransparencyLevel(0.75f, HBStyleGlassShadowResolution.Half, false);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleTransparencyLevel m_Ultra = new HBStyleTransparencyLevel(1f, HBStyleGlassShadowResolution.Half, false);

        public HBStyleTransparencyQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public HBStyleTransparencyLevel Get(HBStyleTransparencyQualityLevel level)
        {
            return level switch
            {
                HBStyleTransparencyQualityLevel.Low => m_Low,
                HBStyleTransparencyQualityLevel.Medium => m_Medium,
                HBStyleTransparencyQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleTransparencyLevel current => Get(m_Level);
    }
}
