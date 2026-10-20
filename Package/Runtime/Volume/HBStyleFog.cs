using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    [VolumeComponentMenu("HiddenBull/HB Style Fog")]
    [DisplayInfo(name = "HB Style Fog")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class HBStyleFog : VolumeComponent
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

        [Tooltip("How strongly the sun lights the fog. Looking toward the sun the fog takes on its " +
                 "colour, since real air scatters light forward — most of what makes a hazy sunset " +
                 "read as depth rather than as a grey wash. From any other angle, air the sun reaches " +
                 "reads a little brighter than air in shadow, so beams through a window or between " +
                 "buildings show from the side as well. It fades out as the sun sets.\n\n" +
                 "The sun's shadow map decides which air it reaches, so beams have clean edges. That " +
                 "needs a Realtime or Mixed sun; for Mixed, use Baked Indirect or Distance Shadowmask " +
                 "so static geometry casts into the shadow map. Where there is no shadow map — past " +
                 "the shadow distance, or with a fully baked sun — the fog assumes the sun reaches " +
                 "any air with open sky above it, so interiors stay dark but beams are not cut.\n\n" +
                 "It also fades as the clouds close in, along with the sun's direct light.")]
        public ClampedFloatParameter sunScattering = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("How far the fog colour is broken up by brush strokes, using the sky's Sky Paint and " +
                 "Sky Brush amounts. Over geometry the strokes are laid on the surfaces behind the fog " +
                 "and sized by distance, so they stay put as you walk or turn instead of sliding across " +
                 "the view. Where the fog swallows the scene completely they hand over to the sky's own " +
                 "strokes, so the horizon still meets the sky without a seam. 0 leaves the fog a smooth " +
                 "gradient.")]
        public ClampedFloatParameter paint = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("How far outlines dissolve into the fog behind them. Each stroke takes its fog from " +
                 "the centre of the stroke, so where a stroke straddles an edge the more distant, " +
                 "foggier side is carried across it and the outline breaks up into brush marks instead " +
                 "of a clean cut. It only ever adds fog, so nearby surfaces are never cleared.")]
        public ClampedFloatParameter silhouette = new ClampedFloatParameter(1f, 0f, 2f);

        [Tooltip("The colour the fog fades toward, read along the view direction: left is looking " +
                 "straight down, the middle of the bar is the horizon, right is straight up. It " +
                 "shares its axis and its day/dusk/night blend with the sky gradients, so match " +
                 "the middle of this bar to the middle of Day Sky and the horizon stays seamless.")]
        public HBStyleGradientParameter dayFog = new HBStyleGradientParameter(DefaultDayFog());

        [Tooltip("The fog while the sun is at the horizon.")]
        public HBStyleGradientParameter duskFog = new HBStyleGradientParameter(DefaultDuskFog());

        [Tooltip("The fog after dark. This is the one worth spending time on: the ambient gradient " +
                 "the fog used to borrow has to keep its zenith bright to light upward-facing " +
                 "surfaces at night, which left distant air looking washed out. Here you can pull " +
                 "the right-hand end down as far as you like without touching how anything is lit.")]
        public HBStyleGradientParameter nightFog = new HBStyleGradientParameter(DefaultNightFog());

        public static Gradient DefaultDayFog() => HBStyleSky.DefaultDayAmbient();

        public static Gradient DefaultDuskFog() => HBStyleSky.DefaultDuskAmbient();

        public static Gradient DefaultNightFog() => HBStyleSky.Make(
            HBStyleSky.Key(0.0000000f, 0.0000000f, 0.0000000f, 0.0676f),
            HBStyleSky.Key(0.0788092f, 0.0788092f, 0.1037736f, 0.1882f),
            HBStyleSky.Key(0.1513884f, 0.1513884f, 0.1698113f, 0.4206f),
            HBStyleSky.Key(0.1668298f, 0.1748540f, 0.2169811f, 0.6676f),
            HBStyleSky.Key(0.1392399f, 0.1457814f, 0.1981132f, 0.8088f),
            HBStyleSky.Key(0.0943396f, 0.0943396f, 0.0943396f, 1f));
    }
}
