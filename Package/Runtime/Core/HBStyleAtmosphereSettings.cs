using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleAtmosphereSettings
    {
        [SerializeField]
        [Tooltip("Turns the atmosphere on: fog, rain and snow closing the sky, and the rain, snow and wet surface " +
                 "looks set here. Off means no fog and default weather looks.")]
        bool m_Enabled;

        [SerializeField]
        [Tooltip("The fog with no rain or snow.")]
        HBStyleFogState m_Clear = HBStyleFogState.Clear();

        [SerializeField]
        [Tooltip("The fog at full rain. Light rain blends between Clear and this.")]
        HBStyleFogState m_Rain = HBStyleFogState.Rain();

        [SerializeField]
        [Tooltip("The fog at full snow. Light snow blends between Clear and this.")]
        HBStyleFogState m_Snow = HBStyleFogState.Snow();

        [SerializeField]
        [Tooltip("Brush painting of the fog and how wind thins it, in any weather.")]
        HBStyleFogShared m_Fog = new HBStyleFogShared();

        [SerializeField]
        [Tooltip("How rain, snow and cloud cover close the sky, and what an overcast sky does to the scene.")]
        HBStyleOvercastSettings m_Overcast = new HBStyleOvercastSettings();

        [SerializeField]
        [Tooltip("How the falling rain and snow look and how surfaces wet, dry, gather snow and melt. How hard it " +
                 "rains, snows and blows is set on the HB Style Environment Controller.")]
        HBStyleWeatherSettings m_Weather = new HBStyleWeatherSettings();

        [NonSerialized]
        int m_Version;

        public bool enabled
        {
            get => m_Enabled;
            set
            {
                m_Enabled = value;
                m_Version++;
            }
        }

        public HBStyleFogState clear => m_Clear;

        public HBStyleFogState rain => m_Rain;

        public HBStyleFogState snow => m_Snow;

        public HBStyleFogShared fog => m_Fog;

        public HBStyleOvercastSettings overcast => m_Overcast;

        public HBStyleWeatherSettings weather => m_Weather;

        internal int version => m_Version;

        internal void Touch()
        {
            m_Version++;
        }

        internal static Vector3 Weights(float rain, float snow)
        {
            var wet = Mathf.Clamp01(rain);
            var cold = Mathf.Clamp01(snow);
            var total = wet + cold;

            if (total > 1f)
            {
                wet /= total;
                cold /= total;
            }

            return new Vector3(1f - wet - cold, wet, cold);
        }
    }
}
