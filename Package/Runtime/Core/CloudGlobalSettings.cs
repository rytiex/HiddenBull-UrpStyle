using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public class CloudGlobalSettings
    {
        [SerializeField]
        [Tooltip("Cloud atlas. Generate one with Tools > HiddenBull > URP Style > Cloud Atlas " +
                 "Generator. RG carry the relief, B the height used for parallax, A the coverage. " +
                 "Leaving this empty disables clouds entirely.")]
        Texture2D m_Atlas;

        public Texture2D atlas
        {
            get => m_Atlas;
            set => m_Atlas = value;
        }
    }
}
