using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Environment Profile", order = 1,
                     menuName = "HiddenBull/URP Style/Profiles/Environment Profile")]
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
        [Tooltip("Atmosphere Profile with everything that changes with the weather: fog for clear weather, rain " +
                 "and snow, overcast, the look of rain and snow and how surfaces wet and dry. It is left out of " +
                 "light bakes. Empty means no fog and default weather looks.")]
        HBStyleAtmosphereProfile m_AtmosphereProfile;

        static HBStyleWeatherSettings s_DefaultWeather;

        [NonSerialized]
        int m_Version;

        public HBStyleBrushSettings brush => m_Brush;

        public HBStyleSkySettings sky => m_Sky;

        public HBStyleCelestialSettings celestial => m_Celestial;

        public HBStyleCloudSettings clouds => m_Clouds;

        public HBStyleAtmosphereProfile atmosphere => m_AtmosphereProfile;

        public HBStyleWeatherSettings weather => m_AtmosphereProfile != null
            ? m_AtmosphereProfile.weather
            : s_DefaultWeather ??= new HBStyleWeatherSettings();

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
