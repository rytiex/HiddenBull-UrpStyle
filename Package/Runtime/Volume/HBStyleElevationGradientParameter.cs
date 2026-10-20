using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleElevationGradientParameter : HBStyleGradientParameter
    {
        [SerializeField]
        bool m_Mirrored;

        public bool mirrored => m_Mirrored;

        public HBStyleElevationGradientParameter(Gradient value, bool mirrored = false,
                                          bool overrideState = false)
            : base(value, overrideState)
        {
            m_Mirrored = mirrored;
        }
    }
}
