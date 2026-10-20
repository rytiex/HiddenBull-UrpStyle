using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public class HBStyleCloudGlobalSettings
    {
        [SerializeField]
        [Tooltip("Cloud atlas. Generate one on the Cloud page of Tools > HiddenBull > URP Style > " +
                 "Texture Studio. RG carry the relief, B the height used for parallax, A the coverage. " +
                 "Leaving this empty disables clouds entirely.")]
        Texture2D m_Atlas;

        public Texture2D atlas
        {
            get => m_Atlas;
            set => m_Atlas = value;
        }
    }
}
