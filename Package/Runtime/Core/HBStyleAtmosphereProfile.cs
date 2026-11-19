using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Atmosphere Profile", order = 2,
                     menuName = "HiddenBull/URP Style/Profiles/Atmosphere Profile")]
    public sealed class HBStyleAtmosphereProfile : ScriptableObject
    {
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

        public HBStyleFogState clear => m_Clear;

        public HBStyleFogState rain => m_Rain;

        public HBStyleFogState snow => m_Snow;

        public HBStyleFogShared fog => m_Fog;

        public HBStyleOvercastSettings overcast => m_Overcast;

        public HBStyleWeatherSettings weather => m_Weather;

        internal int version => m_Version;

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

        void OnValidate()
        {
            m_Version++;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }
}
