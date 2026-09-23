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

        [Tooltip("Blends from the gradient toward Unity's baked lightmaps and probes. 0 keeps the " +
                 "gradient, 1 uses baked indirect light only. Direct light and its shadows always " +
                 "stay realtime regardless.")]
        public ClampedFloatParameter bakedWeight = new ClampedFloatParameter(0f, 0f, 1f);

        [Header("Time of Day")]
        [Tooltip("Sun elevation, as a sine, above which it is fully day. Dusk occupies everything " +
                 "between this and Night Elevation. The blend follows the sun on its own — sky, " +
                 "ambient and fog all read these gradients, so the whole scene changes together and " +
                 "nothing needs to be switched or animated.")]
        public ClampedFloatParameter duskElevation = new ClampedFloatParameter(0.1f, -0.3f, 0.5f);

        [Tooltip("Sun elevation, as a sine, below which it is fully night.")]
        public ClampedFloatParameter nightElevation = new ClampedFloatParameter(-0.15f, -0.6f, 0.2f);

        [Header("Sun")]
        [Tooltip("Tint and brightness of the sun. The main directional light's own colour is " +
                 "multiplied in. Set the intensity to zero for a sky with no sun in it.")]
        public ColorParameter sunColor = new ColorParameter(new Color(1f, 0.8973985f, 0.8254717f), true, false, true);

        [Tooltip("The colour it reaches by dusk, on the same curve the sky gradients follow. It " +
                 "holds this colour once the sun is down rather than returning to the day one.")]
        public ColorParameter sunDuskColor = new ColorParameter(new Color(1f, 0.4895238f, 0f), true, false, true);

        public ClampedFloatParameter sunIntensity = new ClampedFloatParameter(0.85f, 0f, 20f);

        [Tooltip("The brightness it reaches by dusk. The dusk colour already darkens the disc on " +
                 "its own, so this starts matching the day value — push it down for a sun you can " +
                 "look at, or up for one that burns through the haze.")]
        public ClampedFloatParameter sunDuskIntensity = new ClampedFloatParameter(1.65f, 0f, 20f);

        [Tooltip("Angular radius of the disc, in degrees.")]
        public ClampedFloatParameter sunSize = new ClampedFloatParameter(0.1f, 0.1f, 30f);

        [Tooltip("The size it reaches by dusk. A low sun reading larger than a high one is most of " +
                 "what makes a sunset feel like one.")]
        public ClampedFloatParameter sunDuskSize = new ClampedFloatParameter(2f, 0.1f, 30f);

        [Tooltip("Size and strength of the glow around the disc, on one curve.")]
        public ClampedFloatParameter sunGlow = new ClampedFloatParameter(0.475f, 0f, 1f);

        [Tooltip("How far the brush atlas breaks up the disc edge, so the sun reads as painted " +
                 "rather than as a circle. Needs a brush atlas on the renderer feature.")]
        public ClampedFloatParameter sunBrush = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Rays radiating from the sun. 0 leaves the glow smooth.")]
        public ClampedFloatParameter sunRays = new ClampedFloatParameter(0.35f, 0f, 1f);

        [Header("Moon")]
        [Tooltip("Tint and brightness of the moon. It sits exactly opposite the sun, so it rises as " +
                 "the sun sets without anything to place or switch on. Set the intensity to zero " +
                 "for a moonless sky.")]
        public ColorParameter moonColor = new ColorParameter(new Color(0.8f, 0.85f, 1f), true, false, true);

        public ClampedFloatParameter moonIntensity = new ClampedFloatParameter(1.25f, 0f, 20f);

        [Tooltip("Angular radius of the moon, in degrees.")]
        public ClampedFloatParameter moonSize = new ClampedFloatParameter(0.125f, 0.1f, 30f);

        [Tooltip("Size and strength of the glow around the moon.")]
        public ClampedFloatParameter moonGlow = new ClampedFloatParameter(0.305f, 0f, 1f);

        [Header("Stars")]
        [Tooltip("How many stars there are and how brightly they burn, on one curve. They fade out " +
                 "on their own as the sun rises.")]
        public ClampedFloatParameter stars = new ClampedFloatParameter(0.75f, 0f, 1f);

        [Tooltip("How much stars vary in brightness over time.")]
        public ClampedFloatParameter starTwinkle = new ClampedFloatParameter(0.4f, 0f, 1f);

        public static void InvalidateGradients()
        {
            SkyLutBaker.Invalidate();
        }

        public static Gradient DefaultDaySky() => Make(
            Key(0.0754717f, 0.0754717f, 0.0754717f, 0f),
            Key(0.4245283f, 0.4245283f, 0.4245283f, 0.45f),
            Key(0.95f, 0.96f, 0.98f, 0.50f),
            Key(0.55f, 0.72f, 0.93f, 0.70f),
            Key(0.28f, 0.52f, 0.88f, 1f));

        public static Gradient DefaultDuskSky() => Make(
            Key(0.10f, 0.07f, 0.09f, 0f),
            Key(0.22f, 0.13f, 0.16f, 0.44f),
            Key(1.00f, 0.44f, 0.19f, 0.50f),
            Key(0.85f, 0.35f, 0.30f, 0.58f),
            Key(0.45f, 0.28f, 0.45f, 0.75f),
            Key(0.22f, 0.20f, 0.42f, 1f));

        public static Gradient DefaultNightSky() => Make(
            Key(0.0200000f, 0.0250000f, 0.0500000f, 0f),
            Key(0.0400000f, 0.0500000f, 0.0900000f, 0.46f),
            Key(0.1000000f, 0.1200000f, 0.2000000f, 0.52f),
            Key(0.0020470f, 0.0054112f, 0.0188679f, 1f));

        public static Gradient DefaultDayAmbient() => Make(
            Key(0.4357423f, 0.4357423f, 0.6037736f, 0f),
            Key(0.5471698f, 0.5471698f, 0.5471698f, 0.50f),
            Key(1.0000000f, 0.9519084f, 0.8915094f, 1f));

        public static Gradient DefaultDuskAmbient() => Make(
            Key(0.1132075f, 0.0720897f, 0.0857956f, 0f),
            Key(0.6980392f, 0.3275012f, 0.2784314f, 0.50f),
            Key(0.8584906f, 0.6572186f, 0.4170969f, 0.7735f),
            Key(0.6407530f, 0.6678663f, 0.7264151f, 1f));

        public static Gradient DefaultNightAmbient() => Make(
            Key(0.0483721f, 0.0483721f, 0.0800000f, 0f),
            Key(0.0703448f, 0.0703448f, 0.1200000f, 0.50f),
            Key(0.1346154f, 0.1480769f, 0.2100000f, 1f));

        static readonly GradientAlphaKey[] s_Opaque =
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        static GradientColorKey Key(float r, float g, float b, float position)
        {
            return new GradientColorKey(new Color(r, g, b), position);
        }

        static Gradient Make(params GradientColorKey[] keys)
        {
            var gradient = new Gradient();
            gradient.SetKeys(keys, s_Opaque);
            return gradient;
        }
    }
}
