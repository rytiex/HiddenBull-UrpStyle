using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public sealed class HBStyleAtmosphereProfile : ScriptableObject
    {
        [SerializeField]
        HBStyleFogState m_Clear = HBStyleFogState.Clear();

        [SerializeField]
        HBStyleFogState m_Rain = HBStyleFogState.Rain();

        [SerializeField]
        HBStyleFogState m_Snow = HBStyleFogState.Snow();

        [SerializeField]
        HBStyleFogShared m_Fog = new HBStyleFogShared();

        [SerializeField]
        HBStyleOvercastSettings m_Overcast = new HBStyleOvercastSettings();

        [SerializeField]
        HBStyleWeatherSettings m_Weather = new HBStyleWeatherSettings();

        [NonSerialized]
        HBStyleAtmosphereSettings m_Settings;

        internal HBStyleAtmosphereSettings settings
        {
            get
            {
                if (m_Settings == null)
                {
                    m_Settings = new HBStyleAtmosphereSettings();
                    CopyTo(m_Settings);
                }

                return m_Settings;
            }
        }

        internal void CopyTo(HBStyleAtmosphereSettings target)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), target);
            target.enabled = true;
        }

        void OnValidate()
        {
            m_Settings = null;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }
}
