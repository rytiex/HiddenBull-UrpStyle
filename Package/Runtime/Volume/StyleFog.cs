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

        [Tooltip("How far the fog obeys the light that actually reaches it. Fog colour is read from " +
                 "a gradient by view direction, which assumes open sky — so without this, a valley " +
                 "in shadow and a sealed room both glow as though the sky were overhead.\n\n" +
                 "Two things are read separately, because they are not the same. Where the sun is " +
                 "blocked the reading slides down the gradient, so shadowed air takes the colour of " +
                 "the ground instead of the sky but stays lit — which is what really happens, since " +
                 "a shadow outdoors is still under an open sky. Where the baked lighting says there " +
                 "is no light at all, the fog is darkened toward black instead, so an unlit interior " +
                 "no longer has bright air in it.\n\n" +
                 "That reading is taken from the air the fog actually sits in rather than from " +
                 "whatever the view lands on, so standing outside and looking into an unlit room " +
                 "leaves the fog at arm's length bright. It needs Adaptive Probe Volumes, which is " +
                 "the only thing that knows how much light reaches a point that is not a surface.\n\n" +
                 "Without them the fog falls back to reading the surface it lands on, which is " +
                 "cruder: a dark room at the end of the view drags down the fog in front of you as " +
                 "well.\n\n" +
                 "At 1 the fog follows the measured light literally. Lower values lift the middle of " +
                 "that range without touching either end, so a doorway in shade keeps its fog while " +
                 "a room with no light in it still goes black. Reach for this when Sky Occlusion is " +
                 "baked, since sky visibility falls away sharply under even a small overhang and a " +
                 "literal reading of it darkens more than the eye expects.\n\n" +
                 "The slider does nothing at all until Adaptive Probe Volumes are enabled and baked, " +
                 "because there is nothing to read the air from. It is not held back to be strict: " +
                 "a half-working version of this reads the surface at the end of the view instead, " +
                 "which drags a distant dark room onto the fog at your feet.")]
        [DisplayInfo(name = "Shade (Only APV Bake)")]
        public ClampedFloatParameter shade = new ClampedFloatParameter(1f, 0f, 1f);

        [Header("Brush")]
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
