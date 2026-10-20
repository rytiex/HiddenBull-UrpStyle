using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public class HBStyleGradientParameter : VolumeParameter<Gradient>
    {
        public HBStyleGradientParameter(Gradient value, bool overrideState = false)
            : base(value, overrideState)
        {
        }
    }
}
