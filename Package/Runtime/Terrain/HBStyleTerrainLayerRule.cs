using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleTerrainLayerRule
    {
        [SerializeField]
        [Tooltip("The layer's textures and look.")]
        HBStyleTerrainLayer m_Layer;

        [SerializeField]
        [Tooltip("Places the layer where the rules below match. Off: only painting places it. The first row " +
                 "always covers the whole ground under the others.")]
        bool m_Automatic = true;

        [SerializeField]
        [Tooltip("Steepness range, in degrees, where the layer grows: 0 is flat, 90 a vertical wall.")]
        Vector2 m_Slope = new Vector2(0f, 90f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("How many degrees the slope edges fade over.")]
        float m_SlopeSoftness = 5f;

        [SerializeField]
        [Tooltip("Height range, in metres above the terrain's origin, where the layer grows.")]
        Vector2 m_Height = new Vector2(-10000f, 10000f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("How many metres the height edges fade over.")]
        float m_HeightSoftness = 2f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of the layers below this one it covers where its rules match fully.")]
        float m_Opacity = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly noise breaks up the edges of the rules, so borders wander instead of following " +
                 "the slope and height lines exactly.")]
        float m_Breakup = 0.5f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Size of the break-up noise, in metres.")]
        float m_BreakupSize = 8f;

        public HBStyleTerrainLayer layer
        {
            get => m_Layer;
            set => m_Layer = value;
        }

        public bool automatic
        {
            get => m_Automatic;
            set => m_Automatic = value;
        }

        public Vector2 slope
        {
            get => m_Slope;
            set => m_Slope = value;
        }

        public float slopeSoftness
        {
            get => m_SlopeSoftness;
            set => m_SlopeSoftness = Mathf.Max(value, 0f);
        }

        public Vector2 height
        {
            get => m_Height;
            set => m_Height = value;
        }

        public float heightSoftness
        {
            get => m_HeightSoftness;
            set => m_HeightSoftness = Mathf.Max(value, 0f);
        }

        public float opacity
        {
            get => m_Opacity;
            set => m_Opacity = Mathf.Clamp01(value);
        }

        public float breakup
        {
            get => m_Breakup;
            set => m_Breakup = Mathf.Clamp01(value);
        }

        public float breakupSize
        {
            get => m_BreakupSize;
            set => m_BreakupSize = Mathf.Max(value, 0.1f);
        }
    }
}
