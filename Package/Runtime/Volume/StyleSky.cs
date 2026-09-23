using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    [VolumeComponentMenu("HiddenBull/Style Sky")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StyleSky : VolumeComponent
    {
        [Header("Sky")]
        [Tooltip("The sky, read from straight down on the left to straight up on the right. The " +
                 "middle of the bar is the horizon. Put the keys where you want them — the shape of " +
                 "the horizon band is the gradient itself, so there is nothing else to tune.")]
        public GradientParameter daySky = new GradientParameter(DefaultDaySky());

        [Tooltip("The sky while the sun is at the horizon. This is its own stop because at sunset " +
                 "the horizon goes warm while the zenith cools — the two ends move in opposite " +
                 "directions, which no blend between day and night can produce.")]
        public GradientParameter duskSky = new GradientParameter(DefaultDuskSky());

        [Tooltip("The sky after dark. Editing a gradient from script at runtime needs a call to " +
                 "InvalidateGradients afterwards, because the baked lookup only rebuilds when the " +
                 "sun moves or a different volume takes over.")]
        public GradientParameter nightSky = new GradientParameter(DefaultNightSky());

        [Tooltip("Overall brightness of the sky.")]
        public ClampedFloatParameter skyIntensity = new ClampedFloatParameter(1f, 0f, 4f);

        [Tooltip("How far the brush atlas breaks up the sky, so it reads as painted rather than as " +
                 "a smooth ramp. It both bends the gradient and varies the colour, so strokes stay " +
                 "visible on a flat zenith as well as across the bands. It fades out toward the " +
                 "horizon on its own, to keep that line clean. Needs a brush atlas on the renderer " +
                 "feature.")]
        public ClampedFloatParameter skyBrush = new ClampedFloatParameter(0.45f, 0f, 1f);

        [Header("Ambient")]
        [Tooltip("The light the sky casts, read along the surface normal: left is a surface facing " +
                 "down, right is a surface facing up. Keep it calmer and less saturated than the sky " +
                 "gradient. A horizon the sky needs at full orange will look wrong painted straight " +
                 "onto geometry, which is the whole reason this is a separate gradient.")]
        public GradientParameter dayAmbient = new GradientParameter(DefaultDayAmbient());

        [Tooltip("Ambient light while the sun is at the horizon.")]
        public GradientParameter duskAmbient = new GradientParameter(DefaultDuskAmbient());

        [Tooltip("Ambient light after dark.")]
        public GradientParameter nightAmbient = new GradientParameter(DefaultNightAmbient());

        [Tooltip("Overall strength of the ambient term.")]
        public ClampedFloatParameter ambientIntensity = new ClampedFloatParameter(1f, 0f, 4f);

        [Tooltip("How far the ambient leans toward the sun instead of straight up. At 0 it is read " +
                 "purely by height, so a surface reads the same whichever way it faces the sun. " +
                 "Higher values swing the gradient onto the light, which gives form a lit and a " +
                 "shaded side even where the direct light has saturated.\n\n" +
                 "It does nothing at noon, because a sun overhead and a gradient read by height " +
                 "are the same axis. Its whole range lives in a low sun.")]
        public ClampedFloatParameter ambientLightBias = new ClampedFloatParameter(0.85f, 0f, 1f);

        [Tooltip("How far a lightmapped object leans on its lightmap instead of the gradient. 1 uses " +
                 "the baked indirect light, 0 ignores it.\n\n" +
                 "It only applies where there is a lightmap to use. Anything dynamic keeps the " +
                 "gradient whatever this says, so you are not trading one for the other across the " +
                 "whole scene — a baked building and the character walking past it can each have " +
                 "the right answer. It is still scaled down as the sun sets, because a lightmap " +
                 "carries the bounce of a sun that is no longer there. Direct light and its shadows " +
                 "stay realtime regardless.")]
        public ClampedFloatParameter bakedWeight = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("How much the warm band at sunset is kept to the side the sun is actually on. At 0 " +
                 "the horizon glows the same all the way around, which is what a gradient read " +
                 "purely by height has to do. Higher values let the sky opposite the sun run ahead " +
                 "into the night gradient, so it darkens first while the sunset side stays lit.")]
        public ClampedFloatParameter sunsetFocus = new ClampedFloatParameter(1f, 0f, 1f);

        [Header("Sky Light")]
        [Tooltip("The one directional light the scene gets, read by the sun's height: the far left " +
                 "is the middle of the night with the moon overhead, the middle of the bar is the " +
                 "horizon, the far right is noon.\n\n" +
                 "The light swaps to the moon's side once the sun drops below the horizon. It is " +
                 "faded out and back in across that swap for you, so the middle of the bar can " +
                 "hold any colour you like without the light appearing to jump sides — which is " +
                 "also what really happens, since a sun on the horizon puts almost no direct light " +
                 "on the ground. The warm raking light of a sunset belongs just to the right of " +
                 "centre, while the sun is still a few degrees up.")]
        public ElevationGradientParameter skyLight = new ElevationGradientParameter(DefaultSkyLight());

        [Tooltip("Overall strength of that light.")]
        public ClampedFloatParameter skyLightIntensity = new ClampedFloatParameter(1f, 0f, 4f);

        [Tooltip("The colour of the sun's disc, read by its own height: the far right is noon, the " +
                 "middle is the horizon. Everything left of centre is below the horizon and never " +
                 "drawn. How bright and how large it is lives on Style Celestial.")]
        public ElevationGradientParameter sunDiscColor = new ElevationGradientParameter(DefaultSunDiscColor());

        [Tooltip("The colour of the moon's disc, read by the moon's own height rather than the " +
                 "sun's — so a moon climbing while the sun sets is not forced to share its colour. " +
                 "The far right is the moon at its highest, the middle is the horizon.")]
        public ElevationGradientParameter moonDiscColor = new ElevationGradientParameter(DefaultMoonDiscColor(), true);

        [Header("Time of Day")]
        [Tooltip("Sun elevation, in degrees above the horizon, above which it is fully day. Dusk occupies everything " +
                 "between this and Night Elevation. The blend follows the sun on its own — sky, " +
                 "ambient and fog all read these gradients, so the whole scene changes together and " +
                 "nothing needs to be switched or animated.")]
        public ClampedFloatParameter duskElevation = new ClampedFloatParameter(30f, 1f, 40f);

        [Tooltip("Sun elevation, in degrees above the horizon, below which it is fully night. Negative means below it.")]
        public ClampedFloatParameter nightElevation = new ClampedFloatParameter(-8.6f, -30f, -1f);

        public static void InvalidateGradients()
        {
            SkyLutBaker.Invalidate();
        }

        public static Gradient DefaultDaySky() => Make(
            Key(0.4433962f, 0.4433962f, 0.4433962f, 0f),
            Key(0.8584906f, 0.8584906f, 0.8584906f, 0.45f),
            Key(0.95f, 0.96f, 0.98f, 0.50f),
            Key(0.55f, 0.72f, 0.93f, 0.70f),
            Key(0.28f, 0.52f, 0.88f, 1f));

        public static Gradient DefaultDuskSky() => Make(
            Key(0.10f, 0.07f, 0.09f, 0f),
            Key(0.22f, 0.13f, 0.16f, 0.44f),
            Key(1.00f, 0.44f, 0.19f, 0.50f),
            Key(0.85f, 0.35f, 0.30f, 0.68f),
            Key(0.3356989f, 0.2784314f, 0.4509804f, 0.85f),
            Key(0.22f, 0.20f, 0.42f, 1f));

        public static Gradient DefaultNightSky() => Make(
            Key(0.0200000f, 0.0250000f, 0.0500000f, 0f),
            Key(0.0400000f, 0.0500000f, 0.0900000f, 0.46f),
            Key(0.1000000f, 0.1200000f, 0.2000000f, 0.52f),
            Key(0.0020470f, 0.0054112f, 0.0188679f, 1f));

        public static Gradient DefaultDayAmbient() => Make(
            Key(0.2735849f, 0.2735849f, 0.2735849f, 0f),
            Key(0.5606978f, 0.5606978f, 0.6792453f, 0.1147f),
            Key(0.7436943f, 0.7451354f, 0.8431187f, 0.2294f),
            Key(0.8155037f, 0.8505028f, 0.9245283f, 0.50f),
            Key(1.0000000f, 0.9519084f, 0.8915094f, 1f));

        public static Gradient DefaultDuskAmbient() => Make(
            Key(0.2735849f, 0.1185056f, 0.0916251f, 0f),
            Key(0.5000000f, 0.3004838f, 0.2004717f, 0.1029f),
            Key(0.7547170f, 0.4336543f, 0.3666786f, 0.50f),
            Key(0.8962264f, 0.7226003f, 0.5115255f, 0.7735f),
            Key(0.9703549f, 0.8426444f, 0.6886137f, 0.8823f),
            Key(1.0000000f, 1.0000000f, 1.0000000f, 1f));

        public static Gradient DefaultNightAmbient() => Make(
            Key(0.0000000f, 0.0000000f, 0.0000000f, 0.0676f),
            Key(0.0620772f, 0.0620772f, 0.1415094f, 0.1882f),
            Key(0.1776878f, 0.1776878f, 0.3113208f, 0.4206f),
            Key(0.1292275f, 0.1454254f, 0.2264151f, 0.6676f),
            Key(0.4012104f, 0.4223923f, 0.5283019f, 0.8088f),
            Key(0.7722944f, 0.7869310f, 0.8396226f, 1f));

        public static Gradient DefaultSkyLight() => Make(
            Key(0.2321000f, 0.2527000f, 0.3133000f, 0f),
            Key(0.2321000f, 0.2527000f, 0.3133000f, 0.48f),
            Key(0.4433962f, 0.2087761f, 0.1024831f, 0.50f),
            Key(1.0000000f, 0.6414817f, 0.1650943f, 0.60f),
            Key(1.0000000f, 0.7120591f, 0.4481132f, 0.65f),
            Key(1.0000000f, 1.0000000f, 1.0000000f, 0.7412f),
            Key(1.0000000f, 1.0000000f, 1.0000000f, 1f));

        public static Gradient DefaultSunDiscColor() => Make(
            Key(0.0000f, 0.0000f, 0.0000f, 0.49f),
            Key(1.0000f, 0.7593924f, 0.0000f, 0.50f),
            Key(1.0000f, 0.9535000f, 0.9190f, 0.65f),
            Key(1.0000f, 0.9535000f, 0.9190f, 1f));

        public static Gradient DefaultMoonDiscColor() => Make(
            Key(1.0000f, 0.8500000f, 0.7000000f, 0f),
            Key(1.0000f, 0.9138691f, 0.8254717f, 0.52f),
            Key(0.9063f, 0.9309000f, 1.0000000f, 0.65f),
            Key(0.9063f, 0.9309000f, 1.0000000f, 1f));

        static readonly GradientAlphaKey[] s_Opaque =
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        internal static GradientColorKey Key(float r, float g, float b, float position)
        {
            return new GradientColorKey(new Color(r, g, b), position);
        }

        internal static Gradient Make(params GradientColorKey[] keys)
        {
            var gradient = new Gradient();
            gradient.SetKeys(keys, s_Opaque);
            return gradient;
        }
    }
}
