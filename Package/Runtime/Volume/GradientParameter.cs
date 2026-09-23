using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public class GradientParameter : VolumeParameter<Gradient>
    {
        public GradientParameter(Gradient value, bool overrideState = false)
            : base(value, overrideState)
        {
        }
    }
}
