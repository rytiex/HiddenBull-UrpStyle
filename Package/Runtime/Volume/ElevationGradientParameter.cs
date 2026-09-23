using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class ElevationGradientParameter : GradientParameter
    {
        [SerializeField]
        bool m_Mirrored;

        public bool mirrored => m_Mirrored;

        public ElevationGradientParameter(Gradient value, bool mirrored = false,
                                          bool overrideState = false)
            : base(value, overrideState)
        {
            m_Mirrored = mirrored;
        }
    }
}
