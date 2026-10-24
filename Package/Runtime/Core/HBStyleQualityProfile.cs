using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Quality Profile", menuName = "HiddenBull/URP Style/Quality Profile")]
    public sealed class HBStyleQualityProfile : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Main-light shadow levels: distance, cascades, resolution and the contact-hardening filter.")]
        HBStyleShadowQuality m_Shadows = new HBStyleShadowQuality();

        [SerializeField]
        [Tooltip("Atmosphere levels: which optional fog and horizon terms run.")]
        HBStyleAtmosphereQuality m_Atmosphere = new HBStyleAtmosphereQuality();

        public HBStyleShadowQuality shadows => m_Shadows;

        public HBStyleAtmosphereQuality atmosphere => m_Atmosphere;
    }
}
