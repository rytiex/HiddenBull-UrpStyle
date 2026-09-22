using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    /// <summary>
    /// The three-zone gradient ambient model (Architecture D12).
    ///
    /// In this style ambient is not a subtle fill on top of the lighting — it is what makes a flat,
    /// untextured surface read as three tones. Faces pointing up take <see cref="skyColor"/>, faces
    /// pointing down take <see cref="groundColor"/>, and faces near horizontal take
    /// <see cref="horizonColor"/>. That horizon band is what lets a backlit silhouette separate, and
    /// it is the reason a two-colour hemisphere was not enough.
    ///
    /// Lives on a Volume rather than on the renderer feature because it is an artistic, per-area
    /// setting that should blend as the camera moves between an interior and an exterior.
    /// <see cref="StyleSettings"/> on the feature holds performance settings, which are fixed per
    /// quality level and a different concern entirely (Architecture D7).
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("HiddenBull/Style Ambient")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StyleAmbient : VolumeComponent
    {
        [Tooltip("Colour applied to surfaces facing up. Usually the sky.")]
        public ColorParameter skyColor = new ColorParameter(new Color(0.25f, 0.42f, 0.65f), true, false, true);

        [Tooltip("Colour of the band around the horizon. Carries the warm rim that makes backlit " +
                 "silhouettes read.")]
        public ColorParameter horizonColor = new ColorParameter(new Color(0.55f, 0.45f, 0.38f), true, false, true);

        [Tooltip("Colour applied to surfaces facing down. Usually bounced light from the ground.")]
        public ColorParameter groundColor = new ColorParameter(new Color(0.18f, 0.15f, 0.14f), true, false, true);

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
