using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    [VolumeComponentMenu("HiddenBull/Style Ambient")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StyleAmbient : VolumeComponent
    {
        [Tooltip("Colour applied to surfaces facing up. Usually the sky.")]
        public ColorParameter skyColor = new ColorParameter(new Color(1f, 0.938866f, 0.891509f), true, false, true);

        [Tooltip("Colour of the band around the horizon. Carries the warm rim that makes backlit " +
                 "silhouettes read.")]
        public ColorParameter horizonColor = new ColorParameter(new Color(0.537736f, 0.537736f, 0.537736f), true, false, true);

        [Tooltip("Colour applied to surfaces facing down. Usually bounced light from the ground.")]
        public ColorParameter groundColor = new ColorParameter(new Color(0.339623f, 0.339623f, 0.339623f), true, false, true);

        [Tooltip("How quickly the sky colour takes over above the horizon. Lower values make a " +
                 "tighter, more defined horizon band.")]
        public ClampedFloatParameter skyFalloff = new ClampedFloatParameter(0.6f, 0.01f, 1f);

        [Tooltip("How quickly the ground colour takes over below the horizon.")]
        public ClampedFloatParameter groundFalloff = new ClampedFloatParameter(0.8f, 0.01f, 1f);

        [Tooltip("Overall strength of the ambient term.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 4f);

        [Tooltip("Blends from the gradient toward Unity's baked lightmaps and probes. " +
                 "0 keeps the gradient, 1 uses baked indirect light only. Direct light and its " +
                 "shadows always stay realtime regardless (Architecture D16).")]
        public ClampedFloatParameter bakedWeight = new ClampedFloatParameter(0f, 0f, 1f);
    }
}
