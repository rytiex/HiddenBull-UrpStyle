using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    [VolumeComponentMenu("HiddenBull/Style Fog")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StyleFog : VolumeComponent
    {
        [Tooltip("Distance, in metres, before which nothing is fogged at all.")]
        [Min(0f)]
        public FloatParameter startDistance = new FloatParameter(0);

        [Tooltip("Distance, in metres, by which fog has reached Max Opacity. Set this to roughly " +
                 "how far you want to be able to see. 0 disables the fog entirely.")]
        [Min(0f)]
        public FloatParameter endDistance = new FloatParameter(0);

        [Tooltip("How quickly density drops with altitude. 0 gives uniform fog at every height; " +
                 "higher values keep it in the valleys. It has nothing to act on in a scene where " +
                 "everything sits at the same height.")]
        public ClampedFloatParameter heightFalloff = new ClampedFloatParameter(0.05f, 0f, 1f);

        [Tooltip("World height, in metres, at which density is at its full value.")]
        public FloatParameter baseHeight = new FloatParameter(0f);

        [Tooltip("Ceiling on how much the fog can take over. Below 1 the most distant geometry " +
                 "still reads as shape rather than dissolving into flat sky.")]
        public ClampedFloatParameter maxOpacity = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Tint on the ambient colour the fog fades toward. White leaves distant geometry " +
                 "exactly the colour the sky is lighting it with, which is what removes the seam " +
                 "at the horizon.")]
        public ColorParameter tint = new ColorParameter(Color.white, true, false, true);

        [Tooltip("How much the fog takes on the sun's colour when looking toward it. Real air " +
                 "scatters light forward, and this is most of what makes a hazy sunset read as " +
                 "depth rather than as a grey wash. It fades out as the sun sets, and drops away " +
                 "where the sun is blocked, so air in a mountain's shadow stops glowing.")]
        public ClampedFloatParameter sunScattering = new ClampedFloatParameter(1f, 0f, 1f);
    }
}
