using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleLensFlareSettings
    {
        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("Strength of the sun flare: glow, rays, ghosts and halo drawn from the sun when it is on screen. " +
                 "It fades when the sun goes behind objects, clouds or the horizon. 0 turns it off.")]
        float m_SunIntensity = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Strength of the thin rays that spread out from the sun.")]
        float m_Rays = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Strength of the faint ghosts from small bright lights such as lamps and headlights. Large bright " +
                 "areas never flare. 0 turns it off.")]
        float m_LightIntensity = 0.15f;

        [SerializeField]
        [Range(0f, 8f)]
        [Tooltip("How much brighter than the bloom threshold a small light must be to cast ghosts. Raise it so " +
                 "water glints and dim lamps stop flaring.")]
        float m_LightThreshold = 2f;

        [SerializeField]
        [Range(1, 8)]
        [Tooltip("Number of ghosts: the soft copies of the sun and of bright lights along a line through the " +
                 "middle of the screen.")]
        int m_Ghosts = 6;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Distance between the ghosts along their line through the middle of the screen.")]
        float m_GhostSpacing = 0.35f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Strength of the halo: the thin ring around the sun.")]
        float m_Halo = 0.5f;

        [SerializeField]
        [Range(0.1f, 0.9f)]
        [Tooltip("Radius of the halo ring around the sun, as a share of the screen height.")]
        float m_HaloSize = 0.45f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far the red and blue edges of the ghosts and the halo split apart, like a real lens.")]
        float m_ColourFringe = 0.5f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the flare. The light's own colour is kept; this only warms or cools all of it.")]
        Color m_Tint = new Color(1f, 0.92f, 0.82f, 1f);

        public float sunIntensity
        {
            get => m_SunIntensity;
            set => m_SunIntensity = Mathf.Clamp(value, 0f, 2f);
        }

        public float rays
        {
            get => m_Rays;
            set => m_Rays = Mathf.Clamp01(value);
        }

        public float lightIntensity
        {
            get => m_LightIntensity;
            set => m_LightIntensity = Mathf.Clamp01(value);
        }

        public float lightThreshold
        {
            get => m_LightThreshold;
            set => m_LightThreshold = Mathf.Clamp(value, 0f, 8f);
        }

        public int ghosts
        {
            get => m_Ghosts;
            set => m_Ghosts = Mathf.Clamp(value, 1, 8);
        }

        public float ghostSpacing
        {
            get => m_GhostSpacing;
            set => m_GhostSpacing = Mathf.Clamp01(value);
        }

        public float halo
        {
            get => m_Halo;
            set => m_Halo = Mathf.Clamp01(value);
        }

        public float haloSize
        {
            get => m_HaloSize;
            set => m_HaloSize = Mathf.Clamp(value, 0.1f, 0.9f);
        }

        public float colourFringe
        {
            get => m_ColourFringe;
            set => m_ColourFringe = Mathf.Clamp01(value);
        }

        public Color tint
        {
            get => m_Tint;
            set => m_Tint = value;
        }
    }
}
