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
        public ClampedFloatParameter heightFalloff = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("World height, in metres, at which density is at its full value.")]
        public FloatParameter baseHeight = new FloatParameter(0f);

        [Tooltip("Ceiling on how much the fog can take over. Below 1 the most distant geometry " +
                 "still reads as shape rather than dissolving into flat sky.")]
        public ClampedFloatParameter maxOpacity = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("How much the fog takes on the sun's colour when looking toward it. Real air " +
                 "scatters light forward, and this is most of what makes a hazy sunset read as " +
                 "depth rather than as a grey wash. It fades out as the sun sets, and drops away " +
                 "where the sun is blocked, so air in a mountain's shadow stops glowing.")]
        public ClampedFloatParameter sunScattering = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("How far fog darkens where the light does not reach. Fog colour is read from a " +
                 "gradient by view direction, which assumes open sky — so a valley in shadow, or " +
                 "anything indoors, glows as though it were lit. This slides the reading down the " +
                 "same gradient in proportion to how blocked the light is, so shadowed air takes " +
                 "the colour of the ground rather than of the sky.\n\n" +
                 "It follows the main light's shadow, which means baked shadows count too.")]
        public ClampedFloatParameter shade = new ClampedFloatParameter(0.5f, 0f, 2f);

        [Header("Colour")]
        [Tooltip("The colour the fog fades toward, read along the view direction: left is looking " +
                 "straight down, the middle of the bar is the horizon, right is straight up. It " +
                 "shares its axis and its day/dusk/night blend with the sky gradients, so match " +
                 "the middle of this bar to the middle of Day Sky and the horizon stays seamless.")]
        public GradientParameter dayFog = new GradientParameter(DefaultDayFog());

        [Tooltip("The fog while the sun is at the horizon.")]
        public GradientParameter duskFog = new GradientParameter(DefaultDuskFog());

        [Tooltip("The fog after dark. This is the one worth spending time on: the ambient gradient " +
                 "the fog used to borrow has to keep its zenith bright to light upward-facing " +
                 "surfaces at night, which left distant air looking washed out. Here you can pull " +
                 "the right-hand end down as far as you like without touching how anything is lit.")]
        public GradientParameter nightFog = new GradientParameter(DefaultNightFog());

        public static Gradient DefaultDayFog() => StyleSky.DefaultDayAmbient();

        public static Gradient DefaultDuskFog() => StyleSky.DefaultDuskAmbient();

        public static Gradient DefaultNightFog() => StyleSky.Make(
            StyleSky.Key(0.0000000f, 0.0000000f, 0.0000000f, 0.0676f),
            StyleSky.Key(0.0788092f, 0.0788092f, 0.1037736f, 0.1882f),
            StyleSky.Key(0.1513884f, 0.1513884f, 0.1698113f, 0.4206f),
            StyleSky.Key(0.1668298f, 0.1748540f, 0.2169811f, 0.6676f),
            StyleSky.Key(0.1392399f, 0.1457814f, 0.1981132f, 0.8088f),
            StyleSky.Key(0.0943396f, 0.0943396f, 0.0943396f, 1f));
    }
}
