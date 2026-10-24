using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleShadowResolution
    {
        [InspectorName("512")] Size512 = 512,
        [InspectorName("1024")] Size1024 = 1024,
        [InspectorName("2048")] Size2048 = 2048,
        [InspectorName("4096")] Size4096 = 4096,
        [InspectorName("8192")] Size8192 = 8192
    }

    [Serializable]
    public struct HBStyleShadowLevel
    {
        [Tooltip("How far main-light shadows reach from the camera, in metres.")]
        [Min(1f)]
        public float distance;

        [Tooltip("Number of shadow cascades (1, 2 or 4). More cascades spend the shadow map's texels closer " +
                 "to the camera.")]
        [Range(1, 4)]
        public int cascades;

        [Tooltip("Resolution of the main-light shadow map, in texels per side.")]
        public HBStyleShadowResolution resolution;

        [Tooltip("The widest a shadow edge may spread, in metres. 0 hands filtering back to URP everywhere.")]
        [Range(0f, 0.225f)]
        public float softness;

        [Tooltip("Shadow-map taps per search and per filter in the contact-hardening filter (1 to 16). Fewer " +
                 "taps are cheaper and noisier.")]
        [Range(1, 16)]
        public int taps;

        public HBStyleShadowLevel(float distance, int cascades, HBStyleShadowResolution resolution, float softness,
                                  int taps)
        {
            this.distance = distance;
            this.cascades = cascades;
            this.resolution = resolution;
            this.softness = softness;
            this.taps = taps;
        }
    }

    [Serializable]
    public sealed class HBStyleShadowQuality
    {
        internal const float ContactReach = 15f;

        [SerializeField]
        [Tooltip("Active shadow level. Off turns realtime shadows off. Low to Ultra write their distance, " +
                 "cascades and resolution into the active URP asset and drive the contact-hardening filter, " +
                 "which runs within 15 m of the camera on every level.")]
        HBStyleQualityLevel m_Level = HBStyleQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleShadowLevel m_Low = new HBStyleShadowLevel(20f, 1, HBStyleShadowResolution.Size1024, 0.225f, 4);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleShadowLevel m_Medium = new HBStyleShadowLevel(45f, 2, HBStyleShadowResolution.Size2048, 0.225f, 8);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleShadowLevel m_High = new HBStyleShadowLevel(80f, 4, HBStyleShadowResolution.Size2048, 0.225f, 12);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleShadowLevel m_Ultra = new HBStyleShadowLevel(150f, 4, HBStyleShadowResolution.Size4096, 0.225f, 16);

        [SerializeField]
        [Tooltip("How far a caster has to stand off a surface, in metres, before its shadow reaches full " +
                 "softness. Right at the contact point the edge stays sharp and it opens up as the object " +
                 "lifts away. 0 gives one width everywhere.")]
        [Range(0f, 4.5f)]
        float m_Contact = 4.5f;

        [SerializeField]
        [Tooltip("How far the brush drags shadow edges, in metres, so the edge follows a stroke instead of " +
                 "the texel grid. Needs a brush atlas in the Environment Profile.")]
        [Range(0f, 1f)]
        float m_EdgeBrush = 0.25f;

        [SerializeField]
        [Tooltip("World size of one shadow-edge stroke, in metres, measured on the plane facing the light.")]
        [Min(0.05f)]
        float m_EdgeBrushSize = 0.75f;

        public HBStyleQualityLevel level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public float contact => m_Contact;

        public float edgeBrush => m_EdgeBrush;

        public float edgeBrushSize => m_EdgeBrushSize;

        public HBStyleShadowLevel Get(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.Low => m_Low,
                HBStyleQualityLevel.Medium => m_Medium,
                HBStyleQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleShadowLevel current => Get(m_Level);
    }
}
