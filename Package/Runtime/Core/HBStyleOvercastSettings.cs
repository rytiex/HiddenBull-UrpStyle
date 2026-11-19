using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleOvercastSettings
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far rain and snow close the sky (0 to 1): at 1 steady rain brings the sky to full overcast. " +
                 "Overcast takes the sun's light away (at full overcast none is left, so its shadows go with it while " +
                 "rooms and shade stay dark), greys the sky and the fog and blends baked light toward the cloudy " +
                 "sky's ambient. It also follows the cloud cover set in the Clouds tab of the Environment Profile.")]
        float m_CloudResponse = 1f;

        [SerializeField]
        [Tooltip("Direction and speed the cloud bank drifts, as a velocity.")]
        Vector2 m_CloudDrift = new Vector2(1.25f, 0.2f);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far a full overcast drains the colour out of the sky and darkens it (0 to 1).")]
        float m_SkyGrey = 0.6f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far a full overcast drains the colour out of the fog toward plain grey (0 to 1). The sun's " +
                 "glow in the fog fades with the overcast on its own.")]
        float m_FogGrey = 0.6f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far a full overcast replaces baked light (lightmaps and probes, baked in clear weather) with " +
                 "the even ambient of the cloudy sky (0 to 1). 0 keeps the baked light as it was baked.")]
        float m_BakedToSky = 0f;

        public float cloudResponse => m_CloudResponse;
        public Vector2 cloudDrift => m_CloudDrift;
        public float skyGrey => m_SkyGrey;
        public float fogGrey => m_FogGrey;
        public float bakedToSky => m_BakedToSky;
    }
}
