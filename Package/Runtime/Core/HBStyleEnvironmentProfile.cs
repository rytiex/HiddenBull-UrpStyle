using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Environment Profile", menuName = "HiddenBull/URP Style/Environment Profile")]
    public sealed class HBStyleEnvironmentProfile : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Global brush atlas, world scale and stroke variation shared by every HB Style shader.")]
        HBStyleBrushSettings m_Brush = new HBStyleBrushSettings();

        [SerializeField]
        [Tooltip("Sky, ambient and key-light gradients over the day, and the day/night timing.")]
        HBStyleSkySettings m_Sky = new HBStyleSkySettings();

        [SerializeField]
        [Tooltip("Sun and moon discs, stars and aurora.")]
        HBStyleCelestialSettings m_Celestial = new HBStyleCelestialSettings();

        [SerializeField]
        [Tooltip("Cloud atlas, coverage, shape and lighting.")]
        HBStyleCloudSettings m_Clouds = new HBStyleCloudSettings();

        [SerializeField]
        [Tooltip("Height fog with its own colour, and the horizon haze toward the sky colour.")]
        HBStyleAtmosphereSettings m_Atmosphere = new HBStyleAtmosphereSettings();

        [NonSerialized]
        int m_Version;

        public HBStyleBrushSettings brush => m_Brush;

        public HBStyleSkySettings sky => m_Sky;

        public HBStyleCelestialSettings celestial => m_Celestial;

        public HBStyleCloudSettings clouds => m_Clouds;

        public HBStyleAtmosphereSettings atmosphere => m_Atmosphere;

        internal int version => m_Version;

        void OnValidate()
        {
            m_Version++;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }
}
